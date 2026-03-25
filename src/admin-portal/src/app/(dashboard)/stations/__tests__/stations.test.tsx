import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/stations',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock next/link
vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
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
const mockGetAll = vi.fn();
const mockEnable = vi.fn();
const mockDisable = vi.fn();

vi.mock('@/lib/api', () => ({
  stationsApi: {
    getAll: (params: unknown) => mockGetAll(params),
    enable: (id: string) => mockEnable(id),
    disable: (id: string) => mockDisable(id),
  },
}));

import StationsPage from '../page';

const mockStations = [
  {
    id: 'station-1',
    stationCode: 'CS-001',
    name: 'Station Alpha',
    address: '123 Nguyen Hue, District 1, HCMC',
    status: 1,
    isEnabled: true,
    connectorCount: 4,
    lastHeartbeat: '2026-03-08T10:00:00Z',
  },
  {
    id: 'station-2',
    stationCode: 'CS-002',
    name: 'Station Beta',
    address: '456 Le Loi, District 3, HCMC',
    status: 2,
    isEnabled: false,
    connectorCount: 2,
    lastHeartbeat: null,
  },
];

describe('StationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetAll.mockResolvedValue({
      data: { items: mockStations, totalCount: 2 },
    });
    mockEnable.mockResolvedValue({ data: {} });
    mockDisable.mockResolvedValue({ data: {} });
  });

  it('renders stations page title', async () => {
    renderWithProviders(<StationsPage />);
    expect(screen.getByText('Station Management')).toBeInTheDocument();
  });

  it('renders station cards with mock data', async () => {
    renderWithProviders(<StationsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    });
    expect(screen.getByText('CS-001')).toBeInTheDocument();
    expect(screen.getByText('Station Beta')).toBeInTheDocument();
    expect(screen.getByText('CS-002')).toBeInTheDocument();
  });

  it('renders station addresses', async () => {
    renderWithProviders(<StationsPage />);
    await waitFor(() => {
      expect(screen.getByText('123 Nguyen Hue, District 1, HCMC')).toBeInTheDocument();
    });
    expect(screen.getByText('456 Le Loi, District 3, HCMC')).toBeInTheDocument();
  });

  it('renders disabled badge for disabled station', async () => {
    renderWithProviders(<StationsPage />);
    await waitFor(() => {
      expect(screen.getByText('Disabled')).toBeInTheDocument();
    });
  });

  it('search input filters stations', async () => {
    renderWithProviders(<StationsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText('Search stations...');
    fireEvent.change(searchInput, { target: { value: 'Alpha' } });

    // The query key changes to include the search term, triggering a refetch
    await waitFor(() => {
      expect(mockGetAll).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'Alpha' })
      );
    });
  });

  it('shows empty state when no stations', async () => {
    mockGetAll.mockResolvedValue({ data: { items: [], totalCount: 0 } });
    renderWithProviders(<StationsPage />);
    await waitFor(() => {
      expect(screen.getByText('No stations found')).toBeInTheDocument();
    });
  });

  it('renders add station button with link', async () => {
    renderWithProviders(<StationsPage />);
    expect(screen.getByText('Add Station')).toBeInTheDocument();
    const link = screen.getByText('Add Station').closest('a');
    expect(link).toHaveAttribute('href', '/stations/new');
  });

  it('renders view button for each station', async () => {
    renderWithProviders(<StationsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    });
    const viewButtons = screen.getAllByText('View');
    expect(viewButtons.length).toBe(2);
  });

  it('renders connector count for each station', async () => {
    renderWithProviders(<StationsPage />);
    await waitFor(() => {
      expect(screen.getByText('4')).toBeInTheDocument();
    });
    expect(screen.getByText('2')).toBeInTheDocument();
  });
});
