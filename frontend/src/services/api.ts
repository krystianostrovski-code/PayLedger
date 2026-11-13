import type { Account, LedgerTransaction, LedgerSummary, LedgerIntegrityReport } from '../types';

const API_BASE = 'http://localhost:5001/api';

// In-memory fallback simulation dataset for static hosting (Vercel)
let mockAccounts: Account[] = [
  { id: 'acc-1', accountNumber: 'PL-AST-PLN-10001', name: 'Main Treasury Vault (PLN)', type: 1, typeName: 'Asset', currency: 'PLN', balance: 1000000.0, allowOverdraft: false, isActive: true, rowVersion: 2, createdAt: '2026-11-01T08:00:00Z' },
  { id: 'acc-2', accountNumber: 'PL-AST-EUR-10002', name: 'Main Treasury Vault (EUR)', type: 1, typeName: 'Asset', currency: 'EUR', balance: 250000.0, allowOverdraft: false, isActive: true, rowVersion: 2, createdAt: '2026-11-01T08:00:00Z' },
  { id: 'acc-3', accountNumber: 'PL-AST-PLN-20101', name: 'TechCorp Polska Sp. z o.o.', type: 1, typeName: 'Asset', currency: 'PLN', balance: 131660.0, allowOverdraft: false, isActive: true, rowVersion: 5, createdAt: '2026-11-05T10:00:00Z' },
  { id: 'acc-4', accountNumber: 'PL-AST-EUR-20102', name: 'Apex Logistics GmbH', type: 1, typeName: 'Asset', currency: 'EUR', balance: 40000.0, allowOverdraft: false, isActive: true, rowVersion: 3, createdAt: '2026-11-06T11:00:00Z' },
  { id: 'acc-5', accountNumber: 'PL-AST-USD-20103', name: 'CloudScale SaaS Inc.', type: 1, typeName: 'Asset', currency: 'USD', balance: 75000.0, allowOverdraft: false, isActive: true, rowVersion: 2, createdAt: '2026-11-07T09:30:00Z' },
  { id: 'acc-6', accountNumber: 'PL-AST-PLN-20104', name: 'Baltic Freight Sp. k.', type: 1, typeName: 'Asset', currency: 'PLN', balance: 42900.0, allowOverdraft: false, isActive: true, rowVersion: 3, createdAt: '2026-11-08T14:15:00Z' },
  { id: 'acc-7', accountNumber: 'PL-AST-PLN-20105', name: 'Silesia Software House', type: 1, typeName: 'Asset', currency: 'PLN', balance: 18340.0, allowOverdraft: false, isActive: true, rowVersion: 2, createdAt: '2026-11-10T16:00:00Z' },
  { id: 'acc-8', accountNumber: 'SETTLEMENT-INBOUND-PLN', name: 'Inbound Settlement Clearing (PLN)', type: 2, typeName: 'Liability', currency: 'PLN', balance: 1171475.0, allowOverdraft: true, isActive: true, rowVersion: 4, createdAt: '2026-11-01T08:00:00Z' },
  { id: 'acc-9', accountNumber: 'SETTLEMENT-INBOUND-EUR', name: 'Inbound Settlement Clearing (EUR)', type: 2, typeName: 'Liability', currency: 'EUR', balance: 295000.0, allowOverdraft: true, isActive: true, rowVersion: 3, createdAt: '2026-11-01T08:00:00Z' },
  { id: 'acc-10', accountNumber: 'REV-FEE-GATEWAY-PLN', name: 'Gateway Fee Revenue (PLN)', type: 3, typeName: 'Revenue', currency: 'PLN', balance: 0.0, allowOverdraft: true, isActive: true, rowVersion: 3, createdAt: '2026-11-01T08:00:00Z' },
];

