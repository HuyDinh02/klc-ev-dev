"use client";

import { useState, useRef } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import { SkeletonCard } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import {
  Dialog,
  DialogHeader,
  DialogContent,
  DialogFooter,
} from "@/components/ui/dialog";
import { PROMOTION_TYPE } from "@/lib/constants";
import { api } from "@/lib/api";
import {
  Plus,
  Edit,
  Trash2,
  AlertCircle,
  Megaphone,
  Calendar,
  Upload,
  X,
  Image as ImageIcon,
  Loader2,
} from "lucide-react";

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

type FilterTab = "all" | "active" | "inactive";

export default function PromotionsPage() {
  const queryClient = useQueryClient();
  const [isCreating, setIsCreating] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formError, setFormError] = useState("");
  const [filter, setFilter] = useState<FilterTab>("all");
  const [isUploading, setIsUploading] = useState(false);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [formData, setFormData] = useState<PromotionFormData>({
    title: "",
    description: "",
    imageUrl: "",
    startDate: new Date().toISOString().slice(0, 10),
    endDate: new Date().toISOString().slice(0, 10),
    type: 0,
    isActive: true,
  });

  const { data: allPromotions, isLoading } = useQuery<Promotion[]>({
    queryKey: ["promotions"],
    queryFn: async () => {
      const res = await api.get("/admin/promotions", {
        params: { pageSize: 50 },
      });
      return res.data.data || [];
    },
  });

  const promotions = allPromotions?.filter((p) => {
    if (filter === "active") return p.isActive;
    if (filter === "inactive") return !p.isActive;
    return true;
  });

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
    onError: (err: unknown) => handleMutationError(err),
  });

  const updateMutation = useMutation({
    mutationFn: async ({
      id,
      data,
    }: {
      id: string;
      data: PromotionFormData;
    }) => {
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
    onError: (err: unknown) => handleMutationError(err),
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/admin/promotions/${id}`);
    },
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["promotions"] }),
    onError: (err: unknown) => handleMutationError(err),
  });

  const handleMutationError = (err: unknown) => {
    if (err && typeof err === "object" && "response" in err) {
      const axiosError = err as {
        response?: {
          data?: {
            error?: {
              message?: string;
              details?: string;
              validationErrors?: Array<{ message: string }>;
            };
          };
        };
      };
      const apiError = axiosError.response?.data?.error;
      if (apiError?.validationErrors?.length) {
        setFormError(
          apiError.validationErrors.map((e) => e.message).join(". ")
        );
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
    setImagePreview(null);
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
    setImagePreview(promo.imageUrl || null);
    setFormData({
      title: promo.title,
      description: promo.description || "",
      imageUrl: promo.imageUrl || "",
      startDate: promo.startDate
        ? promo.startDate.slice(0, 10)
        : new Date().toISOString().slice(0, 10),
      endDate: promo.endDate
        ? promo.endDate.slice(0, 10)
        : new Date().toISOString().slice(0, 10),
      type: promo.type,
      isActive: promo.isActive,
    });
  };

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      setFormError("Image must be less than 5MB");
      return;
    }

    const allowedTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    if (!allowedTypes.includes(file.type)) {
      setFormError("Only JPEG, PNG, GIF, and WebP images are allowed");
      return;
    }

    setIsUploading(true);
    setFormError("");

    try {
      const formDataUpload = new FormData();
      formDataUpload.append("file", file);

      const res = await api.post(
        "/admin/promotions/upload-image",
        formDataUpload,
        { headers: { "Content-Type": "multipart/form-data" } }
      );

      const url = res.data.url;
      setFormData((prev) => ({ ...prev, imageUrl: url }));
      setImagePreview(url);
    } catch {
      setFormError("Failed to upload image. Please try again.");
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  const handleRemoveImage = () => {
    setFormData((prev) => ({ ...prev, imageUrl: "" }));
    setImagePreview(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
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
    if (editingId) {
      updateMutation.mutate({ id: editingId, data: formData });
    } else {
      createMutation.mutate(formData);
    }
  };

  const formatDate = (date: string) =>
    new Date(date).toLocaleDateString("vi-VN");

  const isFormOpen = isCreating || editingId;

  const closeForm = () => {
    setIsCreating(false);
    setEditingId(null);
    resetForm();
  };

  return (
    <div className="flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title="Promotions" description="Manage promotional campaigns for drivers">
          <Button onClick={() => setIsCreating(true)}>
            <Plus className="mr-2 h-4 w-4" />
            Add Promotion
          </Button>
        </PageHeader>
      </div>

      <div className="flex-1 space-y-6 p-6">
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

        {/* Create/Edit Dialog */}
        <Dialog open={!!isFormOpen} onClose={closeForm} size="xl">
          <DialogHeader onClose={closeForm}>
            {editingId ? "Edit Promotion" : "New Promotion"}
          </DialogHeader>
          <DialogContent>
            <form id="promotion-form" onSubmit={handleSubmit} className="space-y-4">
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
                      setFormData({
                        ...formData,
                        type: parseInt(e.target.value),
                      })
                    }
                    className="h-10 w-full rounded-md border bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    {Object.entries(PROMOTION_TYPE).map(([val, config]) => (
                      <option key={val} value={val}>
                        {config.label}
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

              {/* Image Upload */}
              <div className="space-y-2">
                <label className="text-sm font-medium">Promotion Image</label>
                {imagePreview ? (
                  <div className="relative inline-block">
                    <img
                      src={imagePreview}
                      alt="Preview"
                      className="h-40 w-auto rounded-lg border object-cover"
                    />
                    <button
                      type="button"
                      onClick={handleRemoveImage}
                      className="absolute -right-2 -top-2 rounded-full bg-destructive p-1 text-white shadow-sm hover:bg-destructive/90"
                    >
                      <X className="h-3 w-3" />
                    </button>
                  </div>
                ) : (
                  <div
                    onClick={() => fileInputRef.current?.click()}
                    className="flex h-40 cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-muted-foreground/25 bg-muted/50 transition-colors hover:border-muted-foreground/50 hover:bg-muted"
                  >
                    {isUploading ? (
                      <>
                        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
                        <span className="text-sm text-muted-foreground">
                          Uploading...
                        </span>
                      </>
                    ) : (
                      <>
                        <Upload className="h-8 w-8 text-muted-foreground" />
                        <span className="text-sm text-muted-foreground">
                          Click to upload image
                        </span>
                        <span className="text-xs text-muted-foreground">
                          JPEG, PNG, GIF, WebP (max 5MB)
                        </span>
                      </>
                    )}
                  </div>
                )}
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/jpeg,image/png,image/gif,image/webp"
                  onChange={handleImageUpload}
                  className="hidden"
                />
              </div>

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
            </form>
          </DialogContent>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={closeForm}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              form="promotion-form"
              disabled={
                createMutation.isPending ||
                updateMutation.isPending ||
                isUploading
              }
            >
              {createMutation.isPending || updateMutation.isPending ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : null}
              {editingId ? "Update" : "Create"} Promotion
            </Button>
          </DialogFooter>
        </Dialog>

        {/* Promotions Grid */}
        {isLoading ? (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
          </div>
        ) : promotions && promotions.length > 0 ? (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {promotions.map((promo) => {
              const typeConfig = PROMOTION_TYPE[promo.type];
              return (
                <Card
                  key={promo.id}
                  className="group overflow-hidden transition-shadow hover:shadow-lg"
                >
                  {/* Image / Placeholder */}
                  <div className="relative h-48 w-full overflow-hidden bg-muted">
                    {promo.imageUrl ? (
                      <img
                        src={promo.imageUrl}
                        alt={promo.title}
                        className="h-full w-full object-cover transition-transform group-hover:scale-105"
                      />
                    ) : (
                      <div className="flex h-full items-center justify-center">
                        <ImageIcon className="h-16 w-16 text-muted-foreground/25" />
                      </div>
                    )}
                    {/* Status badge overlay */}
                    <div className="absolute right-3 top-3">
                      <Badge
                        variant={promo.isActive ? "success" : "secondary"}
                        className="shadow-sm"
                      >
                        {promo.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </div>
                    {/* Type badge overlay */}
                    <div className="absolute left-3 top-3">
                      <Badge
                        variant={typeConfig?.badgeVariant || "secondary"}
                        className="shadow-sm"
                      >
                        {typeConfig?.label || "Unknown"}
                      </Badge>
                    </div>
                  </div>

                  <CardContent className="p-4">
                    <h3 className="font-semibold text-lg leading-tight mb-1 line-clamp-1">
                      {promo.title}
                    </h3>
                    {promo.description && (
                      <p className="text-sm text-muted-foreground line-clamp-2 mb-3">
                        {promo.description}
                      </p>
                    )}

                    <div className="flex items-center gap-1.5 text-xs text-muted-foreground mb-3">
                      <Calendar className="h-3.5 w-3.5" />
                      <span>
                        {formatDate(promo.startDate)} - {formatDate(promo.endDate)}
                      </span>
                    </div>

                    <div className="flex gap-2 pt-3 border-t">
                      <Button
                        variant="outline"
                        size="sm"
                        className="flex-1"
                        onClick={() => handleEdit(promo)}
                      >
                        <Edit className="mr-1.5 h-3.5 w-3.5" />
                        Edit
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
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        ) : (
          <EmptyState
            icon={Megaphone}
            title="No promotions found"
            description="Create your first promotion to get started."
            action={{
              label: "Add Promotion",
              onClick: () => setIsCreating(true),
            }}
          />
        )}
      </div>
    </div>
  );
}
