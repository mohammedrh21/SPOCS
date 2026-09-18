import React from 'react';
import { formatCurrency } from '../../../utils/formatters';
import { Check } from 'lucide-react';

export default function ProductVariantSelector({
  variants = [],
  selectedVariant,
  onSelectVariant,
  basePrice = 0,
}) {
  if (!variants || variants.length === 0) return null;

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <label className="text-xs font-bold uppercase tracking-wider text-ink">
          Select Configuration ({variants.length} Options)
        </label>
        {selectedVariant && (
          <span className="text-xs font-mono text-ink-muted">
            SKU: {selectedVariant.sku}
          </span>
        )}
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
        {variants.map((variant) => {
          const isSelected = selectedVariant?.id === variant.id;
          const label = variant.variantOptions && variant.variantOptions.length > 0
            ? variant.variantOptions.join(' • ')
            : variant.sku;

          return (
            <button
              key={variant.id}
              type="button"
              onClick={() => onSelectVariant(variant)}
              className={`p-3.5 rounded-2xl border text-left transition-all duration-200 flex items-center justify-between gap-3 ${
                isSelected
                  ? 'border-ink bg-surface-soft ring-1 ring-ink'
                  : 'border-surface-subtle bg-white hover:border-slate-400'
              }`}
            >
              <div className="min-w-0 space-y-0.5">
                <p className="text-xs font-bold text-ink truncate">{label}</p>
                <p className="text-[11px] text-ink-muted">
                  {variant.stockQuantity > 0 ? (
                    <span className="text-emerald-600 font-semibold">In Stock ({variant.stockQuantity})</span>
                  ) : (
                    <span className="text-rose-500 font-semibold">Out of Stock</span>
                  )}
                </p>
              </div>

              <div className="flex items-center gap-2 shrink-0">
                <span className="text-xs font-extrabold text-ink">
                  {formatCurrency(variant.price)}
                </span>
                {isSelected && (
                  <div className="w-5 h-5 rounded-full bg-ink text-white flex items-center justify-center">
                    <Check className="w-3 h-3" />
                  </div>
                )}
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
}
