"use client";

import { useState, useEffect, useCallback } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard } from "@/components/ui/skeleton";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { powerSharingApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useMonitoringHub, PowerAllocationUpdate } from "@/lib/signalr";
import {
  Plus,
  Trash2,
  Cable,
  Zap,
  Power,
  Activity,
  X,
  RefreshCw,
  Wifi,
  WifiOff,
} from "lucide-react";

// --- Types ---

interface PowerSharingGroupList {
  id: string;
  name: string;
  maxCapacityKw: number;
  mode: number;
  distributionStrategy: number;
  isActive: boolean;
  memberCount: number;
  totalAllocatedKw: number;
  creationTime: string;
}

interface PowerSharingMember {
  id: string;
  stationId: string;
  connectorId: string;
  priority: number;
  allocatedPowerKw: number;
  stationName: string;
  connectorNumber: number;
  connectorType: string;
  maxPowerKw: number;
}

interface PowerSharingGroupDetail {
  id: string;
  name: string;
  maxCapacityKw: number;
  mode: number;
  distributionStrategy: number;
  isActive: boolean;
  minPowerPerConnectorKw: number;
  stationGroupId: string | null;
  members: PowerSharingMember[];
}

const MODE_LABELS: Record<number, string> = { 0: "LINK", 1: "LOOP" };
const STRATEGY_LABELS: Record<number, string> = { 0: "Average", 1: "Proportional", 2: "Dynamic" };

