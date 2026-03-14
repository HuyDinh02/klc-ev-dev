import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { NotificationsScreen } from '../NotificationsScreen';
import { notificationsApi } from '../../api/notifications';
import type { Notification } from '../../types';

jest.mock('../../api/notifications', () => ({
  notificationsApi: {
    getAll: jest.fn(),
    markAsRead: jest.fn(),
    markAllAsRead: jest.fn(),
  },
}));

jest.mock('../../hooks/useSignalR', () => ({
  useSignalR: () => ({
    connect: jest.fn().mockResolvedValue(undefined),
  }),
}));

jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({ navigate: jest.fn(), goBack: jest.fn() }),
  useFocusEffect: (cb: () => void) => {
    const { useEffect } = require('react');
    useEffect(() => { cb(); }, []);
  },
}));

// Mock date-fns to avoid locale issues in tests
jest.mock('date-fns', () => ({
  formatDistanceToNow: () => '5 minutes ago',
}));

jest.mock('date-fns/locale', () => ({
  vi: {},
}));

const mockNotifications: Notification[] = [
  {
    id: 'n1',
    title: 'Charging Complete',
    message: 'Your session at Station A has finished',
    type: 'SessionComplete',
    isRead: false,
    createdAt: '2026-03-14T10:00:00Z',
  },
  {
    id: 'n2',
    title: 'Payment Success',
    message: 'Payment of 150,000d confirmed',
    type: 'PaymentSuccess',
    isRead: true,
    createdAt: '2026-03-14T09:00:00Z',
  },
  {
    id: 'n3',
    title: 'Special Promotion',
    message: 'Get 20% off your next charge!',
    type: 'Promotion',
    isRead: false,
    createdAt: '2026-03-13T15:00:00Z',
  },
];

beforeEach(() => {
  jest.clearAllMocks();
  (notificationsApi.getAll as jest.Mock).mockResolvedValue({
    items: mockNotifications,
    nextCursor: undefined,
    hasMore: false,
  });
});

describe('NotificationsScreen', () => {
  it('shows loading state', () => {
    let resolve: (v: unknown) => void;
    (notificationsApi.getAll as jest.Mock).mockReturnValue(
      new Promise((r) => { resolve = r; })
    );

    const { getByLabelText } = render(<NotificationsScreen />);
    expect(getByLabelText('Loading notifications')).toBeTruthy();

    act(() => { resolve!({ items: [], hasMore: false }); });
  });

  it('renders notifications title', async () => {
    const { getByText } = render(<NotificationsScreen />);

    await waitFor(() => {
      expect(getByText('Notifications')).toBeTruthy();
    });
  });

  it('renders notification items', async () => {
    const { getByText } = render(<NotificationsScreen />);

    await waitFor(() => {
      expect(getByText('Charging Complete')).toBeTruthy();
    });
    expect(getByText('Payment Success')).toBeTruthy();
    expect(getByText('Special Promotion')).toBeTruthy();
  });

  it('shows notification messages', async () => {
    const { getByText } = render(<NotificationsScreen />);

    await waitFor(() => {
      expect(getByText('Your session at Station A has finished')).toBeTruthy();
    });
  });

  it('shows mark all as read button when unread exist', async () => {
    const { getByText } = render(<NotificationsScreen />);

    await waitFor(() => {
      expect(getByText('Mark all as read')).toBeTruthy();
    });
  });

  it('shows unread count', async () => {
    const { getByText } = render(<NotificationsScreen />);

    await waitFor(() => {
      expect(getByText(/2 unread/i)).toBeTruthy();
    });
  });

  it('marks notification as read on press', async () => {
    (notificationsApi.markAsRead as jest.Mock).mockResolvedValue({});

    const { getByText } = render(<NotificationsScreen />);

    await waitFor(() => {
      expect(getByText('Charging Complete')).toBeTruthy();
    });

    fireEvent.press(getByText('Charging Complete'));

    await waitFor(() => {
      expect(notificationsApi.markAsRead).toHaveBeenCalledWith('n1');
    });
  });

  it('marks all as read', async () => {
    (notificationsApi.markAllAsRead as jest.Mock).mockResolvedValue({});

    const { getByText } = render(<NotificationsScreen />);

    await waitFor(() => {
      expect(getByText('Mark all as read')).toBeTruthy();
    });

    await act(async () => {
      fireEvent.press(getByText('Mark all as read'));
    });

    expect(notificationsApi.markAllAsRead).toHaveBeenCalled();
  });

  it('shows empty state when no notifications', async () => {
    (notificationsApi.getAll as jest.Mock).mockResolvedValue({
      items: [],
      nextCursor: undefined,
      hasMore: false,
    });

    const { getByText } = render(<NotificationsScreen />);

    await waitFor(() => {
      expect(getByText('No notifications yet')).toBeTruthy();
    });
  });

  it('does not show mark all button when all are read', async () => {
    const allRead = mockNotifications.map((n) => ({ ...n, isRead: true }));
    (notificationsApi.getAll as jest.Mock).mockResolvedValue({
      items: allRead,
      nextCursor: undefined,
      hasMore: false,
    });

    const { queryByText, getByText } = render(<NotificationsScreen />);

    await waitFor(() => {
      expect(getByText('Charging Complete')).toBeTruthy();
    });

    expect(queryByText('Mark all as read')).toBeNull();
  });
});
