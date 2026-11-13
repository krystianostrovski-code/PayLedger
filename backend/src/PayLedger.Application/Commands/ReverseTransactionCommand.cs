using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Enums;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Application.Commands;

public record ReverseTransactionCommand(
    Guid TransactionId,
    string Reason
) : IRequest<LedgerTransactionDto>;

public class ReverseTransactionCommandHandler : IRequestHandler<ReverseTransactionCommand, LedgerTransactionDto>
{
    private readonly IPayLedgerDbContext _dbContext;

    public ReverseTransactionCommandHandler(IPayLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LedgerTransactionDto> Handle(ReverseTransactionCommand request, CancellationToken cancellationToken)
    {
        var originalTx = await _dbContext.Transactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken)
            ?? throw new InvalidAccountOperationException($"Transaction {request.TransactionId} not found.");

        var stornoTx = originalTx.CreateStorno(request.Reason);

        // Apply reversed entries to accounts
        var accountIds = stornoTx.Entries.Select(e => e.AccountId).Distinct().ToList();
        var accounts = await _dbContext.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        foreach (var entry in stornoTx.Entries)
        {
            if (accounts.TryGetValue(entry.AccountId, out var acc))
            {
                acc.ApplyEntry(entry.Direction, entry.Amount);
            }
        }

        _dbContext.Transactions.Add(stornoTx);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LedgerTransactionDto(
            stornoTx.Id,
            stornoTx.ReferenceNumber,
            stornoTx.Type,
            stornoTx.Type.ToString(),
            stornoTx.Status,
            stornoTx.Status.ToString(),
            stornoTx.Description,
            stornoTx.IdempotencyKey,
            stornoTx.OriginalTransactionId,
            stornoTx.ReversedByTransactionId,
            stornoTx.CreatedAt,
            originalTx.Entries.FirstOrDefault()?.Amount ?? 0,
            originalTx.Entries.FirstOrDefault()?.Currency ?? "PLN",
            stornoTx.Entries.Select(e => new LedgerEntryDto(
                e.Id,
                e.TransactionId,
                e.AccountId,
                accounts.TryGetValue(e.AccountId, out var a) ? a.AccountNumber : e.AccountId.ToString(),
                accounts.TryGetValue(e.AccountId, out var accObj) ? accObj.Name : "Account",
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
