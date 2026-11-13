using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PayLedger.Application.Commands;
using PayLedger.Application.Queries;
using PayLedger.Domain.Entities;
using PayLedger.Domain.Enums;
using PayLedger.Domain.Exceptions;
using PayLedger.Infrastructure.Data;
using PayLedger.Infrastructure.Services;
using Xunit;

namespace PayLedger.UnitTests;

public class CqrsHandlerTests : IDisposable
{
    private readonly PayLedgerDbContext _dbContext;
    private readonly ExchangeRateService _exchangeRateService;

    public CqrsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PayLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PayLedgerDbContext(options);
        _exchangeRateService = new ExchangeRateService();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task ExecuteTransfer_ShouldUpdateBothBalancesAtomicallyAndCreateBalancedTransaction()
    {
        // Arrange
        var source = new Account("PL-AST-PLN-101", "Source Wallet", AccountType.Asset, "PLN", initialBalance: 1000m);
        var dest = new Account("PL-AST-PLN-102", "Dest Wallet", AccountType.Asset, "PLN", initialBalance: 200m);
        _dbContext.Accounts.AddRange(source, dest);
        await _dbContext.SaveChangesAsync();

        var handler = new ExecuteTransferCommandHandler(_dbContext);
        var command = new ExecuteTransferCommand(source.Id, dest.Id, 350m, "Invoice settlement");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(TransactionStatus.Posted);
        result.TotalAmount.Should().Be(350m);
        result.Entries.Should().HaveCount(2);

        var updatedSource = await _dbContext.Accounts.FindAsync(source.Id);
        var updatedDest = await _dbContext.Accounts.FindAsync(dest.Id);

        updatedSource!.Balance.Should().Be(650m);
        updatedDest!.Balance.Should().Be(550m);
    }

    [Fact]
    public async Task ExecuteTransfer_ShouldThrowCurrencyMismatch_WhenCurrenciesDiffer()
    {
        // Arrange
        var plnAcc = new Account("PL-AST-PLN-101", "PLN Account", AccountType.Asset, "PLN", initialBalance: 1000m);
        var eurAcc = new Account("PL-AST-EUR-102", "EUR Account", AccountType.Asset, "EUR", initialBalance: 1000m);
        _dbContext.Accounts.AddRange(plnAcc, eurAcc);
        await _dbContext.SaveChangesAsync();

        var handler = new ExecuteTransferCommandHandler(_dbContext);
        var command = new ExecuteTransferCommand(plnAcc.Id, eurAcc.Id, 100m, "Invalid cross-currency transfer");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CurrencyMismatchException>()
            .WithMessage("*expected PLN, but received EUR*");
    }

    [Fact]
    public async Task ExecuteTransfer_ShouldThrowInsufficientFunds_WhenBalanceTooLow()
    {
        // Arrange
        var source = new Account("PL-AST-PLN-101", "Poor Wallet", AccountType.Asset, "PLN", initialBalance: 50m);
        var dest = new Account("PL-AST-PLN-102", "Dest Wallet", AccountType.Asset, "PLN", initialBalance: 0m);
        _dbContext.Accounts.AddRange(source, dest);
        await _dbContext.SaveChangesAsync();

        var handler = new ExecuteTransferCommandHandler(_dbContext);
        var command = new ExecuteTransferCommand(source.Id, dest.Id, 100m, "Attempted overdraft");

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InsufficientFundsException>();
    }

    [Fact]
    public async Task ExecuteDeposit_ShouldIncreaseBalanceAndCreditClearingAccount()
    {
        // Arrange
        var account = new Account("PL-AST-PLN-201", "Client Deposit Account", AccountType.Asset, "PLN", initialBalance: 0m);
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        var handler = new ExecuteDepositCommandHandler(_dbContext);
        var command = new ExecuteDepositCommand(account.Id, 1250m, "Direct SEPA wire");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(TransactionType.Deposit);
        result.Entries.Should().HaveCount(2);

        var updatedAccount = await _dbContext.Accounts.FindAsync(account.Id);
        updatedAccount!.Balance.Should().Be(1250m);
    }

    [Fact]
    public async Task ExecuteWithdrawal_ShouldDecreaseBalanceAndDebitClearingAccount()
    {
        // Arrange
        var account = new Account("PL-AST-PLN-202", "Client Withdrawal Account", AccountType.Asset, "PLN", initialBalance: 800m);
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        var handler = new ExecuteWithdrawalCommandHandler(_dbContext);
        var command = new ExecuteWithdrawalCommand(account.Id, 300m, "PL61109010140000071219812874");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(TransactionType.Withdrawal);

        var updatedAccount = await _dbContext.Accounts.FindAsync(account.Id);
        updatedAccount!.Balance.Should().Be(500m);
    }

