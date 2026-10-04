import apiClient from './apiClient';

/**
 * The notification inbox.
 *
 * `getUnreadCount` is the polling target: it is a single indexed count on the server, which is why the
 * provider can safely poll it on a timer. Everything else is fetched on demand.
 */
export const notificationApi = {
  getMine: async ({ unreadOnly = false, page = 1, pageSize = 20 } = {}) => {
    const response = await apiClient.get('/notifications', { params: { unreadOnly, page, pageSize } });
    return response.data;
  },

  getUnreadCount: async () => {
    const response = await apiClient.get('/notifications/unread-count');
    return response.data?.unreadCount ?? 0;
  },

  markRead: async (id) => {
    const response = await apiClient.post(`/notifications/${id}/read`);
    return response.data;
  },

  markAllRead: async () => {
    const response = await apiClient.post('/notifications/read-all');
    return response.data;
  },
};

export default notificationApi;
