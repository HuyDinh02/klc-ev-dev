import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/',
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

// Mock recharts — ResponsiveContainer needs width/height to render children
vi.mock('recharts', () => ({
  BarChart: ({ children }: { children: React.ReactNode }) => <div data-testid="bar-chart">{children}</div>,
  Bar: () => <div />,
  XAxis: () => <div />,
  YAxis: () => <div />,
  CartesianGrid: () => <div />,
  Tooltip: () => <div />,
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Cell: () => <div />,
}));

// Mock API modules
const mockGetDashboard = vi.fn();
const mockGetAlerts = vi.fn();

vi.mock('@/lib/api', () => ({
  monitoringApi: {
    getDashboard: () => mockGetDashboard(),
  },
  alertsApi: {
    getAll: () => mockGetAlerts(),
  },
}));

import DashboardPage from '../page';

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetDashboard.mockResolvedValue({
      data: {
        totalStations: 10,
        onlineStations: 8,
        offlineStations: 1,
        faultedStations: 1,
        totalConnectors: 20,
        availableConnectors: 12,
        chargingConnectors: 5,
        faultedConnectors: 3,
        activeSessions: 5,
        todayRevenue: 2500000,
        todayEnergyKwh: 150.5,
      },
    });
    mockGetAlerts.mockResolvedValue({
      data: {
        items: [
          {
            id: 'alert-1',
            type: 0,
            title: 'Station Alpha Offline',
            message: 'Station Alpha has gone offline',
            stationName: 'Station Alpha',
            status: 0,
            creationTime: '2026-03-08T10:00:00Z',
          },
        ],
      },
    });
  });

  it('renders dashboard title', async () => {
    renderWithProviders(<DashboardPage />);
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('renders stat cards with mocked data', async () => {
    renderWithProviders(<DashboardPage />);
    await waitFor(() => {
      expect(screen.getByText('Active Sessions')).toBeInTheDocument();
    });
    // activeSessions = 5, but also chargingConnectors = 5 — multiple "5" on page
    // Verify specific stat cards are present with their labels
    expect(screen.getByText('Network Availability')).toBeInTheDocument();
    expect(screen.getByText('85%')).toBeInTheDocument();
    expect(screen.getByText('Energy Today')).toBeInTheDocument();
    expect(screen.getByText('150.50 kWh')).toBeInTheDocument();
    expect(screen.getByText('Revenue Today')).toBeInTheDocument();
  });

  it('renders loading state initially before data loads', () => {
    // Make the API never resolve to keep the loading state
    mockGetDashboard.mockReturnValue(new Promise(() => {}));
    mockGetAlerts.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<DashboardPage />);
    // The title is always rendered
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    // Stat cards should not be visible yet (loading skeletons shown instead)
    expect(screen.queryByText('Active Sessions')).not.toBeInTheDocument();
  });

  it('handles API error gracefully', async () => {
    mockGetDashboard.mockRejectedValue(new Error('Network error'));
    mockGetAlerts.mockRejectedValue(new Error('Network error'));
    renderWithProviders(<DashboardPage />);
    // Should still render the page structure with default values (0)
    await waitFor(() => {
      expect(screen.getByText('Active Sessions')).toBeInTheDocument();
    });
    // Default values when API fails
    expect(screen.getByText('0%')).toBeInTheDocument();
  });

  it('renders recent alerts section', async () => {
    renderWithProviders(<DashboardPage />);
    await waitFor(() => {
      expect(screen.getByText('Recent Alerts')).toBeInTheDocument();
    });
    await waitFor(() => {
      expect(screen.getByText('Station Alpha Offline')).toBeInTheDocument();
    });
  });

  it('renders empty alerts state when no alerts', async () => {
    mockGetAlerts.mockResolvedValue({ data: { items: [] } });
    renderWithProviders(<DashboardPage />);
    await waitFor(() => {
      expect(screen.getByText('No recent alerts')).toBeInTheDocument();
    });
  });

  it('renders station overview section', async () => {
    renderWithProviders(<DashboardPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Overview')).toBeInTheDocument();
    });
    expect(screen.getByText('Connector Status')).toBeInTheDocument();
  });
});
