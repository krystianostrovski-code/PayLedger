using PayLedger.Domain.Enums;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Domain.Entities;

public class LedgerTransaction
{
    public Guid Id { get; private set; }
    public string ReferenceNumber { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? IdempotencyKey { get; private set; }
    public Guid? OriginalTransactionId { get; private set; }
    public Guid? ReversedByTransactionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<LedgerEntry> _entries = new();
    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    protected LedgerTransaction() { }

    public static LedgerTransaction Create(
        TransactionType type,
        string description,
        IEnumerable<(Guid accountId, EntryDirection direction, decimal amount, string currency, string description)> entryDrafts,
        string? idempotencyKey = null,
        string? customReference = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidAccountOperationException("Transaction description is required.");

        var draftsList = entryDrafts.ToList();

        if (draftsList.Count < 2)
            throw new InvalidAccountOperationException("Double-entry accounting requires at least two entries per transaction.");

        if (!draftsList.Any(e => e.direction == EntryDirection.Debit) || !draftsList.Any(e => e.direction == EntryDirection.Credit))
            throw new InvalidAccountOperationException("A transaction must contain at least one Debit and at least one Credit leg.");

        // Group by currency to enforce double-entry zero-sum invariant per currency leg
        var currencyGroups = draftsList.GroupBy(e => e.currency.Trim().ToUpperInvariant());

        foreach (var group in currencyGroups)
        {
            var currency = group.Key;
            var totalDebits = Math.Round(group.Where(e => e.direction == EntryDirection.Debit).Sum(e => e.amount), 4);
            var totalCredits = Math.Round(group.Where(e => e.direction == EntryDirection.Credit).Sum(e => e.amount), 4);

            if (totalDebits != totalCredits)
            {
                throw new UnbalancedLedgerException(currency, totalDebits, totalCredits);
            }
        }

        var transaction = new LedgerTransaction
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = customReference ?? GenerateReferenceNumber(),
            Type = type,
            Status = TransactionStatus.Posted,
            Description = description.Trim(),
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var draft in draftsList)
        {
            var entry = new LedgerEntry(
                transaction.Id,
                draft.accountId,
                draft.direction,
                draft.amount,
                draft.currency,
                string.IsNullOrWhiteSpace(draft.description) ? description : draft.description
            );
            transaction._entries.Add(entry);
        }

        return transaction;
    }

    public LedgerTransaction CreateStorno(string reason)
    {
        if (Status == TransactionStatus.Reversed)
            throw new InvalidAccountOperationException($"Transaction {ReferenceNumber} has already been reversed.");

        if (Type == TransactionType.StornoReversal)
            throw new InvalidAccountOperationException("Cannot reverse a reversal transaction.");

        var stornoId = Guid.NewGuid();
        var stornoDescription = $"[STORNO / REVERSAL] {ReferenceNumber}: {reason.Trim()}";

        var reversedEntries = _entries.Select(e => (
            accountId: e.AccountId,
            direction: e.Direction == EntryDirection.Debit ? EntryDirection.Credit : EntryDirection.Debit,
            amount: e.Amount,
            currency: e.Currency,
            description: $"Reversal of entry {e.Id} ({e.Description})"
        )).ToList();

        var stornoTx = new LedgerTransaction
        {
            Id = stornoId,
            ReferenceNumber = $"REV-{ReferenceNumber}",
            Type = TransactionType.StornoReversal,
            Status = TransactionStatus.Posted,
            Description = stornoDescription,
            OriginalTransactionId = this.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var rev in reversedEntries)
        {
            stornoTx._entries.Add(new LedgerEntry(
                stornoTx.Id,
                rev.accountId,
                rev.direction,
                rev.amount,
                rev.currency,
                rev.description
            ));
        }

        this.Status = TransactionStatus.Reversed;
        this.ReversedByTransactionId = stornoTx.Id;

        return stornoTx;
    }

    private static string GenerateReferenceNumber()
    {
        var randomHex = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"TX-{DateTimeOffset.UtcNow:yyyyMMdd}-{randomHex}";
    }
}
