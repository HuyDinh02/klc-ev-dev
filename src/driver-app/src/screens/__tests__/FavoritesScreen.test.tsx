import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { FavoritesScreen } from '../FavoritesScreen';
import { favoritesApi } from '../../api/favorites';
import type { Station } from '../../types';

jest.mock('../../api/favorites', () => ({
  favoritesApi: {
    getAll: jest.fn(),
    remove: jest.fn(),
  },
}));

jest.mock('../../hooks/useSignalR', () => ({
  useSignalR: () => ({
    connect: jest.fn().mockResolvedValue(undefined),
    subscribeToStation: jest.fn(),
    unsubscribeFromStation: jest.fn(),
  }),
}));

const mockNavigate = jest.fn();
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({ navigate: mockNavigate, goBack: jest.fn() }),
  useFocusEffect: (cb: () => void) => {
    const { useEffect } = require('react');
    useEffect(() => { cb(); }, []);
  },
}));

jest.spyOn(Alert, 'alert');

const mockFavorites: Station[] = [
  {
    id: 'station-1',
    name: 'Favorite Station A',
    address: '123 Le Loi',
    latitude: 10.762,
    longitude: 106.660,
    status: 'Online',
    isOnline: true,
    distance: 2.3,
    connectors: [
      { id: 'c1', connectorId: 1, type: 'CCS2', status: 'Available', powerKw: 50 },
    ],
  },
  {
    id: 'station-2',
    name: 'Favorite Station B',
    address: '456 Nguyen Hue',
    latitude: 10.770,
    longitude: 106.670,
    status: 'Offline',
    isOnline: false,
    connectors: [
      { id: 'c2', connectorId: 1, type: 'Type2', status: 'Unavailable', powerKw: 22 },
    ],
  },
];

beforeEach(() => {
  jest.clearAllMocks();
  (favoritesApi.getAll as jest.Mock).mockResolvedValue(mockFavorites);
});

describe('FavoritesScreen', () => {
  it('shows loading state', () => {
    let resolve: (v: Station[]) => void;
    (favoritesApi.getAll as jest.Mock).mockReturnValue(new Promise((r) => { resolve = r; }));

    const { getByLabelText } = render(<FavoritesScreen />);
    expect(getByLabelText('Loading')).toBeTruthy();

    act(() => { resolve!([]); });
  });

  it('renders favorites title and count', async () => {
    const { getByText } = render(<FavoritesScreen />);

    await waitFor(() => {
      expect(getByText('Favorites')).toBeTruthy();
    });
  });

  it('renders favorite stations', async () => {
    const { getByText } = render(<FavoritesScreen />);

    await waitFor(() => {
      expect(getByText('Favorite Station A')).toBeTruthy();
    });
    expect(getByText('Favorite Station B')).toBeTruthy();
    expect(getByText('123 Le Loi')).toBeTruthy();
  });

  it('shows online/offline badges', async () => {
    const { getAllByText } = render(<FavoritesScreen />);

    await waitFor(() => {
      expect(getAllByText('Online').length).toBeGreaterThan(0);
    });
    expect(getAllByText('Offline').length).toBeGreaterThan(0);
  });

  it('shows connector badges', async () => {
    const { getByText } = render(<FavoritesScreen />);

    await waitFor(() => {
      expect(getByText('CCS2 50kW')).toBeTruthy();
    });
  });

  it('shows remove button for each station', async () => {
    const { getAllByText } = render(<FavoritesScreen />);

    await waitFor(() => {
      expect(getAllByText('Remove').length).toBe(2);
    });
  });

  it('shows confirmation when remove is pressed', async () => {
    const { getAllByText } = render(<FavoritesScreen />);

    await waitFor(() => {
      expect(getAllByText('Remove').length).toBe(2);
    });

    fireEvent.press(getAllByText('Remove')[0]);

    expect(Alert.alert).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(String),
      expect.any(Array)
    );
  });

  it('removes favorite when confirmed', async () => {
    (favoritesApi.remove as jest.Mock).mockResolvedValue({});

    const { getAllByText } = render(<FavoritesScreen />);

    await waitFor(() => {
      expect(getAllByText('Remove').length).toBe(2);
    });

    fireEvent.press(getAllByText('Remove')[0]);

    const alertCall = (Alert.alert as jest.Mock).mock.calls[0];
    const destructiveButton = alertCall[2].find((b: { style: string }) => b.style === 'destructive');

    await act(async () => {
      await destructiveButton.onPress();
    });

    expect(favoritesApi.remove).toHaveBeenCalledWith('station-1');
  });

  it('shows empty state when no favorites', async () => {
    (favoritesApi.getAll as jest.Mock).mockResolvedValue([]);

    const { getByText } = render(<FavoritesScreen />);

    await waitFor(() => {
      expect(getByText('No favorite stations')).toBeTruthy();
    });
  });
});
