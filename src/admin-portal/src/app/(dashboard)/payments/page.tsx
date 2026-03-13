"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { StatusBadge } from "@/components/ui/status-badge";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { SkeletonTable } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { PAYMENT_STATUS, PAYMENT_GATEWAY_LABELS } from "@/lib/constants";
import { formatCurrency, formatDateTime } from "@/lib/utils";
import { api } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
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

interface PaymentStats {
  todayTotal: number;
  todayCount: number;
  monthTotal: number;
  monthCount: number;
  pendingCount: number;
  failedCount: number;
}

export default function PaymentsPage() {
  const hasAccess = useRequirePermission("KLC.Payments");
  const { t } = useTranslation();
  const router = useRouter();
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

  const closeRefundDialog = () => {
    setRefundTarget(null);
    setRefundReason("");
  };

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title={t("payments.title")}
        description={t("payments.description")}
        className="sticky top-0 z-10 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 -mx-1 px-1 py-2"
      >
        <Button variant="outline">
          <Download className="mr-2 h-4 w-4" aria-hidden="true" />
          {t("common.export")}
        </Button>
      </PageHeader>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatCard
          label={t("payments.todaysRevenue")}
          value={formatCurrency(stats?.todayTotal || 0)}
          icon={DollarSign}
          iconColor="bg-green-50 text-green-600"
        />
        <StatCard
          label={t("payments.monthlyRevenue")}
          value={formatCurrency(stats?.monthTotal || 0)}
          icon={TrendingUp}
          iconColor="bg-blue-50 text-blue-600"
        />
        <StatCard
          label={t("payments.pending")}
          value={stats?.pendingCount || 0}
          icon={CreditCard}
          iconColor="bg-amber-50 text-amber-600"
        />
        <StatCard
          label={t("payments.failed")}
          value={stats?.failedCount || 0}
          icon={CreditCard}
          iconColor="bg-red-50 text-red-600"
        />
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-wrap gap-4">
            <div className="flex-1 min-w-[200px]">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
                <input
                  type="text"
                  placeholder={t("payments.searchPlaceholder")}
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full rounded-md border pl-10 pr-3 py-2 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500"
                  aria-label={t("payments.searchPlaceholder")}
                />
              </div>
            </div>
            <select
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value); resetPagination(); }}
              className="rounded-md border px-3 py-2 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500"
              aria-label={t("payments.filterByStatus")}
            >
              <option value="all">{t("payments.allStatus")}</option>
              {Object.entries(PAYMENT_STATUS).map(([key, config]) => (
                <option key={key} value={key}>{config.label}</option>
              ))}
            </select>
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
              <input
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="rounded-md border px-3 py-2 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500"
                aria-label={t("payments.dateFrom")}
              />
              <span>{t("payments.to")}</span>
              <input
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="rounded-md border px-3 py-2 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500"
                aria-label={t("payments.dateTo")}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Payments Table */}
      {isLoading ? (
        <SkeletonTable rows={8} cols={8} />
      ) : payments.length === 0 ? (
        <Card>
          <CardContent className="p-0">
            <EmptyState
              icon={CreditCard}
              title={t("payments.noPaymentsFound")}
              description={t("payments.noPaymentsDescription")}
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
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("payments.transaction")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("payments.station")}</th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("payments.session")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-right text-sm font-medium">
                      {t("payments.amount")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("payments.method")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("common.status")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">{t("payments.date")}</th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("common.actions")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {payments.map((payment) => (
                    <tr key={payment.id} className="border-b hover:bg-muted/50">
                      <td className="px-4 py-3">
                        <div className="font-mono text-sm">
                          {payment.referenceCode || payment.id.slice(0, 8)}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <MapPin className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
                          <span>{payment.stationName || "—"}</span>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <Zap className="h-4 w-4 text-yellow-500" aria-hidden="true" />
                          <span className="font-mono text-sm">
                            {payment.sessionId?.slice(0, 8) || "—"}
                          </span>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-right tabular-nums font-semibold">
                        {formatCurrency(payment.amount)}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <CreditCard className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
                          <span>{PAYMENT_GATEWAY_LABELS[payment.gateway] ?? "—"}</span>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <StatusBadge type="payment" value={payment.status} />
                      </td>
                      <td className="px-4 py-3 text-sm text-muted-foreground">
                        {formatDateTime(payment.creationTime)}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex gap-1">
                          <Button variant="ghost" size="sm" title={t("payments.viewDetails")} aria-label={t("payments.viewDetails")} onClick={() => router.push(`/payments/${payment.id}`)}>
                            <FileText className="h-4 w-4" />
                          </Button>
                          {payment.status === 2 && (
                            <Button
                              variant="ghost"
                              size="sm"
                              title={t("payments.refund")}
                              aria-label={t("payments.refund")}
                              onClick={() => setRefundTarget(payment)}
                            >
                              <RotateCcw className="h-4 w-4 text-orange-500" />
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
                  {totalCount} {t("payments.totalPayments")}
                </div>
                <div className="flex gap-2">
                  {cursorStack.length > 0 && (
                    <Button
                      variant="outline"
                      size="sm"
                      aria-label={t("common.previous")}
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
                      aria-label={t("common.next")}
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
      )}

      {/* Refund Confirmation Dialog */}
      <Dialog open={!!refundTarget} onClose={closeRefundDialog}>
        <DialogHeader onClose={closeRefundDialog}>{t("payments.confirmRefund")}</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-2">
            {t("payments.refund")} <span className="font-semibold">{formatCurrency(refundTarget?.amount)}</span>{" "}
            {t("payments.transaction").toLowerCase()} <span className="font-mono text-sm">{refundTarget?.referenceCode || refundTarget?.id.slice(0, 8)}</span>?
          </p>
          <p className="text-sm text-muted-foreground mb-4">
            {t("payments.refundWalletNote")}
          </p>
          <div>
            <label className="text-sm font-medium mb-1 block">{t("payments.refundReason")}</label>
            <input
              type="text"
              value={refundReason}
              onChange={(e) => setRefundReason(e.target.value)}
              placeholder={t("payments.refundReasonPlaceholder")}
              className="w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500"
            />
          </div>
          {refundMutation.isError && (
            <p className="text-sm text-red-600 mt-3">
              {(refundMutation.error as Error)?.message || t("payments.refundFailed")}
            </p>
          )}
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={closeRefundDialog}>
            {t("common.cancel")}
          </Button>
          <Button
            variant="destructive"
            disabled={refundMutation.isPending}
            onClick={() => refundTarget && refundMutation.mutate({ id: refundTarget.id, reason: refundReason })}
          >
            {refundMutation.isPending ? t("payments.processing") : t("payments.refund")}
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  );
}
