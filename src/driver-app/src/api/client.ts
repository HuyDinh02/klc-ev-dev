import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import * as SecureStore from 'expo-secure-store';
import { Config } from '../constants/config';
import type { ApiError } from '../types';

const api = axios.create({
  baseURL: Config.API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - add auth token
api.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    const token = await SecureStore.getItemAsync('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor - handle errors
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiError>) => {
    if (error.response?.status === 401) {
      // Clear token and redirect to login
      await SecureStore.deleteItemAsync('authToken');
      // Navigation to login will be handled by auth state
    }
    return Promise.reject(error);
  }
);

export default api;

// Auth helpers
export const setAuthToken = async (token: string) => {
  await SecureStore.setItemAsync('authToken', token);
};

export const clearAuthToken = async () => {
  await SecureStore.deleteItemAsync('authToken');
};

export const getAuthToken = async () => {
  return SecureStore.getItemAsync('authToken');
};
