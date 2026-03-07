"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { api } from "@/lib/api";
import { Plus, Edit, Trash2, AlertCircle, Megaphone, Calendar } from "lucide-react";

interface Promotion {
  id: string;
  title: string;
  description?: string;
  imageUrl?: string;
  startDate: string;
  endDate: string;
  type: number;
  isActive: boolean;
  isCurrentlyActive: boolean;
  createdAt: string;
}

interface PromotionFormData {
  title: string;
  description: string;
  imageUrl: string;
  startDate: string;
  endDate: string;
  type: number;
  isActive: boolean;
}

const PROMOTION_TYPES: Record<number, string> = {
  0: "Banner",
  1: "Popup",
  2: "In-App",
  3: "Push",
};

const TYPE_VARIANTS: Record<number, "default" | "secondary" | "warning" | "outline"> = {
  0: "default",
  1: "warning",
  2: "secondary",
  3: "outline",
};

type FilterTab = "all" | "active" | "inactive";

export default function PromotionsPage() {
  const queryClient = useQueryClient();
  const [isCreating, setIsCreating] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formError, setFormError] = useState("");
  const [filter, setFilter] = useState<FilterTab>("all");
  const [formData, setFormData] = useState<PromotionFormData>({
    title: "",
    description: "",
    imageUrl: "",
    startDate: new Date().toISOString().slice(0, 10),
    endDate: new Date().toISOString().slice(0, 10),
    type: 0,
    isActive: true,
  });

  // Fetch promotions (backend doesn't support isActive filter, so filter client-side)
  const { data: allPromotions, isLoading } = useQuery<Promotion[]>({
    queryKey: ["promotions"],
    queryFn: async () => {
      const res = await api.get("/admin/promotions", { params: { pageSize: 50 } });
      return res.data.data || [];
    },
  });

  const promotions = allPromotions?.filter((p) => {
    if (filter === "active") return p.isActive;
    if (filter === "inactive") return !p.isActive;
    return true;
  });

  // Create promotion
  const createMutation = useMutation({
    mutationFn: async (data: PromotionFormData) => {
      const res = await api.post("/admin/promotions", {
        ...data,
        startDate: new Date(data.startDate).toISOString(),
        endDate: new Date(data.endDate).toISOString(),
      });
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["promotions"] });
      setIsCreating(false);
      setFormError("");
      resetForm();
    },
    onError: (err: unknown) => {
      handleMutationError(err);
    },
  });

  // Update promotion
  const updateMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string; data: PromotionFormData }) => {
      const res = await api.put(`/admin/promotions/${id}`, {
        ...data,
        startDate: new Date(data.startDate).toISOString(),
        endDate: new Date(data.endDate).toISOString(),
      });
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["promotions"] });
      setEditingId(null);
      setFormError("");
      resetForm();
    },
    onError: (err: unknown) => {
      handleMutationError(err);
    },
  });

  // Delete promotion
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/admin/promotions/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["promotions"] });
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
        setFormError("Failed to save promotion. Please check your input.");
      }
    } else {
      setFormError("Unable to connect to server. Please try again.");
    }
  };

  const resetForm = () => {
    setFormError("");
    setFormData({
      title: "",
      description: "",
      imageUrl: "",
      startDate: new Date().toISOString().slice(0, 10),
      endDate: new Date().toISOString().slice(0, 10),
      type: 0,
      isActive: true,
    });
  };

  const handleEdit = (promo: Promotion) => {
    setFormError("");
    setEditingId(promo.id);
    setFormData({
      title: promo.title,
      description: promo.description || "",
      imageUrl: promo.imageUrl || "",
      startDate: promo.startDate ? promo.startDate.slice(0, 10) : new Date().toISOString().slice(0, 10),
      endDate: promo.endDate ? promo.endDate.slice(0, 10) : new Date().toISOString().slice(0, 10),
      type: promo.type,
      isActive: promo.isActive,
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (formData.title.length > 200) {
      setFormError("Title must be 200 characters or less.");
      return;
    }
    if (formData.description.length > 2000) {
      setFormError("Description must be 2000 characters or less.");
      return;
    }
    if (formData.imageUrl.length > 500) {
      setFormError("Image URL must be 500 characters or less.");
      return;
    }
    if (editingId) {
      updateMutation.mutate({ id: editingId, data: formData });
    } else {
      createMutation.mutate(formData);
    }
  };

  const formatDate = (date: string) => new Date(date).toLocaleDateString("vi-VN");

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Promotions</h1>
          <p className="text-muted-foreground">
            Manage promotional campaigns for drivers
          </p>
        </div>
        <Button onClick={() => setIsCreating(true)} disabled={isCreating}>
          <Plus className="mr-2 h-4 w-4" />
          Add Promotion
        </Button>
      </div>

      {/* Filter Tabs */}
      <div className="flex gap-2">
        {(["all", "active", "inactive"] as FilterTab[]).map((tab) => (
          <Button
            key={tab}
            variant={filter === tab ? "default" : "outline"}
            size="sm"
            onClick={() => setFilter(tab)}
          >
            {tab === "all" ? "All" : tab === "active" ? "Active" : "Inactive"}
          </Button>
        ))}
      </div>

      {/* Create/Edit Form */}
      {(isCreating || editingId) && (
        <Card>
          <CardHeader>
            <CardTitle>{editingId ? "Edit Promotion" : "New Promotion"}</CardTitle>
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
                  label="Title"
                  type="text"
                  value={formData.title}
                  onChange={(e) =>
                    setFormData({ ...formData, title: e.target.value })
                  }
                  placeholder="e.g., Summer Charging Discount"
                  maxLength={200}
                  required
                />
                <div className="space-y-1">
                  <label htmlFor="type" className="text-sm font-medium">
                    Type
                  </label>
                  <select
                    id="type"
                    value={formData.type}
                    onChange={(e) =>
                      setFormData({ ...formData, type: parseInt(e.target.value) })
                    }
                    className="h-10 w-full rounded-md border bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    {Object.entries(PROMOTION_TYPES).map(([val, label]) => (
                      <option key={val} value={val}>
                        {label}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="space-y-1">
                <label htmlFor="description" className="text-sm font-medium">
                  Description
                </label>
                <textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) =>
                    setFormData({ ...formData, description: e.target.value })
                  }
                  placeholder="Describe the promotion..."
                  maxLength={2000}
                  rows={3}
                  className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
              <Input
                label="Image URL"
                type="url"
                value={formData.imageUrl}
                onChange={(e) =>
                  setFormData({ ...formData, imageUrl: e.target.value })
                }
                placeholder="https://example.com/image.jpg"
                maxLength={500}
              />
              <div className="grid gap-4 md:grid-cols-2">
                <Input
                  label="Start Date"
                  type="date"
                  value={formData.startDate}
                  onChange={(e) =>
                    setFormData({ ...formData, startDate: e.target.value })
                  }
                  required
                />
                <Input
                  label="End Date"
                  type="date"
                  value={formData.endDate}
                  onChange={(e) =>
                    setFormData({ ...formData, endDate: e.target.value })
                  }
                  required
                />
              </div>
              <div className="flex items-center gap-2">
                <input
                  id="isActive"
                  type="checkbox"
                  checked={formData.isActive}
                  onChange={(e) =>
                    setFormData({ ...formData, isActive: e.target.checked })
                  }
                  className="h-4 w-4 rounded border"
                />
                <label htmlFor="isActive" className="text-sm font-medium">
                  Active
                </label>
              </div>
              <div className="flex gap-2">
                <Button
                  type="submit"
                  disabled={createMutation.isPending || updateMutation.isPending}
                >
                  {editingId ? "Update" : "Create"} Promotion
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

      {/* Promotions Grid */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {isLoading ? (
          <div className="col-span-full text-center py-8">Loading...</div>
        ) : promotions && promotions.length > 0 ? (
          promotions.map((promo) => (
            <Card key={promo.id}>
              {promo.imageUrl && (
                <div className="h-40 w-full overflow-hidden rounded-t-lg">
                  <img
                    src={promo.imageUrl}
                    alt={promo.title}
                    className="h-full w-full object-cover"
                  />
                </div>
              )}
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle className="text-lg">{promo.title}</CardTitle>
                  <Badge variant={promo.isActive ? "success" : "secondary"}>
                    {promo.isActive ? "Active" : "Inactive"}
                  </Badge>
                </div>
                {promo.description && (
                  <p className="text-sm text-muted-foreground line-clamp-2">
                    {promo.description}
                  </p>
                )}
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm">
                      <Megaphone className="h-4 w-4 text-blue-500" />
                      Type
                    </span>
                    <Badge variant={TYPE_VARIANTS[promo.type] || "secondary"}>
                      {PROMOTION_TYPES[promo.type] || "Unknown"}
                    </Badge>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm">
                      <Calendar className="h-4 w-4 text-green-500" />
                      Date Range
                    </span>
                    <span className="text-sm font-medium">
                      {formatDate(promo.startDate)} - {formatDate(promo.endDate)}
                    </span>
                  </div>
                </div>

                <div className="flex flex-wrap gap-2 pt-2 border-t">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleEdit(promo)}
                  >
                    <Edit className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="destructive"
                    size="sm"
                    onClick={() => {
                      if (confirm("Delete this promotion?")) {
                        deleteMutation.mutate(promo.id);
                      }
                    }}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))
        ) : (
          <div className="col-span-full text-center py-8 text-muted-foreground">
            No promotions found. Create your first promotion to get started.
          </div>
        )}
      </div>
    </div>
  );
}
