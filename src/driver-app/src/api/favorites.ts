import api from './client';
import type { Station } from '../types';

export const favoritesApi = {
  getAll: async (): Promise<Station[]> => {
    const { data } = await api.get('/api/v1/favorites');
    return data;
  },

  add: async (stationId: string): Promise<void> => {
    await api.post('/api/v1/favorites', { stationId });
  },

  remove: async (stationId: string): Promise<void> => {
    await api.delete(`/api/v1/favorites/${stationId}`);
  },
};
