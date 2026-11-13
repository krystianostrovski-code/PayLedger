using PayLedger.Domain.Enums;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Domain.Entities;

public class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public Account? Account { get; private set; }
    public EntryDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "PLN";
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    protected LedgerEntry() { }

    public LedgerEntry(
        Guid transactionId,
        Guid accountId,
        EntryDirection direction,
        decimal amount,
        string currency,
        string description = "")
    {
        if (amount <= 0m)
            throw new InvalidAccountOperationException($"Entry amount must be positive. Provided: {amount}");

        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidAccountOperationException("Currency code is required.");

        Id = Guid.NewGuid();
        TransactionId = transactionId;
        AccountId = accountId;
        Direction = direction;
        Amount = Math.Round(amount, 4);
        Currency = currency.Trim().ToUpperInvariant();
        Description = description.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
