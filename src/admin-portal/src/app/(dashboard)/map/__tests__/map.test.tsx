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

const mockStationsWithCoords = [
  {
    stationId: 'station-1',
    stationName: 'Station Alpha',
    status: 1, // Available
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
    status: 2, // Occupied
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
  status: 4, // Faulted
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
  status: 3, // Unavailable
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
      expect(screen.getByText(/Available/)).toBeInTheDocument();
      expect(screen.getByText(/Occupied/)).toBeInTheDocument();
      expect(screen.getByText(/Offline/)).toBeInTheDocument();
      expect(screen.getByText(/Faulted/)).toBeInTheDocument();
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
      // Station Gamma (status 4 = Faulted), Station Delta (status 3 = Unavailable)
      const faultedBadges = screen.getAllByText('Faulted');
      expect(faultedBadges.length).toBeGreaterThanOrEqual(1);
      const unavailableBadges = screen.getAllByText('Unavailable');
      expect(unavailableBadges.length).toBeGreaterThanOrEqual(1);
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

  it('renders online count (status 1 or 2) in Available badge', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      // online = status 1 (Station Alpha) + status 2 (Station Epsilon) = 2
      expect(screen.getByText(/Available \(2\)/)).toBeInTheDocument();
    });
  });

  it('renders offline count (status 0) in Offline badge', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      // offline = status 0 (Station Beta) = 1
      expect(screen.getByText(/Offline \(1\)/)).toBeInTheDocument();
    });
  });

  it('renders faulted count (status 4) in Faulted badge', async () => {
    renderWithProviders(<StationMapPage />);
    await waitFor(() => {
      // faulted = status 4 (Station Gamma) = 1
      expect(screen.getByText(/Faulted \(1\)/)).toBeInTheDocument();
    });
  });
});
