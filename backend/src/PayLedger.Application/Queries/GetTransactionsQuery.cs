using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Enums;

namespace PayLedger.Application.Queries;

public record GetTransactionsQuery(
    Guid? AccountId = null,
    TransactionType? Type = null,
    TransactionStatus? Status = null,
    int Limit = 50
) : IRequest<List<LedgerTransactionDto>>;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, List<LedgerTransactionDto>>
{
    private readonly IPayLedgerDbContext _dbContext;

    public GetTransactionsQueryHandler(IPayLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<LedgerTransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Transactions
            .Include(t => t.Entries)
            .AsNoTracking()
            .AsQueryable();

        if (request.Type.HasValue)
            query = query.Where(t => t.Type == request.Type.Value);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.AccountId.HasValue)
            query = query.Where(t => t.Entries.Any(e => e.AccountId == request.AccountId.Value));

        var transactions = await query.ToListAsync(cancellationToken);

        // In-memory sort to support SQLite DateTimeOffset
        var sorted = transactions
            .OrderByDescending(t => t.CreatedAt)
            .Take(Math.Clamp(request.Limit, 1, 200))
            .ToList();

        // Fetch accounts for entry labels
        var accountIds = sorted.SelectMany(t => t.Entries).Select(e => e.AccountId).Distinct().ToList();
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        return sorted.Select(t =>
        {
            var firstEntry = t.Entries.FirstOrDefault();
            var totalAmount = t.Entries
                .Where(e => e.Direction == EntryDirection.Debit)
                .Sum(e => e.Amount);

            var currency = firstEntry?.Currency ?? "PLN";

            return new LedgerTransactionDto(
                t.Id,
                t.ReferenceNumber,
                t.Type,
                t.Type.ToString(),
                t.Status,
                t.Status.ToString(),
                t.Description,
                t.IdempotencyKey,
                t.OriginalTransactionId,
                t.ReversedByTransactionId,
                t.CreatedAt,
                totalAmount,
                currency,
                t.Entries.Select(e => new LedgerEntryDto(
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
        }).ToList();
    }
}
