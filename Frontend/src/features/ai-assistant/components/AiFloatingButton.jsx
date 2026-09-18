import React from 'react';
import { Sparkles } from 'lucide-react';
import { useAiChat } from '../../../app/providers/AiChatContext';

export default function AiFloatingButton() {
  const { openAi, isAiOpen } = useAiChat();

  if (isAiOpen) return null;

  return (
    <div className="fixed bottom-6 right-6 z-40 animate-fade-in">
      <button
        onClick={openAi}
        className="group relative flex items-center gap-2.5 px-4 py-3 rounded-full bg-ink text-white hover:bg-slate-800 shadow-floating hover:shadow-2xl transition-all duration-300 active:scale-95 border border-slate-700"
        aria-label="Open AI Shopping Assistant"
      >
        <div className="relative">
          <div className="w-7 h-7 rounded-full bg-gradient-to-tr from-accent-indigo to-teal-400 flex items-center justify-center text-white shadow-xs group-hover:scale-110 transition-transform">
            <Sparkles className="w-4 h-4" />
          </div>
          <span className="absolute -top-1 -right-1 flex h-2.5 w-2.5">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-teal-400 opacity-75" />
            <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-teal-500" />
          </span>
        </div>

        <div className="text-left hidden sm:block pr-1">
          <p className="text-xs font-bold leading-none">AI Assistant</p>
          <p className="text-[10px] text-slate-300 leading-none mt-1">Ask questions</p>
        </div>
      </button>
    </div>
  );
}
