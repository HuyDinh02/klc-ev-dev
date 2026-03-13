"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { monitoringApi } from "@/lib/api";
import {
  useMonitoringHub,
  type StationStatusUpdate,
  type ConnectorStatusUpdate,
  type AlertNotification,
  type SessionUpdate,
  type ConnectionStatus,
} from "@/lib/signalr";
import {
  Activity,
  Zap,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  Battery,
  TrendingUp,
  Clock,
  Wifi,
  WifiOff,
} from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { StatusBadge } from "@/components/ui/status-badge";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard } from "@/components/ui/skeleton";
import { CONNECTOR_STATUS } from "@/lib/constants";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";

interface DashboardStats {
  totalStations: number;
  onlineStations: number;
  offlineStations: number;
  faultedStations: number;
  totalConnectors: number;
  availableConnectors: number;
  chargingConnectors: number;
  faultedConnectors: number;
  activeSessions: number;
  todayEnergyKwh: number;
  todayRevenue: number;
  stationSummaries: StationSummary[];
}

interface StationSummary {
  stationId: string;
  stationName: string;
  status: number;
  latitude: number | null;
  longitude: number | null;
  totalConnectors: number;
  availableConnectors: number;
  chargingConnectors: number;
  lastHeartbeat: string | null;
}

interface RealtimeAlert {
  alertId: string;
  stationName: string | null;
  alertType: string;
  message: string;
  timestamp: string;
}

function ConnectionIndicator({ status }: { status: ConnectionStatus }) {
  const { t } = useTranslation();
  if (status === "connected") {
    return (
      <div className="flex items-center gap-1.5 text-green-600" role="status">
        <Wifi className="h-4 w-4" aria-hidden="true" />
        <span className="text-xs font-medium">{t("monitoring.live")}</span>
        <span className="flex h-2 w-2 rounded-full bg-green-500 animate-pulse" aria-hidden="true" />
      </div>
    );
  }
  if (status === "connecting") {
    return (
      <div className="flex items-center gap-1.5 text-amber-600" role="status">
        <Wifi className="h-4 w-4" aria-hidden="true" />
        <span className="text-xs font-medium">{t("monitoring.connecting")}</span>
      </div>
    );
  }
  return (
    <div className="flex items-center gap-1.5 text-muted-foreground" role="status">
      <WifiOff className="h-4 w-4" aria-hidden="true" />
      <span className="text-xs font-medium">{t("monitoring.polling")}</span>
    </div>
  );
}

