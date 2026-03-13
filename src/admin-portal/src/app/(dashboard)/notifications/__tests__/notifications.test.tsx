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

const mockGetAll = vi.fn();
const mockGetById = vi.fn();
const mockGetUnreadCount = vi.fn();
const mockMarkAsRead = vi.fn();
const mockMarkAllAsRead = vi.fn();
const mockBroadcastSend = vi.fn();

vi.mock('@/lib/api', () => ({
  notificationsApi: {
    getAll: (params: unknown) => mockGetAll(params),
    getById: (id: string) => mockGetById(id),
    getUnreadCount: () => mockGetUnreadCount(),
    markAsRead: (id: string) => mockMarkAsRead(id),
    markAllAsRead: () => mockMarkAllAsRead(),
  },
  broadcastApi: {
    send: (data: unknown) => mockBroadcastSend(data),
  },
}));

import NotificationsPage from '../page';

const mockNotifications = [
  {
    id: 'notif-1',
    type: 0,
    title: 'Charging session started at Station Alpha',
    body: 'Your vehicle is now charging.',
    isRead: false,
    createdAt: '2026-03-08T10:00:00Z',
    referenceId: 'session-1',
  },
  {
    id: 'notif-2',
    type: 3,
    title: 'Payment of 250,000d successful',
    body: 'Transaction completed.',
    isRead: true,
    createdAt: '2026-03-07T14:00:00Z',
    referenceId: 'pay-1',
  },
  {
    id: 'notif-3',
    type: 8,
    title: 'System maintenance scheduled',
    body: 'Maintenance window: 2am-4am on March 10.',
    isRead: false,
    createdAt: '2026-03-06T09:00:00Z',
    referenceId: null,
  },
];

describe('NotificationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetAll.mockResolvedValue({
      data: { items: mockNotifications, totalCount: 3 },
    });
    mockGetUnreadCount.mockResolvedValue({ data: 2 });
    mockGetById.mockResolvedValue({ data: mockNotifications[0] });
    mockMarkAsRead.mockResolvedValue({ data: {} });
    mockMarkAllAsRead.mockResolvedValue({ data: {} });
    mockBroadcastSend.mockResolvedValue({
      data: { message: 'Broadcast sent', recipientCount: 100 },
    });
  });

  it('renders notifications page title', async () => {
    renderWithProviders(<NotificationsPage />);
    expect(screen.getByText('Notifications')).toBeInTheDocument();
  });

  it('renders notification list with data', async () => {
    renderWithProviders(<NotificationsPage />);
    await waitFor(() => {
      expect(screen.getByText('Charging session started at Station Alpha')).toBeInTheDocument();
    });
    expect(screen.getByText('Payment of 250,000d successful')).toBeInTheDocument();
    expect(screen.getByText('System maintenance scheduled')).toBeInTheDocument();
  });

  it('renders notification type labels', async () => {
    renderWithProviders(<NotificationsPage />);
    await waitFor(() => {
      expect(screen.getByText('Charging Started')).toBeInTheDocument();
    });
    expect(screen.getByText('Payment Successful')).toBeInTheDocument();
    expect(screen.getByText('System Announcement')).toBeInTheDocument();
  });

  it('renders stat cards', async () => {
    renderWithProviders(<NotificationsPage />);
    await waitFor(() => {
      expect(screen.getByText('Total')).toBeInTheDocument();
    });
    // "Unread" appears both as stat card label and filter button
    expect(screen.getAllByText('Unread').length).toBeGreaterThanOrEqual(1);
    // "Read" appears both as stat card label and filter button
    expect(screen.getAllByText('Read').length).toBeGreaterThanOrEqual(1);
  });

  it('renders filter buttons', async () => {
    renderWithProviders(<NotificationsPage />);
    expect(screen.getAllByText('All').length).toBeGreaterThanOrEqual(1);
  });

  it('renders mark all as read button', async () => {
    renderWithProviders(<NotificationsPage />);
    expect(screen.getByText('Mark All as Read')).toBeInTheDocument();
  });

  it('shows empty state when no notifications', async () => {
    mockGetAll.mockResolvedValue({ data: { items: [], totalCount: 0 } });
    renderWithProviders(<NotificationsPage />);
    await waitFor(() => {
      expect(screen.getByText('No notifications')).toBeInTheDocument();
    });
  });

  it('calls API on page load', async () => {
    renderWithProviders(<NotificationsPage />);
    await waitFor(() => {
      expect(mockGetAll).toHaveBeenCalled();
    });
    expect(mockGetUnreadCount).toHaveBeenCalled();
  });
});
