import React from 'react';
import { Link } from 'react-router-dom';
import { Sparkles, ArrowRight, ShieldCheck, Truck, RefreshCw, Headphones } from 'lucide-react';
import Button from '../ui/Button';

export default function Footer() {
  return (
    <footer className="bg-ink text-slate-300 pt-16 pb-12 border-t border-slate-800">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-12">
        {/* Features / Guarantees Row */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 pb-12 border-b border-slate-800">
          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-slate-800/80 flex items-center justify-center text-accent-orange shrink-0">
              <Truck className="w-6 h-6" />
            </div>
            <div>
              <h4 className="text-sm font-bold text-white uppercase tracking-wider">Fast & Free Shipping</h4>
              <p className="text-xs text-slate-400">On all orders over $75 worldwide</p>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-slate-800/80 flex items-center justify-center text-indigo-400 shrink-0">
              <Sparkles className="w-6 h-6" />
            </div>
            <div>
              <h4 className="text-sm font-bold text-white uppercase tracking-wider">AI Shopping Agent</h4>
              <p className="text-xs text-slate-400">Grounded semantic product lookup</p>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-slate-800/80 flex items-center justify-center text-teal-400 shrink-0">
              <RefreshCw className="w-6 h-6" />
            </div>
            <div>
              <h4 className="text-sm font-bold text-white uppercase tracking-wider">30-Day Returns</h4>
              <p className="text-xs text-slate-400">Hassle-free money-back guarantee</p>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-slate-800/80 flex items-center justify-center text-amber-400 shrink-0">
              <ShieldCheck className="w-6 h-6" />
            </div>
            <div>
              <h4 className="text-sm font-bold text-white uppercase tracking-wider">Authentic Guarantee</h4>
              <p className="text-xs text-slate-400">100% genuine verified products</p>
            </div>
          </div>
        </div>

        {/* Main Footer Links */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-8">
          {/* Brand Col */}
          <div className="lg:col-span-2 space-y-4">
            <Link to="/" className="inline-block">
              <span className="nike-heading text-3xl text-white tracking-tight">
                SPOCS<span className="text-accent-orange">.</span>
              </span>
            </Link>
            <p className="text-xs text-slate-400 max-w-sm leading-relaxed">
              SPOCS integrates state-of-the-art AI embeddings, semantic search, and retrieval-augmented intelligence to revolutionize how you discover modern tech and athletic gear.
            </p>
            <div className="pt-2">
              <span className="inline-flex items-center gap-2 text-xs font-semibold px-3 py-1.5 rounded-full bg-slate-800 text-indigo-300 border border-slate-700">
                <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
                OpenAI + pgvector Integration
              </span>
            </div>
          </div>

          {/* Categories */}
          <div className="space-y-3">
            <h4 className="text-xs font-bold text-white uppercase tracking-wider">Categories</h4>
            <ul className="space-y-2 text-xs">
              <li><Link to="/categories/laptops" className="hover:text-white transition-colors">Laptops & Workstations</Link></li>
              <li><Link to="/categories/smartphones" className="hover:text-white transition-colors">Smartphones</Link></li>
              <li><Link to="/categories/audio-headphones" className="hover:text-white transition-colors">Audio & Headphones</Link></li>
              <li><Link to="/categories/wearables-smartwatches" className="hover:text-white transition-colors">Smartwatches & Wearables</Link></li>
              <li><Link to="/products" className="hover:text-white transition-colors">Browse Full Catalog</Link></li>
            </ul>
          </div>

          {/* AI Features */}
          <div className="space-y-3">
            <h4 className="text-xs font-bold text-white uppercase tracking-wider">AI Capabilities</h4>
            <ul className="space-y-2 text-xs">
              <li className="text-slate-400">Natural-Language Search</li>
              <li className="text-slate-400">Multi-Turn Shopping Chat</li>
              <li className="text-slate-400">Vector Embeddings</li>
              <li className="text-slate-400">Few-Shot Prompt Grounding</li>
              <li className="text-slate-400">Zero Hallucination Retrieval</li>
            </ul>
          </div>

          {/* Newsletter */}
          <div className="space-y-3">
            <h4 className="text-xs font-bold text-white uppercase tracking-wider">Stay In The Loop</h4>
            <p className="text-xs text-slate-400">Get early access to product drops and new AI shopping tools.</p>
            <form onSubmit={(e) => e.preventDefault()} className="space-y-2">
              <input
                type="email"
                placeholder="Enter your email"
                className="w-full bg-slate-800/80 border border-slate-700 rounded-xl px-3.5 py-2 text-xs text-white placeholder:text-slate-500 focus:outline-none focus:border-accent-orange"
              />
              <Button variant="orange" size="sm" className="w-full text-xs uppercase tracking-wider">
                Subscribe
              </Button>
            </form>
          </div>
        </div>

        {/* Bottom Bar */}
        <div className="pt-8 border-t border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-slate-500">
          <p>© {new Date().getFullYear()} SPOCS Inc. All Rights Reserved.</p>
          <div className="flex items-center gap-6">
            <span className="hover:text-slate-400 cursor-pointer">Privacy Policy</span>
            <span className="hover:text-slate-400 cursor-pointer">Terms of Service</span>
            <span className="hover:text-slate-400 cursor-pointer">Security</span>
          </div>
        </div>
      </div>
    </footer>
  );
}
