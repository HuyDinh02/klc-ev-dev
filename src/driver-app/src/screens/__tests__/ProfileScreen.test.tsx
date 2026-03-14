import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { ProfileScreen } from '../ProfileScreen';
import { profileApi, vehiclesApi } from '../../api/profile';
import { useAuthStore } from '../../stores/authStore';
import type { UserProfile, UserStatistics, Vehicle } from '../../types';

jest.mock('../../api/profile', () => ({
  profileApi: {
    get: jest.fn(),
    getStatistics: jest.fn(),
  },
  vehiclesApi: {
    getAll: jest.fn(),
  },
}));

jest.mock('../../stores/authStore', () => ({
  useAuthStore: jest.fn(),
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

const mockProfile: UserProfile = {
  id: 'user-1',
  email: 'driver@example.com',
  phoneNumber: '+84912345678',
  fullName: 'Nguyen Van A',
  isPhoneVerified: true,
  isEmailVerified: true,
};

const mockStats: UserStatistics = {
  totalSessions: 42,
  totalEnergyKwh: 1250.5,
  totalSpent: 5250000,
  totalChargingMinutes: 3600,
  co2Saved: 625,
};

const mockVehicles: Vehicle[] = [
  {
    id: 'v1',
    make: 'VinFast',
    model: 'VF8',
    year: 2025,
    licensePlate: '30A-12345',
    batteryCapacityKwh: 87,
    connectorType: 'CCS2',
    isDefault: true,
  },
];

const mockLogout = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  (useAuthStore as unknown as jest.Mock).mockReturnValue({ logout: mockLogout });
  (profileApi.get as jest.Mock).mockResolvedValue(mockProfile);
  (profileApi.getStatistics as jest.Mock).mockResolvedValue(mockStats);
  (vehiclesApi.getAll as jest.Mock).mockResolvedValue(mockVehicles);
});

describe('ProfileScreen', () => {
  it('shows loading state', () => {
    let resolve: (v: UserProfile) => void;
    (profileApi.get as jest.Mock).mockReturnValue(new Promise((r) => { resolve = r; }));

    const { getByLabelText } = render(<ProfileScreen />);
    expect(getByLabelText('Loading')).toBeTruthy();

    act(() => { resolve!(mockProfile); });
  });

  it('renders user name and email', async () => {
    const { getByText } = render(<ProfileScreen />);

    await waitFor(() => {
      expect(getByText('Nguyen Van A')).toBeTruthy();
    });
    expect(getByText('driver@example.com')).toBeTruthy();
  });

  it('renders avatar with first letter of name', async () => {
    const { getByText } = render(<ProfileScreen />);

    await waitFor(() => {
      expect(getByText('N')).toBeTruthy(); // First letter of Nguyen
    });
  });

  it('renders statistics', async () => {
    const { getByText } = render(<ProfileScreen />);

    await waitFor(() => {
      expect(getByText('42')).toBeTruthy(); // sessions
    });
    expect(getByText('1250.5')).toBeTruthy(); // kWh
    expect(getByText('625')).toBeTruthy(); // CO2
  });

  it('renders vehicle section with vehicle data', async () => {
    const { getByText } = render(<ProfileScreen />);

    await waitFor(() => {
      expect(getByText('VinFast VF8')).toBeTruthy();
    });
    expect(getByText('30A-12345')).toBeTruthy();
    expect(getByText('87 kWh • CCS2')).toBeTruthy();
  });

  it('renders menu items', async () => {
    const { getByText } = render(<ProfileScreen />);

    await waitFor(() => {
      expect(getByText('Payment Methods')).toBeTruthy();
    });
    expect(getByText('Notifications')).toBeTruthy();
    expect(getByText('Settings')).toBeTruthy();
    expect(getByText('Help & Support')).toBeTruthy();
  });

  it('navigates to settings on menu tap', async () => {
    const { getByLabelText } = render(<ProfileScreen />);

    await waitFor(() => {
      expect(getByLabelText('Settings')).toBeTruthy();
    });

    fireEvent.press(getByLabelText('Settings'));
    expect(mockNavigate).toHaveBeenCalledWith('Settings');
  });

  it('renders logout button', async () => {
    const { getByText } = render(<ProfileScreen />);

    await waitFor(() => {
      expect(getByText('Logout')).toBeTruthy();
    });
  });

  it('shows logout confirmation', async () => {
    const { getByText } = render(<ProfileScreen />);

    await waitFor(() => {
      expect(getByText('Logout')).toBeTruthy();
    });

    fireEvent.press(getByText('Logout'));

    expect(Alert.alert).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(String),
      expect.any(Array)
    );
  });

  it('calls logout when confirmed', async () => {
    const { getByText } = render(<ProfileScreen />);

    await waitFor(() => {
      expect(getByText('Logout')).toBeTruthy();
    });

    fireEvent.press(getByText('Logout'));

    const alertCall = (Alert.alert as jest.Mock).mock.calls[0];
    const destructiveButton = alertCall[2].find((b: { style: string }) => b.style === 'destructive');

    await act(async () => {
      destructiveButton.onPress();
    });

    expect(mockLogout).toHaveBeenCalled();
  });

  it('renders app version', async () => {
    const { getByText } = render(<ProfileScreen />);

    await waitFor(() => {
      expect(getByText(/1.0.0/)).toBeTruthy();
    });
  });
});
