import React, { useState, useRef, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { User, LogOut, Package, Sparkles, ChevronDown } from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import Button from '../../../components/ui/Button';

export default function UserDropdown() {
  const { user, isAuthenticated, logout, openAuthModal } = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef(null);
  const navigate = useNavigate();

  useEffect(() => {
    const handleClickOutside = (e) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  if (!isAuthenticated) {
    return (
      <Button
        variant="ghost"
        size="sm"
        onClick={() => openAuthModal('login')}
        className="font-bold text-xs uppercase tracking-wider text-ink hover:text-accent-indigo"
      >
        Sign In
      </Button>
    );
  }

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen((prev) => !prev)}
        className="flex items-center gap-2 p-1.5 pl-2.5 rounded-full border border-surface-subtle hover:border-ink transition-all bg-white text-ink text-xs font-semibold"
      >
        <div className="w-6 h-6 rounded-full bg-ink text-white flex items-center justify-center text-[10px] font-bold uppercase">
          {user?.fullName ? user.fullName[0] : user?.email ? user.email[0] : 'U'}
        </div>
        <span className="hidden sm:inline max-w-[100px] truncate">
          {user?.fullName?.split(' ')[0] || user?.email?.split('@')[0]}
        </span>
        <ChevronDown className="w-3.5 h-3.5 text-ink-muted" />
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-56 bg-white rounded-2xl shadow-floating border border-surface-subtle py-2 z-50 animate-slide-up">
          {/* User Details */}
          <div className="px-4 py-2.5 border-b border-surface-subtle">
            <p className="text-xs font-bold text-ink truncate">{user?.fullName || 'Customer'}</p>
            <p className="text-[11px] text-ink-muted truncate">{user?.email}</p>
          </div>

          <div className="py-1">
            <Link
              to="/orders"
              onClick={() => setIsOpen(false)}
              className="flex items-center gap-2.5 px-4 py-2 text-xs font-semibold text-ink hover:bg-surface-soft hover:text-accent-indigo transition-colors"
            >
              <Package className="w-4 h-4 text-ink-muted" />
              <span>My Orders</span>
            </Link>
          </div>

          <div className="pt-1 border-t border-surface-subtle">
            <button
              onClick={() => {
                setIsOpen(false);
                logout();
              }}
              className="w-full flex items-center gap-2.5 px-4 py-2 text-xs font-semibold text-rose-600 hover:bg-rose-50 transition-colors"
            >
              <LogOut className="w-4 h-4" />
              <span>Sign Out</span>
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
