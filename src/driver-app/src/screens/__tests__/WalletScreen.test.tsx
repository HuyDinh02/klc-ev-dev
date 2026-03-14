import React from 'react';
import { render, fireEvent, waitFor, act } from '@testing-library/react-native';
import { Alert } from 'react-native';
import { WalletScreen } from '../WalletScreen';
import { walletApi } from '../../api/wallet';
import type { WalletTransaction } from '../../types';

jest.mock('../../api/wallet', () => ({
  walletApi: {
    getBalance: jest.fn(),
    getTransactions: jest.fn(),
    topUp: jest.fn(),
  },
}));

jest.mock('../../hooks/useSignalR', () => ({
  useSignalR: () => ({
    connect: jest.fn().mockResolvedValue(undefined),
  }),
}));

jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({ navigate: jest.fn(), goBack: jest.fn() }),
  useFocusEffect: (cb: () => void) => {
    const { useEffect } = require('react');
    useEffect(() => { cb(); }, []);
  },
}));

jest.spyOn(Alert, 'alert');

const mockTransactions: WalletTransaction[] = [
  { id: 'tx-1', type: 'TopUp', amount: 100000, balance: 500000, description: 'MoMo top up', createdAt: '2026-03-14T10:00:00Z' },
  { id: 'tx-2', type: 'Payment', amount: 78500, balance: 421500, description: 'Session #123', createdAt: '2026-03-14T09:00:00Z' },
  { id: 'tx-3', type: 'Refund', amount: 25000, balance: 446500, description: 'Refund for session', createdAt: '2026-03-13T15:00:00Z' },
];

beforeEach(() => {
  jest.clearAllMocks();
  (walletApi.getBalance as jest.Mock).mockResolvedValue({ balance: 500000 });
  (walletApi.getTransactions as jest.Mock).mockResolvedValue({
    items: mockTransactions,
    nextCursor: undefined,
    hasMore: false,
  });
});

describe('WalletScreen', () => {
  it('shows loading state initially', () => {
    let resolveBalance: (v: { balance: number }) => void;
    (walletApi.getBalance as jest.Mock).mockReturnValue(
      new Promise((r) => { resolveBalance = r; })
    );

    const { getByLabelText } = render(<WalletScreen />);
    expect(getByLabelText('Loading wallet')).toBeTruthy();

    act(() => { resolveBalance!({ balance: 0 }); });
  });

  it('renders wallet title', async () => {
    const { getByText } = render(<WalletScreen />);

    await waitFor(() => {
      expect(getByText('Wallet')).toBeTruthy();
    });
  });

  it('renders balance amount', async () => {
    const { getAllByText } = render(<WalletScreen />);

    await waitFor(() => {
      expect(getAllByText(/500/).length).toBeGreaterThan(0);
    });
  });

  it('renders top up button', async () => {
    const { getByText } = render(<WalletScreen />);

    await waitFor(() => {
      expect(getByText('Top Up')).toBeTruthy();
    });
  });

  it('renders quick top-up amounts', async () => {
    const { getByText } = render(<WalletScreen />);

    await waitFor(() => {
      expect(getByText(/50.000/)).toBeTruthy();
    });
  });

  it('renders transaction history', async () => {
    const { getByText } = render(<WalletScreen />);

    await waitFor(() => {
      expect(getByText('MoMo top up')).toBeTruthy();
    });
    expect(getByText('Session #123')).toBeTruthy();
    expect(getByText('Refund for session')).toBeTruthy();
  });

  it('shows confirmation when top-up is pressed', async () => {
    const { getByText } = render(<WalletScreen />);

    await waitFor(() => {
      expect(getByText('Top Up')).toBeTruthy();
    });

    fireEvent.press(getByText('Top Up'));

    expect(Alert.alert).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(String),
      expect.any(Array)
    );
  });

  it('renders empty state when no transactions', async () => {
    (walletApi.getTransactions as jest.Mock).mockResolvedValue({
      items: [],
      nextCursor: undefined,
      hasMore: false,
    });

    const { getByText } = render(<WalletScreen />);

    await waitFor(() => {
      expect(getByText('No Transactions Yet')).toBeTruthy();
    });
  });

  it('renders promotions card', async () => {
    const { getByText } = render(<WalletScreen />);

    await waitFor(() => {
      expect(getByText('Promotions')).toBeTruthy();
    });
  });

  it('renders transaction history section title', async () => {
    const { getByText } = render(<WalletScreen />);

    await waitFor(() => {
      expect(getByText('Transaction History')).toBeTruthy();
    });
  });
});
