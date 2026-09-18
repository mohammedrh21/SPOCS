import React, { useState } from 'react';
import { Link, NavLink } from 'react-router-dom';
import { Search, ShoppingBag, Sparkles, Menu, X } from 'lucide-react';
import { useCart } from '../../app/providers/CartContext';
import { useAiChat } from '../../app/providers/AiChatContext';
import UserDropdown from '../../features/auth/components/UserDropdown';
import SemanticSearchBar from '../../features/products/components/SemanticSearchBar';
import Button from '../ui/Button';

export default function Header() {
  const { totalItemsCount, openCart } = useCart();
  const { openAi } = useAiChat();
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const navLinks = [
    { label: 'All Products', href: '/products' },
    { label: 'Laptops', href: '/categories/laptops' },
    { label: 'Smartphones', href: '/categories/smartphones' },
    { label: 'Audio', href: '/categories/audio-headphones' },
    { label: 'Wearables', href: '/categories/wearables-smartwatches' },
  ];

  return (
    <>
      <header className="sticky top-0 z-40 bg-white/95 backdrop-blur-md border-b border-surface-subtle transition-all">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-18 flex items-center justify-between gap-4">
          {/* Brand Logo */}
          <div className="flex items-center gap-6">
            <Link to="/" className="flex items-center gap-2 group">
              <span className="nike-heading text-2xl sm:text-3xl text-ink tracking-tighter group-hover:text-accent-orange transition-colors">
                SPOCS<span className="text-accent-orange">.</span>
              </span>
            </Link>

            {/* Desktop Navigation Links */}
            <nav className="hidden lg:flex items-center gap-7">
              {navLinks.map((link) => (
                <NavLink
                  key={link.href}
                  to={link.href}
                  className={({ isActive }) =>
                    `text-sm font-semibold tracking-tight transition-colors hover:text-ink relative py-1 ${
                      isActive ? 'text-ink font-bold after:absolute after:bottom-0 after:left-0 after:right-0 after:h-0.5 after:bg-ink' : 'text-ink-muted'
                    }`
                  }
                >
                  {link.label}
                </NavLink>
              ))}
            </nav>
          </div>

          {/* Right Actions: Semantic Search, AI Assistant, Cart, User */}
          <div className="flex items-center gap-2 sm:gap-3">
            {/* Search Input Trigger */}
            <button
              onClick={() => setIsSearchOpen(true)}
              className="flex items-center gap-2.5 bg-surface-muted hover:bg-slate-200 text-ink-muted hover:text-ink px-3.5 py-2 rounded-full text-xs font-medium transition-all duration-200 cursor-pointer w-32 sm:w-56"
              aria-label="Search catalog with AI"
            >
              <Search className="w-4 h-4 text-ink-subtle shrink-0" />
              <span className="truncate hidden sm:inline">Search AI Catalog...</span>
              <span className="truncate sm:hidden">Search...</span>
            </button>

            {/* AI Assistant Quick Trigger */}
            <button
              onClick={openAi}
              className="relative flex items-center gap-1.5 px-3 py-1.5 sm:px-3.5 sm:py-2 rounded-full bg-gradient-to-r from-indigo-500/10 via-purple-500/10 to-teal-500/10 border border-indigo-200 hover:border-indigo-400 text-accent-indigo hover:text-indigo-800 transition-all font-semibold text-xs shadow-xs group"
              title="Ask AI Shopping Assistant"
            >
              <Sparkles className="w-3.5 h-3.5 text-accent-indigo group-hover:rotate-12 transition-transform" />
              <span className="hidden md:inline">AI Assistant</span>
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-indigo-400 opacity-75" />
                <span className="relative inline-flex rounded-full h-2 w-2 bg-indigo-600" />
              </span>
            </button>

            {/* Shopping Bag / Cart */}
            <button
              onClick={openCart}
              className="relative p-2.5 rounded-full hover:bg-surface-muted text-ink transition-colors"
              aria-label="Open Shopping Bag"
            >
              <ShoppingBag className="w-5 h-5" />
              {totalItemsCount > 0 && (
                <span className="absolute top-1 right-1 bg-accent-orange text-white text-[10px] font-extrabold w-4 h-4 rounded-full flex items-center justify-center animate-pulse-subtle">
                  {totalItemsCount}
                </span>
              )}
            </button>

            {/* User Dropdown */}
            <UserDropdown />

            {/* Mobile Menu Toggle */}
            <button
              onClick={() => setIsMobileMenuOpen((prev) => !prev)}
              className="lg:hidden p-2 rounded-xl text-ink hover:bg-surface-muted"
            >
              {isMobileMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
        </div>

        {/* Mobile Dropdown Navigation */}
        {isMobileMenuOpen && (
          <div className="lg:hidden border-t border-surface-subtle bg-white px-6 py-4 space-y-3 animate-slide-up">
            {navLinks.map((link) => (
              <Link
                key={link.href}
                to={link.href}
                onClick={() => setIsMobileMenuOpen(false)}
                className="block text-sm font-bold text-ink py-2 hover:text-accent-orange transition-colors"
              >
                {link.label}
              </Link>
            ))}
          </div>
        )}
      </header>

      {/* Semantic Search Modal */}
      <SemanticSearchBar
        isOpen={isSearchOpen}
        onClose={() => setIsSearchOpen(false)}
      />
    </>
  );
}
