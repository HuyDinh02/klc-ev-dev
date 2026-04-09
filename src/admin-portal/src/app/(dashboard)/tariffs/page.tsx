"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { SkeletonCard } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import {
  Dialog,
  DialogHeader,
  DialogContent,
  DialogFooter,
} from "@/components/ui/dialog";
import { api } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import {
  Plus,
  Edit,
  Trash2,
  Check,
  X,
  DollarSign,
  Zap,
  Star,
  AlertCircle,
  Receipt,
  BarChart3,
  Percent,
} from "lucide-react";

interface Tariff {
  id: string;
  name: string;
  description?: string;
  baseRatePerKwh: number;
  taxRatePercent: number;
  totalRatePerKwh: number;
  isActive: boolean;
  isDefault: boolean;
  effectiveFrom: string;
  effectiveTo?: string;
  creationTime: string;
}

interface TariffFormData {
  name: string;
  description: string;
  baseRatePerKwh: number;
  taxRatePercent: number;
  effectiveFrom: string;
}

export default function TariffsPage() {
  const hasAccess = useRequirePermission("KLC.Tariffs");
  const canCreate = useHasPermission("KLC.Tariffs.Create");
  const canUpdate = useHasPermission("KLC.Tariffs.Update");
  const canActivate = useHasPermission("KLC.Tariffs.Activate");
  const canDeactivate = useHasPermission("KLC.Tariffs.Deactivate");
  const canDelete = useHasPermission("KLC.Tariffs.Delete");
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [isCreating, setIsCreating] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formError, setFormError] = useState("");
  const [formData, setFormData] = useState<TariffFormData>({
    name: "",
    description: "",
    baseRatePerKwh: 0,
    taxRatePercent: 10,
    effectiveFrom: new Date().toISOString().slice(0, 10),
  });

  // Fetch tariffs
  const { data: tariffs, isLoading } = useQuery<Tariff[]>({
    queryKey: ["tariffs"],
    queryFn: async () => {
      const res = await api.get("/tariffs");
      return res.data.items || [];
    },
  });

  // Create tariff
  const createMutation = useMutation({
    mutationFn: async (data: TariffFormData) => {
      const res = await api.post("/tariffs", {
        ...data,
        effectiveFrom: new Date(data.effectiveFrom).toISOString(),
      });
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tariffs"] });
      setIsCreating(false);
      setFormError("");
      resetForm();
    },
    onError: (err: unknown) => {
      handleMutationError(err);
    },
  });

  // Update tariff
  const updateMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string; data: TariffFormData }) => {
      const res = await api.put(`/tariffs/${id}`, {
        ...data,
        effectiveFrom: new Date(data.effectiveFrom).toISOString(),
      });
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tariffs"] });
      setEditingId(null);
      setFormError("");
      resetForm();
    },
    onError: (err: unknown) => {
      handleMutationError(err);
    },
  });

  const handleMutationError = (err: unknown) => {
    if (err && typeof err === "object" && "response" in err) {
      const axiosError = err as { response?: { data?: { error?: { message?: string; details?: string; validationErrors?: Array<{ message: string }> } } } };
      const apiError = axiosError.response?.data?.error;
      if (apiError?.validationErrors?.length) {
        setFormError(apiError.validationErrors.map((e) => e.message).join(". "));
      } else if (apiError?.details) {
        setFormError(apiError.details);
      } else if (apiError?.message) {
        setFormError(apiError.message);
      } else {
        setFormError(t("tariffs.saveFailed"));
      }
    } else {
      setFormError(t("tariffs.connectionError"));
    }
  };

  // Activate/Deactivate tariff
  const toggleActiveMutation = useMutation({
    mutationFn: async ({ id, activate }: { id: string; activate: boolean }) => {
      const endpoint = activate ? "activate" : "deactivate";
      await api.post(`/tariffs/${id}/${endpoint}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tariffs"] });
    },
  });

  // Set default tariff
  const setDefaultMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.post(`/tariffs/${id}/set-default`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tariffs"] });
    },
  });

  // Delete tariff
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/tariffs/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tariffs"] });
    },
    onError: (err: unknown) => {
      handleMutationError(err);
    },
  });

  const resetForm = () => {
    setFormError("");
    setFormData({
      name: "",
      description: "",
      baseRatePerKwh: 0,
      taxRatePercent: 10,
      effectiveFrom: new Date().toISOString().slice(0, 10),
    });
  };

  const handleEdit = (tariff: Tariff) => {
    setFormError("");
    setEditingId(tariff.id);
    setFormData({
      name: tariff.name,
      description: tariff.description || "",
      baseRatePerKwh: tariff.baseRatePerKwh,
      taxRatePercent: tariff.taxRatePercent,
      effectiveFrom: tariff.effectiveFrom ? tariff.effectiveFrom.slice(0, 10) : new Date().toISOString().slice(0, 10),
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (editingId) {
      updateMutation.mutate({ id: editingId, data: formData });
    } else {
      createMutation.mutate(formData);
    }
  };

  const handleCloseDialog = () => {
    setIsCreating(false);
    setEditingId(null);
    resetForm();
  };

  const formatCurrency = (value?: number | null) => {
    return (value ?? 0).toLocaleString("vi-VN") + "đ";
  };

  // Computed stats
  const totalTariffs = tariffs?.length ?? 0;
  const activeTariffs = tariffs?.filter((t) => t.isActive).length ?? 0;
  const defaultTariff = tariffs?.find((t) => t.isDefault);
  const avgRate = totalTariffs > 0
    ? (tariffs!.reduce((sum, t) => sum + (t.totalRatePerKwh || t.baseRatePerKwh * (1 + t.taxRatePercent / 100)), 0) / totalTariffs)
    : 0;

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title={t("tariffs.title")} description={t("tariffs.description")}>
          {canCreate && (
            <Button onClick={() => setIsCreating(true)} disabled={isCreating || !!editingId}>
              <Plus className="mr-2 h-4 w-4" aria-hidden="true" />
              {t("tariffs.addTariff")}
            </Button>
          )}
        </PageHeader>
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* KPI Stats */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard
            label={t("tariffs.totalTariffs")}
            value={totalTariffs}
            icon={Receipt}
            iconColor="bg-primary/10 text-primary"
          />
          <StatCard
            label={t("tariffs.active")}
            value={activeTariffs}
            icon={Check}
            iconColor="bg-green-500/10 text-green-600"
          />
          <StatCard
            label={t("tariffs.avgRatePerKwh")}
            value={formatCurrency(Math.round(avgRate))}
            icon={BarChart3}
            iconColor="bg-blue-500/10 text-blue-600"
          />
          <StatCard
            label={t("tariffs.defaultPlan")}
            value={defaultTariff?.name ?? t("tariffs.none")}
            icon={Star}
            iconColor="bg-amber-500/10 text-amber-600"
          />
        </div>

        {/* Create/Edit Dialog */}
        <Dialog open={isCreating || !!editingId} onClose={handleCloseDialog} size="lg">
          <DialogHeader onClose={handleCloseDialog}>
            {editingId ? t("tariffs.editTariff") : t("tariffs.newTariff")}
          </DialogHeader>
          <form onSubmit={handleSubmit}>
            <DialogContent className="space-y-4">
              {formError && (
                <div className="flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-sm text-destructive" role="alert">
                  <AlertCircle className="h-4 w-4 flex-shrink-0" aria-hidden="true" />
                  <span>{formError}</span>
                </div>
              )}
              <div className="grid gap-4 md:grid-cols-2">
                <Input
                  label={t("tariffs.name")}
                  type="text"
                  value={formData.name}
                  onChange={(e) =>
                    setFormData({ ...formData, name: e.target.value })
                  }
                  placeholder={t("tariffs.namePlaceholder")}
                  required
                />
                <Input
                  label={t("tariffs.descriptionLabel")}
                  type="text"
                  value={formData.description}
                  onChange={(e) =>
                    setFormData({ ...formData, description: e.target.value })
                  }
                  placeholder={t("tariffs.descriptionPlaceholder")}
                />
              </div>
              <Input
                label={t("tariffs.effectiveFrom")}
                type="date"
                value={formData.effectiveFrom}
                onChange={(e) =>
                  setFormData({ ...formData, effectiveFrom: e.target.value })
                }
                required
              />
              <div className="grid gap-4 md:grid-cols-2">
                <Input
                  label={t("tariffs.baseRatePerKwh")}
                  type="number"
                  value={formData.baseRatePerKwh}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      baseRatePerKwh: parseFloat(e.target.value) || 0,
                    })
                  }
                  min={0}
                  max={1000000}
                  step={100}
                  suffix="đ/kWh"
                  hint={t("tariffs.maxRate")}
                  required
                />
                <Input
                  label={t("tariffs.taxRate")}
                  type="number"
                  value={formData.taxRatePercent}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      taxRatePercent: parseFloat(e.target.value) || 0,
                    })
                  }
                  min={0}
                  max={100}
                  step={1}
                  suffix="%"
                />
              </div>
            </DialogContent>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={handleCloseDialog}
              >
                {t("common.cancel")}
              </Button>
              <Button
                type="submit"
                disabled={createMutation.isPending || updateMutation.isPending}
              >
                {editingId ? t("tariffs.updateTariff") : t("tariffs.createTariff")}
              </Button>
            </DialogFooter>
          </form>
        </Dialog>

        {/* Tariffs Grid */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {isLoading ? (
            Array.from({ length: 6 }).map((_, i) => (
              <SkeletonCard key={i} />
            ))
          ) : tariffs && tariffs.length > 0 ? (
            tariffs.map((tariff) => (
              <Card
                key={tariff.id}
                className={`relative ${tariff.isDefault ? "ring-2 ring-primary" : ""}`}
              >
                {tariff.isDefault && (
                  <div className="absolute -top-2 -right-2">
                    <Badge variant="default" className="flex items-center gap-1">
                      <Star className="h-3 w-3" aria-hidden="true" />
                      {t("tariffs.default")}
                    </Badge>
                  </div>
                )}
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-lg">{tariff.name}</CardTitle>
                    <Badge variant={tariff.isActive ? "success" : "secondary"}>
                      {tariff.isActive ? t("common.active") : t("common.inactive")}
                    </Badge>
                  </div>
                  {tariff.description && (
                    <p className="text-sm text-muted-foreground">
                      {tariff.description}
                    </p>
                  )}
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-2">
                    <div className="flex items-center justify-between">
                      <span className="flex items-center gap-2 text-sm">
                        <Zap className="h-4 w-4 text-amber-500" aria-hidden="true" />
                        {t("tariffs.baseRate")}
                      </span>
                      <span className="font-semibold tabular-nums text-right">
                        {formatCurrency(tariff.baseRatePerKwh)}
                      </span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="flex items-center gap-2 text-sm">
                        <DollarSign className="h-4 w-4 text-green-600" aria-hidden="true" />
                        {t("tariffs.totalRate")}
                      </span>
                      <span className="font-semibold tabular-nums text-right">
                        {formatCurrency(tariff.totalRatePerKwh)}
                      </span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="flex items-center gap-2 text-sm">
                        <Percent className="h-4 w-4 text-blue-600" aria-hidden="true" />
                        {t("tariffs.taxRate")}
                      </span>
                      <span className="font-semibold tabular-nums text-right">
                        {tariff.taxRatePercent}%
                      </span>
                    </div>
                  </div>

                  <div className="flex flex-wrap gap-2 pt-2 border-t">
                    {canUpdate && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleEdit(tariff)}
                        aria-label={t("common.edit")}
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                    )}
                    {(tariff.isActive ? canDeactivate : canActivate) && (
                      <Button
                        variant={tariff.isActive ? "outline" : "default"}
                        size="sm"
                        onClick={() =>
                          toggleActiveMutation.mutate({
                            id: tariff.id,
                            activate: !tariff.isActive,
                          })
                        }
                        aria-label={tariff.isActive ? t("common.deactivate") : t("common.activate")}
                      >
                        {tariff.isActive ? (
                          <X className="h-4 w-4" />
                        ) : (
                          <Check className="h-4 w-4" />
                        )}
                      </Button>
                    )}
                    {!tariff.isDefault && tariff.isActive && canUpdate && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setDefaultMutation.mutate(tariff.id)}
                        aria-label={t("tariffs.setDefault")}
                      >
                        <Star className="h-4 w-4" />
                      </Button>
                    )}
                    {!tariff.isDefault && canDelete && (
                      <Button
                        variant="destructive"
                        size="sm"
                        onClick={() => {
                          if (confirm(t("tariffs.deleteConfirm"))) {
                            deleteMutation.mutate(tariff.id);
                          }
                        }}
                        aria-label={t("common.delete")}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </CardContent>
              </Card>
            ))
          ) : (
            <div className="col-span-full">
              <EmptyState
                icon={Receipt}
                title={t("tariffs.noTariffsFound")}
                description={t("tariffs.noTariffsDescription")}
                action={{
                  label: t("tariffs.addTariff"),
                  onClick: () => setIsCreating(true),
                }}
              />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
