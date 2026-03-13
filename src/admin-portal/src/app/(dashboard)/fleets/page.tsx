"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard } from "@/components/ui/skeleton";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { fleetsApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import {
  Plus,
  Trash2,
  Truck,
  Car,
  DollarSign,
  Activity,
  X,
  Pencil,
  Calendar,
  MapPin,
} from "lucide-react";

// --- Types ---

interface FleetListItem {
  id: string;
  name: string;
  description?: string;
  chargingPolicy: number;
  maxMonthlyBudgetVnd: number;
  currentMonthSpentVnd: number;
  budgetAlertThresholdPercent: number;
  vehicleCount: number;
  isActive: boolean;
  creationTime: string;
}

interface FleetVehicle {
  id: string;
  vehicleId: string;
  vehicleName: string;
  licensePlate: string;
  driverName?: string;
  driverUserId?: string;
  dailyChargingLimitKwh?: number;
  currentDayEnergyKwh: number;
  currentMonthEnergyKwh: number;
}

interface FleetSchedule {
  id: string;
  dayOfWeek: number;
  startTimeUtc: string;
  endTimeUtc: string;
}

interface FleetAllowedGroup {
  id: string;
  stationGroupId: string;
  stationGroupName: string;
}

interface FleetDetail {
  id: string;
  name: string;
  description?: string;
  chargingPolicy: number;
  maxMonthlyBudgetVnd: number;
  currentMonthSpentVnd: number;
  budgetAlertThresholdPercent: number;
  isActive: boolean;
  vehicles: FleetVehicle[];
  schedules: FleetSchedule[];
  allowedStationGroups: FleetAllowedGroup[];
  creationTime: string;
}

const POLICY_LABELS: Record<number, string> = {
  0: "policyAnytime",
  1: "policyScheduled",
  2: "policyApproved",
  3: "policyDailyLimit",
};

const DAY_NAMES: Record<number, string> = {
  0: "Sun",
  1: "Mon",
  2: "Tue",
  3: "Wed",
  4: "Thu",
  5: "Fri",
  6: "Sat",
};

function formatVnd(value: number): string {
  return value.toLocaleString("vi-VN") + "\u0111";
}

