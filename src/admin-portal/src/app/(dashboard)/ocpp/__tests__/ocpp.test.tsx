import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/ocpp',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock API
const mockApiGet = vi.fn();
const mockApiPost = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
    post: (url: string, data?: unknown) => mockApiPost(url, data),
  },
}));

import OcppManagementPage from '../page';

const mockConnections = [
  {
    chargePointId: 'CP-001',
    connectedAt: '2026-03-08T08:00:00Z',
    lastHeartbeat: new Date(Date.now() - 120000).toISOString(), // 2 minutes ago
    isRegistered: true,
    stationId: 'station-1',
    vendorProfile: 1,
  },
  {
    chargePointId: 'CP-002',
    connectedAt: '2026-03-08T09:00:00Z',
    lastHeartbeat: new Date(Date.now() - 3600000).toISOString(), // 1 hour ago
    isRegistered: false,
    stationId: null,
    vendorProfile: 0,
  },
];

const mockEvents = [
  {
    id: 'evt-1',
    chargePointId: 'CP-001',
    action: 'Heartbeat',
    uniqueId: 'uid-1',
    messageType: 2,
    payload: '{}',
    latencyMs: 15,
    vendorProfile: 1,
    receivedAt: '2026-03-08T10:00:00Z',
  },
  {
    id: 'evt-2',
    chargePointId: 'CP-001',
    action: 'StatusNotification',
    uniqueId: 'uid-2',
    messageType: 2,
    payload: '{"status":"Available"}',
    latencyMs: null,
    vendorProfile: 1,
    receivedAt: '2026-03-08T09:55:00Z',
  },
];

const mockDetail = {
  chargePointId: 'CP-001',
  isOnline: true,
  connectedAt: '2026-03-08T08:00:00Z',
  lastHeartbeat: '2026-03-08T10:00:00Z',
  isRegistered: true,
  stationId: 'station-1',
  vendorProfile: 1,
  vendor: 'Chargecore',
  model: 'CC-7000',
  firmwareVersion: '1.5.0',
  serialNumber: 'SN-12345',
};

