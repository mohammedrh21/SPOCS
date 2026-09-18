import React from 'react';
import { cn } from '../../utils/cn';

export default function Badge({
  children,
  variant = 'default',
  size = 'md',
  className,
  ...props
}) {
  const variants = {
    default: "bg-surface-muted text-ink-muted border border-surface-subtle",
    brand: "bg-indigo-50 text-accent-indigo border border-indigo-100 font-semibold",
    orange: "bg-orange-50 text-accent-orange border border-orange-200 font-bold",
    success: "bg-emerald-50 text-emerald-700 border border-emerald-200 font-medium",
    warning: "bg-amber-50 text-amber-700 border border-amber-200 font-medium",
    danger: "bg-rose-50 text-rose-700 border border-rose-200 font-medium",
    dark: "bg-ink text-white font-semibold",
    ai: "bg-gradient-to-r from-indigo-500/10 to-teal-500/10 text-indigo-700 border border-indigo-200 font-semibold",
  };

  const sizes = {
    sm: "text-[10px] px-2 py-0.5 rounded-md uppercase tracking-wider",
    md: "text-xs px-2.5 py-1 rounded-full",
    lg: "text-sm px-3.5 py-1.5 rounded-full font-medium",
  };

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 leading-none select-none",
        variants[variant],
        sizes[size],
        className
      )}
      {...props}
    >
      {children}
    </span>
  );
}
