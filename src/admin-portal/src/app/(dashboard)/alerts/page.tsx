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
import { api } from "@/lib/api";
import { useMonitoringHub } from "@/lib/signalr";
import { useAlertsStore } from "@/lib/store";
import {
  Bell,
  AlertTriangle,
  AlertCircle,
  Info,
  CheckCircle2,
  Clock,
  MapPin,
  Eye,
  ChevronLeft,
  ChevronRight,
  Wifi,
} from "lucide-react";

interface Alert {
  id: string;
  type: number;
  title: string;
  message: string;
  stationId?: string;
  stationName?: string;
  status: number; // 0=New, 1=Acknowledged, 2=Resolved
  acknowledgedBy?: string;
  acknowledgedAt?: string;
  createdAt: string;
}

// AlertType: 0=StationOffline, 1=ConnectorFault, 2=LowUtilization, 3=HighUtilization,
// 4=FirmwareUpdate, 5=PaymentFailure, 6=EInvoiceFailure, 7=HeartbeatTimeout
const AlertTypeKeys: Record<number, string> = {
  0: "alerts.stationOffline", 1: "alerts.connectorFault", 2: "alerts.lowUtilization", 3: "alerts.highUtilization",
  4: "alerts.firmwareUpdate", 5: "alerts.paymentFailure", 6: "alerts.eInvoiceFailure", 7: "alerts.heartbeatTimeout",
};

const AlertStatusKeys: Record<number, string> = {
  0: "alerts.new", 1: "alerts.acknowledged", 2: "alerts.resolved",
};

// Critical types: StationOffline, ConnectorFault, PaymentFailure, HeartbeatTimeout
const CRITICAL_TYPES = new Set([0, 1, 5, 7]);
const WARNING_TYPES = new Set([2, 3, 6]);

interface AlertStats {
  criticalCount: number;
  warningCount: number;
  infoCount: number;
  unacknowledgedCount: number;
}

