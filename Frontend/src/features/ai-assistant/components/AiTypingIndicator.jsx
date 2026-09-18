import React from 'react';
import { Sparkles } from 'lucide-react';

export default function AiTypingIndicator() {
  return (
    <div className="flex items-start gap-3 animate-fade-in">
      <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-accent-indigo to-teal-500 text-white flex items-center justify-center shrink-0 shadow-xs">
        <Sparkles className="w-4 h-4 animate-spin" />
      </div>
      <div className="bg-surface-soft border border-surface-subtle rounded-2xl rounded-tl-sm px-4 py-3 shadow-xs space-y-1">
        <p className="text-[11px] font-semibold text-accent-indigo uppercase tracking-wider">
          AI Catalog Reasoning...
        </p>
        <div className="flex items-center gap-1.5 py-1">
          <div className="w-2 h-2 rounded-full bg-indigo-500 animate-bounce" style={{ animationDelay: '0ms' }} />
          <div className="w-2 h-2 rounded-full bg-indigo-500 animate-bounce" style={{ animationDelay: '150ms' }} />
          <div className="w-2 h-2 rounded-full bg-indigo-500 animate-bounce" style={{ animationDelay: '300ms' }} />
        </div>
      </div>
    </div>
  );
}
