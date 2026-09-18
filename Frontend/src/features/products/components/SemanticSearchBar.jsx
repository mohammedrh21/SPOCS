import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Sparkles, X, Loader2, ArrowRight, CornerDownLeft } from 'lucide-react';
import { productApi } from '../api/productApi';
import { formatCurrency } from '../../../utils/formatters';
import { POPULAR_PROMPTS } from '../../../constants/config';
import Badge from '../../../components/ui/Badge';

export default function SemanticSearchBar({ isOpen, onClose }) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [hasSearched, setHasSearched] = useState(false);
  const inputRef = useRef(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (isOpen) {
      setTimeout(() => inputRef.current?.focus(), 50);
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
      setQuery('');
      setResults([]);
      setHasSearched(false);
    }
  }, [isOpen]);

  // Debounced semantic search
  useEffect(() => {
    if (!query.trim() || query.trim().length < 2) {
      setResults([]);
      setHasSearched(false);
      return;
    }

    const timer = setTimeout(async () => {
      setIsLoading(true);
      try {
        const data = await productApi.semanticSearch(query.trim(), 6);
        setResults(data || []);
        setHasSearched(true);
      } catch (err) {
        console.error('Semantic search failed', err);
        setResults([]);
      } finally {
        setIsLoading(false);
      }
    }, 350);

    return () => clearTimeout(timer);
  }, [query]);

  const handleSelectProduct = (product) => {
    onClose();
    navigate(`/products/${product.slug || product.id}`);
  };

  const handlePromptClick = (prompt) => {
    setQuery(prompt);
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Escape') {
      onClose();
    }
  };

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center pt-16 sm:pt-24 px-4 bg-ink/60 backdrop-blur-md animate-fade-in"
      onClick={onClose}
      onKeyDown={handleKeyDown}
    >
      <div
        className="relative bg-white rounded-3xl shadow-2xl border border-surface-subtle w-full max-w-3xl overflow-hidden animate-slide-up"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Search Header */}
        <div className="flex items-center px-6 py-4 border-b border-surface-subtle gap-3 bg-surface-soft/40">
          <div className="w-9 h-9 rounded-xl bg-indigo-50 flex items-center justify-center text-accent-indigo shrink-0">
            <Sparkles className="w-5 h-5" />
          </div>
          <div className="relative flex-1">
            <input
              ref={inputRef}
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Search by keywords or describe what you need (e.g. 'laptop for university with long battery')..."
              className="w-full bg-transparent text-ink text-base sm:text-lg placeholder:text-ink-subtle focus:outline-none pr-8 font-medium"
            />
          </div>
          {isLoading ? (
            <Loader2 className="w-5 h-5 text-accent-indigo animate-spin shrink-0" />
          ) : query ? (
            <button
              onClick={() => setQuery('')}
              className="text-ink-subtle hover:text-ink p-1 rounded-full hover:bg-surface-muted"
            >
              <X className="w-5 h-5" />
            </button>
          ) : (
            <button
              onClick={onClose}
              className="text-xs font-semibold text-ink-subtle hover:text-ink px-2.5 py-1 rounded-lg border border-surface-subtle"
            >
              ESC
            </button>
          )}
        </div>

        {/* Search Body */}
        <div className="p-6 max-h-[60vh] overflow-y-auto space-y-6">
          {/* Active Semantic Results */}
          {results.length > 0 && (
            <div>
              <div className="flex items-center justify-between mb-3">
                <span className="text-xs font-bold uppercase tracking-wider text-ink-muted flex items-center gap-1.5">
                  <Sparkles className="w-3.5 h-3.5 text-accent-indigo" />
                  Semantic AI Matches ({results.length})
                </span>
                <span className="text-xs text-ink-subtle">Vector search</span>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {results.map((product) => (
                  <div
                    key={product.id}
                    onClick={() => handleSelectProduct(product)}
                    className="flex items-center gap-4 p-3 rounded-2xl border border-surface-subtle hover:border-ink hover:bg-surface-soft/60 cursor-pointer transition-all duration-200 group"
                  >
                    <div className="w-16 h-16 rounded-xl bg-surface-muted overflow-hidden shrink-0 flex items-center justify-center p-1">
                      {product.primaryImageUrl ? (
                        <img
                          src={product.primaryImageUrl}
                          alt={product.name}
                          className="w-full h-full object-contain group-hover:scale-105 transition-transform"
                        />
                      ) : (
                        <div className="w-full h-full bg-slate-200 rounded-lg flex items-center justify-center text-xs font-bold text-slate-500">
                          SPOCS
                        </div>
                      )}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-xs font-semibold text-accent-indigo uppercase tracking-wider">
                        {product.categoryName}
                      </p>
                      <h4 className="text-sm font-bold text-ink truncate group-hover:text-accent-indigo transition-colors">
                        {product.name}
                      </h4>
                      <p className="text-sm font-extrabold text-ink mt-0.5">
                        {formatCurrency(product.basePrice)}
                      </p>
                    </div>
                    <ArrowRight className="w-4 h-4 text-ink-subtle opacity-0 group-hover:opacity-100 transition-opacity" />
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* No results found state */}
          {hasSearched && results.length === 0 && !isLoading && (
            <div className="text-center py-10 space-y-2">
              <p className="text-sm font-bold text-ink">No exact matches found for "{query}"</p>
              <p className="text-xs text-ink-muted">
                Try describing features like "gaming laptop under 1000" or "wireless noise cancelling"
              </p>
            </div>
          )}

          {/* Prompt Recommendations when not searched */}
          {!query && (
            <div className="space-y-4">
              <div>
                <span className="text-xs font-bold uppercase tracking-wider text-ink-muted">
                  Popular Natural Language Queries
                </span>
                <div className="flex flex-wrap gap-2 mt-3">
                  {POPULAR_PROMPTS.map((prompt, i) => (
                    <button
                      key={i}
                      onClick={() => handlePromptClick(prompt)}
                      className="text-xs text-left px-3.5 py-2 rounded-xl bg-surface-muted hover:bg-slate-200 text-ink font-medium transition-all flex items-center gap-2 group"
                    >
                      <Sparkles className="w-3.5 h-3.5 text-accent-indigo group-hover:scale-110 transition-transform shrink-0" />
                      <span>{prompt}</span>
                    </button>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Footer info */}
        <div className="px-6 py-3 bg-surface-soft border-t border-surface-subtle flex items-center justify-between text-xs text-ink-muted">
          <span className="flex items-center gap-1.5">
            <span className="w-2 h-2 rounded-full bg-emerald-500" />
            AI Embedding Model: text-embedding-3-small
          </span>
          <span className="flex items-center gap-1">
            <CornerDownLeft className="w-3 h-3" /> Select result
          </span>
        </div>
      </div>
    </div>
  );
}
