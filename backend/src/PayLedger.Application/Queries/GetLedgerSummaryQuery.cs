using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Enums;

namespace PayLedger.Application.Queries;

public record GetLedgerSummaryQuery : IRequest<LedgerSummaryDto>;

public class GetLedgerSummaryQueryHandler : IRequestHandler<GetLedgerSummaryQuery, LedgerSummaryDto>
{
    private readonly IPayLedgerDbContext _dbContext;
    private readonly IExchangeRateService _exchangeRateService;

    public GetLedgerSummaryQueryHandler(IPayLedgerDbContext dbContext, IExchangeRateService exchangeRateService)
    {
        _dbContext = dbContext;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<LedgerSummaryDto> Handle(GetLedgerSummaryQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _dbContext.Accounts.AsNoTracking().ToListAsync(cancellationToken);
        var activeAccountsCount = accounts.Count(a => a.IsActive);

        var rates = _exchangeRateService.GetRatesAgainstPln();

        // Calculate liquidity by currency for customer asset accounts
        var assetAccounts = accounts
            .Where(a => a.Type == AccountType.Asset && !a.AccountNumber.StartsWith("SETTLEMENT-") && !a.AccountNumber.StartsWith("FX-"))
            .ToList();

        var holdings = assetAccounts
            .GroupBy(a => a.Currency)
            .Select(g =>
            {
                var cur = g.Key;
                var totalBal = g.Sum(a => a.Balance);
                var rate = rates.TryGetValue(cur, out var r) ? r : 1.0m;
                var plnEquiv = totalBal * rate;
                return new CurrencyHoldingDto(cur, totalBal, plnEquiv, rate);
            })
            .OrderByDescending(h => h.PlnEquivalent)
            .ToList();

        var totalLiquidityPln = holdings.Sum(h => h.PlnEquivalent);
        var totalTransactions = await _dbContext.Transactions.CountAsync(cancellationToken);

        // Daily volume: evaluate in memory to avoid SQLite DateTimeOffset translation issues
        var startOfToday = DateTimeOffset.UtcNow.Date;
        var allDebitEntries = await _dbContext.Entries
            .AsNoTracking()
            .Where(e => e.Direction == EntryDirection.Debit)
            .ToListAsync(cancellationToken);

        var dailyVolumeEntries = allDebitEntries.Where(e => e.CreatedAt.UtcDateTime.Date >= startOfToday).ToList();

        var dailyVolumePln = dailyVolumeEntries.Sum(e =>
        {
            var r = rates.TryGetValue(e.Currency, out var rate) ? rate : 1.0m;
            return e.Amount * r;
        });

        return new LedgerSummaryDto(
            totalLiquidityPln,
            activeAccountsCount,
            totalTransactions,
            dailyVolumePln,
            0,
            holdings
        );
    }
}
