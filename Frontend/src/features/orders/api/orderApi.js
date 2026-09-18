import apiClient from '../../../services/apiClient';

export const orderApi = {
  /**
   * Create an order from current cart
   */
  async createOrder(data) {
    const response = await apiClient.post('/orders', data);
    return response.data;
  },

  /**
   * Get all orders for the current customer
   */
  async getMyOrders() {
    const response = await apiClient.get('/orders');
    return response.data;
  },

  /**
   * Get order details by ID
   */
  async getOrderById(id) {
    const response = await apiClient.get(`/orders/${id}`);
    return response.data;
  },
};
