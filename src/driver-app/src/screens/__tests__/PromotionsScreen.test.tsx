import React from 'react';
import { render, waitFor, act } from '@testing-library/react-native';
import { PromotionsScreen } from '../PromotionsScreen';
import { promotionsApi } from '../../api/promotions';
import type { Promotion } from '../../types';

jest.mock('../../api/promotions', () => ({
  promotionsApi: {
    getPromotions: jest.fn(),
  },
}));

jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({ navigate: jest.fn(), goBack: jest.fn() }),
  useFocusEffect: (cb: () => void) => {
    const { useEffect } = require('react');
    useEffect(() => { cb(); }, []);
  },
}));

const mockPromotions: Promotion[] = [
  {
    id: 'p1',
    name: 'Weekend Special',
    description: 'Get 20% off all charging on weekends',
    discountType: 0, // Percentage
    discountValue: 20,
    startDate: '2026-03-01T00:00:00Z',
    endDate: '2026-12-31T23:59:59Z',
    isActive: true,
    maxUsageCount: 100,
    currentUsageCount: 45,
    minimumChargeAmount: 50000,
  },
  {
    id: 'p2',
    name: 'New User Bonus',
    description: 'First charge discount',
    discountType: 1, // Fixed amount
    discountValue: 30000,
    startDate: '2026-01-01T00:00:00Z',
    endDate: '2025-12-31T23:59:59Z', // expired
    isActive: false,
  },
];

beforeEach(() => {
  jest.clearAllMocks();
  (promotionsApi.getPromotions as jest.Mock).mockResolvedValue({
    items: mockPromotions,
    nextCursor: undefined,
    hasMore: false,
  });
});

describe('PromotionsScreen', () => {
  it('shows loading state', () => {
    let resolve: (v: unknown) => void;
    (promotionsApi.getPromotions as jest.Mock).mockReturnValue(
      new Promise((r) => { resolve = r; })
    );

    const { getByLabelText } = render(<PromotionsScreen />);
    expect(getByLabelText('Loading promotions')).toBeTruthy();

    act(() => { resolve!({ items: [], hasMore: false }); });
  });

  it('renders promotions title', async () => {
    const { getByText } = render(<PromotionsScreen />);

    await waitFor(() => {
      expect(getByText('Promotions')).toBeTruthy();
    });
  });

  it('renders promotion names', async () => {
    const { getByText } = render(<PromotionsScreen />);

    await waitFor(() => {
      expect(getByText('Weekend Special')).toBeTruthy();
    });
    expect(getByText('New User Bonus')).toBeTruthy();
  });

  it('shows active/expired badges', async () => {
    const { getByText } = render(<PromotionsScreen />);

    await waitFor(() => {
      expect(getByText('Active')).toBeTruthy();
    });
    expect(getByText('Expired')).toBeTruthy();
  });

  it('shows discount badge for percentage type', async () => {
    const { getByText } = render(<PromotionsScreen />);

    await waitFor(() => {
      expect(getByText('20% OFF')).toBeTruthy();
    });
  });

  it('shows promotion description', async () => {
    const { getByText } = render(<PromotionsScreen />);

    await waitFor(() => {
      expect(getByText('Get 20% off all charging on weekends')).toBeTruthy();
    });
  });

  it('shows empty state when no promotions', async () => {
    (promotionsApi.getPromotions as jest.Mock).mockResolvedValue({
      items: [],
      nextCursor: undefined,
      hasMore: false,
    });

    const { getByText } = render(<PromotionsScreen />);

    await waitFor(() => {
      expect(getByText('No promotions available')).toBeTruthy();
    });
  });

  it('shows back button', async () => {
    const { getByLabelText } = render(<PromotionsScreen />);

    await waitFor(() => {
      expect(getByLabelText('Go Back')).toBeTruthy();
    });
  });
});
