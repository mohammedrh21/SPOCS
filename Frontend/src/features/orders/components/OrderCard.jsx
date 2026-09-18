import React from 'react';
import { Package, Calendar, MapPin, ChevronRight, CheckCircle2, Clock } from 'lucide-react';
import { formatCurrency, formatDate } from '../../../utils/formatters';
import Badge from '../../../components/ui/Badge';

export default function OrderCard({ order, onViewDetails }) {
  const statusVariants = {
    'Pending': 'warning',
    'Processing': 'brand',
    'Shipped': 'brand',
    'Delivered': 'success',
    'Completed': 'success',
    'Cancelled': 'danger',
  };

  return (
    <div className="bg-white rounded-3xl border border-surface-subtle p-6 shadow-card hover:border-slate-300 transition-all space-y-4">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-surface-subtle">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-surface-muted flex items-center justify-center text-ink shrink-0">
            <Package className="w-5 h-5" />
          </div>
          <div>
            <p className="text-xs font-mono text-ink-muted">Order #{order.id.substring(0, 8)}</p>
            <div className="flex items-center gap-2 text-xs font-semibold text-ink">
              <Calendar className="w-3.5 h-3.5 text-ink-subtle" />
              <span>{formatDate(order.createdAt)}</span>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between sm:justify-end gap-3">
          <Badge variant={statusVariants[order.status] || 'default'} size="md">
            {order.status || 'Processing'}
          </Badge>
          <span className="text-base font-extrabold text-ink">
            {formatCurrency(order.totalAmount)}
          </span>
        </div>
      </div>

      {/* Items Preview */}
      <div className="space-y-2">
        <p className="text-xs font-bold uppercase tracking-wider text-ink-muted">
          Items ({order.totalItemsCount || order.items?.length || 1})
        </p>
        <div className="flex flex-wrap gap-2">
          {order.items?.slice(0, 3).map((item, i) => (
            <div
              key={i}
              className="flex items-center gap-2 p-2 rounded-xl bg-surface-soft border border-surface-subtle text-xs"
            >
              {item.productPrimaryImageUrl ? (
                <img
                  src={item.productPrimaryImageUrl}
                  alt={item.productName}
                  className="w-8 h-8 rounded-lg object-contain bg-white"
                />
              ) : (
                <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center font-bold text-[9px] text-slate-400">
                  SP
                </div>
              )}
              <div className="max-w-[140px] truncate">
                <p className="font-bold text-ink truncate">{item.productName}</p>
                <p className="text-[10px] text-ink-muted">Qty: {item.quantity}</p>
              </div>
            </div>
          ))}
          {order.items?.length > 3 && (
            <div className="flex items-center justify-center px-3 py-2 rounded-xl bg-surface-soft border border-surface-subtle text-xs font-bold text-ink-muted">
              +{order.items.length - 3} more
            </div>
          )}
        </div>
      </div>

      {/* Shipping Address & Action */}
      <div className="flex items-center justify-between pt-2 text-xs text-ink-muted">
        <div className="flex items-center gap-1.5 truncate">
          <MapPin className="w-3.5 h-3.5 text-ink-subtle shrink-0" />
          <span className="truncate">
            {order.shippingAddress}, {order.city} {order.postalCode}
          </span>
        </div>

        {onViewDetails && (
          <button
            type="button"
            onClick={() => onViewDetails(order)}
            className="font-bold text-ink hover:text-accent-indigo flex items-center gap-1 shrink-0 ml-2"
          >
            <span>Details</span>
            <ChevronRight className="w-4 h-4" />
          </button>
        )}
      </div>
    </div>
  );
}
