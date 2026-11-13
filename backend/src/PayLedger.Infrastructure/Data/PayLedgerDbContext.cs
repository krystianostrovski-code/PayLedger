using Microsoft.EntityFrameworkCore;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Entities;

namespace PayLedger.Infrastructure.Data;

public class PayLedgerDbContext : DbContext, IPayLedgerDbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<LedgerTransaction> Transactions => Set<LedgerTransaction>();
    public DbSet<LedgerEntry> Entries => Set<LedgerEntry>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public PayLedgerDbContext(DbContextOptions<PayLedgerDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Account Configuration
        modelBuilder.Entity<Account>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasIndex(a => a.AccountNumber).IsUnique();
            b.Property(a => a.AccountNumber).IsRequired().HasMaxLength(50);
            b.Property(a => a.Name).IsRequired().HasMaxLength(150);
            b.Property(a => a.Currency).IsRequired().HasMaxLength(3);
            b.Property(a => a.Balance).HasPrecision(18, 4);
            b.Property(a => a.RowVersion).IsConcurrencyToken();

            b.HasMany(a => a.Entries)
                .WithOne(e => e.Account)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LedgerTransaction Configuration
        modelBuilder.Entity<LedgerTransaction>(b =>
        {
            b.HasKey(t => t.Id);
            b.HasIndex(t => t.ReferenceNumber).IsUnique();
            b.HasIndex(t => t.IdempotencyKey);
            b.Property(t => t.ReferenceNumber).IsRequired().HasMaxLength(50);
            b.Property(t => t.Description).IsRequired().HasMaxLength(300);

            b.HasMany(t => t.Entries)
                .WithOne()
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LedgerEntry Configuration
        modelBuilder.Entity<LedgerEntry>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.TransactionId);
            b.HasIndex(e => e.AccountId);
            b.Property(e => e.Amount).HasPrecision(18, 4);
            b.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            b.Property(e => e.Description).HasMaxLength(300);
        });

        // IdempotencyRecord Configuration
        modelBuilder.Entity<IdempotencyRecord>(b =>
        {
            b.HasKey(i => i.Key);
            b.Property(i => i.Key).HasMaxLength(128);
            b.Property(i => i.Endpoint).HasMaxLength(256);
            b.Property(i => i.RequestBodyHash).HasMaxLength(64);
        });
    }
}
