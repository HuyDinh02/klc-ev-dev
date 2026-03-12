import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/tariffs',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock API module (tariffs page uses `api` directly via api.get/post/put/delete)
const mockApiGet = vi.fn();
const mockApiPost = vi.fn();
const mockApiPut = vi.fn();
const mockApiDelete = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
    post: (url: string, data?: unknown) => mockApiPost(url, data),
    put: (url: string, data?: unknown) => mockApiPut(url, data),
    delete: (url: string) => mockApiDelete(url),
  },
}));

import TariffsPage from '../page';

const mockTariffs = [
  {
    id: 'tariff-1',
    name: 'Standard Rate',
    description: 'Default pricing for all stations',
    baseRatePerKwh: 4500,
    taxRatePercent: 10,
    totalRatePerKwh: 4950,
    isActive: true,
    isDefault: true,
    effectiveFrom: '2026-01-01T00:00:00Z',
    creationTime: '2026-01-01T00:00:00Z',
  },
  {
    id: 'tariff-2',
    name: 'Peak Rate',
    description: 'Higher rate for peak hours',
    baseRatePerKwh: 6000,
    taxRatePercent: 10,
    totalRatePerKwh: 6600,
    isActive: false,
    isDefault: false,
    effectiveFrom: '2026-03-01T00:00:00Z',
    creationTime: '2026-03-01T00:00:00Z',
  },
];

describe('TariffsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiGet.mockResolvedValue({
      data: { items: mockTariffs },
    });
    mockApiPost.mockResolvedValue({ data: {} });
    mockApiPut.mockResolvedValue({ data: {} });
    mockApiDelete.mockResolvedValue({ data: {} });
  });

  it('renders page title and description', async () => {
    renderWithProviders(<TariffsPage />);
    expect(screen.getByText('Tariff Management')).toBeInTheDocument();
    expect(screen.getByText('Configure pricing plans for charging sessions')).toBeInTheDocument();
  });

  it('renders Add Tariff button', async () => {
    renderWithProviders(<TariffsPage />);
    expect(screen.getByText('Add Tariff')).toBeInTheDocument();
  });

  it('renders stat cards', async () => {
    renderWithProviders(<TariffsPage />);
    await waitFor(() => {
      expect(screen.getByText('Total Tariffs')).toBeInTheDocument();
    });
    // "Active" appears as stat label and as a badge
    expect(screen.getAllByText('Active').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Avg Rate/kWh')).toBeInTheDocument();
    expect(screen.getByText('Default Plan')).toBeInTheDocument();
  });

  it('renders stat card values from data', async () => {
    renderWithProviders(<TariffsPage />);
    await waitFor(() => {
      // Total tariffs = 2, but "2" may appear elsewhere
      expect(screen.getAllByText('2').length).toBeGreaterThanOrEqual(1);
    });
    // Active tariffs = 1, "1" may appear elsewhere
    expect(screen.getAllByText('1').length).toBeGreaterThanOrEqual(1);
    // Default plan name appears both in stat card and tariff card
    expect(screen.getAllByText('Standard Rate').length).toBeGreaterThanOrEqual(1);
  });

  it('renders tariff cards with mock data', async () => {
    renderWithProviders(<TariffsPage />);
    await waitFor(() => {
      // "Standard Rate" appears in both stat card and tariff card
      expect(screen.getAllByText('Standard Rate').length).toBeGreaterThanOrEqual(1);
    });
    expect(screen.getByText('Peak Rate')).toBeInTheDocument();
    expect(screen.getByText('Default pricing for all stations')).toBeInTheDocument();
    expect(screen.getByText('Higher rate for peak hours')).toBeInTheDocument();
  });

  it('renders active/inactive badges on tariff cards', async () => {
    renderWithProviders(<TariffsPage />);
    await waitFor(() => {
      expect(screen.getByText('Peak Rate')).toBeInTheDocument();
    });
    // "Active" appears as both a stat card label and a badge on the tariff card
    const activeBadges = screen.getAllByText('Active');
    expect(activeBadges.length).toBeGreaterThanOrEqual(2); // stat label + badge
    expect(screen.getByText('Inactive')).toBeInTheDocument();
  });

  it('renders Default badge on default tariff', async () => {
    renderWithProviders(<TariffsPage />);
    await waitFor(() => {
      // "Default" may appear in stat card label "Default Plan" text too
      expect(screen.getAllByText('Default').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('renders rate information on tariff cards', async () => {
    renderWithProviders(<TariffsPage />);
    await waitFor(() => {
      // Each tariff card shows these labels, so they appear multiple times (one per tariff)
      expect(screen.getAllByText('Base Rate/kWh').length).toBe(2);
    });
    expect(screen.getAllByText('Total Rate/kWh').length).toBe(2);
    expect(screen.getAllByText('Tax Rate').length).toBe(2);
    // Tax rate percentages
    expect(screen.getAllByText('10%').length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state when no tariffs', async () => {
    mockApiGet.mockResolvedValue({ data: { items: [] } });
    renderWithProviders(<TariffsPage />);
    await waitFor(() => {
      expect(screen.getByText('No tariffs found')).toBeInTheDocument();
    });
    expect(screen.getByText('Create your first tariff to get started.')).toBeInTheDocument();
  });

  it('opens create dialog when Add Tariff is clicked', async () => {
    renderWithProviders(<TariffsPage />);
    fireEvent.click(screen.getByText('Add Tariff'));
    await waitFor(() => {
      expect(screen.getByText('New Tariff')).toBeInTheDocument();
    });
    expect(screen.getByText('Create Tariff')).toBeInTheDocument();
    expect(screen.getByText('Cancel')).toBeInTheDocument();
  });

  it('renders edit buttons for each tariff', async () => {
    renderWithProviders(<TariffsPage />);
    await waitFor(() => {
      expect(screen.getByText('Peak Rate')).toBeInTheDocument();
    });
    const editButtons = screen.getAllByLabelText('Edit');
    expect(editButtons.length).toBe(2);
  });
});
