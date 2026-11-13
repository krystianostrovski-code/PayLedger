export type AccountType = 'Asset' | 'Liability' | 'Revenue' | 'Expense' | 'Equity';

export interface Account {
  id: string;
  accountNumber: string;
  name: string;
  type: number;
  typeName: AccountType;
  currency: string;
  balance: number;
  allowOverdraft: boolean;
  isActive: boolean;
  rowVersion: number;
  createdAt: string;
}

export interface LedgerEntry {
  id: string;
  transactionId: string;
  accountId: string;
  accountNumber: string;
  accountName: string;
  direction: number; // 1 = Debit, 2 = Credit
  directionName: 'Debit' | 'Credit';
  amount: number;
  currency: string;
  description: string;
  createdAt: string;
}

export interface LedgerTransaction {
  id: string;
  referenceNumber: string;
  type: number;
  typeName: string;
  status: number;
  statusName: 'Posted' | 'Reversed';
  description: string;
  idempotencyKey?: string | null;
  originalTransactionId?: string | null;
  reversedByTransactionId?: string | null;
  createdAt: string;
  totalAmount: number;
  currency: string;
  entries: LedgerEntry[];
}

export interface CurrencyHolding {
  currency: string;
  balance: number;
  plnEquivalent: number;
  exchangeRateToPln: number;
}

export interface LedgerSummary {
  totalLiquidityPln: number;
  activeAccountsCount: number;
  totalTransactionsCount: number;
  dailyVolumePln: number;
  integrityViolationsCount: number;
  holdings: CurrencyHolding[];
}

export interface LedgerIntegrityReport {
  isHealthy: boolean;
  checkedAt: string;
  totalTransactionsAudited: number;
  totalAccountsAudited: number;
  totalDebitSumPln: number;
  totalCreditSumPln: number;
  discrepancies: string[];
}

