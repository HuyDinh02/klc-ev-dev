"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { monitoringApi, api } from "@/lib/api";
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

interface DashboardStats {
  totalStations: number;
  onlineStations: number;
  offlineStations: number;
  totalConnectors: number;
  availableConnectors: number;
  chargingConnectors: number;
  faultedConnectors: number;
  activeSessions: number;
  todayEnergy: number;
  todayRevenue: number;
}

interface StationStatus {
  id: string;
  name: string;
  status: "Online" | "Offline" | "Faulted";
  connectors: ConnectorStatusItem[];
  lastHeartbeat: string;
}

interface ConnectorStatusItem {
  id: string;
  connectorId: number;
  status: "Available" | "Charging" | "Faulted" | "Unavailable";
  currentPower: number;
  sessionId?: string;
}

interface RealtimeAlert {
  alertId: string;
  stationName: string | null;
  alertType: string;
  message: string;
  timestamp: string;
}

function ConnectionIndicator({ status }: { status: ConnectionStatus }) {
  if (status === "connected") {
    return (
      <div className="flex items-center gap-1.5 text-green-600">
        <Wifi className="h-4 w-4" />
        <span className="text-xs font-medium">Live</span>
        <span className="flex h-2 w-2 rounded-full bg-green-500 animate-pulse" />
      </div>
    );
  }
  if (status === "connecting") {
    return (
      <div className="flex items-center gap-1.5 text-yellow-600">
        <Wifi className="h-4 w-4" />
        <span className="text-xs font-medium">Connecting...</span>
      </div>
    );
  }
  return (
    <div className="flex items-center gap-1.5 text-muted-foreground">
      <WifiOff className="h-4 w-4" />
      <span className="text-xs font-medium">Polling (10s)</span>
    </div>
  );
}

