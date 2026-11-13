import React, { useState, useEffect } from 'react';
import type { Account, LedgerTransaction, LedgerSummary, LedgerIntegrityReport, LedgerEntry } from './types';
import { ledgerApi } from './services/api';
import { Navbar } from './components/Navbar';
import { DashboardView } from './components/DashboardView';
import { AccountsView } from './components/AccountsView';
import { JournalView } from './components/JournalView';
import { TransferWizard } from './components/TransferWizard';
import { TransactionDetailModal } from './components/TransactionDetailModal';

export const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'terminal' | 'accounts' | 'journal' | 'transfer'>('terminal');
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [transactions, setTransactions] = useState<LedgerTransaction[]>([]);
  const [summary, setSummary] = useState<LedgerSummary | null>(null);
  const [integrity, setIntegrity] = useState<LedgerIntegrityReport | null>(null);

  const [selectedAccount, setSelectedAccount] = useState<Account | null>(null);
  const [accountEntries, setAccountEntries] = useState<LedgerEntry[]>([]);
  const [selectedTransaction, setSelectedTransaction] = useState<LedgerTransaction | null>(null);
  const [preselectedSourceAccount, setPreselectedSourceAccount] = useState<Account | null>(null);

  const loadData = async () => {
    try {
      const [accs, txs, sum, integ] = await Promise.all([
        ledgerApi.getAccounts(),
        ledgerApi.getTransactions({ limit: 50 }),
        ledgerApi.getSummary(),
        ledgerApi.getIntegrity()
      ]);
      setAccounts(accs);
      setTransactions(txs);
      setSummary(sum);
      setIntegrity(integ);
    } catch (e) {
      console.error('Failed to load initial data', e);
    }
  };

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 10000);
    return () => clearInterval(interval);
  }, []);

  const handleSelectAccount = async (account: Account) => {
    setSelectedAccount(account);
    const details = await ledgerApi.getAccountDetails(account.id);
    setAccountEntries(details.recentEntries);
  };

  const handleReverseTransaction = async (txId: string, reason: string) => {
    await ledgerApi.reverse(txId, reason);
    await loadData();
  };

  const handleExecuteTransfer = async (data: {
    sourceAccountId: string;
    destinationAccountId: string;
    amount: number;
    description: string;
    idempotencyKey?: string;
  }) => {
    const res = await ledgerApi.transfer(data);
    await loadData();
    return res;
  };

  return (
    <div className="min-h-screen bg-[#0b0c0e] text-[#e4e4e7] flex flex-col font-sans selection:bg-emerald-500/20 selection:text-emerald-400">
      {/* Navbar */}
      <Navbar
        activeTab={activeTab}
        onTabChange={tab => {
          setActiveTab(tab);
          if (tab !== 'accounts') setSelectedAccount(null);
        }}
        integrity={integrity}
        onRefresh={loadData}
      />

      {/* Main Content Area */}
      <main className="flex-1 flex overflow-hidden relative">
        {activeTab === 'terminal' && (
          <DashboardView
            summary={summary}
            integrity={integrity}
            recentTransactions={transactions}
            onOpenTransaction={tx => setSelectedTransaction(tx)}
            onNavigateToTransfer={() => setActiveTab('transfer')}
          />
        )}

        {activeTab === 'accounts' && (
          <AccountsView
            accounts={accounts}
            onSelectAccount={handleSelectAccount}
            selectedAccount={selectedAccount}
            accountEntries={accountEntries}
            onCloseAccountDrawer={() => setSelectedAccount(null)}
            onInitiateTransferFrom={acc => {
              setPreselectedSourceAccount(acc);
              setActiveTab('transfer');
            }}
          />
        )}

        {activeTab === 'journal' && (
          <JournalView
            transactions={transactions}
            onSelectTransaction={tx => setSelectedTransaction(tx)}
            onReverseTransaction={handleReverseTransaction}
          />
        )}

        {activeTab === 'transfer' && (
          <TransferWizard
            accounts={accounts}
            preselectedSourceAccount={preselectedSourceAccount}
            onExecuteTransfer={handleExecuteTransfer}
          />
        )}
      </main>

      {/* Transaction Details Modal */}
      <TransactionDetailModal
        transaction={selectedTransaction}
        onClose={() => setSelectedTransaction(null)}
      />
    </div>
  );
};

export default App;