    [Fact]
    public async Task ExecuteExchange_ShouldConvertCrossCurrencyUsingClearingPool()
    {
        // Arrange
        var eurAccount = new Account("PL-AST-EUR-101", "Client EUR", AccountType.Asset, "EUR", initialBalance: 500m);
        var plnAccount = new Account("PL-AST-PLN-102", "Client PLN", AccountType.Asset, "PLN", initialBalance: 100m);
        _dbContext.Accounts.AddRange(eurAccount, plnAccount);
        await _dbContext.SaveChangesAsync();

        var handler = new ExecuteExchangeCommandHandler(_dbContext, _exchangeRateService);
        var command = new ExecuteExchangeCommand(eurAccount.Id, plnAccount.Id, 100m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(TransactionType.CurrencyExchange);
        result.Entries.Should().HaveCount(4); // 4 legs (2 in EUR, 2 in PLN)

        var updatedEur = await _dbContext.Accounts.FindAsync(eurAccount.Id);
        var updatedPln = await _dbContext.Accounts.FindAsync(plnAccount.Id);

        updatedEur!.Balance.Should().Be(400m); // 500 - 100 EUR

        // EUR/PLN rate is 4.2850 -> 100 EUR = 428.50 PLN
        updatedPln!.Balance.Should().Be(528.50m); // 100 + 428.50 PLN
    }

    [Fact]
    public async Task ReverseTransaction_ShouldUndoBalancesCompletely()
    {
        // Arrange
        var accA = new Account("PL-AST-PLN-01", "Acc A", AccountType.Asset, "PLN", initialBalance: 500m);
        var accB = new Account("PL-AST-PLN-02", "Acc B", AccountType.Asset, "PLN", initialBalance: 100m);
        _dbContext.Accounts.AddRange(accA, accB);
        await _dbContext.SaveChangesAsync();

        var transferHandler = new ExecuteTransferCommandHandler(_dbContext);
        var transferResult = await transferHandler.Handle(new ExecuteTransferCommand(accA.Id, accB.Id, 200m, "Mistake payment"), CancellationToken.None);

        // Post-transfer verification
        accA.Balance.Should().Be(300m);
        accB.Balance.Should().Be(300m);

        // Act: Reverse transaction (Storno)
        var reverseHandler = new ReverseTransactionCommandHandler(_dbContext);
        var stornoResult = await reverseHandler.Handle(new ReverseTransactionCommand(transferResult.Id, "Customer refund request"), CancellationToken.None);

        // Assert
        stornoResult.Should().NotBeNull();
        stornoResult.Type.Should().Be(TransactionType.StornoReversal);

        var restoredA = await _dbContext.Accounts.FindAsync(accA.Id);
        var restoredB = await _dbContext.Accounts.FindAsync(accB.Id);

        restoredA!.Balance.Should().Be(500m);
        restoredB!.Balance.Should().Be(100m);
    }

    [Fact]
    public async Task VerifyLedgerIntegrity_ShouldReportHealthy_WhenAllLedgersBalanced()
    {
        // Arrange
        var accA = new Account("PL-AST-PLN-01", "Acc A", AccountType.Asset, "PLN", initialBalance: 0m);
        var accB = new Account("PL-AST-PLN-02", "Acc B", AccountType.Asset, "PLN", initialBalance: 0m);
        _dbContext.Accounts.AddRange(accA, accB);
        await _dbContext.SaveChangesAsync();

        // Perform valid deposit and transfer
        var depositHandler = new ExecuteDepositCommandHandler(_dbContext);
        await depositHandler.Handle(new ExecuteDepositCommand(accA.Id, 1000m, "Inbound wire"), CancellationToken.None);

        var transferHandler = new ExecuteTransferCommandHandler(_dbContext);
        await transferHandler.Handle(new ExecuteTransferCommand(accA.Id, accB.Id, 400m, "Subcontractor payout"), CancellationToken.None);

        var auditHandler = new VerifyLedgerIntegrityQueryHandler(_dbContext, _exchangeRateService);

        // Act
        var report = await auditHandler.Handle(new VerifyLedgerIntegrityQuery(), CancellationToken.None);

        // Assert
        report.IsHealthy.Should().BeTrue();
        report.Discrepancies.Should().BeEmpty();
        report.TotalTransactionsAudited.Should().Be(2);
    }

    [Fact]
    public void IdempotencyRecord_ShouldCorrectlyDetectExpiration()
    {
        // Arrange
        var activeRecord = new IdempotencyRecord("KEY-ACTIVE", "/api/transactions/transfer", "HASH", 200, "{}", TimeSpan.FromHours(1));
        var expiredRecord = new IdempotencyRecord("KEY-EXPIRED", "/api/transactions/transfer", "HASH", 200, "{}", TimeSpan.FromMilliseconds(-100));

        // Act & Assert
        activeRecord.IsExpired().Should().BeFalse();
        expiredRecord.IsExpired().Should().BeTrue();
    }
}
