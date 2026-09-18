import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { ShieldCheck, Truck, CreditCard, Lock, CheckCircle2, ArrowLeft, ArrowRight, Loader2, Package } from 'lucide-react';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import { useAuth } from '../../../app/providers/AuthContext';
import { useCart } from '../../../app/providers/CartContext';
import { useToast } from '../../../app/providers/ToastContext';
import { orderApi } from '../api/orderApi';
import { formatCurrency } from '../../../utils/formatters';

export default function CheckoutPage() {
  const { user, isAuthenticated, openAuthModal } = useAuth();
  const { items, subtotal, clearCart } = useCart();
  const toast = useToast();
  const navigate = useNavigate();

  const [shippingAddress, setShippingAddress] = useState(user?.address || '123 Tech Avenue');
  const [city, setCity] = useState(user?.city || 'San Francisco');
  const [postalCode, setPostalCode] = useState(user?.postalCode || '94105');
  const [country, setCountry] = useState(user?.country || 'USA');
  const [notes, setNotes] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('card');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [completedOrder, setCompletedOrder] = useState(null);

  if (!isAuthenticated) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-20 text-center space-y-6">
        <div className="w-16 h-16 rounded-full bg-indigo-50 text-accent-indigo flex items-center justify-center mx-auto">
          <Lock className="w-8 h-8" />
        </div>
        <div className="space-y-2">
          <h2 className="nike-heading text-3xl text-ink">SIGN IN TO CHECKOUT</h2>
          <p className="text-xs sm:text-sm text-ink-muted">
            Please log in or register an account to finalize your order securely.
          </p>
        </div>
        <Button
          variant="nike-dark"
          size="lg"
          onClick={() => openAuthModal('login')}
          className="uppercase font-bold tracking-wider text-xs"
        >
          Sign In Now
        </Button>
      </div>
    );
  }

  if (completedOrder) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-16 text-center space-y-6 animate-slide-up">
        <div className="w-20 h-20 rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center mx-auto shadow-lg shadow-emerald-500/10">
          <CheckCircle2 className="w-10 h-10" />
        </div>

        <div className="space-y-2">
          <p className="text-xs font-bold text-accent-indigo uppercase tracking-wider">
            Order Confirmed
          </p>
          <h1 className="nike-heading text-3xl sm:text-5xl text-ink">
            THANK YOU FOR YOUR ORDER!
          </h1>
          <p className="text-xs sm:text-sm text-ink-muted">
            Order reference: <span className="font-mono font-bold text-ink">#{completedOrder.id}</span>
          </p>
        </div>

        <div className="p-6 bg-white rounded-3xl border border-surface-subtle shadow-card text-left space-y-4 max-w-md mx-auto">
          <div className="flex items-center justify-between pb-3 border-b border-surface-subtle text-xs">
            <span className="text-ink-muted">Destination</span>
            <span className="font-bold text-ink">{completedOrder.shippingAddress}, {completedOrder.city}</span>
          </div>
          <div className="flex items-center justify-between pb-3 border-b border-surface-subtle text-xs">
            <span className="text-ink-muted">Total Paid</span>
            <span className="font-black text-ink">{formatCurrency(completedOrder.totalAmount)}</span>
          </div>
          <div className="flex items-center justify-between text-xs">
            <span className="text-ink-muted">Status</span>
            <span className="font-bold text-emerald-600">{completedOrder.status || 'Processing'}</span>
          </div>
        </div>

        <div className="flex flex-col sm:flex-row items-center justify-center gap-3 pt-4">
          <Button
            variant="nike-dark"
            size="md"
            onClick={() => navigate('/orders')}
            className="w-full sm:w-auto uppercase font-bold tracking-wider text-xs"
          >
            View My Orders
          </Button>
          <Button
            variant="outline"
            size="md"
            onClick={() => navigate('/products')}
            className="w-full sm:w-auto uppercase font-bold tracking-wider text-xs"
          >
            Continue Shopping
          </Button>
        </div>
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-20 text-center space-y-6">
        <h2 className="nike-heading text-3xl text-ink">YOUR BAG IS EMPTY</h2>
        <p className="text-xs text-ink-muted">Add products before checking out.</p>
        <Button variant="nike-dark" size="md" onClick={() => navigate('/products')}>
          Go to Catalog
        </Button>
      </div>
    );
  }

  const shippingFee = subtotal >= 75 ? 0 : 9.99;
  const grandTotal = subtotal + shippingFee;

  const handleSubmitOrder = async (e) => {
    e.preventDefault();
    if (!shippingAddress || !city || !postalCode) {
      toast.error('Please complete all shipping address fields.');
      return;
    }

    setIsSubmitting(true);
    try {
      const order = await orderApi.createOrder({
        shippingAddress,
        city,
        postalCode,
        country,
        notes: notes || `Payment: ${paymentMethod.toUpperCase()}`,
      });

      setCompletedOrder(order);
      await clearCart();
      toast.success('Order placed successfully!');
    } catch (err) {
      console.error('Failed to create order', err);
      const msg = err.response?.data?.message || 'Order placement failed. Please try again.';
      toast.error(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12 space-y-8">
      {/* Title */}
      <div className="flex items-center gap-4">
        <Link
          to="/cart"
          className="p-2 rounded-full border border-surface-subtle hover:bg-surface-muted text-ink transition-colors"
          title="Back to Bag"
        >
          <ArrowLeft className="w-5 h-5" />
        </Link>
        <div>
          <h1 className="nike-heading text-3xl sm:text-4xl text-ink tracking-tight">
            CHECKOUT
          </h1>
          <p className="text-xs font-semibold text-ink-muted">Complete your purchase details</p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-12 items-start">
        {/* Left: Shipping & Payment Form (7 Cols) */}
        <div className="lg:col-span-7 space-y-8">
          <form onSubmit={handleSubmitOrder} className="space-y-8">
            {/* 1. Shipping Details */}
            <div className="bg-white rounded-3xl border border-surface-subtle p-6 sm:p-8 space-y-4 shadow-card">
              <div className="flex items-center gap-3 pb-3 border-b border-surface-subtle">
                <div className="w-8 h-8 rounded-xl bg-surface-muted flex items-center justify-center text-ink">
                  <Truck className="w-4 h-4" />
                </div>
                <h3 className="nike-heading text-lg text-ink">1. SHIPPING ADDRESS</h3>
              </div>

              <div className="space-y-4">
                <Input
                  label="Street Address"
                  placeholder="123 Tech Avenue"
                  value={shippingAddress}
                  onChange={(e) => setShippingAddress(e.target.value)}
                  required
                />
                <div className="grid grid-cols-2 gap-4">
                  <Input
                    label="City"
                    placeholder="San Francisco"
                    value={city}
                    onChange={(e) => setCity(e.target.value)}
                    required
                  />
                  <Input
                    label="Postal Code"
                    placeholder="94105"
                    value={postalCode}
                    onChange={(e) => setPostalCode(e.target.value)}
                    required
                  />
                </div>
                <Input
                  label="Country"
                  placeholder="United States"
                  value={country}
                  onChange={(e) => setCountry(e.target.value)}
                  required
                />
                <Input
                  label="Delivery Instructions (Optional)"
                  placeholder="Leave at front porch / apartment code..."
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                />
              </div>
            </div>

            {/* 2. Payment Method */}
            <div className="bg-white rounded-3xl border border-surface-subtle p-6 sm:p-8 space-y-4 shadow-card">
              <div className="flex items-center gap-3 pb-3 border-b border-surface-subtle">
                <div className="w-8 h-8 rounded-xl bg-surface-muted flex items-center justify-center text-ink">
                  <CreditCard className="w-4 h-4" />
                </div>
                <h3 className="nike-heading text-lg text-ink">2. PAYMENT METHOD</h3>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <button
                  type="button"
                  onClick={() => setPaymentMethod('card')}
                  className={`p-4 rounded-2xl border text-left transition-all ${
                    paymentMethod === 'card'
                      ? 'border-ink bg-surface-soft ring-1 ring-ink'
                      : 'border-surface-subtle bg-white hover:border-slate-400'
                  }`}
                >
                  <p className="text-xs font-bold text-ink">Credit / Debit Card</p>
                  <p className="text-[11px] text-ink-muted">Simulated instant demo checkout</p>
                </button>

                <button
                  type="button"
                  onClick={() => setPaymentMethod('cod')}
                  className={`p-4 rounded-2xl border text-left transition-all ${
                    paymentMethod === 'cod'
                      ? 'border-ink bg-surface-soft ring-1 ring-ink'
                      : 'border-surface-subtle bg-white hover:border-slate-400'
                  }`}
                >
                  <p className="text-xs font-bold text-ink">Cash On Delivery</p>
                  <p className="text-[11px] text-ink-muted">Pay upon arrival</p>
                </button>
              </div>

              <div className="p-3 bg-slate-50 border border-slate-200 rounded-xl flex items-center gap-2 text-xs text-ink-muted">
                <Lock className="w-4 h-4 text-emerald-600 shrink-0" />
                <span>End-to-end 256-bit encrypted test gateway active.</span>
              </div>
            </div>

            {/* Place Order CTA */}
            <Button
              type="submit"
              variant="orange"
              size="lg"
              isLoading={isSubmitting}
              className="w-full uppercase font-bold tracking-wider text-sm h-14 shadow-lg shadow-orange-500/20"
            >
              <span>Place Order ({formatCurrency(grandTotal)})</span>
              <ArrowRight className="w-4 h-4" />
            </Button>
          </form>
        </div>

        {/* Right: Order Summary Preview (5 Cols) */}
        <div className="lg:col-span-5 bg-white rounded-3xl border border-surface-subtle p-6 sm:p-8 space-y-6 shadow-card sticky top-24">
          <h3 className="nike-heading text-lg text-ink">
            IN YOUR BAG ({items.length})
          </h3>

          <div className="divide-y divide-surface-subtle max-h-80 overflow-y-auto pr-1 space-y-3">
            {items.map((item) => (
              <div key={item.id} className="pt-3 flex items-center justify-between gap-3">
                <div className="flex items-center gap-3 min-w-0">
                  {item.productPrimaryImageUrl ? (
                    <img
                      src={item.productPrimaryImageUrl}
                      alt={item.productName}
                      className="w-12 h-12 rounded-xl object-contain bg-surface-muted p-1 shrink-0"
                    />
                  ) : (
                    <div className="w-12 h-12 rounded-xl bg-surface-muted flex items-center justify-center text-xs font-bold text-slate-400 shrink-0">
                      SP
                    </div>
                  )}
                  <div className="truncate">
                    <p className="font-bold text-xs text-ink truncate">{item.productName}</p>
                    {item.variantDescription && (
                      <p className="text-[10px] text-ink-muted">{item.variantDescription}</p>
                    )}
                    <p className="text-xs text-ink-muted">Qty {item.quantity}</p>
                  </div>
                </div>
                <span className="font-bold text-xs text-ink shrink-0">
                  {formatCurrency(item.unitPrice * item.quantity)}
                </span>
              </div>
            ))}
          </div>

          <div className="space-y-2 pt-4 border-t border-surface-subtle text-xs sm:text-sm">
            <div className="flex justify-between text-ink-muted">
              <span>Subtotal</span>
              <span className="font-bold text-ink">{formatCurrency(subtotal)}</span>
            </div>
            <div className="flex justify-between text-ink-muted">
              <span>Delivery</span>
              <span className="font-semibold text-emerald-600">
                {shippingFee === 0 ? 'FREE' : formatCurrency(shippingFee)}
              </span>
            </div>
            <div className="flex justify-between text-base font-black text-ink pt-3 border-t border-surface-subtle">
              <span>Total</span>
              <span>{formatCurrency(grandTotal)}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
