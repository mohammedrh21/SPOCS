import apiClient from '../../../services/apiClient';

export const chatApi = {
  /**
   * Ask the AI Shopping Assistant a question
   */
  async sendMessage({ message, history = [], topK = 6 }) {
    const response = await apiClient.post('/chat', {
      message,
      history: history.map((h) => ({
        role: h.role, // 'user' | 'assistant'
        content: h.content,
      })),
      topK,
    });
    return response.data;
  },

  /**
   * Semantic product search via embeddings
   */
  async semanticSearch(query, topK = 8) {
    const response = await apiClient.get('/products/search/semantic', {
      params: { query, topK },
    });
    return response.data;
  },
};
