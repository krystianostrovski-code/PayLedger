import React, { useState } from 'react';
import type { Account, LedgerEntry } from '../types';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faBuildingColumns,
  faWallet,
  faFilter,
  faReceipt,
  faXmark,
  faArrowRight
} from './icons';

interface AccountsViewProps {
  accounts: Account[];
  onSelectAccount: (account: Account) => void;
  selectedAccount: Account | null;
  accountEntries: LedgerEntry[];
  onCloseAccountDrawer: () => void;
  onInitiateTransferFrom: (account: Account) => void;
}

export const AccountsView: React.FC<AccountsViewProps> = ({
  accounts,
  onSelectAccount,
  selectedAccount,
  accountEntries,
  onCloseAccountDrawer,
  onInitiateTransferFrom
}) => {
  const [typeFilter, setTypeFilter] = useState<string>('ALL');
  const [currencyFilter, setCurrencyFilter] = useState<string>('ALL');
  const [search, setSearch] = useState<string>('');

  const filtered = accounts.filter(a => {
    if (typeFilter !== 'ALL' && a.typeName !== typeFilter) return false;
    if (currencyFilter !== 'ALL' && a.currency !== currencyFilter) return false;
    if (search.trim()) {
      const s = search.toLowerCase();
      if (!a.accountNumber.toLowerCase().includes(s) && !a.name.toLowerCase().includes(s)) return false;
    }
    return true;
  });

  return (
    <div className="flex-1 flex overflow-hidden font-mono text-zinc-200">
      {/* Main List */}
      <div className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
        {/* Filters Header */}
        <div className="bg-[#121316] border border-[#262830] p-3.5 rounded-sm flex flex-wrap items-center justify-between gap-3">
          {/* Search */}
          <div className="flex items-center space-x-2 flex-1 min-w-[200px]">
            <input
              type="text"
              placeholder="Szukaj po nazwie firmy lub numerze rachunku..."
              value={search}
              onChange={e => setSearch(e.target.value)}
              className="w-full bg-[#181920] border border-[#262830] px-3 py-1.5 rounded-sm text-xs text-white placeholder-zinc-500 font-sans"
            />
          </div>

          {/* Filter Pills */}
          <div className="flex items-center space-x-2">
            <select
              value={typeFilter}
              onChange={e => setTypeFilter(e.target.value)}
              className="bg-[#181920] border border-[#262830] px-2.5 py-1.5 rounded-sm text-xs text-zinc-300"
            >
              <option value="ALL">Wszystkie typy</option>
              <option value="Asset">Aktywa (Portfele B2B)</option>
              <option value="Liability">Pasywa / Rozliczenia</option>
              <option value="Revenue">Przychody (Prowizje)</option>
            </select>

            <select
              value={currencyFilter}
              onChange={e => setCurrencyFilter(e.target.value)}
              className="bg-[#181920] border border-[#262830] px-2.5 py-1.5 rounded-sm text-xs text-zinc-300"
            >
              <option value="ALL">Wszystkie waluty</option>
              <option value="PLN">PLN</option>
              <option value="EUR">EUR</option>
              <option value="USD">USD</option>
            </select>
          </div>
        </div>

        {/* Accounts Cards Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3.5">
          {filtered.map(acc => (
            <div
              key={acc.id}
              onClick={() => onSelectAccount(acc)}
              className="bg-[#121316] border border-[#262830] hover:border-emerald-500/50 p-4 rounded-sm space-y-3 cursor-pointer transition"
            >
              <div className="flex items-start justify-between">
                <div className="space-y-0.5">
                  <div className="flex items-center space-x-2">
                    <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-sm bg-[#1e2027] text-white border border-[#2e313b]">
                      {acc.currency}
                    </span>
                    <span className="text-[10px] text-zinc-400 font-sans uppercase">
                      {acc.typeName}
                    </span>
                  </div>
                  <h4 className="text-xs font-bold text-white tracking-wide mt-1">{acc.name}</h4>
                </div>
                <div className="text-[9px] text-zinc-500 bg-[#16181e] px-1.5 py-0.5 rounded-sm border border-[#262830]">
                  v{acc.rowVersion}
                </div>
              </div>

              <div className="text-[11px] text-zinc-400 select-all font-mono">
                {acc.accountNumber}
              </div>

              <div className="pt-2.5 border-t border-[#20222a] flex items-center justify-between">
                <div>
                  <div className="text-[9px] text-zinc-500">SALDO OPERACYJNE:</div>
                  <div className={`text-base font-bold ${
                    acc.balance >= 0 ? 'text-white' : 'text-rose-400'
                  }`}>
                    {acc.balance.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    <span className="text-xs font-normal text-zinc-400 ml-1">{acc.currency}</span>
                  </div>
                </div>

                <button
                  onClick={e => {
                    e.stopPropagation();
                    onInitiateTransferFrom(acc);
                  }}
                  className="px-2.5 py-1 rounded-sm bg-[#1a1c23] hover:bg-emerald-950/40 text-emerald-400 hover:text-emerald-300 border border-[#262830] hover:border-emerald-600/40 text-[10px] font-bold transition"
                >
                  PRZELEW &rarr;
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Account Details Drawer */}
      {selectedAccount && (
        <aside className="w-full sm:w-[440px] bg-[#14151a] border-l border-[#262830] flex flex-col shadow-2xl z-20">
          {/* Header */}
          <div className="p-4 bg-[#181920] border-b border-[#262830] flex items-center justify-between">
            <div>
              <div className="text-[10px] text-zinc-400 font-sans uppercase">{selectedAccount.typeName} ACCOUNT</div>
              <h3 className="text-xs font-bold text-white">{selectedAccount.name}</h3>
              <div className="text-[10px] text-emerald-400 font-mono select-all mt-0.5">{selectedAccount.accountNumber}</div>
            </div>
            <button
              onClick={onCloseAccountDrawer}
              className="p-1.5 text-zinc-400 hover:text-white hover:bg-[#22232a] rounded-sm transition"
            >
              <FontAwesomeIcon icon={faXmark} />
            </button>
          </div>

          {/* Balance overview */}
          <div className="p-4 bg-[#0e0f12] border-b border-[#262830] flex justify-between items-center">
            <div>
              <span className="text-[10px] text-zinc-500">BIEŻĄCE SALDO RACHUNKU</span>
              <div className="text-xl font-bold text-white">
                {selectedAccount.balance.toLocaleString('pl-PL', { minimumFractionDigits: 2 })} {selectedAccount.currency}
              </div>
            </div>
            <button
              onClick={() => onInitiateTransferFrom(selectedAccount)}
              className="px-3 py-1.5 rounded-sm bg-emerald-600 hover:bg-emerald-500 text-zinc-950 font-bold text-xs border border-emerald-400/50 transition cursor-pointer"
            >
              WYKONAJ PRZELEW
            </button>
          </div>

          {/* Ledger Journal for this account */}
          <div className="flex-1 overflow-y-auto p-4 space-y-2">
            <div className="text-[10px] font-bold text-zinc-400 uppercase tracking-wider mb-2 flex justify-between">
              <span>HISTORIA KSIĘGOWA ({accountEntries.length})</span>
              <span>KIERUNEK</span>
            </div>

            {accountEntries.length === 0 ? (
              <div className="text-center py-12 text-zinc-500 text-xs">
                Brak zarejestrowanych wpisów księgowych dla tego rachunku.
              </div>
            ) : (
              accountEntries.map(e => (
                <div
                  key={e.id}
                  className="p-2.5 rounded-sm bg-[#16181d] border border-[#262830] text-[11px] space-y-1"
                >
                  <div className="flex items-center justify-between">
                    <span className="text-zinc-400 text-[10px]">
                      {new Date(e.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </span>
                    <span className={`text-[10px] font-bold px-1.5 py-0.5 rounded-xs ${
                      e.directionName === 'Debit'
                        ? 'bg-emerald-950/50 text-emerald-400 border border-emerald-700/50'
                        : 'bg-amber-950/50 text-amber-400 border border-amber-700/50'
                    }`}>
                      {e.directionName === 'Debit' ? '+ DEBET' : '- KREDYT'}
                    </span>
                  </div>
                  <div className="text-zinc-200 font-sans line-clamp-1">{e.description}</div>
                  <div className="text-right font-bold text-white text-xs">
                    {e.amount.toLocaleString('pl-PL', { minimumFractionDigits: 2 })} {e.currency}
                  </div>
                </div>
              ))
            )}
          </div>
        </aside>
      )}
    </div>
  );
};
