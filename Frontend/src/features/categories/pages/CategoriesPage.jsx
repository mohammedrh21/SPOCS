import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import CategoryGrid from '../components/CategoryGrid';
import ProductGrid from '../../products/components/ProductGrid';
import ProductSortBar from '../../products/components/ProductSortBar';
import ProductQuickViewModal from '../../products/components/ProductQuickViewModal';
import { categoryApi } from '../api/categoryApi';
import { productApi } from '../../products/api/productApi';
import { CATEGORY_HERO_IMAGES } from '../../../constants/config';
import { ChevronRight } from 'lucide-react';

export default function CategoriesPage() {
  const { slug } = useParams();

  const [categories, setCategories] = useState([]);
  const [currentCategory, setCurrentCategory] = useState(null);
  const [products, setProducts] = useState([]);
  const [sortBy, setSortBy] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [quickViewProduct, setQuickViewProduct] = useState(null);

  // Load Categories
  useEffect(() => {
    categoryApi.getCategories().then((data) => setCategories(data || [])).catch(console.error);
  }, []);

  // If slug is provided, load specific category products
  useEffect(() => {
    if (!slug) {
      setIsLoading(false);
      return;
    }

    const loadCategoryProducts = async () => {
      setIsLoading(true);
      try {
        const cat = await categoryApi.getCategoryBySlug(slug);
        setCurrentCategory(cat);

        const prodRes = await productApi.getProducts({
          CategoryId: cat.id,
          SortBy: sortBy || undefined,
          PageSize: 24,
        });
        setProducts(prodRes.items || []);
      } catch (err) {
        console.error('Failed to load category', err);
      } finally {
        setIsLoading(false);
      }
    };

    loadCategoryProducts();
  }, [slug, sortBy]);

  // If no slug, show all categories grid
  if (!slug) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12 space-y-8">
        <div className="space-y-2">
          <p className="text-xs font-bold uppercase tracking-wider text-accent-indigo">Catalog Index</p>
          <h1 className="nike-heading text-3xl sm:text-5xl text-ink tracking-tight">
            ALL CATEGORIES
          </h1>
          <p className="text-xs sm:text-sm text-ink-muted">
            Explore our cutting-edge tech and sportswear departments.
          </p>
        </div>

        <CategoryGrid categories={categories} isLoading={isLoading} />
      </div>
    );
  }

  const bgImage = currentCategory?.imageUrl || (currentCategory && CATEGORY_HERO_IMAGES[currentCategory.slug]) || 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80';

  return (
    <div className="space-y-8 pb-16">
      {/* Category Hero Banner */}
      <section className="relative h-72 sm:h-96 bg-slate-950 text-white overflow-hidden flex items-center">
        <img
          src={bgImage}
          alt={currentCategory?.name}
          className="absolute inset-0 w-full h-full object-cover brightness-[0.4]"
        />
        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full space-y-3">
          <nav className="flex items-center gap-2 text-xs font-semibold text-slate-300">
            <Link to="/" className="hover:text-white transition-colors">Home</Link>
            <ChevronRight className="w-3.5 h-3.5 text-slate-400" />
            <Link to="/categories" className="hover:text-white transition-colors">Categories</Link>
            <ChevronRight className="w-3.5 h-3.5 text-slate-400" />
            <span className="text-white">{currentCategory?.name || slug}</span>
          </nav>

          <h1 className="nike-heading text-3xl sm:text-5xl md:text-6xl text-white tracking-tight">
            {currentCategory?.name || slug}
          </h1>

          {currentCategory?.description && (
            <p className="text-xs sm:text-sm text-slate-300 max-w-xl">
              {currentCategory.description}
            </p>
          )}
        </div>
      </section>

      {/* Category Products */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">
        <ProductSortBar
          totalCount={products.length}
          sortBy={sortBy}
          onSortChange={setSortBy}
        />

        <ProductGrid
          products={products}
          isLoading={isLoading}
          onQuickView={(p) => setQuickViewProduct(p)}
          columns="grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
        />
      </div>

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
