import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/faults',
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const mockGetAll = vi.fn();
const mockUpdateStatus = vi.fn();

vi.mock('@/lib/api', () => ({
  faultsApi: {
    getAll: (params: unknown) => mockGetAll(params),
    updateStatus: (id: string, status: number) => mockUpdateStatus(id, status),
  },
}));

import FaultsPage from '../page';

const mockFaults = [
  {
    id: 'fault-1',
    errorCode: 'ERR-001',
    status: 0,
    priority: 1,
    errorInfo: 'Over-voltage detected on connector',
    stationName: 'Station Alpha',
    connectorNumber: 2,
    detectedAt: '2026-03-08T10:00:00Z',
    resolvedAt: null,
  },
  {
    id: 'fault-2',
    errorCode: 'ERR-002',
    status: 1,
    priority: 3,
    errorInfo: 'Communication timeout',
    stationName: 'Station Beta',
    connectorNumber: 1,
    detectedAt: '2026-03-07T14:00:00Z',
    resolvedAt: null,
  },
  {
    id: 'fault-3',
    errorCode: 'ERR-003',
    status: 2,
    priority: 4,
    errorInfo: 'Minor firmware warning',
    stationName: 'Station Gamma',
    connectorNumber: 3,
    detectedAt: '2026-03-06T09:00:00Z',
    resolvedAt: '2026-03-06T12:00:00Z',
  },
];

describe('FaultsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetAll.mockResolvedValue({
      data: { items: mockFaults, totalCount: 3 },
    });
    mockUpdateStatus.mockResolvedValue({ data: {} });
  });

  it('renders faults page title', async () => {
    renderWithProviders(<FaultsPage />);
    expect(screen.getByText('Fault Management')).toBeInTheDocument();
  });

  it('renders fault list with data', async () => {
    renderWithProviders(<FaultsPage />);
    await waitFor(() => {
      expect(screen.getByText('ERR-001')).toBeInTheDocument();
    });
    expect(screen.getByText('ERR-002')).toBeInTheDocument();
    expect(screen.getByText('ERR-003')).toBeInTheDocument();
  });

  it('renders fault error info descriptions', async () => {
    renderWithProviders(<FaultsPage />);
    await waitFor(() => {
      expect(screen.getByText('Over-voltage detected on connector')).toBeInTheDocument();
    });
    expect(screen.getByText('Communication timeout')).toBeInTheDocument();
  });

  it('renders station names for faults', async () => {
    renderWithProviders(<FaultsPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    });
    expect(screen.getByText('Station Beta')).toBeInTheDocument();
  });

  it('renders stat cards with computed counts', async () => {
    renderWithProviders(<FaultsPage />);
    await waitFor(() => {
      expect(screen.getByText('Open Faults')).toBeInTheDocument();
    });
    // "Investigating" appears both as stat card label and filter button
    expect(screen.getAllByText('Investigating').length).toBeGreaterThanOrEqual(2);
    // "Critical" appears both as stat card label and severity badge on fault-1
    expect(screen.getAllByText('Critical').length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('Total Faults')).toBeInTheDocument();
  });

  it('shows empty state when no faults', async () => {
    mockGetAll.mockResolvedValue({ data: { items: [], totalCount: 0 } });
    renderWithProviders(<FaultsPage />);
    await waitFor(() => {
      expect(screen.getByText('No faults found')).toBeInTheDocument();
    });
  });

  it('calls API on page load', async () => {
    renderWithProviders(<FaultsPage />);
    await waitFor(() => {
      expect(mockGetAll).toHaveBeenCalled();
    });
  });

  it('renders search input', async () => {
    renderWithProviders(<FaultsPage />);
    expect(screen.getByPlaceholderText('Search faults...')).toBeInTheDocument();
  });

  it('renders status filter buttons', async () => {
    renderWithProviders(<FaultsPage />);
    expect(screen.getByText('All')).toBeInTheDocument();
    expect(screen.getByText('Open')).toBeInTheDocument();
    expect(screen.getByText('Resolved')).toBeInTheDocument();
    expect(screen.getByText('Closed')).toBeInTheDocument();
  });
});
