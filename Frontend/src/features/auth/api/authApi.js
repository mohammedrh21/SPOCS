import apiClient from '../../../services/apiClient';

export const authApi = {
  /**
   * Log in user
   */
  async login({ email, password }) {
    const response = await apiClient.post('/auth/login', { email, password });
    return response.data;
  },

  /**
   * Register new customer account
   */
  async register(data) {
    const response = await apiClient.post('/auth/register', data);
    return response.data;
  },

  /**
   * Get current authenticated user profile
   */
  async getCurrentUser() {
    const response = await apiClient.get('/auth/me');
    return response.data;
  },
};
