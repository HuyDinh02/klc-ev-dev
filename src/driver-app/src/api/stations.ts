import api from './client';
import type { Station, Connector, PaginatedResponse } from '../types';

export interface NearbyStationsParams {
  latitude: number;
  longitude: number;
  radiusKm?: number;
  connectorType?: string;
  limit?: number;
  cursor?: string;
}

export const stationsApi = {
  getNearby: async (params: NearbyStationsParams): Promise<PaginatedResponse<Station>> => {
    const { data } = await api.get('/stations/nearby', { params });
    return data;
  },

  getById: async (id: string): Promise<Station> => {
    const { data } = await api.get(`/stations/${id}`);
    return data;
  },

  getConnectors: async (stationId: string): Promise<Connector[]> => {
    const { data } = await api.get(`/stations/${stationId}/connectors`);
    return data;
  },
};
