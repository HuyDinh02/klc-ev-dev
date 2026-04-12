"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { StatusBadge } from "@/components/ui/status-badge";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard } from "@/components/ui/skeleton";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { api, stationGroupsApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
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

const GROUP_TYPE_KEYS: Record<number, string> = {
  0: "groups.geographic",
  1: "groups.operational",
  2: "groups.business",
  3: "groups.custom",
};

const GroupTypeIcons: Record<number, typeof MapPin> = {
  0: MapPin,
  1: Wrench,
  2: Briefcase,
  3: Tag,
};

const GROUP_TYPE_STYLES: Record<number, { bg: string; text: string; iconColor: string }> = {
  0: { bg: "bg-blue-100",   text: "text-blue-700",   iconColor: "bg-blue-100 text-blue-700" },
  1: { bg: "bg-orange-100", text: "text-orange-700", iconColor: "bg-orange-100 text-orange-700" },
  2: { bg: "bg-green-100",  text: "text-green-700",  iconColor: "bg-green-100 text-green-700" },
  3: { bg: "bg-purple-100", text: "text-purple-700", iconColor: "bg-purple-100 text-purple-700" },
};

/** Map string-based station status to numeric value for StatusBadge */
const STATION_STATUS_NAME_TO_VALUE: Record<string, number> = {
  Offline: 0,
  Online: 1,
  Disabled: 2,
  Decommissioned: 2, // deprecated: map legacy "Decommissioned" to Disabled (2)
};

