"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Header } from "@/components/layout/header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { usersApi, rolesApi } from "@/lib/api";
import { formatDateTime } from "@/lib/utils";
import {
  Plus, Edit, Trash2, Lock, Unlock, Key, Shield, Search, X, Users, ChevronLeft, ChevronRight,
} from "lucide-react";

type TabType = "users" | "roles";

// ---- Users Tab ----
function UsersTab() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingUser, setEditingUser] = useState<Record<string, unknown> | null>(null);
  const [roleModalUser, setRoleModalUser] = useState<Record<string, unknown> | null>(null);
  const [resetPwUser, setResetPwUser] = useState<Record<string, unknown> | null>(null);
  const [newPassword, setNewPassword] = useState("");
  const [userForm, setUserForm] = useState({ userName: "", email: "", password: "", name: "", surname: "", phoneNumber: "", isActive: true, roleNames: [] as string[] });
  const pageSize = 20;

  const { data: usersData, isLoading } = useQuery({
    queryKey: ["users", search, currentPage],
    queryFn: async () => {
      const { data } = await usersApi.getAll({ skipCount: (currentPage - 1) * pageSize, maxResultCount: pageSize, filter: search || undefined });
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
  const totalPages = Math.ceil(totalCount / pageSize);

  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);

  return (
    <>
      {/* Search + Create */}
      <div className="flex items-center justify-between mb-4">
        <div className="relative w-72">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input type="search" placeholder="Search users..." value={search}
            onChange={(e) => { setSearch(e.target.value); setCurrentPage(1); }}
            className="h-10 w-full rounded-md border bg-background pl-9 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-ring" />
        </div>
        <Button onClick={() => { resetForm(); setShowCreateModal(true); }}><Plus className="mr-2 h-4 w-4" /> Add User</Button>
      </div>

      {/* Users Table */}
      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead><tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left text-sm font-medium">Username</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Email</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Name</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Roles</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Status</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Created</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Actions</th>
              </tr></thead>
              <tbody>
                {isLoading ? (
                  <tr><td colSpan={7} className="px-4 py-8 text-center">Loading...</td></tr>
                ) : users.length > 0 ? users.map((user: Record<string, unknown>) => (
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
                        <Badge variant={user.isActive ? "success" : "secondary"}>{user.isActive ? "Active" : "Inactive"}</Badge>
                        {!!user.isLockedOut && <Badge variant="destructive">Locked</Badge>}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-sm text-muted-foreground">{user.creationTime ? formatDateTime(user.creationTime as string) : ""}</td>
                    <td className="px-4 py-3">
                      <div className="flex gap-1">
                        <Button variant="ghost" size="sm" title="Edit" onClick={() => {
                          setEditingUser(user);
                          setUserForm({ userName: user.userName as string, email: user.email as string, password: "", name: (user.name as string) || "", surname: (user.surname as string) || "", phoneNumber: (user.phoneNumber as string) || "", isActive: user.isActive as boolean, roleNames: (user.roles as string[]) || [] });
                        }}><Edit className="h-4 w-4" /></Button>
                        <Button variant="ghost" size="sm" title="Assign Roles" onClick={() => { setRoleModalUser(user); setSelectedRoles((user.roles as string[]) || []); }}>
                          <Shield className="h-4 w-4" />
                        </Button>
                        {!!user.isLockedOut ? (
                          <Button variant="ghost" size="sm" title="Unlock" onClick={() => unlockMutation.mutate(user.id as string)}><Unlock className="h-4 w-4" /></Button>
                        ) : (
                          <Button variant="ghost" size="sm" title="Lock" onClick={() => lockMutation.mutate(user.id as string)}><Lock className="h-4 w-4" /></Button>
                        )}
                        <Button variant="ghost" size="sm" title="Reset Password" onClick={() => { setResetPwUser(user); setNewPassword(""); }}>
                          <Key className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="sm" title="Delete" onClick={() => { if (confirm(`Delete user ${user.userName}?`)) deleteMutation.mutate(user.id as string); }}>
                          <Trash2 className="h-4 w-4 text-red-500" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                )) : (
                  <tr><td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">No users found</td></tr>
                )}
              </tbody>
            </table>
          </div>
          {totalPages > 1 && (
            <div className="flex items-center justify-between border-t px-4 py-3">
              <div className="text-sm text-muted-foreground">Page {currentPage} of {totalPages} ({totalCount} users)</div>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" onClick={() => setCurrentPage((p) => Math.max(1, p - 1))} disabled={currentPage === 1}><ChevronLeft className="h-4 w-4" /></Button>
                <Button variant="outline" size="sm" onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages}><ChevronRight className="h-4 w-4" /></Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit User Modal */}
      {(showCreateModal || editingUser) && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <Card className="w-full max-w-lg m-4">
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>{editingUser ? "Edit User" : "Create User"}</CardTitle>
              <Button variant="ghost" size="sm" onClick={() => { setShowCreateModal(false); setEditingUser(null); resetForm(); }}><X className="h-4 w-4" /></Button>
            </CardHeader>
            <CardContent>
              <form onSubmit={(e) => { e.preventDefault(); editingUser ? updateMutation.mutate() : createMutation.mutate(); }} className="space-y-3">
                <div className="grid gap-3 md:grid-cols-2">
                  <div><label className="text-sm font-medium">Username *</label>
                    <input type="text" value={userForm.userName} onChange={(e) => setUserForm({ ...userForm, userName: e.target.value })}
                      className="mt-1 w-full rounded-md border px-3 py-2" required disabled={!!editingUser} /></div>
                  <div><label className="text-sm font-medium">Email *</label>
                    <input type="email" value={userForm.email} onChange={(e) => setUserForm({ ...userForm, email: e.target.value })}
                      className="mt-1 w-full rounded-md border px-3 py-2" required /></div>
                </div>
                {!editingUser && (
                  <div><label className="text-sm font-medium">Password *</label>
                    <input type="password" value={userForm.password} onChange={(e) => setUserForm({ ...userForm, password: e.target.value })}
                      className="mt-1 w-full rounded-md border px-3 py-2" required minLength={6} /></div>
                )}
                <div className="grid gap-3 md:grid-cols-2">
                  <div><label className="text-sm font-medium">First Name</label>
                    <input type="text" value={userForm.name} onChange={(e) => setUserForm({ ...userForm, name: e.target.value })}
                      className="mt-1 w-full rounded-md border px-3 py-2" /></div>
                  <div><label className="text-sm font-medium">Last Name</label>
                    <input type="text" value={userForm.surname} onChange={(e) => setUserForm({ ...userForm, surname: e.target.value })}
                      className="mt-1 w-full rounded-md border px-3 py-2" /></div>
                </div>
                <div><label className="text-sm font-medium">Phone</label>
                  <input type="tel" value={userForm.phoneNumber} onChange={(e) => setUserForm({ ...userForm, phoneNumber: e.target.value })}
                    className="mt-1 w-full rounded-md border px-3 py-2" /></div>
                <div className="flex items-center gap-2">
                  <input type="checkbox" id="isActive" checked={userForm.isActive} onChange={(e) => setUserForm({ ...userForm, isActive: e.target.checked })} />
                  <label htmlFor="isActive" className="text-sm font-medium">Active</label>
                </div>
                <div className="flex gap-2">
                  <Button type="submit" disabled={createMutation.isPending || updateMutation.isPending}>
                    {editingUser ? "Save" : "Create"}
                  </Button>
                  <Button type="button" variant="outline" onClick={() => { setShowCreateModal(false); setEditingUser(null); resetForm(); }}>Cancel</Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Assign Roles Modal */}
      {roleModalUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <Card className="w-full max-w-md m-4">
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Assign Roles — {roleModalUser.userName as string}</CardTitle>
              <Button variant="ghost" size="sm" onClick={() => setRoleModalUser(null)}><X className="h-4 w-4" /></Button>
            </CardHeader>
            <CardContent className="space-y-3">
              {(allRoles || []).map((role: Record<string, unknown>) => (
                <label key={role.id as string} className="flex items-center gap-2 cursor-pointer">
                  <input type="checkbox" checked={selectedRoles.includes(role.name as string)}
                    onChange={(e) => {
                      if (e.target.checked) setSelectedRoles([...selectedRoles, role.name as string]);
                      else setSelectedRoles(selectedRoles.filter((r) => r !== role.name));
                    }} />
                  <span className="text-sm">{role.name as string}</span>
                  {!!role.isDefault && <Badge variant="secondary">Default</Badge>}
                </label>
              ))}
              <div className="flex gap-2 pt-2">
                <Button onClick={() => updateRolesMutation.mutate({ id: roleModalUser.id as string, roleNames: selectedRoles })}
                  disabled={updateRolesMutation.isPending}>Save Roles</Button>
                <Button variant="outline" onClick={() => setRoleModalUser(null)}>Cancel</Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Reset Password Modal */}
      {resetPwUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <Card className="w-full max-w-sm m-4">
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Reset Password — {resetPwUser.userName as string}</CardTitle>
              <Button variant="ghost" size="sm" onClick={() => setResetPwUser(null)}><X className="h-4 w-4" /></Button>
            </CardHeader>
            <CardContent>
              <form onSubmit={(e) => { e.preventDefault(); resetPasswordMutation.mutate({ id: resetPwUser.id as string, password: newPassword }); }} className="space-y-3">
                <div><label className="text-sm font-medium">New Password *</label>
                  <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)}
                    className="mt-1 w-full rounded-md border px-3 py-2" required minLength={6} /></div>
                <div className="flex gap-2">
                  <Button type="submit" disabled={resetPasswordMutation.isPending}>Reset</Button>
                  <Button type="button" variant="outline" onClick={() => setResetPwUser(null)}>Cancel</Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}
    </>
  );
}

