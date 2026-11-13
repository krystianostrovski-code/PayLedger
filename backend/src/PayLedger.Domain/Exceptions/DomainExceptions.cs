using System.Globalization;

namespace PayLedger.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class UnbalancedLedgerException : DomainException
{
    public decimal TotalDebits { get; }
    public decimal TotalCredits { get; }
    public string Currency { get; }

    public UnbalancedLedgerException(string currency, decimal debits, decimal credits)
        : base(string.Create(CultureInfo.InvariantCulture, $"Double-entry invariant violated for {currency}: Total Debits ({debits:F4}) must equal Total Credits ({credits:F4}). Difference: {Math.Abs(debits - credits):F4}."))
    {
        Currency = currency;
        TotalDebits = debits;
        TotalCredits = credits;
    }
}

public class InsufficientFundsException : DomainException
{
    public Guid AccountId { get; }
    public decimal CurrentBalance { get; }
    public decimal RequestedAmount { get; }
    public string Currency { get; }

    public InsufficientFundsException(Guid accountId, string currency, decimal currentBalance, decimal requestedAmount)
        : base(string.Create(CultureInfo.InvariantCulture, $"Account {accountId} has insufficient funds. Available balance: {currentBalance:F2} {currency}, requested: {requestedAmount:F2} {currency}."))
    {
        AccountId = accountId;
        Currency = currency;
        CurrentBalance = currentBalance;
        RequestedAmount = requestedAmount;
    }
}

public class InactiveAccountException : DomainException
{
    public Guid AccountId { get; }

    public InactiveAccountException(Guid accountId)
        : base($"Account {accountId} is suspended or inactive. Operations are disallowed.")
    {
        AccountId = accountId;
    }
}

public class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(string expectedCurrency, string providedCurrency)
        : base($"Currency mismatch: expected {expectedCurrency}, but received {providedCurrency}.") { }
}

public class InvalidAccountOperationException : DomainException
{
    public InvalidAccountOperationException(string message) : base(message) { }
}

public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string message) : base(message) { }
}

public class DuplicateIdempotencyException : DomainException
{
    public string IdempotencyKey { get; }

    public DuplicateIdempotencyException(string key)
        : base($"An operation with Idempotency-Key '{key}' is already being processed or completed.")
    {
        IdempotencyKey = key;
    }
}
