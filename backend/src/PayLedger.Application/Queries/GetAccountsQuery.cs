using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Enums;

namespace PayLedger.Application.Queries;

public record GetAccountsQuery(
    AccountType? Type = null,
    string? Currency = null,
    string? Search = null
) : IRequest<List<AccountDto>>;

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, List<AccountDto>>
{
    private readonly IPayLedgerDbContext _dbContext;

    public GetAccountsQueryHandler(IPayLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Accounts.AsNoTracking().AsQueryable();

        if (request.Type.HasValue)
            query = query.Where(a => a.Type == request.Type.Value);

        if (!string.IsNullOrWhiteSpace(request.Currency))
            query = query.Where(a => a.Currency == request.Currency.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(a => a.AccountNumber.ToLower().Contains(search) || a.Name.ToLower().Contains(search));
        }

        var accounts = await query.ToListAsync(cancellationToken);

        // Sort in memory to avoid SQLite decimal ORDER BY limitation
        return accounts
            .OrderByDescending(a => a.Balance)
            .Select(a => new AccountDto(
                a.Id,
                a.AccountNumber,
                a.Name,
                a.Type,
                a.Type.ToString(),
                a.Currency,
                a.Balance,
                a.AllowOverdraft,
                a.IsActive,
                a.RowVersion,
                a.CreatedAt
            )).ToList();
    }
}
