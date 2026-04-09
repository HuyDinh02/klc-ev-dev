import api from './client';
import type { ChargingSession, MeterValue, PaginatedResponse } from '../types';

export interface StartSessionRequest {
  connectorId: string;
  vehicleId?: string;
  idTag?: string;
}

export const sessionsApi = {
  start: async (request: StartSessionRequest): Promise<ChargingSession> => {
    const { data } = await api.post('/sessions/start', request);
    return data;
  },

  stop: async (sessionId: string): Promise<ChargingSession> => {
    const { data } = await api.post(`/sessions/${sessionId}/stop`);
    return data;
  },

  getById: async (sessionId: string): Promise<ChargingSession> => {
    const { data } = await api.get(`/sessions/${sessionId}`);
    return data;
  },

  getActive: async (): Promise<ChargingSession | null> => {
    try {
      const { data } = await api.get('/sessions/active');
      return data;
    } catch {
      return null;
    }
  },

  getHistory: async (cursor?: string, limit = 20): Promise<PaginatedResponse<ChargingSession>> => {
    const { data } = await api.get('/sessions/history', {
      params: { cursor, limit },
    });
    // BFF may return { items, nextCursor, hasMore } or legacy { data, pagination: { nextCursor, hasMore } }
    const items: ChargingSession[] = data.items ?? data.data ?? [];
    const nextCursor: string | undefined = data.nextCursor ?? data.pagination?.nextCursor;
    const hasMore: boolean = data.hasMore ?? data.pagination?.hasMore ?? false;
    return { items, nextCursor, hasMore };
  },

  getMeterValues: async (sessionId: string): Promise<MeterValue[]> => {
    const { data } = await api.get(`/sessions/${sessionId}/meter-values`);
    return data;
  },
};
