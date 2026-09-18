import apiClient from '../../../services/apiClient';

export const cartApi = {
  /**
   * Get the current customer's cart
   */
  async getCart() {
    const response = await apiClient.get('/cart');
    return response.data;
  },

  /**
   * Add item to cart
   */
  async addToCart({ productId, variantId, quantity = 1 }) {
    const response = await apiClient.post('/cart/items', {
      productId,
      variantId: variantId || null,
      quantity,
    });
    return response.data;
  },

  /**
   * Update item quantity in cart
   */
  async updateCartItem(itemId, quantity) {
    const response = await apiClient.put(`/cart/items/${itemId}`, { quantity });
    return response.data;
  },

  /**
   * Remove item from cart
   */
  async removeFromCart(itemId) {
    const response = await apiClient.delete(`/cart/items/${itemId}`);
    return response.data;
  },

  /**
   * Clear all items from cart
   */
  async clearCart() {
    await apiClient.delete('/cart');
  },
};
