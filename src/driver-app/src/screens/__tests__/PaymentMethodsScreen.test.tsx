import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { PaymentMethodsScreen } from '../PaymentMethodsScreen';
import { paymentsApi } from '../../api/payments';
import type { PaymentMethodInfo } from '../../types';

// Mock the payments API module
jest.mock('../../api/payments', () => ({
  paymentsApi: {
    getMethods: jest.fn(),
    addMethod: jest.fn(),
    deleteMethod: jest.fn(),
    setDefaultMethod: jest.fn(),
  },
}));

// Override the global navigation mock so we can inspect calls per test
const mockGoBack = jest.fn();
const mockNavigate = jest.fn();
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({
    goBack: mockGoBack,
    navigate: mockNavigate,
    reset: jest.fn(),
  }),
  useFocusEffect: (cb: () => void) => {
    // Execute the callback immediately for testing
    const { useEffect } = require('react');
    useEffect(() => {
      cb();
    }, []);
  },
}));

// Spy on Alert.alert
jest.spyOn(Alert, 'alert');

const mockMethods: PaymentMethodInfo[] = [
  {
    id: 'pm-1',
    type: 'MoMo',
    displayName: 'MoMo Wallet',
    isDefault: true,
    lastFourDigits: undefined,
  },
  {
    id: 'pm-2',
    type: 'Card',
    displayName: 'Visa Card',
    isDefault: false,
    lastFourDigits: '4242',
  },
];

beforeEach(() => {
  jest.clearAllMocks();
  (paymentsApi.getMethods as jest.Mock).mockResolvedValue([]);
});

