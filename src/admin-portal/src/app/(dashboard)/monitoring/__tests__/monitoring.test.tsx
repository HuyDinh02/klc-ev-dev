import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/monitoring',
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

// Mock API modules
const mockGetDashboard = vi.fn();

vi.mock('@/lib/api', () => ({
  monitoringApi: {
    getDashboard: () => mockGetDashboard(),
  },
}));

import MonitoringPage from '../page';

const mockDashboardData = {
  totalStations: 10,
  onlineStations: 8,
  offlineStations: 1,
  faultedStations: 1,
  totalConnectors: 20,
  availableConnectors: 12,
  chargingConnectors: 5,
  faultedConnectors: 3,
  activeSessions: 5,
  todayEnergyKwh: 150.5,
  todayRevenue: 2500000,
  stationSummaries: [
    {
      stationId: 'station-1',
      stationName: 'Station Alpha',
      status: 1,
      latitude: 10.762,
      longitude: 106.66,
      totalConnectors: 4,
      availableConnectors: 2,
      chargingConnectors: 2,
      lastHeartbeat: '2026-03-08T10:00:00Z',
    },
    {
      stationId: 'station-2',
      stationName: 'Station Beta',
      status: 0,
      latitude: 10.763,
      longitude: 106.67,
      totalConnectors: 2,
      availableConnectors: 0,
      chargingConnectors: 0,
      lastHeartbeat: null,
    },
  ],
};

describe('MonitoringPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetDashboard.mockResolvedValue({
      data: mockDashboardData,
    });
  });

  it('renders page title and description', async () => {
    renderWithProviders(<MonitoringPage />);
    expect(screen.getByText('Real-time Monitoring')).toBeInTheDocument();
    expect(screen.getByText('Live system status and performance metrics')).toBeInTheDocument();
  });

  it('renders SignalR connection status indicator', async () => {
    renderWithProviders(<MonitoringPage />);
    // When disconnected, should show "Polling (10s)"
    expect(screen.getByText('Polling (10s)')).toBeInTheDocument();
  });

  it('renders stat cards with mocked data', async () => {
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Status')).toBeInTheDocument();
    });
    expect(screen.getByText('Connector Status')).toBeInTheDocument();
    expect(screen.getByText('Active Sessions')).toBeInTheDocument();
    expect(screen.getByText("Today's Energy")).toBeInTheDocument();
  });

  it('renders station/connector counts', async () => {
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      expect(screen.getByText('8 / 10')).toBeInTheDocument(); // online/total stations
    });
    expect(screen.getByText('5 / 20')).toBeInTheDocument(); // charging/total connectors
  });

  it('renders active session count', async () => {
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      // "5" appears in multiple places (charging connectors badge + active sessions stat)
      expect(screen.getAllByText('5').length).toBeGreaterThanOrEqual(1);
    });
    expect(screen.getByText('Sessions currently in progress')).toBeInTheDocument();
  });

  it('renders energy value', async () => {
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      expect(screen.getByText('150.5 kWh')).toBeInTheDocument();
    });
  });

  it('renders station overview section', async () => {
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Overview')).toBeInTheDocument();
    });
  });

  it('renders station cards in overview', async () => {
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    });
    expect(screen.getByText('Station Beta')).toBeInTheDocument();
  });

  it('renders recent alerts section', async () => {
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      expect(screen.getByText('Recent Alerts')).toBeInTheDocument();
    });
  });

  it('renders faulted connectors alert when faults exist', async () => {
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      expect(screen.getByText(/connector\(s\) reporting faults/)).toBeInTheDocument();
    });
  });

  it('renders all-clear message when no faults', async () => {
    mockGetDashboard.mockResolvedValue({
      data: {
        ...mockDashboardData,
        faultedConnectors: 0,
      },
    });
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      expect(screen.getByText('All systems operating normally')).toBeInTheDocument();
    });
  });

  it('renders empty state when no stations', async () => {
    mockGetDashboard.mockResolvedValue({
      data: {
        ...mockDashboardData,
        stationSummaries: [],
      },
    });
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      expect(screen.getByText('No stations found')).toBeInTheDocument();
    });
  });

  it('renders loading state initially', () => {
    mockGetDashboard.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<MonitoringPage />);
    // Title always renders
    expect(screen.getByText('Real-time Monitoring')).toBeInTheDocument();
    // Stat cards should not be visible while loading
    expect(screen.queryByText('Station Status')).not.toBeInTheDocument();
  });

  it('renders online/offline badge counts', async () => {
    renderWithProviders(<MonitoringPage />);
    await waitFor(() => {
      expect(screen.getByText('8 / 10')).toBeInTheDocument();
    });
    // Check for the online/offline badges in station status card
    // These may appear multiple times so use getAllByText
    expect(screen.getAllByText('Online').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Offline').length).toBeGreaterThanOrEqual(1);
  });
});
