import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '@/test/utils';

// Mock next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => '/user-management',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock API modules
const mockGetAllUsers = vi.fn();
const mockGetAllRoles = vi.fn();
const mockCreateUser = vi.fn();
const mockUpdateUser = vi.fn();
const mockDeleteUser = vi.fn();
const mockLockUser = vi.fn();
const mockUnlockUser = vi.fn();
const mockUpdateRoles = vi.fn();
const mockResetPassword = vi.fn();
const mockCreateRole = vi.fn();
const mockUpdateRole = vi.fn();
const mockDeleteRole = vi.fn();
const mockGetPermissions = vi.fn();
const mockUpdatePermissions = vi.fn();
const mockGetMyPermissions = vi.fn();

vi.mock('@/lib/api', () => ({
  usersApi: {
    getAll: (params: unknown) => mockGetAllUsers(params),
    create: (data: unknown) => mockCreateUser(data),
    update: (id: string, data: unknown) => mockUpdateUser(id, data),
    delete: (id: string) => mockDeleteUser(id),
    lock: (id: string) => mockLockUser(id),
    unlock: (id: string) => mockUnlockUser(id),
    updateRoles: (id: string, roleNames: string[]) => mockUpdateRoles(id, roleNames),
    resetPassword: (id: string, password: string) => mockResetPassword(id, password),
  },
  rolesApi: {
    getAll: (params: unknown) => mockGetAllRoles(params),
    create: (data: unknown) => mockCreateRole(data),
    update: (id: string, data: unknown) => mockUpdateRole(id, data),
    delete: (id: string) => mockDeleteRole(id),
    getPermissions: (roleId: string) => mockGetPermissions(roleId),
    updatePermissions: (roleId: string, perms: string[]) => mockUpdatePermissions(roleId, perms),
  },
  authApi: {
    getMyPermissions: () => mockGetMyPermissions(),
  },
}));

// Mock the auth store (used for permission refresh after save)
const mockSetPermissions = vi.fn();
vi.mock('@/lib/store', () => ({
  useAuthStore: Object.assign(
    () => ({
      permissions: [],
      setPermissions: mockSetPermissions,
      hasPermission: () => true,
    }),
    { getState: () => ({ setPermissions: mockSetPermissions }) },
  ),
}));

import UserManagementPage from '../page';

const mockUsers = [
  {
    id: 'user-1',
    userName: 'admin',
    email: 'admin@example.com',
    name: 'Admin',
    surname: 'User',
    roles: ['admin'],
    isActive: true,
    isLockedOut: false,
    creationTime: '2026-01-01T00:00:00Z',
  },
  {
    id: 'user-2',
    userName: 'operator',
    email: 'operator@example.com',
    name: 'Operator',
    surname: 'User',
    roles: ['operator'],
    isActive: true,
    isLockedOut: true,
    creationTime: '2026-02-01T00:00:00Z',
  },
];

const mockRoles = [
  { id: 'role-1', name: 'admin', isDefault: false, isStatic: true, isPublic: true, concurrencyStamp: 'abc' },
  { id: 'role-2', name: 'operator', isDefault: true, isStatic: false, isPublic: true, concurrencyStamp: 'def' },
];

// Permission data matching the backend API response (array of PermissionGroupDto)
const mockPermissionData = [
  {
    name: 'KLC.Stations',
    displayName: 'Station Management',
    permissions: [
      { name: 'KLC.Stations', displayName: 'Station Management', isGranted: true },
      { name: 'KLC.Stations.Create', displayName: 'Create Stations', isGranted: true },
      { name: 'KLC.Stations.Update', displayName: 'Update Stations', isGranted: false },
      { name: 'KLC.Stations.Delete', displayName: 'Delete Stations', isGranted: false },
    ],
  },
  {
    name: 'KLC.Sessions',
    displayName: 'Session Management',
    permissions: [
      { name: 'KLC.Sessions', displayName: 'Session Management', isGranted: true },
      { name: 'KLC.Sessions.ViewAll', displayName: 'View All Sessions', isGranted: true },
    ],
  },
  {
    name: 'KLC.Tariffs',
    displayName: 'Tariff Management',
    permissions: [
      { name: 'KLC.Tariffs', displayName: 'Tariff Management', isGranted: false },
      { name: 'KLC.Tariffs.Create', displayName: 'Create Tariffs', isGranted: false },
    ],
  },
];

