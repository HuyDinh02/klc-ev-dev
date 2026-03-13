import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/vehicles',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock API module — vehicles page uses `vehiclesApi`
const mockVehiclesGetAll = vi.fn();

vi.mock('@/lib/api', () => ({
  vehiclesApi: {
    getAll: (params?: unknown) => mockVehiclesGetAll(params),
  },
}));

import VehiclesPage from '../page';

const mockVehicles = [
  {
    id: 'v-1',
    userId: 'user-1',
    make: 'VinFast',
    model: 'VF e34',
    licensePlate: '30A-12345',
    color: 'White',
    year: 2025,
    batteryCapacityKwh: 42,
    preferredConnectorType: 1, // CCS2
    isActive: true,
    isDefault: true,
    nickname: null,
    creationTime: '2026-01-15T00:00:00Z',
  },
  {
    id: 'v-2',
    userId: 'user-2',
    make: 'Tesla',
    model: 'Model 3',
    licensePlate: null,
    color: 'Red',
    year: 2024,
    batteryCapacityKwh: 60,
    preferredConnectorType: 5, // NACS
    isActive: true,
    isDefault: false,
    nickname: 'My Tesla',
    creationTime: '2026-02-10T00:00:00Z',
  },
  {
    id: 'v-3',
    userId: 'user-3',
    make: 'Hyundai',
    model: 'Ioniq 5',
    licensePlate: '51A-98765',
    color: null,
    year: null,
    batteryCapacityKwh: null,
    preferredConnectorType: null,
    isActive: false,
    isDefault: false,
    nickname: null,
    creationTime: '2026-03-01T00:00:00Z',
  },
];

describe('VehiclesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockVehiclesGetAll.mockResolvedValue({
      data: { items: mockVehicles },
    });
  });

  it('renders page title and description', async () => {
    renderWithProviders(<VehiclesPage />);
    expect(screen.getByText('Vehicles')).toBeInTheDocument();
    expect(screen.getByText('Registered EV vehicles across all users')).toBeInTheDocument();
  });

  it('renders search input', async () => {
    renderWithProviders(<VehiclesPage />);
    expect(screen.getByPlaceholderText('Search by make, model, plate...')).toBeInTheDocument();
  });

  it('renders stat cards', async () => {
    renderWithProviders(<VehiclesPage />);
    await waitFor(() => {
      expect(screen.getByText('Total Vehicles')).toBeInTheDocument();
    });
    // "Active" appears as stat card label and as status badges on active vehicles
    const activeTexts = screen.getAllByText('Active');
    expect(activeTexts.length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('With CCS2')).toBeInTheDocument();
    expect(screen.getByText('Default Set')).toBeInTheDocument();
  });

  it('renders table column headers', async () => {
    renderWithProviders(<VehiclesPage />);
    await waitFor(() => {
      expect(screen.getByText('Vehicle')).toBeInTheDocument();
    });
    expect(screen.getByText('License Plate')).toBeInTheDocument();
    expect(screen.getByText('Battery')).toBeInTheDocument();
    expect(screen.getByText('Connector')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();
    expect(screen.getByText('Registered')).toBeInTheDocument();
  });

  it('renders vehicle data in table', async () => {
    renderWithProviders(<VehiclesPage />);
    await waitFor(() => {
      expect(screen.getByText('VinFast VF e34')).toBeInTheDocument();
    });
    // Tesla uses nickname as primary display
    expect(screen.getByText('My Tesla')).toBeInTheDocument();
    expect(screen.getByText('Hyundai Ioniq 5')).toBeInTheDocument();
  });

  it('renders license plates', async () => {
    renderWithProviders(<VehiclesPage />);
    await waitFor(() => {
      expect(screen.getByText('30A-12345')).toBeInTheDocument();
    });
    expect(screen.getByText('51A-98765')).toBeInTheDocument();
  });

  it('renders battery capacity', async () => {
    renderWithProviders(<VehiclesPage />);
    await waitFor(() => {
      expect(screen.getByText('42 kWh')).toBeInTheDocument();
    });
    expect(screen.getByText('60 kWh')).toBeInTheDocument();
  });

  it('renders connector type labels', async () => {
    renderWithProviders(<VehiclesPage />);
    await waitFor(() => {
      expect(screen.getByText('CCS2')).toBeInTheDocument();
    });
    expect(screen.getByText('NACS')).toBeInTheDocument();
  });

  it('shows empty state when no vehicles', async () => {
    mockVehiclesGetAll.mockResolvedValue({
      data: { items: [] },
    });
    renderWithProviders(<VehiclesPage />);
    await waitFor(() => {
      expect(screen.getByText('No vehicles registered yet')).toBeInTheDocument();
    });
    expect(screen.getByText('Vehicles will appear here once users register them in the mobile app.')).toBeInTheDocument();
  });

  it('renders total count badge', async () => {
    renderWithProviders(<VehiclesPage />);
    await waitFor(() => {
      expect(screen.getByText('VinFast VF e34')).toBeInTheDocument();
    });
    // Badge shows "3 total"
    expect(screen.getByText(/3 total/)).toBeInTheDocument();
  });

  it('renders loading state initially', () => {
    mockVehiclesGetAll.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<VehiclesPage />);
    expect(screen.getByText('Vehicles')).toBeInTheDocument();
    // Table data should not render while loading
    expect(screen.queryByText('VinFast VF e34')).not.toBeInTheDocument();
  });
});