export default function FleetsPage() {
  const hasAccess = useRequirePermission("KLC.Fleets");
  const canCreate = useHasPermission("KLC.Fleets.Create");
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [selectedFleet, setSelectedFleet] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: "",
    description: "",
    maxMonthlyBudgetVnd: 10000000,
    chargingPolicy: 0,
    budgetAlertThresholdPercent: 80,
  });
  const [editForm, setEditForm] = useState({
    name: "",
    description: "",
    maxMonthlyBudgetVnd: 10000000,
    chargingPolicy: 0,
    budgetAlertThresholdPercent: 80,
  });

  const { data: fleets, isLoading } = useQuery({
    queryKey: ["fleets"],
    queryFn: async () => {
      const res = await fleetsApi.getList({ pageSize: 50 });
      return res.data as FleetListItem[];
    },
  });

  const { data: detail } = useQuery({
    queryKey: ["fleet-detail", selectedFleet],
    queryFn: async () => {
      if (!selectedFleet) return null;
      const res = await fleetsApi.get(selectedFleet);
      return res.data as FleetDetail;
    },
    enabled: !!selectedFleet,
  });

  const createMutation = useMutation({
    mutationFn: () =>
      fleetsApi.create({
        name: form.name,
        description: form.description || undefined,
        maxMonthlyBudgetVnd: form.maxMonthlyBudgetVnd,
        chargingPolicy: form.chargingPolicy,
        budgetAlertThresholdPercent: form.budgetAlertThresholdPercent,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["fleets"] });
      setShowCreate(false);
      setForm({
        name: "",
        description: "",
        maxMonthlyBudgetVnd: 10000000,
        chargingPolicy: 0,
        budgetAlertThresholdPercent: 80,
      });
    },
  });

  const updateMutation = useMutation({
    mutationFn: () => {
      if (!selectedFleet) return Promise.reject();
      return fleetsApi.update(selectedFleet, {
        name: editForm.name,
        description: editForm.description || undefined,
        maxMonthlyBudgetVnd: editForm.maxMonthlyBudgetVnd,
        chargingPolicy: editForm.chargingPolicy,
        budgetAlertThresholdPercent: editForm.budgetAlertThresholdPercent,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["fleets"] });
      queryClient.invalidateQueries({ queryKey: ["fleet-detail"] });
      setShowEdit(false);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => fleetsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["fleets"] });
      setSelectedFleet(null);
    },
  });

  const removeVehicleMutation = useMutation({
    mutationFn: ({ fleetId, vehicleId }: { fleetId: string; vehicleId: string }) =>
      fleetsApi.removeVehicle(fleetId, vehicleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["fleet-detail"] });
      queryClient.invalidateQueries({ queryKey: ["fleets"] });
    },
  });

  const removeScheduleMutation = useMutation({
    mutationFn: ({ fleetId, scheduleId }: { fleetId: string; scheduleId: string }) =>
      fleetsApi.removeSchedule(fleetId, scheduleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["fleet-detail"] });
    },
  });

  const removeAllowedGroupMutation = useMutation({
    mutationFn: ({ fleetId, stationGroupId }: { fleetId: string; stationGroupId: string }) =>
      fleetsApi.removeAllowedStationGroup(fleetId, stationGroupId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["fleet-detail"] });
    },
  });

  const totalFleets = fleets?.length ?? 0;
  const activeFleets = fleets?.filter((f) => f.isActive).length ?? 0;
  const totalVehicles = fleets?.reduce((sum, f) => sum + f.vehicleCount, 0) ?? 0;
  const totalSpending = fleets?.reduce((sum, f) => sum + f.currentMonthSpentVnd, 0) ?? 0;

  const openEdit = () => {
    if (detail) {
      setEditForm({
        name: detail.name,
        description: detail.description ?? "",
        maxMonthlyBudgetVnd: detail.maxMonthlyBudgetVnd,
        chargingPolicy: detail.chargingPolicy,
        budgetAlertThresholdPercent: detail.budgetAlertThresholdPercent,
      });
      setShowEdit(true);
    }
  };

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="space-y-6">
      <PageHeader
        title={t("fleets.title")}
        description={t("fleets.description")}
      >
        {canCreate && (
          <Button onClick={() => setShowCreate(true)} aria-label={t("fleets.createFleet")}>
            <Plus className="mr-2 h-4 w-4" /> {t("fleets.createFleet")}
          </Button>
        )}
      </PageHeader>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-4">
        <StatCard label={t("fleets.title")} value={totalFleets} icon={Truck} />
        <StatCard label={t("fleets.active")} value={activeFleets} icon={Activity} />
        <StatCard label={t("fleets.vehicleCount")} value={totalVehicles} icon={Car} />
        <StatCard label={t("fleets.totalSpending")} value={formatVnd(totalSpending)} icon={DollarSign} />
      </div>

      {/* Fleets list */}
      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3].map((i) => (
            <SkeletonCard key={i} />
          ))}
        </div>
      ) : !fleets?.length ? (
        <EmptyState
          icon={Truck}
          title={t("fleets.noFleets")}
          description={t("fleets.noFleetsDesc")}
          action={{ label: t("fleets.createFleet"), onClick: () => setShowCreate(true) }}
        />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {fleets.map((fleet) => {
            const utilization =
              fleet.maxMonthlyBudgetVnd > 0
                ? (fleet.currentMonthSpentVnd / fleet.maxMonthlyBudgetVnd) * 100
                : 0;
            return (
              <Card
                key={fleet.id}
                className={`cursor-pointer transition-shadow hover:shadow-md ${selectedFleet === fleet.id ? "ring-2 ring-primary" : ""}`}
                onClick={() => setSelectedFleet(fleet.id)}
              >
                <CardHeader className="pb-2">
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-lg">{fleet.name}</CardTitle>
                    <div className="flex gap-1">
                      <Badge variant="outline">
                        {t(`fleets.${POLICY_LABELS[fleet.chargingPolicy]}`)}
                      </Badge>
                      <Badge variant={fleet.isActive ? "default" : "secondary"}>
                        {fleet.isActive ? t("fleets.active") : t("fleets.inactive")}
                      </Badge>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 gap-2 text-sm mb-3">
                    <div>
                      <p className="text-muted-foreground">{t("fleets.vehicles")}</p>
                      <p className="font-medium">{fleet.vehicleCount}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground">{t("fleets.budget")}</p>
                      <p className="font-medium">{formatVnd(fleet.maxMonthlyBudgetVnd)}</p>
                    </div>
                  </div>
                  {/* Budget utilization bar */}
                  <div>
                    <div className="flex justify-between text-xs text-muted-foreground mb-1">
                      <span>{t("fleets.budgetUtilization")}</span>
                      <span>
                        {formatVnd(fleet.currentMonthSpentVnd)} / {formatVnd(fleet.maxMonthlyBudgetVnd)}
                      </span>
                    </div>
                    <div className="h-2 bg-muted rounded-full overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all duration-500 ${
                          utilization > 90
                            ? "bg-red-500"
                            : utilization > 70
                              ? "bg-yellow-500"
                              : "bg-primary"
                        }`}
                        style={{ width: `${Math.min(utilization, 100)}%` }}
                      />
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}

      {/* Detail panel */}
      {detail && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                {detail.name}
                <Badge variant="outline">
                  {t(`fleets.${POLICY_LABELS[detail.chargingPolicy]}`)}
                </Badge>
                <Badge variant={detail.isActive ? "default" : "secondary"}>
                  {detail.isActive ? t("fleets.active") : t("fleets.inactive")}
                </Badge>
              </CardTitle>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={openEdit}
                  aria-label={t("common.edit")}
                >
                  <Pencil className="mr-1 h-4 w-4" />
                  {t("common.edit")}
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => {
                    if (confirm(t("common.confirm"))) deleteMutation.mutate(detail.id);
                  }}
                  aria-label={t("common.delete")}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {/* Fleet info */}
            <div className="grid grid-cols-2 md:grid-cols-5 gap-4 mb-6 text-sm">
              <div>
                <p className="text-muted-foreground">{t("fleets.description_label")}</p>
                <p className="font-semibold">{detail.description || t("common.na")}</p>
              </div>
              <div>
                <p className="text-muted-foreground">{t("fleets.policy")}</p>
                <p className="font-semibold">
                  {t(`fleets.${POLICY_LABELS[detail.chargingPolicy]}`)}
                </p>
              </div>
              <div>
                <p className="text-muted-foreground">{t("fleets.budget")}</p>
                <p className="font-semibold">{formatVnd(detail.maxMonthlyBudgetVnd)}</p>
              </div>
              <div>
                <p className="text-muted-foreground">{t("fleets.currentSpent")}</p>
                <p className="font-semibold">{formatVnd(detail.currentMonthSpentVnd)}</p>
              </div>
              <div>
                <p className="text-muted-foreground">{t("fleets.alertThreshold")}</p>
                <p className="font-semibold">{detail.budgetAlertThresholdPercent}%</p>
              </div>
            </div>

            {/* Vehicles table */}
            <h3 className="text-sm font-semibold mb-2">{t("fleets.vehicles")}</h3>
            {detail.vehicles.length > 0 ? (
              <div className="overflow-x-auto mb-6">
                <table className="w-full text-sm" role="table">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-2" scope="col">
                        {t("fleets.vehicles")}
                      </th>
                      <th className="text-left py-2" scope="col">
                        {t("fleets.driver")}
                      </th>
                      <th className="text-right py-2" scope="col">
                        {t("fleets.dailyLimit")}
                      </th>
                      <th className="text-right py-2" scope="col">
                        {t("fleets.currentDayEnergy")}
                      </th>
                      <th className="text-right py-2" scope="col">
                        {t("fleets.currentMonthEnergy")}
                      </th>
                      <th className="text-right py-2" scope="col"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.vehicles.map((vehicle) => (
                      <tr key={vehicle.id} className="border-b last:border-0">
                        <td className="py-2">
                          <div>
                            <p className="font-medium">{vehicle.vehicleName}</p>
                            <p className="text-xs text-muted-foreground">
                              {vehicle.licensePlate}
                            </p>
                          </div>
                        </td>
                        <td className="py-2">{vehicle.driverName || t("common.na")}</td>
                        <td className="py-2 text-right">
                          {vehicle.dailyChargingLimitKwh
                            ? `${vehicle.dailyChargingLimitKwh} kWh`
                            : t("common.na")}
                        </td>
                        <td className="py-2 text-right">
                          {vehicle.currentDayEnergyKwh.toFixed(1)} kWh
                        </td>
                        <td className="py-2 text-right">
                          {vehicle.currentMonthEnergyKwh.toFixed(1)} kWh
                        </td>
                        <td className="py-2 text-right">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() =>
                              removeVehicleMutation.mutate({
                                fleetId: detail.id,
                                vehicleId: vehicle.vehicleId,
                              })
                            }
                            aria-label={t("fleets.removeVehicle")}
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="text-muted-foreground text-center py-4 mb-6">
                {t("fleets.noFleetsDesc")}
              </p>
            )}

            {/* Charging schedules (for ScheduledOnly policy) */}
            {detail.chargingPolicy === 1 && (
              <>
                <h3 className="text-sm font-semibold mb-2 flex items-center gap-2">
                  <Calendar className="h-4 w-4" />
                  {t("fleets.schedules")}
                </h3>
                {detail.schedules.length > 0 ? (
                  <div className="overflow-x-auto mb-6">
                    <table className="w-full text-sm" role="table">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-2" scope="col">
                            {t("fleets.day")}
                          </th>
                          <th className="text-left py-2" scope="col">
                            {t("fleets.start")}
                          </th>
                          <th className="text-left py-2" scope="col">
                            {t("fleets.end")}
                          </th>
                          <th className="text-right py-2" scope="col"></th>
                        </tr>
                      </thead>
                      <tbody>
                        {detail.schedules.map((schedule) => (
                          <tr key={schedule.id} className="border-b last:border-0">
                            <td className="py-2">{DAY_NAMES[schedule.dayOfWeek]}</td>
                            <td className="py-2">{schedule.startTimeUtc}</td>
                            <td className="py-2">{schedule.endTimeUtc}</td>
                            <td className="py-2 text-right">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() =>
                                  removeScheduleMutation.mutate({
                                    fleetId: detail.id,
                                    scheduleId: schedule.id,
                                  })
                                }
                                aria-label={t("common.delete")}
                              >
                                <X className="h-4 w-4" />
                              </Button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <p className="text-muted-foreground text-center py-4 mb-6">
                    {t("fleets.noFleetsDesc")}
                  </p>
                )}
              </>
            )}

            {/* Allowed station groups (for ApprovedStationsOnly policy) */}
            {detail.chargingPolicy === 2 && (
              <>
                <h3 className="text-sm font-semibold mb-2 flex items-center gap-2">
                  <MapPin className="h-4 w-4" />
                  {t("fleets.allowedGroups")}
                </h3>
                {detail.allowedStationGroups.length > 0 ? (
                  <div className="flex flex-wrap gap-2 mb-6">
                    {detail.allowedStationGroups.map((group) => (
                      <Badge key={group.id} variant="outline" className="flex items-center gap-1 px-3 py-1">
                        {group.stationGroupName}
                        <button
                          onClick={() =>
                            removeAllowedGroupMutation.mutate({
                              fleetId: detail.id,
                              stationGroupId: group.stationGroupId,
                            })
                          }
                          className="ml-1 hover:text-destructive"
                          aria-label={t("common.delete")}
                        >
                          <X className="h-3 w-3" />
                        </button>
                      </Badge>
                    ))}
                  </div>
                ) : (
                  <p className="text-muted-foreground text-center py-4 mb-6">
                    {t("fleets.noFleetsDesc")}
                  </p>
                )}
              </>
            )}
          </CardContent>
        </Card>
      )}

      {/* Create Dialog */}
      <Dialog open={showCreate} onClose={() => setShowCreate(false)} title={t("fleets.createFleet")}>
        <DialogHeader onClose={() => setShowCreate(false)}>
          {t("fleets.createFleet")}
        </DialogHeader>
        <DialogContent>
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium">{t("fleets.name")}</label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
                placeholder={t("fleets.namePlaceholder")}
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("fleets.description_label")}</label>
              <textarea
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
                rows={3}
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("fleets.budget")}</label>
              <input
                type="number"
                min={0}
                value={form.maxMonthlyBudgetVnd}
                onChange={(e) =>
                  setForm({ ...form, maxMonthlyBudgetVnd: parseInt(e.target.value) || 0 })
                }
                className="mt-1 w-full rounded-md border px-3 py-2"
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("fleets.policy")}</label>
              <select
                value={form.chargingPolicy}
                onChange={(e) => setForm({ ...form, chargingPolicy: parseInt(e.target.value) })}
                className="mt-1 w-full rounded-md border px-3 py-2"
              >
                <option value={0}>{t("fleets.policyAnytime")}</option>
                <option value={1}>{t("fleets.policyScheduled")}</option>
                <option value={2}>{t("fleets.policyApproved")}</option>
                <option value={3}>{t("fleets.policyDailyLimit")}</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium">{t("fleets.alertThreshold")}</label>
              <input
                type="number"
                min={0}
                max={100}
                value={form.budgetAlertThresholdPercent}
                onChange={(e) =>
                  setForm({
                    ...form,
                    budgetAlertThresholdPercent: parseInt(e.target.value) || 80,
                  })
                }
                className="mt-1 w-full rounded-md border px-3 py-2"
              />
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowCreate(false)}>
            {t("common.cancel")}
          </Button>
          <Button
            onClick={() => createMutation.mutate()}
            disabled={!form.name || form.maxMonthlyBudgetVnd <= 0 || createMutation.isPending}
          >
            {t("common.create")}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={showEdit} onClose={() => setShowEdit(false)} title={t("common.edit")}>
        <DialogHeader onClose={() => setShowEdit(false)}>{t("common.edit")}</DialogHeader>
        <DialogContent>
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium">{t("fleets.name")}</label>
              <input
                type="text"
                value={editForm.name}
                onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("fleets.description_label")}</label>
              <textarea
                value={editForm.description}
                onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
                rows={3}
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("fleets.budget")}</label>
              <input
                type="number"
                min={0}
                value={editForm.maxMonthlyBudgetVnd}
                onChange={(e) =>
                  setEditForm({ ...editForm, maxMonthlyBudgetVnd: parseInt(e.target.value) || 0 })
                }
                className="mt-1 w-full rounded-md border px-3 py-2"
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("fleets.policy")}</label>
              <select
                value={editForm.chargingPolicy}
                onChange={(e) =>
                  setEditForm({ ...editForm, chargingPolicy: parseInt(e.target.value) })
                }
                className="mt-1 w-full rounded-md border px-3 py-2"
              >
                <option value={0}>{t("fleets.policyAnytime")}</option>
                <option value={1}>{t("fleets.policyScheduled")}</option>
                <option value={2}>{t("fleets.policyApproved")}</option>
                <option value={3}>{t("fleets.policyDailyLimit")}</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium">{t("fleets.alertThreshold")}</label>
              <input
                type="number"
                min={0}
                max={100}
                value={editForm.budgetAlertThresholdPercent}
                onChange={(e) =>
                  setEditForm({
                    ...editForm,
                    budgetAlertThresholdPercent: parseInt(e.target.value) || 80,
                  })
                }
                className="mt-1 w-full rounded-md border px-3 py-2"
              />
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowEdit(false)}>
            {t("common.cancel")}
          </Button>
          <Button
            onClick={() => updateMutation.mutate()}
            disabled={
              !editForm.name || editForm.maxMonthlyBudgetVnd <= 0 || updateMutation.isPending
            }
          >
            {t("common.save")}
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  );
}
