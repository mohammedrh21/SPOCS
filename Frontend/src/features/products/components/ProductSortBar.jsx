import React from 'react';
import { ArrowUpDown, SlidersHorizontal } from 'lucide-react';
import Select from '../../../components/ui/Select';

export default function ProductSortBar({
  totalCount = 0,
  sortBy,
  onSortChange,
  onToggleMobileFilter,
}) {
  const sortOptions = [
    { value: '', label: 'Featured & Relevant' },
    { value: 'price_asc', label: 'Price: Low to High' },
    { value: 'price_desc', label: 'Price: High to Low' },
    { value: 'newest', label: 'Newest Arrivals' },
    { value: 'name_asc', label: 'Alphabetical: A-Z' },
  ];

  return (
    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-4 rounded-2xl border border-surface-subtle shadow-card">
      <div className="flex items-center justify-between sm:justify-start gap-4">
        <p className="text-xs sm:text-sm font-semibold text-ink-muted">
          Showing <span className="font-bold text-ink">{totalCount}</span> {totalCount === 1 ? 'Product' : 'Products'}
        </p>

        {/* Mobile filter toggle trigger */}
        {onToggleMobileFilter && (
          <button
            type="button"
            onClick={onToggleMobileFilter}
            className="lg:hidden flex items-center gap-1.5 px-3 py-1.5 rounded-full border border-surface-subtle text-xs font-semibold text-ink hover:bg-surface-muted transition-colors"
          >
            <SlidersHorizontal className="w-3.5 h-3.5" />
            <span>Filters</span>
          </button>
        )}
      </div>

      <div className="flex items-center gap-2">
        <span className="text-xs font-semibold text-ink-subtle hidden sm:inline">Sort By:</span>
        <select
          value={sortBy}
          onChange={(e) => onSortChange(e.target.value)}
          className="bg-surface-soft border border-surface-subtle text-ink text-xs font-semibold rounded-xl px-3 py-2 pr-8 focus:outline-none focus:border-ink cursor-pointer"
        >
          {sortOptions.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}