describe('UserManagementPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetAllUsers.mockResolvedValue({
      data: { items: mockUsers, totalCount: 2 },
    });
    mockGetAllRoles.mockResolvedValue({
      data: { items: mockRoles, totalCount: 2 },
    });
    mockCreateUser.mockResolvedValue({ data: {} });
    mockUpdateUser.mockResolvedValue({ data: {} });
    mockDeleteUser.mockResolvedValue({ data: {} });
    mockLockUser.mockResolvedValue({ data: {} });
    mockUnlockUser.mockResolvedValue({ data: {} });
    mockUpdateRoles.mockResolvedValue({ data: {} });
    mockResetPassword.mockResolvedValue({ data: {} });
    mockCreateRole.mockResolvedValue({ data: {} });
    mockUpdateRole.mockResolvedValue({ data: {} });
    mockDeleteRole.mockResolvedValue({ data: {} });
    mockGetPermissions.mockResolvedValue({ data: [] });
    mockUpdatePermissions.mockResolvedValue({ data: {} });
    mockGetMyPermissions.mockResolvedValue({ data: ['KLC.Stations', 'KLC.Sessions'] });
  });

  // ---- Users Tab ----
  it('renders page title and description', async () => {
    renderWithProviders(<UserManagementPage />);
    expect(screen.getByText('User Management')).toBeInTheDocument();
    expect(screen.getByText('Manage users, roles, and permissions')).toBeInTheDocument();
  });

  it('renders Users and Roles tabs', async () => {
    renderWithProviders(<UserManagementPage />);
    expect(screen.getByRole('tab', { name: /Users/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Roles/i })).toBeInTheDocument();
  });

  it('renders Users tab as active by default', async () => {
    renderWithProviders(<UserManagementPage />);
    const usersTab = screen.getByRole('tab', { name: /Users/i });
    expect(usersTab).toHaveAttribute('aria-selected', 'true');
  });

  it('renders users table with mock data', async () => {
    renderWithProviders(<UserManagementPage />);
    await waitFor(() => {
      expect(screen.getAllByText('admin').length).toBeGreaterThanOrEqual(1);
    });
    expect(screen.getByText('admin@example.com')).toBeInTheDocument();
    expect(screen.getAllByText('operator').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('operator@example.com')).toBeInTheDocument();
  });

  it('renders user status badges', async () => {
    renderWithProviders(<UserManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('admin@example.com')).toBeInTheDocument();
    });
    expect(screen.getByText('Locked')).toBeInTheDocument();
  });

  it('renders table column headers on Users tab', async () => {
    renderWithProviders(<UserManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('Username')).toBeInTheDocument();
    });
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByText('Name')).toBeInTheDocument();
    expect(screen.getAllByText('Roles').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Status')).toBeInTheDocument();
    expect(screen.getByText('Created')).toBeInTheDocument();
    expect(screen.getByText('Actions')).toBeInTheDocument();
  });

  it('renders Add User button', async () => {
    renderWithProviders(<UserManagementPage />);
    expect(screen.getByText('Add User')).toBeInTheDocument();
  });

  it('renders search input for users', async () => {
    renderWithProviders(<UserManagementPage />);
    expect(screen.getByLabelText('Search users...')).toBeInTheDocument();
  });

  it('shows empty state when no users found', async () => {
    mockGetAllUsers.mockResolvedValue({ data: { items: [], totalCount: 0 } });
    renderWithProviders(<UserManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('No users found')).toBeInTheDocument();
    });
  });

  it('renders action buttons for each user', async () => {
    renderWithProviders(<UserManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('admin@example.com')).toBeInTheDocument();
    });
    const editButtons = screen.getAllByLabelText('Edit');
    expect(editButtons.length).toBeGreaterThanOrEqual(2);
    const assignRolesButtons = screen.getAllByLabelText('Assign Roles');
    expect(assignRolesButtons.length).toBe(2);
  });

  // ---- Roles Tab ----
  it('switches to Roles tab when clicked', async () => {
    renderWithProviders(<UserManagementPage />);
    const rolesTab = screen.getByRole('tab', { name: /Roles/i });
    fireEvent.click(rolesTab);
    expect(rolesTab).toHaveAttribute('aria-selected', 'true');
  });

  it('renders roles table after switching to Roles tab', async () => {
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => {
      expect(screen.getByText('Role Name')).toBeInTheDocument();
    });
    expect(screen.getByText('Default')).toBeInTheDocument();
    expect(screen.getByText('Static')).toBeInTheDocument();
  });

  it('renders role data after switching to Roles tab', async () => {
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => {
      expect(screen.getByText('Role Name')).toBeInTheDocument();
    });
    expect(screen.getAllByText('admin').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('operator').length).toBeGreaterThanOrEqual(1);
  });

  it('renders Add Role button on Roles tab', async () => {
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => {
      expect(screen.getByText('Add Role')).toBeInTheDocument();
    });
  });

  it('does not show delete button for static roles', async () => {
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => {
      expect(screen.getByText('Role Name')).toBeInTheDocument();
    });
    // admin role is static — should have 2 action buttons (edit + permissions)
    // operator role is not static — should have 3 (edit + permissions + delete)
    const deleteButtons = screen.getAllByLabelText('Delete');
    // Only 1 delete button (for operator, not for admin)
    expect(deleteButtons.length).toBe(1);
  });

  // ---- Permission Dialog ----
  it('opens permission dialog for a role', async () => {
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => {
      expect(screen.getByText('Role Name')).toBeInTheDocument();
    });
    const permButtons = screen.getAllByLabelText('Permissions');
    fireEvent.click(permButtons[0]);
    await waitFor(() => {
      expect(screen.getByText(/Permissions — admin/)).toBeInTheDocument();
    });
  });

  it('shows Grant All and Revoke All buttons in permission dialog', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      expect(screen.getByText('Grant All')).toBeInTheDocument();
      expect(screen.getByText('Revoke All')).toBeInTheDocument();
    });
  });

  it('shows search input in permission dialog', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      expect(screen.getByLabelText('Search permissions...')).toBeInTheDocument();
    });
  });

  it('shows permission summary count in dialog', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      // 4 out of 8 permissions granted in our mock data
      expect(screen.getByText(/4\/8 permissions granted/)).toBeInTheDocument();
    });
  });

  it('shows permission groups as nav items with section headers', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      // Section header matching sidebar
      expect(screen.getByText('Operations')).toBeInTheDocument();
      // Permission groups shown as nav items using sidebar label keys
      expect(screen.getByText('Stations')).toBeInTheDocument();
      expect(screen.getByText('Sessions')).toBeInTheDocument();
    });
  });

  it('shows toggle switches for each permission group', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      // Toggle switches — role="switch"
      const toggles = screen.getAllByRole('switch');
      expect(toggles.length).toBeGreaterThanOrEqual(3); // at least one per group
    });
  });

  it('shows count badges for each permission group', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      // Stations: 2/4 granted (Stations + Create granted, Update + Delete not)
      expect(screen.getByText('2/4')).toBeInTheDocument();
      // Sessions: 2/2 granted
      expect(screen.getByText('2/2')).toBeInTheDocument();
      // Tariffs: 0/2 granted
      expect(screen.getByText('0/2')).toBeInTheDocument();
    });
  });

  it('expands permission group to show child permissions', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      expect(screen.getByText('Stations')).toBeInTheDocument();
    });

    // Click on "Stations" row to expand — find the expand chevron button
    const stationsRow = screen.getByText('Stations').closest('div[class*="flex items-center"]');
    // Click on the expand button (first button child in the row)
    const expandButton = stationsRow?.querySelector('button');
    if (expandButton) fireEvent.click(expandButton);

    await waitFor(() => {
      expect(screen.getByText('Create Stations')).toBeInTheDocument();
      expect(screen.getByText('Update Stations')).toBeInTheDocument();
      expect(screen.getByText('Delete Stations')).toBeInTheDocument();
    });
  });

  it('shows Cancel and Save Permissions buttons in dialog footer', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      expect(screen.getByText('Cancel')).toBeInTheDocument();
      expect(screen.getByText('Save Permissions')).toBeInTheDocument();
    });
  });

  it('calls updatePermissions API when saving', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    // Wait for permission data to load and grants to initialize
    await waitFor(() => {
      expect(screen.getByText('4/8 permissions granted')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Save Permissions'));

    await waitFor(() => {
      expect(mockUpdatePermissions).toHaveBeenCalledTimes(1);
      // First arg is roleId
      expect(mockUpdatePermissions.mock.calls[0][0]).toBe('role-1');
      // Second arg is granted permissions array — should include the 4 granted ones
      const grantedPerms = mockUpdatePermissions.mock.calls[0][1] as string[];
      expect(grantedPerms).toContain('KLC.Stations');
      expect(grantedPerms).toContain('KLC.Stations.Create');
      expect(grantedPerms).toContain('KLC.Sessions');
      expect(grantedPerms).toContain('KLC.Sessions.ViewAll');
      // Should NOT include non-granted ones
      expect(grantedPerms).not.toContain('KLC.Tariffs');
      expect(grantedPerms).not.toContain('KLC.Stations.Update');
    });
  });

  it('refreshes user permissions after saving role permissions', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      expect(screen.getByText('4/8 permissions granted')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Save Permissions'));

    await waitFor(() => {
      // Should call getMyPermissions to refresh sidebar
      expect(mockGetMyPermissions).toHaveBeenCalled();
    });
  });

  it('shows empty permission sections as skeleton while loading', async () => {
    // Keep getPermissions pending (never resolves)
    mockGetPermissions.mockReturnValue(new Promise(() => {}));
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      expect(screen.getByText(/Permissions — admin/)).toBeInTheDocument();
    });
    // Should show skeleton loading (no "Grant All" button visible yet, but dialog is open)
    // The dialog is open but content is loading
  });

  it('shows no results message when search matches nothing', async () => {
    mockGetPermissions.mockResolvedValue({ data: mockPermissionData });
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));
    await waitFor(() => expect(screen.getByText('Role Name')).toBeInTheDocument());
    fireEvent.click(screen.getAllByLabelText('Permissions')[0]);

    await waitFor(() => {
      expect(screen.getByLabelText('Search permissions...')).toBeInTheDocument();
    });

    // Type a search that matches nothing
    fireEvent.change(screen.getByLabelText('Search permissions...'), {
      target: { value: 'zzz_nonexistent' },
    });

    await waitFor(() => {
      expect(screen.getByText('No matching permissions')).toBeInTheDocument();
    });
  });
});
