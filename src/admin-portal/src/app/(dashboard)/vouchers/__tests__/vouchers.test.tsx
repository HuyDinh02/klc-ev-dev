import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/vouchers',
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const mockApiGet = vi.fn();
const mockApiPost = vi.fn();
const mockApiPut = vi.fn();
const mockApiDelete = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
    post: (url: string, data?: unknown) => mockApiPost(url, data),
    put: (url: string, data?: unknown) => mockApiPut(url, data),
    delete: (url: string) => mockApiDelete(url),
  },
}));

import VouchersPage from '../page';

const mockVouchers = [
  {
    id: 'voucher-1',
    code: 'SUMMER2026',
    type: 0,
    value: 50000,
    expiryDate: '2026-06-30T00:00:00Z',
    totalQuantity: 100,
    usedQuantity: 25,
    minOrderAmount: 100000,
    maxDiscountAmount: null,
    description: 'Summer promo',
    isActive: true,
    createdAt: '2026-03-01T00:00:00Z',
  },
  {
    id: 'voucher-2',
    code: 'PERCENT20',
    type: 1,
    value: 20,
    expiryDate: '2026-12-31T00:00:00Z',
    totalQuantity: 50,
    usedQuantity: 50,
    minOrderAmount: null,
    maxDiscountAmount: 200000,
    description: '20% off',
    isActive: false,
    createdAt: '2026-02-15T00:00:00Z',
  },
];

describe('VouchersPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiGet.mockImplementation((url: string) => {
      if (url.includes('/admin/vouchers')) {
        return Promise.resolve({ data: { data: mockVouchers } });
      }
      return Promise.resolve({ data: {} });
    });
    mockApiPost.mockResolvedValue({ data: {} });
    mockApiPut.mockResolvedValue({ data: {} });
    mockApiDelete.mockResolvedValue({ data: {} });
  });

  it('renders vouchers page title', async () => {
    renderWithProviders(<VouchersPage />);
    expect(screen.getByText('Voucher Management')).toBeInTheDocument();
  });

  it('renders voucher table with data', async () => {
    renderWithProviders(<VouchersPage />);
    await waitFor(() => {
      expect(screen.getByText('SUMMER2026')).toBeInTheDocument();
    });
    expect(screen.getByText('PERCENT20')).toBeInTheDocument();
  });

  it('renders voucher type badges', async () => {
    renderWithProviders(<VouchersPage />);
    await waitFor(() => {
      expect(screen.getByText('Fixed Discount')).toBeInTheDocument();
    });
    expect(screen.getByText('Percentage Discount')).toBeInTheDocument();
  });

  it('renders usage quantities', async () => {
    renderWithProviders(<VouchersPage />);
    await waitFor(() => {
      expect(screen.getByText('25/100')).toBeInTheDocument();
    });
    expect(screen.getByText('50/50')).toBeInTheDocument();
  });

  it('renders active/inactive status badges', async () => {
    renderWithProviders(<VouchersPage />);
    await waitFor(() => {
      expect(screen.getByText('SUMMER2026')).toBeInTheDocument();
    });
    // Active and Inactive badges in the table
    const activeBadges = screen.getAllByText('Active');
    const inactiveBadges = screen.getAllByText('Inactive');
    expect(activeBadges.length).toBeGreaterThanOrEqual(1);
    expect(inactiveBadges.length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state when no vouchers', async () => {
    mockApiGet.mockResolvedValue({ data: { data: [] } });
    renderWithProviders(<VouchersPage />);
    await waitFor(() => {
      expect(screen.getByText('No vouchers found')).toBeInTheDocument();
    });
  });

  it('renders filter buttons', async () => {
    renderWithProviders(<VouchersPage />);
    expect(screen.getByText('All')).toBeInTheDocument();
    // Active/Inactive filter buttons
    const activeButtons = screen.getAllByText('Active');
    expect(activeButtons.length).toBeGreaterThanOrEqual(1);
  });

  it('calls API on page load', async () => {
    renderWithProviders(<VouchersPage />);
    await waitFor(() => {
      expect(mockApiGet).toHaveBeenCalledWith(
        '/admin/vouchers',
        expect.objectContaining({ params: expect.any(Object) })
      );
    });
  });
});
