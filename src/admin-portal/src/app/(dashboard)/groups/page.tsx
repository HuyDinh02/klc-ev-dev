"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { api, stationGroupsApi } from "@/lib/api";
import {
  Plus,
  Edit,
  Trash2,
  FolderTree,
  MapPin,
  X,
  ChevronDown,
  ChevronRight,
  Layers,
  Zap,
  AlertTriangle,
  WifiOff,
  BarChart3,
  Building2,
  Wrench,
  Briefcase,
  Tag,
  FolderOpen,
} from "lucide-react";

// --- Types ---

interface StationGroupStats {
  totalStations: number;
  totalConnectors: number;
  availableConnectors: number;
  occupiedConnectors: number;
  faultedConnectors: number;
  offlineStations: number;
  totalCapacityKw: number;
}

interface GroupStation {
  stationId: string;
  name: string;
  address: string;
  status: string;
  connectorCount: number;
  availableConnectors: number;
}

interface StationGroupList {
  id: string;
  name: string;
  description: string;
  region: string;
  groupType: number;
  parentGroupId: string | null;
  parentGroupName: string | null;
  isActive: boolean;
  stationCount: number;
  childGroupCount: number;
}

interface StationGroupDetail extends StationGroupList {
  stations: GroupStation[];
  childGroups: StationGroupList[];
  stats: StationGroupStats;
  creationTime: string;
}

interface Station {
  id: string;
  name: string;
  address: string;
  groupId?: string;
}

// --- Constants ---

const GroupTypeLabels: Record<number, string> = {
  0: "Geographic",
  1: "Operational",
  2: "Business",
  3: "Custom",
};

const GroupTypeIcons: Record<number, typeof MapPin> = {
  0: MapPin,
  1: Wrench,
  2: Briefcase,
  3: Tag,
};

const GroupTypeColors: Record<number, string> = {
  0: "bg-blue-100 text-blue-700",
  1: "bg-orange-100 text-orange-700",
  2: "bg-green-100 text-green-700",
  3: "bg-purple-100 text-purple-700",
};

const StationStatusConfig: Record<string, { label: string; color: string }> = {
  Offline: { label: "Offline", color: "bg-gray-100 text-gray-700" },
  Available: { label: "Available", color: "bg-green-100 text-green-700" },
  Occupied: { label: "Occupied", color: "bg-blue-100 text-blue-700" },
  Unavailable: { label: "Unavailable", color: "bg-yellow-100 text-yellow-700" },
  Faulted: { label: "Faulted", color: "bg-red-100 text-red-700" },
  Decommissioned: { label: "Decommissioned", color: "bg-gray-200 text-gray-500" },
};

// --- Components ---

function StatCard({ icon: Icon, label, value, color }: {
  icon: typeof Zap;
  label: string;
  value: number | string;
  color: string;
}) {
  return (
    <div className={`flex items-center gap-2 rounded-lg border px-3 py-2 ${color}`}>
      <Icon className="h-4 w-4 shrink-0" />
      <div>
        <p className="text-xs font-medium opacity-70">{label}</p>
        <p className="text-sm font-bold">{value}</p>
      </div>
    </div>
  );
}

