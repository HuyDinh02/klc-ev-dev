import api from './client';
import type { UserProfile, UserStatistics, Vehicle } from '../types';

export interface UpdateProfileRequest {
  fullName?: string;
  avatarUrl?: string;
}

export interface AddVehicleRequest {
  make: string;
  model: string;
  year: number;
  licensePlate: string;
  batteryCapacityKwh: number;
  connectorType: string;
}

export const profileApi = {
  get: async (): Promise<UserProfile> => {
    const { data } = await api.get('/profile');
    return data;
  },

  update: async (request: UpdateProfileRequest): Promise<UserProfile> => {
    const { data } = await api.put('/profile', request);
    return data;
  },

  getStatistics: async (): Promise<UserStatistics> => {
    const { data } = await api.get('/profile/statistics');
    return data;
  },
};

export const vehiclesApi = {
  getAll: async (): Promise<Vehicle[]> => {
    const { data } = await api.get('/vehicles');
    return data;
  },

  getDefault: async (): Promise<Vehicle | null> => {
    try {
      const { data } = await api.get('/vehicles/default');
      return data;
    } catch {
      return null;
    }
  },

  getById: async (vehicleId: string): Promise<Vehicle> => {
    const { data } = await api.get(`/vehicles/${vehicleId}`);
    return data;
  },

  add: async (request: AddVehicleRequest): Promise<Vehicle> => {
    const { data } = await api.post('/vehicles', request);
    return data;
  },

  update: async (vehicleId: string, request: Partial<AddVehicleRequest>): Promise<Vehicle> => {
    const { data } = await api.put(`/vehicles/${vehicleId}`, request);
    return data;
  },

  delete: async (vehicleId: string): Promise<void> => {
    await api.delete(`/vehicles/${vehicleId}`);
  },

  setDefault: async (vehicleId: string): Promise<void> => {
    await api.post(`/vehicles/${vehicleId}/set-default`);
  },
};
