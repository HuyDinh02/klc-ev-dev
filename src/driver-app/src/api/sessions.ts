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
    return data;
  },

  getMeterValues: async (sessionId: string): Promise<MeterValue[]> => {
    const { data } = await api.get(`/sessions/${sessionId}/meter-values`);
    return data;
  },
};