describe('PaymentMethodsScreen', () => {
  it('renders loading state initially', async () => {
    // Make getMethods hang so loading persists
    let resolveGetMethods: (value: PaymentMethodInfo[]) => void;
    (paymentsApi.getMethods as jest.Mock).mockReturnValue(
      new Promise<PaymentMethodInfo[]>((resolve) => {
        resolveGetMethods = resolve;
      })
    );

    const { getByLabelText } = render(<PaymentMethodsScreen />);

    expect(getByLabelText('Loading')).toBeTruthy();

    // Cleanup
    await act(async () => {
      resolveGetMethods!([]);
    });
  });

  it('renders empty state when no methods', async () => {
    (paymentsApi.getMethods as jest.Mock).mockResolvedValue([]);

    const { getByText } = render(<PaymentMethodsScreen />);

    await waitFor(() => {
      expect(getByText('No payment methods')).toBeTruthy();
    });
    expect(getByText('Add a payment method to start charging')).toBeTruthy();
    expect(getByText('Add Payment Method')).toBeTruthy();
  });

  it('renders payment method list', async () => {
    (paymentsApi.getMethods as jest.Mock).mockResolvedValue(mockMethods);

    const { getByText, getAllByText } = render(<PaymentMethodsScreen />);

    await waitFor(() => {
      // "MoMo Wallet" appears as displayName in the card and also as
      // the translated method type name, so there may be multiple.
      const momoElements = getAllByText('MoMo Wallet');
      expect(momoElements.length).toBeGreaterThanOrEqual(1);
    });
    expect(getByText('Visa Card')).toBeTruthy();
  });

  it('shows default badge on default method', async () => {
    (paymentsApi.getMethods as jest.Mock).mockResolvedValue(mockMethods);

    const { getByText } = render(<PaymentMethodsScreen />);

    await waitFor(() => {
      expect(getByText('Default')).toBeTruthy();
    });
  });

  it('opens add method modal on button press', async () => {
    (paymentsApi.getMethods as jest.Mock).mockResolvedValue([]);

    const { getByText } = render(<PaymentMethodsScreen />);

    await waitFor(() => {
      expect(getByText('Add Payment Method')).toBeTruthy();
    });

    fireEvent.press(getByText('Add Payment Method'));

    await waitFor(() => {
      expect(getByText('MoMo Wallet', { exact: true })).toBeTruthy();
      expect(getByText('ZaloPay')).toBeTruthy();
      expect(getByText('VnPay')).toBeTruthy();
      expect(getByText('OnePay')).toBeTruthy();
      expect(getByText('Credit/Debit Card')).toBeTruthy();
    });
  });

  it('calls addMethod API when method type selected', async () => {
    const newMethod: PaymentMethodInfo = {
      id: 'pm-new',
      type: 'ZaloPay',
      displayName: 'ZaloPay Account',
      isDefault: false,
    };
    (paymentsApi.getMethods as jest.Mock).mockResolvedValue([]);
    (paymentsApi.addMethod as jest.Mock).mockResolvedValue(newMethod);

    const { getByText } = render(<PaymentMethodsScreen />);

    // Wait for empty state, then open modal
    await waitFor(() => {
      expect(getByText('Add Payment Method')).toBeTruthy();
    });
    fireEvent.press(getByText('Add Payment Method'));

    // Wait for modal content, then press ZaloPay
    await waitFor(() => {
      expect(getByText('ZaloPay')).toBeTruthy();
    });
    fireEvent.press(getByText('ZaloPay'));

    await waitFor(() => {
      expect(paymentsApi.addMethod).toHaveBeenCalledWith({ type: 'ZaloPay' });
    });
  });

  it('calls deleteMethod API with confirmation', async () => {
    (paymentsApi.getMethods as jest.Mock).mockResolvedValue(mockMethods);
    (paymentsApi.deleteMethod as jest.Mock).mockResolvedValue(undefined);

    const { getByLabelText } = render(<PaymentMethodsScreen />);

    // Wait for list to render, then press the delete button for the second method
    await waitFor(() => {
      expect(getByLabelText('Remove Method Visa Card')).toBeTruthy();
    });

    fireEvent.press(getByLabelText('Remove Method Visa Card'));

    // Alert.alert should be called with confirmation
    expect(Alert.alert).toHaveBeenCalledWith(
      'Remove Payment Method',
      'Are you sure you want to remove this payment method?',
      expect.arrayContaining([
        expect.objectContaining({ text: 'Cancel', style: 'cancel' }),
        expect.objectContaining({ text: 'Remove', style: 'destructive' }),
      ])
    );

    // Simulate pressing the destructive button
    const alertCall = (Alert.alert as jest.Mock).mock.calls[0];
    const destructiveButton = alertCall[2].find(
      (btn: any) => btn.style === 'destructive'
    );

    await act(async () => {
      await destructiveButton.onPress();
    });

    expect(paymentsApi.deleteMethod).toHaveBeenCalledWith('pm-2');
  });

  it('calls setDefaultMethod API', async () => {
    (paymentsApi.getMethods as jest.Mock).mockResolvedValue(mockMethods);
    (paymentsApi.setDefaultMethod as jest.Mock).mockResolvedValue(undefined);

    const { getByLabelText } = render(<PaymentMethodsScreen />);

    // Wait for list, then press "Set as Default" for the non-default method
    await waitFor(() => {
      expect(getByLabelText('Set as Default Visa Card')).toBeTruthy();
    });

    await act(async () => {
      fireEvent.press(getByLabelText('Set as Default Visa Card'));
    });

    await waitFor(() => {
      expect(paymentsApi.setDefaultMethod).toHaveBeenCalledWith('pm-2');
    });
  });

  it('handles API error on load', async () => {
    (paymentsApi.getMethods as jest.Mock).mockRejectedValue(
      new Error('Network error')
    );

    const { getByText } = render(<PaymentMethodsScreen />);

    await waitFor(() => {
      expect(getByText('Failed to load payment methods')).toBeTruthy();
    });
    expect(getByText('Retry')).toBeTruthy();
  });

  it('navigates back on header back press', async () => {
    (paymentsApi.getMethods as jest.Mock).mockResolvedValue(mockMethods);

    const { getByLabelText } = render(<PaymentMethodsScreen />);

    await waitFor(() => {
      expect(getByLabelText('Go Back')).toBeTruthy();
    });

    fireEvent.press(getByLabelText('Go Back'));

    expect(mockGoBack).toHaveBeenCalled();
  });

  it('shows add method button at the bottom when methods exist', async () => {
    (paymentsApi.getMethods as jest.Mock).mockResolvedValue(mockMethods);

    const { getAllByText } = render(<PaymentMethodsScreen />);

    await waitFor(() => {
      // There should be the bottom "Add Payment Method" button when methods exist
      const addButtons = getAllByText('Add Payment Method');
      expect(addButtons.length).toBeGreaterThanOrEqual(1);
    });
  });
});
