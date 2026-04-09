import api from './client';
import type { WalletTransaction, PaginatedResponse, PaymentMethod } from '../types';

export interface WalletBalance {
  balance: number;
  currency: string;
}

export interface TopUpRequest {
  amount: number;
  gateway: string;
  bankCode?: string;
}

export interface TopUpResponse {
  success: boolean;
  transactionId?: string;
  redirectUrl?: string;
  error?: string;
}

export const walletApi = {
  getBalance: async (): Promise<WalletBalance> => {
    const { data } = await api.get('/wallet/balance');
    return data;
  },

  getTransactions: async (cursor?: string, limit = 20): Promise<PaginatedResponse<WalletTransaction>> => {
    const { data } = await api.get('/wallet/transactions', {
      params: { cursor, limit },
    });
    return data;
  },

  topUp: async (amount: number, gateway: string, bankCode?: string): Promise<TopUpResponse> => {
    const { data } = await api.post('/wallet/topup', { amount, gateway, bankCode });
    return data;
  },
};
