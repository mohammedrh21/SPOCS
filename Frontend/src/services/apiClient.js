import axios from 'axios';
import { API_BASE_URL } from '../constants/config';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

// Request Interceptor: Attach JWT Token if available
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('spocs_auth_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Handle 401 Unauthorized token expiry
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Clear token if expired
      const token = localStorage.getItem('spocs_auth_token');
      if (token) {
        localStorage.removeItem('spocs_auth_token');
        localStorage.removeItem('spocs_auth_user');
        // Dispatch custom event to notify auth context
        window.dispatchEvent(new Event('spocs_auth_logout'));
      }
    }
    return Promise.reject(error);
  }
);

export default apiClient;