let mockTransactions: LedgerTransaction[] = [
  {
    id: 'tx-rev-1',
    referenceNumber: 'REV-TX-20261112-ERR001',
    type: 6,
    typeName: 'StornoReversal',
    status: 1,
    statusName: 'Posted',
    description: '[STORNO / REVERSAL] TX-20261112-ERR001: Anulowanie zdublowanej opłaty serwerowej',
    idempotencyKey: null,
    originalTransactionId: 'tx-err-1',
    reversedByTransactionId: null,
    createdAt: new Date(Date.now() - 3600000).toISOString(),
    totalAmount: 2500.0,
    currency: 'PLN',
    entries: [
      { id: 'e-1', transactionId: 'tx-rev-1', accountId: 'acc-3', accountNumber: 'PL-AST-PLN-20101', accountName: 'TechCorp Polska Sp. z o.o.', direction: 1, directionName: 'Debit', amount: 2500.0, currency: 'PLN', description: 'Reversal refund of server fee', createdAt: new Date(Date.now() - 3600000).toISOString() },
      { id: 'e-2', transactionId: 'tx-rev-1', accountId: 'acc-10', accountNumber: 'REV-FEE-GATEWAY-PLN', accountName: 'Gateway Fee Revenue (PLN)', direction: 2, directionName: 'Credit', amount: 2500.0, currency: 'PLN', description: 'Reversal cancellation of revenue', createdAt: new Date(Date.now() - 3600000).toISOString() }
    ]
  },
  {
    id: 'tx-b2b-1',
    referenceNumber: 'TX-20261110-B2B001',
    type: 3,
    typeName: 'Transfer',
    status: 1,
    statusName: 'Posted',
    description: 'B2B Settlement: TechCorp -> Silesia Software #FV-2026/10/89',
    idempotencyKey: 'IDEMP-TX-TECH-SIL-01',
    originalTransactionId: null,
    reversedByTransactionId: null,
    createdAt: new Date(Date.now() - 86400000).toISOString(),
    totalAmount: 18340.0,
    currency: 'PLN',
    entries: [
      { id: 'e-3', transactionId: 'tx-b2b-1', accountId: 'acc-3', accountNumber: 'PL-AST-PLN-20101', accountName: 'TechCorp Polska Sp. z o.o.', direction: 2, directionName: 'Credit', amount: 18340.0, currency: 'PLN', description: 'Payment for IT Consultancy Invoice', createdAt: new Date(Date.now() - 86400000).toISOString() },
      { id: 'e-4', transactionId: 'tx-b2b-1', accountId: 'acc-7', accountNumber: 'PL-AST-PLN-20105', accountName: 'Silesia Software House', direction: 1, directionName: 'Debit', amount: 18340.0, currency: 'PLN', description: 'Settlement received for Invoice', createdAt: new Date(Date.now() - 86400000).toISOString() }
    ]
  }
];

export interface ExecutionResponse<T> {
  data: T;
  cacheLookup?: 'HIT' | 'MISS';
  isIdempotentReplay?: boolean;
}

