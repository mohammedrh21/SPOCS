import React from 'react';
import { Filter, X, Check, RotateCcw } from 'lucide-react';
import Button from '../../../components/ui/Button';

export default function ProductFilterSidebar({
  categories = [],
  selectedCategory,
  onSelectCategory,
  minPrice,
  maxPrice,
  onPriceChange,
  inStockOnly,
  onInStockChange,
  onResetFilters,
  className,
}) {
  const priceRanges = [
    { label: 'All Prices', min: '', max: '' },
    { label: 'Under $200', min: '', max: 200 },
    { label: '$200 to $500', min: 200, max: 500 },
    { label: '$500 to $1,000', min: 500, max: 1000 },
    { label: '$1,000 & Above', min: 1000, max: '' },
  ];

  return (
    <div className={`space-y-6 bg-white rounded-3xl border border-surface-subtle p-6 shadow-card ${className}`}>
      {/* Filter Header */}
      <div className="flex items-center justify-between pb-4 border-b border-surface-subtle">
        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-ink" />
          <h3 className="text-sm font-bold uppercase tracking-wider text-ink">Filters</h3>
        </div>
        <button
          type="button"
          onClick={onResetFilters}
          className="text-xs font-semibold text-ink-muted hover:text-accent-indigo flex items-center gap-1 transition-colors"
          title="Reset all filters"
        >
          <RotateCcw className="w-3 h-3" />
          <span>Reset</span>
        </button>
      </div>

      {/* Category Filter */}
      <div className="space-y-3">
        <h4 className="text-xs font-bold uppercase tracking-wider text-ink-muted">Categories</h4>
        <div className="space-y-1">
          <button
            type="button"
            onClick={() => onSelectCategory(null)}
            className={`w-full text-left px-3 py-2 rounded-xl text-xs font-semibold transition-all flex items-center justify-between ${
              !selectedCategory
                ? 'bg-ink text-white shadow-xs'
                : 'text-ink-muted hover:bg-surface-muted hover:text-ink'
            }`}
          >
            <span>All Products</span>
            {!selectedCategory && <Check className="w-3.5 h-3.5" />}
          </button>

          {categories.map((cat) => {
            const isSelected = selectedCategory === cat.slug || selectedCategory === cat.id;
            return (
              <button
                key={cat.id}
                type="button"
                onClick={() => onSelectCategory(cat.slug)}
                className={`w-full text-left px-3 py-2 rounded-xl text-xs font-semibold transition-all flex items-center justify-between ${
                  isSelected
                    ? 'bg-ink text-white shadow-xs'
                    : 'text-ink-muted hover:bg-surface-muted hover:text-ink'
                }`}
              >
                <span className="truncate">{cat.name}</span>
                {cat.productCount !== undefined && (
                  <span className={`text-[10px] px-1.5 py-0.5 rounded-full ${isSelected ? 'bg-slate-700 text-white' : 'bg-surface-muted text-ink-subtle'}`}>
                    {cat.productCount}
                  </span>
                )}
              </button>
            );
          })}
        </div>
      </div>

      {/* Quick Price Ranges */}
      <div className="space-y-3 pt-4 border-t border-surface-subtle">
        <h4 className="text-xs font-bold uppercase tracking-wider text-ink-muted">Shop by Price</h4>
        <div className="space-y-1">
          {priceRanges.map((range, idx) => {
            const isSelected = minPrice === range.min && maxPrice === range.max;
            return (
              <button
                key={idx}
                type="button"
                onClick={() => onPriceChange(range.min, range.max)}
                className={`w-full text-left px-3 py-2 rounded-xl text-xs font-medium transition-colors ${
                  isSelected
                    ? 'bg-surface-muted font-bold text-ink border border-surface-subtle'
                    : 'text-ink-muted hover:text-ink hover:bg-surface-soft'
                }`}
              >
                {range.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* Custom Price Range Inputs */}
      <div className="space-y-2 pt-2">
        <span className="text-[11px] font-semibold text-ink-subtle">Custom Range ($)</span>
        <div className="grid grid-cols-2 gap-2">
          <input
            type="number"
            placeholder="Min"
            value={minPrice}
            onChange={(e) => onPriceChange(e.target.value, maxPrice)}
            className="w-full bg-surface-soft border border-surface-subtle rounded-xl px-3 py-1.5 text-xs text-ink placeholder:text-ink-subtle focus:outline-none focus:border-ink"
          />
          <input
            type="number"
            placeholder="Max"
            value={maxPrice}
            onChange={(e) => onPriceChange(minPrice, e.target.value)}
            className="w-full bg-surface-soft border border-surface-subtle rounded-xl px-3 py-1.5 text-xs text-ink placeholder:text-ink-subtle focus:outline-none focus:border-ink"
          />
        </div>
      </div>

      {/* In-Stock Toggle */}
      <div className="pt-4 border-t border-surface-subtle">
        <label className="flex items-center gap-3 cursor-pointer select-none">
          <input
            type="checkbox"
            checked={inStockOnly}
            onChange={(e) => onInStockChange(e.target.checked)}
            className="w-4 h-4 rounded-md border-surface-subtle text-ink focus:ring-ink"
          />
          <span className="text-xs font-semibold text-ink">In Stock Items Only</span>
        </label>
      </div>
    </div>
  );
}