// --- Components ---

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
  const { t } = useTranslation();
  const [form, setForm] = useState({
    name: initialData?.name || "",
    description: initialData?.description || "",
    region: initialData?.region || "",
    groupType: initialData?.groupType ?? 0,
    parentGroupId: initialData?.parentGroupId || "",
    isActive: initialData?.isActive ?? true,
  });

  return (
    <Dialog open onClose={onCancel} size="xl">
      <DialogHeader onClose={onCancel}>
        {initialData ? t("groups.editGroup") : t("groups.newGroup")}
      </DialogHeader>
      <DialogContent>
        <form
          id="group-form"
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
              <label className="text-sm font-medium">{t("groups.groupName")} *</label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                placeholder={t("groups.groupNamePlaceholder")}
                required
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("groups.region")}</label>
              <input
                type="text"
                value={form.region}
                onChange={(e) => setForm({ ...form, region: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                placeholder={t("groups.regionPlaceholder")}
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("groups.type")}</label>
              <select
                value={form.groupType}
                onChange={(e) => setForm({ ...form, groupType: Number(e.target.value) })}
                className="mt-1 w-full rounded-md border px-3 py-2 text-sm bg-white"
              >
                {Object.entries(GROUP_TYPE_KEYS).map(([val, key]) => (
                  <option key={val} value={val}>{t(key)}</option>
                ))}
              </select>
            </div>
            <div className="md:col-span-2">
              <label className="text-sm font-medium">{t("groups.descriptionLabel")}</label>
              <input
                type="text"
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                placeholder={t("groups.descriptionPlaceholder")}
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("groups.parentGroup")}</label>
              <select
                value={form.parentGroupId}
                onChange={(e) => setForm({ ...form, parentGroupId: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2 text-sm bg-white"
              >
                <option value="">{t("groups.noneTopLevel")}</option>
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
              {t("common.active")}
            </label>
          )}
        </form>
      </DialogContent>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel}>
          {t("common.cancel")}
        </Button>
        <Button type="submit" form="group-form" disabled={isLoading}>
          {initialData ? t("groups.updateGroup") : t("groups.createGroup")}
        </Button>
      </DialogFooter>
    </Dialog>
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
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const canUpdate = useHasPermission("KLC.StationGroups.Update");
  const canDelete = useHasPermission("KLC.StationGroups.Delete");
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
  const typeStyle = GROUP_TYPE_STYLES[group.groupType] || { bg: "bg-muted", text: "text-muted-foreground", iconColor: "bg-muted text-muted-foreground" };
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
              <div className={`p-1.5 rounded ${typeStyle.bg} ${typeStyle.text}`}>
                <TypeIcon className="h-4 w-4" />
              </div>
              <div className="min-w-0">
                <div className="flex items-center gap-2">
                  <CardTitle className="text-base truncate">{group.name}</CardTitle>
                  {!group.isActive && (
                    <Badge variant="outline" className="text-xs shrink-0">{t("common.inactive")}</Badge>
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
                {t(GROUP_TYPE_KEYS[group.groupType])}
              </Badge>
              {group.region && (
                <Badge variant="outline" className="text-xs">
                  <MapPin className="h-3 w-3 mr-1" />
                  {group.region}
                </Badge>
              )}
              <Badge className="bg-primary/10 text-primary text-xs">
                {group.stationCount} {group.stationCount !== 1 ? t("groups.stationPlural") : t("groups.station")}
              </Badge>
              {group.childGroupCount > 0 && (
                <Badge variant="outline" className="text-xs">
                  <Layers className="h-3 w-3 mr-1" />
                  {group.childGroupCount} {t("groups.sub")}
                </Badge>
              )}
              {canUpdate && (
                <Button variant="outline" size="sm" onClick={() => setShowAssign(!showAssign)}>
                  <Plus className="h-4 w-4" />
                </Button>
              )}
              {canUpdate && (
                <Button variant="outline" size="sm" onClick={() => onEdit(group)}>
                  <Edit className="h-4 w-4" />
                </Button>
              )}
              {canDelete && (
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => {
                    if (confirm(t("groups.deleteConfirm").replace("{name}", group.name)))
                      onDelete(group.id);
                  }}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              )}
            </div>
          </div>
        </CardHeader>

        {/* Assign station dropdown */}
        {showAssign && (
          <CardContent className="pt-0 pb-2">
            <div className="border rounded-lg p-3 bg-muted/30">
              <p className="text-sm font-medium mb-2">{t("groups.addStationToGroup")}</p>
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
                  <p className="text-sm text-muted-foreground">{t("groups.noUnassignedStations")}</p>
                )}
              </div>
            </div>
          </CardContent>
        )}

        {/* Expanded detail */}
        {expanded && (
          <CardContent className="pt-0 space-y-4">
            {detailLoading ? (
              <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-7 gap-2">
                {Array.from({ length: 7 }).map((_, i) => (
                  <SkeletonCard key={i} />
                ))}
              </div>
            ) : detail ? (
              <>
                {/* Stats row */}
                {stats && stats.totalStations > 0 && (
                  <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-7 gap-2">
                    <StatCard icon={Building2} label={t("groups.stations")} value={stats.totalStations} iconColor="bg-muted text-muted-foreground" />
                    <StatCard icon={Zap} label={t("groups.connectors")} value={stats.totalConnectors} iconColor="bg-muted text-muted-foreground" />
                    <StatCard icon={Zap} label={t("groups.available")} value={stats.availableConnectors} iconColor="bg-green-100 text-green-700" />
                    <StatCard icon={Zap} label={t("groups.occupied")} value={stats.occupiedConnectors} iconColor="bg-blue-100 text-blue-700" />
                    <StatCard icon={AlertTriangle} label={t("groups.faulted")} value={stats.faultedConnectors} iconColor={stats.faultedConnectors > 0 ? "bg-red-100 text-red-700" : "bg-muted text-muted-foreground"} />
                    <StatCard icon={WifiOff} label={t("groups.offline")} value={stats.offlineStations} iconColor="bg-muted text-muted-foreground" />
                    <StatCard icon={BarChart3} label={t("groups.capacity")} value={`${stats.totalCapacityKw.toFixed(0)} kW`} iconColor="bg-muted text-muted-foreground" />
                  </div>
                )}

                {/* Child groups */}
                {detail.childGroups && detail.childGroups.length > 0 && (
                  <div>
                    <h4 className="text-sm font-medium mb-2 flex items-center gap-1">
                      <Layers className="h-4 w-4" /> {t("groups.subGroups")}
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
                      <MapPin className="h-4 w-4" /> {t("groups.stations")}
                    </h4>
                    <div className="space-y-2">
                      {detail.stations.map((station) => {
                        const statusValue = STATION_STATUS_NAME_TO_VALUE[station.status] ?? -1;
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
                              {statusValue >= 0 ? (
                                <StatusBadge type="station" value={statusValue} className="text-xs" />
                              ) : (
                                <Badge variant="secondary" className="text-xs">{station.status}</Badge>
                              )}
                              <span className="text-xs text-muted-foreground">
                                {station.availableConnectors}/{station.connectorCount} {t("groups.avail")}
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
                  <EmptyState
                    icon={MapPin}
                    title={t("groups.noStationsAssigned")}
                    description={t("groups.noStationsAssignedDesc")}
                    className="py-6"
                  />
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
  const hasAccess = useRequirePermission("KLC.StationGroups");
  const canCreate = useHasPermission("KLC.StationGroups.Create");
  const { t } = useTranslation();
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

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader
          title={t("groups.title")}
          description={t("groups.description")}
        >
          {canCreate && (
            <Button onClick={() => { setIsCreating(true); setEditingGroup(null); }} disabled={isCreating}>
              <Plus className="mr-2 h-4 w-4" />
              {t("groups.addGroup")}
            </Button>
          )}
        </PageHeader>
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* Summary cards */}
        <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
          <StatCard
            label={t("groups.allGroups")}
            value={allGroups.length}
            icon={FolderTree}
            iconColor="bg-primary/10 text-primary"
            onClick={() => setFilterType(null)}
            className={filterType === null ? "ring-2 ring-primary" : ""}
          />
          {Object.entries(GROUP_TYPE_KEYS).map(([val, key]) => {
            const type = Number(val);
            const Icon = GroupTypeIcons[type];
            const style = GROUP_TYPE_STYLES[type];
            const isSelected = filterType === type;
            return (
              <StatCard
                key={val}
                label={t(key)}
                value={typeCounts[type] || 0}
                icon={Icon}
                iconColor={style?.iconColor}
                onClick={() => setFilterType(isSelected ? null : type)}
                className={isSelected ? "ring-2 ring-primary" : ""}
              />
            );
          })}
        </div>

        {/* Quick stats bar */}
        <div className="flex items-center gap-4 text-sm text-muted-foreground">
          <span>{totalStations} {t("groups.totalStationsAssigned")}</span>
          <span className="text-muted-foreground/30">|</span>
          <label className="flex items-center gap-1.5 cursor-pointer">
            <input
              type="checkbox"
              checked={showTopLevelOnly}
              onChange={(e) => setShowTopLevelOnly(e.target.checked)}
              className="rounded"
            />
            {t("groups.topLevelOnly")}
          </label>
        </div>

        {/* Create/Edit Form (Dialog) */}
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
            <div className="space-y-3">
              {Array.from({ length: 4 }).map((_, i) => (
                <SkeletonCard key={i} />
              ))}
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
            <EmptyState
              icon={FolderTree}
              title={t("groups.noGroupsFound")}
              description={
                filterType !== null
                  ? t("groups.noGroupsDesc").replace("{type}", t(GROUP_TYPE_KEYS[filterType]))
                  : t("groups.noGroupsDesc")
              }
              action={{
                label: t("groups.createGroup"),
                onClick: () => setIsCreating(true),
              }}
            />
          )}
        </div>
      </div>
    </div>
  );
}
