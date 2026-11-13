using PayLedger.Domain.Enums;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }
    public string Currency { get; private set; } = "PLN";
    public decimal Balance { get; private set; }
    public bool AllowOverdraft { get; private set; }
    public bool IsActive { get; private set; }
    public uint RowVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    private readonly List<LedgerEntry> _entries = new();
    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    protected Account() { }

    public Account(
        string accountNumber,
        string name,
        AccountType type,
        string currency,
        bool allowOverdraft = false,
        decimal initialBalance = 0m)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new InvalidAccountOperationException("Account number is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidAccountOperationException("Account name is required.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new InvalidAccountOperationException("Valid 3-letter currency code (e.g. PLN, EUR, USD) is required.");

        Id = Guid.NewGuid();
        AccountNumber = accountNumber.Trim().ToUpperInvariant();
        Name = name.Trim();
        Type = type;
        Currency = currency.Trim().ToUpperInvariant();
        AllowOverdraft = allowOverdraft;
        Balance = initialBalance;
        IsActive = true;
        RowVersion = 1;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ApplyEntry(EntryDirection direction, decimal amount)
    {
        if (!IsActive)
            throw new InactiveAccountException(Id);

        if (amount <= 0m)
            throw new InvalidAccountOperationException($"Entry amount must be strictly positive. Provided: {amount}");

        decimal delta;

        // In standard double-entry bookkeeping:
        // Assets & Expenses increase with DEBIT, decrease with CREDIT.
        // Liabilities, Revenue & Equity increase with CREDIT, decrease with DEBIT.
        if (Type == AccountType.Asset || Type == AccountType.Expense)
        {
            delta = direction == EntryDirection.Debit ? amount : -amount;
        }
        else
        {
            delta = direction == EntryDirection.Credit ? amount : -amount;
        }

        if (!AllowOverdraft && Type == AccountType.Asset && (Balance + delta) < 0m)
        {
            throw new InsufficientFundsException(Id, Currency, Balance, amount);
        }

        Balance += delta;
        RowVersion++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
