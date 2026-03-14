import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { HomeScreen } from '../HomeScreen';
import { stationsApi } from '../../api/stations';
import { useLocationStore } from '../../stores/locationStore';
import { useSessionStore } from '../../stores/sessionStore';
import type { Station } from '../../types';

jest.mock('../../api/stations', () => ({
  stationsApi: {
    getNearby: jest.fn(),
  },
}));

jest.mock('../../stores/locationStore', () => ({
  useLocationStore: jest.fn(),
}));

jest.mock('../../stores/sessionStore', () => ({
  useSessionStore: jest.fn(),
}));

jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({
    navigate: jest.fn(),
    goBack: jest.fn(),
  }),
}));

const mockStation: Station = {
  id: 'station-1',
  name: 'K-Charge Station A',
  address: '123 Le Loi, District 1, HCMC',
  latitude: 10.762,
  longitude: 106.660,
  status: 'Online',
  isOnline: true,
  distance: 1.5,
  connectors: [
    { id: 'c1', connectorId: 1, type: 'CCS2', status: 'Available', powerKw: 50 },
    { id: 'c2', connectorId: 2, type: 'Type2', status: 'Charging', powerKw: 22 },
  ],
};

const mockStation2: Station = {
  id: 'station-2',
  name: 'K-Charge Station B',
  address: '456 Nguyen Hue, District 1, HCMC',
  latitude: 10.770,
  longitude: 106.670,
  status: 'Online',
  isOnline: true,
  connectors: [
    { id: 'c3', connectorId: 1, type: 'CHAdeMO', status: 'Available', powerKw: 60 },
  ],
};

const defaultLocationState = {
  latitude: 21.0285,
  longitude: 105.8542,
  hasPermission: true,
  isLoading: false,
  error: null,
  requestPermission: jest.fn().mockResolvedValue(true),
  updateLocation: jest.fn(),
  setLocation: jest.fn(),
};

const defaultSessionState = {
  activeSession: null,
  latestMeterValue: null,
  isConnecting: false,
  isCheckingActive: false,
  setActiveSession: jest.fn(),
  clearSession: jest.fn(),
  updateMeterValue: jest.fn(),
  updateSessionStatus: jest.fn(),
  setConnecting: jest.fn(),
  checkActiveSession: jest.fn(),
};

beforeEach(() => {
  jest.clearAllMocks();
  (useLocationStore as unknown as jest.Mock).mockReturnValue(defaultLocationState);
  (useSessionStore as unknown as jest.Mock).mockReturnValue(defaultSessionState);
  (stationsApi.getNearby as jest.Mock).mockResolvedValue({ items: [mockStation, mockStation2] });
});

describe('HomeScreen', () => {
  it('shows loading state initially', () => {
    let resolveStations: (value: { items: Station[] }) => void;
    (stationsApi.getNearby as jest.Mock).mockReturnValue(
      new Promise((resolve) => { resolveStations = resolve; })
    );

    const { getByLabelText } = render(<HomeScreen />);
    expect(getByLabelText('Loading nearby stations')).toBeTruthy();

    act(() => { resolveStations!({ items: [] }); });
  });

  it('renders station list after loading', async () => {
    const { getByText } = render(<HomeScreen />);

    await waitFor(() => {
      expect(getByText('K-Charge Station A')).toBeTruthy();
    });
    expect(getByText('K-Charge Station B')).toBeTruthy();
    expect(getByText('123 Le Loi, District 1, HCMC')).toBeTruthy();
  });

  it('shows connector badges on station cards', async () => {
    const { getByText } = render(<HomeScreen />);

    await waitFor(() => {
      expect(getByText('CCS2 50kW')).toBeTruthy();
    });
    expect(getByText('Type2 22kW')).toBeTruthy();
  });

  it('shows availability count', async () => {
    const { getByText } = render(<HomeScreen />);

    await waitFor(() => {
      expect(getByText('1/2')).toBeTruthy(); // station A: 1 available of 2
    });
    expect(getByText('1/1')).toBeTruthy(); // station B: 1 available of 1
  });

  it('renders empty state when no stations', async () => {
    (stationsApi.getNearby as jest.Mock).mockResolvedValue({ items: [] });

    const { getByText } = render(<HomeScreen />);

    await waitFor(() => {
      expect(getByText('No stations found nearby')).toBeTruthy();
    });
  });

  it('shows active session banner when session exists', async () => {
    (useSessionStore as unknown as jest.Mock).mockReturnValue({
      ...defaultSessionState,
      activeSession: {
        id: 'session-1',
        stationName: 'Station A',
        energyKwh: 12.34,
        status: 'Active',
        durationMinutes: 45,
        estimatedCost: 50000,
        connectorType: 'CCS2',
        startTime: new Date().toISOString(),
        meterStart: 100,
        stationId: 'station-1',
        connectorId: 'c1',
      },
    });

    const { getByText } = render(<HomeScreen />);

    await waitFor(() => {
      expect(getByText('12.34 kWh')).toBeTruthy();
    });
  });

  it('shows QR scanner FAB', async () => {
    const { getByText } = render(<HomeScreen />);

    await waitFor(() => {
      expect(getByText('Scan QR Code to Start')).toBeTruthy();
    });
  });

  it('toggles between list and map view', async () => {
    const { getByText } = render(<HomeScreen />);

    await waitFor(() => {
      expect(getByText('Map')).toBeTruthy();
    });

    fireEvent.press(getByText('Map'));

    await waitFor(() => {
      expect(getByText('List')).toBeTruthy();
    });
  });

  it('requests location permission on mount', async () => {
    render(<HomeScreen />);

    await waitFor(() => {
      expect(defaultLocationState.requestPermission).toHaveBeenCalled();
    });
  });

  it('handles API error gracefully', async () => {
    (stationsApi.getNearby as jest.Mock).mockRejectedValue(new Error('Network error'));

    const { getByText } = render(<HomeScreen />);

    await waitFor(() => {
      expect(getByText('No stations found nearby')).toBeTruthy();
    });
  });
});
