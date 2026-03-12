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
    mockGetPermissions.mockResolvedValue({ data: { groups: [] } });
    mockUpdatePermissions.mockResolvedValue({ data: {} });
  });

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
      // "admin" appears as both username text and role badge text
      expect(screen.getAllByText('admin').length).toBeGreaterThanOrEqual(1);
    });
    expect(screen.getByText('admin@example.com')).toBeInTheDocument();
    // "operator" also appears as username and role badge
    expect(screen.getAllByText('operator').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('operator@example.com')).toBeInTheDocument();
  });

  it('renders user status badges', async () => {
    renderWithProviders(<UserManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('admin@example.com')).toBeInTheDocument();
    });
    // user-2 is locked out
    expect(screen.getByText('Locked')).toBeInTheDocument();
  });

  it('renders table column headers on Users tab', async () => {
    renderWithProviders(<UserManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('Username')).toBeInTheDocument();
    });
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByText('Name')).toBeInTheDocument();
    // "Roles" appears both as a tab label and as a column header
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

  it('switches to Roles tab when clicked', async () => {
    renderWithProviders(<UserManagementPage />);
    const rolesTab = screen.getByRole('tab', { name: /Roles/i });
    fireEvent.click(rolesTab);
    expect(rolesTab).toHaveAttribute('aria-selected', 'true');
  });

  it('renders roles table after switching to Roles tab', async () => {
    renderWithProviders(<UserManagementPage />);
    const rolesTab = screen.getByRole('tab', { name: /Roles/i });
    fireEvent.click(rolesTab);

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
    // Role names are present (may also appear as user role badges in the DOM)
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

  it('renders action buttons for each user', async () => {
    renderWithProviders(<UserManagementPage />);
    await waitFor(() => {
      expect(screen.getByText('admin@example.com')).toBeInTheDocument();
    });
    // Each user should have Edit, Assign Roles, Lock/Unlock, Reset Password, Delete buttons
    const editButtons = screen.getAllByLabelText('Edit');
    expect(editButtons.length).toBeGreaterThanOrEqual(2);
    const assignRolesButtons = screen.getAllByLabelText('Assign Roles');
    expect(assignRolesButtons.length).toBe(2);
  });

  it('opens permission dialog for a role on Roles tab', async () => {
    renderWithProviders(<UserManagementPage />);
    fireEvent.click(screen.getByRole('tab', { name: /Roles/i }));

    await waitFor(() => {
      expect(screen.getByText('Role Name')).toBeInTheDocument();
    });

    // Click the Permissions button (shield icon) for the first role
    const permButtons = screen.getAllByLabelText('Permissions');
    fireEvent.click(permButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Permissions — admin/)).toBeInTheDocument();
    });
  });
});
