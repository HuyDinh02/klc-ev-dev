import { create } from 'zustand';
import * as Location from 'expo-location';
import { Config } from '../constants/config';

interface LocationState {
  latitude: number;
  longitude: number;
  hasPermission: boolean;
  isLoading: boolean;
  error: string | null;
  requestPermission: () => Promise<boolean>;
  updateLocation: () => Promise<void>;
  setLocation: (lat: number, lng: number) => void;
}

export const useLocationStore = create<LocationState>((set, get) => ({
  latitude: Config.DEFAULT_REGION.latitude,
  longitude: Config.DEFAULT_REGION.longitude,
  hasPermission: false,
  isLoading: false,
  error: null,

  requestPermission: async () => {
    set({ isLoading: true, error: null });
    try {
      const { status } = await Location.requestForegroundPermissionsAsync();
      const hasPermission = status === 'granted';
      set({ hasPermission, isLoading: false });

      if (hasPermission) {
        await get().updateLocation();
      }

      return hasPermission;
    } catch (error) {
      set({ error: 'Failed to get location permission', isLoading: false });
      return false;
    }
  },

  updateLocation: async () => {
    try {
      const location = await Location.getCurrentPositionAsync({
        accuracy: Location.Accuracy.Balanced,
      });
      set({
        latitude: location.coords.latitude,
        longitude: location.coords.longitude,
      });
    } catch {
      set({ error: 'Failed to get current location' });
    }
  },

  setLocation: (latitude, longitude) => set({ latitude, longitude }),
}));
