import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/mobile-users',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock API module
const mockApiGet = vi.fn();
const mockApiPost = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    get: (url: string, config?: unknown) => mockApiGet(url, config),
    post: (url: string, data?: unknown) => mockApiPost(url, data),
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

import MobileUsersPage from '../page';

const mockUsers = [
  {
    id: 'user-1',
    fullName: 'Nguyen Van A',
    phoneNumber: '+84901234567',
    email: 'a@test.com',
    walletBalance: 500000,
    membershipTier: 2, // Gold
    isActive: true,
    lastLoginAt: '2026-03-12T10:00:00Z',
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'user-2',
    fullName: 'Tran Thi B',
    phoneNumber: '+84907654321',
    email: 'b@test.com',
    walletBalance: 0,
    membershipTier: 0, // Standard
    isActive: false,
    lastLoginAt: null,
    createdAt: '2026-02-15T00:00:00Z',
  },
  {
    id: 'user-3',
    fullName: 'Le Van C',
    phoneNumber: null,
    email: null,
    walletBalance: 1200000,
    membershipTier: 3, // Platinum
    isActive: true,
    lastLoginAt: '2026-03-10T08:30:00Z',
    createdAt: '2025-12-01T00:00:00Z',
  },
];

describe('MobileUsersPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockApiGet.mockResolvedValue({
      data: {
        data: mockUsers,
        pagination: { hasMore: false, pageSize: 20 },
      },
    });
    mockApiPost.mockResolvedValue({ data: {} });
  });

  it('renders page title and description', async () => {
    renderWithProviders(<MobileUsersPage />);
    expect(screen.getByText('Mobile Users')).toBeInTheDocument();
    // The mobileUsers.description key resolves to "Description" in the EN locale
    expect(screen.getByText('Description')).toBeInTheDocument();
  });

  it('renders search input', async () => {
    renderWithProviders(<MobileUsersPage />);
    expect(screen.getByPlaceholderText('Search by name, phone, or email...')).toBeInTheDocument();
  });

  it('renders table column headers', async () => {
    renderWithProviders(<MobileUsersPage />);
    await waitFor(() => {
      expect(screen.getByText('Name')).toBeInTheDocument();
    });
    expect(screen.getByText('Phone')).toBeInTheDocument();
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByText('Wallet Balance')).toBeInTheDocument();
    expect(screen.getByText('Membership')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();
    expect(screen.getByText('Last Login')).toBeInTheDocument();
    expect(screen.getByText('Actions')).toBeInTheDocument();
  });

  it('renders user data in table', async () => {
    renderWithProviders(<MobileUsersPage />);
    await waitFor(() => {
      expect(screen.getByText('Nguyen Van A')).toBeInTheDocument();
    });
    expect(screen.getByText('Tran Thi B')).toBeInTheDocument();
    expect(screen.getByText('Le Van C')).toBeInTheDocument();
  });

  it('renders membership tier badges', async () => {
    renderWithProviders(<MobileUsersPage />);
    await waitFor(() => {
      expect(screen.getByText('Gold')).toBeInTheDocument();
    });
    expect(screen.getByText('Platinum')).toBeInTheDocument();
    // Standard appears for user-2
    const standardBadges = screen.getAllByText('Standard');
    expect(standardBadges.length).toBeGreaterThanOrEqual(1);
  });

  it('renders active/suspended status badges', async () => {
    renderWithProviders(<MobileUsersPage />);
    await waitFor(() => {
      expect(screen.getByText('Nguyen Van A')).toBeInTheDocument();
    });
    // Active badges for active users
    const activeBadges = screen.getAllByText('Active');
    expect(activeBadges.length).toBeGreaterThanOrEqual(2);
    // Suspended badge for user-2
    expect(screen.getByText('Suspended')).toBeInTheDocument();
  });

  it('renders suspend button for active users', async () => {
    renderWithProviders(<MobileUsersPage />);
    await waitFor(() => {
      expect(screen.getByText('Nguyen Van A')).toBeInTheDocument();
    });
    const suspendButtons = screen.getAllByText('Suspend');
    // user-1 and user-3 are active → 2 suspend buttons
    expect(suspendButtons.length).toBe(2);
  });

  it('renders unsuspend button for suspended users', async () => {
    renderWithProviders(<MobileUsersPage />);
    await waitFor(() => {
      expect(screen.getByText('Tran Thi B')).toBeInTheDocument();
    });
    expect(screen.getByText('Unsuspend')).toBeInTheDocument();
  });

  it('shows empty state when no users found', async () => {
    mockApiGet.mockResolvedValue({
      data: {
        data: [],
        pagination: { hasMore: false, pageSize: 20 },
      },
    });
    renderWithProviders(<MobileUsersPage />);
    await waitFor(() => {
      expect(screen.getByText('No mobile users found')).toBeInTheDocument();
    });
    expect(screen.getByText('No registered mobile users yet.')).toBeInTheDocument();
  });

  it('renders loading state initially', () => {
    mockApiGet.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<MobileUsersPage />);
    expect(screen.getByText('Mobile Users')).toBeInTheDocument();
    // User data should not render while loading
    expect(screen.queryByText('Nguyen Van A')).not.toBeInTheDocument();
  });

  it('renders wallet balance formatted as currency', async () => {
    renderWithProviders(<MobileUsersPage />);
    await waitFor(() => {
      expect(screen.getByText('Nguyen Van A')).toBeInTheDocument();
    });
    // 500,000 formatted as vi-VN → 500.000đ
    expect(screen.getByText('500.000đ')).toBeInTheDocument();
  });
});
