"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { PageHeader } from "@/components/ui/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { SkeletonTable } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { usersApi, rolesApi } from "@/lib/api";
import { formatDateTime } from "@/lib/utils";
import { useTranslation } from "@/lib/i18n";
import {
  Plus, Edit, Trash2, Lock, Unlock, Key, Shield, Search, Users, ChevronLeft, ChevronRight,
} from "lucide-react";

type TabType = "users" | "roles";

// ---- Users Tab ----
function UsersTab() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
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
        <Button onClick={() => { resetForm(); setShowCreateModal(true); }}><Plus className="mr-2 h-4 w-4" aria-hidden="true" /> {t("userManagement.addUser")}</Button>
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
                          <Button variant="ghost" size="sm" title={t("common.edit")} aria-label={t("common.edit")} onClick={() => {
                            setEditingUser(user);
                            setUserForm({ userName: user.userName as string, email: user.email as string, password: "", name: (user.name as string) || "", surname: (user.surname as string) || "", phoneNumber: (user.phoneNumber as string) || "", isActive: user.isActive as boolean, roleNames: (user.roles as string[]) || [] });
                          }}><Edit className="h-4 w-4" /></Button>
                          <Button variant="ghost" size="sm" title={t("userManagement.assignRoles")} aria-label={t("userManagement.assignRoles")} onClick={() => { setRoleModalUser(user); setSelectedRoles((user.roles as string[]) || []); }}>
                            <Shield className="h-4 w-4" />
                          </Button>
                          {!!user.isLockedOut ? (
                            <Button variant="ghost" size="sm" title={t("userManagement.unlock")} aria-label={t("userManagement.unlock")} onClick={() => unlockMutation.mutate(user.id as string)}><Unlock className="h-4 w-4" /></Button>
                          ) : (
                            <Button variant="ghost" size="sm" title={t("userManagement.lock")} aria-label={t("userManagement.lock")} onClick={() => lockMutation.mutate(user.id as string)}><Lock className="h-4 w-4" /></Button>
                          )}
                          <Button variant="ghost" size="sm" title={t("userManagement.resetPassword")} aria-label={t("userManagement.resetPassword")} onClick={() => { setResetPwUser(user); setNewPassword(""); }}>
                            <Key className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm" title={t("common.delete")} aria-label={t("common.delete")} onClick={() => { if (confirm(`${t("userManagement.deleteUserConfirm")} ${user.userName}?`)) deleteMutation.mutate(user.id as string); }}>
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
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
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingRole, setEditingRole] = useState<Record<string, unknown> | null>(null);
  const [permissionsRole, setPermissionsRole] = useState<Record<string, unknown> | null>(null);
  const [roleForm, setRoleForm] = useState({ name: "", isDefault: false, isPublic: true });
  const [permissionGrants, setPermissionGrants] = useState<Record<string, boolean>>({});

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

  // Initialize permission grants when permissions data loads
  const permissionGroups = Array.isArray(permissionsData) ? permissionsData : permissionsData?.groups || [];

  const initializeGrants = () => {
    if (permissionGroups.length > 0) {
      const grants: Record<string, boolean> = {};
      for (const group of permissionGroups) {
        for (const perm of group.permissions || []) {
          grants[perm.name] = perm.isGranted;
        }
      }
      setPermissionGrants(grants);
    }
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
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["role-permissions"] }); setPermissionsRole(null); },
  });

  const roles = rolesData?.items || [];

  return (
    <>
      <div className="flex items-center justify-between mb-4">
        <div />
        <Button onClick={() => { setRoleForm({ name: "", isDefault: false, isPublic: true }); setShowCreateModal(true); }}>
          <Plus className="mr-2 h-4 w-4" aria-hidden="true" /> {t("userManagement.addRole")}
        </Button>
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
                          <Button variant="ghost" size="sm" title={t("common.edit")} aria-label={t("common.edit")} onClick={() => { setEditingRole(role); setRoleForm({ name: role.name as string, isDefault: role.isDefault as boolean, isPublic: (role.isPublic as boolean) ?? true }); }}>
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button variant="ghost" size="sm" title={t("userManagement.permissions")} aria-label={t("userManagement.permissions")} onClick={() => { setPermissionsRole(role); setPermissionGrants({}); }}>
                            <Shield className="h-4 w-4" />
                          </Button>
                          {!role.isStatic && (
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

      {/* Permissions Dialog */}
      <Dialog open={!!permissionsRole} onClose={() => setPermissionsRole(null)} size="xl" className="max-h-[80vh] flex flex-col">
        <DialogHeader onClose={() => setPermissionsRole(null)}>
          {t("userManagement.permissions")} — {permissionsRole?.name as string}
        </DialogHeader>
        <DialogContent className="space-y-4 overflow-y-auto flex-1">
          {permissionGroups.length > 0 ? (
            <>
              {!Object.keys(permissionGrants).length && initializeGrants()}
              {permissionGroups.map((group: { name: string; displayName: string; permissions: Array<{ name: string; displayName: string; isGranted: boolean }> }) => (
                <div key={group.name} className="space-y-2">
                  <h4 className="font-medium text-sm">{group.displayName}</h4>
                  <div className="grid gap-1 pl-4">
                    {group.permissions.map((perm) => (
                      <label key={perm.name} className="flex items-center gap-2 cursor-pointer">
                        <input type="checkbox" checked={permissionGrants[perm.name] ?? perm.isGranted}
                          onChange={(e) => setPermissionGrants({ ...permissionGrants, [perm.name]: e.target.checked })} />
                        <span className="text-sm">{perm.displayName}</span>
                      </label>
                    ))}
                  </div>
                </div>
              ))}
            </>
          ) : (
            <SkeletonTable rows={4} cols={2} />
          )}
        </DialogContent>
        {permissionGroups.length > 0 && (
          <DialogFooter>
            <Button variant="outline" onClick={() => setPermissionsRole(null)}>{t("common.cancel")}</Button>
            <Button onClick={() => updatePermissionsMutation.mutate()} disabled={updatePermissionsMutation.isPending}>{t("userManagement.savePermissions")}</Button>
          </DialogFooter>
        )}
      </Dialog>
    </>
  );
}

// ---- Main Page ----
export default function UserManagementPage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<TabType>("users");

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
