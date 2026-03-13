"use client";

import { useState, useCallback } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard } from "@/components/ui/skeleton";
import { useTranslation } from "@/lib/i18n";
import { notificationsApi, broadcastApi } from "@/lib/api";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import { formatDistanceToNow } from "@/lib/utils";
import {
  Bell,
  BellOff,
  Mail,
  MailOpen,
  CheckCheck,
  Eye,
  Zap,
  CreditCard,
  FileText,
  Wallet,
  Gift,
  Megaphone,
  ChevronLeft,
  ChevronRight,
  Send,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";

interface Notification {
  id: string;
  type: number;
  title: string;
  body?: string;
  isRead: boolean;
  createdAt: string;
  referenceId?: string;
}

// NotificationType: 0=ChargingStarted, 1=ChargingCompleted, 2=ChargingFailed,
// 3=PaymentSuccess, 4=PaymentFailed, 5=EInvoiceReady, 6=WalletTopUp, 7=Promotion, 8=SystemAnnouncement
const NotificationTypeKeys: Record<number, string> = {
  0: "notifications.chargingStarted",
  1: "notifications.chargingCompleted",
  2: "notifications.chargingFailed",
  3: "notifications.paymentSuccess",
  4: "notifications.paymentFailed",
  5: "notifications.eInvoiceReady",
  6: "notifications.walletTopUp",
  7: "notifications.promotion",
  8: "notifications.systemAnnouncement",
};

const NotificationTypeIcons: Record<number, LucideIcon> = {
  0: Zap,
  1: Zap,
  2: Zap,
  3: CreditCard,
  4: CreditCard,
  5: FileText,
  6: Wallet,
  7: Gift,
  8: Megaphone,
};

const PAGE_SIZE = 20;

export default function NotificationsPage() {
  const hasAccess = useRequirePermission("KLC.Notifications");
  const canBroadcast = useHasPermission("KLC.Notifications.Broadcast");
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [readFilter, setReadFilter] = useState<"all" | "unread" | "read">("all");
  const [cursor, setCursor] = useState<string | null>(null);
  const [cursorStack, setCursorStack] = useState<(string | null)[]>([]);
  const [selectedNotification, setSelectedNotification] = useState<Notification | null>(null);
  const [broadcastOpen, setBroadcastOpen] = useState(false);
  const [broadcastForm, setBroadcastForm] = useState({ type: 8, title: "", body: "" });
  const [broadcastResult, setBroadcastResult] = useState<string | null>(null);

  const { data: notificationsData, isLoading } = useQuery({
    queryKey: ["notifications", readFilter, cursor],
    queryFn: async () => {
      const params: Record<string, unknown> = { maxResultCount: PAGE_SIZE };
      if (readFilter === "unread") params.isRead = false;
      if (readFilter === "read") params.isRead = true;
      if (cursor) params.cursor = cursor;
      const { data } = await notificationsApi.getAll(params as Parameters<typeof notificationsApi.getAll>[0]);
      return data;
    },
  });

  const { data: unreadCountData } = useQuery({
    queryKey: ["notifications-unread-count"],
    queryFn: async () => {
      const { data } = await notificationsApi.getUnreadCount();
      return data;
    },
  });

  const markAsReadMutation = useMutation({
    mutationFn: (id: string) => notificationsApi.markAsRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
      queryClient.invalidateQueries({ queryKey: ["notifications-unread-count"] });
    },
  });

  const markAllAsReadMutation = useMutation({
    mutationFn: () => notificationsApi.markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
      queryClient.invalidateQueries({ queryKey: ["notifications-unread-count"] });
    },
  });

  const broadcastMutation = useMutation({
    mutationFn: (data: { type: number; title: string; body: string }) =>
      broadcastApi.send(data),
    onSuccess: (response) => {
      const result = response.data as { message: string; recipientCount: number };
      setBroadcastResult(
        `${t("notifications.broadcastSuccess")} (${result.recipientCount} recipients)`
      );
      setBroadcastForm({ type: 8, title: "", body: "" });
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
      queryClient.invalidateQueries({ queryKey: ["notifications-unread-count"] });
      setTimeout(() => {
        setBroadcastResult(null);
        setBroadcastOpen(false);
      }, 2000);
    },
  });

  const viewDetail = useCallback(async (notification: Notification) => {
    if (!notification.isRead) {
      markAsReadMutation.mutate(notification.id);
    }
    try {
      const { data } = await notificationsApi.getById(notification.id);
      setSelectedNotification(data);
    } catch {
      setSelectedNotification(notification);
    }
  }, [markAsReadMutation]);

  const notifications: Notification[] = notificationsData?.items || [];
  const totalCount = notificationsData?.totalCount || 0;
  const unreadCount = typeof unreadCountData === "number" ? unreadCountData : 0;
  const hasNextPage = notifications.length === PAGE_SIZE;

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title={t("notifications.title")} description={t("notifications.description")}>
          {canBroadcast && (
            <Button onClick={() => setBroadcastOpen(true)}>
              <Send className="mr-2 h-4 w-4" />
              {t("notifications.broadcastNew")}
            </Button>
          )}
          <Button
            variant="outline"
            onClick={() => markAllAsReadMutation.mutate()}
            disabled={markAllAsReadMutation.isPending || unreadCount === 0}
          >
            <CheckCheck className="mr-2 h-4 w-4" />
            {t("notifications.markAllAsRead")}
          </Button>
        </PageHeader>
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* Stats */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <StatCard
            label={t("notifications.totalNotifications")}
            value={totalCount}
            icon={Bell}
          />
          <StatCard
            label={t("notifications.unreadCount")}
            value={unreadCount}
            icon={Mail}
          />
          <StatCard
            label={t("notifications.read")}
            value={Math.max(0, totalCount - unreadCount)}
            icon={MailOpen}
          />
        </div>

        {/* Filter buttons */}
        <div className="flex gap-2">
          {(["all", "unread", "read"] as const).map((filter) => (
            <Button
              key={filter}
              variant={readFilter === filter ? "default" : "outline"}
              size="sm"
              onClick={() => {
                setReadFilter(filter);
                setCursor(null);
                setCursorStack([]);
              }}
            >
              {t(`notifications.${filter}`)}
              {filter === "unread" && unreadCount > 0 && (
                <Badge variant="destructive" className="ml-2">{unreadCount}</Badge>
              )}
            </Button>
          ))}
        </div>

        {/* List */}
        {isLoading ? (
          <div className="space-y-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <SkeletonCard key={i} />
            ))}
          </div>
        ) : notifications.length === 0 ? (
          <EmptyState
            icon={BellOff}
            title={t("notifications.noNotifications")}
            description={t("notifications.noNotificationsDescription")}
          />
        ) : (
          <div className="space-y-2">
            {notifications.map((notification) => {
              const Icon = NotificationTypeIcons[notification.type] || Bell;
              const typeKey = NotificationTypeKeys[notification.type] || "notifications.systemAnnouncement";
              return (
                <Card
                  key={notification.id}
                  className={`cursor-pointer transition-colors hover:bg-accent/50 ${
                    !notification.isRead ? "border-l-4 border-l-primary bg-primary/5" : ""
                  }`}
                  onClick={() => viewDetail(notification)}
                >
                  <CardContent className="flex items-center gap-4 p-4">
                    <div className={`flex h-10 w-10 items-center justify-center rounded-full ${
                      !notification.isRead ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"
                    }`}>
                      <Icon className="h-5 w-5" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <p className={`text-sm truncate ${!notification.isRead ? "font-semibold" : ""}`}>
                          {notification.title}
                        </p>
                        {!notification.isRead && (
                          <span className="h-2 w-2 rounded-full bg-primary flex-shrink-0" />
                        )}
                      </div>
                      <p className="text-xs text-muted-foreground mt-0.5">
                        {t(typeKey)}
                      </p>
                    </div>
                    <div className="flex items-center gap-2 flex-shrink-0">
                      <span className="text-xs text-muted-foreground">
                        {formatDistanceToNow(notification.createdAt)}
                      </span>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={(e) => {
                          e.stopPropagation();
                          viewDetail(notification);
                        }}
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}

        {/* Pagination */}
        {(cursorStack.length > 0 || hasNextPage) && (
          <div className="flex items-center justify-center gap-4 pt-4">
            <Button
              variant="outline"
              size="sm"
              disabled={cursorStack.length === 0}
              onClick={() => {
                const prev = [...cursorStack];
                const prevCursor = prev.pop()!;
                setCursorStack(prev);
                setCursor(prevCursor);
              }}
            >
              <ChevronLeft className="mr-1 h-4 w-4" /> {t("common.previous")}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!hasNextPage}
              onClick={() => {
                const lastItem = notifications[notifications.length - 1];
                if (lastItem) {
                  setCursorStack([...cursorStack, cursor]);
                  setCursor(lastItem.id);
                }
              }}
            >
              {t("common.next")} <ChevronRight className="ml-1 h-4 w-4" />
            </Button>
          </div>
        )}
      </div>

      {/* Detail Dialog */}
      {selectedNotification && (
        <Dialog open onClose={() => setSelectedNotification(null)}>
          <DialogHeader>
            <h2 className="text-lg font-semibold">{t("notifications.details")}</h2>
          </DialogHeader>
          <DialogContent>
            <div className="space-y-4">
              <div>
                <p className="text-sm text-muted-foreground">{t("notifications.broadcastType")}</p>
                <p className="font-medium">{t(NotificationTypeKeys[selectedNotification.type] || "notifications.systemAnnouncement")}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t("notifications.broadcastTitle")}</p>
                <p className="font-medium">{selectedNotification.title}</p>
              </div>
              {selectedNotification.body && (
                <div>
                  <p className="text-sm text-muted-foreground">{t("notifications.broadcastBody")}</p>
                  <p className="whitespace-pre-wrap">{selectedNotification.body}</p>
                </div>
              )}
              <div>
                <p className="text-sm text-muted-foreground">{t("alerts.createdLabel")}</p>
                <p>{new Date(selectedNotification.createdAt).toLocaleString()}</p>
              </div>
            </div>
          </DialogContent>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSelectedNotification(null)}>
              {t("common.close")}
            </Button>
          </DialogFooter>
        </Dialog>
      )}

      {/* Broadcast Dialog */}
      <Dialog
        open={broadcastOpen}
        onClose={() => {
          setBroadcastOpen(false);
          setBroadcastResult(null);
        }}
        title={t("notifications.broadcastNew")}
      >
        <DialogHeader
          onClose={() => {
            setBroadcastOpen(false);
            setBroadcastResult(null);
          }}
        >
          <h2 className="text-lg font-semibold">{t("notifications.broadcastNew")}</h2>
        </DialogHeader>
        <DialogContent>
          <div className="space-y-4">
            {broadcastResult && (
              <div className="rounded-md bg-green-50 p-3 text-sm text-green-800 dark:bg-green-900/20 dark:text-green-400">
                {broadcastResult}
              </div>
            )}
            <div>
              <label className="mb-1 block text-sm font-medium">{t("notifications.broadcastType")}</label>
              <select
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                value={broadcastForm.type}
                onChange={(e) => setBroadcastForm({ ...broadcastForm, type: Number(e.target.value) })}
              >
                <option value={7}>{t("notifications.promotion")}</option>
                <option value={8}>{t("notifications.systemAnnouncement")}</option>
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">{t("notifications.broadcastTitle")}</label>
              <input
                type="text"
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                value={broadcastForm.title}
                onChange={(e) => setBroadcastForm({ ...broadcastForm, title: e.target.value })}
                placeholder={t("notifications.broadcastTitle")}
                required
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">{t("notifications.broadcastBody")}</label>
              <textarea
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                rows={4}
                value={broadcastForm.body}
                onChange={(e) => setBroadcastForm({ ...broadcastForm, body: e.target.value })}
                placeholder={t("notifications.broadcastBody")}
                required
              />
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => {
              setBroadcastOpen(false);
              setBroadcastResult(null);
            }}
          >
            {t("common.cancel")}
          </Button>
          <Button
            onClick={() => broadcastMutation.mutate(broadcastForm)}
            disabled={
              broadcastMutation.isPending ||
              !broadcastForm.title.trim() ||
              !broadcastForm.body.trim()
            }
          >
            <Send className="mr-2 h-4 w-4" />
            {broadcastMutation.isPending ? "..." : t("notifications.broadcastSend")}
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  );
}
