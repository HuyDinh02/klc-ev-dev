import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/feedback',
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const mockApiGet = vi.fn();
const mockApiPut = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
    put: (url: string, data?: unknown) => mockApiPut(url, data),
  },
}));

import FeedbackPage from '../page';

const mockFeedbackList = [
  {
    id: 'fb-1',
    userId: 'user-abc12345-6789',
    userName: 'Nguyen Van A',
    type: 0,
    subject: 'Charger not working',
    message: 'The charger at station X is broken',
    status: 0,
    adminResponse: null,
    respondedAt: null,
    createdAt: '2026-03-08T10:00:00Z',
  },
  {
    id: 'fb-2',
    userId: 'user-def67890-1234',
    userName: 'Tran Thi B',
    type: 1,
    subject: 'Add Apple Pay support',
    message: 'Please add Apple Pay as a payment option',
    status: 1,
    adminResponse: null,
    respondedAt: null,
    createdAt: '2026-03-07T14:00:00Z',
  },
  {
    id: 'fb-3',
    userId: 'user-ghi11111-2222',
    userName: 'Le Van C',
    type: 2,
    subject: 'Session stopped unexpectedly',
    message: 'My charging session ended after 10 minutes',
    status: 2,
    adminResponse: 'We have fixed the issue.',
    respondedAt: '2026-03-06T16:00:00Z',
    createdAt: '2026-03-06T09:00:00Z',
  },
];

describe('FeedbackPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiGet.mockImplementation((url: string) => {
      if (url === '/admin/feedback') {
        return Promise.resolve({ data: { data: mockFeedbackList } });
      }
      if (url.startsWith('/admin/feedback/')) {
        const id = url.split('/').pop();
        const fb = mockFeedbackList.find((f) => f.id === id);
        return Promise.resolve({ data: fb || mockFeedbackList[0] });
      }
      return Promise.resolve({ data: {} });
    });
    mockApiPut.mockResolvedValue({ data: {} });
  });

  it('renders feedback page title', async () => {
    renderWithProviders(<FeedbackPage />);
    expect(screen.getByText('Feedback Management')).toBeInTheDocument();
  });

  it('renders feedback table with data', async () => {
    renderWithProviders(<FeedbackPage />);
    await waitFor(() => {
      expect(screen.getByText('Charger not working')).toBeInTheDocument();
    });
    expect(screen.getByText('Add Apple Pay support')).toBeInTheDocument();
    expect(screen.getByText('Session stopped unexpectedly')).toBeInTheDocument();
  });

  it('renders user names', async () => {
    renderWithProviders(<FeedbackPage />);
    await waitFor(() => {
      expect(screen.getByText('Nguyen Van A')).toBeInTheDocument();
    });
    expect(screen.getByText('Tran Thi B')).toBeInTheDocument();
    expect(screen.getByText('Le Van C')).toBeInTheDocument();
  });

  it('renders feedback type labels', async () => {
    renderWithProviders(<FeedbackPage />);
    await waitFor(() => {
      expect(screen.getByText('Bug')).toBeInTheDocument();
    });
    expect(screen.getByText('Feature Request')).toBeInTheDocument();
    expect(screen.getByText('Charging Issue')).toBeInTheDocument();
  });

  it('renders status filter buttons', async () => {
    renderWithProviders(<FeedbackPage />);
    expect(screen.getByText('All')).toBeInTheDocument();
    expect(screen.getByText('Open')).toBeInTheDocument();
    expect(screen.getByText('In Review')).toBeInTheDocument();
    expect(screen.getByText('Resolved')).toBeInTheDocument();
    expect(screen.getByText('Closed')).toBeInTheDocument();
  });

  it('shows empty state when no feedback', async () => {
    mockApiGet.mockResolvedValue({ data: { data: [] } });
    renderWithProviders(<FeedbackPage />);
    await waitFor(() => {
      expect(screen.getByText('No feedback found')).toBeInTheDocument();
    });
  });

  it('calls API on page load', async () => {
    renderWithProviders(<FeedbackPage />);
    await waitFor(() => {
      expect(mockApiGet).toHaveBeenCalledWith(
        '/admin/feedback',
        expect.objectContaining({ params: expect.any(Object) })
      );
    });
  });
});
