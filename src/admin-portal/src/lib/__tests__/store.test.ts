import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '../store';

describe('useAuthStore', () => {
  beforeEach(() => {
    // Reset store state between tests
    useAuthStore.setState({
      user: null,
      token: null,
      isAuthenticated: false,
      permissions: [],
    });
  });

  it('starts with empty permissions', () => {
    const { permissions } = useAuthStore.getState();
    expect(permissions).toEqual([]);
  });

  it('setPermissions stores the permission list', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations', 'KLC.Sessions']);
    const { permissions } = useAuthStore.getState();
    expect(permissions).toEqual(['KLC.Stations', 'KLC.Sessions']);
  });

  it('hasPermission returns true for granted permissions', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations', 'KLC.Tariffs']);
    expect(useAuthStore.getState().hasPermission('KLC.Stations')).toBe(true);
    expect(useAuthStore.getState().hasPermission('KLC.Tariffs')).toBe(true);
  });

  it('hasPermission returns false for missing permissions', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations']);
    expect(useAuthStore.getState().hasPermission('KLC.Payments')).toBe(false);
    expect(useAuthStore.getState().hasPermission('KLC.Fleets')).toBe(false);
  });

  it('hasPermission returns false when permissions are empty', () => {
    expect(useAuthStore.getState().hasPermission('KLC.Stations')).toBe(false);
  });

  it('logout clears permissions', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations', 'KLC.Sessions']);
    expect(useAuthStore.getState().permissions.length).toBe(2);

    useAuthStore.getState().logout();
    const state = useAuthStore.getState();
    expect(state.permissions).toEqual([]);
    expect(state.isAuthenticated).toBe(false);
    expect(state.user).toBeNull();
    expect(state.token).toBeNull();
  });

  it('login sets user and token but preserves empty permissions', () => {
    const user = { id: '1', username: 'admin', email: 'admin@test.com', role: 'admin' };
    useAuthStore.getState().login(user, 'test-token');

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.user).toEqual(user);
    expect(state.token).toBe('test-token');
    expect(state.permissions).toEqual([]);
  });

  it('setPermissions replaces existing permissions', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations']);
    useAuthStore.getState().setPermissions(['KLC.Payments', 'KLC.Fleets']);

    const { permissions } = useAuthStore.getState();
    expect(permissions).toEqual(['KLC.Payments', 'KLC.Fleets']);
    expect(permissions).not.toContain('KLC.Stations');
  });

  it('setPermissions accepts empty array', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations']);
    useAuthStore.getState().setPermissions([]);
    expect(useAuthStore.getState().permissions).toEqual([]);
  });
});