export const ledgerApi = {
  async getAccounts(params?: { type?: number; currency?: string; search?: string }): Promise<Account[]> {
    try {
      const q = new URLSearchParams();
      if (params?.type !== undefined) q.append('type', params.type.toString());
      if (params?.currency) q.append('currency', params.currency);
      if (params?.search) q.append('search', params.search);

      const res = await fetch(`${API_BASE}/accounts?${q.toString()}`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      return await res.json();
    } catch {
      let filtered = [...mockAccounts];
      if (params?.type !== undefined) filtered = filtered.filter(a => a.type === params.type);
      if (params?.currency) filtered = filtered.filter(a => a.currency === params.currency);
      if (params?.search) {
        const s = params.search.toLowerCase();
        filtered = filtered.filter(a => a.accountNumber.toLowerCase().includes(s) || a.name.toLowerCase().includes(s));
      }
      return filtered;
    }
  },

  async getAccountDetails(id: string): Promise<{ account: Account; recentEntries: any[] }> {
    try {
      const res = await fetch(`${API_BASE}/accounts/${id}`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      return await res.json();
    } catch {
      const acc = mockAccounts.find(a => a.id === id) || mockAccounts[0];
      const entries = mockTransactions.flatMap(t => t.entries.filter(e => e.accountId === id));
      return { account: acc, recentEntries: entries };
    }
  },

  async getTransactions(params?: { limit?: number; accountId?: string }): Promise<LedgerTransaction[]> {
    try {
      const q = new URLSearchParams();
      if (params?.limit) q.append('limit', params.limit.toString());
      if (params?.accountId) q.append('accountId', params.accountId);

      const res = await fetch(`${API_BASE}/transactions?${q.toString()}`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      return await res.json();
    } catch {
      return [...mockTransactions];
    }
  },

  async getSummary(): Promise<LedgerSummary> {
    try {
      const res = await fetch(`${API_BASE}/ledger/summary`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      return await res.json();
    } catch {
      return {
        totalLiquidityPln: 2732700.0,
        activeAccountsCount: mockAccounts.length,
        totalTransactionsCount: mockTransactions.length,
        dailyVolumePln: 2798890.0,
        integrityViolationsCount: 0,
        holdings: [
          { currency: 'EUR', balance: 290000.0, plnEquivalent: 1242650.0, exchangeRateToPln: 4.285 },
          { currency: 'PLN', balance: 1192900.0, plnEquivalent: 1192900.0, exchangeRateToPln: 1.0 },
          { currency: 'USD', balance: 75000.0, plnEquivalent: 297150.0, exchangeRateToPln: 3.962 }
        ]
      };
    }
  },

  async getIntegrity(): Promise<LedgerIntegrityReport> {
    try {
      const res = await fetch(`${API_BASE}/ledger/integrity`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      return await res.json();
    } catch {
      return {
        isHealthy: true,
        checkedAt: new Date().toISOString(),
        totalTransactionsAudited: mockTransactions.length,
        totalAccountsAudited: mockAccounts.length,
        totalDebitSumPln: 2798890.0,
        totalCreditSumPln: 2798890.0,
        discrepancies: []
      };
    }
  },

  async transfer(data: {
    sourceAccountId: string;
    destinationAccountId: string;
    amount: number;
    description: string;
    idempotencyKey?: string;
  }): Promise<ExecutionResponse<LedgerTransaction>> {
    try {
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      if (data.idempotencyKey) {
        headers['Idempotency-Key'] = data.idempotencyKey;
      }

      const res = await fetch(`${API_BASE}/transactions/transfer`, {
        method: 'POST',
        headers,
        body: JSON.stringify(data)
      });

      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || 'Transfer failed');
      }

      const cacheLookup = (res.headers.get('X-Cache-Lookup') as any) || 'MISS';
      const isIdempotentReplay = res.headers.get('X-Idempotent-Replay') === 'true';
      const tx = await res.json();

      return { data: tx, cacheLookup, isIdempotentReplay };
    } catch (e: any) {
      if (e.message && e.message.includes('Double-entry') || e.message.includes('insufficient')) {
        throw e;
      }
      // Simulated transfer in mock
      const src = mockAccounts.find(a => a.id === data.sourceAccountId);
      const dst = mockAccounts.find(a => a.id === data.destinationAccountId);
      if (!src || !dst) throw new Error('Account not found');
      if (src.balance < data.amount) throw new Error(`Insufficient funds on ${src.accountNumber}`);

      src.balance -= data.amount;
      dst.balance += data.amount;

      const newTx: LedgerTransaction = {
        id: `tx-${Date.now()}`,
        referenceNumber: `TX-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}-${Math.random().toString(16).slice(2, 8).toUpperCase()}`,
        type: 3,
        typeName: 'Transfer',
        status: 1,
        statusName: 'Posted',
        description: data.description,
        idempotencyKey: data.idempotencyKey,
        createdAt: new Date().toISOString(),
        totalAmount: data.amount,
        currency: src.currency,
        entries: [
          { id: `e-${Date.now()}-1`, transactionId: `tx-${Date.now()}`, accountId: src.id, accountNumber: src.accountNumber, accountName: src.name, direction: 2, directionName: 'Credit', amount: data.amount, currency: src.currency, description: data.description, createdAt: new Date().toISOString() },
          { id: `e-${Date.now()}-2`, transactionId: `tx-${Date.now()}`, accountId: dst.id, accountNumber: dst.accountNumber, accountName: dst.name, direction: 1, directionName: 'Debit', amount: data.amount, currency: dst.currency, description: data.description, createdAt: new Date().toISOString() }
        ]
      };
      mockTransactions.unshift(newTx);
      return { data: newTx, cacheLookup: 'MISS', isIdempotentReplay: false };
    }
  },

  async reverse(transactionId: string, reason: string): Promise<LedgerTransaction> {
    try {
      const res = await fetch(`${API_BASE}/transactions/${transactionId}/reverse`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason })
      });
      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || 'Reversal failed');
      }
      return await res.json();
    } catch {
      const orig = mockTransactions.find(t => t.id === transactionId);
      if (!orig) throw new Error('Transaction not found');
      orig.status = 2;
      orig.statusName = 'Reversed';

      const storno: LedgerTransaction = {
        id: `rev-${orig.id}`,
        referenceNumber: `REV-${orig.referenceNumber}`,
        type: 6,
        typeName: 'StornoReversal',
        status: 1,
        statusName: 'Posted',
        description: `[STORNO] ${orig.referenceNumber}: ${reason}`,
        originalTransactionId: orig.id,
        createdAt: new Date().toISOString(),
        totalAmount: orig.totalAmount,
        currency: orig.currency,
        entries: orig.entries.map(e => ({
          ...e,
          id: `rev-${e.id}`,
          direction: e.direction === 1 ? 2 : 1,
          directionName: e.direction === 1 ? 'Credit' : 'Debit'
        }))
      };
      mockTransactions.unshift(storno);
      return storno;
    }
  }
};
