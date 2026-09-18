import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { ShoppingBag, Sparkles, Check, ArrowRight, Loader2 } from 'lucide-react';
import Modal from '../../../components/ui/Modal';
import Button from '../../../components/ui/Button';
import Badge from '../../../components/ui/Badge';
import { productApi } from '../api/productApi';
import { useCart } from '../../../app/providers/CartContext';
import { useAiChat } from '../../../app/providers/AiChatContext';
import { formatCurrency } from '../../../utils/formatters';
import ProductVariantSelector from './ProductVariantSelector';

export default function ProductQuickViewModal({ product, isOpen, onClose }) {
  const { addToCart } = useCart();
  const { askAboutProduct } = useAiChat();

  const [detail, setDetail] = useState(null);
  const [selectedVariant, setSelectedVariant] = useState(null);
  const [quantity, setQuantity] = useState(1);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!product || !isOpen) {
      setDetail(null);
      setSelectedVariant(null);
      setQuantity(1);
      return;
    }

    const loadFullDetails = async () => {
      setIsLoading(true);
      try {
        const fullProduct = await productApi.getProductById(product.id);
        setDetail(fullProduct);
        if (fullProduct.variants && fullProduct.variants.length > 0) {
          setSelectedVariant(fullProduct.variants[0]);
        }
      } catch (err) {
        console.error('Error fetching quick view details', err);
        setDetail(product);
      } finally {
        setIsLoading(false);
      }
    };

    loadFullDetails();
  }, [product, isOpen]);

  if (!product) return null;

  const activeProduct = detail || product;
  const activePrice = selectedVariant ? selectedVariant.price : activeProduct.basePrice;
  const inStock = selectedVariant ? selectedVariant.stockQuantity > 0 : activeProduct.inStock;

  const handleAddToCart = () => {
    addToCart(activeProduct, selectedVariant, quantity);
    onClose();
  };

  const handleAskAi = () => {
    onClose();
    askAboutProduct(activeProduct);
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      maxWidth="max-w-3xl"
      title="Quick Look"
    >
      {isLoading ? (
        <div className="py-20 flex flex-col items-center justify-center space-y-3">
          <Loader2 className="w-8 h-8 text-ink animate-spin" />
          <p className="text-xs text-ink-muted">Loading product details...</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 items-start">
          {/* Product Image */}
          <div className="w-full aspect-square bg-surface-muted/60 rounded-2xl p-6 flex items-center justify-center border border-surface-subtle">
            {activeProduct.primaryImageUrl || (activeProduct.images && activeProduct.images[0]?.imageUrl) ? (
              <img
                src={activeProduct.primaryImageUrl || activeProduct.images[0]?.imageUrl}
                alt={activeProduct.name}
                className="w-full h-full object-contain mix-blend-multiply"
              />
            ) : (
              <div className="text-sm font-bold text-slate-400">SPOCS</div>
            )}
          </div>

          {/* Product Details & Actions */}
          <div className="space-y-4">
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <Badge variant="brand" size="sm">
                  {activeProduct.categoryName || 'Tech'}
                </Badge>
                {inStock ? (
                  <Badge variant="success" size="sm">In Stock</Badge>
                ) : (
                  <Badge variant="danger" size="sm">Out of Stock</Badge>
                )}
              </div>
              <h2 className="text-xl font-extrabold text-ink tracking-tight">
                {activeProduct.name}
              </h2>
              <p className="text-2xl font-black text-ink tracking-tight">
                {formatCurrency(activePrice)}
              </p>
            </div>

            {activeProduct.shortDescription && (
              <p className="text-xs text-ink-muted leading-relaxed">
                {activeProduct.shortDescription}
              </p>
            )}

            {/* Variant Selector */}
            {activeProduct.variants && activeProduct.variants.length > 0 && (
              <ProductVariantSelector
                variants={activeProduct.variants}
                selectedVariant={selectedVariant}
                onSelectVariant={setSelectedVariant}
                basePrice={activeProduct.basePrice}
              />
            )}

            {/* Actions */}
            <div className="space-y-2 pt-2 border-t border-surface-subtle">
              <Button
                variant="nike-dark"
                size="lg"
                onClick={handleAddToCart}
                disabled={!inStock}
                className="w-full uppercase font-bold tracking-wider text-xs"
              >
                <ShoppingBag className="w-4 h-4" />
                <span>Add to Shopping Bag</span>
              </Button>

              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="md"
                  onClick={handleAskAi}
                  className="flex-1 text-xs font-semibold text-accent-indigo border-indigo-200 hover:bg-indigo-50"
                >
                  <Sparkles className="w-3.5 h-3.5" />
                  <span>Ask AI Assistant</span>
                </Button>

                <Link
                  to={`/products/${activeProduct.slug || activeProduct.id}`}
                  onClick={onClose}
                  className="px-4 py-2.5 rounded-full border border-surface-subtle hover:border-ink text-xs font-bold text-ink uppercase tracking-wider flex items-center gap-1 transition-colors"
                >
                  <span>Full Details</span>
                  <ArrowRight className="w-3.5 h-3.5" />
                </Link>
              </div>
            </div>
          </div>
        </div>
      )}
    </Modal>
  );
}
