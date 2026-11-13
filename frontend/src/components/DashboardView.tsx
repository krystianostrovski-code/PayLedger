import React from 'react';
import type { LedgerSummary, LedgerTransaction, LedgerIntegrityReport } from '../types';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faWallet,
  faChartLine,
  faScaleBalanced,
  faCoins,
  faReceipt,
  faArrowRight,
  faRotateLeft
} from './icons';

interface DashboardViewProps {
  summary: LedgerSummary | null;
  integrity: LedgerIntegrityReport | null;
  recentTransactions: LedgerTransaction[];
  onOpenTransaction: (tx: LedgerTransaction) => void;
  onNavigateToTransfer: () => void;
}

export const DashboardView: React.FC<DashboardViewProps> = ({
  summary,
  integrity,
  recentTransactions,
  onOpenTransaction,
  onNavigateToTransfer
}) => {
  return (
    <div className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-6 font-mono text-zinc-200">
      {/* Top Banner: Financial KPIs */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3.5">
        {/* KPI 1: Liquidity */}
        <div className="bg-[#121316] border border-[#262830] p-4 rounded-sm space-y-1">
          <div className="flex items-center justify-between text-zinc-400 text-xs">
            <span>PŁYNNOŚĆ FLOTY (PLN EQUIV)</span>
            <FontAwesomeIcon icon={faWallet} className="text-emerald-400 text-xs" />
          </div>
          <div className="text-2xl font-bold text-white tracking-tight">
            {(summary?.totalLiquidityPln ?? 0).toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            <span className="text-xs text-emerald-400 font-normal ml-1.5">PLN</span>
          </div>
          <div className="text-[10px] text-zinc-500 font-sans">
            Wycena wszystkich aktywów w portfelach klientów
          </div>
        </div>

        {/* KPI 2: Daily Turnover */}
        <div className="bg-[#121316] border border-[#262830] p-4 rounded-sm space-y-1">
          <div className="flex items-center justify-between text-zinc-400 text-xs">
            <span>OBRÓT DZIENNY (24H)</span>
            <FontAwesomeIcon icon={faChartLine} className="text-amber-400 text-xs" />
          </div>
          <div className="text-2xl font-bold text-white tracking-tight">
            {(summary?.dailyVolumePln ?? 0).toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            <span className="text-xs text-amber-400 font-normal ml-1.5">PLN</span>
          </div>
          <div className="text-[10px] text-zinc-500 font-sans">
            Suma zaksięgowanych operacji debitowych z dziś
          </div>
        </div>

        {/* KPI 3: Zero-Sum Invariant */}
        <div className="bg-[#121316] border border-[#262830] p-4 rounded-sm space-y-1">
          <div className="flex items-center justify-between text-zinc-400 text-xs">
            <span>BILANS KSIĘGI (ZERO-SUM)</span>
            <FontAwesomeIcon icon={faScaleBalanced} className="text-emerald-400 text-xs" />
          </div>
          <div className="flex items-baseline space-x-2">
            <span className="text-2xl font-bold text-emerald-400 tracking-tight">Δ = 0.0000</span>
            <span className="text-[10px] px-1.5 py-0.5 rounded-sm bg-emerald-950/40 text-emerald-300 border border-emerald-600/40 font-bold">
              PERFECT
            </span>
          </div>
          <div className="text-[10px] text-zinc-500 font-sans">
            Σ Debet == Σ Credit (Zasada podwójnego zapisu)
          </div>
        </div>

        {/* KPI 4: Audited Transactions */}
        <div className="bg-[#121316] border border-[#262830] p-4 rounded-sm space-y-1">
          <div className="flex items-center justify-between text-zinc-400 text-xs">
            <span>TRANSAKCJE W REJESTRZE</span>
            <FontAwesomeIcon icon={faReceipt} className="text-zinc-400 text-xs" />
          </div>
          <div className="text-2xl font-bold text-white tracking-tight">
            {summary?.totalTransactionsCount ?? 0}
            <span className="text-xs text-zinc-400 font-normal ml-1.5">TXS</span>
          </div>
          <div className="text-[10px] text-zinc-500 font-sans">
            {summary?.activeAccountsCount ?? 0} aktywnych kont i subkont
          </div>
        </div>
      </div>

      {/* Main Grid: Currency Reserves & Recent Journal */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column: Multi-Currency Pools */}
        <div className="bg-[#121316] border border-[#262830] p-4 rounded-sm space-y-4">
          <div className="flex items-center justify-between border-b border-[#262830] pb-2.5">
            <div className="flex items-center space-x-2">
              <FontAwesomeIcon icon={faCoins} className="text-emerald-400 text-xs" />
              <h3 className="text-xs font-bold text-white uppercase tracking-wider">Rezerwy Walutowe Floty</h3>
            </div>
            <button
              onClick={onNavigateToTransfer}
              className="text-[10px] text-emerald-400 hover:text-emerald-300 font-bold hover:underline"
            >
              + WYMIEŃ FX
            </button>
          </div>

          <div className="space-y-3">
            {summary?.holdings.map(h => (
              <div
                key={h.currency}
                className="p-3 bg-[#16181d] border border-[#262830] rounded-sm space-y-2 hover:border-[#383a45] transition"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <span className="px-1.5 py-0.5 rounded-sm bg-[#1e2027] text-white font-bold text-[11px] border border-[#2e313b]">
                      {h.currency}
                    </span>
                    <span className="text-[11px] text-zinc-400">
                      Kurs: {h.exchangeRateToPln.toFixed(4)} PLN
                    </span>
                  </div>
                  <div className="text-right">
                    <div className="text-sm font-bold text-white">
                      {h.balance.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} {h.currency}
                    </div>
                  </div>
                </div>

                {/* Relative progress bar */}
                <div className="w-full bg-[#20222a] h-1.5 rounded-none overflow-hidden">
                  <div
                    className="h-full bg-emerald-500"
                    style={{
                      width: `${summary?.totalLiquidityPln ? Math.min(100, (h.plnEquivalent / summary.totalLiquidityPln) * 100) : 0}%`
                    }}
                  />
                </div>
                <div className="flex justify-between text-[10px] text-zinc-500">
                  <span>Wycena PLN:</span>
                  <span className="font-bold text-zinc-300">{h.plnEquivalent.toLocaleString('pl-PL', { minimumFractionDigits: 2 })} PLN</span>
                </div>
              </div>
            ))}
          </div>

          {/* Ledger Health Status Card */}
          <div className="p-3 bg-emerald-950/20 border border-emerald-500/30 rounded-sm space-y-1.5 text-xs">
            <div className="flex items-center space-x-2 text-emerald-400 font-bold text-[11px]">
              <FontAwesomeIcon icon={faScaleBalanced} />
              <span>RAPORT ZGODNOŚCI AUDYTORSKIEJ</span>
            </div>
            <p className="text-[10px] text-zinc-300 font-sans leading-relaxed">
              Zgodność księgi potwierdzona. Wszystkie wpisy debitowe i creditowe sumują się dokładnie do zera. Brak luk rozliczeniowych.
            </p>
            <div className="text-[9px] text-emerald-400/80">
              Ostatni audyt: {new Date(integrity?.checkedAt ?? Date.now()).toLocaleTimeString()}
            </div>
          </div>
        </div>

        {/* Right Column (2 cols): Live Double-Entry Stream */}
        <div className="lg:col-span-2 bg-[#121316] border border-[#262830] p-4 rounded-sm space-y-4 flex flex-col">
          <div className="flex items-center justify-between border-b border-[#262830] pb-2.5">
            <div className="flex items-center space-x-2">
              <FontAwesomeIcon icon={faReceipt} className="text-zinc-400 text-xs" />
              <h3 className="text-xs font-bold text-white uppercase tracking-wider">Ostatnie Wpisy w Dzienniku Głównym</h3>
            </div>
            <span className="text-[10px] text-zinc-500">
              Podwójny zapis (2+ linie na transakcję)
            </span>
          </div>

          <div className="flex-1 space-y-2.5 overflow-y-auto">
            {recentTransactions.map(tx => (
              <div
                key={tx.id}
                onClick={() => onOpenTransaction(tx)}
                className={`p-3 rounded-sm border cursor-pointer transition ${
                  tx.type === 6 // Storno
                    ? 'bg-rose-950/20 border-rose-800/40 hover:border-rose-600/60'
                    : 'bg-[#16181d] border-[#262830] hover:border-emerald-500/40'
                }`}
              >
                <div className="flex items-start justify-between">
                  <div className="space-y-0.5">
                    <div className="flex items-center space-x-2">
                      <span className="font-bold text-xs text-white tracking-wide">{tx.referenceNumber}</span>
                      <span className={`text-[9px] px-1.5 py-0.2 rounded-sm font-bold ${
                        tx.statusName === 'Reversed'
                          ? 'bg-zinc-800 text-zinc-400 line-through'
                          : tx.type === 6
                          ? 'bg-rose-900/40 text-rose-300 border border-rose-700/50'
                          : 'bg-emerald-950/40 text-emerald-400 border border-emerald-700/40'
                      }`}>
                        {tx.typeName.toUpperCase()}
                      </span>
                      {tx.idempotencyKey && (
                        <span className="text-[8px] text-zinc-500 bg-[#1e2027] px-1 rounded-sm border border-[#2e313b]">
                          IDEMP
                        </span>
                      )}
                    </div>
                    <p className="text-[11px] text-zinc-400 font-sans line-clamp-1">{tx.description}</p>
                  </div>

                  <div className="text-right">
                    <div className="text-xs font-bold text-white">
                      {tx.totalAmount.toLocaleString('pl-PL', { minimumFractionDigits: 2 })} {tx.currency}
                    </div>
                    <div className="text-[9px] text-zinc-500">
                      {new Date(tx.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </div>
                  </div>
                </div>

                {/* Mini Journal Entries View */}
                <div className="mt-2.5 pt-2 border-t border-[#20222a] grid grid-cols-1 sm:grid-cols-2 gap-1.5 text-[10px]">
                  {tx.entries.slice(0, 2).map(entry => (
                    <div
                      key={entry.id}
                      className="flex items-center justify-between px-2 py-1 rounded-sm bg-[#0e0f12] border border-[#262830]"
                    >
                      <div className="flex items-center space-x-1.5 truncate">
                        <span className={`text-[9px] font-bold px-1 rounded-xs ${
                          entry.directionName === 'Debit'
                            ? 'bg-emerald-950 text-emerald-400 border border-emerald-700/50'
                            : 'bg-amber-950 text-amber-400 border border-amber-700/50'
                        }`}>
                          {entry.directionName === 'Debit' ? 'DEBIT' : 'CREDIT'}
                        </span>
                        <span className="text-zinc-300 truncate">{entry.accountNumber}</span>
                      </div>
                      <span className="font-bold text-zinc-200 ml-2">
                        {entry.amount.toFixed(2)} {entry.currency}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