export default function MonitoringPage() {
  const hasAccess = useRequirePermission("KLC.Monitoring");
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [realtimeAlerts, setRealtimeAlerts] = useState<RealtimeAlert[]>([]);
  const [lastEvent, setLastEvent] = useState<Date | null>(null);

  // SignalR event handlers — invalidate dashboard to refresh all data
  const onStationStatusChanged = useCallback(
    (_update: StationStatusUpdate) => {
      setLastEvent(new Date());
      queryClient.invalidateQueries({ queryKey: ["monitoring-dashboard"] });
    },
    [queryClient]
  );

  const onConnectorStatusChanged = useCallback(
    (_update: ConnectorStatusUpdate) => {
      setLastEvent(new Date());
      queryClient.invalidateQueries({ queryKey: ["monitoring-dashboard"] });
    },
    [queryClient]
  );

  const onAlertCreated = useCallback((alert: AlertNotification) => {
    setLastEvent(new Date());
    setRealtimeAlerts((prev) =>
      [
        {
          alertId: alert.alertId,
          stationName: alert.stationName,
          alertType: alert.alertType,
          message: alert.message,
          timestamp: alert.timestamp,
        },
        ...prev,
      ].slice(0, 10)
    );
  }, []);

  const onSessionUpdated = useCallback(
    (_update: SessionUpdate) => {
      setLastEvent(new Date());
      queryClient.invalidateQueries({ queryKey: ["monitoring-dashboard"] });
    },
    [queryClient]
  );

  const { status: hubStatus } = useMonitoringHub({
    onStationStatusChanged,
    onConnectorStatusChanged,
    onAlertCreated,
    onSessionUpdated,
  });

  // Fallback polling — slower when SignalR is connected
  const pollingInterval = hubStatus === "connected" ? 60000 : 10000;

  // Fetch dashboard stats (initial + fallback polling)
  const { data: dashboard, isLoading } = useQuery<DashboardStats>({
    queryKey: ["monitoring-dashboard"],
    queryFn: async () => {
      const res = await monitoringApi.getDashboard();
      return res.data;
    },
    refetchInterval: pollingInterval,
  });

  const stats = dashboard || {
    totalStations: 0,
    onlineStations: 0,
    offlineStations: 0,
    faultedStations: 0,
    totalConnectors: 0,
    availableConnectors: 0,
    chargingConnectors: 0,
    faultedConnectors: 0,
    activeSessions: 0,
    todayEnergyKwh: 0,
    todayRevenue: 0,
    stationSummaries: [],
  };

  const stations = stats.stationSummaries;

  // Connector status enum values from CONNECTOR_STATUS constants
  const CONNECTOR_AVAILABLE = 0;
  const CONNECTOR_CHARGING = 2;
  const CONNECTOR_UNAVAILABLE = 7;

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader
          title={t("monitoring.title")}
          description={t("monitoring.description")}
        >
          <ConnectionIndicator status={hubStatus} />
          {lastEvent && (
            <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
              <Clock className="h-4 w-4" aria-hidden="true" />
              <span>{t("monitoring.lastEvent")} {lastEvent.toLocaleTimeString()}</span>
            </div>
          )}
        </PageHeader>
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* KPI Cards */}
        {isLoading ? (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <SkeletonCard key={i} />
            ))}
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <StatCard
              label={t("monitoring.stationStatus")}
              value={`${stats.onlineStations} / ${stats.totalStations}`}
              icon={Activity}
              iconColor="bg-green-100 text-green-600"
            >
              <div className="mt-2 flex gap-2">
                <Badge variant="success" className="text-xs">
                  <CheckCircle2 className="mr-1 h-3 w-3" aria-hidden="true" />
                  <span className="tabular-nums">{stats.onlineStations}</span>&nbsp;{t("common.online")}
                </Badge>
                <Badge variant="destructive" className="text-xs">
                  <XCircle className="mr-1 h-3 w-3" aria-hidden="true" />
                  <span className="tabular-nums">{stats.offlineStations}</span>&nbsp;{t("common.offline")}
                </Badge>
              </div>
            </StatCard>

            <StatCard
              label={t("monitoring.connectorStatus")}
              value={`${stats.chargingConnectors} / ${stats.totalConnectors}`}
              icon={Battery}
              iconColor="bg-blue-100 text-blue-600"
            >
              <div className="mt-2 flex flex-wrap gap-1">
                <Badge variant="success" className="text-xs">
                  <span className="tabular-nums">{stats.availableConnectors}</span>&nbsp;{t("status.available")}
                </Badge>
                <Badge variant="info" className="text-xs">
                  <span className="tabular-nums">{stats.chargingConnectors}</span>&nbsp;{t("status.charging")}
                </Badge>
                <Badge variant="destructive" className="text-xs">
                  <span className="tabular-nums">{stats.faultedConnectors}</span>&nbsp;{t("status.faulted")}
                </Badge>
              </div>
            </StatCard>

            <StatCard
              label={t("monitoring.activeSessions")}
              value={stats.activeSessions}
              icon={Zap}
              iconColor="bg-amber-100 text-amber-600"
            >
              <p className="mt-1 text-xs text-muted-foreground">
                {t("monitoring.sessionsInProgress")}
              </p>
            </StatCard>

            <StatCard
              label={t("monitoring.todaysEnergy")}
              value={`${stats.todayEnergyKwh.toFixed(1)} kWh`}
              icon={TrendingUp}
              iconColor="bg-purple-100 text-purple-600"
            >
              <p className="mt-1 text-xs text-muted-foreground">
                {t("monitoring.revenue")} <span className="tabular-nums">{stats.todayRevenue.toLocaleString("vi-VN")}</span>đ
              </p>
            </StatCard>
          </div>
        )}

        {/* Station Grid */}
        <Card>
          <CardHeader>
            <CardTitle>{t("monitoring.stationOverview")}</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                {Array.from({ length: 6 }).map((_, i) => (
                  <SkeletonCard key={i} />
                ))}
              </div>
            ) : (!stations || stations.length === 0) ? (
              <EmptyState
                icon={Activity}
                title={t("monitoring.noStationsFound")}
                description={t("monitoring.addStationsDescription")}
              />
            ) : (
              <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                {stations.slice(0, 12).map((station) => {
                  const faultedConnectors =
                    station.totalConnectors -
                    station.availableConnectors -
                    station.chargingConnectors;

                  const availableDot = CONNECTOR_STATUS[CONNECTOR_AVAILABLE].dotColor;
                  const chargingDot = CONNECTOR_STATUS[CONNECTOR_CHARGING].dotColor;
                  const unavailableDot = CONNECTOR_STATUS[CONNECTOR_UNAVAILABLE].dotColor;

                  return (
                    <div
                      key={station.stationId}
                      className="rounded-lg border p-4 hover:bg-accent/50 transition-colors"
                    >
                      <div className="flex items-center justify-between mb-3">
                        <h3 className="font-medium truncate">
                          {station.stationName}
                        </h3>
                        <StatusBadge type="station" value={station.status} />
                      </div>
                      <div className="space-y-2">
                        <div className="flex gap-1">
                          {station.totalConnectors > 0 ? (
                            <>
                              {Array.from({
                                length: station.availableConnectors,
                              }).map((_, i) => (
                                <div
                                  key={`avail-${i}`}
                                  className="flex-1 h-8 rounded flex items-center justify-center text-xs font-medium"
                                  style={{
                                    backgroundColor: `${availableDot}18`,
                                    color: availableDot,
                                  }}
                                  title={CONNECTOR_STATUS[CONNECTOR_AVAILABLE].label}
                                >
                                  <span
                                    className="inline-block h-2 w-2 rounded-full mr-1"
                                    style={{ backgroundColor: availableDot }}
                                  />
                                  {CONNECTOR_STATUS[CONNECTOR_AVAILABLE].label}
                                </div>
                              ))}
                              {Array.from({
                                length: station.chargingConnectors,
                              }).map((_, i) => (
                                <div
                                  key={`charge-${i}`}
                                  className="flex-1 h-8 rounded flex items-center justify-center text-xs font-medium"
                                  style={{
                                    backgroundColor: `${chargingDot}18`,
                                    color: chargingDot,
                                  }}
                                  title={CONNECTOR_STATUS[CONNECTOR_CHARGING].label}
                                >
                                  <span
                                    className="inline-block h-2 w-2 rounded-full mr-1"
                                    style={{ backgroundColor: chargingDot }}
                                  />
                                  {CONNECTOR_STATUS[CONNECTOR_CHARGING].label}
                                </div>
                              ))}
                              {faultedConnectors > 0 &&
                                Array.from({ length: faultedConnectors }).map(
                                  (_, i) => (
                                    <div
                                      key={`other-${i}`}
                                      className="flex-1 h-8 rounded flex items-center justify-center text-xs font-medium"
                                      style={{
                                        backgroundColor: `${unavailableDot}18`,
                                        color: unavailableDot,
                                      }}
                                      title={CONNECTOR_STATUS[CONNECTOR_UNAVAILABLE].label}
                                    >
                                      <span
                                        className="inline-block h-2 w-2 rounded-full mr-1"
                                        style={{ backgroundColor: unavailableDot }}
                                      />
                                      {t("monitoring.other")}
                                    </div>
                                  )
                                )}
                            </>
                          ) : (
                            <div className="flex-1 h-8 rounded bg-gray-100 flex items-center justify-center text-xs text-gray-500">
                              {t("monitoring.noConnectors")}
                            </div>
                          )}
                        </div>
                        <div className="text-xs text-muted-foreground">
                          {t("monitoring.lastHeartbeat")}{" "}
                          {station.lastHeartbeat
                            ? new Date(
                                station.lastHeartbeat
                              ).toLocaleTimeString()
                            : t("common.na")}
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Alerts Section */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-yellow-500" aria-hidden="true" />
              <CardTitle>{t("monitoring.recentAlerts")}</CardTitle>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {realtimeAlerts.length > 0 ? (
                realtimeAlerts.map((alert) => (
                  <div
                    key={alert.alertId}
                    className="flex items-center gap-3 rounded-lg bg-amber-50 p-3 text-amber-800"
                  >
                    <AlertTriangle className="h-5 w-5 shrink-0" aria-hidden="true" />
                    <div className="flex-1 min-w-0">
                      <p className="font-medium truncate">
                        [{alert.alertType}] {alert.stationName || t("monitoring.system")}
                      </p>
                      <p className="text-sm truncate">{alert.message}</p>
                    </div>
                    <span className="text-xs text-amber-600 shrink-0">
                      {new Date(alert.timestamp).toLocaleTimeString()}
                    </span>
                  </div>
                ))
              ) : stats.faultedConnectors > 0 ? (
                <div className="flex items-center gap-3 rounded-lg bg-red-50 p-3 text-red-700" role="alert">
                  <XCircle className="h-5 w-5" aria-hidden="true" />
                  <div>
                    <p className="font-medium">
                      <span className="tabular-nums">{stats.faultedConnectors}</span> {t("monitoring.connectorsReportingFaults")}
                    </p>
                    <p className="text-sm">{t("monitoring.checkFaultsPage")}</p>
                  </div>
                </div>
              ) : (
                <div className="flex items-center gap-3 rounded-lg bg-green-50 p-3 text-green-700" role="status">
                  <CheckCircle2 className="h-5 w-5" aria-hidden="true" />
                  <p className="font-medium">{t("monitoring.allSystemsNormal")}</p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
