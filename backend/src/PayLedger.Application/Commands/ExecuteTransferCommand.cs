using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Entities;
using PayLedger.Domain.Enums;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Application.Commands;

public record ExecuteTransferCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Description,
    string? IdempotencyKey = null
) : IRequest<LedgerTransactionDto>;

public class ExecuteTransferCommandHandler : IRequestHandler<ExecuteTransferCommand, LedgerTransactionDto>
{
    private readonly IPayLedgerDbContext _dbContext;

    public ExecuteTransferCommandHandler(IPayLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LedgerTransactionDto> Handle(ExecuteTransferCommand request, CancellationToken cancellationToken)
    {
        if (request.SourceAccountId == request.DestinationAccountId)
            throw new InvalidAccountOperationException("Source and destination accounts cannot be identical.");

        if (request.Amount <= 0)
            throw new InvalidAccountOperationException("Transfer amount must be strictly greater than zero.");

        var source = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId, cancellationToken)
            ?? throw new InvalidAccountOperationException($"Source account {request.SourceAccountId} not found.");

        var destination = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId, cancellationToken)
            ?? throw new InvalidAccountOperationException($"Destination account {request.DestinationAccountId} not found.");

        if (!source.IsActive)
            throw new InactiveAccountException(source.Id);

        if (!destination.IsActive)
            throw new InactiveAccountException(destination.Id);

        if (!string.Equals(source.Currency, destination.Currency, StringComparison.OrdinalIgnoreCase))
            throw new CurrencyMismatchException(source.Currency, destination.Currency);

        var currency = source.Currency.ToUpperInvariant();
        var desc = string.IsNullOrWhiteSpace(request.Description)
            ? $"Transfer from {source.AccountNumber} to {destination.AccountNumber}"
            : request.Description.Trim();

        // In standard double-entry accounting for Asset accounts:
        // Source Account is CREDITED (decreases balance)
        // Destination Account is DEBITED (increases balance)
        var entries = new List<(Guid accountId, EntryDirection direction, decimal amount, string currency, string description)>
        {
            (source.Id, EntryDirection.Credit, request.Amount, currency, $"Transfer out to {destination.AccountNumber}: {desc}"),
            (destination.Id, EntryDirection.Debit, request.Amount, currency, $"Transfer in from {source.AccountNumber}: {desc}")
        };

        var transaction = LedgerTransaction.Create(
            TransactionType.Transfer,
            desc,
            entries,
            request.IdempotencyKey
        );

        // Apply to domain aggregates
        source.ApplyEntry(EntryDirection.Credit, request.Amount);
        destination.ApplyEntry(EntryDirection.Debit, request.Amount);

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
            request.Amount,
            currency,
            transaction.Entries.Select(e => new LedgerEntryDto(
                e.Id,
                e.TransactionId,
                e.AccountId,
                e.AccountId == source.Id ? source.AccountNumber : destination.AccountNumber,
                e.AccountId == source.Id ? source.Name : destination.Name,
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
