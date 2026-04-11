"use client";

import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { StatusBadge } from "@/components/ui/status-badge";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonTable, SkeletonCard } from "@/components/ui/skeleton";
import { Tabs } from "@/components/ui/tabs";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { api } from "@/lib/api";
import { formatDate, formatDateTime } from "@/lib/utils";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import {
  Search,
  UserX,
  UserCheck,
  AlertCircle,
  ChevronDown,
  ChevronUp,
  Users,
  Wallet,
  Receipt,
  Zap,
  CheckCircle,
} from "lucide-react";

interface MobileUser {
  id: string;
  fullName: string;
  phoneNumber?: string;
  email?: string;
  walletBalance: number;
  membershipTier: number;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
}

interface MobileUserDetail extends MobileUser {
  avatarUrl?: string;
  sessionCount?: number;
  totalSpent?: number;
}

interface WalletTransaction {
  id: string;
  type: number;
  amount: number;
  balanceAfter: number;
  description?: string;
  createdAt: string;
  status: number;
}

interface UserSession {
  id: string;
  stationId?: string;
  status: number;
  startTime: string;
  endTime?: string;
  totalEnergyKwh?: number;
  totalCost?: number;
  createdAt: string;
}

interface CursorPagination {
  nextCursor?: string;
  hasMore: boolean;
  pageSize: number;
}

interface CursorPagedResult<T> {
  data: T[];
  pagination: CursorPagination;
}

const MembershipTierLabels: Record<number, string> = {
  0: "Standard",
  1: "Silver",
  2: "Gold",
  3: "Platinum",
};

const MembershipTierVariant: Record<number, "secondary" | "default" | "warning" | "success"> = {
  0: "secondary",
  1: "default",
  2: "warning",
  3: "success",
};

const TransactionTypeLabels: Record<number, string> = {
  0: "TopUp",
  1: "Payment",
  2: "Refund",
  3: "Bonus",
};

const formatCurrency = (value?: number | null) => {
  return (value ?? 0).toLocaleString("vi-VN") + "đ";
};



