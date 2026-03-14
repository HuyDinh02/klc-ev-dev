import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { StationDetailScreen } from '../StationDetailScreen';
import { stationsApi } from '../../api/stations';
import { sessionsApi } from '../../api/sessions';
import { useSessionStore } from '../../stores/sessionStore';
import type { Station } from '../../types';

jest.mock('../../api/stations', () => ({
  stationsApi: { getById: jest.fn() },
}));

jest.mock('../../api/sessions', () => ({
  sessionsApi: { start: jest.fn() },
}));

jest.mock('../../stores/sessionStore', () => ({
  useSessionStore: jest.fn(),
}));

const mockNavigate = jest.fn();
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({ navigate: mockNavigate, goBack: jest.fn() }),
  useRoute: () => ({ params: { stationId: 'station-1' } }),
}));

jest.spyOn(Alert, 'alert');

const mockStation: Station = {
  id: 'station-1',
  name: 'K-Charge Station A',
  address: '123 Le Loi, District 1, HCMC',
  latitude: 10.762,
  longitude: 106.660,
  status: 'Online',
  isOnline: true,
  connectors: [
    { id: 'c1', connectorId: 1, type: 'CCS2', status: 'Available', powerKw: 50 },
    { id: 'c2', connectorId: 2, type: 'Type2', status: 'Charging', powerKw: 22, currentSessionId: 'sess-1' },
  ],
};

const defaultSessionState = {
  activeSession: null,
  latestMeterValue: null,
  isConnecting: false,
  setActiveSession: jest.fn(),
  setConnecting: jest.fn(),
  clearSession: jest.fn(),
  updateMeterValue: jest.fn(),
  updateSessionStatus: jest.fn(),
  isCheckingActive: false,
  checkActiveSession: jest.fn(),
};

beforeEach(() => {
  jest.clearAllMocks();
  (useSessionStore as unknown as jest.Mock).mockReturnValue(defaultSessionState);
  (stationsApi.getById as jest.Mock).mockResolvedValue(mockStation);
});

describe('StationDetailScreen', () => {
  it('shows loading state initially', () => {
    let resolve: (v: Station) => void;
    (stationsApi.getById as jest.Mock).mockReturnValue(new Promise((r) => { resolve = r; }));

    const { getByLabelText } = render(<StationDetailScreen />);
    expect(getByLabelText('Loading')).toBeTruthy();

    act(() => { resolve!(mockStation); });
  });

  it('renders station info after loading', async () => {
    const { getByText } = render(<StationDetailScreen />);

    await waitFor(() => {
      expect(getByText('K-Charge Station A')).toBeTruthy();
    });
    expect(getByText('123 Le Loi, District 1, HCMC')).toBeTruthy();
    expect(getByText('Online')).toBeTruthy();
  });

  it('renders connectors section', async () => {
    const { getByText } = render(<StationDetailScreen />);

    await waitFor(() => {
      expect(getByText('#1')).toBeTruthy();
    });
    expect(getByText('#2')).toBeTruthy();
    expect(getByText('CCS2')).toBeTruthy();
    expect(getByText('50 kW')).toBeTruthy();
    expect(getByText('Available')).toBeTruthy();
    expect(getByText('Charging')).toBeTruthy();
  });

  it('shows start charging button for available connectors', async () => {
    const { getByText } = render(<StationDetailScreen />);

    await waitFor(() => {
      expect(getByText('Start Charging')).toBeTruthy();
    });
  });

  it('shows in-use indicator for charging connectors', async () => {
    const { getByText } = render(<StationDetailScreen />);

    await waitFor(() => {
      expect(getByText('In use')).toBeTruthy();
    });
  });

  it('starts charging session successfully', async () => {
    const mockSession = {
      id: 'new-session',
      stationId: 'station-1',
      stationName: 'K-Charge Station A',
      connectorId: 'c1',
      connectorType: 'CCS2',
      status: 'Active',
      startTime: new Date().toISOString(),
      energyKwh: 0,
      durationMinutes: 0,
      estimatedCost: 0,
      meterStart: 0,
    };
    (sessionsApi.start as jest.Mock).mockResolvedValue(mockSession);

    const { getByText } = render(<StationDetailScreen />);

    await waitFor(() => {
      expect(getByText('Start Charging')).toBeTruthy();
    });

    await act(async () => {
      fireEvent.press(getByText('Start Charging'));
    });

    expect(sessionsApi.start).toHaveBeenCalledWith({ connectorId: 'c1' });
    expect(defaultSessionState.setActiveSession).toHaveBeenCalledWith(mockSession);
    expect(mockNavigate).toHaveBeenCalledWith('Session');
  });

  it('shows error when start charging fails', async () => {
    (sessionsApi.start as jest.Mock).mockRejectedValue(new Error('Failed'));

    const { getByText } = render(<StationDetailScreen />);

    await waitFor(() => {
      expect(getByText('Start Charging')).toBeTruthy();
    });

    await act(async () => {
      fireEvent.press(getByText('Start Charging'));
    });

    expect(Alert.alert).toHaveBeenCalledWith('Error', expect.any(String));
  });

  it('shows not found state when station is null', async () => {
    (stationsApi.getById as jest.Mock).mockRejectedValue(new Error('Not found'));

    const { getByText } = render(<StationDetailScreen />);

    await waitFor(() => {
      expect(getByText('Station not found')).toBeTruthy();
    });
  });

  it('renders scan QR button', async () => {
    const { getByText } = render(<StationDetailScreen />);

    await waitFor(() => {
      expect(getByText('Scan QR Code to Start')).toBeTruthy();
    });
  });
});
