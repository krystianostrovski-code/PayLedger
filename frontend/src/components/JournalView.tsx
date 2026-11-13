import React, { useState } from 'react';
import type { LedgerTransaction } from '../types';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faReceipt,
  faRotateLeft,
  faScaleBalanced,
  faXmark
} from './icons';

interface JournalViewProps {
  transactions: LedgerTransaction[];
  onSelectTransaction: (tx: LedgerTransaction) => void;
  onReverseTransaction: (txId: string, reason: string) => Promise<void>;
}

export const JournalView: React.FC<JournalViewProps> = ({
  transactions,
  onSelectTransaction,
  onReverseTransaction
}) => {
  const [filterType, setFilterType] = useState<string>('ALL');
  const [reversalModalTx, setReversalModalTx] = useState<LedgerTransaction | null>(null);
  const [reversalReason, setReversalReason] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  const filtered = transactions.filter(t => {
    if (filterType !== 'ALL' && t.typeName !== filterType) return false;
    return true;
  });

  const handleConfirmReversal = async () => {
    if (!reversalModalTx || !reversalReason.trim()) return;
    try {
      setIsSubmitting(true);
      await onReverseTransaction(reversalModalTx.id, reversalReason.trim());
      setReversalModalTx(null);
      setReversalReason('');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4 font-mono text-zinc-200">
      {/* Header bar */}
      <div className="bg-[#121316] border border-[#262830] p-3.5 rounded-sm flex items-center justify-between">
        <div className="flex items-center space-x-2.5">
          <FontAwesomeIcon icon={faReceipt} className="text-emerald-400" />
          <h2 className="text-xs font-bold text-white uppercase tracking-wider">
            Główna Księga Rachunkowa (Dziennik Podwójnego Zapisu)
          </h2>
        </div>

        <select
          value={filterType}
          onChange={e => setFilterType(e.target.value)}
          className="bg-[#181920] border border-[#262830] px-2.5 py-1.5 rounded-sm text-xs text-zinc-300"
        >
          <option value="ALL">Wszystkie rodzaje transakcji</option>
          <option value="Transfer">Transfery B2B</option>
          <option value="Deposit">Wpłaty / Doładowania</option>
          <option value="CurrencyExchange">Wymiana Walut (FX)</option>
          <option value="StornoReversal">Korekty Storno</option>
          <option value="FeeCharge">Opłaty manipulacyjne</option>
        </select>
      </div>

      {/* Transactions Table */}
      <div className="bg-[#121316] border border-[#262830] rounded-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="bg-[#16181d] border-b border-[#262830] text-[10px] text-zinc-400 uppercase tracking-wider">
                <th className="p-3">Referencja / Hash</th>
                <th className="p-3">Data i Czas</th>
                <th className="p-3">Typ Operacji</th>
                <th className="p-3">Opis Transakcji</th>
                <th className="p-3 text-right">Kwota Zlecenia</th>
                <th className="p-3 text-center">Nogi Księgowania</th>
                <th className="p-3 text-right">Akcja</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#20222a]">
              {filtered.map(tx => (
                <tr
                  key={tx.id}
                  onClick={() => onSelectTransaction(tx)}
                  className={`hover:bg-[#181920] cursor-pointer transition ${
                    tx.type === 6 ? 'bg-rose-950/10' : ''
                  }`}
                >
                  <td className="p-3 font-bold text-white tracking-wide">
                    {tx.referenceNumber}
                    {tx.idempotencyKey && (
                      <span className="ml-1.5 text-[8px] text-zinc-500 bg-[#1e2027] px-1 rounded-sm border border-[#2e313b]">
                        IDEMP
                      </span>
                    )}
                  </td>
                  <td className="p-3 text-zinc-400 text-[11px]">
                    {new Date(tx.createdAt).toLocaleString()}
                  </td>
                  <td className="p-3">
                    <span className={`text-[10px] font-bold px-2 py-0.5 rounded-sm ${
                      tx.statusName === 'Reversed'
                        ? 'bg-zinc-800 text-zinc-400 line-through'
                        : tx.type === 6
                        ? 'bg-rose-900/40 text-rose-300 border border-rose-700/50'
                        : tx.type === 4
                        ? 'bg-amber-950/40 text-amber-300 border border-amber-700/50'
                        : 'bg-emerald-950/40 text-emerald-400 border border-emerald-700/50'
                    }`}>
                      {tx.typeName.toUpperCase()}
                    </span>
                  </td>
                  <td className="p-3 text-zinc-300 font-sans text-xs max-w-[280px] truncate">
                    {tx.description}
                  </td>
                  <td className="p-3 text-right font-bold text-white text-xs">
                    {tx.totalAmount.toLocaleString('pl-PL', { minimumFractionDigits: 2 })} {tx.currency}
                  </td>
                  <td className="p-3 text-center">
                    <span className="text-[10px] px-2 py-0.5 rounded-sm bg-[#16181e] text-zinc-300 border border-[#262830]">
                      {tx.entries.length} LEGS (Σ=0)
                    </span>
                  </td>
                  <td className="p-3 text-right">
                    {tx.statusName !== 'Reversed' && tx.type !== 6 && (
                      <button
                        onClick={e => {
                          e.stopPropagation();
                          setReversalModalTx(tx);
                          setReversalReason(`Anulowanie transakcji ${tx.referenceNumber}`);
                        }}
                        className="px-2 py-1 rounded-sm bg-rose-950/30 hover:bg-rose-900/50 text-rose-300 border border-rose-700/40 text-[10px] font-bold transition cursor-pointer"
                        title="Wykonaj księgowanie korygujące (Storno)"
                      >
                        <FontAwesomeIcon icon={faRotateLeft} className="mr-1" />
                        STORNO
                      </button>
                    )}
                    {tx.statusName === 'Reversed' && (
                      <span className="text-[10px] text-zinc-500 font-bold">
                        SKORYGOWANE
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Storno Modal */}
      {reversalModalTx && (
        <div className="fixed inset-0 z-50 bg-black/75 flex items-center justify-center p-4">
          <div className="w-full max-w-md bg-[#14151a] border border-[#262830] rounded-sm shadow-2xl p-5 space-y-4">
            <div className="flex items-center justify-between border-b border-[#262830] pb-3">
              <div className="flex items-center space-x-2 text-rose-400 font-bold text-xs">
                <FontAwesomeIcon icon={faRotateLeft} />
                <span>KOREKTA KSIĘGOWA (STORNO)</span>
              </div>
              <button
                onClick={() => setReversalModalTx(null)}
                className="text-zinc-500 hover:text-white"
              >
                <FontAwesomeIcon icon={faXmark} />
              </button>
            </div>

            <p className="text-xs text-zinc-300 font-sans leading-relaxed">
              Zgodnie z zasadą podwójnego zapisu transakcje nie mogą być usuwane. Wygenerowana zostanie transakcja korygująca odwracająca wszystkie pozycje Debet i Kredyt dla <span className="font-mono font-bold text-white">{reversalModalTx.referenceNumber}</span>.
            </p>

            <div className="space-y-1">
              <label className="text-[10px] text-zinc-400 font-bold uppercase">Powód korekty:</label>
              <input
                type="text"
                value={reversalReason}
                onChange={e => setReversalReason(e.target.value)}
                className="w-full bg-[#181920] border border-[#262830] px-3 py-2 rounded-sm text-xs text-white font-sans"
              />
            </div>

            <div className="pt-2 flex justify-end space-x-2">
              <button
                onClick={() => setReversalModalTx(null)}
                className="px-3 py-1.5 rounded-sm bg-[#1e2027] text-zinc-300 text-xs font-bold"
              >
                ANULUJ
              </button>
              <button
                onClick={handleConfirmReversal}
                disabled={isSubmitting || !reversalReason.trim()}
                className="px-4 py-1.5 rounded-sm bg-rose-600 hover:bg-rose-500 text-white font-bold text-xs transition disabled:opacity-50"
              >
                {isSubmitting ? 'KSIĘGOWANIE...' : 'ZATWIERDŹ STORNO'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
