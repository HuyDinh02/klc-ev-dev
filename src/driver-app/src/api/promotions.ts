import api from './client';
import type { Promotion, PaginatedResponse } from '../types';

export const promotionsApi = {
  getPromotions: async (cursor?: string, limit = 20): Promise<PaginatedResponse<Promotion>> => {
    const { data } = await api.get('/promotions', {
      params: { cursor, limit },
    });
    return data;
  },

  getPromotion: async (id: string): Promise<Promotion> => {
    const { data } = await api.get(`/promotions/${id}`);
    return data;
  },
};
