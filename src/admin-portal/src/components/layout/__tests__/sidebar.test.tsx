import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next modules
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn() }),
  usePathname: () => '/',
}));

vi.mock('next/image', () => ({
  default: (props: Record<string, unknown>) => <img {...props} />,
}));

vi.mock('next/link', () => ({
  default: ({ children, href, ...props }: { children: React.ReactNode; href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

// Control store state via mutable objects
const storeState = {
  isCollapsed: false,
  permissions: [] as string[],
  user: { id: '1', username: 'admin', email: 'admin@test.com', role: 'admin' } as { id: string; username: string; email: string; role: string } | null,
  isAuthenticated: true,
  unreadCount: 0,
};

vi.mock('@/lib/store', () => ({
  useSidebarStore: () => ({
    isCollapsed: storeState.isCollapsed,
    toggle: vi.fn(),
    setCollapsed: vi.fn(),
  }),
  useAuthStore: () => ({
    user: storeState.user,
    token: 'test-token',
    isAuthenticated: storeState.isAuthenticated,
    permissions: storeState.permissions,
    login: vi.fn(),
    logout: vi.fn(),
    setPermissions: vi.fn(),
    hasPermission: (p: string) => storeState.permissions.includes(p),
  }),
  useAlertsStore: () => ({
    unreadCount: storeState.unreadCount,
    setUnreadCount: vi.fn(),
    incrementUnreadCount: vi.fn(),
    decrementUnreadCount: vi.fn(),
  }),
}));

import { Sidebar } from '../sidebar';

describe('Sidebar', () => {
  beforeEach(() => {
    storeState.isCollapsed = false;
    storeState.permissions = [];
    storeState.user = { id: '1', username: 'admin', email: 'admin@test.com', role: 'admin' };
    storeState.isAuthenticated = true;
    storeState.unreadCount = 0;
  });

  it('renders brand name', () => {
    renderWithProviders(<Sidebar />);
    expect(screen.getByText('K-Charge')).toBeInTheDocument();
  });

  it('renders section headers', () => {
    renderWithProviders(<Sidebar />);
    expect(screen.getByText('Operations')).toBeInTheDocument();
    expect(screen.getByText('Incidents')).toBeInTheDocument();
    expect(screen.getByText('Business')).toBeInTheDocument();
    // "Users" appears as both section header and nav item — use getAllByText
    expect(screen.getAllByText('Users').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('System')).toBeInTheDocument();
  });

  it('renders Dashboard link (always visible, no permission required)', () => {
    renderWithProviders(<Sidebar />);
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('shows all nav items when permissions are empty (not yet loaded)', () => {
    storeState.permissions = []; // empty = not loaded yet
    renderWithProviders(<Sidebar />);
    // Should show everything as fallback
    expect(screen.getByText('Stations')).toBeInTheDocument();
    expect(screen.getByText('Sessions')).toBeInTheDocument();
    expect(screen.getByText('Tariffs')).toBeInTheDocument();
    expect(screen.getByText('Fleets')).toBeInTheDocument();
  });

  it('shows only permitted nav items when permissions are loaded', () => {
    storeState.permissions = ['KLC.Stations', 'KLC.Sessions'];
    renderWithProviders(<Sidebar />);
    // Permitted items visible
    expect(screen.getByText('Stations')).toBeInTheDocument();
    expect(screen.getByText('Sessions')).toBeInTheDocument();
    // Non-permitted items hidden
    expect(screen.queryByText('Tariffs')).not.toBeInTheDocument();
    expect(screen.queryByText('Payments')).not.toBeInTheDocument();
    expect(screen.queryByText('Fleets')).not.toBeInTheDocument();
    expect(screen.queryByText('Operators')).not.toBeInTheDocument();
  });

  it('hides entire section when no items in that section have permission', () => {
    // Only grant Operations permissions — Business section should be hidden
    storeState.permissions = ['KLC.Stations', 'KLC.Monitoring'];
    renderWithProviders(<Sidebar />);
    expect(screen.getByText('Operations')).toBeInTheDocument();
    expect(screen.queryByText('Business')).not.toBeInTheDocument();
    expect(screen.queryByText('Incidents')).not.toBeInTheDocument();
  });

  it('Dashboard is always visible regardless of permissions', () => {
    storeState.permissions = ['KLC.Tariffs']; // no stations, no monitoring
    renderWithProviders(<Sidebar />);
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('shows Settings and Logout (no permission required)', () => {
    storeState.permissions = ['KLC.Stations']; // minimal permissions
    renderWithProviders(<Sidebar />);
    expect(screen.getByText('Settings')).toBeInTheDocument();
    expect(screen.getByText('Logout')).toBeInTheDocument();
  });

  it('shows Alerts link when user has KLC.Alerts permission', () => {
    storeState.permissions = ['KLC.Alerts'];
    renderWithProviders(<Sidebar />);
    expect(screen.getByText('Alerts')).toBeInTheDocument();
  });

  it('hides Alerts link when user lacks KLC.Alerts permission', () => {
    storeState.permissions = ['KLC.Stations'];
    renderWithProviders(<Sidebar />);
    // Alerts section at bottom should be hidden
    // Note: "Faults & Alerts" is a different nav item (KLC.Faults)
    // The standalone "Alerts" link at the bottom should be hidden
    const alertsLinks = screen.queryAllByText('Alerts');
    // The section-bottom Alerts link should not be present
    // (there might be the "Alerts" text in nav.faults = "Faults & Alerts")
    const standaloneAlertsLink = alertsLinks.filter(
      (el) => el.closest('a')?.getAttribute('href') === '/alerts'
    );
    expect(standaloneAlertsLink.length).toBe(0);
  });

  it('shows user info section', () => {
    renderWithProviders(<Sidebar />);
    // Username "admin" displayed in user section — may also match role text
    expect(screen.getAllByText('admin').length).toBeGreaterThanOrEqual(1);
  });

  it('shows full permission set for admin with all permissions', () => {
    storeState.permissions = [
      'KLC.Stations', 'KLC.Monitoring', 'KLC.Sessions', 'KLC.PowerSharing',
      'KLC.Faults', 'KLC.Maintenance',
      'KLC.Tariffs', 'KLC.Payments', 'KLC.Vouchers', 'KLC.Promotions', 'KLC.Operators', 'KLC.Fleets',
      'KLC.UserManagement', 'KLC.MobileUsers',
      'KLC.StationGroups', 'KLC.AuditLogs', 'KLC.EInvoices', 'KLC.Feedback',
      'KLC.Alerts',
    ];
    renderWithProviders(<Sidebar />);
    // All sections visible
    expect(screen.getByText('Operations')).toBeInTheDocument();
    expect(screen.getByText('Incidents')).toBeInTheDocument();
    expect(screen.getByText('Business')).toBeInTheDocument();
    expect(screen.getByText('System')).toBeInTheDocument();
    // Key items
    expect(screen.getByText('Stations')).toBeInTheDocument();
    expect(screen.getByText('Tariffs')).toBeInTheDocument();
    expect(screen.getByText('Audit Logs')).toBeInTheDocument();
  });

  it('shows operator-like view with limited permissions', () => {
    storeState.permissions = ['KLC.Stations', 'KLC.Sessions', 'KLC.Faults', 'KLC.Monitoring'];
    renderWithProviders(<Sidebar />);
    // Visible
    expect(screen.getByText('Stations')).toBeInTheDocument();
    expect(screen.getByText('Sessions')).toBeInTheDocument();
    expect(screen.getByText('Monitoring')).toBeInTheDocument();
    // Hidden
    expect(screen.queryByText('Tariffs')).not.toBeInTheDocument();
    expect(screen.queryByText('Payments')).not.toBeInTheDocument();
    expect(screen.queryByText('Audit Logs')).not.toBeInTheDocument();
    expect(screen.queryByText('E-Invoices')).not.toBeInTheDocument();
  });
});
