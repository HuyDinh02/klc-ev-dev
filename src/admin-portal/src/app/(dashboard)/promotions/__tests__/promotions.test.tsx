import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/promotions',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock API module
const mockApiGet = vi.fn();
const mockApiPost = vi.fn();
const mockApiPut = vi.fn();
const mockApiDelete = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
    post: (url: string, data?: unknown, config?: unknown) => mockApiPost(url, data, config),
    put: (url: string, data?: unknown) => mockApiPut(url, data),
    delete: (url: string) => mockApiDelete(url),
  },
}));

// Mock store — permissions empty means "not loaded yet" → useRequirePermission returns true
vi.mock('@/lib/store', () => ({
  useAuthStore: () => ({
    permissions: [],
    user: null,
    token: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
    setPermissions: vi.fn(),
    hasPermission: () => true,
  }),
}));

import PromotionsPage from '../page';

const mockPromotions = [
  {
    id: 'promo-1',
    title: 'Summer Charging Discount',
    description: 'Get 20% off all charging sessions',
    imageUrl: null,
    startDate: '2026-06-01T00:00:00Z',
    endDate: '2026-08-31T00:00:00Z',
    type: 0, // Banner
    isActive: true,
    isCurrentlyActive: true,
    createdAt: '2026-03-01T00:00:00Z',
  },
  {
    id: 'promo-2',
    title: 'New User Welcome',
    description: 'Free first charge for new users',
    imageUrl: 'https://example.com/promo.jpg',
    startDate: '2026-03-01T00:00:00Z',
    endDate: '2026-12-31T00:00:00Z',
    type: 1, // Popup
    isActive: false,
    isCurrentlyActive: false,
    createdAt: '2026-02-15T00:00:00Z',
  },
  {
    id: 'promo-3',
    title: 'Weekend Special',
    description: null,
    imageUrl: null,
    startDate: '2026-04-01T00:00:00Z',
    endDate: '2026-04-30T00:00:00Z',
    type: 2, // In-App
    isActive: true,
    isCurrentlyActive: false,
    createdAt: '2026-03-10T00:00:00Z',
  },
];

describe('PromotionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiGet.mockResolvedValue({
      data: { data: mockPromotions },
    });
    mockApiPost.mockResolvedValue({ data: {} });
    mockApiPut.mockResolvedValue({ data: {} });
    mockApiDelete.mockResolvedValue({ data: {} });
  });

  it('renders page title and description', async () => {
    renderWithProviders(<PromotionsPage />);
    expect(screen.getByText('Promotions')).toBeInTheDocument();
    expect(screen.getByText('Manage promotional campaigns for drivers')).toBeInTheDocument();
  });

  it('renders Add Promotion button', async () => {
    renderWithProviders(<PromotionsPage />);
    expect(screen.getByText('Add Promotion')).toBeInTheDocument();
  });

  it('renders filter tabs', async () => {
    renderWithProviders(<PromotionsPage />);
    expect(screen.getByText('All')).toBeInTheDocument();
    expect(screen.getByText('Active')).toBeInTheDocument();
    expect(screen.getByText('Inactive')).toBeInTheDocument();
  });

  it('renders promotion cards with data', async () => {
    renderWithProviders(<PromotionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Summer Charging Discount')).toBeInTheDocument();
    });
    expect(screen.getByText('New User Welcome')).toBeInTheDocument();
    expect(screen.getByText('Weekend Special')).toBeInTheDocument();
  });

  it('renders promotion descriptions', async () => {
    renderWithProviders(<PromotionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Get 20% off all charging sessions')).toBeInTheDocument();
    });
    expect(screen.getByText('Free first charge for new users')).toBeInTheDocument();
  });

  it('renders promotion type badges', async () => {
    renderWithProviders(<PromotionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Banner')).toBeInTheDocument();
    });
    expect(screen.getByText('Popup')).toBeInTheDocument();
    expect(screen.getByText('In-App')).toBeInTheDocument();
  });

  it('renders active/inactive status badges', async () => {
    renderWithProviders(<PromotionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Summer Charging Discount')).toBeInTheDocument();
    });
    // Active appears in filter tab + badges on active promotions
    const activeBadges = screen.getAllByText('Active');
    expect(activeBadges.length).toBeGreaterThanOrEqual(2);
    // Inactive appears in filter tab + badge on inactive promotion
    const inactiveBadges = screen.getAllByText('Inactive');
    expect(inactiveBadges.length).toBeGreaterThanOrEqual(2);
  });

  it('shows empty state when no promotions found', async () => {
    mockApiGet.mockResolvedValue({ data: { data: [] } });
    renderWithProviders(<PromotionsPage />);
    await waitFor(() => {
      expect(screen.getByText('No promotions found')).toBeInTheDocument();
    });
    expect(screen.getByText('Create your first promotion to get started.')).toBeInTheDocument();
  });

  it('opens create dialog when Add Promotion is clicked', async () => {
    renderWithProviders(<PromotionsPage />);
    fireEvent.click(screen.getByText('Add Promotion'));
    await waitFor(() => {
      expect(screen.getByText('New Promotion')).toBeInTheDocument();
    });
    expect(screen.getByText('Create Promotion')).toBeInTheDocument();
    expect(screen.getByText('Cancel')).toBeInTheDocument();
  });

  it('renders edit buttons on promotion cards', async () => {
    renderWithProviders(<PromotionsPage />);
    await waitFor(() => {
      expect(screen.getByText('Summer Charging Discount')).toBeInTheDocument();
    });
    const editButtons = screen.getAllByText('Edit');
    expect(editButtons.length).toBe(3);
  });

  it('renders loading state initially', () => {
    mockApiGet.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<PromotionsPage />);
    expect(screen.getByText('Promotions')).toBeInTheDocument();
    // Cards should not render while loading
    expect(screen.queryByText('Summer Charging Discount')).not.toBeInTheDocument();
  });
});
