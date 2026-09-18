import React from 'react';
import { Link } from 'react-router-dom';
import { Sparkles, User, ExternalLink, Tag } from 'lucide-react';
import { formatCurrency, formatDateTime } from '../../../utils/formatters';
import { cn } from '../../../utils/cn';

export default function AiChatMessage({ message, onProductClick }) {
  const isAssistant = message.role === 'assistant';

  return (
    <div
      className={cn(
        "flex gap-3 animate-fade-in",
        isAssistant ? "items-start" : "items-start justify-end flex-row-reverse"
      )}
    >
      {/* Avatar */}
      <div
        className={cn(
          "w-8 h-8 rounded-full flex items-center justify-center shrink-0 text-xs font-bold shadow-xs",
          isAssistant
            ? "bg-gradient-to-tr from-accent-indigo via-indigo-600 to-teal-500 text-white"
            : "bg-ink text-white"
        )}
      >
        {isAssistant ? <Sparkles className="w-4 h-4" /> : <User className="w-4 h-4" />}
      </div>

      {/* Bubble Content */}
      <div
        className={cn(
          "max-w-[85%] rounded-2xl p-4 text-xs sm:text-sm space-y-2.5 leading-relaxed shadow-xs",
          isAssistant
            ? "bg-white border border-surface-subtle text-ink rounded-tl-xs"
            : "bg-ink text-white rounded-tr-xs"
        )}
      >
        {/* Message Text */}
        <div className="whitespace-pre-line">
          {message.content}
        </div>

        {/* Catalog Product Citations */}
        {isAssistant && message.referencedProducts && message.referencedProducts.length > 0 && (
          <div className="pt-2 border-t border-surface-subtle space-y-2">
            <p className="text-[10px] font-bold uppercase tracking-wider text-ink-muted flex items-center gap-1">
              <Tag className="w-3 h-3 text-accent-indigo" />
              Retrieved Products ({message.referencedProducts.length})
            </p>
            <div className="space-y-1.5">
              {message.referencedProducts.map((p) => (
                <div
                  key={p.id}
                  onClick={() => onProductClick?.(p)}
                  className="flex items-center justify-between gap-2 p-2 rounded-xl bg-surface-soft hover:bg-slate-200/80 border border-surface-subtle cursor-pointer transition-colors group"
                >
                  <div className="flex items-center gap-2 min-w-0">
                    {p.primaryImageUrl ? (
                      <img
                        src={p.primaryImageUrl}
                        alt={p.name}
                        className="w-8 h-8 rounded-lg object-contain bg-white p-0.5 border border-surface-subtle shrink-0"
                      />
                    ) : (
                      <div className="w-8 h-8 rounded-lg bg-white border border-surface-subtle flex items-center justify-center text-[9px] font-bold text-slate-400 shrink-0">
                        SP
                      </div>
                    )}
                    <div className="truncate">
                      <p className="font-bold text-xs text-ink truncate group-hover:text-accent-indigo transition-colors">
                        {p.name}
                      </p>
                      <p className="text-[10px] text-ink-muted">{p.categoryName}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-1.5 shrink-0">
                    <span className="font-extrabold text-xs text-ink">
                      {formatCurrency(p.basePrice)}
                    </span>
                    <ExternalLink className="w-3 h-3 text-ink-subtle group-hover:text-accent-indigo transition-colors" />
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