export default function MobileUsersPage() {
  const hasAccess = useRequirePermission("KLC.MobileUsers");
  const canAdjustWallet = useHasPermission("KLC.MobileUsers.WalletAdjust");
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [cursor, setCursor] = useState<string | undefined>(undefined);
  const pageSize = 20;
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [detailTab, setDetailTab] = useState<"profile" | "wallet" | "sessions">("profile");

  // Wallet adjustment state
  const [adjustDialogOpen, setAdjustDialogOpen] = useState(false);
  const [adjustUserId, setAdjustUserId] = useState<string | null>(null);
  const [adjustAmount, setAdjustAmount] = useState("");
  const [adjustReason, setAdjustReason] = useState("");
  const [adjustError, setAdjustError] = useState("");
  const [adjustResult, setAdjustResult] = useState<{ newBalance: number; transactionId: string } | null>(null);

  // Fetch mobile users list
  const { data: usersData, isLoading } = useQuery<CursorPagedResult<MobileUser>>({
    queryKey: ["mobile-users", search, cursor],
    queryFn: async () => {
      const params: Record<string, unknown> = { pageSize };
      if (search) params.search = search;
      if (cursor) params.cursor = cursor;
      const res = await api.get("/admin/mobile-users", { params });
      return res.data;
    },
  });

  const users: MobileUser[] = usersData?.data || [];
  const hasMore = usersData?.pagination?.hasMore ?? false;

  // Fetch selected user detail
  const { data: userDetail, isLoading: isLoadingDetail } = useQuery<MobileUserDetail>({
    queryKey: ["mobile-users", selectedUserId, "detail"],
    queryFn: async () => {
      const res = await api.get(`/admin/mobile-users/${selectedUserId}`);
      return res.data;
    },
    enabled: !!selectedUserId,
  });

  // Fetch selected user sessions
  const { data: userSessionsData } = useQuery({
    queryKey: ["mobile-users", selectedUserId, "sessions"],
    queryFn: async () => {
      const res = await api.get(`/admin/mobile-users/${selectedUserId}/sessions`, {
        params: { pageSize: 5 },
      });
      return res.data;
    },
    enabled: !!selectedUserId && detailTab === "sessions",
  });

  const userSessions: UserSession[] = userSessionsData?.data || [];

  // Fetch selected user transactions
  const { data: userTransactionsData } = useQuery({
    queryKey: ["mobile-users", selectedUserId, "transactions"],
    queryFn: async () => {
      const res = await api.get(`/admin/mobile-users/${selectedUserId}/transactions`, {
        params: { pageSize: 5 },
      });
      return res.data;
    },
    enabled: !!selectedUserId && detailTab === "wallet",
  });

  const userTransactions: WalletTransaction[] = userTransactionsData?.data || [];

  // Suspend user
  const suspendMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.post(`/admin/mobile-users/${id}/suspend`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mobile-users"] });
    },
  });

  // Unsuspend user
  const unsuspendMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.post(`/admin/mobile-users/${id}/unsuspend`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mobile-users"] });
    },
  });

  // Wallet adjustment
  const walletAdjustMutation = useMutation({
    mutationFn: async ({ userId, data }: { userId: string; data: { amount: number; reason: string } }) => {
      const res = await api.post(`/admin/mobile-users/${userId}/wallet/adjust`, data);
      return res.data as { newBalance: number; transactionId: string };
    },
    onSuccess: (result) => {
      setAdjustResult(result);
      setAdjustAmount("");
      setAdjustReason("");
      setAdjustError("");
      queryClient.invalidateQueries({ queryKey: ["mobile-users"] });
    },
    onError: (err: unknown) => {
      if (err && typeof err === "object" && "response" in err) {
        const axiosError = err as { response?: { data?: { error?: { message?: string; details?: string; validationErrors?: Array<{ message: string }> } } } };
        const apiError = axiosError.response?.data?.error;
        if (apiError?.validationErrors?.length) {
          setAdjustError(apiError.validationErrors.map((e) => e.message).join(". "));
        } else if (apiError?.details) {
          setAdjustError(apiError.details);
        } else if (apiError?.message) {
          setAdjustError(apiError.message);
        } else {
          setAdjustError("An error occurred");
        }
      } else {
        setAdjustError("An error occurred");
      }
    },
  });

  const openAdjustDialog = (userId: string) => {
    setAdjustUserId(userId);
    setAdjustAmount("");
    setAdjustReason("");
    setAdjustError("");
    setAdjustResult(null);
    setAdjustDialogOpen(true);
  };

  const closeAdjustDialog = () => {
    setAdjustDialogOpen(false);
    setAdjustUserId(null);
    setAdjustResult(null);
  };

  const handleAdjustSubmit = () => {
    const amount = parseFloat(adjustAmount);
    if (isNaN(amount) || amount === 0) {
      setAdjustError(t("mobileUsers.adjustAmount"));
      return;
    }
    if (!adjustReason.trim()) {
      setAdjustError(t("mobileUsers.adjustReason"));
      return;
    }
    if (!adjustUserId) return;
    setAdjustError("");
    walletAdjustMutation.mutate({
      userId: adjustUserId,
      data: { amount, reason: adjustReason.trim() },
    });
  };

  const handleRowClick = (userId: string) => {
    if (selectedUserId === userId) {
      setSelectedUserId(null);
    } else {
      setSelectedUserId(userId);
      setDetailTab("profile");
    }
  };

  const getFullName = (user: MobileUser | MobileUserDetail) => {
    return user.fullName || "—";
  };

  const detailTabs = [
    { value: "profile", label: t("mobileUsers.profile") },
    { value: "wallet", label: t("mobileUsers.wallet") },
    { value: "sessions", label: t("mobileUsers.sessions") },
  ];

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      {/* Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader
          title={t("mobileUsers.title")}
          description={t("mobileUsers.description")}
        />
      </div>

      <div className="flex-1 space-y-6 p-6">
      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder={t("mobileUsers.searchPlaceholder")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-9"
        />
      </div>

      {/* Users Table */}
      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left text-sm font-medium">{t("mobileUsers.name")}</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">{t("mobileUsers.phone")}</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">{t("mobileUsers.email")}</th>
                  <th className="px-4 py-3 text-right text-sm font-medium">{t("mobileUsers.walletBalance")}</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">{t("mobileUsers.membership")}</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">{t("common.status")}</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">{t("mobileUsers.lastLogin")}</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">{t("common.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  <tr>
                    <td colSpan={8} className="p-0">
                      <SkeletonTable rows={5} cols={8} />
                    </td>
                  </tr>
                ) : users.length > 0 ? (
                  users.map((user) => (
                    <React.Fragment key={user.id}>
                      <tr
                        className={`border-b cursor-pointer transition-colors ${
                          selectedUserId === user.id
                            ? "bg-muted"
                            : "hover:bg-muted/50"
                        }`}
                        onClick={() => handleRowClick(user.id)}
                      >
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-2">
                            <span className="font-medium">{getFullName(user)}</span>
                            {selectedUserId === user.id ? (
                              <ChevronUp className="h-4 w-4 text-muted-foreground" />
                            ) : (
                              <ChevronDown className="h-4 w-4 text-muted-foreground" />
                            )}
                          </div>
                        </td>
                        <td className="px-4 py-3 text-sm">
                          {user.phoneNumber || "—"}
                        </td>
                        <td className="px-4 py-3 text-sm">
                          {user.email || "—"}
                        </td>
                        <td className="px-4 py-3 text-sm font-medium tabular-nums text-right">
                          {formatCurrency(user.walletBalance)}
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={MembershipTierVariant[user.membershipTier] || "secondary"}>
                            {MembershipTierLabels[user.membershipTier] || "Standard"}
                          </Badge>
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={user.isActive ? "success" : "destructive"}>
                            {user.isActive ? t("common.active") : t("mobileUsers.suspended")}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-sm">
                          {formatDate(user.lastLoginAt)}
                        </td>
                        <td className="px-4 py-3">
                          <div
                            className="flex items-center gap-2"
                            role="group"
                            onClick={(e) => e.stopPropagation()}
                            onKeyDown={(e) => e.stopPropagation()}
                          >
                            {user.isActive ? (
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => {
                                  if (confirm(t("mobileUsers.suspendConfirm"))) {
                                    suspendMutation.mutate(user.id);
                                  }
                                }}
                                disabled={suspendMutation.isPending}
                              >
                                <UserX className="mr-1 h-4 w-4" />
                                {t("mobileUsers.suspend")}
                              </Button>
                            ) : (
                              <Button
                                variant="default"
                                size="sm"
                                onClick={() => unsuspendMutation.mutate(user.id)}
                                disabled={unsuspendMutation.isPending}
                              >
                                <UserCheck className="mr-1 h-4 w-4" />
                                {t("mobileUsers.unsuspend")}
                              </Button>
                            )}
                          </div>
                        </td>
                      </tr>

                      {/* Expanded Detail Row */}
                      {selectedUserId === user.id && (
                        <tr key={`${user.id}-detail`}>
                          <td colSpan={8} className="border-b bg-muted/30 p-0">
                            <div className="p-4 space-y-4">
                              {/* Tab Navigation */}
                              <Tabs
                                tabs={detailTabs}
                                value={detailTab}
                                onChange={(v) => setDetailTab(v as "profile" | "wallet" | "sessions")}
                              />

                              {/* Profile Tab */}
                              {detailTab === "profile" && (
                                <div>
                                  {isLoadingDetail ? (
                                    <div className="grid gap-4 md:grid-cols-3">
                                      <SkeletonCard />
                                      <SkeletonCard />
                                      <SkeletonCard />
                                    </div>
                                  ) : userDetail ? (
                                    <div className="grid gap-4 md:grid-cols-3">
                                      <Card>
                                        <CardHeader className="pb-2">
                                          <CardTitle className="text-sm font-medium text-muted-foreground">
                                            {t("mobileUsers.accountInfo")}
                                          </CardTitle>
                                        </CardHeader>
                                        <CardContent className="space-y-2 text-sm">
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">{t("mobileUsers.name")}</span>
                                            <span className="font-medium">{getFullName(userDetail)}</span>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">{t("mobileUsers.phone")}</span>
                                            <span className="font-medium">{userDetail.phoneNumber || "—"}</span>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">{t("mobileUsers.email")}</span>
                                            <span className="font-medium">{userDetail.email || "—"}</span>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">{t("mobileUsers.registered")}</span>
                                            <span className="font-medium">{formatDate(userDetail.createdAt)}</span>
                                          </div>
                                        </CardContent>
                                      </Card>

                                      <Card>
                                        <CardHeader className="pb-2">
                                          <CardTitle className="text-sm font-medium text-muted-foreground">
                                            {t("mobileUsers.membershipCard")}
                                          </CardTitle>
                                        </CardHeader>
                                        <CardContent className="space-y-2 text-sm">
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">{t("mobileUsers.tier")}</span>
                                            <Badge variant={MembershipTierVariant[userDetail.membershipTier] || "secondary"}>
                                              {MembershipTierLabels[userDetail.membershipTier] || "Standard"}
                                            </Badge>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">{t("common.status")}</span>
                                            <Badge variant={userDetail.isActive ? "success" : "destructive"}>
                                              {userDetail.isActive ? t("common.active") : t("mobileUsers.suspended")}
                                            </Badge>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">{t("mobileUsers.lastLogin")}</span>
                                            <span className="font-medium">{formatDateTime(userDetail.lastLoginAt)}</span>
                                          </div>
                                        </CardContent>
                                      </Card>

                                      <Card>
                                        <CardHeader className="pb-2">
                                          <CardTitle className="text-sm font-medium text-muted-foreground">
                                            {t("mobileUsers.activitySummary")}
                                          </CardTitle>
                                        </CardHeader>
                                        <CardContent className="space-y-2 text-sm">
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">{t("mobileUsers.totalSessions")}</span>
                                            <span className="font-medium">{userDetail.sessionCount ?? 0}</span>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">{t("mobileUsers.totalSpent")}</span>
                                            <span className="font-medium">{formatCurrency(userDetail.totalSpent)}</span>
                                          </div>
                                        </CardContent>
                                      </Card>
                                    </div>
                                  ) : (
                                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                      <AlertCircle className="h-4 w-4" />
                                      <span>{t("mobileUsers.unableToLoadProfile")}</span>
                                    </div>
                                  )}
                                </div>
                              )}

                              {/* Wallet Tab */}
                              {detailTab === "wallet" && (
                                <div className="space-y-4">
                                  <div className="grid gap-4 md:grid-cols-3">
                                    <StatCard
                                      label={t("mobileUsers.walletBalance")}
                                      value={formatCurrency(userDetail?.walletBalance)}
                                      icon={Wallet}
                                      iconColor="bg-emerald-500/10 text-emerald-600"
                                    />
                                    <StatCard
                                      label={t("mobileUsers.totalSessions")}
                                      value={userDetail?.sessionCount ?? 0}
                                      icon={Zap}
                                      iconColor="bg-blue-500/10 text-blue-600"
                                    />
                                    <StatCard
                                      label={t("mobileUsers.totalSpent")}
                                      value={formatCurrency(userDetail?.totalSpent)}
                                      icon={Receipt}
                                      iconColor="bg-amber-500/10 text-amber-600"
                                    />
                                  </div>

                                  {canAdjustWallet && (
                                    <div className="flex justify-end">
                                      <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={(e) => {
                                          e.stopPropagation();
                                          openAdjustDialog(user.id);
                                        }}
                                      >
                                        <Wallet className="mr-1 h-4 w-4" />
                                        {t("mobileUsers.adjustBalance")}
                                      </Button>
                                    </div>
                                  )}

                                  <div>
                                    <h4 className="mb-2 text-sm font-medium">{t("mobileUsers.recentTransactions")}</h4>
                                    {userTransactions.length > 0 ? (
                                      <div className="overflow-x-auto rounded-md border">
                                        <table className="w-full">
                                          <thead>
                                            <tr className="border-b bg-muted/50">
                                              <th className="px-3 py-2 text-left text-xs font-medium">{t("mobileUsers.type")}</th>
                                              <th className="px-3 py-2 text-right text-xs font-medium">{t("mobileUsers.amount")}</th>
                                              <th className="px-3 py-2 text-left text-xs font-medium">{t("mobileUsers.description")}</th>
                                              <th className="px-3 py-2 text-left text-xs font-medium">{t("mobileUsers.date")}</th>
                                            </tr>
                                          </thead>
                                          <tbody>
                                            {userTransactions.map((tx) => (
                                              <tr key={tx.id} className="border-b">
                                                <td className="px-3 py-2 text-sm">
                                                  <Badge variant={tx.type === 0 || tx.type === 2 || tx.type === 3 ? "success" : "secondary"}>
                                                    {TransactionTypeLabels[tx.type] || "Unknown"}
                                                  </Badge>
                                                </td>
                                                <td className="px-3 py-2 text-sm font-medium tabular-nums text-right">
                                                  <span className={tx.type === 1 ? "text-destructive" : "text-emerald-600"}>
                                                    {tx.type === 1 ? "-" : "+"}{formatCurrency(tx.amount)}
                                                  </span>
                                                </td>
                                                <td className="px-3 py-2 text-sm text-muted-foreground">
                                                  {tx.description || "—"}
                                                </td>
                                                <td className="px-3 py-2 text-sm">
                                                  {formatDateTime(tx.createdAt)}
                                                </td>
                                              </tr>
                                            ))}
                                          </tbody>
                                        </table>
                                      </div>
                                    ) : (
                                      <EmptyState
                                        icon={Wallet}
                                        title={t("mobileUsers.noRecentTransactions")}
                                        description={t("mobileUsers.noTransactionsDescription")}
                                        className="py-8"
                                      />
                                    )}
                                  </div>
                                </div>
                              )}

                              {/* Sessions Tab */}
                              {detailTab === "sessions" && (
                                <div>
                                  <h4 className="mb-2 text-sm font-medium">{t("mobileUsers.recentSessions")}</h4>
                                  {userSessions.length > 0 ? (
                                    <div className="overflow-x-auto rounded-md border">
                                      <table className="w-full">
                                        <thead>
                                          <tr className="border-b bg-muted/50">
                                            <th className="px-3 py-2 text-left text-xs font-medium">{t("mobileUsers.station")}</th>
                                            <th className="px-3 py-2 text-left text-xs font-medium">{t("common.status")}</th>
                                            <th className="px-3 py-2 text-left text-xs font-medium">{t("mobileUsers.startTime")}</th>
                                            <th className="px-3 py-2 text-left text-xs font-medium">{t("mobileUsers.endTime")}</th>
                                            <th className="px-3 py-2 text-right text-xs font-medium">{t("mobileUsers.energy")}</th>
                                            <th className="px-3 py-2 text-right text-xs font-medium">{t("mobileUsers.cost")}</th>
                                          </tr>
                                        </thead>
                                        <tbody>
                                          {userSessions.map((session) => (
                                            <tr key={session.id} className="border-b">
                                              <td className="px-3 py-2 text-sm">
                                                <span className="font-medium font-mono text-xs">{session.stationId?.substring(0, 8) || "—"}</span>
                                              </td>
                                              <td className="px-3 py-2 text-sm">
                                                <StatusBadge type="session" value={session.status} />
                                              </td>
                                              <td className="px-3 py-2 text-sm">
                                                {formatDateTime(session.startTime)}
                                              </td>
                                              <td className="px-3 py-2 text-sm">
                                                {formatDateTime(session.endTime)}
                                              </td>
                                              <td className="px-3 py-2 text-sm tabular-nums text-right">
                                                {((session.totalEnergyKwh ?? 0)).toFixed(2)} kWh
                                              </td>
                                              <td className="px-3 py-2 text-sm font-medium tabular-nums text-right">
                                                {formatCurrency(session.totalCost)}
                                              </td>
                                            </tr>
                                          ))}
                                        </tbody>
                                      </table>
                                    </div>
                                  ) : (
                                    <EmptyState
                                      icon={Zap}
                                      title={t("mobileUsers.noRecentSessions")}
                                      description={t("mobileUsers.noSessionsDescription")}
                                      className="py-8"
                                    />
                                  )}
                                </div>
                              )}
                            </div>
                          </td>
                        </tr>
                      )}
                    </React.Fragment>
                  ))
                ) : (
                  <tr>
                    <td colSpan={8} className="p-0">
                      <EmptyState
                        icon={Users}
                        title={t("mobileUsers.noUsersFound")}
                        description={search ? t("mobileUsers.noUsersSearchDescription") : t("mobileUsers.noUsersDescription")}
                      />
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {(hasMore || cursor) && (
            <div className="flex items-center justify-between border-t px-4 py-3">
              <p className="text-sm text-muted-foreground">
                {t("mobileUsers.showingUsers").replace("{count}", String(users.length))}
              </p>
              <div className="flex items-center gap-2">
                {cursor && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setCursor(undefined)}
                  >
                    {t("mobileUsers.firstPage")}
                  </Button>
                )}
                {hasMore && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setCursor(usersData?.pagination?.nextCursor)}
                  >
                    {t("common.next")}
                  </Button>
                )}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Wallet Adjustment Dialog */}
      <Dialog open={adjustDialogOpen} onClose={closeAdjustDialog} size="sm" title={t("mobileUsers.adjustBalance")}>
        <DialogHeader onClose={closeAdjustDialog}>
          {t("mobileUsers.adjustBalance")}
        </DialogHeader>
        <DialogContent>
          {adjustResult ? (
            <div className="space-y-4 text-center py-4">
              <CheckCircle className="mx-auto h-12 w-12 text-emerald-500" />
              <p className="text-sm font-medium">{t("mobileUsers.adjustSuccess")}</p>
              <div className="rounded-md bg-muted p-3">
                <p className="text-sm text-muted-foreground">{t("mobileUsers.newBalance")}</p>
                <p className="text-lg font-semibold">{formatCurrency(adjustResult.newBalance)}</p>
              </div>
            </div>
          ) : (
            <div className="space-y-4">
              {adjustError && (
                <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive flex items-center gap-2">
                  <AlertCircle className="h-4 w-4 shrink-0" />
                  {adjustError}
                </div>
              )}
              <div className="space-y-2">
                <label htmlFor="adjust-amount" className="text-sm font-medium">
                  {t("mobileUsers.adjustAmount")}
                </label>
                <Input
                  id="adjust-amount"
                  type="number"
                  value={adjustAmount}
                  onChange={(e) => setAdjustAmount(e.target.value)}
                  placeholder="0"
                />
                <p className="text-xs text-muted-foreground">{t("mobileUsers.adjustAmountHint")}</p>
              </div>
              <div className="space-y-2">
                <label htmlFor="adjust-reason" className="text-sm font-medium">
                  {t("mobileUsers.adjustReason")}
                </label>
                <Input
                  id="adjust-reason"
                  type="text"
                  value={adjustReason}
                  onChange={(e) => setAdjustReason(e.target.value)}
                  placeholder={t("mobileUsers.adjustReason")}
                />
              </div>
            </div>
          )}
        </DialogContent>
        <DialogFooter>
          {adjustResult ? (
            <Button variant="default" onClick={closeAdjustDialog}>
              {t("common.close")}
            </Button>
          ) : (
            <>
              <Button variant="outline" onClick={closeAdjustDialog}>
                {t("common.cancel")}
              </Button>
              <Button
                variant="default"
                onClick={handleAdjustSubmit}
                disabled={walletAdjustMutation.isPending}
              >
                {walletAdjustMutation.isPending ? t("common.saving") : t("common.confirm")}
              </Button>
            </>
          )}
        </DialogFooter>
      </Dialog>
      </div>
    </div>
  );
}
