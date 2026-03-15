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

  // --- Add Vehicle Modal ---

  describe('Add Vehicle Modal', () => {
    it('opens modal when FAB is pressed', async () => {
      const { getByLabelText, getByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByLabelText('Add vehicle')).toBeTruthy();
      });

      fireEvent.press(getByLabelText('Add vehicle'));

      await waitFor(() => {
        expect(getByText('Make *')).toBeTruthy();
      });
      expect(getByText('Model *')).toBeTruthy();
      expect(getByText('Year *')).toBeTruthy();
      expect(getByText('License Plate *')).toBeTruthy();
      expect(getByText('Battery Capacity (kWh) *')).toBeTruthy();
      expect(getByText('Connector Type *')).toBeTruthy();
    });

    it('opens modal from empty state "Add Your First Vehicle" button', async () => {
      (vehiclesApi.getAll as jest.Mock).mockResolvedValue([]);

      const { getByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByText('Add Your First Vehicle')).toBeTruthy();
      });

      fireEvent.press(getByText('Add Your First Vehicle'));

      await waitFor(() => {
        expect(getByText('Make *')).toBeTruthy();
      });
    });

    it('fills form fields and submits successfully', async () => {
      const newVehicle: Vehicle = {
        id: 'v3',
        make: 'Hyundai',
        model: 'Ioniq 5',
        year: 2025,
        licensePlate: '51F-99999',
        batteryCapacityKwh: 72.6,
        connectorType: 'CCS2',
        isDefault: false,
      };
      (vehiclesApi.add as jest.Mock).mockResolvedValue(newVehicle);

      const { getByLabelText, getByText, getAllByText, queryByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByLabelText('Add vehicle')).toBeTruthy();
      });

      fireEvent.press(getByLabelText('Add vehicle'));

      await waitFor(() => {
        expect(getByText('Make *')).toBeTruthy();
      });

      fireEvent.changeText(getByLabelText('Make'), 'Hyundai');
      fireEvent.changeText(getByLabelText('Model'), 'Ioniq 5');
      fireEvent.changeText(getByLabelText('Year'), '2025');
      fireEvent.changeText(getByLabelText('License plate'), '51F-99999');
      fireEvent.changeText(getByLabelText('Battery capacity in kilowatt hours'), '72.6');

      // CCS2 is default, so it should already be selected
      // Press submit (second "Add Vehicle" is the button; first is modal title)
      const addVehicleElements = getAllByText('Add Vehicle');
      fireEvent.press(addVehicleElements[addVehicleElements.length - 1]);

      await waitFor(() => {
        expect(vehiclesApi.add).toHaveBeenCalledWith({
          make: 'Hyundai',
          model: 'Ioniq 5',
          year: 2025,
          licensePlate: '51F-99999',
          batteryCapacityKwh: 72.6,
          connectorType: 'CCS2',
        });
      });

      // Modal should close after successful add
      await waitFor(() => {
        expect(queryByText('Make *')).toBeNull();
      });

      // New vehicle should appear in list
      expect(getByText('Hyundai Ioniq 5')).toBeTruthy();
    });

    it('selects a different connector type', async () => {
      const newVehicle: Vehicle = {
        id: 'v3',
        make: 'Nissan',
        model: 'Leaf',
        year: 2024,
        licensePlate: '30A-11111',
        batteryCapacityKwh: 40,
        connectorType: 'CHAdeMO',
        isDefault: false,
      };
      (vehiclesApi.add as jest.Mock).mockResolvedValue(newVehicle);

      const { getByLabelText, getByText, getAllByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByLabelText('Add vehicle')).toBeTruthy();
      });

      fireEvent.press(getByLabelText('Add vehicle'));

      await waitFor(() => {
        expect(getByText('Make *')).toBeTruthy();
      });

      fireEvent.changeText(getByLabelText('Make'), 'Nissan');
      fireEvent.changeText(getByLabelText('Model'), 'Leaf');
      fireEvent.changeText(getByLabelText('Year'), '2024');
      fireEvent.changeText(getByLabelText('License plate'), '30A-11111');
      fireEvent.changeText(getByLabelText('Battery capacity in kilowatt hours'), '40');

      // Select CHAdeMO connector
      fireEvent.press(getByLabelText('Connector type CHAdeMO'));

      const addBtns = getAllByText('Add Vehicle');
      fireEvent.press(addBtns[addBtns.length - 1]);

      await waitFor(() => {
        expect(vehiclesApi.add).toHaveBeenCalledWith(
          expect.objectContaining({ connectorType: 'CHAdeMO' })
        );
      });
    });

    it('closes modal when cancel is pressed', async () => {
      const { getByLabelText, getByText, queryByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByLabelText('Add vehicle')).toBeTruthy();
      });

      fireEvent.press(getByLabelText('Add vehicle'));

      await waitFor(() => {
        expect(getByText('Make *')).toBeTruthy();
      });

      fireEvent.press(getByLabelText('Cancel'));

      // Modal form fields should disappear
      await waitFor(() => {
        expect(queryByText('Make *')).toBeNull();
      });
    });
  });

  // --- Form Validation ---

  describe('Form Validation', () => {
    it('shows validation errors for all empty required fields', async () => {
      const { getByLabelText, getByText, getAllByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByLabelText('Add vehicle')).toBeTruthy();
      });

      fireEvent.press(getByLabelText('Add vehicle'));

      await waitFor(() => {
        expect(getByText('Make *')).toBeTruthy();
      });

      // Clear the default year value
      fireEvent.changeText(getByLabelText('Year'), '');

      // Submit without filling fields (last "Add Vehicle" is the button)
      const addBtns = getAllByText('Add Vehicle');
      fireEvent.press(addBtns[addBtns.length - 1]);

      await waitFor(() => {
        expect(getByText('Make is required')).toBeTruthy();
      });
      expect(getByText('Model is required')).toBeTruthy();
      expect(getByText('License plate is required')).toBeTruthy();
      expect(getByText('Enter a valid battery capacity')).toBeTruthy();
    });

    it('shows year validation error for invalid year', async () => {
      const { getByLabelText, getByText, getAllByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByLabelText('Add vehicle')).toBeTruthy();
      });

      fireEvent.press(getByLabelText('Add vehicle'));

      await waitFor(() => {
        expect(getByText('Make *')).toBeTruthy();
      });

      fireEvent.changeText(getByLabelText('Make'), 'Tesla');
      fireEvent.changeText(getByLabelText('Model'), 'Model Y');
      fireEvent.changeText(getByLabelText('Year'), '1980');
      fireEvent.changeText(getByLabelText('License plate'), '30A-11111');
      fireEvent.changeText(getByLabelText('Battery capacity in kilowatt hours'), '75');

      const addBtns = getAllByText('Add Vehicle');
      fireEvent.press(addBtns[addBtns.length - 1]);

      await waitFor(() => {
        expect(getByText('Enter a valid year')).toBeTruthy();
      });

      // API should NOT have been called
      expect(vehiclesApi.add).not.toHaveBeenCalled();
    });

    it('clears field error when user types in that field', async () => {
      const { getByLabelText, getByText, getAllByText, queryByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByLabelText('Add vehicle')).toBeTruthy();
      });

      fireEvent.press(getByLabelText('Add vehicle'));

      await waitFor(() => {
        expect(getByText('Make *')).toBeTruthy();
      });

      // Submit to trigger errors (last "Add Vehicle" is the button)
      const addBtns = getAllByText('Add Vehicle');
      fireEvent.press(addBtns[addBtns.length - 1]);

      await waitFor(() => {
        expect(getByText('Make is required')).toBeTruthy();
      });

      // Type in the make field — error should clear
      fireEvent.changeText(getByLabelText('Make'), 'V');

      await waitFor(() => {
        expect(queryByText('Make is required')).toBeNull();
      });
    });

    it('does not call API when validation fails', async () => {
      const { getByLabelText, getByText, getAllByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByLabelText('Add vehicle')).toBeTruthy();
      });

      fireEvent.press(getByLabelText('Add vehicle'));

      await waitFor(() => {
        expect(getByText('Make *')).toBeTruthy();
      });

      const addBtns = getAllByText('Add Vehicle');
      fireEvent.press(addBtns[addBtns.length - 1]);

      await waitFor(() => {
        expect(getByText('Make is required')).toBeTruthy();
      });

      expect(vehiclesApi.add).not.toHaveBeenCalled();
    });
  });

  // --- Delete Vehicle ---

  describe('Delete Vehicle', () => {
    it('calls delete API when confirmed', async () => {
      (vehiclesApi.delete as jest.Mock).mockResolvedValue(undefined);

      const { getAllByText, queryByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getAllByText('Delete').length).toBeGreaterThan(0);
      });

      // Press delete on first vehicle (VinFast VF8)
      fireEvent.press(getAllByText('Delete')[0]);

      expect(Alert.alert).toHaveBeenCalledWith(
        'Delete Vehicle',
        'Are you sure you want to delete VinFast VF8?',
        expect.any(Array)
      );

      // Simulate pressing the destructive "Delete" button in the alert
      const alertButtons = (Alert.alert as jest.Mock).mock.calls[0][2];
      const deleteButton = alertButtons.find((b: { style: string }) => b.style === 'destructive');
      expect(deleteButton).toBeDefined();

      await act(async () => {
        await deleteButton.onPress();
      });

      expect(vehiclesApi.delete).toHaveBeenCalledWith('v1');

      // Vehicle should be removed from the list
      await waitFor(() => {
        expect(queryByText('VinFast VF8')).toBeNull();
      });
    });

    it('does not delete when cancel is pressed in confirmation', async () => {
      const { getAllByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getAllByText('Delete').length).toBeGreaterThan(0);
      });

      fireEvent.press(getAllByText('Delete')[0]);

      const alertButtons = (Alert.alert as jest.Mock).mock.calls[0][2];
      const cancelButton = alertButtons.find((b: { style: string }) => b.style === 'cancel');
      expect(cancelButton).toBeDefined();
      expect(cancelButton.text).toBe('Cancel');

      // Cancel doesn't call delete
      expect(vehiclesApi.delete).not.toHaveBeenCalled();
    });

    it('shows error alert when delete API fails', async () => {
      (vehiclesApi.delete as jest.Mock).mockRejectedValue(new Error('Network error'));

      const { getAllByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getAllByText('Delete').length).toBeGreaterThan(0);
      });

      fireEvent.press(getAllByText('Delete')[0]);

      const alertButtons = (Alert.alert as jest.Mock).mock.calls[0][2];
      const deleteButton = alertButtons.find((b: { style: string }) => b.style === 'destructive');

      await act(async () => {
        await deleteButton.onPress();
      });

      // Error alert should be shown
      expect(Alert.alert).toHaveBeenCalledWith(
        'Error',
        'Failed to delete vehicle.'
      );
    });
  });

  // --- Set as Default ---

  describe('Set as Default', () => {
    it('calls setDefault API when non-default vehicle is pressed', async () => {
      (vehiclesApi.setDefault as jest.Mock).mockResolvedValue(undefined);

      const { getByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByText('Set as Default')).toBeTruthy();
      });

      fireEvent.press(getByText('Set as Default'));

      await waitFor(() => {
        expect(vehiclesApi.setDefault).toHaveBeenCalledWith('v2');
      });
    });

    it('does not call setDefault for already-default vehicle', async () => {
      const { getByLabelText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByLabelText(/VinFast VF8.*Default vehicle/)).toBeTruthy();
      });

      // Press the default vehicle card
      fireEvent.press(getByLabelText(/VinFast VF8.*Default vehicle/));

      expect(vehiclesApi.setDefault).not.toHaveBeenCalled();
    });

    it('shows error alert when setDefault API fails', async () => {
      (vehiclesApi.setDefault as jest.Mock).mockRejectedValue(new Error('Server error'));

      const { getByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByText('Set as Default')).toBeTruthy();
      });

      fireEvent.press(getByText('Set as Default'));

      await waitFor(() => {
        expect(Alert.alert).toHaveBeenCalledWith(
          'Error',
          'Failed to set default vehicle.'
        );
      });
    });
  });

  // --- Error States ---

  describe('Error States', () => {
    it('shows load error with retry button', async () => {
      (vehiclesApi.getAll as jest.Mock).mockRejectedValue(new Error('Network error'));

      const { getByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByText('Failed to load vehicles. Pull to refresh.')).toBeTruthy();
      });

      expect(getByText('Retry')).toBeTruthy();
    });

    it('retries loading when retry button is pressed', async () => {
      (vehiclesApi.getAll as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const { getByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByText('Failed to load vehicles. Pull to refresh.')).toBeTruthy();
      });

      // Now mock a successful response
      (vehiclesApi.getAll as jest.Mock).mockResolvedValue(mockVehicles);

      fireEvent.press(getByText('Retry'));

      await waitFor(() => {
        expect(getByText('VinFast VF8')).toBeTruthy();
      });
    });

    it('shows error alert when add vehicle API fails', async () => {
      (vehiclesApi.add as jest.Mock).mockRejectedValue(new Error('Server error'));

      const { getByLabelText, getByText, getAllByText } = render(<VehiclesScreen />);

      await waitFor(() => {
        expect(getByLabelText('Add vehicle')).toBeTruthy();
      });

      fireEvent.press(getByLabelText('Add vehicle'));

      await waitFor(() => {
        expect(getByText('Make *')).toBeTruthy();
      });

      fireEvent.changeText(getByLabelText('Make'), 'BYD');
      fireEvent.changeText(getByLabelText('Model'), 'Atto 3');
      fireEvent.changeText(getByLabelText('Year'), '2025');
      fireEvent.changeText(getByLabelText('License plate'), '30A-22222');
      fireEvent.changeText(getByLabelText('Battery capacity in kilowatt hours'), '60');

      const addBtns = getAllByText('Add Vehicle');
      fireEvent.press(addBtns[addBtns.length - 1]);

      await waitFor(() => {
        expect(Alert.alert).toHaveBeenCalledWith(
          'Error',
          'Failed to add vehicle. Please try again.'
        );
      });
    });
  });
});
