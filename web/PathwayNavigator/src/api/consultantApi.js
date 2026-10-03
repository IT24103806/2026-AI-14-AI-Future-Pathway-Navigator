import apiClient from './apiClient';

/**
 * The Consultant Desk API.
 *
 * Scope is enforced server-side: a Consultant sees the unclaimed pool plus their own caseload and can
 * never reach the counsellor approval queue.
 */
export const consultantApi = {
  getQueue: async ({ scope = 'Open', category = '', priority = '', contextType = '', search = '', sort = 'sla', page = 1, pageSize = 10 } = {}) => {
    const response = await apiClient.get('/consultant/queue', {
      params: {
        scope,
        category: category || undefined,
        priority: priority || undefined,
        contextType: contextType || undefined,
        search: search || undefined,
        sort,
        page,
        pageSize,
      },
    });
    return response.data;
  },

  getCase: async (id) => {
    const response = await apiClient.get(`/consultant/queue/${id}`);
    return response.data;
  },

  claim: async (id) => {
    const response = await apiClient.post(`/consultant/queue/${id}/claim`);
    return response.data;
  },

  release: async (id, reason) => {
    const response = await apiClient.post(`/consultant/queue/${id}/release`, { reason });
    return response.data;
  },

  reply: async (id, payload) => {
    const response = await apiClient.post(`/consultant/queue/${id}/reply`, payload);
    return response.data;
  },

  addNote: async (id, body) => {
    const response = await apiClient.post(`/consultant/queue/${id}/note`, { body });
    return response.data;
  },

  updatePriority: async (id, priority) => {
    const response = await apiClient.post(`/consultant/queue/${id}/priority`, { priority });
    return response.data;
  },

  escalate: async (id, reason) => {
    const response = await apiClient.post(`/consultant/queue/${id}/escalate`, { reason });
    return response.data;
  },

  /** Agent 5 case brief. Resolves to null when the copilot is unavailable. */
  getBrief: async (id) => {
    try {
      const response = await apiClient.get(`/consultant/queue/${id}/brief`);
      return response.data;
    } catch {
      return null;
    }
  },

  /** Agent 5 draft reply. The consultant edits it; nothing is sent automatically. */
  getDraft: async (id, tone = 'Supportive') => {
    try {
      const response = await apiClient.post(`/consultant/queue/${id}/draft`, { tone });
      return response.data;
    } catch {
      return null;
    }
  },

  getStats: async () => {
    const response = await apiClient.get('/consultant/me/stats');
    return response.data;
  },

  getProfile: async () => {
    const response = await apiClient.get('/consultant/me/profile');
    return response.data;
  },

  updateProfile: async (payload) => {
    const response = await apiClient.put('/consultant/me/profile', payload);
    return response.data;
  },
};

export default consultantApi;
