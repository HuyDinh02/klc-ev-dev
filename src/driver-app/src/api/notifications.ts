import api from './client';
import type { Notification, NotificationPreferences, PaginatedResponse } from '../types';

export const notificationsApi = {
  getAll: async (cursor?: string, limit = 20): Promise<PaginatedResponse<Notification>> => {
    const { data } = await api.get('/notifications', {
      params: { cursor, limit },
    });
    return data;
  },

  getUnreadCount: async (): Promise<number> => {
    const { data } = await api.get('/notifications/unread-count');
    return data.count;
  },

  markAsRead: async (notificationId: string): Promise<void> => {
    await api.put(`/notifications/${notificationId}/read`);
  },

  markAllAsRead: async (): Promise<void> => {
    await api.put('/notifications/read-all');
  },

  registerDevice: async (fcmToken: string, platform?: string): Promise<void> => {
    await api.post('/devices/register', { fcmToken, platform });
  },

  unregisterDevice: async (token: string): Promise<void> => {
    await api.delete(`/devices/${encodeURIComponent(token)}`);
  },

  getPreferences: async (): Promise<NotificationPreferences> => {
    const { data } = await api.get('/notifications/preferences');
    return data;
  },

  updatePreferences: async (preferences: NotificationPreferences): Promise<NotificationPreferences> => {
    const { data } = await api.put('/notifications/preferences', preferences);
    return data;
  },
};
