import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';

// Create a fresh QueryClient for each test
export function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
    },
  });
}

// Wrapper with QueryClient provider
export function renderWithProviders(ui: React.ReactElement) {
  const queryClient = createTestQueryClient();
  return {
    ...render(
      <QueryClientProvider client={queryClient}>
        {ui}
      </QueryClientProvider>
    ),
    queryClient,
  };
}
