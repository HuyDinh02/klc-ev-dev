import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/map',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock leaflet — the map page uses require("leaflet") inside useEffect
vi.mock('leaflet', () => {
  const mockMarker = {
    addTo: vi.fn().mockReturnThis(),
    bindPopup: vi.fn().mockReturnThis(),
    remove: vi.fn(),
  };
  const mockMap = {
    setView: vi.fn().mockReturnThis(),
    remove: vi.fn(),
    fitBounds: vi.fn(),
  };
  const mockTileLayer = { addTo: vi.fn() };
  return {
    default: {
      map: vi.fn(() => mockMap),
      tileLayer: vi.fn(() => mockTileLayer),
      marker: vi.fn(() => mockMarker),
      divIcon: vi.fn(),
      latLngBounds: vi.fn(() => ({})),
      Icon: { Default: { prototype: {}, mergeOptions: vi.fn() } },
    },
  };
});

// Mock API modules
const mockGetDashboard = vi.fn();

vi.mock('@/lib/api', () => ({
  monitoringApi: {
    getDashboard: () => mockGetDashboard(),
  },
}));

import StationMapPage from '../page';

// Station statuses: 0=Offline, 1=Online, 2=Disabled, 3=Decommissioned
const mockStationsWithCoords = [
  {
    stationId: 'station-1',
    stationName: 'Station Alpha',
    status: 1, // Online
    latitude: 21.028,
    longitude: 105.854,
    totalConnectors: 4,
    availableConnectors: 2,
    chargingConnectors: 2,
    lastHeartbeat: '2026-03-08T10:00:00Z',
  },
  {
    stationId: 'station-2',
    stationName: 'Station Beta',
    status: 0, // Offline
    latitude: 21.035,
    longitude: 105.860,
    totalConnectors: 2,
    availableConnectors: 0,
    chargingConnectors: 0,
    lastHeartbeat: null,
  },
  {
    stationId: 'station-5',
    stationName: 'Station Epsilon',
    status: 1, // Online
    latitude: 21.040,
    longitude: 105.870,
    totalConnectors: 3,
    availableConnectors: 1,
    chargingConnectors: 2,
    lastHeartbeat: '2026-03-08T12:00:00Z',
  },
];

const mockStationNoCoords = {
  stationId: 'station-3',
  stationName: 'Station Gamma',
  status: 2, // Disabled
  latitude: null,
  longitude: null,
  totalConnectors: 3,
  availableConnectors: 1,
  chargingConnectors: 0,
  lastHeartbeat: '2026-03-07T08:00:00Z',
};

const mockStationNoCoords2 = {
  stationId: 'station-4',
  stationName: 'Station Delta',
  status: 3, // Decommissioned
  latitude: null,
  longitude: null,
  totalConnectors: 1,
  availableConnectors: 0,
  chargingConnectors: 0,
  lastHeartbeat: null,
};

const allStations = [...mockStationsWithCoords, mockStationNoCoords, mockStationNoCoords2];

describe('StationMapPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetDashboard.mockResolvedValue({
      data: {
        stationSummaries: allStations,
      },
    });
  });

  it('renders page title and description', async () => {
    renderWithProviders(<StationMapPage />);
    expect(screen.getByText('Station Map')).toBeInTheDocument();
    expect(screen.getByText('Geographic overview of all charging stations')).toBeInTheDocument();
  });

  it('renders all status legend badges', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      expect(screen.getByText(/Online/)).toBeInTheDocument();
      expect(screen.getByText(/Offline/)).toBeInTheDocument();
      expect(screen.getByText(/Disabled/)).toBeInTheDocument();
      expect(screen.getByText(/Decommissioned/)).toBeInTheDocument();
    });
  });

  it('renders station coordinate count text', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      // 3 stations with coords out of 5 total
      expect(screen.getByText('3 / 5 stations with coordinates')).toBeInTheDocument();
    });
  });

  it('renders the leaflet stylesheet link after hydration', async () => {
    const { container } = renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      const link = container.querySelector('link[href*="leaflet"]');
      expect(link).toBeInTheDocument();
    });
  });

  it('renders "Stations without coordinates" section', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      expect(screen.getByText('Stations without coordinates')).toBeInTheDocument();
    });
  });

  it('lists station names that have no coordinates', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Gamma')).toBeInTheDocument();
      expect(screen.getByText('Station Delta')).toBeInTheDocument();
    });
  });

  it('shows status badges for stations without coordinates', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      // Station Gamma (status 2 = Disabled), Station Delta (status 3 = Decommissioned)
      const disabledBadges = screen.getAllByText('Disabled');
      expect(disabledBadges.length).toBeGreaterThanOrEqual(1);
      const decommissionedBadges = screen.getAllByText('Decommissioned');
      expect(decommissionedBadges.length).toBeGreaterThanOrEqual(1);
    });
  });

  it('shows "all stations have coordinates" message when none are missing', async () => {
    mockGetDashboard.mockResolvedValue({
      data: {
        stationSummaries: mockStationsWithCoords, // all have coords
      },
    });
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      expect(
        screen.getByText('All stations have coordinates configured.')
      ).toBeInTheDocument();
    });
  });

  it('renders correctly with empty station list', async () => {
    mockGetDashboard.mockResolvedValue({
      data: { stationSummaries: [] },
    });
    renderWithProviders(<StationMapPage />);
    expect(screen.getByText('Station Map')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText('0 / 0 stations with coordinates')).toBeInTheDocument();
    });
    expect(
      screen.getByText('All stations have coordinates configured.')
    ).toBeInTheDocument();
  });

  it('renders online count (status 1) in Online badge', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      // online = status 1 (Station Alpha) + status 1 (Station Epsilon) = 2
      expect(screen.getByText(/Online \(2\)/)).toBeInTheDocument();
    });
  });

  it('renders offline count (status 0) in Offline badge', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      // offline = status 0 (Station Beta) = 1
      expect(screen.getByText(/Offline \(1\)/)).toBeInTheDocument();
    });
  });

  it('renders disabled count (status 2) in Disabled badge', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      // disabled = status 2 (Station Gamma) = 1
      expect(screen.getByText(/Disabled \(1\)/)).toBeInTheDocument();
    });
  });
});
