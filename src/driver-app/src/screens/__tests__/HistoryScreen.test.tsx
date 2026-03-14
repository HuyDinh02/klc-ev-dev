import React from 'react';
import { render, waitFor, act } from '@testing-library/react-native';
import { HistoryScreen } from '../HistoryScreen';
import { sessionsApi } from '../../api/sessions';
import type { ChargingSession } from '../../types';

jest.mock('../../api/sessions', () => ({
  sessionsApi: {
    getHistory: jest.fn(),
  },
}));

jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({ navigate: jest.fn(), goBack: jest.fn() }),
  useFocusEffect: (cb: () => void) => {
    const { useEffect } = require('react');
    useEffect(() => { cb(); }, []);
  },
}));

const mockSessions: ChargingSession[] = [
  {
    id: 'sess-1',
    stationId: 'station-1',
    stationName: 'K-Charge Station A',
    connectorId: 'c1',
    connectorType: 'CCS2',
    status: 'Completed',
    startTime: '2026-03-14T10:00:00Z',
    endTime: '2026-03-14T11:30:00Z',
    energyKwh: 35.5,
    durationMinutes: 90,
    estimatedCost: 150000,
    actualCost: 148500,
    meterStart: 1000,
    meterStop: 1035.5,
  },
  {
    id: 'sess-2',
    stationId: 'station-2',
    stationName: 'K-Charge Station B',
    connectorId: 'c2',
    connectorType: 'Type2',
    status: 'Failed',
    startTime: '2026-03-13T14:00:00Z',
    energyKwh: 2.1,
    durationMinutes: 15,
    estimatedCost: 10000,
    meterStart: 500,
  },
];

beforeEach(() => {
  jest.clearAllMocks();
  (sessionsApi.getHistory as jest.Mock).mockResolvedValue({
    items: mockSessions,
    nextCursor: undefined,
    hasMore: false,
  });
});

describe('HistoryScreen', () => {
  it('shows loading state', () => {
    let resolve: (v: unknown) => void;
    (sessionsApi.getHistory as jest.Mock).mockReturnValue(
      new Promise((r) => { resolve = r; })
    );

    const { getByLabelText } = render(<HistoryScreen />);
    expect(getByLabelText('Loading')).toBeTruthy();

    act(() => { resolve!({ items: [], hasMore: false }); });
  });

  it('renders history title', async () => {
    const { getByText } = render(<HistoryScreen />);

    await waitFor(() => {
      expect(getByText('Charging History')).toBeTruthy();
    });
  });

  it('renders session cards', async () => {
    const { getByText } = render(<HistoryScreen />);

    await waitFor(() => {
      expect(getByText('K-Charge Station A')).toBeTruthy();
    });
    expect(getByText('K-Charge Station B')).toBeTruthy();
  });

  it('shows energy and duration for sessions', async () => {
    const { getByText } = render(<HistoryScreen />);

    await waitFor(() => {
      expect(getByText('35.50')).toBeTruthy(); // energy
    });
    expect(getByText('1h 30m')).toBeTruthy(); // duration
  });

  it('shows status badges', async () => {
    const { getByText } = render(<HistoryScreen />);

    await waitFor(() => {
      expect(getByText('Completed')).toBeTruthy();
    });
    expect(getByText('Failed')).toBeTruthy();
  });

  it('shows connector type', async () => {
    const { getByText } = render(<HistoryScreen />);

    await waitFor(() => {
      expect(getByText('CCS2')).toBeTruthy();
    });
    expect(getByText('Type2')).toBeTruthy();
  });

  it('shows empty state when no sessions', async () => {
    (sessionsApi.getHistory as jest.Mock).mockResolvedValue({
      items: [],
      nextCursor: undefined,
      hasMore: false,
    });

    const { getByText } = render(<HistoryScreen />);

    await waitFor(() => {
      expect(getByText('No charging history')).toBeTruthy();
    });
  });

  it('handles API error gracefully', async () => {
    (sessionsApi.getHistory as jest.Mock).mockRejectedValue(new Error('Network'));

    const { getByText } = render(<HistoryScreen />);

    await waitFor(() => {
      expect(getByText('No charging history')).toBeTruthy();
    });
  });
});
