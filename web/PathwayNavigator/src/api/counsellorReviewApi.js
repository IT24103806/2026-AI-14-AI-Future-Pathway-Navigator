import apiClient from './apiClient';

export const counsellorReviewApi = {
  // Pending review ලැයිස්තුව ලබා ගැනීම
  getPendingReviews: async () => {
    const response = await apiClient.get('/CounsellorReview/pending');
    return response.data;
  },

  // Review එකක තොරතුරු ලබා ගැනීම
  getReviewById: async (id) => {
    const response = await apiClient.get(`/CounsellorReview/${id}`);
    return response.data;
  },

  // Counsellor තීරණය submit කිරීම (Approve / Reject / NeedsRevision)
  submitDecision: async (id, decision, feedback) => {
    const response = await apiClient.post(`/CounsellorReview/${id}/decision`, {
      decision,
      feedback,
    });
    return response.data;
  },
};