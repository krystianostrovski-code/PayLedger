using PayLedger.Domain.Enums;

namespace PayLedger.Application.DTOs;

public record AccountDto(
    Guid Id,
    string AccountNumber,
    string Name,
    AccountType Type,
    string TypeName,
    string Currency,
    decimal Balance,
    bool AllowOverdraft,
    bool IsActive,
    uint RowVersion,
    DateTimeOffset CreatedAt
);

public record LedgerEntryDto(
    Guid Id,
    Guid TransactionId,
    Guid AccountId,
    string AccountNumber,
    string AccountName,
    EntryDirection Direction,
    string DirectionName,
    decimal Amount,
    string Currency,
    string Description,
    DateTimeOffset CreatedAt
);

public record LedgerTransactionDto(
    Guid Id,
    string ReferenceNumber,
    TransactionType Type,
    string TypeName,
    TransactionStatus Status,
    string StatusName,
    string Description,
    string? IdempotencyKey,
    Guid? OriginalTransactionId,
    Guid? ReversedByTransactionId,
    DateTimeOffset CreatedAt,
    decimal TotalAmount,
    string Currency,
    List<LedgerEntryDto> Entries
);

public record CurrencyHoldingDto(
    string Currency,
    decimal Balance,
    decimal PlnEquivalent,
    decimal ExchangeRateToPln
);

public record LedgerSummaryDto(
    decimal TotalLiquidityPln,
    int ActiveAccountsCount,
    int TotalTransactionsCount,
    decimal DailyVolumePln,
    int IntegrityViolationsCount,
    List<CurrencyHoldingDto> Holdings
);

public record LedgerIntegrityReportDto(
    bool IsHealthy,
    DateTimeOffset CheckedAt,
    int TotalTransactionsAudited,
    int TotalAccountsAudited,
    decimal TotalDebitSumPln,
    decimal TotalCreditSumPln,
    List<string> Discrepancies
);
