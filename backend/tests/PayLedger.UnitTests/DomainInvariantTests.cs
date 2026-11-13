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

public class DomainInvariantTests
{
    [Fact]
    public void CreateTransaction_ShouldThrow_WhenDebitsDoNotEqualCredits()
    {
        // Arrange
        var acc1 = Guid.NewGuid();
        var acc2 = Guid.NewGuid();

        var unbalancedEntries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (acc1, EntryDirection.Debit, 100.00m, "PLN", "Leg 1"),
            (acc2, EntryDirection.Credit, 95.00m, "PLN", "Leg 2") // 5 PLN missing
        };

        // Act
        Action act = () => LedgerTransaction.Create(
            TransactionType.Transfer,
            "Unbalanced transfer",
            unbalancedEntries
        );

        // Assert
        act.Should().Throw<UnbalancedLedgerException>()
            .WithMessage("*Double-entry invariant violated for PLN: Total Debits (100.0000) must equal Total Credits (95.0000)*");
    }

    [Fact]
    public void CreateTransaction_ShouldThrow_WhenFewerThanTwoEntries()
    {
        // Arrange
        var singleEntry = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (Guid.NewGuid(), EntryDirection.Debit, 100.00m, "PLN", "Single leg")
        };

        // Act
        Action act = () => LedgerTransaction.Create(
            TransactionType.Deposit,
            "Invalid single leg",
            singleEntry
        );

        // Assert
        act.Should().Throw<InvalidAccountOperationException>()
            .WithMessage("*Double-entry accounting requires at least two entries per transaction.*");
    }

    [Fact]
    public void Account_ShouldThrow_WhenAssetAccountHasInsufficientFunds()
    {
        // Arrange
        var account = new Account("PL-AST-PLN-01", "Client Account", AccountType.Asset, "PLN", allowOverdraft: false, initialBalance: 50.00m);

        // Act: Try to Credit (spend) 100 PLN when balance is only 50 PLN
        Action act = () => account.ApplyEntry(EntryDirection.Credit, 100.00m);

        // Assert
        act.Should().Throw<InsufficientFundsException>()
            .WithMessage("*insufficient funds*Available balance: 50.00 PLN, requested: 100.00 PLN*");
    }

    [Fact]
    public void Account_ShouldAllowNegativeBalance_WhenAllowOverdraftIsTrue()
    {
        // Arrange (e.g. Technical clearing account)
        var clearing = new Account("CLEARING-01", "Settlement Clearing", AccountType.Liability, "PLN", allowOverdraft: true, initialBalance: 0m);

        // Act
        clearing.ApplyEntry(EntryDirection.Debit, 500.00m);

        // Assert: for liability, debit decreases balance -> -500m
        clearing.Balance.Should().Be(-500.00m);
    }

    [Fact]
    public void Account_NormalBalance_AssetIncreasesWithDebit_DecreasesWithCredit()
    {
        // Arrange
        var asset = new Account("PL-AST-01", "Cash", AccountType.Asset, "PLN", initialBalance: 100.00m);

        // Act & Assert
        asset.ApplyEntry(EntryDirection.Debit, 50.00m);
        asset.Balance.Should().Be(150.00m);

        asset.ApplyEntry(EntryDirection.Credit, 30.00m);
        asset.Balance.Should().Be(120.00m);
    }

    [Fact]
    public void Account_NormalBalance_LiabilityIncreasesWithCredit_DecreasesWithDebit()
    {
        // Arrange
        var liability = new Account("PL-LIA-01", "Deposits Owed", AccountType.Liability, "PLN", allowOverdraft: true, initialBalance: 100.00m);

        // Act & Assert
        liability.ApplyEntry(EntryDirection.Credit, 50.00m);
        liability.Balance.Should().Be(150.00m);

        liability.ApplyEntry(EntryDirection.Debit, 30.00m);
        liability.Balance.Should().Be(120.00m);
    }

    [Fact]
    public void Storno_ShouldCreateExactOppositeReversalAndMarkOriginalReversed()
    {
        // Arrange
        var acc1 = Guid.NewGuid();
        var acc2 = Guid.NewGuid();

        var originalEntries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (acc1, EntryDirection.Debit, 250.00m, "EUR", "Original debit"),
            (acc2, EntryDirection.Credit, 250.00m, "EUR", "Original credit")
        };

        var originalTx = LedgerTransaction.Create(
            TransactionType.Transfer,
            "Original SEPA transfer",
            originalEntries
        );

        // Act
        var stornoTx = originalTx.CreateStorno("User duplicate request cancellation");

        // Assert
        originalTx.Status.Should().Be(TransactionStatus.Reversed);
        originalTx.ReversedByTransactionId.Should().Be(stornoTx.Id);

        stornoTx.Type.Should().Be(TransactionType.StornoReversal);
        stornoTx.OriginalTransactionId.Should().Be(originalTx.Id);
        stornoTx.ReferenceNumber.Should().StartWith("REV-");
        stornoTx.Entries.Should().HaveCount(2);

        var stornoLeg1 = stornoTx.Entries.First(e => e.AccountId == acc1);
        stornoLeg1.Direction.Should().Be(EntryDirection.Credit); // Flipped from Debit
        stornoLeg1.Amount.Should().Be(250.00m);

        var stornoLeg2 = stornoTx.Entries.First(e => e.AccountId == acc2);
        stornoLeg2.Direction.Should().Be(EntryDirection.Debit); // Flipped from Credit
        stornoLeg2.Amount.Should().Be(250.00m);
    }

    [Fact]
    public void Storno_ShouldThrow_WhenAttemptingToReverseAnAlreadyReversedTransaction()
    {
        // Arrange
        var acc1 = Guid.NewGuid();
        var acc2 = Guid.NewGuid();
        var entries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (acc1, EntryDirection.Debit, 100m, "PLN", "Leg A"),
            (acc2, EntryDirection.Credit, 100m, "PLN", "Leg B")
        };
        var tx = LedgerTransaction.Create(TransactionType.Transfer, "First transfer", entries);
        tx.CreateStorno("First reversal");

        // Act
        Action act = () => tx.CreateStorno("Second redundant reversal attempt");

        // Assert
        act.Should().Throw<InvalidAccountOperationException>()
            .WithMessage("*has already been reversed*");
    }
}
