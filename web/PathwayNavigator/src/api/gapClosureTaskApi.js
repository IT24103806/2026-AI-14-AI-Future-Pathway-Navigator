import apiClient from './apiClient';
export const gapClosureTaskApi = {
  list: async (reviewId) => (await apiClient.get('/gap-closure-tasks', { params: { reviewId } })).data,
  create: async (pathwayReviewId, title) => (await apiClient.post('/gap-closure-tasks', { pathwayReviewId, title, status: 'ToDo' })).data,
  update: async (id, task) => (await apiClient.put(`/gap-closure-tasks/${id}`, { title: task.title, status: task.status, dueDate: task.dueDate })).data,
  remove: async (id) => apiClient.delete(`/gap-closure-tasks/${id}`),
};
