import { describe, it, expect, beforeEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { useAuthStore } from '../store';
import { useHasPermission, useRequirePermission } from '../use-permission';

describe('useHasPermission', () => {
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      token: null,
      isAuthenticated: false,
      permissions: [],
    });
  });

  it('returns true when permissions are empty (not loaded)', () => {
    const { result } = renderHook(() => useHasPermission('KLC.Stations'));
    expect(result.current).toBe(true);
  });

  it('returns true when user has the permission', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations', 'KLC.Sessions']);
    const { result } = renderHook(() => useHasPermission('KLC.Stations'));
    expect(result.current).toBe(true);
  });

  it('returns false when user lacks the permission', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations']);
    const { result } = renderHook(() => useHasPermission('KLC.Tariffs'));
    expect(result.current).toBe(false);
  });

  it('returns true for granular permission when granted', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations', 'KLC.Stations.Create']);
    const { result } = renderHook(() => useHasPermission('KLC.Stations.Create'));
    expect(result.current).toBe(true);
  });

  it('returns false for granular permission when not granted', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations']);
    const { result } = renderHook(() => useHasPermission('KLC.Stations.Create'));
    expect(result.current).toBe(false);
  });
});

describe('useRequirePermission', () => {
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      token: null,
      isAuthenticated: false,
      permissions: [],
    });
  });

  it('returns true when permissions not loaded (graceful fallback)', () => {
    const { result } = renderHook(() => useRequirePermission('KLC.Stations'));
    expect(result.current).toBe(true);
  });

  it('returns true when user has the required permission', () => {
    useAuthStore.getState().setPermissions(['KLC.Stations', 'KLC.Tariffs']);
    const { result } = renderHook(() => useRequirePermission('KLC.Stations'));
    expect(result.current).toBe(true);
  });

  it('returns false when user lacks the required permission', () => {
    useAuthStore.getState().setPermissions(['KLC.Tariffs']);
    const { result } = renderHook(() => useRequirePermission('KLC.Stations'));
    expect(result.current).toBe(false);
  });
});
