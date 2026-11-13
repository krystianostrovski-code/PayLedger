using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Enums;

namespace PayLedger.Application.Queries;

public record VerifyLedgerIntegrityQuery : IRequest<LedgerIntegrityReportDto>;

public class VerifyLedgerIntegrityQueryHandler : IRequestHandler<VerifyLedgerIntegrityQuery, LedgerIntegrityReportDto>
{
    private readonly IPayLedgerDbContext _dbContext;
    private readonly IExchangeRateService _exchangeRateService;

    public VerifyLedgerIntegrityQueryHandler(IPayLedgerDbContext dbContext, IExchangeRateService exchangeRateService)
    {
        _dbContext = dbContext;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<LedgerIntegrityReportDto> Handle(VerifyLedgerIntegrityQuery request, CancellationToken cancellationToken)
    {
        var discrepancies = new List<string>();

        // 1. Audit all transactions for double-entry zero-sum balance
        var transactions = await _dbContext.Transactions
            .Include(t => t.Entries)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        decimal totalDebitSumPln = 0m;
        decimal totalCreditSumPln = 0m;
        var rates = _exchangeRateService.GetRatesAgainstPln();

        foreach (var tx in transactions)
        {
            var currencyGroups = tx.Entries.GroupBy(e => e.Currency.ToUpperInvariant());
            foreach (var group in currencyGroups)
            {
                var debits = group.Where(e => e.Direction == EntryDirection.Debit).Sum(e => e.Amount);
                var credits = group.Where(e => e.Direction == EntryDirection.Credit).Sum(e => e.Amount);

                if (debits != credits)
                {
                    discrepancies.Add($"Transaction {tx.ReferenceNumber} ({tx.Id}) has unbalanced {group.Key} leg: Debits={debits:F4}, Credits={credits:F4}");
                }

                var r = rates.TryGetValue(group.Key, out var rate) ? rate : 1.0m;
                totalDebitSumPln += debits * r;
                totalCreditSumPln += credits * r;
            }
        }

        // 2. Audit all accounts: verify running balance against all historical entries
        var accounts = await _dbContext.Accounts.AsNoTracking().ToListAsync(cancellationToken);
        var entries = await _dbContext.Entries.AsNoTracking().ToListAsync(cancellationToken);
        var entriesByAccount = entries.GroupBy(e => e.AccountId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var acc in accounts)
        {
            if (entriesByAccount.TryGetValue(acc.Id, out var accEntries))
            {
                decimal calculated;
                if (acc.Type == AccountType.Asset || acc.Type == AccountType.Expense)
                {
                    var debits = accEntries.Where(e => e.Direction == EntryDirection.Debit).Sum(e => e.Amount);
                    var credits = accEntries.Where(e => e.Direction == EntryDirection.Credit).Sum(e => e.Amount);
                    calculated = debits - credits;
                }
                else
                {
                    var credits = accEntries.Where(e => e.Direction == EntryDirection.Credit).Sum(e => e.Amount);
                    var debits = accEntries.Where(e => e.Direction == EntryDirection.Debit).Sum(e => e.Amount);
                    calculated = credits - debits;
                }

                if (Math.Round(calculated, 4) != Math.Round(acc.Balance, 4))
                {
                    discrepancies.Add($"Account {acc.AccountNumber} balance mismatch: stored {acc.Balance:F4} != calculated {calculated:F4}");
                }
            }
        }

        return new LedgerIntegrityReportDto(
            IsHealthy: discrepancies.Count == 0,
            CheckedAt: DateTimeOffset.UtcNow,
            TotalTransactionsAudited: transactions.Count,
            TotalAccountsAudited: accounts.Count,
            TotalDebitSumPln: totalDebitSumPln,
            TotalCreditSumPln: totalCreditSumPln,
            Discrepancies: discrepancies
        );
    }
}
