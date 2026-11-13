using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Entities;
using PayLedger.Domain.Enums;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Application.Commands;

public record ExecuteWithdrawalCommand(
    Guid AccountId,
    decimal Amount,
    string DestinationIban,
    string? IdempotencyKey = null
) : IRequest<LedgerTransactionDto>;

public class ExecuteWithdrawalCommandHandler : IRequestHandler<ExecuteWithdrawalCommand, LedgerTransactionDto>
{
    private readonly IPayLedgerDbContext _dbContext;

    public ExecuteWithdrawalCommandHandler(IPayLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LedgerTransactionDto> Handle(ExecuteWithdrawalCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidAccountOperationException("Withdrawal amount must be strictly positive.");

        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new InvalidAccountOperationException($"Account {request.AccountId} not found.");

        if (!account.IsActive)
            throw new InactiveAccountException(account.Id);

        var currency = account.Currency.ToUpperInvariant();

        var clearingNumber = $"SETTLEMENT-OUTBOUND-{currency}";
        var clearingAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == clearingNumber, cancellationToken);

        if (clearingAccount == null)
        {
            clearingAccount = new Account(
                clearingNumber,
                $"Outbound Settlement Clearing ({currency})",
                AccountType.Liability,
                currency,
                allowOverdraft: true
            );
            _dbContext.Accounts.Add(clearingAccount);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var ibanClean = string.IsNullOrWhiteSpace(request.DestinationIban) ? "EXTERNAL" : request.DestinationIban.Trim();
        var desc = $"Withdrawal to {ibanClean}";

        // Double-entry:
        // Asset (Customer account) is CREDITED (balance goes down)
        // Settlement Outbound is DEBITED (liability reduces / balanced)
        var entries = new List<(Guid accountId, EntryDirection direction, decimal amount, string currency, string description)>
        {
            (account.Id, EntryDirection.Credit, request.Amount, currency, desc),
            (clearingAccount.Id, EntryDirection.Debit, request.Amount, currency, $"Outbound payout to {ibanClean} for {account.AccountNumber}")
        };

        var transaction = LedgerTransaction.Create(
            TransactionType.Withdrawal,
            desc,
            entries,
            request.IdempotencyKey
        );

        account.ApplyEntry(EntryDirection.Credit, request.Amount);
        clearingAccount.ApplyEntry(EntryDirection.Debit, request.Amount);

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
