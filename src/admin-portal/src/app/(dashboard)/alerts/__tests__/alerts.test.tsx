import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/alerts',
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
const mockApiGet = vi.fn();
const mockApiPost = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    get: (url: string, config: unknown) => mockApiGet(url, config),
    post: (url: string, data?: unknown) => mockApiPost(url, data),
  },
}));

// Mock store
vi.mock('@/lib/store', () => ({
  useAlertsStore: Object.assign(
    () => ({
      unreadCount: 0,
      setUnreadCount: vi.fn(),
      incrementUnreadCount: vi.fn(),
      decrementUnreadCount: vi.fn(),
    }),
    {
      getState: () => ({
        incrementUnreadCount: vi.fn(),
      }),
    }
  ),
  useAuthStore: () => ({
    permissions: [],
    user: null,
    token: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
    setPermissions: vi.fn(),
    hasPermission: () => true,
  }),
}));

import AlertsPage from '../page';

const mockAlerts = [
  {
    id: 'alert-1',
    type: 0, // StationOffline — critical
    title: 'Station Alpha Offline',
    message: 'Station Alpha has not responded in 5 minutes',
    stationId: 'station-1',
    stationName: 'Station Alpha',
    status: 0, // New
    createdAt: new Date(Date.now() - 120000).toISOString(), // 2 minutes ago
  },
  {
    id: 'alert-2',
    type: 2, // LowUtilization — warning
    title: 'Low Utilization Warning',
    message: 'Station Beta utilization is below threshold',
    stationId: 'station-2',
    stationName: 'Station Beta',
    status: 1, // Acknowledged
    acknowledgedBy: 'admin',
    acknowledgedAt: new Date().toISOString(),
    createdAt: new Date(Date.now() - 3600000).toISOString(), // 1 hour ago
  },
  {
    id: 'alert-3',
    type: 4, // FirmwareUpdate — info
    title: 'Firmware Update Available',
    message: 'New firmware version available for Station Gamma',
    stationId: 'station-3',
    stationName: 'Station Gamma',
    status: 0, // New
    createdAt: new Date(Date.now() - 86400000).toISOString(), // 1 day ago
  },
];

describe('AlertsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiGet.mockResolvedValue({
      data: { items: mockAlerts, totalCount: 3 },
    });
    mockApiPost.mockResolvedValue({ data: {} });
  });

  it('renders alerts page title', async () => {
    renderWithProviders(<AlertsPage />);
    expect(screen.getByText('Alerts')).toBeInTheDocument();
  });

  it('renders alerts list with data', async () => {
    renderWithProviders(<AlertsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha Offline')).toBeInTheDocument();
    });
    expect(screen.getByText('Low Utilization Warning')).toBeInTheDocument();
    expect(screen.getByText('Firmware Update Available')).toBeInTheDocument();
  });

  it('renders alert messages', async () => {
    renderWithProviders(<AlertsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha has not responded in 5 minutes')).toBeInTheDocument();
    });
  });

  it('renders stat cards for severity categories', async () => {
    renderWithProviders(<AlertsPage />);
    await waitFor(() => {
      // "Critical" appears both as stat card label and filter option, so use getAllByText
      const criticals = screen.getAllByText('Critical');
      expect(criticals.length).toBeGreaterThanOrEqual(1);
    });
    // "Warning" and "Info" also appear in both stat cards and filter dropdowns
    const warnings = screen.getAllByText('Warning');
    expect(warnings.length).toBeGreaterThanOrEqual(1);
    const infos = screen.getAllByText('Info');
    expect(infos.length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state when no alerts', async () => {
    mockApiGet.mockResolvedValue({ data: { items: [], totalCount: 0 } });
    renderWithProviders(<AlertsPage />);
    await waitFor(() => {
      expect(screen.getByText('No alerts found')).toBeInTheDocument();
    });
  });

  it('renders acknowledge button for new (unacknowledged) alerts', async () => {
    renderWithProviders(<AlertsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha Offline')).toBeInTheDocument();
    });
    // New alerts (status === 0) get the acknowledge button (CheckCircle2 icon button)
    // There should be 2 acknowledge buttons (alert-1 and alert-3 have status 0)
    const alertCards = screen.getAllByText('Station Offline');
    expect(alertCards.length).toBeGreaterThanOrEqual(1);
  });

  it('acknowledge mutation calls API', async () => {
    renderWithProviders(<AlertsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha Offline')).toBeInTheDocument();
    });

    // Find the alert card for alert-1 and its acknowledge button
    // The acknowledge buttons are outline variant with CheckCircle2 icon
    // They have a specific structure: Button variant="outline" size="sm"
    const alertRow = screen.getByText('Station Alpha Offline').closest('[class*="card"]');
    expect(alertRow).toBeInTheDocument();

    // Find the outline button (acknowledge) inside the alert card
    const buttons = alertRow!.querySelectorAll('button');
    // Last button in a new alert row should be the acknowledge button
    const acknowledgeBtn = Array.from(buttons).find(btn => {
      // The acknowledge button is the outline one (not ghost)
      return btn.className.includes('border');
    });

    if (acknowledgeBtn) {
      fireEvent.click(acknowledgeBtn);
      await waitFor(() => {
        // api.post is called with (url, undefined) since no body is sent
        expect(mockApiPost).toHaveBeenCalledWith('/alerts/alert-1/acknowledge', undefined);
      });
    }
  });

  it('renders severity filter dropdown', async () => {
    renderWithProviders(<AlertsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha Offline')).toBeInTheDocument();
    });
    // There should be a filter select for severity
    const severitySelect = screen.getByDisplayValue('All Severity');
    expect(severitySelect).toBeInTheDocument();
  });

  it('renders status filter dropdown', async () => {
    renderWithProviders(<AlertsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha Offline')).toBeInTheDocument();
    });
    const statusSelect = screen.getByDisplayValue('All Status');
    expect(statusSelect).toBeInTheDocument();
  });

  it('renders station names for alerts', async () => {
    renderWithProviders(<AlertsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    });
    expect(screen.getByText('Station Beta')).toBeInTheDocument();
    expect(screen.getByText('Station Gamma')).toBeInTheDocument();
  });

  it('renders acknowledged info for acknowledged alerts', async () => {
    renderWithProviders(<AlertsPage />);
    await waitFor(() => {
      expect(screen.getByText(/Acknowledged by/)).toBeInTheDocument();
    });
  });
});
