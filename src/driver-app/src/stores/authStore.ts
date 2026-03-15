import { create } from 'zustand';
import * as SecureStore from 'expo-secure-store';
import type { UserProfile } from '../types';
import { registerForPushNotifications, unregisterPushNotifications } from '../services/notifications';

interface AuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: UserProfile | null;
  token: string | null;
  refreshToken: string | null;
  setUser: (user: UserProfile | null) => void;
  setToken: (token: string | null) => void;
  login: (token: string, refreshToken: string, user: UserProfile) => Promise<void>;
  logout: () => Promise<void>;
  checkAuth: () => Promise<void>;
  updateTokens: (token: string, refreshToken: string) => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  isAuthenticated: false,
  isLoading: true,
  user: null,
  token: null,
  refreshToken: null,

  setUser: (user) => set({ user }),

  setToken: (token) => set({ token, isAuthenticated: !!token }),

  login: async (token, refreshToken, user) => {
    await SecureStore.setItemAsync('authToken', token);
    await SecureStore.setItemAsync('refreshToken', refreshToken);
    set({ token, refreshToken, user, isAuthenticated: true, isLoading: false });

    // Register device for push notifications after login (non-blocking)
    registerForPushNotifications().catch(() => {
      // Silently ignore — push registration is best-effort
    });
  },

  logout: async () => {
    // Unregister device from push notifications before clearing tokens
    await unregisterPushNotifications();

    await SecureStore.deleteItemAsync('authToken');
    await SecureStore.deleteItemAsync('refreshToken');
    set({ token: null, refreshToken: null, user: null, isAuthenticated: false });
  },

  checkAuth: async () => {
    try {
      const token = await SecureStore.getItemAsync('authToken');
      const refreshToken = await SecureStore.getItemAsync('refreshToken');
      if (token) {
        set({ token, refreshToken, isAuthenticated: true, isLoading: false });
      } else {
        set({ isAuthenticated: false, isLoading: false });
      }
    } catch {
      set({ isAuthenticated: false, isLoading: false });
    }
  },

  updateTokens: async (token, refreshToken) => {
    await SecureStore.setItemAsync('authToken', token);
    await SecureStore.setItemAsync('refreshToken', refreshToken);
    set({ token, refreshToken });
  },
}));