export default function AlertsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [typeFilter, setTypeFilter] = useState("all");
  const [acknowledgedFilter, setAcknowledgedFilter] = useState("all");
  const [cursor, setCursor] = useState<string | null>(null);
  const [cursorStack, setCursorStack] = useState<(string | null)[]>([]);
  const [selectedAlert, setSelectedAlert] = useState<Alert | null>(null);
  const pageSize = 20;

  // SignalR real-time updates — refresh alerts list when new alerts arrive
  const onAlertCreated = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ["alerts"] });
    useAlertsStore.getState().incrementUnreadCount();
  }, [queryClient]);

  const { status: hubStatus } = useMonitoringHub({
    onAlertCreated,
  });

  const resetPagination = () => { setCursor(null); setCursorStack([]); };

  // Fetch alerts
  const { data: alertsData, isLoading } = useQuery({
    queryKey: ["alerts", typeFilter, acknowledgedFilter, cursor],
    queryFn: async () => {
      const params: Record<string, string | number> = {
        maxResultCount: pageSize,
      };
      // typeFilter is severity-based (critical/warning/info) — client-side filtering only
      if (acknowledgedFilter !== "all") {
        params.status = Number(acknowledgedFilter);
      }
      if (cursor) params.cursor = cursor;

      const res = await api.get("/alerts", { params });
      return res.data;
    },
  });

  // Acknowledge alert
  const acknowledgeMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.post(`/alerts/${id}/acknowledge`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["alerts"] });
    },
  });

  const alerts: Alert[] = alertsData?.items || [];
  const totalCount = alertsData?.totalCount || 0;

  // Compute stats from fetched data
  const stats: AlertStats = {
    criticalCount: alerts.filter((a) => CRITICAL_TYPES.has(a.type)).length,
    warningCount: alerts.filter((a) => WARNING_TYPES.has(a.type)).length,
    infoCount: alerts.filter((a) => !CRITICAL_TYPES.has(a.type) && !WARNING_TYPES.has(a.type)).length,
    unacknowledgedCount: alerts.filter((a) => a.status === 0).length,
  };

  const getSeverity = (type: number): "critical" | "warning" | "info" => {
    if (CRITICAL_TYPES.has(type)) return "critical";
    if (WARNING_TYPES.has(type)) return "warning";
    return "info";
  };

  const getTypeIcon = (type: number) => {
    const severity = getSeverity(type);
    switch (severity) {
      case "critical": return <AlertCircle className="h-5 w-5 text-red-500" />;
      case "warning": return <AlertTriangle className="h-5 w-5 text-yellow-500" />;
      case "info": return <Info className="h-5 w-5 text-blue-500" />;
      default: return <Bell className="h-5 w-5" />;
    }
  };

  const getTypeColor = (type: number): "destructive" | "warning" | "default" | "secondary" => {
    const severity = getSeverity(type);
    switch (severity) {
      case "critical": return "destructive";
      case "warning": return "warning";
      case "info": return "default";
      default: return "secondary";
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString("vi-VN");
  };

  const getTimeAgo = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (days > 0) return `${days}${t("alerts.daysAgo")}`;
    if (hours > 0) return `${hours}${t("alerts.hoursAgo")}`;
    if (minutes > 0) return `${minutes}${t("alerts.minutesAgo")}`;
    return t("alerts.justNow");
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader title={t("alerts.title")} description={t("alerts.description")}>
        {hubStatus === "connected" && (
          <div className="flex items-center gap-1.5 text-green-600">
            <Wifi className="h-3.5 w-3.5" />
            <span className="text-xs font-medium">{t("monitoring.live")}</span>
            <span className="flex h-1.5 w-1.5 rounded-full bg-green-500 animate-pulse" />
          </div>
        )}
        <Badge variant="secondary" className="text-sm">
          {stats?.unacknowledgedCount || 0} {t("alerts.unacknowledged")}
        </Badge>
      </PageHeader>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <StatCard
          label={t("alerts.critical")}
          value={stats?.criticalCount || 0}
          icon={AlertCircle}
          iconColor="bg-red-50 text-red-600"
          className={typeFilter === "critical" ? "ring-2 ring-red-500" : ""}
          onClick={() =>
            setTypeFilter(typeFilter === "critical" ? "all" : "critical")
          }
        />
        <StatCard
          label={t("alerts.warning")}
          value={stats?.warningCount || 0}
          icon={AlertTriangle}
          iconColor="bg-amber-50 text-amber-600"
          className={typeFilter === "warning" ? "ring-2 ring-amber-500" : ""}
          onClick={() =>
            setTypeFilter(typeFilter === "warning" ? "all" : "warning")
          }
        />
        <StatCard
          label={t("alerts.info")}
          value={stats?.infoCount || 0}
          icon={Info}
          iconColor="bg-blue-50 text-blue-600"
          className={typeFilter === "info" ? "ring-2 ring-blue-500" : ""}
          onClick={() => setTypeFilter(typeFilter === "info" ? "all" : "info")}
        />
        <StatCard
          label={t("alerts.unacknowledged")}
          value={stats?.unacknowledgedCount || 0}
          icon={Bell}
          iconColor="bg-primary/10 text-primary"
          className={acknowledgedFilter === "unacknowledged" ? "ring-2 ring-primary" : ""}
          onClick={() =>
            setAcknowledgedFilter(
              acknowledgedFilter === "unacknowledged" ? "all" : "unacknowledged"
            )
          }
        />
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex gap-4">
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              className="rounded-lg border bg-background px-3 py-2 text-sm focus:ring-2 focus:ring-primary/50 focus:border-primary"
            >
              <option value="all">{t("alerts.allSeverity")}</option>
              <option value="critical">{t("alerts.critical")}</option>
              <option value="warning">{t("alerts.warning")}</option>
              <option value="info">{t("alerts.info")}</option>
            </select>
            <select
              value={acknowledgedFilter}
              onChange={(e) => setAcknowledgedFilter(e.target.value)}
              className="rounded-lg border bg-background px-3 py-2 text-sm focus:ring-2 focus:ring-primary/50 focus:border-primary"
            >
              <option value="all">{t("alerts.allStatus")}</option>
              <option value="0">{t("alerts.new")}</option>
              <option value="1">{t("alerts.acknowledged")}</option>
              <option value="2">{t("alerts.resolved")}</option>
            </select>
          </div>
        </CardContent>
      </Card>

      {/* Alerts List */}
      <div className="space-y-3">
        {isLoading ? (
          <div className="grid gap-4 md:grid-cols-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <SkeletonCard key={i} />
            ))}
          </div>
        ) : alerts.length > 0 ? (
          alerts
            .filter((alert) => {
              if (typeFilter === "all") return true;
              return getSeverity(alert.type) === typeFilter;
            })
            .map((alert) => {
              const severity = getSeverity(alert.type);
              const isNew = alert.status === 0;
              return (
              <Card
                key={alert.id}
                className={`${
                  isNew
                    ? severity === "critical"
                      ? "border-l-4 border-l-red-500"
                      : severity === "warning"
                      ? "border-l-4 border-l-yellow-500"
                      : "border-l-4 border-l-blue-500"
                    : ""
                } ${isNew ? "bg-muted/30" : ""}`}
              >
                <CardContent className="py-4">
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-4">
                      {getTypeIcon(alert.type)}
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <h3 className="font-semibold">{alert.title}</h3>
                          <Badge variant={getTypeColor(alert.type)}>
                            {AlertTypeKeys[alert.type] ? t(AlertTypeKeys[alert.type]) : t("alerts.alert")}
                          </Badge>
                          {alert.status > 0 && (
                            <Badge variant="secondary">
                              {AlertStatusKeys[alert.status] ? t(AlertStatusKeys[alert.status]) : ""}
                            </Badge>
                          )}
                        </div>
                        <p className="text-sm text-muted-foreground">
                          {alert.message}
                        </p>
                        <div className="flex items-center gap-4 text-sm text-muted-foreground">
                          <span className="flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            {getTimeAgo(alert.createdAt)}
                          </span>
                          {alert.stationName && (
                            <span className="flex items-center gap-1">
                              <MapPin className="h-3 w-3" />
                              {alert.stationName}
                            </span>
                          )}
                        </div>
                        {alert.status >= 1 && alert.acknowledgedBy && (
                          <p className="text-xs text-muted-foreground">
                            {t("alerts.acknowledgedBy")} {alert.acknowledgedBy}{alert.acknowledgedAt ? ` ${t("alerts.at")} ${formatDate(alert.acknowledgedAt)}` : ""}
                          </p>
                        )}
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setSelectedAlert(alert)}
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                      {alert.status === 0 && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => acknowledgeMutation.mutate(alert.id)}
                        >
                          <CheckCircle2 className="h-4 w-4" />
                        </Button>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
              );
            })
        ) : (
          <EmptyState
            icon={Bell}
            title={t("alerts.noAlertsFound")}
            description={t("alerts.noAlertsDescription")}
          />
        )}
      </div>

      {/* Pagination */}
      {(totalCount > pageSize || cursorStack.length > 0) && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-muted-foreground">
            {totalCount} {t("alerts.totalAlerts")}
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
            {alerts.length === pageSize && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  const lastId = alerts[alerts.length - 1]?.id;
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

      {/* Detail Modal */}
      <Dialog open={!!selectedAlert} onClose={() => setSelectedAlert(null)}>
        <DialogHeader onClose={() => setSelectedAlert(null)}>
          <div className="flex items-center gap-2">
            {selectedAlert && getTypeIcon(selectedAlert.type)}
            {t("alerts.alertDetails")}
          </div>
        </DialogHeader>
        <DialogContent className="space-y-4">
          {selectedAlert && (
            <>
              <div>
                <p className="text-sm text-muted-foreground">{t("alerts.titleLabel")}</p>
                <p className="font-medium">{selectedAlert.title}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t("alerts.messageLabel")}</p>
                <p>{selectedAlert.message}</p>
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <p className="text-sm text-muted-foreground">{t("alerts.typeLabel")}</p>
                  <Badge variant={getTypeColor(selectedAlert.type)}>
                    {AlertTypeKeys[selectedAlert.type] ? t(AlertTypeKeys[selectedAlert.type]) : t("alerts.alert")}
                  </Badge>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t("alerts.statusLabel")}</p>
                  <Badge
                    variant={
                      selectedAlert.status > 0 ? "secondary" : "default"
                    }
                  >
                    {AlertStatusKeys[selectedAlert.status] ? t(AlertStatusKeys[selectedAlert.status]) : t("alerts.new")}
                  </Badge>
                </div>
              </div>
              {selectedAlert.stationName && (
                <div>
                  <p className="text-sm text-muted-foreground">{t("alerts.stationLabel")}</p>
                  <p className="flex items-center gap-2">
                    <MapPin className="h-4 w-4" />
                    {selectedAlert.stationName}
                  </p>
                </div>
              )}
              <div>
                <p className="text-sm text-muted-foreground">{t("alerts.createdLabel")}</p>
                <p>{formatDate(selectedAlert.createdAt)}</p>
              </div>
              {selectedAlert.status >= 1 && selectedAlert.acknowledgedBy && (
                <div>
                  <p className="text-sm text-muted-foreground">{t("alerts.acknowledgedLabel")}</p>
                  <p>
                    {selectedAlert.acknowledgedBy}{selectedAlert.acknowledgedAt ? ` ${t("alerts.at")} ${formatDate(selectedAlert.acknowledgedAt)}` : ""}
                  </p>
                </div>
              )}
            </>
          )}
        </DialogContent>
        {selectedAlert && selectedAlert.status === 0 && (
          <DialogFooter>
            <Button
              className="w-full"
              onClick={() => {
                acknowledgeMutation.mutate(selectedAlert.id);
                setSelectedAlert(null);
              }}
            >
              <CheckCircle2 className="mr-2 h-4 w-4" />
              {t("alerts.acknowledgeAlert")}
            </Button>
          </DialogFooter>
        )}
      </Dialog>
    </div>
  );
}
