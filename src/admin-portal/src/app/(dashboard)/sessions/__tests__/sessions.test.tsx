import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
const mockPush = vi.fn();
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: mockPush, replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/sessions',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock SignalR
vi.mock('@/lib/signalr', () => ({
  useMonitoringHub: () => ({
    status: 'disconnected',
    subscribeToStation: vi.fn(),
    unsubscribeFromStation: vi.fn(),
  }),
}));

// Mock API
const mockGetAllSessions = vi.fn();

vi.mock('@/lib/api', () => ({
  sessionsApi: {
    getAll: (params: unknown) => mockGetAllSessions(params),
  },
}));

import SessionsPage from '../page';

const mockSessions = [
  {
    id: 'session-1',
    stationName: 'Station Alpha',
    connectorNumber: 1,
    userName: 'Nguyen Van A',
    status: 2,
    startTime: '2026-03-08T08:00:00Z',
    endTime: null,
    totalEnergyKwh: 25.5,
    totalCost: 127500,
  },
  {
    id: 'session-2',
    stationName: 'Station Beta',
    connectorNumber: 2,
    userName: 'Tran Thi B',
    status: 5,
    startTime: '2026-03-08T06:00:00Z',
    endTime: '2026-03-08T07:30:00Z',
    totalEnergyKwh: 40.0,
    totalCost: 200000,
  },
];

describe('SessionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetAllSessions.mockResolvedValue({
      data: { items: mockSessions, totalCount: 2 },
    });
  });

  it('renders sessions page title', async () => {
    renderWithProviders(<SessionsPage />);
    expect(screen.getByText('Charging Sessions')).toBeInTheDocument();
  });

  it('renders sessions table with data', async () => {
    renderWithProviders(<SessionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    });
    expect(screen.getByText('Nguyen Van A')).toBeInTheDocument();
    expect(screen.getByText('Station Beta')).toBeInTheDocument();
    expect(screen.getByText('Tran Thi B')).toBeInTheDocument();
  });

  it('renders table column headers', async () => {
    renderWithProviders(<SessionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station')).toBeInTheDocument();
    });
    expect(screen.getByText('User')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();
    expect(screen.getByText('Start Time')).toBeInTheDocument();
    expect(screen.getByText('Duration')).toBeInTheDocument();
    expect(screen.getByText('Energy')).toBeInTheDocument();
    expect(screen.getByText('Cost')).toBeInTheDocument();
  });

  it('renders stat cards', async () => {
    renderWithProviders(<SessionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Active Sessions')).toBeInTheDocument();
    });
    expect(screen.getByText('Energy Delivered')).toBeInTheDocument();
    expect(screen.getByText('Total Revenue')).toBeInTheDocument();
    expect(screen.getByText('Total Sessions')).toBeInTheDocument();
  });

  it('status filter buttons work', async () => {
    renderWithProviders(<SessionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    });

    const activeBtn = screen.getByRole('button', { name: 'Active' });
    fireEvent.click(activeBtn);

    await waitFor(() => {
      expect(mockGetAllSessions).toHaveBeenCalledWith(
        expect.objectContaining({ status: 2 })
      );
    });
  });

  it('shows completed filter', async () => {
    renderWithProviders(<SessionsPage />);

    const completedBtn = screen.getByRole('button', { name: 'Completed' });
    fireEvent.click(completedBtn);

    await waitFor(() => {
      expect(mockGetAllSessions).toHaveBeenCalledWith(
        expect.objectContaining({ status: 5 })
      );
    });
  });

  it('shows empty state when no sessions', async () => {
    mockGetAllSessions.mockResolvedValue({
      data: { items: [], totalCount: 0 },
    });
    renderWithProviders(<SessionsPage />);
    await waitFor(() => {
      expect(screen.getByText('No sessions found')).toBeInTheDocument();
    });
  });

  it('renders connector numbers', async () => {
    renderWithProviders(<SessionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Connector #1')).toBeInTheDocument();
    });
    expect(screen.getByText('Connector #2')).toBeInTheDocument();
  });

  it('clicking a session row navigates to session detail', async () => {
    renderWithProviders(<SessionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    });
    const row = screen.getByText('Station Alpha').closest('tr');
    fireEvent.click(row!);
    expect(mockPush).toHaveBeenCalledWith('/sessions/session-1');
  });
});
