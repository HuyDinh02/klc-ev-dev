import api from './client';
import type { UserProfile } from '../types';

export interface LoginRequest {
  phoneNumber: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  accessToken?: string;
  refreshToken?: string;
  expiresIn?: number;
  user?: {
    userId: string;
    fullName: string;
    phoneNumber?: string;
    email?: string;
    avatarUrl?: string;
    isPhoneVerified: boolean;
    membershipTier?: number;
    walletBalance?: number;
  };
  error?: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export function mapAuthUserToProfile(user: LoginResponse['user']): UserProfile | null {
  if (!user) return null;
  return {
    id: user.userId,
    email: user.email ?? '',
    phoneNumber: user.phoneNumber,
    fullName: user.fullName,
    avatarUrl: user.avatarUrl,
    isPhoneVerified: user.isPhoneVerified,
    isEmailVerified: !!user.email,
  };
}

export interface RegisterRequest {
  phoneNumber: string;
  fullName: string;
  password: string;
}

export interface RegisterResponse {
  success: boolean;
  userId?: string;
  message?: string;
  error?: { code: string; message: string };
}

export interface VerifyOtpRequest {
  phoneNumber: string;
  otp: string;
}

export interface VerifyOtpResponse {
  success: boolean;
  error?: { code: string; message: string };
}

export interface ResendOtpRequest {
  phoneNumber: string;
}

export const authApi = {
  login: async (request: LoginRequest): Promise<LoginResponse> => {
    const { data } = await api.post<LoginResponse>('/auth/login', request);
    return data;
  },

  register: async (request: RegisterRequest): Promise<RegisterResponse> => {
    const { data } = await api.post<RegisterResponse>('/auth/register', request);
    return data;
  },

  verifyOtp: async (request: VerifyOtpRequest): Promise<VerifyOtpResponse> => {
    const { data } = await api.post<VerifyOtpResponse>('/auth/verify-phone', request);
    return data;
  },

  resendOtp: async (request: ResendOtpRequest): Promise<void> => {
    await api.post('/auth/resend-otp', request);
  },

  refreshToken: async (request: RefreshTokenRequest): Promise<LoginResponse> => {
    const { data } = await api.post<LoginResponse>('/auth/refresh-token', request);
    return data;
  },

  logout: async (refreshToken?: string): Promise<void> => {
    await api.post('/auth/logout', { refreshToken: refreshToken ?? '' });
  },
};
