import React from 'react';
import { Sparkles, Truck, RefreshCw } from 'lucide-react';

export default function TopAnnouncementBar() {
  return (
    <div className="bg-ink text-white py-2 px-4 text-xs font-medium tracking-wide">
      <div className="max-w-7xl mx-auto flex items-center justify-between">
        <div className="hidden sm:flex items-center gap-2 text-ink-subtle text-[11px]">
          <span>SPOCS OFFICIAL STORE</span>
          <span>•</span>
          <span className="text-emerald-400 font-semibold flex items-center gap-1">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
            AI Catalog Online
          </span>
        </div>

        <div className="flex items-center justify-center gap-6 w-full sm:w-auto text-center font-semibold text-slate-200">
          <div className="flex items-center gap-1.5 hover:text-white transition-colors cursor-default">
            <Truck className="w-3.5 h-3.5 text-accent-orange" />
            <span>Free Express Delivery on Orders Over $75</span>
          </div>
          <span className="hidden md:inline text-slate-600">|</span>
          <div className="hidden md:flex items-center gap-1.5 text-slate-300">
            <Sparkles className="w-3.5 h-3.5 text-indigo-400" />
            <span>AI-Powered Semantic Product Search</span>
          </div>
          <span className="hidden lg:inline text-slate-600">|</span>
          <div className="hidden lg:flex items-center gap-1.5 text-slate-300">
            <RefreshCw className="w-3.5 h-3.5 text-teal-400" />
            <span>30-Day Hassle-Free Returns</span>
          </div>
        </div>

        <div className="hidden sm:flex items-center gap-4 text-xs text-ink-subtle">
          <span className="hover:text-white transition-colors cursor-pointer">Find a Store</span>
          <span>|</span>
          <span className="hover:text-white transition-colors cursor-pointer">Help</span>
        </div>
      </div>
    </div>
  );
}
