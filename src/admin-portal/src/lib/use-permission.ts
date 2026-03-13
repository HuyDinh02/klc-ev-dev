import { useAuthStore } from "@/lib/store";

/**
 * Check if the current user has a specific permission.
 * Returns true while permissions haven't loaded yet (empty array = not loaded) to avoid flash.
 */
export function useHasPermission(permission: string): boolean {
  const { permissions } = useAuthStore();
  if (permissions.length === 0) return true; // not loaded yet
  return permissions.includes(permission);
}

/**
 * Gate a page by permission. Returns false when the user definitively lacks the permission.
 * While permissions are loading (empty array), returns true to prevent content flash.
 */
export function useRequirePermission(permission: string): boolean {
  return useHasPermission(permission);
}