export default function PowerSharingPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [selectedGroup, setSelectedGroup] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", maxCapacityKw: 150, mode: 0, distributionStrategy: 0, minPowerPerConnectorKw: 1.4 });
  const [liveAllocations, setLiveAllocations] = useState<Record<string, PowerAllocationUpdate>>({});

  const onPowerAllocationChanged = useCallback((update: PowerAllocationUpdate) => {
    setLiveAllocations(prev => ({ ...prev, [update.groupId]: update }));
  }, []);

  const { status: signalRStatus, subscribeToPowerSharingGroup, unsubscribeFromPowerSharingGroup } = useMonitoringHub({
    onPowerAllocationChanged,
  });

  // Subscribe to the selected group for detailed updates
  useEffect(() => {
    if (selectedGroup && signalRStatus === "connected") {
      subscribeToPowerSharingGroup(selectedGroup);
      return () => { unsubscribeFromPowerSharingGroup(selectedGroup); };
    }
  }, [selectedGroup, signalRStatus, subscribeToPowerSharingGroup, unsubscribeFromPowerSharingGroup]);

  const { data: groups, isLoading } = useQuery({
    queryKey: ["power-sharing-groups"],
    queryFn: async () => {
      const res = await powerSharingApi.getList({ pageSize: 50 });
      return res.data as PowerSharingGroupList[];
    },
  });

  const { data: detail } = useQuery({
    queryKey: ["power-sharing-detail", selectedGroup],
    queryFn: async () => {
      if (!selectedGroup) return null;
      const res = await powerSharingApi.get(selectedGroup);
      return res.data as PowerSharingGroupDetail;
    },
    enabled: !!selectedGroup,
  });

  const createMutation = useMutation({
    mutationFn: () => powerSharingApi.create(form),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["power-sharing-groups"] });
      setShowCreate(false);
      setForm({ name: "", maxCapacityKw: 150, mode: 0, distributionStrategy: 0, minPowerPerConnectorKw: 1.4 });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => powerSharingApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["power-sharing-groups"] });
      setSelectedGroup(null);
    },
  });

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      isActive ? powerSharingApi.deactivate(id) : powerSharingApi.activate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["power-sharing-groups"] });
      queryClient.invalidateQueries({ queryKey: ["power-sharing-detail"] });
    },
  });

  const recalculateMutation = useMutation({
    mutationFn: (id: string) => powerSharingApi.recalculate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["power-sharing-detail"] });
    },
  });

  const removeMemberMutation = useMutation({
    mutationFn: ({ groupId, connectorId }: { groupId: string; connectorId: string }) =>
      powerSharingApi.removeMember(groupId, connectorId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["power-sharing-detail"] });
      queryClient.invalidateQueries({ queryKey: ["power-sharing-groups"] });
    },
  });

  const totalGroups = groups?.length ?? 0;
  const activeGroups = groups?.filter(g => g.isActive).length ?? 0;
  const totalMembers = groups?.reduce((sum, g) => sum + g.memberCount, 0) ?? 0;
  // Prefer live allocation data when available
  const totalAllocated = groups?.reduce((sum, g) => {
    const live = liveAllocations[g.id];
    return sum + (live ? live.totalAllocatedKw : g.totalAllocatedKw);
  }, 0) ?? 0;

  return (
    <div className="space-y-6">
      <PageHeader
        title={t("powerSharing.title")}
        description={t("powerSharing.description")}
      >
        <div className="flex items-center gap-2">
          <span className="flex items-center gap-1 text-xs text-muted-foreground" aria-label={`SignalR ${signalRStatus}`}>
            {signalRStatus === "connected" ? (
              <Wifi className="h-3 w-3 text-green-500" />
            ) : (
              <WifiOff className="h-3 w-3 text-muted-foreground" />
            )}
            {signalRStatus === "connected" ? t("powerSharing.live") : ""}
          </span>
          <Button onClick={() => setShowCreate(true)} aria-label={t("powerSharing.createGroup")}>
            <Plus className="mr-2 h-4 w-4" /> {t("powerSharing.createGroup")}
          </Button>
        </div>
      </PageHeader>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-4">
        <StatCard label={t("powerSharing.title")} value={totalGroups} icon={Cable} />
        <StatCard label={t("powerSharing.active")} value={activeGroups} icon={Power} />
        <StatCard label={t("powerSharing.memberCount")} value={totalMembers} icon={Zap} />
        <StatCard label={t("powerSharing.totalAllocated")} value={`${totalAllocated.toFixed(1)} kW`} icon={Activity} />
      </div>

      {/* Groups list */}
      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3].map(i => <SkeletonCard key={i} />)}
        </div>
      ) : !groups?.length ? (
        <EmptyState
          icon={Cable}
          title={t("powerSharing.noGroups")}
          description={t("powerSharing.noGroupsDesc")}
          action={{ label: t("powerSharing.createGroup"), onClick: () => setShowCreate(true) }}
        />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {groups.map(group => (
            <Card
              key={group.id}
              className={`cursor-pointer transition-shadow hover:shadow-md ${selectedGroup === group.id ? "ring-2 ring-primary" : ""}`}
              onClick={() => setSelectedGroup(group.id)}
            >
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-lg">{group.name}</CardTitle>
                  <div className="flex gap-1">
                    <Badge variant={group.mode === 0 ? "default" : "secondary"}>
                      {MODE_LABELS[group.mode]}
                    </Badge>
                    <Badge variant={group.isActive ? "default" : "secondary"}>
                      {group.isActive ? t("powerSharing.active") : t("powerSharing.inactive")}
                    </Badge>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-3 gap-2 text-sm">
                  <div>
                    <p className="text-muted-foreground">{t("powerSharing.maxCapacity")}</p>
                    <p className="font-medium">{group.maxCapacityKw} kW</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground">{t("powerSharing.memberCount")}</p>
                    <p className="font-medium">{group.memberCount}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground">{t("powerSharing.strategy")}</p>
                    <p className="font-medium">{STRATEGY_LABELS[group.distributionStrategy]}</p>
                  </div>
                </div>
                {(() => {
                  const live = liveAllocations[group.id];
                  const allocated = live ? live.totalAllocatedKw : group.totalAllocatedKw;
                  return allocated > 0 ? (
                    <div className="mt-3">
                      <div className="flex justify-between text-xs text-muted-foreground mb-1">
                        <span className="flex items-center gap-1">
                          {t("powerSharing.allocatedPower")}
                          {live && <span className="inline-block h-1.5 w-1.5 rounded-full bg-green-500 animate-pulse" />}
                        </span>
                        <span>{allocated.toFixed(1)} / {group.maxCapacityKw} kW</span>
                      </div>
                      <div className="h-2 bg-muted rounded-full overflow-hidden">
                        <div
                          className="h-full bg-primary rounded-full transition-all duration-500"
                          style={{ width: `${Math.min((allocated / group.maxCapacityKw) * 100, 100)}%` }}
                        />
                      </div>
                    </div>
                  ) : null;
                })()}
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Detail panel */}
      {detail && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                {detail.name}
                <Badge variant={detail.mode === 0 ? "default" : "secondary"}>{MODE_LABELS[detail.mode]}</Badge>
              </CardTitle>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => recalculateMutation.mutate(detail.id)}
                  disabled={recalculateMutation.isPending}
                  aria-label={t("powerSharing.recalculate")}
                >
                  <RefreshCw className={`mr-1 h-4 w-4 ${recalculateMutation.isPending ? "animate-spin" : ""}`} />
                  {t("powerSharing.recalculate")}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => toggleActiveMutation.mutate({ id: detail.id, isActive: detail.isActive })}
                  aria-label={detail.isActive ? t("powerSharing.deactivate") : t("powerSharing.activate")}
                >
                  {detail.isActive ? t("powerSharing.deactivate") : t("powerSharing.activate")}
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => { if (confirm(t("common.confirm"))) deleteMutation.mutate(detail.id); }}
                  aria-label={t("common.delete")}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-4 gap-4 mb-4 text-sm">
              <div>
                <p className="text-muted-foreground">{t("powerSharing.maxCapacity")}</p>
                <p className="font-semibold">{detail.maxCapacityKw} kW</p>
              </div>
              <div>
                <p className="text-muted-foreground">{t("powerSharing.strategy")}</p>
                <p className="font-semibold">{STRATEGY_LABELS[detail.distributionStrategy]}</p>
              </div>
              <div>
                <p className="text-muted-foreground">{t("powerSharing.minPower")}</p>
                <p className="font-semibold">{detail.minPowerPerConnectorKw} kW</p>
              </div>
              <div>
                <p className="text-muted-foreground">{t("powerSharing.memberCount")}</p>
                <p className="font-semibold">{detail.members.length}</p>
              </div>
            </div>

            {/* Members table */}
            {detail.members.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-sm" role="table">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-2" scope="col">Station</th>
                      <th className="text-left py-2" scope="col">Connector</th>
                      <th className="text-left py-2" scope="col">Type</th>
                      <th className="text-right py-2" scope="col">Max (kW)</th>
                      <th className="text-right py-2" scope="col">{t("powerSharing.allocatedPower")}</th>
                      <th className="text-right py-2" scope="col">{t("powerSharing.priority")}</th>
                      <th className="text-right py-2" scope="col"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.members.map(member => {
                      const liveGroup = liveAllocations[detail.id];
                      const liveAlloc = liveGroup?.allocations.find(a => a.connectorId === member.connectorId);
                      const allocated = liveAlloc ? liveAlloc.allocatedPowerKw : member.allocatedPowerKw;
                      return (
                        <tr key={member.id} className="border-b last:border-0">
                          <td className="py-2">{member.stationName || member.stationId.slice(0, 8)}</td>
                          <td className="py-2">#{member.connectorNumber}</td>
                          <td className="py-2"><Badge variant="outline">{member.connectorType}</Badge></td>
                          <td className="py-2 text-right">{member.maxPowerKw}</td>
                          <td className="py-2 text-right font-medium">
                            <span className={liveAlloc ? "text-green-600" : ""}>
                              {allocated.toFixed(1)} kW
                            </span>
                            {liveAlloc && <span className="inline-block ml-1 h-1.5 w-1.5 rounded-full bg-green-500 animate-pulse" />}
                          </td>
                          <td className="py-2 text-right">{member.priority}</td>
                          <td className="py-2 text-right">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => removeMemberMutation.mutate({ groupId: detail.id, connectorId: member.connectorId })}
                              aria-label={t("powerSharing.removeMember")}
                            >
                              <X className="h-4 w-4" />
                            </Button>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="text-muted-foreground text-center py-4">{t("powerSharing.noGroupsDesc")}</p>
            )}
          </CardContent>
        </Card>
      )}

      {/* Create Dialog */}
      <Dialog open={showCreate} onClose={() => setShowCreate(false)}>
        <DialogHeader>{t("powerSharing.createGroup")}</DialogHeader>
        <DialogContent>
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium">{t("powerSharing.groupName")}</label>
              <input
                type="text"
                value={form.name}
                onChange={e => setForm({ ...form, name: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
                placeholder="Site A Power Pool"
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("powerSharing.maxCapacity")}</label>
              <input
                type="number"
                min={1}
                value={form.maxCapacityKw}
                onChange={e => setForm({ ...form, maxCapacityKw: parseFloat(e.target.value) || 0 })}
                className="mt-1 w-full rounded-md border px-3 py-2"
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("powerSharing.mode")}</label>
              <select
                value={form.mode}
                onChange={e => setForm({ ...form, mode: parseInt(e.target.value) })}
                className="mt-1 w-full rounded-md border px-3 py-2"
              >
                <option value={0}>{t("powerSharing.modeLink")} — {t("powerSharing.linkDesc")}</option>
                <option value={1}>{t("powerSharing.modeLoop")} — {t("powerSharing.loopDesc")}</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium">{t("powerSharing.strategy")}</label>
              <select
                value={form.distributionStrategy}
                onChange={e => setForm({ ...form, distributionStrategy: parseInt(e.target.value) })}
                className="mt-1 w-full rounded-md border px-3 py-2"
              >
                <option value={0}>{t("powerSharing.strategyAverage")}</option>
                <option value={1}>{t("powerSharing.strategyProportional")}</option>
                <option value={2}>{t("powerSharing.strategyDynamic")}</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium">{t("powerSharing.minPower")}</label>
              <input
                type="number"
                min={0}
                step={0.1}
                value={form.minPowerPerConnectorKw}
                onChange={e => setForm({ ...form, minPowerPerConnectorKw: parseFloat(e.target.value) || 0 })}
                className="mt-1 w-full rounded-md border px-3 py-2"
              />
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowCreate(false)}>{t("common.cancel")}</Button>
          <Button
            onClick={() => createMutation.mutate()}
            disabled={!form.name || form.maxCapacityKw <= 0 || createMutation.isPending}
          >
            {t("common.create")}
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  );
}
