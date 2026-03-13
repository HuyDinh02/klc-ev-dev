import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/e-invoices',
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const mockApiGet = vi.fn();
const mockApiPost = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
    post: (url: string, data?: unknown) => mockApiPost(url, data),
  },
}));

import EInvoicesPage from '../page';

const mockInvoices = [
  {
    id: 'inv-1',
    invoiceId: 'invoice-001',
    invoiceNumber: 'INV-2026-001',
    eInvoiceNumber: 'E-001',
    provider: 0,
    status: 2, // Issued
    totalAmount: 250000,
    issuedAt: '2026-03-08T10:00:00Z',
    retryCount: 0,
    creationTime: '2026-03-08T09:00:00Z',
    stationName: 'Station Alpha',
  },
  {
    id: 'inv-2',
    invoiceId: 'invoice-002',
    invoiceNumber: 'INV-2026-002',
    eInvoiceNumber: null,
    provider: 1,
    status: 0, // Pending
    totalAmount: 180000,
    issuedAt: null,
    retryCount: 0,
    creationTime: '2026-03-07T15:00:00Z',
    stationName: 'Station Beta',
  },
  {
    id: 'inv-3',
    invoiceId: 'invoice-003',
    invoiceNumber: 'INV-2026-003',
    eInvoiceNumber: null,
    provider: 2,
    status: 3, // Failed
    totalAmount: 95000,
    issuedAt: null,
    retryCount: 2,
    creationTime: '2026-03-06T08:00:00Z',
    stationName: 'Station Gamma',
  },
];

describe('EInvoicesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiGet.mockImplementation((url: string) => {
      if (url === '/e-invoices') {
        return Promise.resolve({
          data: { items: mockInvoices, totalCount: 3 },
        });
      }
      return Promise.resolve({ data: {} });
    });
    mockApiPost.mockResolvedValue({ data: {} });
  });

  it('renders e-invoices page title', async () => {
    renderWithProviders(<EInvoicesPage />);
    expect(screen.getByText('E-Invoices')).toBeInTheDocument();
  });

  it('renders invoice table with data', async () => {
    renderWithProviders(<EInvoicesPage />);
    await waitFor(() => {
      expect(screen.getByText('INV-2026-001')).toBeInTheDocument();
    });
    expect(screen.getByText('INV-2026-002')).toBeInTheDocument();
    expect(screen.getByText('INV-2026-003')).toBeInTheDocument();
  });

  it('renders station names', async () => {
    renderWithProviders(<EInvoicesPage />);
    await waitFor(() => {
      expect(screen.getByText('Station Alpha')).toBeInTheDocument();
    });
    expect(screen.getByText('Station Beta')).toBeInTheDocument();
    expect(screen.getByText('Station Gamma')).toBeInTheDocument();
  });

  it('renders provider badges', async () => {
    renderWithProviders(<EInvoicesPage />);
    await waitFor(() => {
      expect(screen.getByText('MISA')).toBeInTheDocument();
    });
    expect(screen.getByText('Viettel')).toBeInTheDocument();
    expect(screen.getByText('VNPT')).toBeInTheDocument();
  });

  it('renders stat cards after data loads', async () => {
    renderWithProviders(<EInvoicesPage />);
    // Wait for data to load (stat cards only show after isLoading becomes false)
    await waitFor(() => {
      expect(screen.getByText('INV-2026-001')).toBeInTheDocument();
    });
    // Stat card labels (may appear as both stat label and status badge)
    expect(screen.getAllByText('Issued').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Pending').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Failed').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Total Amount')).toBeInTheDocument();
  });

  it('shows empty state when no invoices', async () => {
    mockApiGet.mockResolvedValue({ data: { items: [], totalCount: 0 } });
    renderWithProviders(<EInvoicesPage />);
    await waitFor(() => {
      expect(screen.getByText('No e-invoices found')).toBeInTheDocument();
    });
  });

  it('renders search input', async () => {
    renderWithProviders(<EInvoicesPage />);
    expect(screen.getByPlaceholderText('Search by invoice number...')).toBeInTheDocument();
  });

  it('calls API on page load', async () => {
    renderWithProviders(<EInvoicesPage />);
    await waitFor(() => {
      expect(mockApiGet).toHaveBeenCalledWith(
        '/e-invoices',
        expect.objectContaining({ params: expect.any(Object) })
      );
    });
  });
});
