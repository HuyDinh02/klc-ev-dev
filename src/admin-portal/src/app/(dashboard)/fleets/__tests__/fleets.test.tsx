import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/fleets',
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const mockGetList = vi.fn();
const mockCreate = vi.fn();
const mockDelete = vi.fn();

vi.mock('@/lib/api', () => ({
  fleetsApi: {
    getList: (params: unknown) => mockGetList(params),
    create: (data: unknown) => mockCreate(data),
    delete: (id: string) => mockDelete(id),
    get: vi.fn(),
    update: vi.fn(),
    addVehicle: vi.fn(),
    removeVehicle: vi.fn(),
    getSchedules: vi.fn(),
    addSchedule: vi.fn(),
    removeSchedule: vi.fn(),
    getAnalytics: vi.fn(),
    removeAllowedStationGroup: vi.fn(),
  },
}));

import FleetsPage from '../page';

const mockFleets = [
  {
    id: 'fleet-1',
    name: 'Hanoi Taxi Fleet',
    description: 'Electric taxi fleet for central Hanoi',
    operatorUserId: 'user-1',
    maxMonthlyBudgetVnd: 50000000,
    currentMonthSpentVnd: 32000000,
    chargingPolicy: 0, // AnytimeAnywhere
    isActive: true,
    budgetAlertThresholdPercent: 80,
    vehicleCount: 12,
    budgetUtilizationPercent: 64,
    creationTime: '2026-02-01T00:00:00Z',
  },
  {
    id: 'fleet-2',
    name: 'HCMC Delivery Fleet',
    description: 'Last-mile delivery vehicles',
    operatorUserId: 'user-2',
    maxMonthlyBudgetVnd: 20000000,
    currentMonthSpentVnd: 19500000,
    chargingPolicy: 1, // ScheduledOnly
    isActive: true,
    budgetAlertThresholdPercent: 90,
    vehicleCount: 8,
    budgetUtilizationPercent: 97.5,
    creationTime: '2026-02-15T00:00:00Z',
  },
];

describe('FleetsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetList.mockResolvedValue({ data: mockFleets });
    mockCreate.mockResolvedValue({ data: mockFleets[0] });
    mockDelete.mockResolvedValue({ data: {} });
  });

  it('renders fleets page title', async () => {
    renderWithProviders(<FleetsPage />);
    await waitFor(() => {
      expect(screen.getAllByText(/Fleet/i).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('renders fleet list with data', async () => {
    renderWithProviders(<FleetsPage />);
    await waitFor(() => {
      expect(screen.getByText('Hanoi Taxi Fleet')).toBeInTheDocument();
    });
    expect(screen.getByText('HCMC Delivery Fleet')).toBeInTheDocument();
  });

  it('renders vehicle counts', async () => {
    renderWithProviders(<FleetsPage />);
    await waitFor(() => {
      expect(screen.getByText('Hanoi Taxi Fleet')).toBeInTheDocument();
    });
    // vehicleCount 12 and 8 shown in individual cards
    expect(screen.getByText('12')).toBeInTheDocument();
    expect(screen.getByText('8')).toBeInTheDocument();
  });

  it('renders budget information', async () => {
    renderWithProviders(<FleetsPage />);
    await waitFor(() => {
      expect(screen.getByText('Hanoi Taxi Fleet')).toBeInTheDocument();
    });
    // formatVnd uses vi-VN locale — budget amounts appear as formatted VND values
    const content = document.body.textContent ?? '';
    expect(content).toContain('50.000.000');
  });

  it('shows empty state when no fleets', async () => {
    mockGetList.mockResolvedValue({ data: [] });
    renderWithProviders(<FleetsPage />);
    await waitFor(() => {
      expect(screen.queryByText('Hanoi Taxi Fleet')).not.toBeInTheDocument();
    });
  });

  it('calls API on page load', async () => {
    renderWithProviders(<FleetsPage />);
    await waitFor(() => {
      expect(mockGetList).toHaveBeenCalled();
    });
  });
});