export default function MonitoringPage() {
  const queryClient = useQueryClient();
  const [realtimeAlerts, setRealtimeAlerts] = useState<RealtimeAlert[]>([]);
  const [lastEvent, setLastEvent] = useState<Date | null>(null);

  // SignalR event handlers
  const onStationStatusChanged = useCallback(
    (update: StationStatusUpdate) => {
      setLastEvent(new Date());
      // Update station in cache
      queryClient.setQueryData<StationStatus[]>(
        ["monitoring-stations"],
        (old) =>
          old?.map((s) =>
            s.id === update.stationId
              ? { ...s, status: update.newStatus as StationStatus["status"] }
              : s
          )
      );
      // Refetch dashboard for updated counts
      queryClient.invalidateQueries({ queryKey: ["monitoring-dashboard"] });
    },
    [queryClient]
  );

  const onConnectorStatusChanged = useCallback(
    (update: ConnectorStatusUpdate) => {
      setLastEvent(new Date());
      queryClient.setQueryData<StationStatus[]>(
        ["monitoring-stations"],
        (old) =>
          old?.map((s) =>
            s.id === update.stationId
              ? {
                  ...s,
                  connectors: s.connectors.map((c) =>
                    c.connectorId === update.connectorNumber
                      ? {
                          ...c,
                          status:
                            update.newStatus as ConnectorStatusItem["status"],
                        }
                      : c
                  ),
                }
              : s
          )
      );
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
  const { data: dashboard } = useQuery<DashboardStats>({
    queryKey: ["monitoring-dashboard"],
    queryFn: async () => {
      const res = await monitoringApi.getDashboard();
      return res.data;
    },
    refetchInterval: pollingInterval,
  });

  // Fetch station statuses (initial + fallback polling)
  const { data: stations } = useQuery<StationStatus[]>({
    queryKey: ["monitoring-stations"],
    queryFn: async () => {
      const res = await api.get("/stations", {
        params: { maxResultCount: 50 },
      });
      return res.data.items || [];
    },
    refetchInterval: pollingInterval,
  });

  const stats = dashboard || {
    totalStations: 0,
    onlineStations: 0,
    offlineStations: 0,
    totalConnectors: 0,
    availableConnectors: 0,
    chargingConnectors: 0,
    faultedConnectors: 0,
    activeSessions: 0,
    todayEnergy: 0,
    todayRevenue: 0,
  };

  const getStatusColor = (
    status: string
  ): "success" | "default" | "secondary" | "destructive" => {
    switch (status) {
      case "Online":
      case "Available":
        return "success";
      case "Charging":
        return "default";
      case "Offline":
      case "Unavailable":
        return "secondary";
      case "Faulted":
        return "destructive";
      default:
        return "secondary";
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Real-time Monitoring</h1>
          <p className="text-muted-foreground">
            Live system status and performance metrics
          </p>
        </div>
        <div className="flex items-center gap-4">
          <ConnectionIndicator status={hubStatus} />
          {lastEvent && (
            <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
              <Clock className="h-4 w-4" />
              <span>Last event: {lastEvent.toLocaleTimeString()}</span>
            </div>
          )}
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Station Status
            </CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="flex items-baseline gap-2">
              <span className="text-2xl font-bold text-green-600">
                {stats.onlineStations}
              </span>
              <span className="text-sm text-muted-foreground">
                / {stats.totalStations} online
              </span>
            </div>
            <div className="mt-2 flex gap-2">
              <Badge variant="success" className="text-xs">
                <CheckCircle2 className="mr-1 h-3 w-3" />
                {stats.onlineStations} Online
              </Badge>
              <Badge variant="destructive" className="text-xs">
                <XCircle className="mr-1 h-3 w-3" />
                {stats.offlineStations} Offline
              </Badge>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Connector Status
            </CardTitle>
            <Battery className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="flex items-baseline gap-2">
              <span className="text-2xl font-bold text-blue-600">
                {stats.chargingConnectors}
              </span>
              <span className="text-sm text-muted-foreground">
                / {stats.totalConnectors} charging
              </span>
            </div>
            <div className="mt-2 flex flex-wrap gap-1">
              <Badge variant="success" className="text-xs">
                {stats.availableConnectors} Available
              </Badge>
              <Badge variant="default" className="text-xs">
                {stats.chargingConnectors} Charging
              </Badge>
              <Badge variant="destructive" className="text-xs">
                {stats.faultedConnectors} Faulted
              </Badge>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Active Sessions
            </CardTitle>
            <Zap className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.activeSessions}</div>
            <p className="text-xs text-muted-foreground">
              Sessions currently in progress
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Today&apos;s Energy
            </CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {(stats.todayEnergy / 1000).toFixed(1)} kWh
            </div>
            <p className="text-xs text-muted-foreground">
              Revenue: {stats.todayRevenue.toLocaleString("vi-VN")}đ
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Station Grid */}
      <Card>
        <CardHeader>
          <CardTitle>Station Overview</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {(stations || []).slice(0, 12).map((station) => (
              <div
                key={station.id}
                className="rounded-lg border p-4 hover:bg-accent/50 transition-colors"
              >
                <div className="flex items-center justify-between mb-3">
                  <h3 className="font-medium truncate">{station.name}</h3>
                  <Badge variant={getStatusColor(station.status)}>
                    {station.status}
                  </Badge>
                </div>
                <div className="space-y-2">
                  <div className="flex gap-1">
                    {(station.connectors || []).map((conn) => (
                      <div
                        key={conn.id}
                        className={`flex-1 h-8 rounded flex items-center justify-center text-xs font-medium ${
                          conn.status === "Available"
                            ? "bg-green-100 text-green-700"
                            : conn.status === "Charging"
                              ? "bg-blue-100 text-blue-700"
                              : conn.status === "Faulted"
                                ? "bg-red-100 text-red-700"
                                : "bg-gray-100 text-gray-700"
                        }`}
                        title={`Connector ${conn.connectorId}: ${conn.status}`}
                      >
                        #{conn.connectorId}
                      </div>
                    ))}
                    {(!station.connectors ||
                      station.connectors.length === 0) && (
                      <div className="flex-1 h-8 rounded bg-gray-100 flex items-center justify-center text-xs text-gray-500">
                        No connectors
                      </div>
                    )}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    Last heartbeat:{" "}
                    {station.lastHeartbeat
                      ? new Date(station.lastHeartbeat).toLocaleTimeString()
                      : "N/A"}
                  </div>
                </div>
              </div>
            ))}
            {(!stations || stations.length === 0) && (
              <div className="col-span-full text-center py-8 text-muted-foreground">
                No stations found. Add stations to see monitoring data.
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Alerts Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-yellow-500" />
            <CardTitle>Recent Alerts</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-2">
            {realtimeAlerts.length > 0 ? (
              realtimeAlerts.map((alert) => (
                <div
                  key={alert.alertId}
                  className="flex items-center gap-3 rounded-lg bg-yellow-50 p-3 text-yellow-800"
                >
                  <AlertTriangle className="h-5 w-5 shrink-0" />
                  <div className="flex-1 min-w-0">
                    <p className="font-medium truncate">
                      [{alert.alertType}] {alert.stationName || "System"}
                    </p>
                    <p className="text-sm truncate">{alert.message}</p>
                  </div>
                  <span className="text-xs text-yellow-600 shrink-0">
                    {new Date(alert.timestamp).toLocaleTimeString()}
                  </span>
                </div>
              ))
            ) : stats.faultedConnectors > 0 ? (
              <div className="flex items-center gap-3 rounded-lg bg-red-50 p-3 text-red-700">
                <XCircle className="h-5 w-5" />
                <div>
                  <p className="font-medium">
                    {stats.faultedConnectors} connector(s) reporting faults
                  </p>
                  <p className="text-sm">Check Faults page for details</p>
                </div>
              </div>
            ) : (
              <div className="flex items-center gap-3 rounded-lg bg-green-50 p-3 text-green-700">
                <CheckCircle2 className="h-5 w-5" />
                <p className="font-medium">All systems operating normally</p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
