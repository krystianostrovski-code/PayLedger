import React from 'react';
import type { LedgerTransaction } from '../types';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faReceipt,
  faScaleBalanced,
  faXmark,
  faFingerprint
} from './icons';

interface TransactionDetailModalProps {
  transaction: LedgerTransaction | null;
  onClose: () => void;
}

export const TransactionDetailModal: React.FC<TransactionDetailModalProps> = ({
  transaction,
  onClose
}) => {
  if (!transaction) return null;

  const totalDebits = transaction.entries
    .filter(e => e.directionName === 'Debit')
    .reduce((acc, cur) => acc + cur.amount, 0);

  const totalCredits = transaction.entries
    .filter(e => e.directionName === 'Credit')
    .reduce((acc, cur) => acc + cur.amount, 0);

  const isBalanced = Math.abs(totalDebits - totalCredits) < 0.0001;

  return (
    <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4 font-mono">
      <div className="w-full max-w-2xl bg-[#14151a] border border-[#262830] rounded-sm shadow-2xl flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="p-4 bg-[#181920] border-b border-[#262830] flex items-center justify-between">
          <div className="flex items-center space-x-2.5">
            <div className="w-8 h-8 rounded-sm bg-emerald-950/40 border border-emerald-600/40 text-emerald-400 flex items-center justify-center text-xs">
              <FontAwesomeIcon icon={faReceipt} />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="text-xs font-bold text-white">{transaction.referenceNumber}</span>
                <span className="text-[10px] px-1.5 py-0.2 rounded-sm bg-emerald-950 text-emerald-300 border border-emerald-700/50 font-bold">
                  {transaction.typeName.toUpperCase()}
                </span>
              </div>
              <div className="text-[10px] text-zinc-400">
                Data księgowania: {new Date(transaction.createdAt).toLocaleString()}
              </div>
            </div>
          </div>

          <button
            onClick={onClose}
            className="p-1.5 text-zinc-400 hover:text-white hover:bg-[#22232a] rounded-sm transition"
          >
            <FontAwesomeIcon icon={faXmark} />
          </button>
        </div>

        {/* Content */}
        <div className="p-5 overflow-y-auto space-y-4 text-xs">
          {/* Metadata Card */}
          <div className="p-3 bg-[#0e0f12] border border-[#262830] rounded-sm space-y-2 text-[11px]">
            <div className="flex justify-between">
              <span className="text-zinc-500">TYTUŁ / OPIS:</span>
              <span className="text-zinc-200 font-sans font-medium">{transaction.description}</span>
            </div>
            {transaction.idempotencyKey && (
              <div className="flex justify-between items-center">
                <span className="text-zinc-500 flex items-center space-x-1">
                  <FontAwesomeIcon icon={faFingerprint} className="text-emerald-400" />
                  <span>KLUCZ IDEMPOTENCJI:</span>
                </span>
                <span className="text-emerald-400 font-bold">{transaction.idempotencyKey}</span>
              </div>
            )}
            {transaction.originalTransactionId && (
              <div className="flex justify-between">
                <span className="text-rose-400">KORYGUJE TRANSAKCJĘ ID:</span>
                <span className="text-zinc-300">{transaction.originalTransactionId}</span>
              </div>
            )}
          </div>

          {/* Double-Entry Legs Journal */}
          <div className="space-y-2">
            <div className="flex items-center justify-between text-[11px] font-bold text-zinc-300">
              <span>NOGI KSIĘGOWANIA (PODWÓJNY ZAPIS):</span>
              <span className={isBalanced ? 'text-emerald-400' : 'text-rose-400'}>
                {isBalanced ? 'BILANS ZEROWY OK' : 'BŁĄD ZEROWEGO BILANSU'}
              </span>
            </div>

            <div className="space-y-1.5">
              {transaction.entries.map((entry, idx) => (
                <div
                  key={entry.id}
                  className="p-3 rounded-sm bg-[#16181d] border border-[#262830] flex items-center justify-between"
                >
                  <div className="flex items-center space-x-3">
                    <span className="text-zinc-500 text-[10px] w-4">{idx + 1}.</span>
                    <span className={`text-[10px] font-bold px-2 py-0.5 rounded-sm ${
                      entry.directionName === 'Debit'
                        ? 'bg-emerald-950/70 text-emerald-400 border border-emerald-600/50'
                        : 'bg-amber-950/70 text-amber-400 border border-amber-600/50'
                    }`}>
                      {entry.directionName === 'Debit' ? 'DEBET (Winien)' : 'KREDYT (Ma)'}
                    </span>
                    <div>
                      <div className="text-white font-bold text-xs">{entry.accountName}</div>
                      <div className="text-[10px] text-zinc-400">{entry.accountNumber}</div>
                    </div>
                  </div>

                  <div className="text-right">
                    <div className="text-xs font-bold text-white">
                      {entry.amount.toLocaleString('pl-PL', { minimumFractionDigits: 2 })} {entry.currency}
                    </div>
                    <div className="text-[9px] text-zinc-500 max-w-[200px] truncate">{entry.description}</div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Sums Verification */}
          <div className="p-3 bg-[#0e0f12] border border-[#262830] rounded-sm flex items-center justify-between text-xs font-bold">
            <div className="flex items-center space-x-2 text-zinc-400">
              <FontAwesomeIcon icon={faScaleBalanced} className="text-emerald-400" />
              <span>SUMA D/C:</span>
            </div>
            <div className="flex space-x-4">
              <div>
                <span className="text-zinc-500 text-[10px] mr-1">DEBET:</span>
                <span className="text-emerald-400">{totalDebits.toFixed(2)}</span>
              </div>
              <div>
                <span className="text-zinc-500 text-[10px] mr-1">KREDYT:</span>
                <span className="text-amber-400">{totalCredits.toFixed(2)}</span>
              </div>
              <div>
                <span className="text-zinc-500 text-[10px] mr-1">RÓŻNICA:</span>
                <span className="text-white">{(totalDebits - totalCredits).toFixed(4)}</span>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="p-3 bg-[#181920] border-t border-[#262830] flex justify-end">
          <button
            onClick={onClose}
            className="px-4 py-1.5 rounded-sm bg-[#22232a] hover:bg-[#2d2e38] text-white text-xs font-bold transition"
          >
            ZAMKNIJ
          </button>
        </div>
      </div>
    </div>
  );
};

