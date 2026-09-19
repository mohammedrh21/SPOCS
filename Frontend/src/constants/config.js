const defaultBaseUrl = 'https://' + 'spocs-api.onrender.com/api';
const rawBaseUrl = import.meta.env.VITE_API_BASE_URL || defaultBaseUrl;
const cleanBaseUrl = rawBaseUrl.replace(/\/+$/, '');
export const API_BASE_URL = cleanBaseUrl.endsWith('/api') ? cleanBaseUrl : `${cleanBaseUrl}/api`;

export const DEMO_USER = {
  email: 'customer@spocs.com',
  password: 'Customer123!',
};

export const POPULAR_PROMPTS = [
  "I need a laptop for programming under $1500",
  "Which smartphones have the best camera and battery life?",
  "Show me wireless noise-cancelling headphones for travel",
  "Recommend a smartwatch with health tracking and GPS",
];

export const CATEGORY_HERO_IMAGES = {
  'laptops': 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80',
  'smartphones': 'https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=800&auto=format&fit=crop&q=80',
  'audio-headphones': 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80',
  'wearables-smartwatches': 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80',
  'tablets': 'https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800&auto=format&fit=crop&q=80',
  'gaming': 'https://images.unsplash.com/photo-1538481199705-c710c4e965fc?w=800&auto=format&fit=crop&q=80',
  'cameras': 'https://images.unsplash.com/photo-1516035069371-29a1b244cc32?w=800&auto=format&fit=crop&q=80',
  'accessories': 'https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=800&auto=format&fit=crop&q=80',
};
