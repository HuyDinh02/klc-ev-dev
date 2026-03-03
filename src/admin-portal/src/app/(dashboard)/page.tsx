"use client";

import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  Zap,
  MapPin,
  DollarSign,
  AlertTriangle,
  TrendingUp,
} from "lucide-react";
import { Header } from "@/components/layout/header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { monitoringApi, alertsApi } from "@/lib/api";
import { formatCurrency, formatEnergy } from "@/lib/utils";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";

export default function DashboardPage() {
  const { data: dashboardData, isLoading } = useQuery({
    queryKey: ["dashboard"],
    queryFn: async () => {
      const { data } = await monitoringApi.getDashboard();
      return data;
    },
    refetchInterval: 30000,
  });

  const { data: recentAlerts } = useQuery({
    queryKey: ["recent-alerts"],
    queryFn: async () => {
      const { data } = await alertsApi.getAll({ maxResultCount: 5 });
      return data.items || [];
    },
    refetchInterval: 30000,
  });

  const data = dashboardData || {
    totalStations: 0,
    onlineStations: 0,
    offlineStations: 0,
    faultedStations: 0,
    totalConnectors: 0,
    availableConnectors: 0,
    chargingConnectors: 0,
    faultedConnectors: 0,
    activeSessions: 0,
    todayRevenue: 0,
    todayEnergyKwh: 0,
  };

  const connectorStatusData = [
    { name: "Available", value: data.availableConnectors || 0 },
    { name: "Charging", value: data.chargingConnectors || 0 },
    { name: "Faulted", value: data.faultedConnectors || 0 },
  ];

  return (
    <div className="flex flex-col">
      <Header
        title="Dashboard"
        description="Real-time overview of your charging network"
      />

      <div className="flex-1 space-y-6 p-6">
        {/* Stats Cards */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Stations</CardTitle>
              <MapPin className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{data.totalStations}</div>
              <p className="text-xs text-muted-foreground">
                {data.onlineStations} online
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Active Sessions</CardTitle>
              <Zap className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{data.activeSessions}</div>
              <p className="text-xs text-muted-foreground">
                {data.chargingConnectors} connectors in use
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Today&apos;s Revenue</CardTitle>
              <DollarSign className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{formatCurrency(data.todayRevenue || 0)}</div>
              <p className="text-xs text-muted-foreground">
                {formatEnergy(data.todayEnergyKwh || 0)} delivered
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Faulted</CardTitle>
              <AlertTriangle className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{data.faultedStations || 0}</div>
              <p className="text-xs text-muted-foreground">
                {data.faultedConnectors} faulted connectors
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Charts */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
          {/* Connector Status */}
          <Card className="col-span-4">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="h-5 w-5" />
                Connector Status
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={connectorStatusData} layout="vertical">
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis type="number" className="text-xs" />
                  <YAxis dataKey="name" type="category" className="text-xs" width={80} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: "var(--card)",
                      border: "1px solid var(--border)",
                    }}
                  />
                  <Bar dataKey="value" fill="hsl(var(--primary))" radius={[0, 4, 4, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* Station Summary */}
          <Card className="col-span-3">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <TrendingUp className="h-5 w-5" />
                Station Overview
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Online</span>
                  <span className="font-semibold text-green-600">{data.onlineStations}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Offline</span>
                  <span className="font-semibold text-gray-600">{data.offlineStations || 0}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Faulted</span>
                  <span className="font-semibold text-red-600">{data.faultedStations || 0}</span>
                </div>
                <div className="border-t pt-4">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Total Connectors</span>
                    <span className="font-semibold">{data.totalConnectors}</span>
                  </div>
                  <div className="flex items-center justify-between mt-2">
                    <span className="text-sm text-muted-foreground">Available</span>
                    <span className="font-semibold text-green-600">{data.availableConnectors}</span>
                  </div>
                  <div className="flex items-center justify-between mt-2">
                    <span className="text-sm text-muted-foreground">Charging</span>
                    <span className="font-semibold text-blue-600">{data.chargingConnectors}</span>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Recent Alerts */}
        <Card>
          <CardHeader>
            <CardTitle>Recent Alerts</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {recentAlerts && recentAlerts.length > 0 ? (
                recentAlerts.map((alert: { id: string; type: number | string; title: string; message?: string; stationName?: string; createdAt?: string; creationTime?: string; status: number | string }) => {
                  // AlertType: 0=StationOffline, 1=ConnectorFault, 5=PaymentFailure, 7=HeartbeatTimeout
                  const alertTypeMap: Record<number, string> = { 0: "Station Offline", 1: "Connector Fault", 2: "Low Utilization", 3: "High Utilization", 4: "Firmware Update", 5: "Payment Failure", 6: "E-Invoice Failure", 7: "Heartbeat Timeout" };
                  const typeLabel = typeof alert.type === "number" ? (alertTypeMap[alert.type] || "Alert") : alert.type;
                  const isCritical = alert.type === 0 || alert.type === 1 || alert.type === 5 || alert.type === 7;
                  const dateStr = alert.creationTime || alert.createdAt;
                  return (
                  <div key={alert.id} className="flex items-center justify-between">
                    <div className="flex items-center gap-4">
                      <Badge variant={isCritical ? "destructive" : "warning"}>
                        {typeLabel}
                      </Badge>
                      <div>
                        <p className="text-sm font-medium">{alert.title}</p>
                        <p className="text-xs text-muted-foreground">{alert.stationName || "System"}</p>
                      </div>
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {dateStr ? new Date(dateStr).toLocaleString("vi-VN") : ""}
                    </span>
                  </div>
                  );
                })
              ) : (
                <p className="text-sm text-muted-foreground">No recent alerts</p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
