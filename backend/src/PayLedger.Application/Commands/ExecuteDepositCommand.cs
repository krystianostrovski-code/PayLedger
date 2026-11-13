using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Entities;
using PayLedger.Domain.Enums;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Application.Commands;

public record ExecuteDepositCommand(
    Guid AccountId,
    decimal Amount,
    string Reference,
    string? IdempotencyKey = null
) : IRequest<LedgerTransactionDto>;

public class ExecuteDepositCommandHandler : IRequestHandler<ExecuteDepositCommand, LedgerTransactionDto>
{
    private readonly IPayLedgerDbContext _dbContext;

    public ExecuteDepositCommandHandler(IPayLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LedgerTransactionDto> Handle(ExecuteDepositCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidAccountOperationException("Deposit amount must be strictly greater than zero.");

        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new InvalidAccountOperationException($"Account {request.AccountId} not found.");

        if (!account.IsActive)
            throw new InactiveAccountException(account.Id);

        var currency = account.Currency.ToUpperInvariant();

        // Technical clearing account for inbound funds (e.g. Bank Wire / Card Gateway)
        var clearingNumber = $"SETTLEMENT-INBOUND-{currency}";
        var clearingAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == clearingNumber, cancellationToken);

        if (clearingAccount == null)
        {
            clearingAccount = new Account(
                clearingNumber,
                $"Inbound Settlement Clearing ({currency})",
                AccountType.Liability,
                currency,
                allowOverdraft: true
            );
            _dbContext.Accounts.Add(clearingAccount);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var desc = string.IsNullOrWhiteSpace(request.Reference)
            ? $"Deposit to {account.AccountNumber}"
            : $"Deposit: {request.Reference.Trim()}";

        // Double-entry:
        // Asset (Customer account) is DEBITED (balance goes up)
        // Liability/Clearing is CREDITED (liability to client goes up / balanced)
        var entries = new List<(Guid accountId, EntryDirection direction, decimal amount, string currency, string description)>
        {
            (account.Id, EntryDirection.Debit, request.Amount, currency, desc),
            (clearingAccount.Id, EntryDirection.Credit, request.Amount, currency, $"Inbound funding offset for {account.AccountNumber}")
        };

        var transaction = LedgerTransaction.Create(
            TransactionType.Deposit,
            desc,
            entries,
            request.IdempotencyKey
        );

        account.ApplyEntry(EntryDirection.Debit, request.Amount);
        clearingAccount.ApplyEntry(EntryDirection.Credit, request.Amount);

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
                e.AccountId == account.Id ? account.AccountNumber : clearingAccount.AccountNumber,
                e.AccountId == account.Id ? account.Name : clearingAccount.Name,
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
