import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Sparkles, ArrowRight, ShoppingBag, Zap, Shield, Compass, TrendingUp, Check } from 'lucide-react';
import Button from '../../../components/ui/Button';
import ProductGrid from '../../products/components/ProductGrid';
import CategoryGrid from '../../categories/components/CategoryGrid';
import ProductQuickViewModal from '../../products/components/ProductQuickViewModal';
import { productApi } from '../../products/api/productApi';
import { categoryApi } from '../../categories/api/categoryApi';
import { useAiChat } from '../../../app/providers/AiChatContext';

export default function HomePage() {
  const { openAi } = useAiChat();
  const [featuredProducts, setFeaturedProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [quickViewProduct, setQuickViewProduct] = useState(null);

  useEffect(() => {
    const loadHomeData = async () => {
      setIsLoading(true);
      try {
        const [prodRes, catRes] = await Promise.all([
          productApi.getProducts({ PageSize: 8, SortBy: 'newest' }),
          categoryApi.getCategories(),
        ]);
        setFeaturedProducts(prodRes.items || []);
        setCategories(catRes || []);
      } catch (err) {
        console.error('Failed to load homepage data', err);
      } finally {
        setIsLoading(false);
      }
    };
    loadHomeData();
  }, []);

  return (
    <div className="space-y-16 sm:space-y-24 pb-20">
      {/* 1. NIKE-STYLE HERO BANNER */}
      <section className="relative overflow-hidden bg-slate-950 text-white rounded-b-[2.5rem] sm:rounded-b-[4rem] shadow-2xl">
        {/* Background glow & gradients */}
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_right,_var(--tw-gradient-stops))] from-indigo-900/40 via-slate-950 to-slate-950" />
        <div className="absolute -top-40 -right-40 w-96 h-96 bg-accent-orange/20 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute top-1/2 -left-40 w-80 h-80 bg-accent-indigo/20 rounded-full blur-3xl pointer-events-none" />

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 sm:py-32 lg:py-40 flex flex-col items-center text-center space-y-8">
          {/* Badge */}
          <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-white/10 backdrop-blur-md border border-white/15 text-xs font-bold uppercase tracking-wider text-accent-orange animate-fade-in">
            <Zap className="w-3.5 h-3.5 fill-current" />
            <span>AI-POWERED COMMERCE REVOLUTION</span>
          </div>

          {/* Bold Impact Heading */}
          <div className="space-y-4 max-w-4xl">
            <h1 className="nike-heading text-4xl sm:text-6xl md:text-7xl lg:text-8xl tracking-tighter uppercase leading-[0.9] font-black text-white">
              INNOVATION <br />
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-accent-orange via-amber-400 to-indigo-400">
                IN MOTION.
              </span>
            </h1>
            <p className="text-sm sm:text-lg md:text-xl text-slate-300 max-w-2xl mx-auto font-normal leading-relaxed">
              Experience the future of tech and sportswear shopping. Ask in natural language, explore vector-indexed catalogs, and find exactly what fits your needs.
            </p>
          </div>

          {/* Call to Actions */}
          <div className="flex flex-col sm:flex-row items-center gap-4 pt-2 w-full sm:w-auto justify-center">
            <Link to="/products" className="w-full sm:w-auto">
              <Button
                variant="orange"
                size="lg"
                className="w-full sm:w-auto uppercase font-bold tracking-wider text-sm h-13 px-8 shadow-lg shadow-orange-500/20"
              >
                <span>Shop New Arrivals</span>
                <ArrowRight className="w-4 h-4" />
              </Button>
            </Link>

            <Button
              variant="outline"
              size="lg"
              onClick={openAi}
              className="w-full sm:w-auto border-white/20 text-white hover:bg-white/10 hover:border-white h-13 px-8 text-sm font-bold uppercase tracking-wider flex items-center gap-2"
            >
              <Sparkles className="w-4 h-4 text-indigo-400" />
              <span>Ask AI Assistant</span>
            </Button>
          </div>

          {/* Metrics bar */}
          <div className="pt-12 grid grid-cols-2 md:grid-cols-4 gap-6 sm:gap-12 border-t border-white/10 w-full max-w-4xl text-center">
            <div>
              <p className="nike-heading text-2xl sm:text-3xl text-white">100%</p>
              <p className="text-xs text-slate-400 font-medium">Grounded RAG</p>
            </div>
            <div>
              <p className="nike-heading text-2xl sm:text-3xl text-accent-orange">1536-D</p>
              <p className="text-xs text-slate-400 font-medium">Vector Embeddings</p>
            </div>
            <div>
              <p className="nike-heading text-2xl sm:text-3xl text-white">&lt;50ms</p>
              <p className="text-xs text-slate-400 font-medium">Semantic Search</p>
            </div>
            <div>
              <p className="nike-heading text-2xl sm:text-3xl text-teal-400">Zero</p>
              <p className="text-xs text-slate-400 font-medium">Hallucinations</p>
            </div>
          </div>
        </div>
      </section>

      {/* 2. CATEGORIES SPOTLIGHT */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-8">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
          <div>
            <p className="text-xs font-bold text-accent-indigo uppercase tracking-wider">Curated Collections</p>
            <h2 className="nike-heading text-2xl sm:text-4xl text-ink tracking-tight">
              EXPLORE BY CATEGORY
            </h2>
          </div>
          <Link
            to="/products"
            className="text-xs font-bold uppercase tracking-wider text-ink hover:text-accent-orange flex items-center gap-1 transition-colors"
          >
            <span>View All Categories</span>
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>

        <CategoryGrid categories={categories.slice(0, 4)} isLoading={isLoading} />
      </section>

      {/* 3. AI SHOPPING ASSISTANT HERO TEASER */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-r from-indigo-950 via-slate-900 to-teal-950 text-white p-8 sm:p-12 lg:p-16 border border-slate-800 shadow-2xl">
          <div className="relative z-10 max-w-2xl space-y-6">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-400/30 text-xs font-bold uppercase tracking-wider">
              <Sparkles className="w-3.5 h-3.5" />
              <span>Next-Gen Shopping AI</span>
            </div>

            <h3 className="nike-heading text-3xl sm:text-5xl text-white tracking-tight leading-tight">
              CAN'T DECIDE? <br />
              LET AI PICK FOR YOU.
            </h3>

            <p className="text-xs sm:text-sm text-slate-300 leading-relaxed font-normal">
              Forget scrolling through endless specs. Type exactly what you need in plain English — like "I need a light laptop for coding under $1200" — and our grounded AI assistant retrieves the ideal choices instantly.
            </p>

            <div className="pt-2 flex flex-wrap gap-3">
              <Button
                variant="orange"
                size="md"
                onClick={openAi}
                className="uppercase font-bold tracking-wider text-xs"
              >
                Launch Assistant
              </Button>
              <Link to="/products">
                <Button
                  variant="outline"
                  size="md"
                  className="border-white/30 text-white hover:bg-white/10 text-xs uppercase font-bold tracking-wider"
                >
                  Browse Catalog
                </Button>
              </Link>
            </div>
          </div>

          {/* Decorative AI chat sample preview */}
          <div className="hidden lg:block absolute right-12 top-1/2 -translate-y-1/2 w-80 space-y-3 pointer-events-none opacity-90">
            <div className="bg-white/10 backdrop-blur-md border border-white/15 p-4 rounded-2xl rounded-tr-xs text-xs text-slate-200">
              "Show me lightweight laptops with 16GB RAM for programming under $1500."
            </div>
            <div className="bg-indigo-600/90 backdrop-blur-md p-4 rounded-2xl rounded-tl-xs text-xs text-white space-y-1.5 shadow-xl">
              <div className="flex items-center gap-1.5 font-bold text-amber-300 text-[11px]">
                <Sparkles className="w-3.5 h-3.5" />
                <span>SPOCS AI Advisor</span>
              </div>
              <p>Found 2 matching laptops: MacBook Pro 14" M3 Pro and ThinkPad X1 Carbon with 16GB RAM.</p>
            </div>
          </div>
        </div>
      </section>

      {/* 4. FEATURED PRODUCTS GRID */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-8">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
          <div>
            <p className="text-xs font-bold text-accent-indigo uppercase tracking-wider">Top Rated & Trending</p>
            <h2 className="nike-heading text-2xl sm:text-4xl text-ink tracking-tight">
              FEATURED DROPS
            </h2>
          </div>
          <Link
            to="/products"
            className="text-xs font-bold uppercase tracking-wider text-ink hover:text-accent-orange flex items-center gap-1 transition-colors"
          >
            <span>See Everything</span>
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>

        <ProductGrid
          products={featuredProducts}
          isLoading={isLoading}
          onQuickView={(p) => setQuickViewProduct(p)}
        />
      </section>

      {/* Quick View Modal */}
      {quickViewProduct && (
        <ProductQuickViewModal
          product={quickViewProduct}
          isOpen={Boolean(quickViewProduct)}
          onClose={() => setQuickViewProduct(null)}
        />
      )}
    </div>
  );
}