function GroupForm({
  initialData,
  parentGroups,
  onSubmit,
  onCancel,
  isLoading,
}: {
  initialData?: { name: string; description: string; region: string; groupType: number; parentGroupId: string | null; isActive?: boolean };
  parentGroups: StationGroupList[];
  onSubmit: (data: {
    name: string;
    description: string;
    region: string;
    groupType: number;
    parentGroupId: string | null;
    isActive?: boolean;
  }) => void;
  onCancel: () => void;
  isLoading: boolean;
}) {
  const [form, setForm] = useState({
    name: initialData?.name || "",
    description: initialData?.description || "",
    region: initialData?.region || "",
    groupType: initialData?.groupType ?? 0,
    parentGroupId: initialData?.parentGroupId || "",
    isActive: initialData?.isActive ?? true,
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>{initialData ? "Edit Group" : "New Group"}</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            onSubmit({
              ...form,
              parentGroupId: form.parentGroupId || null,
            });
          }}
          className="space-y-4"
        >
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            <div>
              <label className="text-sm font-medium">Group Name *</label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                placeholder="e.g., Khu vực Hà Nội"
                required
              />
            </div>
            <div>
              <label className="text-sm font-medium">Region</label>
              <input
                type="text"
                value={form.region}
                onChange={(e) => setForm({ ...form, region: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                placeholder="e.g., North, Central, South"
              />
            </div>
            <div>
              <label className="text-sm font-medium">Type</label>
              <select
                value={form.groupType}
                onChange={(e) => setForm({ ...form, groupType: Number(e.target.value) })}
                className="mt-1 w-full rounded-md border px-3 py-2 text-sm bg-white"
              >
                {Object.entries(GroupTypeLabels).map(([val, label]) => (
                  <option key={val} value={val}>{label}</option>
                ))}
              </select>
            </div>
            <div className="md:col-span-2">
              <label className="text-sm font-medium">Description</label>
              <input
                type="text"
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                placeholder="Optional description"
              />
            </div>
            <div>
              <label className="text-sm font-medium">Parent Group</label>
              <select
                value={form.parentGroupId}
                onChange={(e) => setForm({ ...form, parentGroupId: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2 text-sm bg-white"
              >
                <option value="">None (Top Level)</option>
                {parentGroups.map((g) => (
                  <option key={g.id} value={g.id}>{g.name}</option>
                ))}
              </select>
            </div>
          </div>
          {initialData && (
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                className="rounded"
              />
              Active
            </label>
          )}
          <div className="flex gap-2">
            <Button type="submit" disabled={isLoading}>
              {initialData ? "Update" : "Create"} Group
            </Button>
            <Button type="button" variant="outline" onClick={onCancel}>
              Cancel
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

function GroupCard({
  group,
  onEdit,
  onDelete,
  onAssignStation,
  level = 0,
}: {
  group: StationGroupList;
  onEdit: (group: StationGroupList) => void;
  onDelete: (id: string) => void;
  onAssignStation: (groupId: string) => void;
  level?: number;
}) {
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);
  const [showAssign, setShowAssign] = useState(false);

  // Fetch detail when expanded
  const { data: detail, isLoading: detailLoading } = useQuery<StationGroupDetail>({
    queryKey: ["station-group-detail", group.id],
    queryFn: async () => {
      const res = await stationGroupsApi.getById(group.id);
      return res.data;
    },
    enabled: expanded,
    staleTime: 30_000,
  });

  // Fetch unassigned stations when assigning
  const { data: unassignedStations } = useQuery<Station[]>({
    queryKey: ["unassigned-stations"],
    queryFn: async () => {
      const res = await api.get("/stations", {
        params: { maxResultCount: 100 },
      });
      return (res.data.items || []).filter((s: Station) => !s.groupId);
    },
    enabled: showAssign,
  });

  const assignMutation = useMutation({
    mutationFn: async (stationId: string) => {
      await stationGroupsApi.assignStation(group.id, stationId);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-groups"] });
      queryClient.invalidateQueries({ queryKey: ["station-group-detail", group.id] });
      queryClient.invalidateQueries({ queryKey: ["unassigned-stations"] });
    },
  });

  const unassignMutation = useMutation({
    mutationFn: async (stationId: string) => {
      await stationGroupsApi.unassignStation(group.id, stationId);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-groups"] });
      queryClient.invalidateQueries({ queryKey: ["station-group-detail", group.id] });
      queryClient.invalidateQueries({ queryKey: ["unassigned-stations"] });
    },
  });

  const TypeIcon = GroupTypeIcons[group.groupType] || Tag;
  const typeColor = GroupTypeColors[group.groupType] || "bg-gray-100 text-gray-700";
  const stats = detail?.stats;

  return (
    <div style={{ marginLeft: level * 24 }}>
      <Card className={`transition-shadow hover:shadow-md ${!group.isActive ? "opacity-60" : ""}`}>
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3 min-w-0">
              <button
                onClick={() => setExpanded(!expanded)}
                className="p-1 hover:bg-muted rounded shrink-0"
              >
                {expanded ? (
                  <ChevronDown className="h-5 w-5" />
                ) : (
                  <ChevronRight className="h-5 w-5" />
                )}
              </button>
              <div className={`p-1.5 rounded ${typeColor}`}>
                <TypeIcon className="h-4 w-4" />
              </div>
              <div className="min-w-0">
                <div className="flex items-center gap-2">
                  <CardTitle className="text-base truncate">{group.name}</CardTitle>
                  {!group.isActive && (
                    <Badge variant="outline" className="text-xs shrink-0">Inactive</Badge>
                  )}
                </div>
                {group.description && (
                  <p className="text-xs text-muted-foreground truncate">{group.description}</p>
                )}
                {group.parentGroupName && (
                  <p className="text-xs text-muted-foreground">
                    <FolderOpen className="inline h-3 w-3 mr-1" />
                    {group.parentGroupName}
                  </p>
                )}
              </div>
            </div>
            <div className="flex items-center gap-2 shrink-0 ml-4">
              <Badge variant="secondary" className="text-xs">
                <TypeIcon className="h-3 w-3 mr-1" />
                {GroupTypeLabels[group.groupType]}
              </Badge>
              {group.region && (
                <Badge variant="outline" className="text-xs">
                  <MapPin className="h-3 w-3 mr-1" />
                  {group.region}
                </Badge>
              )}
              <Badge className="bg-primary/10 text-primary text-xs">
                {group.stationCount} station{group.stationCount !== 1 ? "s" : ""}
              </Badge>
              {group.childGroupCount > 0 && (
                <Badge variant="outline" className="text-xs">
                  <Layers className="h-3 w-3 mr-1" />
                  {group.childGroupCount} sub
                </Badge>
              )}
              <Button variant="outline" size="sm" onClick={() => setShowAssign(!showAssign)}>
                <Plus className="h-4 w-4" />
              </Button>
              <Button variant="outline" size="sm" onClick={() => onEdit(group)}>
                <Edit className="h-4 w-4" />
              </Button>
              <Button
                variant="destructive"
                size="sm"
                onClick={() => {
                  if (confirm(`Delete "${group.name}"? Stations will be unassigned, child groups moved up.`))
                    onDelete(group.id);
                }}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </CardHeader>

        {/* Assign station dropdown */}
        {showAssign && (
          <CardContent className="pt-0 pb-2">
            <div className="border rounded-lg p-3 bg-muted/30">
              <p className="text-sm font-medium mb-2">Add station to this group:</p>
              <div className="flex flex-wrap gap-2">
                {unassignedStations && unassignedStations.length > 0 ? (
                  unassignedStations.map((station) => (
                    <Button
                      key={station.id}
                      variant="outline"
                      size="sm"
                      disabled={assignMutation.isPending}
                      onClick={() => assignMutation.mutate(station.id)}
                    >
                      <MapPin className="mr-1 h-3 w-3" />
                      {station.name}
                    </Button>
                  ))
                ) : (
                  <p className="text-sm text-muted-foreground">No unassigned stations available</p>
                )}
              </div>
            </div>
          </CardContent>
        )}

        {/* Expanded detail */}
        {expanded && (
          <CardContent className="pt-0 space-y-4">
            {detailLoading ? (
              <div className="text-center py-4 text-sm text-muted-foreground">Loading details...</div>
            ) : detail ? (
              <>
                {/* Stats row */}
                {stats && stats.totalStations > 0 && (
                  <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-7 gap-2">
                    <StatCard icon={Building2} label="Stations" value={stats.totalStations} color="bg-slate-50" />
                    <StatCard icon={Zap} label="Connectors" value={stats.totalConnectors} color="bg-slate-50" />
                    <StatCard icon={Zap} label="Available" value={stats.availableConnectors} color="bg-green-50 text-green-700" />
                    <StatCard icon={Zap} label="Occupied" value={stats.occupiedConnectors} color="bg-blue-50 text-blue-700" />
                    <StatCard icon={AlertTriangle} label="Faulted" value={stats.faultedConnectors} color={stats.faultedConnectors > 0 ? "bg-red-50 text-red-700" : "bg-slate-50"} />
                    <StatCard icon={WifiOff} label="Offline" value={stats.offlineStations} color={stats.offlineStations > 0 ? "bg-gray-100 text-gray-700" : "bg-slate-50"} />
                    <StatCard icon={BarChart3} label="Capacity" value={`${stats.totalCapacityKw.toFixed(0)} kW`} color="bg-slate-50" />
                  </div>
                )}

                {/* Child groups */}
                {detail.childGroups && detail.childGroups.length > 0 && (
                  <div>
                    <h4 className="text-sm font-medium mb-2 flex items-center gap-1">
                      <Layers className="h-4 w-4" /> Sub-Groups
                    </h4>
                    <div className="space-y-2">
                      {detail.childGroups.map((child) => (
                        <GroupCard
                          key={child.id}
                          group={child}
                          onEdit={onEdit}
                          onDelete={onDelete}
                          onAssignStation={onAssignStation}
                          level={0}
                        />
                      ))}
                    </div>
                  </div>
                )}

                {/* Stations list */}
                {detail.stations && detail.stations.length > 0 ? (
                  <div>
                    <h4 className="text-sm font-medium mb-2 flex items-center gap-1">
                      <MapPin className="h-4 w-4" /> Stations
                    </h4>
                    <div className="space-y-2">
                      {detail.stations.map((station) => {
                        const statusCfg = StationStatusConfig[station.status] || { label: station.status, color: "bg-gray-100 text-gray-700" };
                        return (
                          <div
                            key={station.stationId}
                            className="flex items-center justify-between rounded-lg border p-3 hover:bg-muted/30 transition-colors"
                          >
                            <div className="flex items-center gap-3 min-w-0">
                              <MapPin className="h-4 w-4 text-muted-foreground shrink-0" />
                              <div className="min-w-0">
                                <p className="font-medium text-sm truncate">{station.name}</p>
                                <p className="text-xs text-muted-foreground truncate">{station.address}</p>
                              </div>
                            </div>
                            <div className="flex items-center gap-2 shrink-0 ml-4">
                              <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${statusCfg.color}`}>
                                {statusCfg.label}
                              </span>
                              <span className="text-xs text-muted-foreground">
                                {station.availableConnectors}/{station.connectorCount} avail
                              </span>
                              <Button
                                variant="ghost"
                                size="sm"
                                className="h-7 w-7 p-0"
                                onClick={() => unassignMutation.mutate(station.stationId)}
                                disabled={unassignMutation.isPending}
                              >
                                <X className="h-3.5 w-3.5" />
                              </Button>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground py-2 text-center">
                    No stations assigned yet. Click + to add stations.
                  </p>
                )}
              </>
            ) : null}
          </CardContent>
        )}
      </Card>
    </div>
  );
}

// --- Main Page ---

export default function StationGroupsPage() {
  const queryClient = useQueryClient();
  const [isCreating, setIsCreating] = useState(false);
  const [editingGroup, setEditingGroup] = useState<StationGroupList | null>(null);
  const [filterType, setFilterType] = useState<number | null>(null);
  const [showTopLevelOnly, setShowTopLevelOnly] = useState(true);

  // Fetch groups
  const { data: groupsData, isLoading } = useQuery({
    queryKey: ["station-groups", filterType, showTopLevelOnly],
    queryFn: async () => {
      const params: Record<string, unknown> = { maxResultCount: 100 };
      if (filterType !== null) params.groupType = filterType;
      if (showTopLevelOnly) params.topLevelOnly = true;
      const res = await stationGroupsApi.getAll(params as Parameters<typeof stationGroupsApi.getAll>[0]);
      return res.data;
    },
  });

  // All groups (for parent selector in form)
  const { data: allGroupsData } = useQuery({
    queryKey: ["station-groups-all"],
    queryFn: async () => {
      const res = await stationGroupsApi.getAll({ maxResultCount: 100 });
      return res.data;
    },
  });

  const groups: StationGroupList[] = groupsData?.items || [];
  const allGroups: StationGroupList[] = allGroupsData?.items || [];

  const createMutation = useMutation({
    mutationFn: async (data: Parameters<typeof stationGroupsApi.create>[0]) => {
      return stationGroupsApi.create(data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-groups"] });
      setIsCreating(false);
    },
  });

  const updateMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string; data: Parameters<typeof stationGroupsApi.update>[1] }) => {
      return stationGroupsApi.update(id, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-groups"] });
      setEditingGroup(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await stationGroupsApi.delete(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-groups"] });
    },
  });

  // Group counts by type
  const typeCounts = allGroups.reduce<Record<number, number>>((acc, g) => {
    acc[g.groupType] = (acc[g.groupType] || 0) + 1;
    return acc;
  }, {});
  const totalStations = allGroups.reduce((sum, g) => sum + g.stationCount, 0);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Station Groups</h1>
          <p className="text-muted-foreground">
            Organize stations by geography, operations, or business units
          </p>
        </div>
        <Button onClick={() => { setIsCreating(true); setEditingGroup(null); }} disabled={isCreating}>
          <Plus className="mr-2 h-4 w-4" />
          Add Group
        </Button>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
        <Card
          className={`cursor-pointer transition-colors ${filterType === null ? "ring-2 ring-primary" : "hover:bg-muted/50"}`}
          onClick={() => setFilterType(null)}
        >
          <CardContent className="p-4 flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10">
              <FolderTree className="h-5 w-5 text-primary" />
            </div>
            <div>
              <p className="text-2xl font-bold">{allGroups.length}</p>
              <p className="text-xs text-muted-foreground">All Groups</p>
            </div>
          </CardContent>
        </Card>
        {Object.entries(GroupTypeLabels).map(([val, label]) => {
          const type = Number(val);
          const Icon = GroupTypeIcons[type];
          const isSelected = filterType === type;
          return (
            <Card
              key={val}
              className={`cursor-pointer transition-colors ${isSelected ? "ring-2 ring-primary" : "hover:bg-muted/50"}`}
              onClick={() => setFilterType(isSelected ? null : type)}
            >
              <CardContent className="p-4 flex items-center gap-3">
                <div className={`p-2 rounded-lg ${GroupTypeColors[type]}`}>
                  <Icon className="h-5 w-5" />
                </div>
                <div>
                  <p className="text-2xl font-bold">{typeCounts[type] || 0}</p>
                  <p className="text-xs text-muted-foreground">{label}</p>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Quick stats bar */}
      <div className="flex items-center gap-4 text-sm text-muted-foreground">
        <span>{totalStations} total stations assigned</span>
        <span className="text-muted-foreground/30">|</span>
        <label className="flex items-center gap-1.5 cursor-pointer">
          <input
            type="checkbox"
            checked={showTopLevelOnly}
            onChange={(e) => setShowTopLevelOnly(e.target.checked)}
            className="rounded"
          />
          Top-level only
        </label>
      </div>

      {/* Create/Edit Form */}
      {isCreating && (
        <GroupForm
          parentGroups={allGroups}
          onSubmit={(data) => createMutation.mutate({ ...data, parentGroupId: data.parentGroupId || undefined })}
          onCancel={() => setIsCreating(false)}
          isLoading={createMutation.isPending}
        />
      )}
      {editingGroup && (
        <GroupForm
          initialData={{
            name: editingGroup.name,
            description: editingGroup.description || "",
            region: editingGroup.region || "",
            groupType: editingGroup.groupType,
            parentGroupId: editingGroup.parentGroupId,
            isActive: editingGroup.isActive,
          }}
          parentGroups={allGroups.filter((g) => g.id !== editingGroup.id)}
          onSubmit={(data) => updateMutation.mutate({ id: editingGroup.id, data })}
          onCancel={() => setEditingGroup(null)}
          isLoading={updateMutation.isPending}
        />
      )}

      {/* Groups List */}
      <div className="space-y-3">
        {isLoading ? (
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-3" />
            <p className="text-muted-foreground">Loading groups...</p>
          </div>
        ) : groups.length > 0 ? (
          groups.map((group) => (
            <GroupCard
              key={group.id}
              group={group}
              onEdit={(g) => { setEditingGroup(g); setIsCreating(false); }}
              onDelete={(id) => deleteMutation.mutate(id)}
              onAssignStation={() => {}}
            />
          ))
        ) : (
          <Card>
            <CardContent className="py-12 text-center">
              <FolderTree className="h-12 w-12 text-muted-foreground/30 mx-auto mb-4" />
              <h3 className="text-lg font-medium mb-1">No groups found</h3>
              <p className="text-sm text-muted-foreground mb-4">
                {filterType !== null
                  ? `No ${GroupTypeLabels[filterType]} groups yet. Create one to get started.`
                  : "Create your first group to organize charging stations."}
              </p>
              <Button onClick={() => setIsCreating(true)}>
                <Plus className="mr-2 h-4 w-4" />
                Create Group
              </Button>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
