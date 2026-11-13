using Microsoft.EntityFrameworkCore;
using PayLedger.Domain.Entities;

namespace PayLedger.Application.Interfaces;

public interface IPayLedgerDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<LedgerTransaction> Transactions { get; }
    DbSet<LedgerEntry> Entries { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
