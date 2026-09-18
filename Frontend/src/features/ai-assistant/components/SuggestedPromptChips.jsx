import React from 'react';
import { Sparkles } from 'lucide-react';

const SUGGESTED_PROMPTS = [
  "Find a high-performance laptop for programming",
  "Which smartphones have the best camera and battery?",
  "Recommend noise-cancelling headphones for flights",
  "Show me smartwatches with heart rate tracking",
];

export default function SuggestedPromptChips({ onSelectPrompt }) {
  return (
    <div className="space-y-2">
      <p className="text-[11px] font-bold text-ink-muted uppercase tracking-wider flex items-center gap-1">
        <Sparkles className="w-3 h-3 text-accent-indigo" />
        Suggested Inquiries
      </p>
      <div className="flex flex-wrap gap-1.5">
        {SUGGESTED_PROMPTS.map((prompt, i) => (
          <button
            key={i}
            type="button"
            onClick={() => onSelectPrompt(prompt)}
            className="text-xs text-left px-3 py-1.5 rounded-full bg-surface-soft hover:bg-slate-200 border border-surface-subtle text-ink transition-all font-medium hover:border-slate-400"
          >
            {prompt}
          </button>
        ))}
      </div>
    </div>
  );
}
