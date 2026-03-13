import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/',
}));

vi.mock('next/link', () => ({
  default: ({ children, href, ...props }: { children: React.ReactNode; href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

// Control store state via mutable object
const storeState = {
  unreadCount: 0,
  theme: 'system' as 'light' | 'dark' | 'system',
};

const mockSetTheme = vi.fn();

vi.mock('@/lib/store', () => ({
  useAlertsStore: () => ({
    unreadCount: storeState.unreadCount,
    setUnreadCount: vi.fn(),
    incrementUnreadCount: vi.fn(),
    decrementUnreadCount: vi.fn(),
  }),
  useThemeStore: () => ({
    theme: storeState.theme,
    setTheme: mockSetTheme,
  }),
}));

import { Header } from '../header';

describe('Header', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    storeState.unreadCount = 0;
    storeState.theme = 'system';
  });

  it('renders title', () => {
    renderWithProviders(<Header title="Dashboard" />);
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('renders title and description', () => {
    renderWithProviders(<Header title="Stations" description="Manage your stations" />);
    expect(screen.getByText('Stations')).toBeInTheDocument();
    expect(screen.getByText('Manage your stations')).toBeInTheDocument();
  });

  it('renders search input', () => {
    renderWithProviders(<Header title="Test" />);
    expect(screen.getByPlaceholderText('Search...')).toBeInTheDocument();
  });

  it('renders theme toggle button', () => {
    renderWithProviders(<Header title="Test" />);
    expect(screen.getByLabelText('Toggle theme')).toBeInTheDocument();
  });

  it('clicking theme toggle opens dropdown with Light/Dark/System options', () => {
    renderWithProviders(<Header title="Test" />);
    const toggleBtn = screen.getByLabelText('Toggle theme');
    fireEvent.click(toggleBtn);
    expect(screen.getByText('Light')).toBeInTheDocument();
    expect(screen.getByText('Dark')).toBeInTheDocument();
    expect(screen.getByText('System')).toBeInTheDocument();
  });

  it('clicking Dark option calls setTheme with dark', () => {
    renderWithProviders(<Header title="Test" />);
    const toggleBtn = screen.getByLabelText('Toggle theme');
    fireEvent.click(toggleBtn);
    fireEvent.click(screen.getByText('Dark'));
    expect(mockSetTheme).toHaveBeenCalledWith('dark');
  });

  it('clicking Light option calls setTheme with light', () => {
    renderWithProviders(<Header title="Test" />);
    const toggleBtn = screen.getByLabelText('Toggle theme');
    fireEvent.click(toggleBtn);
    fireEvent.click(screen.getByText('Light'));
    expect(mockSetTheme).toHaveBeenCalledWith('light');
  });

  it('does not show alert badge when unreadCount is 0', () => {
    storeState.unreadCount = 0;
    renderWithProviders(<Header title="Test" />);
    expect(screen.queryByText('0')).not.toBeInTheDocument();
  });

  it('shows alert badge with unread count', () => {
    storeState.unreadCount = 5;
    renderWithProviders(<Header title="Test" />);
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('shows 9+ when unread count exceeds 9', () => {
    storeState.unreadCount = 15;
    renderWithProviders(<Header title="Test" />);
    expect(screen.getByText('9+')).toBeInTheDocument();
  });

  it('renders alerts link to /alerts', () => {
    renderWithProviders(<Header title="Test" />);
    const alertLink = screen.getByRole('link', { name: '' });
    // The bell icon link points to /alerts
    const links = document.querySelectorAll('a[href="/alerts"]');
    expect(links.length).toBe(1);
  });
});
