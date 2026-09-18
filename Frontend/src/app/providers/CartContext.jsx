import React, { createContext, useContext, useState, useEffect, useCallback, useMemo } from 'react';
import { cartApi } from '../../features/cart/api/cartApi';
import { useAuth } from './AuthContext';
import { useToast } from './ToastContext';

const CartContext = createContext(null);

export function CartProvider({ children }) {
  const { isAuthenticated, user, openAuthModal } = useAuth();
  const toast = useToast();

  const [cart, setCart] = useState(null);
  const [isCartOpen, setIsCartOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  // Fetch cart from backend if user is authenticated
  const fetchCart = useCallback(async () => {
    if (!isAuthenticated) {
      // Local fallback for guest
      const localCart = localStorage.getItem('spocs_guest_cart');
      if (localCart) {
        try {
          setCart(JSON.parse(localCart));
        } catch {
          setCart({ items: [] });
        }
      } else {
        setCart({ items: [] });
      }
      return;
    }

    try {
      setIsLoading(true);
      const remoteCart = await cartApi.getCart();
      setCart(remoteCart);
    } catch (err) {
      console.warn('Could not fetch cart from server', err);
    } finally {
      setIsLoading(false);
    }
  }, [isAuthenticated]);

  useEffect(() => {
    fetchCart();
  }, [fetchCart, isAuthenticated]);

  const openCart = () => setIsCartOpen(true);
  const closeCart = () => setIsCartOpen(false);
  const toggleCart = () => setIsCartOpen((prev) => !prev);

  // Add Item
  const addToCart = async (product, variant = null, quantity = 1) => {
    if (!isAuthenticated) {
      // If user isn't logged in, prompt login or allow guest cart
      openAuthModal('login');
      toast.info('Please log in to add items to your cart.');
      return;
    }

    try {
      setIsLoading(true);
      const updatedCart = await cartApi.addToCart({
        productId: product.id,
        variantId: variant ? variant.id : null,
        quantity,
      });
      setCart(updatedCart);
      toast.success(`Added "${product.name}" to your cart!`);
      openCart();
    } catch (err) {
      const errorMsg = err.response?.data?.message || 'Failed to add item to cart.';
      toast.error(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  // Update Item Quantity
  const updateQuantity = async (itemId, newQuantity) => {
    if (!isAuthenticated) return;
    if (newQuantity < 1) {
      await removeItem(itemId);
      return;
    }

    try {
      setIsLoading(true);
      const updatedCart = await cartApi.updateCartItem(itemId, newQuantity);
      setCart(updatedCart);
    } catch (err) {
      const errorMsg = err.response?.data?.message || 'Failed to update quantity.';
      toast.error(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  // Remove Item
  const removeItem = async (itemId) => {
    if (!isAuthenticated) return;
    try {
      setIsLoading(true);
      const updatedCart = await cartApi.removeFromCart(itemId);
      setCart(updatedCart);
      toast.info('Item removed from cart');
    } catch (err) {
      const errorMsg = err.response?.data?.message || 'Failed to remove item.';
      toast.error(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  // Clear Cart
  const clearCart = async () => {
    if (!isAuthenticated) return;
    try {
      setIsLoading(true);
      await cartApi.clearCart();
      setCart({ items: [], customerId: user?.id });
    } catch (err) {
      console.error('Error clearing cart', err);
    } finally {
      setIsLoading(false);
    }
  };

  const items = cart?.items || [];

  const totalItemsCount = useMemo(() => {
    return items.reduce((sum, item) => sum + (item.quantity || 0), 0);
  }, [items]);

  const subtotal = useMemo(() => {
    return items.reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);
  }, [items]);

  return (
    <CartContext.Provider
      value={{
        cart,
        items,
        totalItemsCount,
        subtotal,
        isCartOpen,
        isLoading,
        openCart,
        closeCart,
        toggleCart,
        addToCart,
        updateQuantity,
        removeItem,
        clearCart,
        refreshCart: fetchCart,
      }}
    >
      {children}
    </CartContext.Provider>
  );
}

export function useCart() {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error('useCart must be used within a CartProvider');
  }
  return context;
}
