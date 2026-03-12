import api from './client';
import type { PaginatedResponse } from '../types';

export interface FaqItem {
  question: string;
  answer: string;
  category?: string;
}

export interface SubmitFeedbackRequest {
  type: number;
  subject: string;
  message: string;
}

export interface FeedbackItem {
  id: string;
  type: number;
  subject: string;
  message: string;
  status: number;
  createdAt: string;
  response?: string;
}

export const feedbackApi = {
  getFaq: async (): Promise<FaqItem[]> => {
    const { data } = await api.get('/support/faq');
    return data;
  },

  submitFeedback: async (request: SubmitFeedbackRequest): Promise<void> => {
    await api.post('/feedback', request);
  },

  getHistory: async (cursor?: string, limit = 20): Promise<PaginatedResponse<FeedbackItem>> => {
    const { data } = await api.get('/feedback', {
      params: { cursor, limit },
    });
    return data;
  },
};
