import React from 'react';
import ProductCard from './ProductCard';
import { ProductCardSkeleton } from '../../../components/ui/Skeleton';
import Button from '../../../components/ui/Button';
import { Sparkles, PackageSearch } from 'lucide-react';

export default function ProductGrid({
  products = [],
  isLoading = false,
  onQuickView,
  onResetFilters,
  columns = 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4',
}) {
  if (isLoading) {
    return (
      <div className={`grid ${columns} gap-6`}>
        {Array.from({ length: 8 }).map((_, i) => (
          <ProductCardSkeleton key={i} />
        ))}
      </div>
    );
  }

  if (products.length === 0) {
    return (
      <div className="bg-white rounded-3xl border border-surface-subtle p-12 text-center space-y-4 shadow-card">
        <div className="w-16 h-16 rounded-2xl bg-surface-muted flex items-center justify-center mx-auto text-ink-subtle">
          <PackageSearch className="w-8 h-8" />
        </div>
        <div className="space-y-1">
          <h3 className="text-lg font-bold text-ink">No products found</h3>
          <p className="text-xs text-ink-muted max-w-sm mx-auto">
            We couldn't find any products matching your current filters or query.
          </p>
        </div>
        {onResetFilters && (
          <Button
            variant="outline"
            size="sm"
            onClick={onResetFilters}
            className="uppercase font-bold text-xs"
          >
            Clear All Filters
          </Button>
        )}
      </div>
    );
  }

  return (
    <div className={`grid ${columns} gap-6`}>
      {products.map((product) => (
        <ProductCard
          key={product.id}
          product={product}
          onQuickView={onQuickView}
        />
      ))}
    </div>
  );
}
