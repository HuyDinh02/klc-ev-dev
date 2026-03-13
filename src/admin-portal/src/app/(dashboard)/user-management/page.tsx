"use client";

import { useState, useRef, useEffect, useCallback, useMemo } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "@/components/ui/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { SkeletonTable } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { usersApi, rolesApi, authApi } from "@/lib/api";
import { useAuthStore } from "@/lib/store";
import { formatDateTime } from "@/lib/utils";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import {
  Plus, Edit, Trash2, Lock, Unlock, Key, Shield, Search, Users, ChevronLeft, ChevronRight,
  MapPin, Activity, Zap, Cable, AlertTriangle, Wrench, DollarSign, CreditCard, Ticket,
  Building2, Truck, FileText, Bell, ChevronDown, Plug, type LucideIcon,
  Check, Minus,
} from "lucide-react";

// Permission sections — mirrors sidebar navigation exactly
const PERMISSION_SECTIONS: Array<{
  sectionKey: string;
  groups: Array<{ permissionGroup: string; icon: LucideIcon; labelKey: string }>;
}> = [
  {
    sectionKey: "nav.operations",
    groups: [
      { permissionGroup: "KLC.Stations", icon: MapPin, labelKey: "nav.stations" },
      { permissionGroup: "KLC.Connectors", icon: Plug, labelKey: "nav.connectors" },
      { permissionGroup: "KLC.Monitoring", icon: Activity, labelKey: "nav.monitoring" },
      { permissionGroup: "KLC.Sessions", icon: Zap, labelKey: "nav.sessions" },
      { permissionGroup: "KLC.PowerSharing", icon: Cable, labelKey: "nav.powerSharing" },
    ],
  },
  {
    sectionKey: "nav.incidents",
    groups: [
      { permissionGroup: "KLC.Faults", icon: AlertTriangle, labelKey: "nav.faults" },
      { permissionGroup: "KLC.Maintenance", icon: Wrench, labelKey: "nav.maintenance" },
      { permissionGroup: "KLC.Alerts", icon: Bell, labelKey: "nav.alerts" },
    ],
  },
  {
    sectionKey: "nav.business",
    groups: [
      { permissionGroup: "KLC.Tariffs", icon: DollarSign, labelKey: "nav.tariffs" },
      { permissionGroup: "KLC.Payments", icon: CreditCard, labelKey: "nav.payments" },
      { permissionGroup: "KLC.Vouchers", icon: Ticket, labelKey: "nav.marketing" },
      { permissionGroup: "KLC.Promotions", icon: Ticket, labelKey: "nav.promotions" },
      { permissionGroup: "KLC.Operators", icon: Building2, labelKey: "nav.operators" },
      { permissionGroup: "KLC.Fleets", icon: Truck, labelKey: "nav.fleets" },
    ],
  },
  {
    sectionKey: "nav.users",
    groups: [
      { permissionGroup: "KLC.UserManagement", icon: Users, labelKey: "nav.userManagement" },
      { permissionGroup: "KLC.RoleManagement", icon: Shield, labelKey: "nav.roleManagement" },
      { permissionGroup: "KLC.MobileUsers", icon: Users, labelKey: "nav.mobileUsers" },
    ],
  },
  {
    sectionKey: "nav.system",
    groups: [
      { permissionGroup: "KLC.StationGroups", icon: MapPin, labelKey: "nav.stationGroups" },
      { permissionGroup: "KLC.AuditLogs", icon: FileText, labelKey: "nav.auditLogs" },
      { permissionGroup: "KLC.EInvoices", icon: FileText, labelKey: "nav.eInvoices" },
      { permissionGroup: "KLC.Notifications", icon: Bell, labelKey: "nav.notifications" },
      { permissionGroup: "KLC.Feedback", icon: FileText, labelKey: "nav.feedback" },
    ],
  },
];

type PermissionItem = { name: string; displayName: string; isGranted: boolean };
type PermissionGroup = { name: string; displayName: string; permissions: PermissionItem[] };

// Toggle switch for group-level grant/revoke
function PermissionToggle({ checked, indeterminate, onChange }: {
  checked: boolean; indeterminate: boolean; onChange: (checked: boolean) => void;
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={indeterminate ? "mixed" : checked}
      onClick={(e) => { e.stopPropagation(); onChange(!checked); }}
      className={`relative inline-flex h-5 w-9 flex-shrink-0 items-center rounded-full transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ${
        checked ? "bg-primary" : indeterminate ? "bg-amber-400" : "bg-muted-foreground/25"
      }`}
    >
      <span className={`pointer-events-none flex h-4 w-4 items-center justify-center rounded-full bg-white shadow-sm transition-transform ${
        checked ? "translate-x-[18px]" : indeterminate ? "translate-x-[9px]" : "translate-x-[2px]"
      }`}>
        {checked && <Check className="h-2.5 w-2.5 text-primary" />}
        {indeterminate && <Minus className="h-2.5 w-2.5 text-amber-500" />}
      </span>
    </button>
  );
}

