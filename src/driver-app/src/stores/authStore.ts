import { create } from 'zustand';
import * as SecureStore from 'expo-secure-store';
import type { UserProfile } from '../types';

interface AuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: UserProfile | null;
  token: string | null;
  setUser: (user: UserProfile | null) => void;
  setToken: (token: string | null) => void;
  login: (token: string, user: UserProfile) => Promise<void>;
  logout: () => Promise<void>;
  checkAuth: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  isAuthenticated: false,
  isLoading: true,
  user: null,
  token: null,

  setUser: (user) => set({ user }),

  setToken: (token) => set({ token, isAuthenticated: !!token }),

  login: async (token, user) => {
    await SecureStore.setItemAsync('authToken', token);
    set({ token, user, isAuthenticated: true, isLoading: false });
  },

  logout: async () => {
    await SecureStore.deleteItemAsync('authToken');
    set({ token: null, user: null, isAuthenticated: false });
  },

  checkAuth: async () => {
    try {
      const token = await SecureStore.getItemAsync('authToken');
      if (token) {
        set({ token, isAuthenticated: true, isLoading: false });
      } else {
        set({ isAuthenticated: false, isLoading: false });
      }
    } catch {
      set({ isAuthenticated: false, isLoading: false });
    }
  },
}));
