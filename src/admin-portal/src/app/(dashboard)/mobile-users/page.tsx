"use client";

import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { api } from "@/lib/api";
import {
  Search,
  UserX,
  UserCheck,
  AlertCircle,
  ChevronDown,
  ChevronUp,
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

const SessionStatusLabels: Record<number, string> = {
  0: "Pending",
  1: "Starting",
  2: "InProgress",
  3: "Suspended",
  4: "Stopping",
  5: "Completed",
  6: "Failed",
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

const formatDate = (date?: string | null) => {
  if (!date) return "—";
  return new Date(date).toLocaleDateString("vi-VN");
};

const formatDateTime = (date?: string | null) => {
  if (!date) return "—";
  return new Date(date).toLocaleString("vi-VN");
};

export default function MobileUsersPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [cursor, setCursor] = useState<string | undefined>(undefined);
  const pageSize = 20;
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [detailTab, setDetailTab] = useState<"profile" | "wallet" | "sessions">("profile");

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

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold">Mobile Users</h1>
        <p className="text-muted-foreground">
          Manage mobile app users, view activity, and handle account status
        </p>
      </div>

      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by name, phone, or email..."
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
                  <th className="px-4 py-3 text-left text-sm font-medium">Name</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">Phone</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">Email</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">Wallet Balance</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">Membership</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">Status</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">Last Login</th>
                  <th className="px-4 py-3 text-left text-sm font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  <tr>
                    <td colSpan={8} className="px-4 py-8 text-center">
                      Loading...
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
                        <td className="px-4 py-3 text-sm font-medium">
                          {formatCurrency(user.walletBalance)}
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={MembershipTierVariant[user.membershipTier] || "secondary"}>
                            {MembershipTierLabels[user.membershipTier] || "Standard"}
                          </Badge>
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={user.isActive ? "success" : "destructive"}>
                            {user.isActive ? "Active" : "Suspended"}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-sm">
                          {formatDate(user.lastLoginAt)}
                        </td>
                        <td className="px-4 py-3">
                          <div
                            className="flex items-center gap-2"
                            onClick={(e) => e.stopPropagation()}
                          >
                            {user.isActive ? (
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => {
                                  if (confirm("Suspend this user? They will not be able to use the app.")) {
                                    suspendMutation.mutate(user.id);
                                  }
                                }}
                                disabled={suspendMutation.isPending}
                              >
                                <UserX className="mr-1 h-4 w-4" />
                                Suspend
                              </Button>
                            ) : (
                              <Button
                                variant="default"
                                size="sm"
                                onClick={() => unsuspendMutation.mutate(user.id)}
                                disabled={unsuspendMutation.isPending}
                              >
                                <UserCheck className="mr-1 h-4 w-4" />
                                Unsuspend
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
                              <div className="flex items-center gap-2 border-b pb-2">
                                <Button
                                  variant={detailTab === "profile" ? "default" : "outline"}
                                  size="sm"
                                  onClick={() => setDetailTab("profile")}
                                >
                                  Profile
                                </Button>
                                <Button
                                  variant={detailTab === "wallet" ? "default" : "outline"}
                                  size="sm"
                                  onClick={() => setDetailTab("wallet")}
                                >
                                  Wallet
                                </Button>
                                <Button
                                  variant={detailTab === "sessions" ? "default" : "outline"}
                                  size="sm"
                                  onClick={() => setDetailTab("sessions")}
                                >
                                  Sessions
                                </Button>
                              </div>

                              {/* Profile Tab */}
                              {detailTab === "profile" && (
                                <div>
                                  {isLoadingDetail ? (
                                    <p className="text-sm text-muted-foreground">Loading profile...</p>
                                  ) : userDetail ? (
                                    <div className="grid gap-4 md:grid-cols-3">
                                      <Card>
                                        <CardHeader className="pb-2">
                                          <CardTitle className="text-sm font-medium text-muted-foreground">
                                            Account Info
                                          </CardTitle>
                                        </CardHeader>
                                        <CardContent className="space-y-2 text-sm">
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">Name</span>
                                            <span className="font-medium">{getFullName(userDetail)}</span>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">Phone</span>
                                            <span className="font-medium">{userDetail.phoneNumber || "—"}</span>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">Email</span>
                                            <span className="font-medium">{userDetail.email || "—"}</span>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">Registered</span>
                                            <span className="font-medium">{formatDate(userDetail.createdAt)}</span>
                                          </div>
                                        </CardContent>
                                      </Card>

                                      <Card>
                                        <CardHeader className="pb-2">
                                          <CardTitle className="text-sm font-medium text-muted-foreground">
                                            Membership
                                          </CardTitle>
                                        </CardHeader>
                                        <CardContent className="space-y-2 text-sm">
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">Tier</span>
                                            <Badge variant={MembershipTierVariant[userDetail.membershipTier] || "secondary"}>
                                              {MembershipTierLabels[userDetail.membershipTier] || "Standard"}
                                            </Badge>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">Status</span>
                                            <Badge variant={userDetail.isActive ? "success" : "destructive"}>
                                              {userDetail.isActive ? "Active" : "Suspended"}
                                            </Badge>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">Last Login</span>
                                            <span className="font-medium">{formatDateTime(userDetail.lastLoginAt)}</span>
                                          </div>
                                        </CardContent>
                                      </Card>

                                      <Card>
                                        <CardHeader className="pb-2">
                                          <CardTitle className="text-sm font-medium text-muted-foreground">
                                            Activity Summary
                                          </CardTitle>
                                        </CardHeader>
                                        <CardContent className="space-y-2 text-sm">
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">Total Sessions</span>
                                            <span className="font-medium">{userDetail.sessionCount ?? 0}</span>
                                          </div>
                                          <div className="flex justify-between">
                                            <span className="text-muted-foreground">Total Spent</span>
                                            <span className="font-medium">{formatCurrency(userDetail.totalSpent)}</span>
                                          </div>
                                        </CardContent>
                                      </Card>
                                    </div>
                                  ) : (
                                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                      <AlertCircle className="h-4 w-4" />
                                      <span>Unable to load user profile.</span>
                                    </div>
                                  )}
                                </div>
                              )}

                              {/* Wallet Tab */}
                              {detailTab === "wallet" && (
                                <div className="space-y-4">
                                  <div className="flex items-center gap-4">
                                    <Card className="flex-1">
                                      <CardContent className="pt-6">
                                        <p className="text-sm text-muted-foreground">Wallet Balance</p>
                                        <p className="text-2xl font-bold">
                                          {formatCurrency(userDetail?.walletBalance)}
                                        </p>
                                      </CardContent>
                                    </Card>
                                  </div>

                                  <div>
                                    <h4 className="mb-2 text-sm font-medium">Recent Transactions</h4>
                                    {userTransactions.length > 0 ? (
                                      <div className="overflow-x-auto rounded-md border">
                                        <table className="w-full">
                                          <thead>
                                            <tr className="border-b bg-muted/50">
                                              <th className="px-3 py-2 text-left text-xs font-medium">Type</th>
                                              <th className="px-3 py-2 text-left text-xs font-medium">Amount</th>
                                              <th className="px-3 py-2 text-left text-xs font-medium">Description</th>
                                              <th className="px-3 py-2 text-left text-xs font-medium">Date</th>
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
                                                <td className="px-3 py-2 text-sm font-medium">
                                                  <span className={tx.type === 1 ? "text-destructive" : "text-green-600"}>
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
                                      <p className="text-sm text-muted-foreground">No recent transactions.</p>
                                    )}
                                  </div>
                                </div>
                              )}

                              {/* Sessions Tab */}
                              {detailTab === "sessions" && (
                                <div>
                                  <h4 className="mb-2 text-sm font-medium">Recent Sessions</h4>
                                  {userSessions.length > 0 ? (
                                    <div className="overflow-x-auto rounded-md border">
                                      <table className="w-full">
                                        <thead>
                                          <tr className="border-b bg-muted/50">
                                            <th className="px-3 py-2 text-left text-xs font-medium">Station</th>
                                            <th className="px-3 py-2 text-left text-xs font-medium">Status</th>
                                            <th className="px-3 py-2 text-left text-xs font-medium">Start Time</th>
                                            <th className="px-3 py-2 text-left text-xs font-medium">End Time</th>
                                            <th className="px-3 py-2 text-left text-xs font-medium">Energy</th>
                                            <th className="px-3 py-2 text-left text-xs font-medium">Cost</th>
                                          </tr>
                                        </thead>
                                        <tbody>
                                          {userSessions.map((session) => (
                                            <tr key={session.id} className="border-b">
                                              <td className="px-3 py-2 text-sm">
                                                <span className="font-medium font-mono text-xs">{session.stationId?.substring(0, 8) || "—"}</span>
                                              </td>
                                              <td className="px-3 py-2 text-sm">
                                                <Badge
                                                  variant={
                                                    session.status === 5
                                                      ? "success"
                                                      : session.status === 6
                                                        ? "destructive"
                                                        : session.status === 2
                                                          ? "default"
                                                          : "secondary"
                                                  }
                                                >
                                                  {SessionStatusLabels[session.status] || "Unknown"}
                                                </Badge>
                                              </td>
                                              <td className="px-3 py-2 text-sm">
                                                {formatDateTime(session.startTime)}
                                              </td>
                                              <td className="px-3 py-2 text-sm">
                                                {formatDateTime(session.endTime)}
                                              </td>
                                              <td className="px-3 py-2 text-sm">
                                                {((session.totalEnergyKwh ?? 0)).toFixed(2)} kWh
                                              </td>
                                              <td className="px-3 py-2 text-sm font-medium">
                                                {formatCurrency(session.totalCost)}
                                              </td>
                                            </tr>
                                          ))}
                                        </tbody>
                                      </table>
                                    </div>
                                  ) : (
                                    <p className="text-sm text-muted-foreground">No recent sessions.</p>
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
                    <td colSpan={8} className="px-4 py-8 text-center text-muted-foreground">
                      No mobile users found.
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
                Showing {users.length} users
              </p>
              <div className="flex items-center gap-2">
                {cursor && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setCursor(undefined)}
                  >
                    First Page
                  </Button>
                )}
                {hasMore && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setCursor(usersData?.pagination?.nextCursor)}
                  >
                    Next
                  </Button>
                )}
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
