"use client";

import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  Zap,
  MapPin,
  DollarSign,
  AlertTriangle,
  TrendingUp,
  BatteryCharging,
  Gauge,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { StatCard } from "@/components/ui/stat-card";
import { StatusDot } from "@/components/ui/status-badge";
import { PageHeader } from "@/components/ui/page-header";
import { SkeletonCard, SkeletonChart } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { CHART_COLORS } from "@/lib/constants";
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
  Cell,
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
    { name: "Available", value: data.availableConnectors || 0, color: "#22C55E" },
    { name: "Charging", value: data.chargingConnectors || 0, color: "#3B82F6" },
    { name: "Faulted", value: data.faultedConnectors || 0, color: "#EF4444" },
  ];

  const networkAvailability = data.totalConnectors > 0
    ? Math.round(((data.availableConnectors + data.chargingConnectors) / data.totalConnectors) * 100)
    : 0;

  return (
    <div className="flex flex-col">
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader
          title="Dashboard"
          description="Real-time overview of your charging network"
        />
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* KPI Cards */}
        {isLoading ? (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <StatCard
              label="Active Sessions"
              value={data.activeSessions}
              icon={Zap}
              iconColor="bg-blue-50 text-blue-600"
            />
            <StatCard
              label="Network Availability"
              value={`${networkAvailability}%`}
              icon={Gauge}
              iconColor="bg-primary/10 text-primary"
            />
            <StatCard
              label="Energy Today"
              value={formatEnergy(data.todayEnergyKwh || 0)}
              icon={BatteryCharging}
              iconColor="bg-[var(--color-brand-orange)]/10 text-[var(--color-brand-orange-dark)]"
            />
            <StatCard
              label="Revenue Today"
              value={formatCurrency(data.todayRevenue || 0)}
              icon={DollarSign}
              iconColor="bg-green-50 text-green-600"
            />
          </div>
        )}

        {/* Charts Row */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
          {/* Connector Status */}
          <Card className="col-span-4">
            <CardHeader className="pb-2">
              <CardTitle className="flex items-center gap-2 text-base font-semibold">
                <Activity className="h-4 w-4 text-muted-foreground" />
                Connector Status
              </CardTitle>
            </CardHeader>
            <CardContent>
              {isLoading ? (
                <div className="skeleton h-[300px] w-full" />
              ) : (
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={connectorStatusData} layout="vertical">
                    <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                    <XAxis type="number" className="text-xs" />
                    <YAxis dataKey="name" type="category" className="text-xs" width={80} />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "hsl(var(--card))",
                        border: "1px solid hsl(var(--border))",
                        borderRadius: "8px",
                        fontSize: "13px",
                      }}
                    />
                    <Bar dataKey="value" radius={[0, 4, 4, 0]}>
                      {connectorStatusData.map((entry, index) => (
                        <Cell key={index} fill={entry.color} />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              )}
            </CardContent>
          </Card>

          {/* Station Overview */}
          <Card className="col-span-3">
            <CardHeader className="pb-2">
              <CardTitle className="flex items-center gap-2 text-base font-semibold">
                <TrendingUp className="h-4 w-4 text-muted-foreground" />
                Station Overview
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                <OverviewRow label="Online" value={data.onlineStations} dotType="station" dotValue={1} />
                <OverviewRow label="Offline" value={data.offlineStations || 0} dotType="station" dotValue={0} />
                <OverviewRow label="Faulted" value={data.faultedStations || 0} dotType="station" dotValue={4} />
                <div className="border-t pt-3">
                  <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">Connectors</p>
                  <div className="space-y-3">
                    <OverviewRow label="Total" value={data.totalConnectors} />
                    <OverviewRow label="Available" value={data.availableConnectors} dotType="connector" dotValue={0} />
                    <OverviewRow label="Charging" value={data.chargingConnectors} dotType="connector" dotValue={2} />
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Recent Alerts */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="flex items-center gap-2 text-base font-semibold">
              <AlertTriangle className="h-4 w-4 text-muted-foreground" />
              Recent Alerts
            </CardTitle>
            <a href="/alerts" className="text-sm font-medium text-primary hover:underline">
              View all
            </a>
          </CardHeader>
          <CardContent>
            {recentAlerts && recentAlerts.length > 0 ? (
              <div className="space-y-3">
                {recentAlerts.map((alert: { id: string; type: number | string; title: string; message?: string; stationName?: string; createdAt?: string; creationTime?: string; status: number | string }) => {
                  const alertTypeMap: Record<number, string> = { 0: "Station Offline", 1: "Connector Fault", 2: "Low Utilization", 3: "High Utilization", 4: "Firmware Update", 5: "Payment Failure", 6: "E-Invoice Failure", 7: "Heartbeat Timeout" };
                  const typeLabel = typeof alert.type === "number" ? (alertTypeMap[alert.type] || "Alert") : alert.type;
                  const isCritical = alert.type === 0 || alert.type === 1 || alert.type === 5 || alert.type === 7;
                  const dateStr = alert.creationTime || alert.createdAt;
                  return (
                    <div key={alert.id} className="flex items-center justify-between rounded-lg border p-3 transition-colors hover:bg-muted/50">
                      <div className="flex items-center gap-3">
                        <Badge variant={isCritical ? "destructive" : "warning"}>
                          {typeLabel}
                        </Badge>
                        <div>
                          <p className="text-sm font-medium">{alert.title}</p>
                          <p className="text-xs text-muted-foreground">{alert.stationName || "System"}</p>
                        </div>
                      </div>
                      <span className="text-xs text-muted-foreground whitespace-nowrap">
                        {dateStr ? new Date(dateStr).toLocaleString("vi-VN") : ""}
                      </span>
                    </div>
                  );
                })}
              </div>
            ) : (
              <EmptyState
                icon={AlertTriangle}
                title="No recent alerts"
                description="Your charging network is running smoothly"
                className="py-8"
              />
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function OverviewRow({ label, value, dotType, dotValue }: {
  label: string;
  value: number;
  dotType?: "station" | "connector";
  dotValue?: number;
}) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex items-center gap-2">
        {dotType && dotValue !== undefined && (
          <StatusDot type={dotType} value={dotValue} />
        )}
        <span className="text-sm text-muted-foreground">{label}</span>
      </div>
      <span className="text-sm font-semibold tabular-nums">{value}</span>
    </div>
  );
}
