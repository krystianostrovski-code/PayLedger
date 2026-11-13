using MediatR;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.DTOs;
using PayLedger.Application.Interfaces;
using PayLedger.Domain.Entities;
using PayLedger.Domain.Enums;
using PayLedger.Domain.Exceptions;

namespace PayLedger.Application.Commands;

public record CreateAccountCommand(
    string Name,
    AccountType Type,
    string Currency,
    decimal InitialBalance = 0m,
    bool AllowOverdraft = false
) : IRequest<AccountDto>;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountDto>
{
    private readonly IPayLedgerDbContext _dbContext;

    public CreateAccountCommandHandler(IPayLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "PLN" : request.Currency.Trim().ToUpperInvariant();
        var accountNumber = GenerateAccountNumber(request.Type, currency);

        var account = new Account(
            accountNumber,
            request.Name,
            request.Type,
            currency,
            request.AllowOverdraft,
            0m // balance will be initialized through ledger if initialBalance > 0
        );

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.InitialBalance > 0)
        {
            // Create opening ledger balance transaction
            var openingClearingNum = $"EQUITY-OPENING-BALANCE-{currency}";
            var openingAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == openingClearingNum, cancellationToken);
            if (openingAccount == null)
            {
                openingAccount = new Account(openingClearingNum, $"Opening Balance Equity ({currency})", AccountType.Equity, currency, allowOverdraft: true);
                _dbContext.Accounts.Add(openingAccount);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var entries = new List<(Guid accountId, EntryDirection direction, decimal amount, string currency, string description)>
            {
                (account.Id, EntryDirection.Debit, request.InitialBalance, currency, "Opening balance deposit"),
                (openingAccount.Id, EntryDirection.Credit, request.InitialBalance, currency, $"Initial balance equity offset for {account.AccountNumber}")
            };

            var tx = LedgerTransaction.Create(TransactionType.Deposit, $"Opening balance for {account.Name}", entries);
            account.ApplyEntry(EntryDirection.Debit, request.InitialBalance);
            openingAccount.ApplyEntry(EntryDirection.Credit, request.InitialBalance);

            _dbContext.Transactions.Add(tx);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AccountDto(
            account.Id,
            account.AccountNumber,
            account.Name,
            account.Type,
            account.Type.ToString(),
            account.Currency,
            account.Balance,
            account.AllowOverdraft,
            account.IsActive,
            account.RowVersion,
            account.CreatedAt
        );
    }

    private static string GenerateAccountNumber(AccountType type, string currency)
    {
        var typeCode = type switch
        {
            AccountType.Asset => "AST",
            AccountType.Liability => "LIA",
            AccountType.Revenue => "REV",
            AccountType.Expense => "EXP",
            AccountType.Equity => "EQU",
            _ => "GEN"
        };
        var randomNum = new Random().Next(10000, 99999);
        return $"PL-{typeCode}-{currency}-{randomNum}";
    }
}
