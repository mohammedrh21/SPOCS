import apiClient from '../../../services/apiClient';

export const productApi = {
  /**
   * Get paginated products with filters
   */
  async getProducts(params = {}) {
    const response = await apiClient.get('/products', { params });
    return response.data;
  },

  /**
   * Get product detail by ID
   */
  async getProductById(id) {
    const response = await apiClient.get(`/products/${id}`);
    return response.data;
  },

  /**
   * Get product detail by Slug
   */
  async getProductBySlug(slug) {
    const response = await apiClient.get(`/products/slug/${slug}`);
    return response.data;
  },

  /**
   * Semantic product search powered by embeddings
   */
  async semanticSearch(query, topK = 10) {
    const response = await apiClient.get('/products/search/semantic', {
      params: { query, topK },
    });
    return response.data;
  },
};
