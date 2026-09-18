import React from 'react';
import { cn } from '../../utils/cn';
import { Loader2 } from 'lucide-react';

const Button = React.forwardRef(({
  className,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  disabled,
  children,
  type = 'button',
  ...props
}, ref) => {
  const baseStyles = "inline-flex items-center justify-center font-medium transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 active:scale-[0.98] disabled:opacity-50 disabled:pointer-events-none disabled:active:scale-100 rounded-full select-none";

  const variants = {
    // Nike-style high impact dark button
    'nike-dark': "bg-ink text-white hover:bg-slate-800 focus:ring-ink",
    // Brand primary
    'primary': "bg-accent-indigo text-white hover:bg-indigo-700 focus:ring-accent-indigo shadow-sm",
    // Nike vibrant orange
    'orange': "bg-accent-orange text-white hover:bg-orange-600 focus:ring-accent-orange shadow-sm font-semibold",
    // Secondary
    'secondary': "bg-surface-muted text-ink hover:bg-surface-subtle focus:ring-slate-400",
    // Outline
    'outline': "border border-surface-subtle bg-transparent text-ink hover:border-ink hover:bg-surface-soft focus:ring-ink",
    // Ghost
    'ghost': "bg-transparent text-ink hover:bg-surface-muted focus:ring-slate-300",
    // Danger
    'danger': "bg-rose-600 text-white hover:bg-rose-700 focus:ring-rose-500",
    // Subtle Brand Teal
    'teal': "bg-teal-600 text-white hover:bg-teal-700 focus:ring-teal-500",
  };

  const sizes = {
    sm: "text-xs px-3.5 py-1.5 h-8 gap-1.5",
    md: "text-sm px-5 py-2.5 h-10 gap-2",
    lg: "text-base px-7 py-3.5 h-12 gap-2.5 font-semibold",
    icon: "h-10 w-10 p-0 rounded-full",
    'icon-sm': "h-8 w-8 p-0 rounded-full",
  };

  return (
    <button
      ref={ref}
      type={type}
      disabled={disabled || isLoading}
      className={cn(baseStyles, variants[variant], sizes[size], className)}
      {...props}
    >
      {isLoading && <Loader2 className="w-4 h-4 animate-spin shrink-0" />}
      {children}
    </button>
  );
});

Button.displayName = 'Button';

export default Button;
