import React, { useState, useEffect, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import ProductGrid from '../components/ProductGrid';
import ProductFilterSidebar from '../components/ProductFilterSidebar';
import ProductSortBar from '../components/ProductSortBar';
import ProductQuickViewModal from '../components/ProductQuickViewModal';
import Drawer from '../../../components/ui/Drawer';
import { productApi } from '../api/productApi';
import { categoryApi } from '../../categories/api/categoryApi';
import { SlidersHorizontal } from 'lucide-react';

export default function ProductsPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [isMobileFilterOpen, setIsMobileFilterOpen] = useState(false);
  const [quickViewProduct, setQuickViewProduct] = useState(null);

  // Read filter params from URL searchParams
  const selectedCategory = searchParams.get('category') || '';
  const minPrice = searchParams.get('minPrice') || '';
  const maxPrice = searchParams.get('maxPrice') || '';
  const inStockOnly = searchParams.get('inStock') === 'true';
  const sortBy = searchParams.get('sortBy') || '';
  const searchTerm = searchParams.get('q') || '';

  // Load Categories
  useEffect(() => {
    categoryApi.getCategories().then((data) => setCategories(data || [])).catch(console.error);
  }, []);

  // Fetch Products based on URL query params
  const fetchProducts = useCallback(async () => {
    setIsLoading(true);
    try {
      // Find category GUID if category param is slug
      let categoryId = null;
      if (selectedCategory) {
        const matchingCat = categories.find((c) => c.slug === selectedCategory || c.id === selectedCategory);
        if (matchingCat) {
          categoryId = matchingCat.id;
        }
      }

      const params = {
        CategoryId: categoryId || undefined,
        MinPrice: minPrice ? Number(minPrice) : undefined,
        MaxPrice: maxPrice ? Number(maxPrice) : undefined,
        InStockOnly: inStockOnly ? true : undefined,
        SearchTerm: searchTerm || undefined,
        SortBy: sortBy || undefined,
        PageSize: 24,
      };

      const result = await productApi.getProducts(params);
      setProducts(result.items || []);
      setTotalCount(result.totalCount || result.items?.length || 0);
    } catch (err) {
      console.error('Failed to load products', err);
      setProducts([]);
      setTotalCount(0);
    } finally {
      setIsLoading(false);
    }
  }, [categories, selectedCategory, minPrice, maxPrice, inStockOnly, sortBy, searchTerm]);

  useEffect(() => {
    fetchProducts();
  }, [fetchProducts]);

  // Handlers for modifying filter params
  const handleSelectCategory = (slug) => {
    const nextParams = new URLSearchParams(searchParams);
    if (slug) {
      nextParams.set('category', slug);
    } else {
      nextParams.delete('category');
    }
    setSearchParams(nextParams);
    setIsMobileFilterOpen(false);
  };

  const handlePriceChange = (min, max) => {
    const nextParams = new URLSearchParams(searchParams);
    if (min !== '' && min !== undefined) nextParams.set('minPrice', min);
    else nextParams.delete('minPrice');

    if (max !== '' && max !== undefined) nextParams.set('maxPrice', max);
    else nextParams.delete('maxPrice');

    setSearchParams(nextParams);
  };

  const handleInStockChange = (checked) => {
    const nextParams = new URLSearchParams(searchParams);
    if (checked) nextParams.set('inStock', 'true');
    else nextParams.delete('inStock');
    setSearchParams(nextParams);
  };

  const handleSortChange = (newSort) => {
    const nextParams = new URLSearchParams(searchParams);
    if (newSort) nextParams.set('sortBy', newSort);
    else nextParams.delete('sortBy');
    setSearchParams(nextParams);
  };

  const handleResetFilters = () => {
    setSearchParams({});
    setIsMobileFilterOpen(false);
  };

  const activeCategoryObject = categories.find((c) => c.slug === selectedCategory);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12 space-y-8">
      {/* Header Banner */}
      <div className="space-y-2">
        <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-accent-indigo">
          <span>SPOCS Catalog</span>
          {activeCategoryObject && (
            <>
              <span>/</span>
              <span>{activeCategoryObject.name}</span>
            </>
          )}
        </div>
        <h1 className="nike-heading text-3xl sm:text-5xl text-ink tracking-tight">
          {activeCategoryObject ? activeCategoryObject.name : 'ALL PRODUCTS'}
        </h1>
        {activeCategoryObject?.description && (
          <p className="text-xs sm:text-sm text-ink-muted max-w-2xl">
            {activeCategoryObject.description}
          </p>
        )}
      </div>

      {/* Main Grid + Filter Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
        {/* Desktop Sidebar */}
        <aside className="hidden lg:block lg:col-span-1 sticky top-24">
          <ProductFilterSidebar
            categories={categories}
            selectedCategory={selectedCategory}
            onSelectCategory={handleSelectCategory}
            minPrice={minPrice}
            maxPrice={maxPrice}
            onPriceChange={handlePriceChange}
            inStockOnly={inStockOnly}
            onInStockChange={handleInStockChange}
            onResetFilters={handleResetFilters}
          />
        </aside>

        {/* Product Catalog Content */}
        <div className="lg:col-span-3 space-y-6">
          <ProductSortBar
            totalCount={totalCount}
            sortBy={sortBy}
            onSortChange={handleSortChange}
            onToggleMobileFilter={() => setIsMobileFilterOpen(true)}
          />

          <ProductGrid
            products={products}
            isLoading={isLoading}
            onQuickView={(p) => setQuickViewProduct(p)}
            onResetFilters={handleResetFilters}
            columns="grid-cols-1 sm:grid-cols-2 xl:grid-cols-3"
          />
        </div>
      </div>

      {/* Mobile Filter Slide-out Drawer */}
      <Drawer
        isOpen={isMobileFilterOpen}
        onClose={() => setIsMobileFilterOpen(false)}
        title="Filter Catalog"
        icon={SlidersHorizontal}
      >
        <ProductFilterSidebar
          categories={categories}
          selectedCategory={selectedCategory}
          onSelectCategory={handleSelectCategory}
          minPrice={minPrice}
          maxPrice={maxPrice}
          onPriceChange={handlePriceChange}
          inStockOnly={inStockOnly}
          onInStockChange={handleInStockChange}
          onResetFilters={handleResetFilters}
          className="border-0 shadow-none p-0"
        />
      </Drawer>

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
