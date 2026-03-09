import api from './client';
import type { WalletTransaction, PaginatedResponse, PaymentMethod } from '../types';

export interface WalletBalance {
  balance: number;
  currency: string;
}

export interface TopUpRequest {
  amount: number;
  paymentMethod: PaymentMethod;
}

export interface TopUpResponse {
  transactionId: string;
  redirectUrl?: string;
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

  topUp: async (amount: number, paymentMethod: PaymentMethod): Promise<TopUpResponse> => {
    const { data } = await api.post('/wallet/topup', { amount, paymentMethod });
    return data;
  },
};
