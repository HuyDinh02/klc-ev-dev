import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/operators',
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const mockGetList = vi.fn();
const mockCreate = vi.fn();
const mockDelete = vi.fn();
const mockRegenerateKey = vi.fn();

vi.mock('@/lib/api', () => ({
  operatorsApi: {
    getList: (params: unknown) => mockGetList(params),
    create: (data: unknown) => mockCreate(data),
    delete: (id: string) => mockDelete(id),
    regenerateApiKey: (id: string) => mockRegenerateKey(id),
    get: vi.fn(),
    update: vi.fn(),
    removeStation: vi.fn(),
  },
}));

import OperatorsPage from '../page';

const mockOperators = [
  {
    id: 'op-1',
    name: 'EV Partner Corp',
    contactEmail: 'api@evpartner.com',
    webhookUrl: 'https://evpartner.com/webhook',
    isActive: true,
    rateLimitPerMinute: 1000,
    stationCount: 5,
    creationTime: '2026-02-15T08:00:00Z',
  },
  {
    id: 'op-2',
    name: 'GreenCharge Ltd',
    contactEmail: 'tech@greencharge.vn',
    webhookUrl: null,
    isActive: false,
    rateLimitPerMinute: 500,
    stationCount: 0,
    creationTime: '2026-03-01T10:00:00Z',
  },
];

describe('OperatorsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetList.mockResolvedValue({ data: mockOperators });
    mockCreate.mockResolvedValue({ data: { operator: mockOperators[0], apiKey: 'abc123' } });
    mockDelete.mockResolvedValue({ data: {} });
    mockRegenerateKey.mockResolvedValue({ data: { apiKey: 'newkey456' } });
  });

  it('renders operators page title', async () => {
    renderWithProviders(<OperatorsPage />);
    await waitFor(() => {
      expect(screen.getAllByText(/Operators/i).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('renders operator list with data', async () => {
    renderWithProviders(<OperatorsPage />);
    await waitFor(() => {
      expect(screen.getByText('EV Partner Corp')).toBeInTheDocument();
    });
    expect(screen.getByText('GreenCharge Ltd')).toBeInTheDocument();
  });

  it('renders contact emails', async () => {
    renderWithProviders(<OperatorsPage />);
    await waitFor(() => {
      expect(screen.getByText('api@evpartner.com')).toBeInTheDocument();
    });
    expect(screen.getByText('tech@greencharge.vn')).toBeInTheDocument();
  });

  it('renders station counts', async () => {
    renderWithProviders(<OperatorsPage />);
    await waitFor(() => {
      expect(screen.getByText('EV Partner Corp')).toBeInTheDocument();
    });
    // totalStationsAssigned stat = 5 + 0 = 5, shown in stat card and individual card
    expect(screen.getAllByText('5').length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state when no operators', async () => {
    mockGetList.mockResolvedValue({ data: [] });
    renderWithProviders(<OperatorsPage />);
    await waitFor(() => {
      expect(screen.queryByText('EV Partner Corp')).not.toBeInTheDocument();
    });
  });

  it('calls API on page load', async () => {
    renderWithProviders(<OperatorsPage />);
    await waitFor(() => {
      expect(mockGetList).toHaveBeenCalled();
    });
  });
});
