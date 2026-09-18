import React from 'react';
import { cn } from '../../utils/cn';

export default function Tabs({
  tabs,
  activeTab,
  onChange,
  className,
}) {
  return (
    <div className={cn("flex items-center gap-2 border-b border-surface-subtle pb-px overflow-x-auto no-scrollbar", className)}>
      {tabs.map((tab) => {
        const isActive = activeTab === tab.id;
        return (
          <button
            key={tab.id}
            type="button"
            onClick={() => onChange(tab.id)}
            className={cn(
              "px-4 py-2.5 text-sm font-semibold transition-all relative whitespace-nowrap rounded-t-lg",
              isActive
                ? "text-ink border-b-2 border-ink bg-surface-soft"
                : "text-ink-muted hover:text-ink hover:bg-surface-muted"
            )}
          >
            {tab.label}
            {tab.count !== undefined && (
              <span className={cn(
                "ml-2 text-xs py-0.5 px-2 rounded-full",
                isActive ? "bg-ink text-white" : "bg-surface-subtle text-ink-muted"
              )}>
                {tab.count}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}
