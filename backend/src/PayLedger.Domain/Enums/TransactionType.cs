namespace PayLedger.Domain.Enums;

public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    Transfer = 3,
    CurrencyExchange = 4,
    FeeCharge = 5,
    StornoReversal = 6
}
