"use client";

import { useState } from "react";
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
import { broadcastApi } from "@/lib/api";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import { formatDistanceToNow, formatDateTime } from "@/lib/utils";
import {
  Bell,
  BellOff,
  Users,
  Zap,
  CreditCard,
  FileText,
  Wallet,
  Gift,
  Megaphone,
  ChevronLeft,
  ChevronRight,
  Send,
  History,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";

interface BroadcastHistory {
  title: string;
  body: string;
  type: number;
  recipientCount: number;
  sentAt: string;
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
  const [page, setPage] = useState(0);
  const [selectedBroadcast, setSelectedBroadcast] = useState<BroadcastHistory | null>(null);
  const [broadcastOpen, setBroadcastOpen] = useState(false);
  const [broadcastForm, setBroadcastForm] = useState({ type: 8, title: "", body: "" });
  const [broadcastResult, setBroadcastResult] = useState<string | null>(null);

  const { data: broadcasts, isLoading } = useQuery({
    queryKey: ["broadcast-history", page],
    queryFn: async () => {
      const { data } = await broadcastApi.getHistory({ pageSize: PAGE_SIZE });
      return data as BroadcastHistory[];
    },
  });

  const broadcastMutation = useMutation({
    mutationFn: (data: { type: number; title: string; body: string }) =>
      broadcastApi.send(data),
    onSuccess: (response) => {
      const result = response.data as { message: string; recipientCount: number };
      setBroadcastResult(
        `${t("notifications.broadcastSuccess")} (${result.recipientCount} ${t("notifications.recipients")})`
      );
      setBroadcastForm({ type: 8, title: "", body: "" });
      queryClient.invalidateQueries({ queryKey: ["broadcast-history"] });
      setTimeout(() => {
        setBroadcastResult(null);
        setBroadcastOpen(false);
      }, 2000);
    },
  });

  const broadcastList: BroadcastHistory[] = broadcasts || [];
  const totalBroadcasts = broadcastList.length;
  const totalRecipients = broadcastList.reduce((sum, b) => sum + b.recipientCount, 0);

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
        </PageHeader>
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* Stats */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <StatCard
            label={t("notifications.totalBroadcasts")}
            value={totalBroadcasts}
            icon={Megaphone}
          />
          <StatCard
            label={t("notifications.totalRecipients")}
            value={totalRecipients}
            icon={Users}
          />
          <StatCard
            label={t("notifications.avgRecipients")}
            value={totalBroadcasts > 0 ? Math.round(totalRecipients / totalBroadcasts) : 0}
            icon={Bell}
          />
        </div>

        {/* Broadcast History */}
        <div>
          <h3 className="mb-4 flex items-center gap-2 text-sm font-medium text-muted-foreground">
            <History className="h-4 w-4" />
            {t("notifications.broadcastHistory")}
          </h3>

          {isLoading ? (
            <div className="space-y-4">
              {Array.from({ length: 5 }).map((_, i) => (
                <SkeletonCard key={i} />
              ))}
            </div>
          ) : broadcastList.length === 0 ? (
            <EmptyState
              icon={BellOff}
              title={t("notifications.noBroadcasts")}
              description={t("notifications.noBroadcastsDescription")}
            />
          ) : (
            <div className="space-y-2">
              {broadcastList.map((broadcast, index) => {
                const Icon = NotificationTypeIcons[broadcast.type] || Bell;
                const typeKey = NotificationTypeKeys[broadcast.type] || "notifications.systemAnnouncement";
                return (
                  <Card
                    key={`${broadcast.sentAt}-${index}`}
                    className="cursor-pointer transition-colors hover:bg-accent/50"
                    onClick={() => setSelectedBroadcast(broadcast)}
                  >
                    <CardContent className="flex items-center gap-4 p-4">
                      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 text-primary">
                        <Icon className="h-5 w-5" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium truncate">
                          {broadcast.title}
                        </p>
                        <p className="text-xs text-muted-foreground mt-0.5">
                          {t(typeKey)}
                        </p>
                      </div>
                      <div className="flex items-center gap-3 flex-shrink-0">
                        <Badge variant="secondary">
                          <Users className="mr-1 h-3 w-3" />
                          {broadcast.recipientCount} {t("notifications.recipients")}
                        </Badge>
                        <span className="text-xs text-muted-foreground">
                          {formatDistanceToNow(broadcast.sentAt)}
                        </span>
                      </div>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {/* Detail Dialog */}
      {selectedBroadcast && (
        <Dialog open onClose={() => setSelectedBroadcast(null)}>
          <DialogHeader>
            <h2 className="text-lg font-semibold">{t("notifications.details")}</h2>
          </DialogHeader>
          <DialogContent>
            <div className="space-y-4">
              <div>
                <p className="text-sm text-muted-foreground">{t("notifications.broadcastType")}</p>
                <p className="font-medium">{t(NotificationTypeKeys[selectedBroadcast.type] || "notifications.systemAnnouncement")}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t("notifications.broadcastTitle")}</p>
                <p className="font-medium">{selectedBroadcast.title}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t("notifications.broadcastBody")}</p>
                <p className="whitespace-pre-wrap">{selectedBroadcast.body}</p>
              </div>
              <div className="flex gap-6">
                <div>
                  <p className="text-sm text-muted-foreground">{t("notifications.recipients")}</p>
                  <p className="font-medium">{selectedBroadcast.recipientCount}</p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t("notifications.sentAt")}</p>
                  <p className="font-medium">{formatDateTime(selectedBroadcast.sentAt)}</p>
                </div>
              </div>
            </div>
          </DialogContent>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSelectedBroadcast(null)}>
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
