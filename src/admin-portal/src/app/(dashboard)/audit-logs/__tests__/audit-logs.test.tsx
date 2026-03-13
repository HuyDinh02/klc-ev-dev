import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/audit-logs',
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const mockApiGet = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
  },
}));

import AuditLogsPage from '../page';

const mockLogs = [
  {
    id: 'log-1',
    userId: 'user-1',
    userName: 'admin',
    httpMethod: 'GET',
    url: '/api/v1/stations',
    httpStatusCode: 200,
    executionDuration: 45,
    clientIpAddress: '192.168.1.1',
    browserInfo: 'Chrome/120.0',
    entityChanges: [],
    executionTime: '2026-03-08T10:00:00Z',
  },
  {
    id: 'log-2',
    userId: 'user-2',
    userName: 'operator',
    httpMethod: 'POST',
    url: '/api/v1/stations',
    httpStatusCode: 201,
    executionDuration: 120,
    clientIpAddress: '10.0.0.5',
    browserInfo: 'Firefox/115.0',
    entityChanges: [
      {
        id: 'change-1',
        entityTypeFullName: 'KLC.Domain.Stations.ChargingStation',
        changeType: 'Created',
        entityId: 'station-new',
        propertyChanges: [
          { propertyName: 'Name', originalValue: undefined, newValue: 'New Station' },
        ],
      },
    ],
    executionTime: '2026-03-08T11:00:00Z',
  },
  {
    id: 'log-3',
    userId: '',
    userName: '',
    httpMethod: 'DELETE',
    url: '/api/v1/faults/fault-old',
    httpStatusCode: 500,
    executionDuration: 2500,
    clientIpAddress: '172.16.0.1',
    browserInfo: 'Safari/17.0',
    entityChanges: [],
    executionTime: '2026-03-08T12:00:00Z',
  },
];

describe('AuditLogsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiGet.mockImplementation((url: string) => {
      if (url === '/audit-logs') {
        return Promise.resolve({
          data: { items: mockLogs, totalCount: 3 },
        });
      }
      return Promise.resolve({ data: {} });
    });
  });

  it('renders audit logs page title', async () => {
    renderWithProviders(<AuditLogsPage />);
    expect(screen.getByText('Audit Logs')).toBeInTheDocument();
  });

  it('renders log table with data', async () => {
    renderWithProviders(<AuditLogsPage />);
    await waitFor(() => {
      expect(screen.getByText('admin')).toBeInTheDocument();
    });
    expect(screen.getByText('operator')).toBeInTheDocument();
  });

  it('renders HTTP methods as badges', async () => {
    renderWithProviders(<AuditLogsPage />);
    await waitFor(() => {
      expect(screen.getByText('GET')).toBeInTheDocument();
    });
    expect(screen.getByText('POST')).toBeInTheDocument();
    expect(screen.getByText('DELETE')).toBeInTheDocument();
  });

  it('renders URLs in the table', async () => {
    renderWithProviders(<AuditLogsPage />);
    await waitFor(() => {
      // /api/v1/stations appears for both GET and POST rows
      expect(screen.getAllByText('/api/v1/stations').length).toBe(2);
    });
    expect(screen.getByText('/api/v1/faults/fault-old')).toBeInTheDocument();
  });

  it('renders HTTP status codes', async () => {
    renderWithProviders(<AuditLogsPage />);
    await waitFor(() => {
      expect(screen.getByText('200')).toBeInTheDocument();
    });
    expect(screen.getByText('201')).toBeInTheDocument();
    expect(screen.getByText('500')).toBeInTheDocument();
  });

  it('renders IP addresses', async () => {
    renderWithProviders(<AuditLogsPage />);
    await waitFor(() => {
      expect(screen.getByText('192.168.1.1')).toBeInTheDocument();
    });
    expect(screen.getByText('10.0.0.5')).toBeInTheDocument();
  });

  it('shows empty state when no logs', async () => {
    mockApiGet.mockResolvedValue({ data: { items: [], totalCount: 0 } });
    renderWithProviders(<AuditLogsPage />);
    await waitFor(() => {
      expect(screen.getByText('No audit logs found')).toBeInTheDocument();
    });
  });

  it('renders export button', async () => {
    renderWithProviders(<AuditLogsPage />);
    expect(screen.getByText('Export CSV')).toBeInTheDocument();
  });

  it('renders search and filter inputs', async () => {
    renderWithProviders(<AuditLogsPage />);
    expect(screen.getByPlaceholderText('Search by URL...')).toBeInTheDocument();
  });

  it('calls API on page load', async () => {
    renderWithProviders(<AuditLogsPage />);
    await waitFor(() => {
      expect(mockApiGet).toHaveBeenCalledWith(
        '/audit-logs',
        expect.objectContaining({ params: expect.any(Object) })
      );
    });
  });
});
