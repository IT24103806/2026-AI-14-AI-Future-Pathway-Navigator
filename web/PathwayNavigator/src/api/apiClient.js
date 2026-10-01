import axios from 'axios';
import { STORAGE_KEYS } from '../config/constants';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5081/api',
  headers: {
    'Content-Type': 'application/json',
  },
  // Agent calls (LLM + market-data lookups) are proxied through the .NET API and can take well
  // over 10s; the gateway's own AI-service timeout is 90s, so allow a little longer here.
  timeout: Number(import.meta.env.VITE_API_TIMEOUT_MS) || 120000,
});

// Request Interceptor: Automatically attach Bearer token if available
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN);
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response Interceptor: Standard error formatting & handle 401 Unauthorized
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Clear expired token if 401 unauthorized
      localStorage.removeItem(STORAGE_KEYS.TOKEN);
      localStorage.removeItem(STORAGE_KEYS.USER);
    }
    return Promise.reject(error);
  }
);

export default apiClient;