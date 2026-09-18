import apiClient from '../../../services/apiClient';

export const categoryApi = {
  /**
   * Get all product categories
   */
  async getCategories() {
    const response = await apiClient.get('/categories');
    return response.data;
  },

  /**
   * Get category by ID
   */
  async getCategoryById(id) {
    const response = await apiClient.get(`/categories/${id}`);
    return response.data;
  },

  /**
   * Get category by slug
   */
  async getCategoryBySlug(slug) {
    const response = await apiClient.get(`/categories/slug/${slug}`);
    return response.data;
  },
};
