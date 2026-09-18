import React from 'react';
import { useNavigate } from 'react-router-dom';
import { ShoppingBag, Plus, Minus, Trash2, ArrowRight, Truck } from 'lucide-react';
import Drawer from '../../../components/ui/Drawer';
import Button from '../../../components/ui/Button';
import { useCart } from '../../../app/providers/CartContext';
import { formatCurrency } from '../../../utils/formatters';

const FREE_SHIPPING_THRESHOLD = 75;

export default function CartDrawer() {
  const { isCartOpen, closeCart, items, subtotal, updateQuantity, removeItem, clearCart, isLoading } = useCart();
  const navigate = useNavigate();

  const progressToFreeShipping = Math.min(100, (subtotal / FREE_SHIPPING_THRESHOLD) * 100);
  const remainingForFreeShipping = Math.max(0, FREE_SHIPPING_THRESHOLD - subtotal);

  const handleCheckoutClick = () => {
    closeCart();
    navigate('/checkout');
  };

  const handleViewCartClick = () => {
    closeCart();
    navigate('/cart');
  };

  return (
    <Drawer
      isOpen={isCartOpen}
      onClose={closeCart}
      title="Shopping Bag"
      subtitle={`${items.length} ${items.length === 1 ? 'item' : 'items'}`}
      icon={ShoppingBag}
      footer={
        items.length > 0 ? (
          <div className="space-y-4">
            {/* Free shipping banner progress */}
            <div className="bg-surface-muted p-3 rounded-2xl space-y-1.5 border border-surface-subtle">
              <div className="flex items-center justify-between text-xs font-semibold">
                <span className="flex items-center gap-1.5 text-ink">
                  <Truck className="w-3.5 h-3.5 text-accent-orange" />
                  {remainingForFreeShipping === 0 ? 'Free Shipping Unlocked!' : `Add ${formatCurrency(remainingForFreeShipping)} for Free Shipping`}
                </span>
                <span className="text-ink-muted">{Math.round(progressToFreeShipping)}%</span>
              </div>
              <div className="w-full bg-slate-200 h-1.5 rounded-full overflow-hidden">
                <div
                  className="bg-accent-orange h-full rounded-full transition-all duration-300"
                  style={{ width: `${progressToFreeShipping}%` }}
                />
              </div>
            </div>

            {/* Price breakdown */}
            <div className="space-y-1.5 text-xs">
              <div className="flex justify-between text-ink-muted">
                <span>Subtotal</span>
                <span className="font-bold text-ink">{formatCurrency(subtotal)}</span>
              </div>
              <div className="flex justify-between text-ink-muted">
                <span>Shipping</span>
                <span className="font-semibold text-emerald-600">
                  {remainingForFreeShipping === 0 ? 'FREE' : '$9.99'}
                </span>
              </div>
              <div className="flex justify-between text-sm font-extrabold text-ink pt-2 border-t border-surface-subtle">
                <span>Total</span>
                <span>{formatCurrency(subtotal + (remainingForFreeShipping === 0 ? 0 : 9.99))}</span>
              </div>
            </div>

            {/* Actions */}
            <div className="space-y-2">
              <Button
                variant="nike-dark"
                size="lg"
                onClick={handleCheckoutClick}
                className="w-full uppercase font-bold tracking-wider text-sm flex items-center justify-center gap-2"
              >
                <span>Checkout</span>
                <ArrowRight className="w-4 h-4" />
              </Button>
              <Button
                variant="outline"
                size="md"
                onClick={handleViewCartClick}
                className="w-full text-xs font-bold uppercase tracking-wider"
              >
                View Full Bag
              </Button>
            </div>
          </div>
        ) : null
      }
    >
      {items.length === 0 ? (
        <div className="h-full flex flex-col items-center justify-center text-center py-16 space-y-4">
          <div className="w-16 h-16 rounded-full bg-surface-muted flex items-center justify-center text-ink-subtle">
            <ShoppingBag className="w-8 h-8" />
          </div>
          <div className="space-y-1">
            <h3 className="text-base font-bold text-ink">Your bag is empty</h3>
            <p className="text-xs text-ink-muted max-w-xs">
              Items you add from the catalog or AI recommendations will appear here.
            </p>
          </div>
          <Button
            variant="nike-dark"
            size="md"
            onClick={() => {
              closeCart();
              navigate('/products');
            }}
            className="uppercase font-bold tracking-wider text-xs"
          >
            Explore Products
          </Button>
        </div>
      ) : (
        <div className="divide-y divide-surface-subtle">
          {items.map((item) => (
            <div key={item.id} className="py-4 flex gap-4 group">
              {/* Product Thumbnail */}
              <div className="w-20 h-20 rounded-xl bg-surface-muted overflow-hidden shrink-0 p-1 flex items-center justify-center border border-surface-subtle">
                {item.productPrimaryImageUrl ? (
                  <img
                    src={item.productPrimaryImageUrl}
                    alt={item.productName}
                    className="w-full h-full object-contain"
                  />
                ) : (
                  <div className="w-full h-full bg-slate-200 rounded-lg flex items-center justify-center text-[10px] font-bold text-slate-400">
                    SPOCS
                  </div>
                )}
              </div>

              {/* Item Info */}
              <div className="flex-1 min-w-0 space-y-1">
                <h4 className="text-sm font-bold text-ink leading-snug line-clamp-1">
                  {item.productName}
                </h4>
                {item.variantDescription && (
                  <p className="text-xs text-ink-muted">{item.variantDescription}</p>
                )}
                <p className="text-xs font-bold text-ink">{formatCurrency(item.unitPrice)}</p>

                {/* Stepper + Delete */}
                <div className="flex items-center justify-between pt-2">
                  <div className="flex items-center border border-surface-subtle rounded-full bg-surface-soft overflow-hidden">
                    <button
                      type="button"
                      disabled={isLoading}
                      onClick={() => updateQuantity(item.id, item.quantity - 1)}
                      className="p-1 px-2 text-ink-muted hover:text-ink disabled:opacity-40"
                    >
                      <Minus className="w-3 h-3" />
                    </button>
                    <span className="text-xs font-bold px-2 min-w-[20px] text-center text-ink">
                      {item.quantity}
                    </span>
                    <button
                      type="button"
                      disabled={isLoading}
                      onClick={() => updateQuantity(item.id, item.quantity + 1)}
                      className="p-1 px-2 text-ink-muted hover:text-ink disabled:opacity-40"
                    >
                      <Plus className="w-3 h-3" />
                    </button>
                  </div>

                  <button
                    type="button"
                    onClick={() => removeItem(item.id)}
                    className="text-ink-subtle hover:text-rose-600 p-1.5 transition-colors"
                    title="Remove item"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </Drawer>
  );
}
