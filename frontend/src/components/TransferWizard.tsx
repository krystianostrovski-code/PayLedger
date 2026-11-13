import React, { useState } from 'react';
import type { Account, LedgerTransaction } from '../types';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faArrowRightArrowLeft,
  faFingerprint,
  faCircleCheck,
  faRotate,
  faTriangleExclamation
} from './icons';
import type { ExecutionResponse } from '../services/api';

interface TransferWizardProps {
  accounts: Account[];
  preselectedSourceAccount?: Account | null;
  onExecuteTransfer: (data: {
    sourceAccountId: string;
    destinationAccountId: string;
    amount: number;
    description: string;
    idempotencyKey?: string;
  }) => Promise<ExecutionResponse<LedgerTransaction>>;
}

export const TransferWizard: React.FC<TransferWizardProps> = ({
  accounts,
  preselectedSourceAccount,
  onExecuteTransfer
}) => {
  const [sourceId, setSourceId] = useState<string>(preselectedSourceAccount?.id ?? accounts[0]?.id ?? '');
  const [destinationId, setDestinationId] = useState<string>(accounts[1]?.id ?? '');
  const [amount, setAmount] = useState<string>('5000');
  const [description, setDescription] = useState<string>('Rozliczenie B2B za usługi transportowe i logistyczne');
  const [useIdempotency, setUseIdempotency] = useState<boolean>(true);
  const [idempotencyKey, setIdempotencyKey] = useState<string>(`IDEMP-${Date.now().toString(36).toUpperCase()}`);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [lastResponse, setLastResponse] = useState<ExecutionResponse<LedgerTransaction> | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const sourceAccount = accounts.find(a => a.id === sourceId);
  const destinationAccount = accounts.find(a => a.id === destinationId);

  // Available destination accounts (same currency, not identical to source)
  const availableDestinations = accounts.filter(a => a.id !== sourceId && a.currency === sourceAccount?.currency);

  const handleGenerateNewKey = () => {
    setIdempotencyKey(`IDEMP-${Date.now().toString(36).toUpperCase()}`);
  };

  const handleSubmit = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    setErrorMessage(null);

    const parsedAmount = parseFloat(amount);
    if (isNaN(parsedAmount) || parsedAmount <= 0) {
      setErrorMessage('Wprowadź prawidłową kwotę transferu większą od zera.');
      return;
    }

    if (!sourceId || !destinationId) {
      setErrorMessage('Wybierz rachunek źródłowy oraz docelowy.');
      return;
    }

    try {
      setIsSubmitting(true);
      const res = await onExecuteTransfer({
        sourceAccountId: sourceId,
        destinationAccountId: destinationId,
        amount: parsedAmount,
        description: description.trim(),
        idempotencyKey: useIdempotency ? idempotencyKey.trim() : undefined
      });
      setLastResponse(res);
    } catch (err: any) {
      setErrorMessage(err.message || 'Wystąpił błąd podczas księgowania.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex-1 overflow-y-auto p-4 sm:p-6 font-mono text-zinc-200">
      <div className="max-w-3xl mx-auto space-y-6">
        {/* Header */}
        <div className="bg-[#121316] border border-[#262830] p-4 rounded-sm flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="w-8 h-8 rounded-sm bg-emerald-950/40 border border-emerald-600/40 text-emerald-400 flex items-center justify-center text-xs">
              <FontAwesomeIcon icon={faArrowRightArrowLeft} />
            </div>
            <div>
              <h2 className="text-xs font-bold text-white uppercase tracking-wider">
                Generator Transferu B2B i Test Idempotencji
              </h2>
              <p className="text-[10px] text-zinc-400 font-sans">
                Realizuje atomowy podwójny zapis w księdze głównej z gwarancją pojedynczego wykonania
              </p>
            </div>
          </div>
        </div>

        {/* Main Form */}
        <div className="bg-[#121316] border border-[#262830] p-5 rounded-sm space-y-5">
          {errorMessage && (
            <div className="p-3 bg-rose-950/30 border border-rose-600/40 rounded-sm text-xs text-rose-300 flex items-center space-x-2">
              <FontAwesomeIcon icon={faTriangleExclamation} />
              <span>{errorMessage}</span>
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Source Account */}
            <div className="space-y-1.5">
              <label className="text-[10px] text-zinc-400 font-bold uppercase">
                Rachunek Źródłowy (Kredyt / Obciążenie)
              </label>
              <select
                value={sourceId}
                onChange={e => {
                  setSourceId(e.target.value);
                  setDestinationId('');
                }}
                className="w-full bg-[#181920] border border-[#262830] px-3 py-2 rounded-sm text-xs text-white"
              >
                {accounts.filter(a => a.balance > 0 || a.allowOverdraft).map(a => (
                  <option key={a.id} value={a.id}>
                    {a.name} — {a.balance.toLocaleString('pl-PL', { minimumFractionDigits: 2 })} {a.currency} ({a.accountNumber})
                  </option>
                ))}
              </select>
              {sourceAccount && (
                <div className="text-[10px] text-zinc-500">
                  Dostępne środki: <span className="text-white font-bold">{sourceAccount.balance.toLocaleString('pl-PL', { minimumFractionDigits: 2 })} {sourceAccount.currency}</span>
                </div>
              )}
            </div>

            {/* Destination Account */}
            <div className="space-y-1.5">
              <label className="text-[10px] text-zinc-400 font-bold uppercase">
                Rachunek Docelowy (Debet / Uznanie)
              </label>
              <select
                value={destinationId}
                onChange={e => setDestinationId(e.target.value)}
                className="w-full bg-[#181920] border border-[#262830] px-3 py-2 rounded-sm text-xs text-white"
              >
                <option value="">Wybierz rachunek docelowy...</option>
                {availableDestinations.map(a => (
                  <option key={a.id} value={a.id}>
                    {a.name} ({a.accountNumber})
                  </option>
                ))}
              </select>
              {destinationAccount && (
                <div className="text-[10px] text-zinc-500">
                  Waluta odbiorcy: <span className="text-white font-bold">{destinationAccount.currency}</span>
                </div>
              )}
            </div>
          </div>

          {/* Amount & Description */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="space-y-1.5">
              <label className="text-[10px] text-zinc-400 font-bold uppercase">
                Kwota Transferu ({sourceAccount?.currency ?? 'PLN'})
              </label>
              <input
                type="number"
                step="0.01"
                min="0.01"
                value={amount}
                onChange={e => setAmount(e.target.value)}
                className="w-full bg-[#181920] border border-[#262830] px-3 py-2 rounded-sm text-xs text-white font-bold"
              />
            </div>

            <div className="sm:col-span-2 space-y-1.5">
              <label className="text-[10px] text-zinc-400 font-bold uppercase">
                Tytuł Płatności B2B
              </label>
              <input
                type="text"
                value={description}
                onChange={e => setDescription(e.target.value)}
                className="w-full bg-[#181920] border border-[#262830] px-3 py-2 rounded-sm text-xs text-white font-sans"
              />
            </div>
          </div>

          {/* Idempotency Control Panel (Key feature for recruiters) */}
          <div className="p-3.5 bg-[#0e0f12] border border-[#262830] rounded-sm space-y-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <FontAwesomeIcon icon={faFingerprint} className="text-emerald-400 text-xs" />
                <span className="text-xs font-bold text-white uppercase">Mechanizm Idempotencji (Idempotency-Key)</span>
              </div>
              <label className="flex items-center space-x-2 text-xs text-zinc-300 cursor-pointer">
                <input
                  type="checkbox"
                  checked={useIdempotency}
                  onChange={e => setUseIdempotency(e.target.checked)}
                  className="rounded-none accent-emerald-500"
                />
                <span className="text-[11px]">Włącz nagłówek</span>
              </label>
            </div>

            {useIdempotency && (
              <div className="flex items-center space-x-2">
                <input
                  type="text"
                  value={idempotencyKey}
                  onChange={e => setIdempotencyKey(e.target.value)}
                  className="flex-1 bg-[#181920] border border-[#262830] px-3 py-1.5 rounded-sm text-xs text-emerald-400 font-mono"
                />
                <button
                  type="button"
                  onClick={handleGenerateNewKey}
                  className="px-2.5 py-1.5 rounded-sm bg-[#1e2027] hover:bg-[#282a35] text-zinc-300 text-xs font-bold transition"
                  title="Wygeneruj nowy losowy klucz"
                >
                  <FontAwesomeIcon icon={faRotate} className="mr-1" />
                  NOWY
                </button>
              </div>
            )}
            <p className="text-[10px] text-zinc-500 font-sans leading-relaxed">
              Klucz idempotencji zapewnia, że nawet w przypadku ponowienia żądania przez sieć lub wielokrotnego kliknięcia, konto zostanie obciążone tylko raz. Serwer zwróci odpowiedź z pamięci podręcznej.
            </p>
          </div>

          {/* Submit Buttons */}
          <div className="pt-2 flex flex-wrap gap-2.5 justify-end">
            <button
              type="button"
              onClick={() => handleSubmit()}
              disabled={isSubmitting}
              className="px-5 py-2 rounded-sm bg-emerald-600 hover:bg-emerald-500 text-zinc-950 font-black text-xs border border-emerald-400/50 transition cursor-pointer disabled:opacity-50"
            >
              {isSubmitting ? 'KSIĘGOWANIE...' : 'ZATWIERDŹ I ZAKSIĘGUJ'}
            </button>
          </div>
        </div>

        {/* Idempotency Result Card */}
        {lastResponse && (
          <div className={`p-4 rounded-sm border space-y-3 ${
            lastResponse.isIdempotentReplay
              ? 'bg-amber-950/20 border-amber-600/50'
              : 'bg-emerald-950/20 border-emerald-600/50'
          }`}>
            <div className="flex items-center justify-between border-b border-[#262830] pb-2">
              <div className="flex items-center space-x-2">
                <FontAwesomeIcon
                  icon={faCircleCheck}
                  className={lastResponse.isIdempotentReplay ? 'text-amber-400' : 'text-emerald-400'}
                />
                <span className="text-xs font-bold text-white uppercase">
                  Odpowiedź Serwera: 200 OK
                </span>
                <span className={`text-[9px] px-1.5 py-0.5 rounded-sm font-bold ${
                  lastResponse.isIdempotentReplay
                    ? 'bg-amber-900/50 text-amber-200 border border-amber-600'
                    : 'bg-emerald-900/50 text-emerald-200 border border-emerald-600'
                }`}>
                  CACHE: {lastResponse.cacheLookup} {lastResponse.isIdempotentReplay ? '(REPLAYED / NO DOUBLE DEBIT)' : '(FRESH EXECUTION)'}
                </span>
              </div>
              <span className="text-[10px] text-zinc-400">
                Ref: {lastResponse.data.referenceNumber}
              </span>
            </div>

            <p className="text-xs text-zinc-300 font-sans">
              {lastResponse.isIdempotentReplay
                ? 'Żądanie zostało pomyślnie zidentyfikowane jako duplikat. Zwrócono wcześniejszą odpowiedź z pamięci cache bez powtórnego obciążania salda.'
                : `Transakcja zaksięgowana pomyślnie w podwójnym zapisie: ${lastResponse.data.totalAmount.toLocaleString('pl-PL', { minimumFractionDigits: 2 })} ${lastResponse.data.currency}.`}
            </p>

            {/* Simulation button */}
            <div className="pt-1">
              <button
                type="button"
                onClick={() => handleSubmit()}
                className="px-3 py-1.5 rounded-sm bg-[#1e2027] hover:bg-[#282a35] text-amber-300 border border-amber-600/40 text-xs font-bold transition cursor-pointer"
              >
                ⚡ Kliknij ponownie z tym samym kluczem (Test ponowienia / podwójnego kliku)
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
