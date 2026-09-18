import React, { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { ShoppingBag, Sparkles, Truck, ShieldCheck, RefreshCw, ChevronRight, Check, Loader2 } from 'lucide-react';
import ProductGallery from '../components/ProductGallery';
import ProductVariantSelector from '../components/ProductVariantSelector';
import ProductSpecsTable from '../components/ProductSpecsTable';
import Button from '../../../components/ui/Button';
import Badge from '../../../components/ui/Badge';
import { productApi } from '../api/productApi';
import { useCart } from '../../../app/providers/CartContext';
import { useAiChat } from '../../../app/providers/AiChatContext';
import { formatCurrency } from '../../../utils/formatters';

export default function ProductDetailPage() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const { addToCart } = useCart();
  const { askAboutProduct } = useAiChat();

  const [product, setProduct] = useState(null);
  const [selectedVariant, setSelectedVariant] = useState(null);
  const [quantity, setQuantity] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const loadProduct = async () => {
      setIsLoading(true);
      setError(null);
      try {
        let data;
        // Check if slug is a GUID
        const isGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(slug);
        if (isGuid) {
          data = await productApi.getProductById(slug);
        } else {
          data = await productApi.getProductBySlug(slug);
        }
        setProduct(data);
        if (data.variants && data.variants.length > 0) {
          setSelectedVariant(data.variants[0]);
        }
      } catch (err) {
        console.error('Failed to load product details', err);
        setError('Product not found or failed to load.');
      } finally {
        setIsLoading(false);
      }
    };

    loadProduct();
  }, [slug]);

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-24 flex flex-col items-center justify-center space-y-4">
        <Loader2 className="w-10 h-10 text-ink animate-spin" />
        <p className="text-sm font-semibold text-ink-muted">Loading product details...</p>
      </div>
    );
  }

  if (error || !product) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-24 text-center space-y-4">
        <h2 className="text-2xl font-bold text-ink">Product Not Found</h2>
        <p className="text-sm text-ink-muted">{error || 'The requested product could not be found in our catalog.'}</p>
        <Button variant="nike-dark" size="md" onClick={() => navigate('/products')}>
          Back to Catalog
        </Button>
      </div>
    );
  }

  const activePrice = selectedVariant ? selectedVariant.price : product.basePrice;
  const inStock = selectedVariant ? selectedVariant.stockQuantity > 0 : product.inStock;

  const handleAddToCart = () => {
    addToCart(product, selectedVariant, quantity);
  };

  const handleAskAi = () => {
    askAboutProduct(product);
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12 space-y-12">
      {/* Breadcrumb navigation */}
      <nav className="flex items-center gap-2 text-xs font-semibold text-ink-muted">
        <Link to="/" className="hover:text-ink transition-colors">Home</Link>
        <ChevronRight className="w-3.5 h-3.5 text-ink-subtle" />
        <Link to="/products" className="hover:text-ink transition-colors">Products</Link>
        {product.categoryName && (
          <>
            <ChevronRight className="w-3.5 h-3.5 text-ink-subtle" />
            <Link to={`/categories/${product.categorySlug}`} className="hover:text-ink transition-colors">
              {product.categoryName}
            </Link>
          </>
        )}
        <ChevronRight className="w-3.5 h-3.5 text-ink-subtle" />
        <span className="text-ink truncate max-w-[200px]">{product.name}</span>
      </nav>

      {/* Main Top Section: Gallery + Product Buy Box */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-12 items-start">
        {/* Left: Gallery (7 Cols) */}
        <div className="lg:col-span-7">
          <ProductGallery
            images={product.images || []}
            productName={product.name}
          />
        </div>

        {/* Right: Product Buy Box (5 Cols) */}
        <div className="lg:col-span-5 space-y-6 lg:sticky lg:top-24">
          <div className="space-y-2">
            <div className="flex items-center justify-between gap-2">
              <Link
                to={`/categories/${product.categorySlug}`}
                className="text-xs font-bold uppercase tracking-wider text-accent-indigo hover:underline"
              >
                {product.categoryName}
              </Link>
              {inStock ? (
                <Badge variant="success" size="md">In Stock</Badge>
              ) : (
                <Badge variant="danger" size="md">Sold Out</Badge>
              )}
            </div>

            <h1 className="nike-heading text-2xl sm:text-4xl text-ink tracking-tight">
              {product.name}
            </h1>

            <div className="pt-2 flex items-baseline gap-3">
              <span className="text-3xl sm:text-4xl font-black text-ink tracking-tight">
                {formatCurrency(activePrice)}
              </span>
              <span className="text-xs font-mono text-ink-subtle uppercase">
                SKU: {selectedVariant ? selectedVariant.sku : product.sku}
              </span>
            </div>
          </div>

          {product.shortDescription && (
            <p className="text-xs sm:text-sm text-ink-muted leading-relaxed">
              {product.shortDescription}
            </p>
          )}

          {/* Variant Selector */}
          {product.variants && product.variants.length > 0 && (
            <div className="pt-2">
              <ProductVariantSelector
                variants={product.variants}
                selectedVariant={selectedVariant}
                onSelectVariant={setSelectedVariant}
                basePrice={product.basePrice}
              />
            </div>
          )}

          {/* Add to Cart & AI Consultation Actions */}
          <div className="space-y-3 pt-4 border-t border-surface-subtle">
            <Button
              variant="nike-dark"
              size="lg"
              onClick={handleAddToCart}
              disabled={!inStock}
              className="w-full uppercase font-bold tracking-wider text-sm h-14 shadow-lg shadow-slate-900/10"
            >
              <ShoppingBag className="w-5 h-5" />
              <span>Add to Shopping Bag</span>
            </Button>

            <Button
              variant="outline"
              size="lg"
              onClick={handleAskAi}
              className="w-full text-xs font-bold uppercase tracking-wider text-accent-indigo border-indigo-200 hover:bg-indigo-50/70 h-12 flex items-center justify-center gap-2"
            >
              <Sparkles className="w-4 h-4 text-accent-indigo" />
              <span>Ask AI Assistant About This Product</span>
            </Button>
          </div>

          {/* Guarantees & Shipping checklist */}
          <div className="pt-4 border-t border-surface-subtle space-y-3 text-xs text-ink-muted">
            <div className="flex items-center gap-2.5">
              <Truck className="w-4 h-4 text-accent-orange shrink-0" />
              <span>Free standard delivery on orders over $75</span>
            </div>
            <div className="flex items-center gap-2.5">
              <RefreshCw className="w-4 h-4 text-teal-600 shrink-0" />
              <span>Free 30-day returns & exchanges</span>
            </div>
            <div className="flex items-center gap-2.5">
              <ShieldCheck className="w-4 h-4 text-accent-indigo shrink-0" />
              <span>1-Year SPOCS Official Manufacturer Warranty</span>
            </div>
          </div>
        </div>
      </div>

      {/* Specifications & Features Tabs */}
      <ProductSpecsTable
        description={product.description}
        features={product.features}
        specifications={product.specifications}
      />
    </div>
  );
}
