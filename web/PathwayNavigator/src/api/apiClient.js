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

// --- Transient-failure retry -------------------------------------------------------------------
// The API already retries dropped PostgreSQL connections server-side. This is the second line of
// defence for the moments where the API itself is briefly unreachable (restart, cold start, a
// proxy 502/503/504). Only repeats requests that are safe to repeat: every idempotent verb, plus
// POSTs that opted in with `{ retryOnTransient: true }` (sign-in is read-only).
const TRANSIENT_RETRY_STATUSES = new Set([502, 503, 504]);
const IDEMPOTENT_METHODS = new Set(['get', 'head', 'options']);
const MAX_TRANSIENT_RETRIES = 2;
const RETRY_BASE_DELAY_MS = 500;

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

const isTransientFailure = (error) => {
  if (error?.response) return TRANSIENT_RETRY_STATUSES.has(error.response.status);
  // No response at all: the request never reached the API (connection refused / server restarting).
  return error?.code === 'ERR_NETWORK';
};

const canRetryTransient = (error) => {
  const config = error?.config;
  if (!config) return false;
  if ((config.__transientRetryCount || 0) >= MAX_TRANSIENT_RETRIES) return false;

  const method = (config.method || 'get').toLowerCase();
  const safeToRepeat = IDEMPOTENT_METHODS.has(method) || config.retryOnTransient === true;
  return safeToRepeat && isTransientFailure(error);
};

// Response Interceptor: retry transient failures, standard error handling & 401 logout
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (canRetryTransient(error)) {
      const config = error.config;
      config.__transientRetryCount = (config.__transientRetryCount || 0) + 1;
      await delay(RETRY_BASE_DELAY_MS * config.__transientRetryCount);
      return apiClient.request(config);
    }

    if (error.response?.status === 401) {
      // Clear expired token if 401 unauthorized
      localStorage.removeItem(STORAGE_KEYS.TOKEN);
      localStorage.removeItem(STORAGE_KEYS.USER);
    }
    return Promise.reject(error);
  }
);

export default apiClient;
