"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { StatusBadge } from "@/components/ui/status-badge";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { SkeletonCard, SkeletonTable } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { EINVOICE_STATUS, EINVOICE_PROVIDER_LABELS } from "@/lib/constants";
import { formatCurrency, formatDateTime } from "@/lib/utils";
import { api } from "@/lib/api";
import {
  Receipt,
  Search,
  Download,
  Calendar,
  RefreshCw,
  XCircle,
  ChevronLeft,
  ChevronRight,
  DollarSign,
  CheckCircle,
  Clock,
  AlertTriangle,
} from "lucide-react";

interface EInvoice {
  id: string;
  invoiceId: string;
  invoiceNumber: string;
  eInvoiceNumber?: string;
  provider: number;
  status: number;
  totalAmount: number;
  issuedAt?: string;
  retryCount?: number;
  creationTime?: string;
  stationName?: string;
}

export default function EInvoicesPage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState("all");
  const [providerFilter, setProviderFilter] = useState("all");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [cursor, setCursor] = useState<string | null>(null);
  const [cursorStack, setCursorStack] = useState<(string | null)[]>([]);
  const pageSize = 20;

  const resetPagination = () => { setCursor(null); setCursorStack([]); };

  // Fetch e-invoices
  const { data: invoicesData, isLoading } = useQuery({
    queryKey: [
      "e-invoices",
      statusFilter,
      providerFilter,
      dateFrom,
      dateTo,
      searchQuery,
      cursor,
    ],
    queryFn: async () => {
      const params: Record<string, string | number> = {
        maxResultCount: pageSize,
      };
      if (statusFilter !== "all") params.status = statusFilter;
      if (providerFilter !== "all") params.provider = providerFilter;
      if (dateFrom) params.fromDate = dateFrom;
      if (dateTo) params.toDate = dateTo;
      if (searchQuery) params.search = searchQuery;
      if (cursor) params.cursor = cursor;

      const res = await api.get("/e-invoices", { params });
      return res.data;
    },
  });

  // Retry failed invoice
  const retryMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.post(`/e-invoices/${id}/retry`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["e-invoices"] });
    },
  });

  // Cancel invoice
  const cancelMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.post(`/e-invoices/${id}/cancel`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["e-invoices"] });
    },
  });

  const invoices: EInvoice[] = invoicesData?.items || [];
  const totalCount = invoicesData?.totalCount || 0;

  // Compute stats from fetched data
  const stats = {
    totalIssued: invoices.filter((i) => i.status === 2).length,
    totalPending: invoices.filter((i) => i.status === 0 || i.status === 1).length,
    totalFailed: invoices.filter((i) => i.status === 3).length,
    totalCancelled: invoices.filter((i) => i.status === 4).length,
    totalAmount: invoices.reduce((sum, i) => sum + (i.totalAmount || 0), 0),
  };

  // Cancel confirmation dialog state
  const [cancelTarget, setCancelTarget] = useState<string | null>(null);

  const handleDownloadPdf = async (invoiceId: string) => {
    try {
      const res = await api.get(`/e-invoices/${invoiceId}/pdf-url`);
      const url = res.data.pdfUrl || res.data.url;
      if (url) {
        window.open(url, "_blank");
      }
    } catch (error) {
      console.error("Failed to get PDF URL:", error);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title="E-Invoices"
        description="Manage electronic invoices (Hóa đơn điện tử)"
        className="sticky top-0 z-10 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 -mx-1 px-1 py-2"
      />

      {/* Stats Cards */}
      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
          {Array.from({ length: 5 }).map((_, i) => (
            <SkeletonCard key={i} />
          ))}
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
          <StatCard
            label="Issued"
            value={stats.totalIssued}
            icon={CheckCircle}
            iconColor="bg-green-50 text-green-600"
          />
          <StatCard
            label="Pending"
            value={stats.totalPending}
            icon={Clock}
            iconColor="bg-amber-50 text-amber-600"
          />
          <StatCard
            label="Failed"
            value={stats.totalFailed}
            icon={AlertTriangle}
            iconColor="bg-red-50 text-red-600"
          />
          <StatCard
            label="Cancelled"
            value={stats.totalCancelled}
            icon={XCircle}
            iconColor="bg-gray-50 text-gray-500"
          />
          <StatCard
            label="Total Amount"
            value={formatCurrency(stats.totalAmount)}
            icon={DollarSign}
            iconColor="bg-primary/10 text-primary"
          />
        </div>
      )}

      {/* Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-wrap gap-4">
            <div className="flex-1 min-w-[200px]">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Search by invoice number..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full rounded-md border pl-10 pr-3 py-2 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500"
                />
              </div>
            </div>
            <select
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value); resetPagination(); }}
              className="rounded-md border px-3 py-2 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500"
            >
              <option value="all">All Status</option>
              {Object.entries(EINVOICE_STATUS).map(([key, config]) => (
                <option key={key} value={key}>{config.label}</option>
              ))}
            </select>
            <select
              value={providerFilter}
              onChange={(e) => { setProviderFilter(e.target.value); resetPagination(); }}
              className="rounded-md border px-3 py-2 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500"
            >
              <option value="all">All Providers</option>
              {Object.entries(EINVOICE_PROVIDER_LABELS).map(([key, label]) => (
                <option key={key} value={key}>{label}</option>
              ))}
            </select>
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-muted-foreground" />
              <input
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="rounded-md border px-3 py-2 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500"
              />
              <span>to</span>
              <input
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="rounded-md border px-3 py-2 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Invoices Table */}
      {isLoading ? (
        <SkeletonTable rows={8} cols={8} />
      ) : invoices.length === 0 ? (
        <Card>
          <CardContent className="p-0">
            <EmptyState
              icon={Receipt}
              title="No e-invoices found"
              description="Try adjusting your filters or search query."
            />
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b bg-muted/50">
                  <tr>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Invoice #
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Station
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Provider
                    </th>
                    <th className="px-4 py-3 text-right text-sm font-medium">
                      Amount
                    </th>
                    <th className="px-4 py-3 text-right text-sm font-medium">
                      Retries
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Status
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Date
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {invoices.map((invoice) => (
                    <tr key={invoice.id} className="border-b hover:bg-muted/50">
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <Receipt className="h-4 w-4 text-muted-foreground" />
                          <div>
                            <p className="font-mono font-medium">
                              {invoice.invoiceNumber}
                            </p>
                            {invoice.eInvoiceNumber && (
                              <p className="text-xs text-muted-foreground">
                                {invoice.eInvoiceNumber}
                              </p>
                            )}
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <span>{invoice.stationName || "—"}</span>
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant="outline">{EINVOICE_PROVIDER_LABELS[invoice.provider] ?? invoice.provider}</Badge>
                      </td>
                      <td className="px-4 py-3 text-right tabular-nums font-semibold">
                        {formatCurrency(invoice.totalAmount)}
                      </td>
                      <td className="px-4 py-3 text-right tabular-nums text-sm">
                        {invoice.retryCount ?? 0}
                      </td>
                      <td className="px-4 py-3">
                        <StatusBadge type="eInvoice" value={invoice.status} showIcon />
                      </td>
                      <td className="px-4 py-3 text-sm text-muted-foreground">
                        {formatDateTime(invoice.issuedAt || invoice.creationTime)}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex gap-1">
                          {invoice.status === 2 && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleDownloadPdf(invoice.id)}
                              title="Download PDF"
                            >
                              <Download className="h-4 w-4" />
                            </Button>
                          )}
                          {invoice.status === 3 && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => retryMutation.mutate(invoice.id)}
                              title="Retry"
                            >
                              <RefreshCw className="h-4 w-4" />
                            </Button>
                          )}
                          {invoice.status === 2 && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => setCancelTarget(invoice.id)}
                              title="Cancel"
                            >
                              <XCircle className="h-4 w-4 text-destructive" />
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {(totalCount > pageSize || cursorStack.length > 0) && (
              <div className="flex items-center justify-between border-t px-4 py-3">
                <div className="text-sm text-muted-foreground">
                  {totalCount} total e-invoices
                </div>
                <div className="flex gap-2">
                  {cursorStack.length > 0 && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        const prev = [...cursorStack];
                        const prevCursor = prev.pop()!;
                        setCursorStack(prev);
                        setCursor(prevCursor);
                      }}
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </Button>
                  )}
                  {invoices.length === pageSize && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        const lastId = invoices[invoices.length - 1]?.id;
                        if (lastId) {
                          setCursorStack([...cursorStack, cursor]);
                          setCursor(lastId);
                        }
                      }}
                    >
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Cancel Confirmation Dialog */}
      <Dialog open={!!cancelTarget} onClose={() => setCancelTarget(null)}>
        <DialogHeader onClose={() => setCancelTarget(null)}>Cancel E-Invoice</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground">
            Are you sure you want to cancel this e-invoice? This action cannot be undone.
          </p>
          {cancelMutation.isError && (
            <p className="text-sm text-destructive mt-3">
              {(cancelMutation.error as Error)?.message || "Cancellation failed. Please try again."}
            </p>
          )}
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setCancelTarget(null)}>
            No, Keep It
          </Button>
          <Button
            variant="destructive"
            disabled={cancelMutation.isPending}
            onClick={() => {
              if (cancelTarget) {
                cancelMutation.mutate(cancelTarget, {
                  onSuccess: () => setCancelTarget(null),
                });
              }
            }}
          >
            {cancelMutation.isPending ? "Cancelling..." : "Yes, Cancel"}
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  );
}