describe('OcppManagementPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiGet.mockImplementation((url: string) => {
      if (url === '/ocpp/connections') {
        return Promise.resolve({ data: mockConnections });
      }
      if (url.startsWith('/ocpp/connections/')) {
        return Promise.resolve({ data: mockDetail });
      }
      if (url.startsWith('/ocpp/events')) {
        return Promise.resolve({ data: mockEvents });
      }
      return Promise.resolve({ data: null });
    });
  });

  it('renders page title', async () => {
    renderWithProviders(<OcppManagementPage />);
    expect(screen.getByText('OCPP Management')).toBeInTheDocument();
  });

  it('renders connected chargers count in description', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('Connected chargers: 2')).toBeInTheDocument();
    });
  });

  it('renders refresh button', async () => {
    renderWithProviders(<OcppManagementPage />);
    expect(screen.getByText('Refresh')).toBeInTheDocument();
  });

  it('renders connected chargers card', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('Connected Chargers')).toBeInTheDocument();
    });
  });

  it('renders charger list with charge point IDs', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      // CP-001 appears in connections list AND events table
      expect(screen.getAllByText('CP-001').length).toBeGreaterThanOrEqual(1);
    });
    expect(screen.getByText('CP-002')).toBeInTheDocument();
  });

  it('renders vendor profile badges', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getAllByText('Chargecore Global').length).toBeGreaterThanOrEqual(1);
    });
    expect(screen.getAllByText('Generic').length).toBeGreaterThanOrEqual(1);
  });

  it('renders registration status badges', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('Registered')).toBeInTheDocument();
    });
    expect(screen.getByText('Pending')).toBeInTheDocument();
  });

  it('renders event log card', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('OCPP Event Log')).toBeInTheDocument();
    });
  });

  it('renders event log table headers', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('Time')).toBeInTheDocument();
    });
    expect(screen.getByText('Action')).toBeInTheDocument();
    expect(screen.getByText('Latency')).toBeInTheDocument();
  });

  it('renders event actions in table', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('Heartbeat')).toBeInTheDocument();
    });
    expect(screen.getByText('StatusNotification')).toBeInTheDocument();
  });

  it('renders event latency values', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('15ms')).toBeInTheDocument();
    });
  });

  it('renders select charger empty state when no charger selected', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('Select a charger')).toBeInTheDocument();
    });
    expect(screen.getByText('Choose a charger from the list to view details and send commands')).toBeInTheDocument();
  });

  it('renders charger detail panel when a charger is selected', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getAllByText('CP-001').length).toBeGreaterThanOrEqual(1);
    });

    // Click on the first charger in connections list (font-medium div)
    const cp001Elements = screen.getAllByText('CP-001');
    fireEvent.click(cp001Elements[0].closest('[class*="cursor-pointer"]') || cp001Elements[0]);

    await waitFor(() => {
      expect(screen.getByText('Chargecore')).toBeInTheDocument();
    });
    expect(screen.getByText('CC-7000')).toBeInTheDocument();
    expect(screen.getByText('1.5.0')).toBeInTheDocument();
    expect(screen.getByText('SN-12345')).toBeInTheDocument();
  });

  it('renders detail panel labels', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getAllByText('CP-001').length).toBeGreaterThanOrEqual(1);
    });
    const cp001Elements = screen.getAllByText('CP-001');
    fireEvent.click(cp001Elements[0].closest('[class*="cursor-pointer"]') || cp001Elements[0]);

    await waitFor(() => {
      expect(screen.getByText('Vendor')).toBeInTheDocument();
    });
    expect(screen.getByText('Model')).toBeInTheDocument();
    expect(screen.getByText('Serial')).toBeInTheDocument();
  });

  it('renders remote commands for online charger', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getAllByText('CP-001').length).toBeGreaterThanOrEqual(1);
    });
    const cp001Elements = screen.getAllByText('CP-001');
    fireEvent.click(cp001Elements[0].closest('[class*="cursor-pointer"]') || cp001Elements[0]);

    await waitFor(() => {
      expect(screen.getByText('Remote Commands')).toBeInTheDocument();
    });
    expect(screen.getByText('Start')).toBeInTheDocument();
    expect(screen.getByText('Stop')).toBeInTheDocument();
    expect(screen.getByText('Reset')).toBeInTheDocument();
    expect(screen.getByText('Unlock')).toBeInTheDocument();
    expect(screen.getByText('Availability')).toBeInTheDocument();
    expect(screen.getByText('Trigger')).toBeInTheDocument();
    expect(screen.getByText('Get Configuration')).toBeInTheDocument();
    expect(screen.getByText('Power Limit')).toBeInTheDocument();
  });

  it('shows empty state when no chargers connected', async () => {
    mockApiGet.mockImplementation((url: string) => {
      if (url === '/ocpp/connections') {
        return Promise.resolve({ data: [] });
      }
      if (url.startsWith('/ocpp/events')) {
        return Promise.resolve({ data: [] });
      }
      return Promise.resolve({ data: null });
    });
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('No chargers connected')).toBeInTheDocument();
    });
    expect(screen.getByText('No chargers are currently connected via OCPP')).toBeInTheDocument();
  });

  it('shows empty event log when no events', async () => {
    mockApiGet.mockImplementation((url: string) => {
      if (url === '/ocpp/connections') {
        return Promise.resolve({ data: mockConnections });
      }
      if (url.startsWith('/ocpp/events')) {
        return Promise.resolve({ data: [] });
      }
      return Promise.resolve({ data: null });
    });
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('No events recorded')).toBeInTheDocument();
    });
    expect(screen.getByText('OCPP events will appear here when chargers communicate')).toBeInTheDocument();
  });

  it('renders loading state initially', () => {
    mockApiGet.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<OcppManagementPage />);
    // Title always renders
    expect(screen.getByText('OCPP Management')).toBeInTheDocument();
  });

  it('renders filter input for events', async () => {
    renderWithProviders(<OcppManagementPage />);
    await waitFor(() => {
      expect(screen.getByPlaceholderText('Filter by cpId...')).toBeInTheDocument();
    });
  });
});
