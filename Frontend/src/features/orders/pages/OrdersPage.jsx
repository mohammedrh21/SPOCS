import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Package, Lock, Loader2, ArrowRight } from 'lucide-react';
import OrderCard from '../components/OrderCard';
import OrderReceiptModal from '../components/OrderReceiptModal';
import Button from '../../../components/ui/Button';
import { useAuth } from '../../../app/providers/AuthContext';
import { orderApi } from '../api/orderApi';

export default function OrdersPage() {
  const { isAuthenticated, openAuthModal } = useAuth();
  const [orders, setOrders] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState(null);

  useEffect(() => {
    if (!isAuthenticated) {
      setIsLoading(false);
      return;
    }

    const fetchOrders = async () => {
      setIsLoading(true);
      try {
        const data = await orderApi.getMyOrders();
        setOrders(data || []);
      } catch (err) {
        console.error('Failed to load orders', err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchOrders();
  }, [isAuthenticated]);

  if (!isAuthenticated) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-20 text-center space-y-6">
        <div className="w-16 h-16 rounded-full bg-surface-muted text-ink-subtle flex items-center justify-center mx-auto">
          <Lock className="w-8 h-8" />
        </div>
        <div className="space-y-2">
          <h1 className="nike-heading text-3xl sm:text-4xl text-ink">SIGN IN TO VIEW ORDERS</h1>
          <p className="text-xs sm:text-sm text-ink-muted">
            You need to be logged in to access your order history and tracking.
          </p>
        </div>
        <Button
          variant="nike-dark"
          size="lg"
          onClick={() => openAuthModal('login')}
          className="uppercase font-bold tracking-wider text-xs"
        >
          Sign In
        </Button>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-24 flex flex-col items-center justify-center space-y-3">
        <Loader2 className="w-10 h-10 text-ink animate-spin" />
        <p className="text-xs font-semibold text-ink-muted">Loading your orders...</p>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 py-8 sm:py-12 space-y-8">
      {/* Title */}
      <div className="space-y-1 pb-4 border-b border-surface-subtle">
        <h1 className="nike-heading text-3xl sm:text-5xl text-ink tracking-tight">
          MY ORDERS
        </h1>
        <p className="text-xs font-semibold text-ink-muted">
          Track and inspect details for your recent purchases
        </p>
      </div>

      {orders.length === 0 ? (
        <div className="bg-white rounded-3xl border border-surface-subtle p-12 text-center space-y-4 shadow-card">
          <div className="w-16 h-16 rounded-2xl bg-surface-muted flex items-center justify-center mx-auto text-ink-subtle">
            <Package className="w-8 h-8" />
          </div>
          <div className="space-y-1">
            <h3 className="text-lg font-bold text-ink">No orders yet</h3>
            <p className="text-xs text-ink-muted max-w-xs mx-auto">
              Once you complete a purchase, your order history will appear here.
            </p>
          </div>
          <Link to="/products">
            <Button variant="nike-dark" size="md" className="uppercase font-bold tracking-wider text-xs">
              Start Shopping
            </Button>
          </Link>
        </div>
      ) : (
        <div className="space-y-4">
          {orders.map((order) => (
            <OrderCard
              key={order.id}
              order={order}
              onViewDetails={(ord) => setSelectedOrder(ord)}
            />
          ))}
        </div>
      )}

      {/* Receipt Modal */}
      {selectedOrder && (
        <OrderReceiptModal
          order={selectedOrder}
          isOpen={Boolean(selectedOrder)}
          onClose={() => setSelectedOrder(null)}
        />
      )}
    </div>
  );
}
