using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Entities;
using PayLedger.Domain.Enums;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Application.Commands;

public record ExecuteExchangeCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal SourceAmount,
    string? IdempotencyKey = null
) : IRequest<LedgerTransactionDto>;

public class ExecuteExchangeCommandHandler : IRequestHandler<ExecuteExchangeCommand, LedgerTransactionDto>
{
    private readonly IPayLedgerDbContext _dbContext;
    private readonly IExchangeRateService _exchangeRateService;

    public ExecuteExchangeCommandHandler(IPayLedgerDbContext dbContext, IExchangeRateService exchangeRateService)
    {
        _dbContext = dbContext;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<LedgerTransactionDto> Handle(ExecuteExchangeCommand request, CancellationToken cancellationToken)
    {
        if (request.SourceAmount <= 0)
            throw new InvalidAccountOperationException("Exchange amount must be positive.");

        var source = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId, cancellationToken)
            ?? throw new InvalidAccountOperationException($"Source account {request.SourceAccountId} not found.");

        var destination = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId, cancellationToken)
            ?? throw new InvalidAccountOperationException($"Destination account {request.DestinationAccountId} not found.");

        if (!source.IsActive || !destination.IsActive)
            throw new InactiveAccountException(!source.IsActive ? source.Id : destination.Id);

        var srcCurrency = source.Currency.ToUpperInvariant();
        var destCurrency = destination.Currency.ToUpperInvariant();

        if (srcCurrency == destCurrency)
            throw new InvalidAccountOperationException("Currency exchange requires accounts of different currencies. Use Transfer instead.");

        var rate = _exchangeRateService.GetRate(srcCurrency, destCurrency);
        var destAmount = Math.Round(request.SourceAmount * rate, 4);

        // Technical FX clearing accounts for each currency leg
        var fxClearingSrcNumber = $"FX-CLEARING-{srcCurrency}";
        var fxClearingDestNumber = $"FX-CLEARING-{destCurrency}";

        var fxClearingSrc = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == fxClearingSrcNumber, cancellationToken);
        if (fxClearingSrc == null)
        {
            fxClearingSrc = new Account(fxClearingSrcNumber, $"FX Clearing {srcCurrency}", AccountType.Asset, srcCurrency, allowOverdraft: true);
            _dbContext.Accounts.Add(fxClearingSrc);
        }

        var fxClearingDest = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == fxClearingDestNumber, cancellationToken);
        if (fxClearingDest == null)
        {
            fxClearingDest = new Account(fxClearingDestNumber, $"FX Clearing {destCurrency}", AccountType.Asset, destCurrency, allowOverdraft: true);
            _dbContext.Accounts.Add(fxClearingDest);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var desc = $"FX Conversion: {request.SourceAmount:F2} {srcCurrency} -> {destAmount:F2} {destCurrency} (Rate: {rate:F4})";

        // Double-entry 4-legged transaction:
        // Leg 1 (Source Currency): Source credited, FX clearing debited
        // Leg 2 (Dest Currency): FX clearing credited, Dest debited
        var entries = new List<(Guid accountId, EntryDirection direction, decimal amount, string currency, string description)>
        {
            (source.Id, EntryDirection.Credit, request.SourceAmount, srcCurrency, $"Sold {request.SourceAmount:F2} {srcCurrency} @ {rate:F4}"),
            (fxClearingSrc.Id, EntryDirection.Debit, request.SourceAmount, srcCurrency, $"FX Inbound {srcCurrency} pool"),
            (fxClearingDest.Id, EntryDirection.Credit, destAmount, destCurrency, $"FX Outbound {destCurrency} pool"),
            (destination.Id, EntryDirection.Debit, destAmount, destCurrency, $"Bought {destAmount:F2} {destCurrency} @ {rate:F4}")
        };

        var transaction = LedgerTransaction.Create(
            TransactionType.CurrencyExchange,
            desc,
            entries,
            request.IdempotencyKey
        );

        source.ApplyEntry(EntryDirection.Credit, request.SourceAmount);
        fxClearingSrc.ApplyEntry(EntryDirection.Debit, request.SourceAmount);
        fxClearingDest.ApplyEntry(EntryDirection.Credit, destAmount);
        destination.ApplyEntry(EntryDirection.Debit, destAmount);

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LedgerTransactionDto(
            transaction.Id,
            transaction.ReferenceNumber,
            transaction.Type,
            transaction.Type.ToString(),
            transaction.Status,
            transaction.Status.ToString(),
            transaction.Description,
            transaction.IdempotencyKey,
            transaction.OriginalTransactionId,
            transaction.ReversedByTransactionId,
            transaction.CreatedAt,
            request.SourceAmount,
            srcCurrency,
            transaction.Entries.Select(e => new LedgerEntryDto(
                e.Id,
                e.TransactionId,
                e.AccountId,
                e.AccountId == source.Id ? source.AccountNumber : e.AccountId == destination.Id ? destination.AccountNumber : "FX-CLEARING",
                e.AccountId == source.Id ? source.Name : e.AccountId == destination.Id ? destination.Name : "FX Clearing Pool",
                e.Direction,
                e.Direction.ToString(),
                e.Amount,
                e.Currency,
                e.Description,
                e.CreatedAt
            )).ToList()
        );
    }
}
