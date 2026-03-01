import api from './client';
import type { Payment, PaymentMethodInfo, PaymentMethod, PaginatedResponse } from '../types';

export interface ProcessPaymentRequest {
  sessionId: string;
  paymentMethodId?: string;
  paymentMethod?: PaymentMethod;
}

export interface AddPaymentMethodRequest {
  type: PaymentMethod;
  token?: string;
}

export const paymentsApi = {
  process: async (request: ProcessPaymentRequest): Promise<Payment> => {
    const { data } = await api.post('/payments/process', request);
    return data;
  },

  getById: async (paymentId: string): Promise<Payment> => {
    const { data } = await api.get(`/payments/${paymentId}`);
    return data;
  },

  getHistory: async (cursor?: string, limit = 20): Promise<PaginatedResponse<Payment>> => {
    const { data } = await api.get('/payments/history', {
      params: { cursor, limit },
    });
    return data;
  },

  getMethods: async (): Promise<PaymentMethodInfo[]> => {
    const { data } = await api.get('/payment-methods');
    return data;
  },

  addMethod: async (request: AddPaymentMethodRequest): Promise<PaymentMethodInfo> => {
    const { data } = await api.post('/payment-methods', request);
    return data;
  },

  deleteMethod: async (methodId: string): Promise<void> => {
    await api.delete(`/payment-methods/${methodId}`);
  },

  setDefaultMethod: async (methodId: string): Promise<void> => {
    await api.post(`/payment-methods/${methodId}/set-default`);
  },
};
