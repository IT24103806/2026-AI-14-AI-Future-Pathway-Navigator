import apiClient from './apiClient';
import { getApiErrorMessage } from '../utils/apiError';

/**
 * Student side of the consultation channel.
 *
 * A question is always anchored to what the student was looking at (`contextType` + `contextRefId`),
 * so the consultant sees the same numbers and the answer can be written back into that component.
 */
export const consultationApi = {
  create: async ({ contextType = 'General', contextRefId = null, category, priority, subject, body }) => {
    const response = await apiClient.post('/consultations', {
      contextType,
      contextRefId,
      category,
      priority,
      subject,
      body,
    });
    return response.data;
  },

  getMine: async ({ status = '', page = 1, pageSize = 10 } = {}) => {
    const response = await apiClient.get('/consultations/me', {
      params: { status: status || undefined, page, pageSize },
    });
    return response.data;
  },

  getById: async (id) => {
    const response = await apiClient.get(`/consultations/${id}`);
    return response.data;
  },

  addMessage: async (id, body) => {
    const response = await apiClient.post(`/consultations/${id}/messages`, { body });
    return response.data;
  },

  close: async (id, { rating, feedback } = {}) => {
    const response = await apiClient.post(`/consultations/${id}/close`, { rating, feedback });
    return response.data;
  },

  reopen: async (id) => {
    const response = await apiClient.post(`/consultations/${id}/reopen`);
    return response.data;
  },

  /** "You already asked about this" badge for a journey component. */
  getContextStatus: async (contextType, refId) => {
    if (!refId) return { hasOpenRequest: false };
    try {
      const response = await apiClient.get(`/consultations/context/${contextType}/${refId}`);
      return response.data;
    } catch {
      return { hasOpenRequest: false };
    }
  },

  /** FAQ suggestions shown before a request is created. Never blocks the "ask a human" path. */
  matchFaq: async (question) => {
    try {
      const response = await apiClient.get('/consultations/faq-match', { params: { question } });
      return response.data;
    } catch {
      return { matches: [] };
    }
  },

  describeError: (error, fallback) => getApiErrorMessage(error, fallback),
};

export default consultationApi;
