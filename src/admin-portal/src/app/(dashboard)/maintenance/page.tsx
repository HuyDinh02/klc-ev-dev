"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTableQuery } from "@/hooks/use-table-query";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { StatusBadge } from "@/components/ui/status-badge";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogHeader,
  DialogContent,
  DialogFooter,
} from "@/components/ui/dialog";
import { api, maintenanceApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import {
  Wrench,
  Plus,
  Calendar,
  MapPin,
  Clock,
  User,
  CheckCircle2,
  AlertTriangle,
  Play,
  X,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";

interface MaintenanceTask {
  id: string;
  stationId: string;
  stationName: string;
  connectorNumber?: number;
  type: number;
  status: number;
  title: string;
  description: string;
  assignedTo: string;
  scheduledDate: string;
  startedAt?: string;
  completedAt?: string;
  notes?: string;
  creationTime: string;
}

interface MaintenanceStats {
  plannedCount: number;
  inProgressCount: number;
  completedCount: number;
  overdueCount: number;
}

export default function MaintenancePage() {
  const hasAccess = useRequirePermission("KLC.Maintenance");
  const canCreate = useHasPermission("KLC.Maintenance.Create");
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const TypeLabels: Record<number, string> = {
    0: t("maintenance.scheduled"),
    1: t("maintenance.inspection"),
    2: t("maintenance.emergency"),
  };
  const [typeFilter, setTypeFilter] = useState("all");
  const [isCreating, setIsCreating] = useState(false);
  const [formData, setFormData] = useState({
    stationId: "",
    connectorId: "",
    type: "0",
    title: "",
    description: "",
    assignedTo: "",
    scheduledDate: "",
  });

  const {
    items: tasks,
    totalCount,
    isLoading,
    statusFilter,
    setStatusFilter,
    setStatusFilterAndReset,
    pageSize,
    goNextPage,
    goPrevPage,
    hasNextPage,
    hasPrevPage,
    resetPage,
  } = useTableQuery<MaintenanceTask>({
    queryKey: "maintenance-tasks",
    fetchFn: async (params) => {
      const p = { ...params } as Record<string, unknown>;
      if (typeFilter !== "all") p.type = typeFilter;
      const res = await maintenanceApi.getAll(p as Parameters<typeof maintenanceApi.getAll>[0]);
      return res.data;
    },
    extraQueryKeys: [typeFilter],
  });

  // Fetch stations for dropdown
  const { data: stations } = useQuery({
    queryKey: ["stations-list"],
    queryFn: async () => {
      const res = await api.get("/stations", {
        params: { maxResultCount: 100 },
      });
      return res.data.items || [];
    },
    enabled: isCreating,
  });

  // Fetch stats
  const { data: stats } = useQuery<MaintenanceStats>({
    queryKey: ["maintenance-stats"],
    queryFn: async () => {
      const res = await maintenanceApi.getStats();
      return res.data;
    },
  });

  // Create task
  const createMutation = useMutation({
    mutationFn: async () => {
      await maintenanceApi.create({
        stationId: formData.stationId,
        type: parseInt(formData.type),
        title: formData.title,
        description: formData.description || undefined,
        assignedTo: formData.assignedTo,
        scheduledDate: new Date(formData.scheduledDate).toISOString(),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
      queryClient.invalidateQueries({ queryKey: ["maintenance-stats"] });
      setIsCreating(false);
      setFormData({
        stationId: "",
        connectorId: "",
        type: "0",
        title: "",
        description: "",
        assignedTo: "",
        scheduledDate: "",
      });
    },
  });

  // Start task
  const startMutation = useMutation({
    mutationFn: async (id: string) => {
      await maintenanceApi.start(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
      queryClient.invalidateQueries({ queryKey: ["maintenance-stats"] });
    },
  });

  // Complete task
  const completeMutation = useMutation({
    mutationFn: async (id: string) => {
      await maintenanceApi.complete(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
      queryClient.invalidateQueries({ queryKey: ["maintenance-stats"] });
    },
  });

  // Cancel task
  const cancelMutation = useMutation({
    mutationFn: async (id: string) => {
      await maintenanceApi.cancel(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
      queryClient.invalidateQueries({ queryKey: ["maintenance-stats"] });
    },
  });

  const getTypeColor = (type: number): "destructive" | "warning" | "default" | "secondary" => {
    switch (type) {
      case 2: return "destructive";
      case 1: return "warning";
      case 0: return "default";
      default: return "secondary";
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("vi-VN");
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate();
  };

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title={t("maintenance.title")} description={t("maintenance.description")}>
          {canCreate && (
            <Button onClick={() => setIsCreating(true)} disabled={isCreating}>
              <Plus className="mr-2 h-4 w-4" aria-hidden="true" />
              {t("maintenance.newTask")}
            </Button>
          )}
        </PageHeader>
      </div>

      <div className="px-6 space-y-6">
        {/* Stats Cards */}
        <div className="grid gap-4 md:grid-cols-4">
          <StatCard
            label={t("maintenance.planned")}
            value={stats?.plannedCount || 0}
            icon={Calendar}
            iconColor="bg-primary/10 text-primary"
            className={statusFilter === "0" ? "ring-2 ring-primary" : ""}
            onClick={() => setStatusFilterAndReset(statusFilter === "0" ? "all" : "0")}
          />
          <StatCard
            label={t("maintenance.inProgress")}
            value={stats?.inProgressCount || 0}
            icon={Wrench}
            iconColor="bg-blue-50 text-blue-600"
            className={statusFilter === "1" ? "ring-2 ring-blue-500" : ""}
            onClick={() => setStatusFilterAndReset(statusFilter === "1" ? "all" : "1")}
          />
          <StatCard
            label={t("maintenance.completed")}
            value={stats?.completedCount || 0}
            icon={CheckCircle2}
            iconColor="bg-green-50 text-green-600"
            className={statusFilter === "2" ? "ring-2 ring-green-500" : ""}
            onClick={() => setStatusFilterAndReset(statusFilter === "2" ? "all" : "2")}
          />
          <StatCard
            label={t("maintenance.overdue")}
            value={stats?.overdueCount || 0}
            icon={AlertTriangle}
            iconColor="bg-red-50 text-red-600"
          />
        </div>

        {/* Create Task Dialog */}
        <Dialog open={isCreating} onClose={() => setIsCreating(false)} size="lg">
          <DialogHeader onClose={() => setIsCreating(false)}>{t("maintenance.newMaintenanceTask")}</DialogHeader>
          <form onSubmit={handleSubmit}>
            <DialogContent>
              <div className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium">{t("maintenance.station")}</label>
                    <select
                      value={formData.stationId}
                      onChange={(e) =>
                        setFormData({ ...formData, stationId: e.target.value })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                      required
                    >
                      <option value="">{t("maintenance.selectStation")}</option>
                      {(stations || []).map((station: { id: string; name: string }) => (
                        <option key={station.id} value={station.id}>
                          {station.name}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="text-sm font-medium">{t("maintenance.type")}</label>
                    <select
                      value={formData.type}
                      onChange={(e) =>
                        setFormData({ ...formData, type: e.target.value })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    >
                      <option value="0">{t("maintenance.scheduled")}</option>
                      <option value="1">{t("maintenance.inspection")}</option>
                      <option value="2">{t("maintenance.emergency")}</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="text-sm font-medium">{t("maintenance.titleField")}</label>
                  <input
                    type="text"
                    value={formData.title}
                    onChange={(e) =>
                      setFormData({ ...formData, title: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder={t("maintenance.titlePlaceholder")}
                    required
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">{t("maintenance.descriptionField")}</label>
                  <textarea
                    value={formData.description}
                    onChange={(e) =>
                      setFormData({ ...formData, description: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    rows={3}
                    placeholder={t("maintenance.descriptionPlaceholder")}
                  />
                </div>
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium">{t("maintenance.assignedTo")}</label>
                    <input
                      type="text"
                      value={formData.assignedTo}
                      onChange={(e) =>
                        setFormData({ ...formData, assignedTo: e.target.value })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                      placeholder={t("maintenance.technicianPlaceholder")}
                      required
                    />
                  </div>
                  <div>
                    <label className="text-sm font-medium">{t("maintenance.scheduledDate")}</label>
                    <input
                      type="date"
                      value={formData.scheduledDate}
                      onChange={(e) =>
                        setFormData({ ...formData, scheduledDate: e.target.value })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                      required
                    />
                  </div>
                </div>
              </div>
            </DialogContent>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsCreating(false)}
              >
                {t("common.cancel")}
              </Button>
              <Button type="submit" disabled={createMutation.isPending}>
                {createMutation.isPending ? t("maintenance.creating") : t("maintenance.createTask")}
              </Button>
            </DialogFooter>
          </form>
        </Dialog>

        {/* Filters */}
        <Card>
          <CardContent className="pt-6">
            <div className="flex gap-4">
              <select
                value={statusFilter}
                onChange={(e) => setStatusFilterAndReset(e.target.value)}
                className="rounded-md border px-3 py-2"
                aria-label={t("maintenance.filterByStatus")}
              >
                <option value="all">{t("maintenance.allStatus")}</option>
                <option value="0">{t("maintenance.planned")}</option>
                <option value="1">{t("maintenance.inProgress")}</option>
                <option value="2">{t("maintenance.completed")}</option>
                <option value="3">{t("maintenance.cancelled")}</option>
              </select>
              <select
                value={typeFilter}
                onChange={(e) => { setTypeFilter(e.target.value); resetPage(); }}
                className="rounded-md border px-3 py-2"
                aria-label={t("maintenance.filterByType")}
              >
                <option value="all">{t("maintenance.allTypes")}</option>
                <option value="0">{t("maintenance.scheduled")}</option>
                <option value="1">{t("maintenance.inspection")}</option>
                <option value="2">{t("maintenance.emergency")}</option>
              </select>
            </div>
          </CardContent>
        </Card>

        {/* Tasks List */}
        <div className="space-y-3">
          {isLoading ? (
            <div className="grid gap-4 md:grid-cols-2">
              <SkeletonCard />
              <SkeletonCard />
              <SkeletonCard />
              <SkeletonCard />
            </div>
          ) : tasks.length > 0 ? (
            tasks.map((task) => (
              <Card key={task.id}>
                <CardContent className="py-4">
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-4">
                      <Wrench className="h-5 w-5 text-muted-foreground mt-1" aria-hidden="true" />
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <h3 className="font-semibold">{task.title}</h3>
                          <Badge variant={getTypeColor(task.type)}>
                            {TypeLabels[task.type] || t("maintenance.unknown")}
                          </Badge>
                          <StatusBadge type="maintenance" value={task.status} />
                        </div>
                        {task.description && (
                          <p className="text-sm text-muted-foreground">
                            {task.description}
                          </p>
                        )}
                        <div className="flex items-center gap-4 text-sm text-muted-foreground">
                          <span className="flex items-center gap-1">
                            <MapPin className="h-3 w-3" aria-hidden="true" />
                            {task.stationName}
                          </span>
                          <span className="flex items-center gap-1">
                            <Calendar className="h-3 w-3" aria-hidden="true" />
                            {formatDate(task.scheduledDate)}
                          </span>
                          <span className="flex items-center gap-1">
                            <User className="h-3 w-3" aria-hidden="true" />
                            {task.assignedTo}
                          </span>
                          {task.completedAt && (
                            <span className="flex items-center gap-1">
                              <Clock className="h-3 w-3" aria-hidden="true" />
                              {t("maintenance.done")} {formatDate(task.completedAt)}
                            </span>
                          )}
                        </div>
                        {task.notes && (
                          <p className="text-xs text-muted-foreground italic">
                            {t("maintenance.notes")} {task.notes}
                          </p>
                        )}
                      </div>
                    </div>
                    <div className="flex gap-2">
                      {task.status === 0 && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => startMutation.mutate(task.id)}
                          disabled={startMutation.isPending}
                          title={t("maintenance.start")}
                          aria-label={t("maintenance.start")}
                        >
                          <Play className="h-4 w-4" />
                        </Button>
                      )}
                      {task.status === 1 && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => completeMutation.mutate(task.id)}
                          disabled={completeMutation.isPending}
                          title={t("maintenance.complete")}
                          aria-label={t("maintenance.complete")}
                        >
                          <CheckCircle2 className="h-4 w-4" />
                        </Button>
                      )}
                      {(task.status === 0 || task.status === 1) && (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            if (confirm(t("maintenance.cancelConfirm"))) {
                              cancelMutation.mutate(task.id);
                            }
                          }}
                          disabled={cancelMutation.isPending}
                          title={t("common.cancel")}
                          aria-label={t("common.cancel")}
                        >
                          <X className="h-4 w-4 text-destructive" />
                        </Button>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))
          ) : (
            <EmptyState
              icon={Wrench}
              title={t("maintenance.noTasksFound")}
              description={t("maintenance.noTasksDescription")}
              action={{
                label: t("maintenance.newTask"),
                onClick: () => setIsCreating(true),
              }}
            />
          )}
        </div>

        {/* Pagination */}
        {(totalCount > pageSize || hasPrevPage) && (
          <div className="flex items-center justify-between">
            <div className="text-sm tabular-nums text-muted-foreground">
              {totalCount} {t("maintenance.totalTasks")}
            </div>
            <div className="flex gap-2">
              {hasPrevPage && (
                <Button
                  variant="outline"
                  size="sm"
                  aria-label={t("common.previous")}
                  onClick={goPrevPage}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
              )}
              {hasNextPage && (
                <Button
                  variant="outline"
                  size="sm"
                  aria-label={t("common.next")}
                  onClick={goNextPage}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
