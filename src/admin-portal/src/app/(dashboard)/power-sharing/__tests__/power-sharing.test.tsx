import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/power-sharing',
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock('@/lib/signalr', () => ({
  useMonitoringHub: () => ({
    status: 'disconnected',
    subscribeToPowerSharingGroup: vi.fn(),
    unsubscribeFromPowerSharingGroup: vi.fn(),
    onPowerAllocationChanged: vi.fn(),
  }),
}));

const mockGetList = vi.fn();
const mockCreate = vi.fn();
const mockDelete = vi.fn();
const mockRecalculate = vi.fn();

vi.mock('@/lib/api', () => ({
  powerSharingApi: {
    getList: (params: unknown) => mockGetList(params),
    create: (data: unknown) => mockCreate(data),
    delete: (id: string) => mockDelete(id),
    get: vi.fn(),
    update: vi.fn(),
    activate: vi.fn(),
    deactivate: vi.fn(),
    addMember: vi.fn(),
    removeMember: vi.fn(),
    recalculate: (id: string) => mockRecalculate(id),
    getLoadProfiles: vi.fn(),
  },
}));

import PowerSharingPage from '../page';

const mockGroups = [
  {
    id: 'group-1',
    name: 'Building A Power Pool',
    maxCapacityKw: 150,
    mode: 0, // Link
    distributionStrategy: 0, // Average
    isActive: true,
    memberCount: 4,
    totalAllocatedKw: 120,
    creationTime: '2026-02-20T00:00:00Z',
  },
  {
    id: 'group-2',
    name: 'Parking Lot B Loop',
    maxCapacityKw: 75,
    mode: 1, // Loop
    distributionStrategy: 1, // Proportional
    isActive: false,
    memberCount: 2,
    totalAllocatedKw: 0,
    creationTime: '2026-03-01T00:00:00Z',
  },
];

describe('PowerSharingPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetList.mockResolvedValue({ data: mockGroups });
    mockCreate.mockResolvedValue({ data: mockGroups[0] });
    mockDelete.mockResolvedValue({ data: {} });
    mockRecalculate.mockResolvedValue({ data: [] });
  });

  it('renders power sharing page title', async () => {
    renderWithProviders(<PowerSharingPage />);
    await waitFor(() => {
      expect(screen.getAllByText(/Power Sharing/i).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('renders group list with data', async () => {
    renderWithProviders(<PowerSharingPage />);
    await waitFor(() => {
      expect(screen.getByText('Building A Power Pool')).toBeInTheDocument();
    });
    expect(screen.getByText('Parking Lot B Loop')).toBeInTheDocument();
  });

  it('renders capacity information', async () => {
    renderWithProviders(<PowerSharingPage />);
    await waitFor(() => {
      expect(screen.getByText('Building A Power Pool')).toBeInTheDocument();
    });
    // maxCapacityKw is rendered as "{value} kW"
    expect(screen.getByText('150 kW')).toBeInTheDocument();
  });

  it('renders member counts', async () => {
    renderWithProviders(<PowerSharingPage />);
    await waitFor(() => {
      expect(screen.getByText('Building A Power Pool')).toBeInTheDocument();
    });
    // memberCount 4 shown in group-1 card
    expect(screen.getAllByText('4').length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state when no groups', async () => {
    mockGetList.mockResolvedValue({ data: [] });
    renderWithProviders(<PowerSharingPage />);
    await waitFor(() => {
      expect(screen.queryByText('Building A Power Pool')).not.toBeInTheDocument();
    });
  });

  it('calls API on page load', async () => {
    renderWithProviders(<PowerSharingPage />);
    await waitFor(() => {
      expect(mockGetList).toHaveBeenCalled();
    });
  });
});
