"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
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
  CheckCircle2,
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

const EInvoiceStatusLabels: Record<number, string> = {
  0: "Pending", 1: "Processing", 2: "Issued", 3: "Failed", 4: "Cancelled",
};

const EInvoiceProviderLabels: Record<number, string> = {
  0: "MISA", 1: "Viettel", 2: "VNPT",
};

interface EInvoiceStats {
  totalIssued: number;
  totalPending: number;
  totalFailed: number;
  totalCancelled: number;
  totalAmount: number;
}

export default function EInvoicesPage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState("all");
  const [providerFilter, setProviderFilter] = useState("all");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 20;

  // Fetch e-invoices
  const { data: invoicesData, isLoading } = useQuery({
    queryKey: [
      "e-invoices",
      statusFilter,
      providerFilter,
      dateFrom,
      dateTo,
      searchQuery,
      currentPage,
    ],
    queryFn: async () => {
      const params: Record<string, string | number> = {
        skipCount: (currentPage - 1) * pageSize,
        maxResultCount: pageSize,
      };
      if (statusFilter !== "all") params.status = statusFilter;
      if (providerFilter !== "all") params.provider = providerFilter;
      if (dateFrom) params.fromDate = dateFrom;
      if (dateTo) params.toDate = dateTo;
      if (searchQuery) params.search = searchQuery;

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
  const totalPages = Math.ceil(totalCount / pageSize);

  // Compute stats from fetched data
  const stats: EInvoiceStats = {
    totalIssued: invoices.filter((i) => i.status === 2).length,
    totalPending: invoices.filter((i) => i.status === 0 || i.status === 1).length,
    totalFailed: invoices.filter((i) => i.status === 3).length,
    totalCancelled: invoices.filter((i) => i.status === 4).length,
    totalAmount: invoices.reduce((sum, i) => sum + (i.totalAmount || 0), 0),
  };

  const getStatusIcon = (status: number) => {
    switch (status) {
      case 2: return <CheckCircle2 className="h-4 w-4 text-green-500" />;    // Issued
      case 0: return <Clock className="h-4 w-4 text-yellow-500" />;          // Pending
      case 1: return <Clock className="h-4 w-4 text-blue-500" />;            // Processing
      case 3: return <AlertTriangle className="h-4 w-4 text-red-500" />;     // Failed
      case 4: return <XCircle className="h-4 w-4 text-gray-500" />;          // Cancelled
      default: return null;
    }
  };

  const getStatusColor = (status: number): "success" | "warning" | "destructive" | "secondary" | "default" => {
    switch (status) {
      case 2: return "success";     // Issued
      case 0: return "warning";     // Pending
      case 1: return "default";     // Processing
      case 3: return "destructive"; // Failed
      case 4: return "secondary";   // Cancelled
      default: return "secondary";
    }
  };

  const formatCurrency = (value?: number | null) => {
    return (value ?? 0).toLocaleString("vi-VN") + "đ";
  };

  const formatDate = (dateString?: string | null) => {
    if (!dateString) return "—";
    return new Date(dateString).toLocaleString("vi-VN");
  };

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
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">E-Invoices</h1>
          <p className="text-muted-foreground">
            Manage electronic invoices (Hóa đơn điện tử)
          </p>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Issued</CardTitle>
            <CheckCircle2 className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              {stats?.totalIssued || 0}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Pending</CardTitle>
            <Clock className="h-4 w-4 text-yellow-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">
              {stats?.totalPending || 0}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Failed</CardTitle>
            <AlertTriangle className="h-4 w-4 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">
              {stats?.totalFailed || 0}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Cancelled</CardTitle>
            <XCircle className="h-4 w-4 text-gray-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-gray-600">
              {stats?.totalCancelled || 0}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Amount</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {formatCurrency(stats?.totalAmount || 0)}
            </div>
          </CardContent>
        </Card>
      </div>

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
                  className="w-full rounded-md border pl-10 pr-3 py-2"
                />
              </div>
            </div>
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="rounded-md border px-3 py-2"
            >
              <option value="all">All Status</option>
              <option value="0">Pending</option>
              <option value="1">Processing</option>
              <option value="2">Issued</option>
              <option value="3">Failed</option>
              <option value="4">Cancelled</option>
            </select>
            <select
              value={providerFilter}
              onChange={(e) => setProviderFilter(e.target.value)}
              className="rounded-md border px-3 py-2"
            >
              <option value="all">All Providers</option>
              <option value="0">MISA</option>
              <option value="1">Viettel</option>
              <option value="2">VNPT</option>
            </select>
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-muted-foreground" />
              <input
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="rounded-md border px-3 py-2"
              />
              <span>to</span>
              <input
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="rounded-md border px-3 py-2"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Invoices Table */}
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
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Amount
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">
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
                {isLoading ? (
                  <tr>
                    <td colSpan={8} className="px-4 py-8 text-center">
                      Loading...
                    </td>
                  </tr>
                ) : invoices.length > 0 ? (
                  invoices.map((invoice) => (
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
                        <Badge variant="outline">{EInvoiceProviderLabels[invoice.provider] ?? invoice.provider}</Badge>
                      </td>
                      <td className="px-4 py-3 font-semibold">
                        {formatCurrency(invoice.totalAmount)}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {invoice.retryCount ?? 0}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          {getStatusIcon(invoice.status)}
                          <Badge variant={getStatusColor(invoice.status)}>
                            {EInvoiceStatusLabels[invoice.status] || "Unknown"}
                          </Badge>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-sm text-muted-foreground">
                        {formatDate(invoice.issuedAt || invoice.creationTime)}
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
                              onClick={() => {
                                if (confirm("Cancel this e-invoice?")) {
                                  cancelMutation.mutate(invoice.id);
                                }
                              }}
                              title="Cancel"
                            >
                              <XCircle className="h-4 w-4 text-red-500" />
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td
                      colSpan={8}
                      className="px-4 py-8 text-center text-muted-foreground"
                    >
                      No e-invoices found
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between border-t px-4 py-3">
              <div className="text-sm text-muted-foreground">
                Showing {(currentPage - 1) * pageSize + 1} -{" "}
                {Math.min(currentPage * pageSize, totalCount)} of {totalCount}
              </div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                  disabled={currentPage === 1}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                  disabled={currentPage === totalPages}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
