import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ShoppingBag, Plus, Minus, Trash2, ArrowRight, Truck, ShieldCheck } from 'lucide-react';
import Button from '../../../components/ui/Button';
import { useCart } from '../../../app/providers/CartContext';
import { formatCurrency } from '../../../utils/formatters';

const FREE_SHIPPING_THRESHOLD = 75;

export default function CartPage() {
  const { items, subtotal, totalItemsCount, updateQuantity, removeItem, clearCart, isLoading } = useCart();
  const navigate = useNavigate();

  const progressToFreeShipping = Math.min(100, (subtotal / FREE_SHIPPING_THRESHOLD) * 100);
  const remainingForFreeShipping = Math.max(0, FREE_SHIPPING_THRESHOLD - subtotal);
  const shippingFee = remainingForFreeShipping === 0 ? 0 : 9.99;
  const grandTotal = subtotal + shippingFee;

  if (items.length === 0) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 text-center space-y-6">
        <div className="w-20 h-20 rounded-full bg-surface-muted flex items-center justify-center mx-auto text-ink-subtle">
          <ShoppingBag className="w-10 h-10" />
        </div>
        <div className="space-y-2">
          <h1 className="nike-heading text-3xl sm:text-4xl text-ink">YOUR BAG IS EMPTY</h1>
          <p className="text-xs sm:text-sm text-ink-muted max-w-sm mx-auto">
            Once you add items from our catalog or recommendations, they will appear here.
          </p>
        </div>
        <Button
          variant="nike-dark"
          size="lg"
          onClick={() => navigate('/products')}
          className="uppercase font-bold tracking-wider text-xs"
        >
          Explore Catalog
        </Button>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12 space-y-8">
      {/* Title */}
      <div className="flex items-center justify-between pb-4 border-b border-surface-subtle">
        <div>
          <h1 className="nike-heading text-3xl sm:text-5xl text-ink tracking-tight">
            YOUR BAG
          </h1>
          <p className="text-xs font-semibold text-ink-muted">
            {totalItemsCount} {totalItemsCount === 1 ? 'item' : 'items'} in your cart
          </p>
        </div>
        <button
          type="button"
          onClick={clearCart}
          className="text-xs font-semibold text-ink-subtle hover:text-rose-600 transition-colors"
        >
          Clear Bag
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-12 items-start">
        {/* Left: Cart Items (7 Cols) */}
        <div className="lg:col-span-8 space-y-4">
          {/* Free Shipping Alert */}
          <div className="bg-white p-4 rounded-2xl border border-surface-subtle space-y-2 shadow-card">
            <div className="flex items-center justify-between text-xs font-semibold">
              <span className="flex items-center gap-2 text-ink">
                <Truck className="w-4 h-4 text-accent-orange" />
                {remainingForFreeShipping === 0
                  ? 'Congratulations! You unlocked Free Delivery.'
                  : `Add ${formatCurrency(remainingForFreeShipping)} more to qualify for Free Shipping.`}
              </span>
              <span className="text-ink-muted">{Math.round(progressToFreeShipping)}%</span>
            </div>
            <div className="w-full bg-surface-muted h-2 rounded-full overflow-hidden">
              <div
                className="bg-accent-orange h-full rounded-full transition-all duration-300"
                style={{ width: `${progressToFreeShipping}%` }}
              />
            </div>
          </div>

          {/* Items List */}
          <div className="bg-white rounded-3xl border border-surface-subtle divide-y divide-surface-subtle shadow-card overflow-hidden">
            {items.map((item) => (
              <div key={item.id} className="p-6 flex flex-col sm:flex-row items-start sm:items-center gap-6">
                {/* Image */}
                <div className="w-24 h-24 rounded-2xl bg-surface-muted p-2 flex items-center justify-center shrink-0 border border-surface-subtle">
                  {item.productPrimaryImageUrl ? (
                    <img
                      src={item.productPrimaryImageUrl}
                      alt={item.productName}
                      className="w-full h-full object-contain"
                    />
                  ) : (
                    <div className="text-xs font-bold text-slate-400">SPOCS</div>
                  )}
                </div>

                {/* Details */}
                <div className="flex-1 min-w-0 space-y-1">
                  <h3 className="text-base font-bold text-ink truncate">
                    {item.productName}
                  </h3>
                  {item.variantDescription && (
                    <p className="text-xs text-ink-muted">{item.variantDescription}</p>
                  )}
                  <p className="text-xs font-extrabold text-ink">
                    {formatCurrency(item.unitPrice)}
                  </p>
                </div>

                {/* Stepper */}
                <div className="flex items-center gap-4 w-full sm:w-auto justify-between sm:justify-end">
                  <div className="flex items-center border border-surface-subtle rounded-full bg-surface-soft overflow-hidden">
                    <button
                      type="button"
                      disabled={isLoading}
                      onClick={() => updateQuantity(item.id, item.quantity - 1)}
                      className="p-1.5 px-3 text-ink-muted hover:text-ink disabled:opacity-40"
                    >
                      <Minus className="w-3.5 h-3.5" />
                    </button>
                    <span className="text-xs font-bold px-2 min-w-[24px] text-center text-ink">
                      {item.quantity}
                    </span>
                    <button
                      type="button"
                      disabled={isLoading}
                      onClick={() => updateQuantity(item.id, item.quantity + 1)}
                      className="p-1.5 px-3 text-ink-muted hover:text-ink disabled:opacity-40"
                    >
                      <Plus className="w-3.5 h-3.5" />
                    </button>
                  </div>

                  <span className="text-base font-black text-ink min-w-[80px] text-right">
                    {formatCurrency(item.unitPrice * item.quantity)}
                  </span>

                  <button
                    type="button"
                    onClick={() => removeItem(item.id)}
                    className="text-ink-subtle hover:text-rose-600 p-2 transition-colors"
                    title="Remove item"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Right: Order Summary (4 Cols) */}
        <div className="lg:col-span-4 bg-white rounded-3xl border border-surface-subtle p-6 sm:p-8 space-y-6 shadow-card sticky top-24">
          <h3 className="nike-heading text-xl text-ink tracking-tight">
            SUMMARY
          </h3>

          <div className="space-y-3 text-xs sm:text-sm">
            <div className="flex justify-between text-ink-muted">
              <span>Subtotal</span>
              <span className="font-bold text-ink">{formatCurrency(subtotal)}</span>
            </div>
            <div className="flex justify-between text-ink-muted">
              <span>Estimated Shipping</span>
              <span className="font-semibold text-emerald-600">
                {shippingFee === 0 ? 'FREE' : formatCurrency(shippingFee)}
              </span>
            </div>
            <div className="flex justify-between text-base sm:text-lg font-black text-ink pt-4 border-t border-surface-subtle">
              <span>Total</span>
              <span>{formatCurrency(grandTotal)}</span>
            </div>
          </div>

          <Button
            variant="nike-dark"
            size="lg"
            onClick={() => navigate('/checkout')}
            className="w-full uppercase font-bold tracking-wider text-sm h-14 flex items-center justify-center gap-2 shadow-lg"
          >
            <span>Proceed to Checkout</span>
            <ArrowRight className="w-4 h-4" />
          </Button>

          <div className="space-y-2 pt-2 border-t border-surface-subtle text-xs text-ink-muted">
            <div className="flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-emerald-600 shrink-0" />
              <span>Secure Encrypted Checkout</span>
            </div>
            <div className="flex items-center gap-2">
              <Truck className="w-4 h-4 text-accent-orange shrink-0" />
              <span>Tracked Express Delivery</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
