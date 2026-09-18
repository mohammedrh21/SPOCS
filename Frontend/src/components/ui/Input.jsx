import React from 'react';
import { cn } from '../../utils/cn';

const Input = React.forwardRef(({
  className,
  type = 'text',
  label,
  error,
  helperText,
  icon: Icon,
  ...props
}, ref) => {
  return (
    <div className="w-full space-y-1.5">
      {label && (
        <label className="block text-xs font-semibold text-ink-muted uppercase tracking-wider">
          {label}
        </label>
      )}
      <div className="relative">
        {Icon && (
          <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-ink-subtle">
            <Icon className="h-4 w-4" />
          </div>
        )}
        <input
          type={type}
          ref={ref}
          className={cn(
            "w-full bg-white border border-surface-subtle text-ink text-sm rounded-xl px-4 py-2.5 transition-all duration-200",
            "placeholder:text-ink-subtle focus:outline-none focus:border-ink focus:ring-1 focus:ring-ink",
            "disabled:opacity-50 disabled:bg-surface-muted",
            Icon && "pl-10",
            error && "border-rose-500 focus:border-rose-500 focus:ring-rose-500",
            className
          )}
          {...props}
        />
      </div>
      {error && (
        <p className="text-xs text-rose-600 font-medium">{error}</p>
      )}
      {helperText && !error && (
        <p className="text-xs text-ink-subtle">{helperText}</p>
      )}
    </div>
  );
});

Input.displayName = 'Input';

export default Input;
