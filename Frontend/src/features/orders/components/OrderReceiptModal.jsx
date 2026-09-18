import React from 'react';
import Modal from '../../../components/ui/Modal';
import { formatCurrency, formatDateTime } from '../../../utils/formatters';
import Badge from '../../../components/ui/Badge';
import { MapPin, Package, CheckCircle2 } from 'lucide-react';

export default function OrderReceiptModal({ order, isOpen, onClose }) {
  if (!order) return null;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      maxWidth="max-w-xl"
      title={`Order Details #${order.id.substring(0, 8)}`}
    >
      <div className="space-y-6">
        {/* Order Status & Date */}
        <div className="flex items-center justify-between p-4 bg-surface-soft rounded-2xl border border-surface-subtle">
          <div>
            <p className="text-xs text-ink-muted">Placed on</p>
            <p className="text-xs sm:text-sm font-bold text-ink">{formatDateTime(order.createdAt)}</p>
          </div>
          <Badge variant="success" size="lg">
            {order.status || 'Confirmed'}
          </Badge>
        </div>

        {/* Items List */}
        <div className="space-y-3">
          <h4 className="text-xs font-bold uppercase tracking-wider text-ink-muted">
            Ordered Items ({order.items?.length || 0})
          </h4>
          <div className="divide-y divide-surface-subtle border border-surface-subtle rounded-2xl p-2 bg-white">
            {order.items?.map((item) => (
              <div key={item.id} className="p-3 flex items-center justify-between gap-3">
                <div className="flex items-center gap-3 min-w-0">
                  {item.productPrimaryImageUrl ? (
                    <img
                      src={item.productPrimaryImageUrl}
                      alt={item.productName}
                      className="w-12 h-12 rounded-xl object-contain bg-surface-muted p-1 shrink-0"
                    />
                  ) : (
                    <div className="w-12 h-12 rounded-xl bg-surface-muted flex items-center justify-center font-bold text-xs text-slate-400 shrink-0">
                      SP
                    </div>
                  )}
                  <div className="truncate">
                    <p className="font-bold text-xs sm:text-sm text-ink truncate">{item.productName}</p>
                    {item.variantDescription && (
                      <p className="text-[11px] text-ink-muted">{item.variantDescription}</p>
                    )}
                    <p className="text-xs text-ink-muted">Qty: {item.quantity} × {formatCurrency(item.unitPrice)}</p>
                  </div>
                </div>
                <span className="font-extrabold text-sm text-ink shrink-0">
                  {formatCurrency(item.totalPrice)}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Shipping Address */}
        <div className="p-4 bg-surface-soft rounded-2xl border border-surface-subtle space-y-1">
          <h4 className="text-xs font-bold uppercase tracking-wider text-ink-muted flex items-center gap-1.5">
            <MapPin className="w-3.5 h-3.5 text-ink-subtle" />
            Delivery Destination
          </h4>
          <p className="text-xs sm:text-sm font-semibold text-ink">
            {order.shippingAddress}
          </p>
          <p className="text-xs text-ink-muted">
            {order.city}, {order.postalCode}, {order.country}
          </p>
          {order.notes && (
            <p className="text-xs text-slate-500 italic pt-1">Note: {order.notes}</p>
          )}
        </div>

        {/* Price Breakdown */}
        <div className="space-y-1.5 pt-2 border-t border-surface-subtle text-xs">
          <div className="flex justify-between text-ink-muted">
            <span>Subtotal</span>
            <span className="font-bold text-ink">{formatCurrency(order.totalAmount)}</span>
          </div>
          <div className="flex justify-between text-ink-muted">
            <span>Standard Shipping</span>
            <span className="font-semibold text-emerald-600">FREE</span>
          </div>
          <div className="flex justify-between text-base font-black text-ink pt-2 border-t border-surface-subtle">
            <span>Total Paid</span>
            <span>{formatCurrency(order.totalAmount)}</span>
          </div>
        </div>
      </div>
    </Modal>
  );
}
