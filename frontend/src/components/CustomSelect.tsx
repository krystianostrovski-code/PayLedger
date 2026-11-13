import React, { useState, useRef, useEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCheck } from './icons';

export interface SelectOption {
  value: string;
  label: string;
  subLabel?: string;
  badge?: string;
  balance?: string;
}

interface CustomSelectProps {
  options: SelectOption[];
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  label?: string;
}

export const CustomSelect: React.FC<CustomSelectProps> = ({
  options,
  value,
  onChange,
  placeholder = 'Wybierz opcję...'
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const selectedOption = options.find(o => o.value === value);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div className="relative font-mono select-none" ref={containerRef}>
      {/* Selected Box */}
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="w-full bg-[#16181d] hover:bg-[#1a1c23] border border-[#262830] hover:border-[#383a45] px-3 py-2 rounded-sm text-left flex items-center justify-between transition cursor-pointer text-xs"
      >
        <div className="flex items-center space-x-2 truncate">
          {selectedOption ? (
            <>
              {selectedOption.badge && (
                <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-sm bg-[#22242c] text-white border border-[#30333d]">
                  {selectedOption.badge}
                </span>
              )}
              <span className="font-bold text-white truncate">{selectedOption.label}</span>
              {selectedOption.balance && (
                <span className="text-emerald-400 font-bold ml-1 text-[11px] truncate">
                  ({selectedOption.balance})
                </span>
              )}
            </>
          ) : (
            <span className="text-zinc-500">{placeholder}</span>
          )}
        </div>

        <svg
          className={`w-3.5 h-3.5 text-zinc-400 transition-transform duration-150 ${isOpen ? 'rotate-180' : ''}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute left-0 right-0 top-full mt-1 bg-[#14151a] border border-[#2a2c36] shadow-2xl z-50 max-h-60 overflow-y-auto rounded-sm divide-y divide-[#1e2027]">
          {options.length === 0 ? (
            <div className="p-3 text-center text-xs text-zinc-500">Brak dostępnych opcji</div>
          ) : (
            options.map(option => {
              const isSelected = option.value === value;
              return (
                <div
                  key={option.value}
                  onClick={() => {
                    onChange(option.value);
                    setIsOpen(false);
                  }}
                  className={`p-2.5 flex items-center justify-between cursor-pointer transition text-xs ${
                    isSelected
                      ? 'bg-[#20222a] text-white border-l-2 border-emerald-400'
                      : 'hover:bg-[#1a1c22] text-zinc-300'
                  }`}
                >
                  <div className="flex items-center space-x-2.5 truncate">
                    {option.badge && (
                      <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-sm bg-[#1e2027] text-white border border-[#2e313b]">
                        {option.badge}
                      </span>
                    )}
                    <div className="truncate">
                      <div className="font-bold text-white truncate flex items-center space-x-2">
                        <span>{option.label}</span>
                        {option.balance && (
                          <span className="text-emerald-400 text-[11px] font-mono">
                            • {option.balance}
                          </span>
                        )}
                      </div>
                      {option.subLabel && (
                        <div className="text-[10px] text-zinc-400 truncate">{option.subLabel}</div>
                      )}
                    </div>
                  </div>

                  {isSelected && (
                    <FontAwesomeIcon icon={faCheck} className="text-emerald-400 text-xs ml-2" />
                  )}
                </div>
              );
            })
          )}
        </div>
      )}
    </div>
  );
};
