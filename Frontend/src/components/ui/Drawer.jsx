import React, { useEffect } from 'react';
import { cn } from '../../utils/cn';
import { X } from 'lucide-react';
import Button from './Button';

export default function Drawer({
  isOpen,
  onClose,
  title,
  subtitle,
  children,
  footer,
  width = 'max-w-md',
  position = 'right',
  icon: HeaderIcon,
}) {
  useEffect(() => {
    const handleKeyDown = (e) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      window.addEventListener('keydown', handleKeyDown);
    }
    return () => {
      document.body.style.overflow = '';
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-hidden">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-ink/40 backdrop-blur-xs transition-opacity duration-300 animate-fade-in"
        onClick={onClose}
      />

      <div className="fixed inset-y-0 right-0 flex max-w-full pl-10">
        <div
          className={cn(
            "w-screen bg-white shadow-drawer border-l border-surface-subtle flex flex-col z-10 animate-slide-in-right",
            width
          )}
        >
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-surface-subtle bg-white">
            <div className="flex items-center gap-3">
              {HeaderIcon && (
                <div className="w-9 h-9 rounded-xl bg-surface-muted flex items-center justify-center text-ink">
                  <HeaderIcon className="w-5 h-5" />
                </div>
              )}
              <div>
                <h2 className="text-base font-bold text-ink tracking-tight">{title}</h2>
                {subtitle && <p className="text-xs text-ink-muted">{subtitle}</p>}
              </div>
            </div>
            <Button
              variant="ghost"
              size="icon-sm"
              onClick={onClose}
              className="text-ink-muted hover:text-ink -mr-2"
              aria-label="Close"
            >
              <X className="w-5 h-5" />
            </Button>
          </div>

          {/* Body */}
          <div className="flex-1 overflow-y-auto p-6">
            {children}
          </div>

          {/* Footer */}
          {footer && (
            <div className="p-6 border-t border-surface-subtle bg-surface-soft/60">
              {footer}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
