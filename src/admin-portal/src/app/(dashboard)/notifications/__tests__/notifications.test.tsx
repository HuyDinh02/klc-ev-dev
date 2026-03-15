import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/notifications',
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const mockBroadcastSend = vi.fn();
const mockGetHistory = vi.fn();

vi.mock('@/lib/api', () => ({
  broadcastApi: {
    send: (data: unknown) => mockBroadcastSend(data),
    getHistory: (params: unknown) => mockGetHistory(params),
  },
}));

import NotificationsPage from '../page';

const mockBroadcasts = [
  {
    title: 'System maintenance scheduled',
    body: 'Maintenance window: 2am-4am on March 10.',
    type: 8,
    recipientCount: 10,
    sentAt: '2026-03-06T09:00:00Z',
  },
  {
    title: 'New promotion available',
    body: 'Get 20% off your next charge!',
    type: 7,
    recipientCount: 8,
    sentAt: '2026-03-05T14:00:00Z',
  },
];

describe('NotificationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetHistory.mockResolvedValue({ data: mockBroadcasts });
    mockBroadcastSend.mockResolvedValue({
      data: { message: 'Broadcast sent', recipientCount: 10 },
    });
  });

  it('renders notifications page title', async () => {
    renderWithProviders(<NotificationsPage />);
    expect(screen.getByText('Notifications')).toBeInTheDocument();
  });

  it('renders broadcast history with data', async () => {
    renderWithProviders(<NotificationsPage />);
    await waitFor(() => {
      expect(screen.getByText('System maintenance scheduled')).toBeInTheDocument();
    });
    expect(screen.getByText('New promotion available')).toBeInTheDocument();
  });

  it('renders notification type labels', async () => {
    renderWithProviders(<NotificationsPage />);
    await waitFor(() => {
      expect(screen.getByText('System Announcement')).toBeInTheDocument();
    });
    expect(screen.getByText('Promotion')).toBeInTheDocument();
  });

  it('renders stat cards', async () => {
    renderWithProviders(<NotificationsPage />);
    await waitFor(() => {
      expect(screen.getByText('Broadcasts Sent')).toBeInTheDocument();
    });
    expect(screen.getByText('Total Recipients')).toBeInTheDocument();
    expect(screen.getByText('Avg. Recipients')).toBeInTheDocument();
  });

  it('renders broadcast button', async () => {
    renderWithProviders(<NotificationsPage />);
    expect(screen.getByText('Broadcast')).toBeInTheDocument();
  });

  it('shows empty state when no broadcasts', async () => {
    mockGetHistory.mockResolvedValue({ data: [] });
    renderWithProviders(<NotificationsPage />);
    await waitFor(() => {
      expect(screen.getByText('No broadcasts yet')).toBeInTheDocument();
    });
  });

  it('calls broadcast history API on page load', async () => {
    renderWithProviders(<NotificationsPage />);
    await waitFor(() => {
      expect(mockGetHistory).toHaveBeenCalled();
    });
  });
});
