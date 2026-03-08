"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { api } from "@/lib/api";
import {
  CreditCard,
  DollarSign,
  TrendingUp,
  Calendar,
  Search,
  Download,
  ChevronLeft,
  ChevronRight,
  FileText,
  MapPin,
  Zap,
  RotateCcw,
  X,
} from "lucide-react";

interface Payment {
  id: string;
  sessionId: string;
  amount: number;
  status: number;
  gateway: number;
  referenceCode?: string;
  stationName?: string;
  creationTime?: string;
}

const PaymentStatusLabels: Record<number, string> = {
  0: "Pending", 1: "Processing", 2: "Completed", 3: "Failed", 4: "Refunded", 5: "Cancelled",
};

const PaymentGatewayLabels: Record<number, string> = {
  0: "ZaloPay", 1: "MoMo", 2: "OnePay", 3: "Wallet", 4: "VnPay", 5: "QR Payment", 6: "Voucher", 7: "Urbox",
};

interface PaymentStats {
  todayTotal: number;
  todayCount: number;
  monthTotal: number;
  monthCount: number;
  pendingCount: number;
  failedCount: number;
}

export default function PaymentsPage() {
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [cursor, setCursor] = useState<string | null>(null);
  const [cursorStack, setCursorStack] = useState<(string | null)[]>([]);
  const [refundTarget, setRefundTarget] = useState<Payment | null>(null);
  const [refundReason, setRefundReason] = useState("");
  const pageSize = 20;
  const queryClient = useQueryClient();

  const resetPagination = () => { setCursor(null); setCursorStack([]); };

  // Fetch payments
  const { data: paymentsData, isLoading } = useQuery({
    queryKey: ["payments", statusFilter, dateFrom, dateTo, searchQuery, cursor],
    queryFn: async () => {
      const params: Record<string, string | number> = {
        maxResultCount: pageSize,
      };
      if (statusFilter !== "all") params.status = Number(statusFilter);
      if (dateFrom) params.fromDate = dateFrom;
      if (dateTo) params.toDate = dateTo;
      if (cursor) params.cursor = cursor;

      const res = await api.get("/payments/history", { params });
      return res.data;
    },
  });

  const payments: Payment[] = paymentsData?.items || [];
  const totalCount = paymentsData?.totalCount || 0;

  // Compute stats from fetched data
  const stats: PaymentStats = {
    todayTotal: payments.reduce((sum, p) => sum + (p.amount || 0), 0),
    todayCount: payments.length,
    monthTotal: payments.reduce((sum, p) => sum + (p.amount || 0), 0),
    monthCount: totalCount,
    pendingCount: payments.filter((p) => p.status === 0).length,
    failedCount: payments.filter((p) => p.status === 3).length,
  };

  const refundMutation = useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason: string }) => {
      const res = await api.post(`/payments/${id}/refund`, { reason });
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["payments"] });
      setRefundTarget(null);
      setRefundReason("");
    },
  });

  const getStatusColor = (status: number): "success" | "warning" | "destructive" | "secondary" | "default" => {
    switch (status) {
      case 2: return "success";    // Completed
      case 0: return "warning";    // Pending
      case 1: return "default";    // Processing
      case 3: return "destructive"; // Failed
      case 4: return "secondary";  // Refunded
      case 5: return "secondary";  // Cancelled
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

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Payments</h1>
          <p className="text-muted-foreground">
            View and manage payment transactions
          </p>
        </div>
        <Button variant="outline">
          <Download className="mr-2 h-4 w-4" />
          Export
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Today's Revenue</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {formatCurrency(stats?.todayTotal || 0)}
            </div>
            <p className="text-xs text-muted-foreground">
              {stats?.todayCount || 0} transactions
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Monthly Revenue</CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {formatCurrency(stats?.monthTotal || 0)}
            </div>
            <p className="text-xs text-muted-foreground">
              {stats?.monthCount || 0} transactions
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Pending</CardTitle>
            <CreditCard className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">
              {stats?.pendingCount || 0}
            </div>
            <p className="text-xs text-muted-foreground">Awaiting confirmation</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Failed</CardTitle>
            <CreditCard className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">
              {stats?.failedCount || 0}
            </div>
            <p className="text-xs text-muted-foreground">Requires attention</p>
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
                  placeholder="Search by transaction ID or user..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full rounded-md border pl-10 pr-3 py-2"
                />
              </div>
            </div>
            <select
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value); resetPagination(); }}
              className="rounded-md border px-3 py-2"
            >
              <option value="all">All Status</option>
              <option value="0">Pending</option>
              <option value="1">Processing</option>
              <option value="2">Completed</option>
              <option value="3">Failed</option>
              <option value="4">Refunded</option>
              <option value="5">Cancelled</option>
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

      {/* Payments Table */}
      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="border-b bg-muted/50">
                <tr>
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Transaction
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">Station</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Session
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Amount
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Method
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Status
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">Date</th>
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
                ) : payments.length > 0 ? (
                  payments.map((payment) => (
                    <tr key={payment.id} className="border-b hover:bg-muted/50">
                      <td className="px-4 py-3">
                        <div className="font-mono text-sm">
                          {payment.referenceCode || payment.id.slice(0, 8)}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <MapPin className="h-4 w-4 text-muted-foreground" />
                          <span>{payment.stationName || "—"}</span>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <Zap className="h-4 w-4 text-yellow-500" />
                          <span className="font-mono text-sm">
                            {payment.sessionId?.slice(0, 8) || "—"}
                          </span>
                        </div>
                      </td>
                      <td className="px-4 py-3 font-semibold">
                        {formatCurrency(payment.amount)}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <CreditCard className="h-4 w-4 text-muted-foreground" />
                          <span>{PaymentGatewayLabels[payment.gateway] ?? "—"}</span>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant={getStatusColor(payment.status)}>
                          {PaymentStatusLabels[payment.status] || "Unknown"}
                        </Badge>
                      </td>
                      <td className="px-4 py-3 text-sm text-muted-foreground">
                        {formatDate(payment.creationTime)}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex gap-1">
                          <Button variant="ghost" size="sm" title="View details">
                            <FileText className="h-4 w-4" />
                          </Button>
                          {payment.status === 2 && (
                            <Button
                              variant="ghost"
                              size="sm"
                              title="Refund"
                              onClick={() => setRefundTarget(payment)}
                            >
                              <RotateCcw className="h-4 w-4 text-orange-500" />
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
                      No payments found
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {(totalCount > pageSize || cursorStack.length > 0) && (
            <div className="flex items-center justify-between border-t px-4 py-3">
              <div className="text-sm text-muted-foreground">
                {totalCount} total payments
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
                {payments.length === pageSize && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      const lastId = payments[payments.length - 1]?.id;
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

      {/* Refund Confirmation Dialog */}
      {refundTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-lg">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold">Confirm Refund</h3>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => { setRefundTarget(null); setRefundReason(""); }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
            <p className="text-sm text-muted-foreground mb-2">
              Refund <span className="font-semibold">{formatCurrency(refundTarget.amount)}</span> for
              transaction <span className="font-mono">{refundTarget.referenceCode || refundTarget.id.slice(0, 8)}</span>?
            </p>
            <p className="text-sm text-muted-foreground mb-4">
              This will credit the amount back to the user&apos;s wallet.
            </p>
            <div className="mb-4">
              <label className="text-sm font-medium mb-1 block">Reason (optional)</label>
              <input
                type="text"
                value={refundReason}
                onChange={(e) => setRefundReason(e.target.value)}
                placeholder="e.g. Customer request, billing error..."
                className="w-full rounded-md border px-3 py-2 text-sm"
              />
            </div>
            {refundMutation.isError && (
              <p className="text-sm text-red-600 mb-3">
                {(refundMutation.error as Error)?.message || "Refund failed. Please try again."}
              </p>
            )}
            <div className="flex justify-end gap-2">
              <Button
                variant="outline"
                onClick={() => { setRefundTarget(null); setRefundReason(""); }}
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                disabled={refundMutation.isPending}
                onClick={() => refundMutation.mutate({ id: refundTarget.id, reason: refundReason })}
              >
                {refundMutation.isPending ? "Processing..." : "Refund"}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
