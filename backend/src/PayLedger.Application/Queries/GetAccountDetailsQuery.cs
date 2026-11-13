using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Application.Queries;

public record GetAccountDetailsQuery(Guid AccountId) : IRequest<AccountDetailsDto>;

public record AccountDetailsDto(
    AccountDto Account,
    List<LedgerEntryDto> RecentEntries
);

public class GetAccountDetailsQueryHandler : IRequestHandler<GetAccountDetailsQuery, AccountDetailsDto>
{
    private readonly IPayLedgerDbContext _dbContext;

    public GetAccountDetailsQueryHandler(IPayLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountDetailsDto> Handle(GetAccountDetailsQuery request, CancellationToken cancellationToken)
    {
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new InvalidAccountOperationException($"Account {request.AccountId} not found.");

        var entries = await _dbContext.Entries
            .AsNoTracking()
            .Where(e => e.AccountId == request.AccountId)
            .ToListAsync(cancellationToken);

        // Sort in memory to avoid SQLite DateTimeOffset limitation
        var sortedEntries = entries
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .ToList();

        var accountDto = new AccountDto(
            account.Id,
            account.AccountNumber,
            account.Name,
            account.Type,
            account.Type.ToString(),
            account.Currency,
            account.Balance,
            account.AllowOverdraft,
            account.IsActive,
            account.RowVersion,
            account.CreatedAt
        );

        var entriesDto = sortedEntries.Select(e => new LedgerEntryDto(
            e.Id,
            e.TransactionId,
            e.AccountId,
            account.AccountNumber,
            account.Name,
            e.Direction,
            e.Direction.ToString(),
            e.Amount,
            e.Currency,
            e.Description,
            e.CreatedAt
        )).ToList();

        return new AccountDetailsDto(accountDto, entriesDto);
    }
}
