using Microsoft.EntityFrameworkCore;
using PayLedger.Domain.Entities;
using PayLedger.Domain.Enums;

namespace PayLedger.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(PayLedgerDbContext context)
    {
        if (await context.Accounts.AnyAsync())
            return; // Already seeded

        // 1. Technical Settlement & FX Clearing Accounts
        var inboundPln = new Account("SETTLEMENT-INBOUND-PLN", "Inbound Settlement Clearing (PLN)", AccountType.Liability, "PLN", allowOverdraft: true);
        var inboundEur = new Account("SETTLEMENT-INBOUND-EUR", "Inbound Settlement Clearing (EUR)", AccountType.Liability, "EUR", allowOverdraft: true);
        var inboundUsd = new Account("SETTLEMENT-INBOUND-USD", "Inbound Settlement Clearing (USD)", AccountType.Liability, "USD", allowOverdraft: true);

        var fxPln = new Account("FX-CLEARING-PLN", "FX Liquidity Pool (PLN)", AccountType.Asset, "PLN", allowOverdraft: true);
        var fxEur = new Account("FX-CLEARING-EUR", "FX Liquidity Pool (EUR)", AccountType.Asset, "EUR", allowOverdraft: true);
        var fxUsd = new Account("FX-CLEARING-USD", "FX Liquidity Pool (USD)", AccountType.Asset, "USD", allowOverdraft: true);

        var feeRevenuePln = new Account("REV-FEE-GATEWAY-PLN", "Gateway Fee Revenue (PLN)", AccountType.Revenue, "PLN", allowOverdraft: true);

        // 2. Main Treasury Accounts
        var treasuryPln = new Account("PL-AST-PLN-10001", "Main Treasury Vault (PLN)", AccountType.Asset, "PLN");
        var treasuryEur = new Account("PL-AST-EUR-10002", "Main Treasury Vault (EUR)", AccountType.Asset, "EUR");
        var treasuryUsd = new Account("PL-AST-USD-10003", "Main Treasury Vault (USD)", AccountType.Asset, "USD");

        // 3. Client B2B Wallets
        var techCorp = new Account("PL-AST-PLN-20101", "TechCorp Polska Sp. z o.o.", AccountType.Asset, "PLN");
        var apexLogistics = new Account("PL-AST-EUR-20102", "Apex Logistics GmbH", AccountType.Asset, "EUR");
        var cloudScale = new Account("PL-AST-USD-20103", "CloudScale SaaS Inc.", AccountType.Asset, "USD");
        var balticFreight = new Account("PL-AST-PLN-20104", "Baltic Freight Sp. k.", AccountType.Asset, "PLN");
        var silesiaSoft = new Account("PL-AST-PLN-20105", "Silesia Software House", AccountType.Asset, "PLN");
        var escrowHoldings = new Account("PL-LIA-PLN-30101", "B2B Merchant Escrow Holdings", AccountType.Liability, "PLN", allowOverdraft: true);

        var allAccounts = new List<Account>
        {
            inboundPln, inboundEur, inboundUsd,
            fxPln, fxEur, fxUsd, feeRevenuePln,
            treasuryPln, treasuryEur, treasuryUsd,
            techCorp, apexLogistics, cloudScale,
            balticFreight, silesiaSoft, escrowHoldings
        };

        context.Accounts.AddRange(allAccounts);
        await context.SaveChangesAsync();

        var transactions = new List<LedgerTransaction>();

        // Transaction 1: Treasury Funding PLN (1,000,000.00 PLN)
        var tx1Entries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (treasuryPln.Id, EntryDirection.Debit, 1000000.00m, "PLN", "Initial Treasury Capitalization Deposit"),
            (inboundPln.Id, EntryDirection.Credit, 1000000.00m, "PLN", "Treasury Capital Funding Offset")
        };
        var tx1 = LedgerTransaction.Create(TransactionType.Deposit, "Treasury Capitalization (PLN)", tx1Entries, "IDEMP-INIT-TREASURY-PLN", "TX-20261101-TRS001");
        treasuryPln.ApplyEntry(EntryDirection.Debit, 1000000.00m);
        inboundPln.ApplyEntry(EntryDirection.Credit, 1000000.00m);
        transactions.Add(tx1);

        // Transaction 2: Treasury Funding EUR (250,000.00 EUR)
        var tx2Entries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (treasuryEur.Id, EntryDirection.Debit, 250000.00m, "EUR", "Initial Treasury Capitalization Deposit (EUR)"),
            (inboundEur.Id, EntryDirection.Credit, 250000.00m, "EUR", "Treasury EUR Inbound Offset")
        };
        var tx2 = LedgerTransaction.Create(TransactionType.Deposit, "Treasury Capitalization (EUR)", tx2Entries, "IDEMP-INIT-TREASURY-EUR", "TX-20261101-TRS002");
        treasuryEur.ApplyEntry(EntryDirection.Debit, 250000.00m);
        inboundEur.ApplyEntry(EntryDirection.Credit, 250000.00m);
        transactions.Add(tx2);

        // Transaction 3: Client Funding - TechCorp Deposit (150,000.00 PLN)
        var tx3Entries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (techCorp.Id, EntryDirection.Debit, 150000.00m, "PLN", "Wire Deposit mBank S.A. ref: TECHCORP-FUNDING-2026"),
            (inboundPln.Id, EntryDirection.Credit, 150000.00m, "PLN", "Inbound settlement clearing TechCorp")
        };
        var tx3 = LedgerTransaction.Create(TransactionType.Deposit, "Deposit: TechCorp Polska Sp. z o.o.", tx3Entries, "IDEMP-DEP-TECHCORP-01", "TX-20261105-DEP001");
        techCorp.ApplyEntry(EntryDirection.Debit, 150000.00m);
        inboundPln.ApplyEntry(EntryDirection.Credit, 150000.00m);
        transactions.Add(tx3);

        // Transaction 4: Client Funding - Apex Logistics EUR (45,000.00 EUR)
        var tx4Entries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (apexLogistics.Id, EntryDirection.Debit, 45000.00m, "EUR", "SEPA Credit Transfer from Deutsche Bank AG"),
            (inboundEur.Id, EntryDirection.Credit, 45000.00m, "EUR", "Inbound settlement clearing Apex Logistics")
        };
        var tx4 = LedgerTransaction.Create(TransactionType.Deposit, "Deposit: Apex Logistics GmbH", tx4Entries, "IDEMP-DEP-APEX-01", "TX-20261106-DEP002");
        apexLogistics.ApplyEntry(EntryDirection.Debit, 45000.00m);
        inboundEur.ApplyEntry(EntryDirection.Credit, 45000.00m);
        transactions.Add(tx4);

        // Transaction 5: Client Funding - CloudScale SaaS USD (75,000.00 USD)
        var tx5Entries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (cloudScale.Id, EntryDirection.Debit, 75000.00m, "USD", "SWIFT Transfer from Chase Bank USA"),
            (inboundUsd.Id, EntryDirection.Credit, 75000.00m, "USD", "Inbound settlement clearing CloudScale")
        };
        var tx5 = LedgerTransaction.Create(TransactionType.Deposit, "Deposit: CloudScale SaaS Inc.", tx5Entries, "IDEMP-DEP-CLOUD-01", "TX-20261107-DEP003");
        cloudScale.ApplyEntry(EntryDirection.Debit, 75000.00m);
        inboundUsd.ApplyEntry(EntryDirection.Credit, 75000.00m);
        transactions.Add(tx5);

        // Transaction 6: B2B Settlement - TechCorp pays Silesia Software House (18,340.00 PLN)
        var tx6Entries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (techCorp.Id, EntryDirection.Credit, 18340.00m, "PLN", "Payment for IT Consultancy Invoice #FV-2026/10/89"),
            (silesiaSoft.Id, EntryDirection.Debit, 18340.00m, "PLN", "Settlement received for Invoice #FV-2026/10/89")
        };
        var tx6 = LedgerTransaction.Create(TransactionType.Transfer, "B2B Settlement: TechCorp -> Silesia Software", tx6Entries, "IDEMP-TX-TECH-SIL-01", "TX-20261110-B2B001");
        techCorp.ApplyEntry(EntryDirection.Credit, 18340.00m);
        silesiaSoft.ApplyEntry(EntryDirection.Debit, 18340.00m);
        transactions.Add(tx6);

        // Transaction 7: FX Conversion - Apex Logistics exchanges 5,000 EUR to PLN (@ 4.2850 = 21,425.00 PLN) into Baltic Freight
        var tx7Entries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (apexLogistics.Id, EntryDirection.Credit, 5000.00m, "EUR", "FX Sell 5,000.00 EUR @ 4.2850"),
            (fxEur.Id, EntryDirection.Debit, 5000.00m, "EUR", "FX Liquidity Pool EUR Absorption"),
            (fxPln.Id, EntryDirection.Credit, 21425.00m, "PLN", "FX Liquidity Pool PLN Release"),
            (balticFreight.Id, EntryDirection.Debit, 21425.00m, "PLN", "FX Payout 21,425.00 PLN @ 4.2850")
        };
        var tx7 = LedgerTransaction.Create(TransactionType.CurrencyExchange, "Cross-Border Settlement: 5,000 EUR -> 21,425 PLN", tx7Entries, "IDEMP-FX-APEX-BALTIC-01", "TX-20261111-FX001");
        apexLogistics.ApplyEntry(EntryDirection.Credit, 5000.00m);
        fxEur.ApplyEntry(EntryDirection.Debit, 5000.00m);
        fxPln.ApplyEntry(EntryDirection.Credit, 21425.00m);
        balticFreight.ApplyEntry(EntryDirection.Debit, 21425.00m);
        transactions.Add(tx7);

        // Transaction 8: Baltic Freight additional deposit (21,475.00 PLN -> brings total to 42,900.00 PLN)
        var tx8Entries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (balticFreight.Id, EntryDirection.Debit, 21475.00m, "PLN", "Wire Deposit Santander Bank Polska"),
            (inboundPln.Id, EntryDirection.Credit, 21475.00m, "PLN", "Inbound settlement Baltic Freight")
        };
        var tx8 = LedgerTransaction.Create(TransactionType.Deposit, "Deposit: Baltic Freight Sp. k.", tx8Entries, "IDEMP-DEP-BALTIC-01", "TX-20261112-DEP004");
        balticFreight.ApplyEntry(EntryDirection.Debit, 21475.00m);
        inboundPln.ApplyEntry(EntryDirection.Credit, 21475.00m);
        transactions.Add(tx8);

        // Transaction 9: Erroneous duplicate charge (2,500.00 PLN) that will be reversed with Storno
        var tx9Entries = new List<(Guid, EntryDirection, decimal, string, string)>
        {
            (techCorp.Id, EntryDirection.Credit, 2500.00m, "PLN", "Billing charge: Duplicate Monthly Server Fee"),
            (feeRevenuePln.Id, EntryDirection.Debit, 2500.00m, "PLN", "Temporary Fee Collection")
        };
        var tx9 = LedgerTransaction.Create(TransactionType.FeeCharge, "Erroneous Fee Billing (TechCorp)", tx9Entries, null, "TX-20261112-ERR001");
        techCorp.ApplyEntry(EntryDirection.Credit, 2500.00m);
        feeRevenuePln.ApplyEntry(EntryDirection.Debit, 2500.00m);
        transactions.Add(tx9);

        // Transaction 10: STORNO Reversal of Transaction 9!
        var stornoTx = tx9.CreateStorno("Korekta błędu rozliczeniowego: Anulowanie zdublowanej opłaty serwerowej");
        techCorp.ApplyEntry(EntryDirection.Debit, 2500.00m);
        feeRevenuePln.ApplyEntry(EntryDirection.Credit, 2500.00m);
        transactions.Add(stornoTx);

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();
    }
}
