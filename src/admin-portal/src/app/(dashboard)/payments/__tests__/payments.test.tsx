import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
const mockPush = vi.fn();
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: mockPush, replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/payments',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock API module (payments page uses `api` directly via api.get/post)
const mockApiGet = vi.fn();
const mockApiPost = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
    post: (url: string, data?: unknown) => mockApiPost(url, data),
  },
}));

import PaymentsPage from '../page';

const mockPayments = [
  {
    id: 'pay-1',
    sessionId: 'session-abc12345',
    amount: 250000,
    status: 2, // Completed
    gateway: 1, // MoMo
    referenceCode: 'TXN-001',
    stationName: 'Station Alpha',
    creationTime: '2026-03-08T10:00:00Z',
  },
  {
    id: 'pay-2',
    sessionId: 'session-def67890',
    amount: 180000,
    status: 0, // Pending
    gateway: 4, // VnPay
    referenceCode: 'TXN-002',
    stationName: 'Station Beta',
    creationTime: '2026-03-08T11:00:00Z',
  },
  {
    id: 'pay-3',
    sessionId: 'session-ghi11111',
    amount: 50000,
    status: 3, // Failed
    gateway: 0, // ZaloPay
    referenceCode: 'TXN-003',
    stationName: 'Station Gamma',
    creationTime: '2026-03-08T12:00:00Z',
  },
];

describe('PaymentsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiGet.mockResolvedValue({
      data: { items: mockPayments, totalCount: 3 },
    });
    mockApiPost.mockResolvedValue({ data: {} });
  });

  it('renders page title and description', async () => {
    renderWithProviders(<PaymentsPage />);
    expect(screen.getByText('Payments')).toBeInTheDocument();
    expect(screen.getByText('View and manage payment transactions')).toBeInTheDocument();
  });

  it('renders stat cards', async () => {
    renderWithProviders(<PaymentsPage />);
    await waitFor(() => {
      expect(screen.getByText("Today's Revenue")).toBeInTheDocument();
    });
    expect(screen.getByText('Monthly Revenue')).toBeInTheDocument();
    // "Pending" and "Failed" appear both as stat card labels and as filter options,
    // so use getAllByText to verify they are present
    expect(screen.getAllByText('Pending').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Failed').length).toBeGreaterThanOrEqual(1);
  });

  it('renders export button', async () => {
    renderWithProviders(<PaymentsPage />);
    expect(screen.getByText('Export')).toBeInTheDocument();
  });

  it('renders search input', async () => {
    renderWithProviders(<PaymentsPage />);
    expect(screen.getByLabelText('Search by transaction ID or user...')).toBeInTheDocument();
  });

  it('renders status filter dropdown', async () => {
    renderWithProviders(<PaymentsPage />);
    expect(screen.getByLabelText('payments.filterByStatus')).toBeInTheDocument();
  });

  it('renders table column headers', async () => {
    renderWithProviders(<PaymentsPage />);
    await waitFor(() => {
      expect(screen.getByText('Transaction')).toBeInTheDocument();
    });
    expect(screen.getByText('Station')).toBeInTheDocument();
    expect(screen.getByText('Session')).toBeInTheDocument();
    expect(screen.getByText('Amount')).toBeInTheDocument();
    expect(screen.getByText('Method')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();
    expect(screen.getByText('Date')).toBeInTheDocument();
    expect(screen.getByText('Actions')).toBeInTheDocument();
  });

  it('renders payment data in table', async () => {
    renderWithProviders(<PaymentsPage />);
    await waitFor(() => {
      expect(screen.getByText('TXN-001')).toBeInTheDocument();
    });
    expect(screen.getByText('TXN-002')).toBeInTheDocument();
    expect(screen.getByText('TXN-003')).toBeInTheDocument();
    expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    expect(screen.getByText('Station Beta')).toBeInTheDocument();
    expect(screen.getByText('Station Gamma')).toBeInTheDocument();
  });

  it('renders payment gateway labels', async () => {
    renderWithProviders(<PaymentsPage />);
    await waitFor(() => {
      expect(screen.getByText('MoMo')).toBeInTheDocument();
    });
    expect(screen.getByText('VnPay')).toBeInTheDocument();
    expect(screen.getByText('ZaloPay')).toBeInTheDocument();
  });

  it('renders refund button for completed payments only', async () => {
    renderWithProviders(<PaymentsPage />);
    await waitFor(() => {
      expect(screen.getByText('TXN-001')).toBeInTheDocument();
    });
    // pay-1 is completed (status 2), should have refund button
    const refundButtons = screen.getAllByLabelText('Refund');
    expect(refundButtons.length).toBe(1);
  });

  it('renders view details buttons for all payments', async () => {
    renderWithProviders(<PaymentsPage />);
    await waitFor(() => {
      expect(screen.getByText('TXN-001')).toBeInTheDocument();
    });
    const viewButtons = screen.getAllByLabelText('View details');
    expect(viewButtons.length).toBe(3);
  });

  it('shows empty state when no payments found', async () => {
    mockApiGet.mockResolvedValue({ data: { items: [], totalCount: 0 } });
    renderWithProviders(<PaymentsPage />);
    await waitFor(() => {
      expect(screen.getByText('No payments found')).toBeInTheDocument();
    });
    expect(screen.getByText('Try adjusting your filters or search query.')).toBeInTheDocument();
  });

  it('renders date filter inputs', async () => {
    renderWithProviders(<PaymentsPage />);
    expect(screen.getByLabelText('payments.dateFrom')).toBeInTheDocument();
    expect(screen.getByLabelText('payments.dateTo')).toBeInTheDocument();
  });

  it('clicking view details navigates to payment detail', async () => {
    renderWithProviders(<PaymentsPage />);
    await waitFor(() => {
      expect(screen.getByText('TXN-001')).toBeInTheDocument();
    });
    const viewButtons = screen.getAllByLabelText('View details');
    fireEvent.click(viewButtons[0]);
    expect(mockPush).toHaveBeenCalledWith('/payments/pay-1');
  });

  it('renders loading state initially', () => {
    mockApiGet.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<PaymentsPage />);
    expect(screen.getByText('Payments')).toBeInTheDocument();
    // Table should not render while loading
    expect(screen.queryByText('TXN-001')).not.toBeInTheDocument();
  });
});
