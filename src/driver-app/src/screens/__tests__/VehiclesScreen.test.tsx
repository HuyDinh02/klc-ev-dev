import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { VehiclesScreen } from '../VehiclesScreen';
import { vehiclesApi } from '../../api/profile';
import type { Vehicle } from '../../types';

jest.mock('../../api/profile', () => ({
  vehiclesApi: {
    getAll: jest.fn(),
    add: jest.fn(),
    setDefault: jest.fn(),
    delete: jest.fn(),
  },
}));

jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({ navigate: jest.fn(), goBack: jest.fn() }),
  useFocusEffect: (cb: () => void) => {
    const { useEffect } = require('react');
    useEffect(() => { cb(); }, []);
  },
}));

jest.spyOn(Alert, 'alert');

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
  {
    id: 'v2',
    make: 'Tesla',
    model: 'Model 3',
    year: 2024,
    licensePlate: '29B-67890',
    batteryCapacityKwh: 75,
    connectorType: 'NACS',
    isDefault: false,
  },
];

beforeEach(() => {
  jest.clearAllMocks();
  (vehiclesApi.getAll as jest.Mock).mockResolvedValue(mockVehicles);
});

describe('VehiclesScreen', () => {
  it('shows loading state', () => {
    let resolve: (v: Vehicle[]) => void;
    (vehiclesApi.getAll as jest.Mock).mockReturnValue(new Promise((r) => { resolve = r; }));

    const { getByLabelText } = render(<VehiclesScreen />);
    expect(getByLabelText('Loading')).toBeTruthy();

    act(() => { resolve!([]); });
  });

  it('renders vehicles title', async () => {
    const { getByText } = render(<VehiclesScreen />);

    await waitFor(() => {
      expect(getByText('My Vehicles')).toBeTruthy();
    });
  });

  it('renders vehicle list', async () => {
    const { getByText } = render(<VehiclesScreen />);

    await waitFor(() => {
      expect(getByText('VinFast VF8')).toBeTruthy();
    });
    expect(getByText('Tesla Model 3')).toBeTruthy();
    expect(getByText('30A-12345')).toBeTruthy();
    expect(getByText('29B-67890')).toBeTruthy();
  });

  it('shows default badge for default vehicle', async () => {
    const { getByText } = render(<VehiclesScreen />);

    await waitFor(() => {
      expect(getByText('Default')).toBeTruthy();
    });
  });

  it('shows vehicle details', async () => {
    const { getByText } = render(<VehiclesScreen />);

    await waitFor(() => {
      expect(getByText('87 kWh')).toBeTruthy();
    });
    expect(getByText('CCS2')).toBeTruthy();
  });

  it('shows delete option for vehicles', async () => {
    const { getAllByText } = render(<VehiclesScreen />);

    await waitFor(() => {
      expect(getAllByText('Delete').length).toBeGreaterThan(0);
    });
  });

  it('shows delete confirmation dialog', async () => {
    const { getAllByText } = render(<VehiclesScreen />);

    await waitFor(() => {
      expect(getAllByText('Delete').length).toBeGreaterThan(0);
    });

    fireEvent.press(getAllByText('Delete')[0]);

    expect(Alert.alert).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(String),
      expect.any(Array)
    );
  });

  it('shows empty state when no vehicles', async () => {
    (vehiclesApi.getAll as jest.Mock).mockResolvedValue([]);

    const { getByText } = render(<VehiclesScreen />);

    await waitFor(() => {
      expect(getByText('No vehicles added')).toBeTruthy();
    });
  });

  it('shows set as default button for non-default vehicles', async () => {
    const { getByText } = render(<VehiclesScreen />);

    await waitFor(() => {
      expect(getByText('Set as Default')).toBeTruthy();
    });
  });

  it('renders add vehicle FAB when vehicles exist', async () => {
    const { getByLabelText } = render(<VehiclesScreen />);

    await waitFor(() => {
      expect(getByLabelText('Add vehicle')).toBeTruthy();
    });
  });
});
