import apiClient from './apiClient';

export const counsellorReviewApi = {
  startRealityCheck: async (analysisId, input) => {
    const response = await apiClient.post(`/counsellor-review/analysis/${analysisId}/evaluate`, input);
    return response.data;
  },
  resubmitRealityCheck: async (id, input) => {
    const response = await apiClient.post(`/counsellor-review/${id}/resubmit`, input);
    return response.data;
  },
  getMyStatus: async () => {
    const response = await apiClient.get('/counsellor-review/me/status');
    return response.data;
  },
  getMyHistory: async () => {
    const response = await apiClient.get('/counsellor-review/me/history');
    return response.data;
  },
  // Pending review ලැයිස්තුව ලබා ගැනීම
  getReviews: async ({ status = 'Pending', search = '', sort = 'newest', page = 1, pageSize = 10 } = {}) => {
    const response = await apiClient.get('/counsellor-review', {
      params: { status, search: search || undefined, sort, page, pageSize },
    });
    return response.data;
  },

  // Review එකක තොරතුරු ලබා ගැනීම
  getReviewById: async (id) => {
    const response = await apiClient.get(`/counsellor-review/${id}`);
    return response.data;
  },

  // Counsellor තීරණය submit කිරීම (Approve / Reject / NeedsRevision)
  submitDecision: async (id, decision, feedback) => {
    const response = await apiClient.post(`/counsellor-review/${id}/decision`, {
      decision,
      feedback,
    });
    return response.data;
  },
};
