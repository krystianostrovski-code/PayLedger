import React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faScaleBalanced,
  faBuildingColumns,
  faReceipt,
  faArrowRightArrowLeft,
  faShieldHalved,
  faCircleCheck
} from './icons';
import type { LedgerIntegrityReport } from '../types';

interface NavbarProps {
  activeTab: 'terminal' | 'accounts' | 'journal' | 'transfer';
  onTabChange: (tab: 'terminal' | 'accounts' | 'journal' | 'transfer') => void;
  integrity: LedgerIntegrityReport | null;
  onRefresh: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({
  activeTab,
  onTabChange,
  integrity,
  onRefresh
}) => {
  return (
    <header className="bg-[#121316] border-b border-[#262830] px-4 py-2.5 flex items-center justify-between font-mono select-none">
      {/* Brand */}
      <div className="flex items-center space-x-3">
        <div className="w-8 h-8 rounded-sm bg-emerald-950/40 border border-emerald-500/40 text-emerald-400 flex items-center justify-center text-sm shadow-[0_0_12px_rgba(16,185,129,0.2)]">
          <FontAwesomeIcon icon={faScaleBalanced} />
        </div>
        <div>
          <div className="flex items-center space-x-2">
            <span className="font-bold text-sm tracking-wider text-white">PAYLEDGER</span>
            <span className="text-[9px] px-1.5 py-0.5 rounded-sm bg-[#1e2027] text-zinc-400 border border-[#2e313b] font-bold">
              CORE v1.0
            </span>
          </div>
          <div className="text-[10px] text-zinc-500 font-sans tracking-tight">
            Double-Entry B2B Settlement Engine
          </div>
        </div>
      </div>

      {/* Navigation Tabs */}
      <nav className="hidden md:flex items-center space-x-1">
        {[
          { id: 'terminal', label: 'TERMINAL', icon: faScaleBalanced },
          { id: 'accounts', label: 'KONTA I WALLETY', icon: faBuildingColumns },
          { id: 'journal', label: 'DZIENNIK GŁÓWNY', icon: faReceipt },
          { id: 'transfer', label: 'OPERACJE B2B', icon: faArrowRightArrowLeft }
        ].map(tab => {
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => onTabChange(tab.id as any)}
              className={`flex items-center space-x-2 px-3.5 py-1.5 text-xs font-bold transition rounded-sm cursor-pointer ${
                isActive
                  ? 'bg-emerald-950/30 text-emerald-400 border border-emerald-500/50 shadow-[0_0_10px_rgba(16,185,129,0.15)]'
                  : 'text-zinc-400 hover:text-zinc-200 hover:bg-[#1a1c23] border border-transparent'
              }`}
            >
              <FontAwesomeIcon icon={tab.icon} className="text-[11px]" />
              <span>{tab.label}</span>
            </button>
          );
        })}
      </nav>

      {/* Right Side: Ledger Integrity Status */}
      <div className="flex items-center space-x-3">
        <div
          onClick={onRefresh}
          className="flex items-center space-x-2 px-2.5 py-1 rounded-sm bg-[#16181e] border border-[#262830] text-[10px] cursor-pointer hover:border-zinc-600 transition"
          title="Kliknij, aby odświeżyć audyt księgowy"
        >
          <span className="relative flex h-2 w-2">
            <span className={`animate-ping absolute inline-flex h-full w-full rounded-full opacity-75 ${
              integrity?.isHealthy ? 'bg-emerald-400' : 'bg-rose-400'
            }`} />
            <span className={`relative inline-flex rounded-full h-2 w-2 ${
              integrity?.isHealthy ? 'bg-emerald-500' : 'bg-rose-500'
            }`} />
          </span>
          <span className="text-zinc-400 font-bold hidden sm:inline">AUDYT:</span>
          <span className={integrity?.isHealthy ? 'text-emerald-400 font-bold' : 'text-rose-400 font-bold'}>
            {integrity?.isHealthy ? 'ZEROWY BILANS (OK)' : 'NIEZBILANSOWANY'}
          </span>
        </div>
      </div>
    </header>
  );
};