// ---- Roles Tab ----
function RolesTab() {
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
          <Plus className="mr-2 h-4 w-4" /> Add Role
        </Button>
      </div>

      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead><tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left text-sm font-medium">Name</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Default</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Static</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Actions</th>
              </tr></thead>
              <tbody>
                {isLoading ? (
                  <tr><td colSpan={4} className="px-4 py-8 text-center">Loading...</td></tr>
                ) : roles.length > 0 ? roles.map((role: Record<string, unknown>) => (
                  <tr key={role.id as string} className="border-b hover:bg-muted/50">
                    <td className="px-4 py-3 font-medium">{role.name as string}</td>
                    <td className="px-4 py-3"><Badge variant={role.isDefault ? "default" : "secondary"}>{role.isDefault ? "Yes" : "No"}</Badge></td>
                    <td className="px-4 py-3"><Badge variant={role.isStatic ? "default" : "secondary"}>{role.isStatic ? "Yes" : "No"}</Badge></td>
                    <td className="px-4 py-3">
                      <div className="flex gap-1">
                        <Button variant="ghost" size="sm" title="Edit" onClick={() => { setEditingRole(role); setRoleForm({ name: role.name as string, isDefault: role.isDefault as boolean, isPublic: (role.isPublic as boolean) ?? true }); }}>
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="sm" title="Permissions" onClick={() => { setPermissionsRole(role); setPermissionGrants({}); }}>
                          <Shield className="h-4 w-4" />
                        </Button>
                        {!role.isStatic && (
                          <Button variant="ghost" size="sm" title="Delete" onClick={() => { if (confirm(`Delete role ${role.name}?`)) deleteMutation.mutate(role.id as string); }}>
                            <Trash2 className="h-4 w-4 text-red-500" />
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                )) : (
                  <tr><td colSpan={4} className="px-4 py-8 text-center text-muted-foreground">No roles found</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      {/* Create/Edit Role Modal */}
      {(showCreateModal || editingRole) && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <Card className="w-full max-w-md m-4">
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>{editingRole ? "Edit Role" : "Create Role"}</CardTitle>
              <Button variant="ghost" size="sm" onClick={() => { setShowCreateModal(false); setEditingRole(null); }}><X className="h-4 w-4" /></Button>
            </CardHeader>
            <CardContent>
              <form onSubmit={(e) => { e.preventDefault(); editingRole ? updateMutation.mutate() : createMutation.mutate(); }} className="space-y-3">
                <div><label className="text-sm font-medium">Role Name *</label>
                  <input type="text" value={roleForm.name} onChange={(e) => setRoleForm({ ...roleForm, name: e.target.value })}
                    className="mt-1 w-full rounded-md border px-3 py-2" required /></div>
                <div className="flex items-center gap-4">
                  <label className="flex items-center gap-2"><input type="checkbox" checked={roleForm.isDefault}
                    onChange={(e) => setRoleForm({ ...roleForm, isDefault: e.target.checked })} /><span className="text-sm">Default role</span></label>
                  <label className="flex items-center gap-2"><input type="checkbox" checked={roleForm.isPublic}
                    onChange={(e) => setRoleForm({ ...roleForm, isPublic: e.target.checked })} /><span className="text-sm">Public</span></label>
                </div>
                <div className="flex gap-2">
                  <Button type="submit" disabled={createMutation.isPending || updateMutation.isPending}>
                    {editingRole ? "Save" : "Create"}
                  </Button>
                  <Button type="button" variant="outline" onClick={() => { setShowCreateModal(false); setEditingRole(null); }}>Cancel</Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Permissions Modal */}
      {permissionsRole && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <Card className="w-full max-w-2xl m-4 max-h-[80vh] overflow-y-auto">
            <CardHeader className="flex flex-row items-center justify-between sticky top-0 bg-card z-10">
              <CardTitle>Permissions — {permissionsRole.name as string}</CardTitle>
              <Button variant="ghost" size="sm" onClick={() => setPermissionsRole(null)}><X className="h-4 w-4" /></Button>
            </CardHeader>
            <CardContent className="space-y-4">
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
                  <div className="flex gap-2 pt-2 sticky bottom-0 bg-card py-3">
                    <Button onClick={() => updatePermissionsMutation.mutate()} disabled={updatePermissionsMutation.isPending}>Save Permissions</Button>
                    <Button variant="outline" onClick={() => setPermissionsRole(null)}>Cancel</Button>
                  </div>
                </>
              ) : (
                <p className="text-center text-muted-foreground">Loading permissions...</p>
              )}
            </CardContent>
          </Card>
        </div>
      )}
    </>
  );
}

// ---- Main Page ----
export default function UserManagementPage() {
  const [activeTab, setActiveTab] = useState<TabType>("users");

  return (
    <div className="flex flex-col">
      <Header title="User Management" description="Manage users, roles, and permissions" />

      <div className="flex-1 space-y-6 p-6">
        {/* Tabs */}
        <div className="flex gap-2 border-b">
          <button
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === "users" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}
            onClick={() => setActiveTab("users")}
          >
            <Users className="inline mr-2 h-4 w-4" />Users
          </button>
          <button
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${activeTab === "roles" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"}`}
            onClick={() => setActiveTab("roles")}
          >
            <Shield className="inline mr-2 h-4 w-4" />Roles
          </button>
        </div>

        {activeTab === "users" ? <UsersTab /> : <RolesTab />}
      </div>
    </div>
  );
}
