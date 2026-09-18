import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { ShoppingBag, Eye, Sparkles } from 'lucide-react';
import { formatCurrency } from '../../../utils/formatters';
import { useCart } from '../../../app/providers/CartContext';
import { useAiChat } from '../../../app/providers/AiChatContext';
import Badge from '../../../components/ui/Badge';
import Button from '../../../components/ui/Button';

export default function ProductCard({ product, onQuickView }) {
  const { addToCart } = useCart();
  const { askAboutProduct } = useAiChat();
  const [isHovered, setIsHovered] = useState(false);

  const handleQuickAdd = (e) => {
    e.preventDefault();
    e.stopPropagation();
    addToCart(product, null, 1);
  };

  const handleAskAi = (e) => {
    e.preventDefault();
    e.stopPropagation();
    askAboutProduct(product);
  };

  return (
    <div
      className="group relative bg-white rounded-3xl border border-surface-subtle shadow-card hover:shadow-card-hover transition-all duration-300 flex flex-col overflow-hidden"
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {/* Top Image Container */}
      <Link
        to={`/products/${product.slug || product.id}`}
        className="relative block w-full aspect-square bg-surface-muted/60 p-6 overflow-hidden"
      >
        {/* Badges */}
        <div className="absolute top-4 left-4 z-10 flex flex-col gap-1.5 items-start">
          {product.variantCount > 1 && (
            <Badge variant="default" size="sm">
              {product.variantCount} Options
            </Badge>
          )}
          {product.inStock ? (
            <Badge variant="success" size="sm">
              In Stock
            </Badge>
          ) : (
            <Badge variant="danger" size="sm">
              Out of Stock
            </Badge>
          )}
        </div>

        {/* AI Inquire Badge Button on Image */}
        <button
          onClick={handleAskAi}
          className="absolute top-4 right-4 z-10 w-8 h-8 rounded-full bg-white/90 backdrop-blur-sm border border-surface-subtle text-accent-indigo hover:text-indigo-800 hover:bg-white flex items-center justify-center shadow-xs transition-all opacity-0 group-hover:opacity-100"
          title="Ask AI about this item"
        >
          <Sparkles className="w-4 h-4" />
        </button>

        {/* Product Image */}
        <div className="w-full h-full flex items-center justify-center">
          {product.primaryImageUrl ? (
            <img
              src={product.primaryImageUrl}
              alt={product.name}
              className="w-full h-full object-contain mix-blend-multiply transition-transform duration-500 group-hover:scale-105"
              loading="lazy"
            />
          ) : (
            <div className="w-full h-full bg-surface-subtle/50 rounded-2xl flex items-center justify-center text-xs font-bold text-slate-400">
              SPOCS
            </div>
          )}
        </div>

        {/* Quick Action Floating Bar on Hover */}
        <div className="absolute inset-x-4 bottom-4 z-10 flex items-center gap-2 translate-y-4 opacity-0 group-hover:translate-y-0 group-hover:opacity-100 transition-all duration-300">
          <Button
            variant="nike-dark"
            size="sm"
            onClick={handleQuickAdd}
            className="flex-1 shadow-md text-xs font-bold uppercase tracking-wider h-9"
          >
            <ShoppingBag className="w-3.5 h-3.5" />
            <span>Add to Bag</span>
          </Button>
          {onQuickView && (
            <button
              onClick={(e) => {
                e.preventDefault();
                e.stopPropagation();
                onQuickView(product);
              }}
              className="h-9 w-9 rounded-full bg-white border border-surface-subtle text-ink hover:bg-slate-100 flex items-center justify-center shadow-md transition-colors"
              title="Quick View"
            >
              <Eye className="w-4 h-4" />
            </button>
          )}
        </div>
      </Link>

      {/* Product Information */}
      <div className="p-5 flex-1 flex flex-col justify-between space-y-2 bg-white">
        <div className="space-y-1">
          <p className="text-[11px] font-bold text-accent-indigo uppercase tracking-wider">
            {product.categoryName || 'Tech Gear'}
          </p>
          <Link
            to={`/products/${product.slug || product.id}`}
            className="block text-sm sm:text-base font-bold text-ink hover:text-accent-indigo transition-colors line-clamp-1"
          >
            {product.name}
          </Link>
          {product.shortDescription && (
            <p className="text-xs text-ink-muted line-clamp-2 leading-relaxed">
              {product.shortDescription}
            </p>
          )}
        </div>

        {/* Price & SKU */}
        <div className="pt-2 flex items-center justify-between border-t border-surface-subtle">
          <div>
            <span className="text-base sm:text-lg font-extrabold text-ink tracking-tight">
              {formatCurrency(product.basePrice)}
            </span>
          </div>
          <span className="text-[10px] text-ink-subtle font-mono uppercase">
            {product.sku}
          </span>
        </div>
      </div>
    </div>
  );
}
