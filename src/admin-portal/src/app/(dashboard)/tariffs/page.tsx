"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
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
} from "lucide-react";

interface Tariff {
  id: string;
  name: string;
  description: string;
  pricePerKwh: number;
  pricePerMinute: number;
  connectionFee: number;
  currency: string;
  isActive: boolean;
  isDefault: boolean;
  validFrom: string;
  validTo?: string;
  timeSlots?: TimeSlot[];
  createdAt: string;
}

interface TimeSlot {
  startHour: number;
  endHour: number;
  pricePerKwh: number;
  pricePerMinute: number;
}

interface TariffFormData {
  name: string;
  description: string;
  pricePerKwh: number;
  pricePerMinute: number;
  connectionFee: number;
}

export default function TariffsPage() {
  const queryClient = useQueryClient();
  const [isCreating, setIsCreating] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formData, setFormData] = useState<TariffFormData>({
    name: "",
    description: "",
    pricePerKwh: 0,
    pricePerMinute: 0,
    connectionFee: 0,
  });

  // Fetch tariffs
  const { data: tariffs, isLoading } = useQuery<Tariff[]>({
    queryKey: ["tariffs"],
    queryFn: async () => {
      const res = await api.get("/api/v1/tariffs");
      return res.data.items || [];
    },
  });

  // Create tariff
  const createMutation = useMutation({
    mutationFn: async (data: TariffFormData) => {
      const res = await api.post("/api/v1/tariffs", data);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tariffs"] });
      setIsCreating(false);
      resetForm();
    },
  });

  // Update tariff
  const updateMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string; data: TariffFormData }) => {
      const res = await api.put(`/api/v1/tariffs/${id}`, data);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tariffs"] });
      setEditingId(null);
      resetForm();
    },
  });

  // Activate/Deactivate tariff
  const toggleActiveMutation = useMutation({
    mutationFn: async ({ id, activate }: { id: string; activate: boolean }) => {
      const endpoint = activate ? "activate" : "deactivate";
      await api.post(`/api/v1/tariffs/${id}/${endpoint}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tariffs"] });
    },
  });

  // Set default tariff
  const setDefaultMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.post(`/api/v1/tariffs/${id}/set-default`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tariffs"] });
    },
  });

  // Delete tariff
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/v1/tariffs/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tariffs"] });
    },
  });

  const resetForm = () => {
    setFormData({
      name: "",
      description: "",
      pricePerKwh: 0,
      pricePerMinute: 0,
      connectionFee: 0,
    });
  };

  const handleEdit = (tariff: Tariff) => {
    setEditingId(tariff.id);
    setFormData({
      name: tariff.name,
      description: tariff.description,
      pricePerKwh: tariff.pricePerKwh,
      pricePerMinute: tariff.pricePerMinute,
      connectionFee: tariff.connectionFee,
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

  const formatCurrency = (value: number) => {
    return value.toLocaleString("vi-VN") + "đ";
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
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="text-sm font-medium">Name</label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) =>
                      setFormData({ ...formData, name: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder="e.g., Standard Rate"
                    required
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">Description</label>
                  <input
                    type="text"
                    value={formData.description}
                    onChange={(e) =>
                      setFormData({ ...formData, description: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder="e.g., Default pricing for all stations"
                  />
                </div>
              </div>
              <div className="grid gap-4 md:grid-cols-3">
                <div>
                  <label className="text-sm font-medium">Price per kWh (VNĐ)</label>
                  <input
                    type="number"
                    value={formData.pricePerKwh}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        pricePerKwh: parseFloat(e.target.value) || 0,
                      })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    min="0"
                    step="100"
                    required
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">
                    Price per Minute (VNĐ)
                  </label>
                  <input
                    type="number"
                    value={formData.pricePerMinute}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        pricePerMinute: parseFloat(e.target.value) || 0,
                      })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    min="0"
                    step="10"
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">Connection Fee (VNĐ)</label>
                  <input
                    type="number"
                    value={formData.connectionFee}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        connectionFee: parseFloat(e.target.value) || 0,
                      })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    min="0"
                    step="1000"
                  />
                </div>
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
                      Per kWh
                    </span>
                    <span className="font-semibold">
                      {formatCurrency(tariff.pricePerKwh)}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm">
                      <Clock className="h-4 w-4 text-blue-500" />
                      Per Minute
                    </span>
                    <span className="font-semibold">
                      {formatCurrency(tariff.pricePerMinute)}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm">
                      <DollarSign className="h-4 w-4 text-green-500" />
                      Connection Fee
                    </span>
                    <span className="font-semibold">
                      {formatCurrency(tariff.connectionFee)}
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