type TabType = "users" | "roles";

// ---- Users Tab ----
function UsersTab() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const canCreateUser = useHasPermission("KLC.UserManagement.Create");
  const canUpdateUser = useHasPermission("KLC.UserManagement.Update");
  const canDeleteUser = useHasPermission("KLC.UserManagement.Delete");
  const [search, setSearch] = useState("");
  const [pageIndex, setPageIndex] = useState(0);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingUser, setEditingUser] = useState<Record<string, unknown> | null>(null);
  const [roleModalUser, setRoleModalUser] = useState<Record<string, unknown> | null>(null);
  const [resetPwUser, setResetPwUser] = useState<Record<string, unknown> | null>(null);
  const [newPassword, setNewPassword] = useState("");
  const [userForm, setUserForm] = useState({ userName: "", email: "", password: "", name: "", surname: "", phoneNumber: "", isActive: true, roleNames: [] as string[] });
  const pageSize = 20;

  const { data: usersData, isLoading } = useQuery({
    queryKey: ["users", search, pageIndex],
    queryFn: async () => {
      const { data } = await usersApi.getAll({ skipCount: pageIndex * pageSize, maxResultCount: pageSize, filter: search || undefined });
      return data;
    },
  });

  const { data: allRoles } = useQuery({
    queryKey: ["all-roles"],
    queryFn: async () => {
      const { data } = await rolesApi.getAll({ maxResultCount: 100 });
      return data.items || [];
    },
  });

  const createMutation = useMutation({
    mutationFn: async () => { await usersApi.create(userForm); },
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["users"] }); setShowCreateModal(false); resetForm(); },
  });

  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!editingUser) return;
      await usersApi.update(editingUser.id as string, {
        userName: userForm.userName,
        email: userForm.email,
        name: userForm.name || undefined,
        surname: userForm.surname || undefined,
        phoneNumber: userForm.phoneNumber || undefined,
        isActive: userForm.isActive,
      });
    },
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["users"] }); setEditingUser(null); resetForm(); },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => { await usersApi.delete(id); },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["users"] }),
  });

  const lockMutation = useMutation({
    mutationFn: async (id: string) => { await usersApi.lock(id); },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["users"] }),
  });

  const unlockMutation = useMutation({
    mutationFn: async (id: string) => { await usersApi.unlock(id); },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["users"] }),
  });

  const updateRolesMutation = useMutation({
    mutationFn: async ({ id, roleNames }: { id: string; roleNames: string[] }) => {
      await usersApi.updateRoles(id, roleNames);
    },
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["users"] }); setRoleModalUser(null); },
  });

  const resetPasswordMutation = useMutation({
    mutationFn: async ({ id, password }: { id: string; password: string }) => {
      await usersApi.resetPassword(id, password);
    },
    onSuccess: () => { setResetPwUser(null); setNewPassword(""); },
  });

  const resetForm = () => setUserForm({ userName: "", email: "", password: "", name: "", surname: "", phoneNumber: "", isActive: true, roleNames: [] });

  const users = usersData?.items || [];
  const totalCount = usersData?.totalCount || 0;

  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);

  return (
    <>
      {/* Search + Create */}
      <div className="flex items-center justify-between mb-4">
        <div className="relative w-72">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
          <input type="search" placeholder={t("userManagement.searchPlaceholder")} value={search}
            onChange={(e) => { setSearch(e.target.value); setPageIndex(0); }}
            className="h-10 w-full rounded-md border bg-background pl-9 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            aria-label={t("userManagement.searchPlaceholder")} />
        </div>
        {canCreateUser && (
          <Button onClick={() => { resetForm(); setShowCreateModal(true); }}><Plus className="mr-2 h-4 w-4" aria-hidden="true" /> {t("userManagement.addUser")}</Button>
        )}
      </div>

      {/* Users Table */}
      {isLoading ? (
        <SkeletonTable rows={5} cols={7} />
      ) : users.length === 0 ? (
        <Card>
          <CardContent className="p-0">
            <EmptyState
              icon={Users}
              title={t("userManagement.noUsersFound")}
              description={search ? t("userManagement.noUsersSearch") : t("userManagement.noUsersGetStarted")}
              action={!search ? { label: t("userManagement.addUser"), onClick: () => { resetForm(); setShowCreateModal(true); } } : undefined}
            />
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead><tr className="border-b bg-muted/50">
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("userManagement.username")}</th>
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("userManagement.email")}</th>
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("userManagement.name")}</th>
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("userManagement.roles")}</th>
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("common.status")}</th>
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("userManagement.created")}</th>
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("common.actions")}</th>
                </tr></thead>
                <tbody>
                  {users.map((user: Record<string, unknown>) => (
                    <tr key={user.id as string} className="border-b hover:bg-muted/50">
                      <td className="px-4 py-3 font-medium">{user.userName as string}</td>
                      <td className="px-4 py-3 text-sm">{user.email as string}</td>
                      <td className="px-4 py-3">{`${user.name || ""} ${user.surname || ""}`.trim() || "—"}</td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-1">
                          {Array.isArray(user.roles) && (user.roles as string[]).map((r) => (
                            <Badge key={r} variant="outline">{r}</Badge>
                          ))}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex gap-1">
                          <Badge variant={user.isActive ? "success" : "secondary"}>{user.isActive ? t("common.active") : t("common.inactive")}</Badge>
                          {!!user.isLockedOut && <Badge variant="destructive">{t("userManagement.locked")}</Badge>}
                        </div>
                      </td>
                      <td className="px-4 py-3 text-sm text-muted-foreground">{user.creationTime ? formatDateTime(user.creationTime as string) : ""}</td>
                      <td className="px-4 py-3">
                        <div className="flex gap-1">
                          {canUpdateUser && (
                            <Button variant="ghost" size="sm" title={t("common.edit")} aria-label={t("common.edit")} onClick={() => {
                              setEditingUser(user);
                              setUserForm({ userName: user.userName as string, email: user.email as string, password: "", name: (user.name as string) || "", surname: (user.surname as string) || "", phoneNumber: (user.phoneNumber as string) || "", isActive: user.isActive as boolean, roleNames: (user.roles as string[]) || [] });
                            }}><Edit className="h-4 w-4" /></Button>
                          )}
                          {canUpdateUser && (
                            <Button variant="ghost" size="sm" title={t("userManagement.assignRoles")} aria-label={t("userManagement.assignRoles")} onClick={() => { setRoleModalUser(user); setSelectedRoles((user.roles as string[]) || []); }}>
                              <Shield className="h-4 w-4" />
                            </Button>
                          )}
                          {canUpdateUser && (!!user.isLockedOut ? (
                            <Button variant="ghost" size="sm" title={t("userManagement.unlock")} aria-label={t("userManagement.unlock")} onClick={() => unlockMutation.mutate(user.id as string)}><Unlock className="h-4 w-4" /></Button>
                          ) : (
                            <Button variant="ghost" size="sm" title={t("userManagement.lock")} aria-label={t("userManagement.lock")} onClick={() => lockMutation.mutate(user.id as string)}><Lock className="h-4 w-4" /></Button>
                          ))}
                          {canUpdateUser && (
                            <Button variant="ghost" size="sm" title={t("userManagement.resetPassword")} aria-label={t("userManagement.resetPassword")} onClick={() => { setResetPwUser(user); setNewPassword(""); }}>
                              <Key className="h-4 w-4" />
                            </Button>
                          )}
                          {canDeleteUser && (
                            <Button variant="ghost" size="sm" title={t("common.delete")} aria-label={t("common.delete")} onClick={() => { if (confirm(`${t("userManagement.deleteUserConfirm")} ${user.userName}?`)) deleteMutation.mutate(user.id as string); }}>
                              <Trash2 className="h-4 w-4 text-destructive" />
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {(totalCount > pageSize || pageIndex > 0) && (
              <div className="flex items-center justify-between border-t px-4 py-3">
                <div className="text-sm tabular-nums text-muted-foreground">{totalCount} {t("userManagement.totalUsers")}</div>
                <div className="flex gap-2">
                  {pageIndex > 0 && (
                    <Button variant="outline" size="sm" aria-label={t("common.previous")} onClick={() => setPageIndex((p) => p - 1)}><ChevronLeft className="h-4 w-4" /></Button>
                  )}
                  {users.length === pageSize && (
                    <Button variant="outline" size="sm" aria-label={t("common.next")} onClick={() => setPageIndex((p) => p + 1)}><ChevronRight className="h-4 w-4" /></Button>
                  )}
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Create/Edit User Dialog */}
      <Dialog open={showCreateModal || !!editingUser} onClose={() => { setShowCreateModal(false); setEditingUser(null); resetForm(); }} size="lg">
        <DialogHeader onClose={() => { setShowCreateModal(false); setEditingUser(null); resetForm(); }}>
          {editingUser ? t("userManagement.editUser") : t("userManagement.createUser")}
        </DialogHeader>
        <form onSubmit={(e) => { e.preventDefault(); editingUser ? updateMutation.mutate() : createMutation.mutate(); }}>
          <DialogContent className="space-y-3">
            <div className="grid gap-3 md:grid-cols-2">
              <div><label className="text-sm font-medium">{t("userManagement.usernameLabel")}</label>
                <input type="text" value={userForm.userName} onChange={(e) => setUserForm({ ...userForm, userName: e.target.value })}
                  className="mt-1 w-full rounded-md border px-3 py-2" required disabled={!!editingUser} /></div>
              <div><label className="text-sm font-medium">{t("userManagement.emailLabel")}</label>
                <input type="email" value={userForm.email} onChange={(e) => setUserForm({ ...userForm, email: e.target.value })}
                  className="mt-1 w-full rounded-md border px-3 py-2" required /></div>
            </div>
            {!editingUser && (
              <div><label className="text-sm font-medium">{t("userManagement.passwordLabel")}</label>
                <input type="password" value={userForm.password} onChange={(e) => setUserForm({ ...userForm, password: e.target.value })}
                  className="mt-1 w-full rounded-md border px-3 py-2" required minLength={6} /></div>
            )}
            <div className="grid gap-3 md:grid-cols-2">
              <div><label className="text-sm font-medium">{t("userManagement.firstNameLabel")}</label>
                <input type="text" value={userForm.name} onChange={(e) => setUserForm({ ...userForm, name: e.target.value })}
                  className="mt-1 w-full rounded-md border px-3 py-2" /></div>
              <div><label className="text-sm font-medium">{t("userManagement.lastNameLabel")}</label>
                <input type="text" value={userForm.surname} onChange={(e) => setUserForm({ ...userForm, surname: e.target.value })}
                  className="mt-1 w-full rounded-md border px-3 py-2" /></div>
            </div>
            <div><label className="text-sm font-medium">{t("userManagement.phoneLabel")}</label>
              <input type="tel" value={userForm.phoneNumber} onChange={(e) => setUserForm({ ...userForm, phoneNumber: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2" /></div>
            <div className="flex items-center gap-2">
              <input type="checkbox" id="isActive" checked={userForm.isActive} onChange={(e) => setUserForm({ ...userForm, isActive: e.target.checked })} />
              <label htmlFor="isActive" className="text-sm font-medium">{t("userManagement.activeLabel")}</label>
            </div>
          </DialogContent>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => { setShowCreateModal(false); setEditingUser(null); resetForm(); }}>{t("common.cancel")}</Button>
            <Button type="submit" disabled={createMutation.isPending || updateMutation.isPending}>
              {editingUser ? t("common.save") : t("common.create")}
            </Button>
          </DialogFooter>
        </form>
      </Dialog>

      {/* Assign Roles Dialog */}
      <Dialog open={!!roleModalUser} onClose={() => setRoleModalUser(null)} size="md">
        <DialogHeader onClose={() => setRoleModalUser(null)}>
          {t("userManagement.assignRoles")} — {roleModalUser?.userName as string}
        </DialogHeader>
        <DialogContent className="space-y-3">
          {(allRoles || []).map((role: Record<string, unknown>) => (
            <label key={role.id as string} className="flex items-center gap-2 cursor-pointer">
              <input type="checkbox" checked={selectedRoles.includes(role.name as string)}
                onChange={(e) => {
                  if (e.target.checked) setSelectedRoles([...selectedRoles, role.name as string]);
                  else setSelectedRoles(selectedRoles.filter((r) => r !== role.name));
                }} />
              <span className="text-sm">{role.name as string}</span>
              {!!role.isDefault && <Badge variant="secondary">{t("userManagement.defaultBadge")}</Badge>}
            </label>
          ))}
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setRoleModalUser(null)}>{t("common.cancel")}</Button>
          <Button onClick={() => updateRolesMutation.mutate({ id: roleModalUser!.id as string, roleNames: selectedRoles })}
            disabled={updateRolesMutation.isPending}>{t("userManagement.saveRoles")}</Button>
        </DialogFooter>
      </Dialog>

      {/* Reset Password Dialog */}
      <Dialog open={!!resetPwUser} onClose={() => setResetPwUser(null)} size="sm">
        <DialogHeader onClose={() => setResetPwUser(null)}>
          {t("userManagement.resetPassword")} — {resetPwUser?.userName as string}
        </DialogHeader>
        <form onSubmit={(e) => { e.preventDefault(); resetPasswordMutation.mutate({ id: resetPwUser!.id as string, password: newPassword }); }}>
          <DialogContent>
            <div><label className="text-sm font-medium">{t("userManagement.newPasswordLabel")}</label>
              <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)}
                className="mt-1 w-full rounded-md border px-3 py-2" required minLength={6} /></div>
          </DialogContent>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setResetPwUser(null)}>{t("common.cancel")}</Button>
            <Button type="submit" disabled={resetPasswordMutation.isPending}>{t("userManagement.reset")}</Button>
          </DialogFooter>
        </form>
      </Dialog>
    </>
  );
}

// ---- Roles Tab ----
function RolesTab() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const canCreateRole = useHasPermission("KLC.RoleManagement.Create");
  const canUpdateRole = useHasPermission("KLC.RoleManagement.Update");
  const canDeleteRole = useHasPermission("KLC.RoleManagement.Delete");
  const canManagePermissions = useHasPermission("KLC.RoleManagement.ManagePermissions");
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingRole, setEditingRole] = useState<Record<string, unknown> | null>(null);
  const [permissionsRole, setPermissionsRole] = useState<Record<string, unknown> | null>(null);
  const [roleForm, setRoleForm] = useState({ name: "", isDefault: false, isPublic: true });
  const [permissionGrants, setPermissionGrants] = useState<Record<string, boolean>>({});
  const [permSearch, setPermSearch] = useState("");
  const [collapsedSections, setCollapsedSections] = useState<Record<string, boolean>>({});
  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>({});

  const { data: rolesData, isLoading } = useQuery({
    queryKey: ["roles"],
    queryFn: async () => {
      const { data } = await rolesApi.getAll({ maxResultCount: 100 });
      return data;
    },
  });

  const { data: permissionsData } = useQuery({
    queryKey: ["role-permissions", permissionsRole?.id],
    queryFn: async () => {
      if (!permissionsRole) return null;
      const { data } = await rolesApi.getPermissions(permissionsRole.id as string);
      return data;
    },
    enabled: !!permissionsRole,
  });

  const permissionGroups: PermissionGroup[] = Array.isArray(permissionsData) ? permissionsData : permissionsData?.groups || [];

  // Build a lookup from group name to group data
  const groupLookup = useMemo(() => {
    const map: Record<string, PermissionGroup> = {};
    for (const g of permissionGroups) map[g.name] = g;
    return map;
  }, [permissionGroups]);

  // Initialize grants when permissions data loads
  const initializeGrants = useCallback(() => {
    if (permissionGroups.length > 0) {
      const grants: Record<string, boolean> = {};
      for (const group of permissionGroups) {
        for (const perm of group.permissions || []) {
          grants[perm.name] = perm.isGranted;
        }
      }
      setPermissionGrants(grants);
    }
  }, [permissionGroups]);

  // Auto-initialize when permissions load
  useEffect(() => {
    if (permissionGroups.length > 0 && Object.keys(permissionGrants).length === 0) {
      initializeGrants();
    }
  }, [permissionGroups, permissionGrants, initializeGrants]);

  // Computed: total and granted counts
  const allPermNames = useMemo(() => {
    const names: string[] = [];
    for (const g of permissionGroups) for (const p of g.permissions || []) names.push(p.name);
    return names;
  }, [permissionGroups]);

  const grantedCount = useMemo(() =>
    allPermNames.filter((n) => permissionGrants[n]).length,
    [allPermNames, permissionGrants],
  );

  const handleGrantAll = () => {
    if (!confirm(t("permissions.grantAllConfirm"))) return;
    const next: Record<string, boolean> = {};
    for (const n of allPermNames) next[n] = true;
    setPermissionGrants(next);
  };

  const handleRevokeAll = () => {
    if (!confirm(t("permissions.revokeAllConfirm"))) return;
    const next: Record<string, boolean> = {};
    for (const n of allPermNames) next[n] = false;
    setPermissionGrants(next);
  };

  const toggleGroupAll = (group: PermissionGroup, grant: boolean) => {
    const next = { ...permissionGrants };
    for (const p of group.permissions || []) next[p.name] = grant;
    setPermissionGrants(next);
  };

  const getGroupState = (group: PermissionGroup) => {
    const perms = group.permissions || [];
    if (perms.length === 0) return { all: false, none: true, indeterminate: false };
    const checked = perms.filter((p) => permissionGrants[p.name]).length;
    return { all: checked === perms.length, none: checked === 0, indeterminate: checked > 0 && checked < perms.length };
  };

  const toggleSection = (key: string) => {
    setCollapsedSections((prev) => ({ ...prev, [key]: !prev[key] }));
  };

  // Filter groups by search
  const matchesSearch = (group: PermissionGroup) => {
    if (!permSearch) return true;
    const q = permSearch.toLowerCase();
    if (group.displayName.toLowerCase().includes(q)) return true;
    return group.permissions?.some((p) => p.displayName.toLowerCase().includes(q));
  };

  const createMutation = useMutation({
    mutationFn: async () => { await rolesApi.create(roleForm); },
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["roles"] }); setShowCreateModal(false); },
  });

  const updateMutation = useMutation({
    mutationFn: async () => {
      if (!editingRole) return;
      await rolesApi.update(editingRole.id as string, { ...roleForm, concurrencyStamp: editingRole.concurrencyStamp as string });
    },
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["roles"] }); setEditingRole(null); },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => { await rolesApi.delete(id); },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["roles"] }),
  });

  const updatePermissionsMutation = useMutation({
    mutationFn: async () => {
      if (!permissionsRole) return;
      const grantedPermissions = Object.entries(permissionGrants)
        .filter(([, isGranted]) => isGranted)
        .map(([name]) => name);
      await rolesApi.updatePermissions(permissionsRole.id as string, grantedPermissions);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["role-permissions"] });
      setPermissionsRole(null);
      // Refresh current user's sidebar permissions in case their role was updated
      authApi.getMyPermissions()
        .then(({ data }) => useAuthStore.getState().setPermissions(data))
        .catch(() => {});
    },
  });

  const roles = rolesData?.items || [];

  const toggleGroup = (name: string) => {
    setExpandedGroups((prev) => ({ ...prev, [name]: !prev[name] }));
  };

  // Render a sidebar-style nav item for a permission group
  const renderNavItem = (groupDef: { permissionGroup: string; icon: LucideIcon; labelKey: string }) => {
    const group = groupLookup[groupDef.permissionGroup];
    if (!group) return null;
    if (!matchesSearch(group)) return null;
    const Icon = groupDef.icon;
    const state = getGroupState(group);
    const childPerms = (group.permissions || []).filter((p) => p.name !== group.name);
    const filteredPerms = permSearch
      ? childPerms.filter((p) => p.displayName.toLowerCase().includes(permSearch.toLowerCase()) || group.displayName.toLowerCase().includes(permSearch.toLowerCase()))
      : childPerms;
    const isExpanded = expandedGroups[group.name];
    const checkedCount = (group.permissions || []).filter((p) => permissionGrants[p.name]).length;
    const totalCount = (group.permissions || []).length;

    return (
      <div key={group.name}>
        {/* Nav-style row */}
        <div className={`flex items-center gap-2 rounded-md px-2 py-1.5 transition-colors hover:bg-muted/60 ${
          state.all ? "text-foreground" : state.indeterminate ? "text-foreground" : "text-muted-foreground"
        }`}>
          {/* Expand chevron or spacer */}
          {childPerms.length > 0 ? (
            <button type="button" className="p-0.5 -ml-0.5 hover:bg-muted rounded" onClick={() => toggleGroup(group.name)}>
              {isExpanded
                ? <ChevronDown className="h-3.5 w-3.5 text-muted-foreground" />
                : <ChevronRight className="h-3.5 w-3.5 text-muted-foreground" />
              }
            </button>
          ) : (
            <span className="w-[18px]" />
          )}
          <Icon className="h-4 w-4 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm flex-1 truncate">{t(groupDef.labelKey)}</span>
          <span className={`text-[10px] tabular-nums px-1.5 py-0.5 rounded ${
            state.all ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400"
              : state.indeterminate ? "bg-amber-100 text-amber-700 dark:bg-amber-950/50 dark:text-amber-400"
              : "bg-muted text-muted-foreground"
          }`}>
            {checkedCount}/{totalCount}
          </span>
          <PermissionToggle
            checked={state.all}
            indeterminate={state.indeterminate}
            onChange={(checked) => toggleGroupAll(group, checked)}
          />
        </div>
        {/* Expanded children */}
        {isExpanded && filteredPerms.length > 0 && (
          <div className="ml-[34px] pl-3 border-l border-border/50 mt-0.5 mb-1.5 space-y-0.5">
            {filteredPerms.map((perm) => (
              <label key={perm.name} className="flex items-center gap-2 cursor-pointer rounded px-2 py-1 hover:bg-muted/40 transition-colors">
                <input type="checkbox" checked={permissionGrants[perm.name] ?? false}
                  onChange={(e) => setPermissionGrants({ ...permissionGrants, [perm.name]: e.target.checked })}
                  className="h-3.5 w-3.5 rounded border-gray-300 accent-primary" />
                <span className="text-xs">{perm.displayName}</span>
              </label>
            ))}
          </div>
        )}
      </div>
    );
  };

  // Check if a section has any visible groups
  const sectionHasVisibleGroups = (section: typeof PERMISSION_SECTIONS[0]) => {
    return section.groups.some((gDef) => {
      const group = groupLookup[gDef.permissionGroup];
      return group && matchesSearch(group);
    });
  };

  return (
    <>
      <div className="flex items-center justify-between mb-4">
        <div />
        {canCreateRole && (
          <Button onClick={() => { setRoleForm({ name: "", isDefault: false, isPublic: true }); setShowCreateModal(true); }}>
            <Plus className="mr-2 h-4 w-4" aria-hidden="true" /> {t("userManagement.addRole")}
          </Button>
        )}
      </div>

      {isLoading ? (
        <SkeletonTable rows={5} cols={4} />
      ) : roles.length === 0 ? (
        <Card>
          <CardContent className="p-0">
            <EmptyState
              icon={Shield}
              title={t("userManagement.noRolesFound")}
              description={t("userManagement.noRolesGetStarted")}
              action={{ label: t("userManagement.addRole"), onClick: () => { setRoleForm({ name: "", isDefault: false, isPublic: true }); setShowCreateModal(true); } }}
            />
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead><tr className="border-b bg-muted/50">
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("userManagement.roleName")}</th>
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("userManagement.default")}</th>
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("userManagement.static")}</th>
                  <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("common.actions")}</th>
                </tr></thead>
                <tbody>
                  {roles.map((role: Record<string, unknown>) => (
                    <tr key={role.id as string} className="border-b hover:bg-muted/50">
                      <td className="px-4 py-3 font-medium">{role.name as string}</td>
                      <td className="px-4 py-3"><Badge variant={role.isDefault ? "default" : "secondary"}>{role.isDefault ? t("userManagement.yes") : t("userManagement.no")}</Badge></td>
                      <td className="px-4 py-3"><Badge variant={role.isStatic ? "default" : "secondary"}>{role.isStatic ? t("userManagement.yes") : t("userManagement.no")}</Badge></td>
                      <td className="px-4 py-3">
                        <div className="flex gap-1">
                          {canUpdateRole && (
                            <Button variant="ghost" size="sm" title={t("common.edit")} aria-label={t("common.edit")} onClick={() => { setEditingRole(role); setRoleForm({ name: role.name as string, isDefault: role.isDefault as boolean, isPublic: (role.isPublic as boolean) ?? true }); }}>
                              <Edit className="h-4 w-4" />
                            </Button>
                          )}
                          {canManagePermissions && (
                            <Button variant="ghost" size="sm" title={t("userManagement.permissions")} aria-label={t("userManagement.permissions")} onClick={() => { setPermissionsRole(role); setPermissionGrants({}); setPermSearch(""); setCollapsedSections({}); setExpandedGroups({}); }}>
                              <Shield className="h-4 w-4" />
                            </Button>
                          )}
                          {canDeleteRole && !role.isStatic && (
                            <Button variant="ghost" size="sm" title={t("common.delete")} aria-label={t("common.delete")} onClick={() => { if (confirm(`${t("userManagement.deleteRoleConfirm")} ${role.name}?`)) deleteMutation.mutate(role.id as string); }}>
                              <Trash2 className="h-4 w-4 text-destructive" />
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Create/Edit Role Dialog */}
      <Dialog open={showCreateModal || !!editingRole} onClose={() => { setShowCreateModal(false); setEditingRole(null); }} size="md">
        <DialogHeader onClose={() => { setShowCreateModal(false); setEditingRole(null); }}>
          {editingRole ? t("userManagement.editRole") : t("userManagement.createRole")}
        </DialogHeader>
        <form onSubmit={(e) => { e.preventDefault(); editingRole ? updateMutation.mutate() : createMutation.mutate(); }}>
          <DialogContent className="space-y-3">
            <div><label className="text-sm font-medium">{t("userManagement.roleNameLabel")}</label>
              <input type="text" value={roleForm.name} onChange={(e) => setRoleForm({ ...roleForm, name: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2" required /></div>
            <div className="flex items-center gap-4">
              <label className="flex items-center gap-2"><input type="checkbox" checked={roleForm.isDefault}
                onChange={(e) => setRoleForm({ ...roleForm, isDefault: e.target.checked })} /><span className="text-sm">{t("userManagement.defaultRole")}</span></label>
              <label className="flex items-center gap-2"><input type="checkbox" checked={roleForm.isPublic}
                onChange={(e) => setRoleForm({ ...roleForm, isPublic: e.target.checked })} /><span className="text-sm">{t("userManagement.public")}</span></label>
            </div>
          </DialogContent>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => { setShowCreateModal(false); setEditingRole(null); }}>{t("common.cancel")}</Button>
            <Button type="submit" disabled={createMutation.isPending || updateMutation.isPending}>
              {editingRole ? t("common.save") : t("common.create")}
            </Button>
          </DialogFooter>
        </form>
      </Dialog>

      {/* Permissions Dialog — Sidebar-style Navigation Tree */}
      <Dialog open={!!permissionsRole} onClose={() => setPermissionsRole(null)} size="lg" className="max-h-[85vh] flex flex-col">
        <DialogHeader onClose={() => setPermissionsRole(null)}>
          {t("userManagement.permissions")} — {permissionsRole?.name as string}
        </DialogHeader>
        <DialogContent className="overflow-y-auto flex-1 px-3 py-3">
          {permissionGroups.length > 0 ? (
            <>
              {/* Toolbar */}
              <div className="flex items-center gap-2 mb-3 pb-2 border-b sticky top-0 bg-background z-10">
                <Button variant="outline" size="sm" onClick={handleGrantAll}>
                  {t("permissions.grantAll")}
                </Button>
                <Button variant="outline" size="sm" onClick={handleRevokeAll}>
                  {t("permissions.revokeAll")}
                </Button>
                <div className="relative flex-1 ml-1">
                  <Search className="absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
                  <input
                    type="search"
                    placeholder={t("permissions.searchPermissions")}
                    value={permSearch}
                    onChange={(e) => setPermSearch(e.target.value)}
                    className="h-8 w-full rounded-md border bg-background pl-8 pr-3 text-xs focus:outline-none focus:ring-2 focus:ring-ring"
                    aria-label={t("permissions.searchPermissions")}
                  />
                </div>
              </div>

              {/* Navigation tree — mirrors left sidebar */}
              <nav className="space-y-4">
                {PERMISSION_SECTIONS.map((section) => {
                  if (!sectionHasVisibleGroups(section)) return null;
                  const isCollapsed = collapsedSections[section.sectionKey];
                  return (
                    <div key={section.sectionKey}>
                      <button
                        type="button"
                        className="flex items-center gap-1 w-full text-left px-2 mb-1"
                        onClick={() => toggleSection(section.sectionKey)}
                      >
                        <span className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                          {t(section.sectionKey)}
                        </span>
                      </button>
                      {!isCollapsed && (
                        <div className="space-y-0.5">
                          {section.groups.map((gDef) => renderNavItem(gDef))}
                        </div>
                      )}
                    </div>
                  );
                })}

                {/* Unmapped groups */}
                {(() => {
                  const mappedNames = new Set(PERMISSION_SECTIONS.flatMap((s) => s.groups.map((g) => g.permissionGroup)));
                  const unmapped = permissionGroups.filter((g) => !mappedNames.has(g.name) && matchesSearch(g));
                  if (unmapped.length === 0) return null;
                  return (
                    <div>
                      <div className="px-2 mb-1">
                        <span className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                          {t("common.other") ?? "Other"}
                        </span>
                      </div>
                      <div className="space-y-0.5">
                        {unmapped.map((group) => renderNavItem({ permissionGroup: group.name, icon: FileText, labelKey: group.displayName }))}
                      </div>
                    </div>
                  );
                })()}
              </nav>

              {/* No results */}
              {permSearch && !PERMISSION_SECTIONS.some((s) => sectionHasVisibleGroups(s)) && (
                <p className="text-center text-sm text-muted-foreground py-8">{t("permissions.noResults")}</p>
              )}
            </>
          ) : (
            <SkeletonTable rows={4} cols={2} />
          )}
        </DialogContent>
        {permissionGroups.length > 0 && (
          <div className="border-t px-4 py-3 flex items-center justify-between">
            <span className="text-xs text-muted-foreground tabular-nums">
              {t("permissions.summary").replace("{granted}", String(grantedCount)).replace("{total}", String(allPermNames.length))}
            </span>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setPermissionsRole(null)}>{t("common.cancel")}</Button>
              <Button onClick={() => updatePermissionsMutation.mutate()} disabled={updatePermissionsMutation.isPending}>{t("userManagement.savePermissions")}</Button>
            </div>
          </div>
        )}
      </Dialog>
    </>
  );
}

// ---- Main Page ----
export default function UserManagementPage() {
  const hasAccess = useRequirePermission("KLC.UserManagement");
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<TabType>("users");

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title={t("userManagement.title")} description={t("userManagement.description")} />
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* Tabs */}
        <div className="flex gap-2 border-b" role="tablist">
          <button
            role="tab"
            aria-selected={activeTab === "users"}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === "users" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}
            onClick={() => setActiveTab("users")}
          >
            <Users className="inline mr-2 h-4 w-4" aria-hidden="true" />{t("userManagement.usersTab")}
          </button>
          <button
            role="tab"
            aria-selected={activeTab === "roles"}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === "roles" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}
            onClick={() => setActiveTab("roles")}
          >
            <Shield className="inline mr-2 h-4 w-4" aria-hidden="true" />{t("userManagement.rolesTab")}
          </button>
        </div>

        {activeTab === "users" ? <UsersTab /> : <RolesTab />}
      </div>
    </div>
  );
}
