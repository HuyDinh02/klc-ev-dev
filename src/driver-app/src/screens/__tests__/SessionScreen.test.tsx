import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { SessionScreen } from '../SessionScreen';
import { sessionsApi } from '../../api/sessions';
import { useSessionStore } from '../../stores/sessionStore';

jest.mock('../../api/sessions', () => ({
  sessionsApi: { stop: jest.fn() },
}));

jest.mock('../../stores/sessionStore', () => ({
  useSessionStore: jest.fn(),
}));

jest.mock('../../hooks/useSignalR', () => ({
  useSignalR: () => ({
    connect: jest.fn().mockResolvedValue(undefined),
    subscribeToSession: jest.fn(),
    unsubscribeFromSession: jest.fn(),
  }),
}));

const mockGoBack = jest.fn();
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({ goBack: mockGoBack, navigate: jest.fn() }),
}));

jest.spyOn(Alert, 'alert');

const mockSession = {
  id: 'session-1',
  stationId: 'station-1',
  stationName: 'K-Charge Station A',
  connectorId: 'c1',
  connectorType: 'CCS2',
  status: 'Active',
  startTime: '2026-03-14T10:00:00Z',
  energyKwh: 15.75,
  durationMinutes: 45,
  estimatedCost: 78500,
  meterStart: 1000.123,
};

const mockMeterValue = {
  timestamp: '2026-03-14T10:45:00Z',
  energyKwh: 15.75,
  powerKw: 48.5,
  soc: 72,
};

const defaultSessionState = {
  activeSession: mockSession,
  latestMeterValue: mockMeterValue,
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
  (useSessionStore as unknown as jest.Mock).mockReturnValue(defaultSessionState);
});

describe('SessionScreen', () => {
  it('renders no active session state', () => {
    (useSessionStore as unknown as jest.Mock).mockReturnValue({
      ...defaultSessionState,
      activeSession: null,
    });

    const { getByText } = render(<SessionScreen />);
    expect(getByText('No active session')).toBeTruthy();
    expect(getByText('Go Back')).toBeTruthy();
  });

  it('renders go back button in no-session state', () => {
    (useSessionStore as unknown as jest.Mock).mockReturnValue({
      ...defaultSessionState,
      activeSession: null,
    });

    const { getByText } = render(<SessionScreen />);
    fireEvent.press(getByText('Go Back'));
    expect(mockGoBack).toHaveBeenCalled();
  });

  it('renders station name and charging status', () => {
    const { getByText } = render(<SessionScreen />);
    expect(getByText('K-Charge Station A')).toBeTruthy();
    expect(getByText('Charging')).toBeTruthy();
  });

  it('renders energy delivered', () => {
    const { getByText } = render(<SessionScreen />);
    expect(getByText('15.75')).toBeTruthy();
    expect(getByText('kWh')).toBeTruthy();
  });

  it('renders power, duration and SOC stats', () => {
    const { getByText } = render(<SessionScreen />);
    expect(getByText('48.5')).toBeTruthy(); // power kW
    expect(getByText('45m')).toBeTruthy(); // duration
    expect(getByText('72%')).toBeTruthy(); // SOC
  });

  it('renders estimated cost', () => {
    const { getByText } = render(<SessionScreen />);
    // Intl.NumberFormat vi-VN VND format
    expect(getByText(/78/)).toBeTruthy();
  });

  it('renders connector info and session details', () => {
    const { getByText } = render(<SessionScreen />);
    expect(getByText('CCS2')).toBeTruthy();
    expect(getByText('1000.123 kWh')).toBeTruthy(); // meter start
  });

  it('renders stop charging button', () => {
    const { getByText } = render(<SessionScreen />);
    expect(getByText('Stop Charging')).toBeTruthy();
  });

  it('shows confirmation dialog when stop is pressed', () => {
    const { getByText } = render(<SessionScreen />);
    fireEvent.press(getByText('Stop Charging'));
    expect(Alert.alert).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(String),
      expect.arrayContaining([
        expect.objectContaining({ style: 'cancel' }),
        expect.objectContaining({ style: 'destructive' }),
      ])
    );
  });

  it('stops session when confirmed', async () => {
    (sessionsApi.stop as jest.Mock).mockResolvedValue({});

    const { getByText } = render(<SessionScreen />);
    fireEvent.press(getByText('Stop Charging'));

    // Get the destructive button handler from Alert.alert call
    const alertCall = (Alert.alert as jest.Mock).mock.calls[0];
    const destructiveButton = alertCall[2].find((b: { style: string }) => b.style === 'destructive');

    await act(async () => {
      await destructiveButton.onPress();
    });

    expect(sessionsApi.stop).toHaveBeenCalledWith('session-1');
    expect(defaultSessionState.clearSession).toHaveBeenCalled();
    expect(mockGoBack).toHaveBeenCalled();
  });

  it('shows error when stop fails', async () => {
    (sessionsApi.stop as jest.Mock).mockRejectedValue(new Error('Failed'));

    const { getByText } = render(<SessionScreen />);
    fireEvent.press(getByText('Stop Charging'));

    const alertCall = (Alert.alert as jest.Mock).mock.calls[0];
    const destructiveButton = alertCall[2].find((b: { style: string }) => b.style === 'destructive');

    await act(async () => {
      await destructiveButton.onPress();
    });

    // Second Alert.alert call is the error
    expect(Alert.alert).toHaveBeenCalledTimes(2);
  });

  it('shows -- for SOC when meter value has no soc', () => {
    (useSessionStore as unknown as jest.Mock).mockReturnValue({
      ...defaultSessionState,
      latestMeterValue: { ...mockMeterValue, soc: undefined },
    });

    const { getByText } = render(<SessionScreen />);
    expect(getByText('--%')).toBeTruthy();
  });
});
