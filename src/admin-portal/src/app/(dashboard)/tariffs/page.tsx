"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { api } from "@/lib/api";
import {
  Plus,
  Edit,
  Trash2,
  Check,
  X,
  DollarSign,
  Clock,
  Zap,
  Star,
  AlertCircle,
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
        setFormError("Failed to save tariff. Please check your input.");
      }
    } else {
      setFormError("Unable to connect to server. Please try again.");
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

  const formatCurrency = (value?: number | null) => {
    return (value ?? 0).toLocaleString("vi-VN") + "đ";
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Tariff Management</h1>
          <p className="text-muted-foreground">
            Configure pricing plans for charging sessions
          </p>
        </div>
        <Button onClick={() => setIsCreating(true)} disabled={isCreating}>
          <Plus className="mr-2 h-4 w-4" />
          Add Tariff
        </Button>
      </div>

      {/* Create/Edit Form */}
      {(isCreating || editingId) && (
        <Card>
          <CardHeader>
            <CardTitle>{editingId ? "Edit Tariff" : "New Tariff"}</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4">
              {formError && (
                <div className="flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                  <AlertCircle className="h-4 w-4 flex-shrink-0" />
                  <span>{formError}</span>
                </div>
              )}
              <div className="grid gap-4 md:grid-cols-2">
                <Input
                  label="Name"
                  type="text"
                  value={formData.name}
                  onChange={(e) =>
                    setFormData({ ...formData, name: e.target.value })
                  }
                  placeholder="e.g., Standard Rate"
                  required
                />
                <Input
                  label="Description"
                  type="text"
                  value={formData.description}
                  onChange={(e) =>
                    setFormData({ ...formData, description: e.target.value })
                  }
                  placeholder="e.g., Default pricing for all stations"
                />
              </div>
              <Input
                label="Effective From"
                type="date"
                value={formData.effectiveFrom}
                onChange={(e) =>
                  setFormData({ ...formData, effectiveFrom: e.target.value })
                }
                required
              />
              <div className="grid gap-4 md:grid-cols-2">
                <Input
                  label="Base Rate per kWh"
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
                  hint="Max 1,000,000"
                  required
                />
                <Input
                  label="Tax Rate"
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
              <div className="flex gap-2">
                <Button
                  type="submit"
                  disabled={createMutation.isPending || updateMutation.isPending}
                >
                  {editingId ? "Update" : "Create"} Tariff
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => {
                    setIsCreating(false);
                    setEditingId(null);
                    resetForm();
                  }}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      {/* Tariffs Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {isLoading ? (
          <div className="col-span-full text-center py-8">Loading...</div>
        ) : tariffs && tariffs.length > 0 ? (
          tariffs.map((tariff) => (
            <Card
              key={tariff.id}
              className={`relative ${tariff.isDefault ? "ring-2 ring-primary" : ""}`}
            >
              {tariff.isDefault && (
                <div className="absolute -top-2 -right-2">
                  <Badge variant="default" className="flex items-center gap-1">
                    <Star className="h-3 w-3" />
                    Default
                  </Badge>
                </div>
              )}
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle className="text-lg">{tariff.name}</CardTitle>
                  <Badge variant={tariff.isActive ? "success" : "secondary"}>
                    {tariff.isActive ? "Active" : "Inactive"}
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
                      <Zap className="h-4 w-4 text-yellow-500" />
                      Base Rate/kWh
                    </span>
                    <span className="font-semibold">
                      {formatCurrency(tariff.baseRatePerKwh)}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm">
                      <DollarSign className="h-4 w-4 text-green-500" />
                      Total Rate/kWh
                    </span>
                    <span className="font-semibold">
                      {formatCurrency(tariff.totalRatePerKwh)}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm">
                      <Clock className="h-4 w-4 text-blue-500" />
                      Tax Rate
                    </span>
                    <span className="font-semibold">
                      {tariff.taxRatePercent}%
                    </span>
                  </div>
                </div>

                <div className="flex flex-wrap gap-2 pt-2 border-t">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleEdit(tariff)}
                  >
                    <Edit className="h-4 w-4" />
                  </Button>
                  <Button
                    variant={tariff.isActive ? "outline" : "default"}
                    size="sm"
                    onClick={() =>
                      toggleActiveMutation.mutate({
                        id: tariff.id,
                        activate: !tariff.isActive,
                      })
                    }
                  >
                    {tariff.isActive ? (
                      <X className="h-4 w-4" />
                    ) : (
                      <Check className="h-4 w-4" />
                    )}
                  </Button>
                  {!tariff.isDefault && tariff.isActive && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setDefaultMutation.mutate(tariff.id)}
                    >
                      <Star className="h-4 w-4" />
                    </Button>
                  )}
                  {!tariff.isDefault && (
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => {
                        if (confirm("Delete this tariff?")) {
                          deleteMutation.mutate(tariff.id);
                        }
                      }}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))
        ) : (
          <div className="col-span-full text-center py-8 text-muted-foreground">
            No tariffs found. Create your first tariff to get started.
          </div>
        )}
      </div>
    </div>
  );
}
