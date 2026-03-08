"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import { SkeletonTable } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { api } from "@/lib/api";
import { Plus, Edit, Trash2, AlertCircle, Ticket, Eye } from "lucide-react";

interface Voucher {
  id: string;
  code: string;
  type: number;
  value: number;
  expiryDate: string;
  totalQuantity: number;
  usedQuantity: number;
  minOrderAmount?: number;
  maxDiscountAmount?: number;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

interface VoucherFormData {
  code: string;
  type: number;
  value: number;
  expiryDate: string;
  totalQuantity: number;
  minOrderAmount?: number;
  maxDiscountAmount?: number;
  description?: string;
}

interface VoucherUsage {
  totalQuantity: number;
  usedQuantity: number;
  usages: Array<{
    userId: string;
    isUsed: boolean;
    usedAt?: string;
    claimedAt: string;
  }>;
}

const VoucherTypeLabels: Record<number, string> = {
  0: "Giảm cố định",
  1: "Giảm %",
  2: "Miễn phí sạc",
};

const VoucherTypeBadgeVariants: Record<number, "default" | "warning" | "success" | "secondary"> = {
  0: "default",
  1: "warning",
  2: "success",
};

export default function VouchersPage() {
  const queryClient = useQueryClient();
  const [isCreating, setIsCreating] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [viewingUsageId, setViewingUsageId] = useState<string | null>(null);
  const [formError, setFormError] = useState("");
  const [activeFilter, setActiveFilter] = useState<string>("all");
  const [formData, setFormData] = useState<VoucherFormData>({
    code: "",
    type: 0,
    value: 0,
    expiryDate: new Date().toISOString().slice(0, 10),
    totalQuantity: 1,
    minOrderAmount: undefined,
    maxDiscountAmount: undefined,
    description: "",
  });

  // Fetch vouchers
  const { data: vouchers, isLoading } = useQuery<Voucher[]>({
    queryKey: ["vouchers", activeFilter],
    queryFn: async () => {
      const params: Record<string, unknown> = { pageSize: 20 };
      if (activeFilter === "active") params.isActive = true;
      if (activeFilter === "inactive") params.isActive = false;
      const res = await api.get("/admin/vouchers", { params });
      return res.data.data || [];
    },
  });

  // Fetch voucher usage
  const { data: usageData } = useQuery<VoucherUsage>({
    queryKey: ["voucher-usage", viewingUsageId],
    queryFn: async () => {
      const res = await api.get(`/admin/vouchers/${viewingUsageId}/usage`);
      return res.data;
    },
    enabled: !!viewingUsageId,
  });

  // Create voucher
  const createMutation = useMutation({
    mutationFn: async (data: VoucherFormData) => {
      const res = await api.post("/admin/vouchers", {
        ...data,
        expiryDate: new Date(data.expiryDate).toISOString(),
      });
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vouchers"] });
      setIsCreating(false);
      setFormError("");
      resetForm();
    },
    onError: (err: unknown) => {
      handleMutationError(err);
    },
  });

  // Update voucher
  const updateMutation = useMutation({
    mutationFn: async ({ id, data }: { id: string; data: VoucherFormData }) => {
      const res = await api.put(`/admin/vouchers/${id}`, {
        ...data,
        expiryDate: new Date(data.expiryDate).toISOString(),
      });
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vouchers"] });
      setEditingId(null);
      setFormError("");
      resetForm();
    },
    onError: (err: unknown) => {
      handleMutationError(err);
    },
  });

  // Delete voucher
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/admin/vouchers/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vouchers"] });
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
        setFormError("Failed to save voucher. Please check your input.");
      }
    } else {
      setFormError("Unable to connect to server. Please try again.");
    }
  };

  const resetForm = () => {
    setFormError("");
    setFormData({
      code: "",
      type: 0,
      value: 0,
      expiryDate: new Date().toISOString().slice(0, 10),
      totalQuantity: 1,
      minOrderAmount: undefined,
      maxDiscountAmount: undefined,
      description: "",
    });
  };

  const handleEdit = (voucher: Voucher) => {
    setFormError("");
    setEditingId(voucher.id);
    setIsCreating(false);
    setFormData({
      code: voucher.code,
      type: voucher.type,
      value: voucher.value,
      expiryDate: voucher.expiryDate ? voucher.expiryDate.slice(0, 10) : new Date().toISOString().slice(0, 10),
      totalQuantity: voucher.totalQuantity,
      minOrderAmount: voucher.minOrderAmount,
      maxDiscountAmount: voucher.maxDiscountAmount,
      description: voucher.description || "",
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

  const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString("vi-VN");
  };

  const formatValue = (type: number, value: number) => {
    if (type === 1) return `${value}%`;
    if (type === 2) return "N/A";
    return formatCurrency(value);
  };

  return (
    <div className="flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader
          title="Voucher Management"
          description="Create and manage discount vouchers for charging sessions"
        >
          <Button onClick={() => { setIsCreating(true); setEditingId(null); resetForm(); }} disabled={isCreating}>
            <Plus className="mr-2 h-4 w-4" />
            Add Voucher
          </Button>
        </PageHeader>
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* Create/Edit Form Dialog */}
        <Dialog
          open={isCreating || !!editingId}
          onClose={() => { setIsCreating(false); setEditingId(null); resetForm(); }}
          size="xl"
        >
          <DialogHeader onClose={() => { setIsCreating(false); setEditingId(null); resetForm(); }}>
            {editingId ? "Edit Voucher" : "New Voucher"}
          </DialogHeader>
          <form onSubmit={handleSubmit}>
            <DialogContent className="space-y-4">
              {formError && (
                <div className="flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                  <AlertCircle className="h-4 w-4 flex-shrink-0" />
                  <span>{formError}</span>
                </div>
              )}
              <div className="grid gap-4 md:grid-cols-2">
                <Input
                  label="Code"
                  type="text"
                  value={formData.code}
                  onChange={(e) =>
                    setFormData({ ...formData, code: e.target.value.toUpperCase() })
                  }
                  placeholder="e.g., SUMMER2026"
                  maxLength={50}
                  required
                  disabled={!!editingId}
                />
                <div className="space-y-1">
                  <label className="text-sm font-medium">Type</label>
                  <select
                    value={formData.type}
                    onChange={(e) =>
                      setFormData({ ...formData, type: parseInt(e.target.value) })
                    }
                    className="h-10 w-full rounded-md border bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                    required
                  >
                    <option value={0}>Giảm cố định</option>
                    <option value={1}>Giảm %</option>
                    <option value={2}>Miễn phí sạc</option>
                  </select>
                </div>
              </div>
              <div className="grid gap-4 md:grid-cols-3">
                <Input
                  label="Value"
                  type="number"
                  value={formData.value}
                  onChange={(e) =>
                    setFormData({ ...formData, value: parseFloat(e.target.value) || 0 })
                  }
                  min={0}
                  step={formData.type === 1 ? 1 : 100}
                  suffix={formData.type === 1 ? "%" : "đ"}
                  required
                  disabled={formData.type === 2}
                />
                <Input
                  label="Expiry Date"
                  type="date"
                  value={formData.expiryDate}
                  onChange={(e) =>
                    setFormData({ ...formData, expiryDate: e.target.value })
                  }
                  required
                />
                <Input
                  label="Total Quantity"
                  type="number"
                  value={formData.totalQuantity}
                  onChange={(e) =>
                    setFormData({ ...formData, totalQuantity: parseInt(e.target.value) || 1 })
                  }
                  min={1}
                  required
                />
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <Input
                  label="Min Order Amount"
                  type="number"
                  value={formData.minOrderAmount ?? ""}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      minOrderAmount: e.target.value ? parseFloat(e.target.value) : undefined,
                    })
                  }
                  min={0}
                  step={1000}
                  suffix="đ"
                  hint="Optional"
                />
                <Input
                  label="Max Discount Amount"
                  type="number"
                  value={formData.maxDiscountAmount ?? ""}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      maxDiscountAmount: e.target.value ? parseFloat(e.target.value) : undefined,
                    })
                  }
                  min={0}
                  step={1000}
                  suffix="đ"
                  hint="Optional"
                />
              </div>
              <Input
                label="Description"
                type="text"
                value={formData.description ?? ""}
                onChange={(e) =>
                  setFormData({ ...formData, description: e.target.value })
                }
                placeholder="e.g., Summer promotion - 20% off all charging sessions"
              />
            </DialogContent>
            <DialogFooter>
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
              <Button
                type="submit"
                disabled={createMutation.isPending || updateMutation.isPending}
              >
                {editingId ? "Update" : "Create"} Voucher
              </Button>
            </DialogFooter>
          </form>
        </Dialog>

        {/* Usage Detail Dialog */}
        <Dialog
          open={!!viewingUsageId && !!usageData}
          onClose={() => setViewingUsageId(null)}
          size="lg"
        >
          <DialogHeader onClose={() => setViewingUsageId(null)}>
            Voucher Usage ({usageData?.usedQuantity ?? 0}/{usageData?.totalQuantity ?? 0})
          </DialogHeader>
          <DialogContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="px-4 py-3 text-left text-sm font-medium">User ID</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Status</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Used At</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Claimed At</th>
                  </tr>
                </thead>
                <tbody>
                  {usageData && usageData.usages.length > 0 ? (
                    usageData.usages.map((usage, idx) => (
                      <tr key={idx} className="border-b hover:bg-muted/50">
                        <td className="px-4 py-3 font-mono text-xs">{usage.userId.substring(0, 8)}...</td>
                        <td className="px-4 py-3 text-sm">
                          <Badge variant={usage.isUsed ? "success" : "secondary"}>
                            {usage.isUsed ? "Used" : "Claimed"}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-sm">{usage.usedAt ? formatDate(usage.usedAt) : "—"}</td>
                        <td className="px-4 py-3 text-sm">{formatDate(usage.claimedAt)}</td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan={4}>
                        <EmptyState
                          icon={Ticket}
                          title="No usage records"
                          description="No one has claimed or used this voucher yet."
                        />
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </DialogContent>
        </Dialog>

        {/* Filter */}
        <div className="flex items-center gap-2">
          <Ticket className="h-4 w-4 text-muted-foreground" />
          <Button
            variant={activeFilter === "all" ? "default" : "outline"}
            size="sm"
            onClick={() => setActiveFilter("all")}
          >
            All
          </Button>
          <Button
            variant={activeFilter === "active" ? "default" : "outline"}
            size="sm"
            onClick={() => setActiveFilter("active")}
          >
            Active
          </Button>
          <Button
            variant={activeFilter === "inactive" ? "default" : "outline"}
            size="sm"
            onClick={() => setActiveFilter("inactive")}
          >
            Inactive
          </Button>
        </div>

        {/* Vouchers Table */}
        {isLoading ? (
          <SkeletonTable rows={5} cols={7} />
        ) : vouchers && vouchers.length > 0 ? (
          <Card>
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-4 py-3 text-left text-sm font-medium">Code</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Type</th>
                      <th className="px-4 py-3 text-right text-sm font-medium">Value</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Expiry Date</th>
                      <th className="px-4 py-3 text-right text-sm font-medium">Quantity</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Status</th>
                      <th className="px-4 py-3 text-right text-sm font-medium">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {vouchers.map((voucher) => (
                      <tr key={voucher.id} className="border-b hover:bg-muted/50">
                        <td className="px-4 py-3">
                          <span className="font-mono font-medium">{voucher.code}</span>
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={VoucherTypeBadgeVariants[voucher.type] ?? "secondary"}>
                            {VoucherTypeLabels[voucher.type] ?? "Unknown"}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-right tabular-nums font-medium">
                          {formatValue(voucher.type, voucher.value)}
                        </td>
                        <td className="px-4 py-3 text-sm">{formatDate(voucher.expiryDate)}</td>
                        <td className="px-4 py-3 text-right text-sm tabular-nums">
                          {voucher.usedQuantity}/{voucher.totalQuantity}
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={voucher.isActive ? "success" : "secondary"}>
                            {voucher.isActive ? "Active" : "Inactive"}
                          </Badge>
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex items-center justify-end gap-1">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => setViewingUsageId(voucher.id)}
                              title="View usage"
                            >
                              <Eye className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleEdit(voucher)}
                              title="Edit"
                            >
                              <Edit className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="destructive"
                              size="sm"
                              onClick={() => {
                                if (confirm("Delete this voucher?")) {
                                  deleteMutation.mutate(voucher.id);
                                }
                              }}
                              title="Delete"
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardContent className="p-0">
              <EmptyState
                icon={Ticket}
                title="No vouchers found"
                description="Create your first voucher to get started."
                action={{
                  label: "Add Voucher",
                  onClick: () => { setIsCreating(true); setEditingId(null); resetForm(); },
                }}
              />
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
