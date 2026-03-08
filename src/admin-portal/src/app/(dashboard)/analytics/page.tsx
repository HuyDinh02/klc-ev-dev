"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard, SkeletonChart } from "@/components/ui/skeleton";
import { monitoringApi } from "@/lib/api";
import { formatCurrency, formatEnergy } from "@/lib/utils";
import { CHART_COLORS } from "@/lib/constants";
import {
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import {
  TrendingUp,
  Zap,
  DollarSign,
  Clock,
  Activity,
  ArrowUpDown,
  Wrench,
  Sun,
  BarChart3,
} from "lucide-react";

interface DailyStats {
  date: string;
  sessions: number;
  energyKwh: number;
  revenue: number;
}

interface StationUtilization {
  stationId: string;
  stationName: string;
  totalSessions: number;
  totalEnergyKwh: number;
  totalRevenue: number;
  utilizationPercent: number;
  onlinePercent: number;
}

interface AnalyticsData {
  dailyStats: DailyStats[];
  stationUtilization: StationUtilization[];
  totalRevenue: number;
  totalEnergyKwh: number;
  totalSessions: number;
  averageSessionDurationMinutes: number;
  uptimePercent: number;
  mtbfHours: number;
  peakHourUtc: number | null;
  peakHourSessionCount: number;
}

type SortKey = "stationName" | "totalSessions" | "totalEnergyKwh" | "totalRevenue" | "utilizationPercent" | "onlinePercent";
type SortDir = "asc" | "desc";

type DateRange = "7d" | "30d" | "90d";

function getDateRange(range: DateRange): { fromDate: string; toDate: string } {
  const to = new Date();
  const from = new Date();
  switch (range) {
    case "7d":
      from.setDate(from.getDate() - 7);
      break;
    case "30d":
      from.setDate(from.getDate() - 30);
      break;
    case "90d":
      from.setDate(from.getDate() - 90);
      break;
  }
  return {
    fromDate: from.toISOString(),
    toDate: to.toISOString(),
  };
}

const rangeLabels: Record<DateRange, string> = {
  "7d": "Last 7 days",
  "30d": "Last 30 days",
  "90d": "Last 90 days",
};

function formatDate(dateStr: string): string {
  const d = new Date(dateStr);
  return `${d.getDate()}/${d.getMonth() + 1}`;
}

function formatVnd(value: number): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`;
  if (value >= 1_000) return `${(value / 1_000).toFixed(0)}K`;
  return value.toString();
}

function formatPeakHour(hourUtc: number | null): string {
  if (hourUtc === null) return "N/A";
  // Convert UTC hour to UTC+7 (Vietnam)
  const vn = (hourUtc + 7) % 24;
  const next = (vn + 1) % 24;
  return `${vn.toString().padStart(2, "0")}:00–${next.toString().padStart(2, "0")}:00`;
}

function uptimeColorClass(pct: number): string {
  if (pct >= 95) return "text-green-600";
  if (pct >= 90) return "text-yellow-600";
  return "text-red-600";
}

export default function AnalyticsPage() {
  const [range, setRange] = useState<DateRange>("30d");
  const [sortKey, setSortKey] = useState<SortKey>("totalSessions");
  const [sortDir, setSortDir] = useState<SortDir>("desc");

  const { fromDate, toDate } = getDateRange(range);

  const { data: analytics, isLoading } = useQuery<AnalyticsData>({
    queryKey: ["analytics", range],
    queryFn: async () => {
      const res = await monitoringApi.getAnalytics({ fromDate, toDate });
      return res.data;
    },
  });

  const data = analytics || {
    dailyStats: [],
    stationUtilization: [],
    totalRevenue: 0,
    totalEnergyKwh: 0,
    totalSessions: 0,
    averageSessionDurationMinutes: 0,
    uptimePercent: 0,
    mtbfHours: 0,
    peakHourUtc: null,
    peakHourSessionCount: 0,
  };

  const toggleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDir(sortDir === "asc" ? "desc" : "asc");
    } else {
      setSortKey(key);
      setSortDir("desc");
    }
  };

  const sortedUtilization = [...data.stationUtilization].sort((a, b) => {
    const aVal = a[sortKey];
    const bVal = b[sortKey];
    if (typeof aVal === "string" && typeof bVal === "string") {
      return sortDir === "asc" ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal);
    }
    return sortDir === "asc" ? (aVal as number) - (bVal as number) : (bVal as number) - (aVal as number);
  });

  // Compute averages for daily comparison
  const dailyAvgRevenue =
    data.dailyStats.length > 0
      ? data.totalRevenue / data.dailyStats.length
      : 0;
  const dailyAvgSessions =
    data.dailyStats.length > 0
      ? data.totalSessions / data.dailyStats.length
      : 0;

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
          <PageHeader title="Analytics" description="Revenue trends, utilization rates, and performance KPIs" />
        </div>
        <div className="px-6 space-y-6">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
            {Array.from({ length: 5 }).map((_, i) => (
              <SkeletonCard key={i} />
            ))}
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <SkeletonCard />
            <SkeletonCard />
          </div>
          <SkeletonChart />
          <div className="grid gap-4 md:grid-cols-2">
            <SkeletonChart />
            <SkeletonChart />
          </div>
          <SkeletonChart />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title="Analytics" description="Revenue trends, utilization rates, and performance KPIs">
          <div className="flex gap-1 rounded-lg border p-1">
            {(["7d", "30d", "90d"] as DateRange[]).map((r) => (
              <button
                key={r}
                onClick={() => setRange(r)}
                className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                  range === r
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-accent"
                }`}
              >
                {rangeLabels[r]}
              </button>
            ))}
          </div>
        </PageHeader>
      </div>

      {/* KPI Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
        <StatCard
          label="Total Revenue"
          value={formatCurrency(data.totalRevenue)}
          icon={DollarSign}
          iconColor="bg-green-100 text-green-700"
        >
          <p className="text-xs text-muted-foreground">
            ~{formatCurrency(dailyAvgRevenue)}/day avg
          </p>
        </StatCard>

        <StatCard
          label="Energy Delivered"
          value={formatEnergy(data.totalEnergyKwh)}
          icon={Zap}
          iconColor="bg-amber-100 text-amber-700"
        >
          <p className="text-xs text-muted-foreground">
            {data.totalSessions} sessions
          </p>
        </StatCard>

        <StatCard
          label="Avg Session Duration"
          value={`${data.averageSessionDurationMinutes.toFixed(0)} min`}
          icon={Clock}
          iconColor="bg-blue-100 text-blue-700"
        >
          <p className="text-xs text-muted-foreground">
            ~{dailyAvgSessions.toFixed(1)} sessions/day
          </p>
        </StatCard>

        <StatCard
          label="Network Uptime"
          value={`${data.uptimePercent.toFixed(1)}%`}
          icon={Activity}
          iconColor="bg-purple-100 text-purple-700"
          className={uptimeColorClass(data.uptimePercent)}
        >
          <p className="text-xs text-muted-foreground">
            {data.uptimePercent >= 95 ? "Healthy" : data.uptimePercent >= 90 ? "Degraded" : "Critical"} availability
          </p>
        </StatCard>

        <StatCard
          label="Avg Revenue/kWh"
          value={data.totalEnergyKwh > 0 ? formatCurrency(data.totalRevenue / data.totalEnergyKwh) : "0\u0111"}
          icon={TrendingUp}
          iconColor="bg-teal-100 text-teal-700"
        >
          <p className="text-xs text-muted-foreground">Effective rate</p>
        </StatCard>
      </div>

      {/* Operational Metrics */}
      <div className="grid gap-4 md:grid-cols-2">
        <StatCard
          label="Mean Time Between Faults (MTBF)"
          value={
            data.mtbfHours > 0
              ? data.mtbfHours >= 24
                ? `${(data.mtbfHours / 24).toFixed(1)} days`
                : `${data.mtbfHours.toFixed(1)} hrs`
              : "No faults"
          }
          icon={Wrench}
          iconColor="bg-orange-100 text-orange-700"
        >
          <p className="text-xs text-muted-foreground">
            {data.mtbfHours > 0
              ? "Avg interval between fault occurrences"
              : "No faults recorded in this period"}
          </p>
        </StatCard>

        <StatCard
          label="Peak Charging Hour"
          value={formatPeakHour(data.peakHourUtc)}
          icon={Sun}
          iconColor="bg-yellow-100 text-yellow-700"
        >
          <p className="text-xs text-muted-foreground">
            {data.peakHourSessionCount > 0
              ? `${data.peakHourSessionCount} sessions started during this hour (UTC+7)`
              : "No sessions in this period"}
          </p>
        </StatCard>
      </div>

      {/* Revenue Trend */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <DollarSign className="h-5 w-5" />
            Revenue Trend
          </CardTitle>
        </CardHeader>
        <CardContent>
          {data.dailyStats.length > 0 ? (
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart data={data.dailyStats}>
                <defs>
                  <linearGradient id="revenueGradient" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.3} />
                    <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                <XAxis
                  dataKey="date"
                  tickFormatter={formatDate}
                  className="text-xs"
                />
                <YAxis tickFormatter={formatVnd} className="text-xs" />
                <Tooltip
                  formatter={(value) => [formatCurrency(value as number), "Revenue"]}
                  labelFormatter={(label) => new Date(label).toLocaleDateString("vi-VN")}
                  contentStyle={{
                    backgroundColor: "var(--card)",
                    border: "1px solid var(--border)",
                  }}
                />
                <Area
                  type="monotone"
                  dataKey="revenue"
                  stroke="hsl(var(--primary))"
                  fill="url(#revenueGradient)"
                  strokeWidth={2}
                />
              </AreaChart>
            </ResponsiveContainer>
          ) : (
            <EmptyState
              icon={BarChart3}
              title="No revenue data"
              description="No data available for the selected period"
              className="h-[300px] py-0"
            />
          )}
        </CardContent>
      </Card>

      {/* Energy + Sessions Trend */}
      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Zap className="h-5 w-5" />
              Energy Delivered (kWh)
            </CardTitle>
          </CardHeader>
          <CardContent>
            {data.dailyStats.length > 0 ? (
              <ResponsiveContainer width="100%" height={250}>
                <AreaChart data={data.dailyStats}>
                  <defs>
                    <linearGradient id="energyGradient" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor={CHART_COLORS.green} stopOpacity={0.3} />
                      <stop offset="95%" stopColor={CHART_COLORS.green} stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="date" tickFormatter={formatDate} className="text-xs" />
                  <YAxis className="text-xs" />
                  <Tooltip
                    formatter={(value) => [`${(value as number).toFixed(2)} kWh`, "Energy"]}
                    labelFormatter={(label) => new Date(label).toLocaleDateString("vi-VN")}
                    contentStyle={{
                      backgroundColor: "var(--card)",
                      border: "1px solid var(--border)",
                    }}
                  />
                  <Area
                    type="monotone"
                    dataKey="energyKwh"
                    stroke={CHART_COLORS.green}
                    fill="url(#energyGradient)"
                    strokeWidth={2}
                  />
                </AreaChart>
              </ResponsiveContainer>
            ) : (
              <EmptyState
                icon={Zap}
                title="No energy data"
                description="No data available for the selected period"
                className="h-[250px] py-0"
              />
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Activity className="h-5 w-5" />
              Daily Sessions
            </CardTitle>
          </CardHeader>
          <CardContent>
            {data.dailyStats.length > 0 ? (
              <ResponsiveContainer width="100%" height={250}>
                <BarChart data={data.dailyStats}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="date" tickFormatter={formatDate} className="text-xs" />
                  <YAxis className="text-xs" allowDecimals={false} />
                  <Tooltip
                    formatter={(value) => [value as number, "Sessions"]}
                    labelFormatter={(label) => new Date(label).toLocaleDateString("vi-VN")}
                    contentStyle={{
                      backgroundColor: "var(--card)",
                      border: "1px solid var(--border)",
                    }}
                  />
                  <Bar dataKey="sessions" fill={CHART_COLORS.blue} radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <EmptyState
                icon={Activity}
                title="No session data"
                description="No data available for the selected period"
                className="h-[250px] py-0"
              />
            )}
          </CardContent>
        </Card>
      </div>

      {/* Station Utilization */}
      <Card>
        <CardHeader>
          <CardTitle>Station Utilization</CardTitle>
        </CardHeader>
        <CardContent>
          {data.stationUtilization.length > 0 ? (
            <div className="space-y-4">
              {/* Chart */}
              <ResponsiveContainer width="100%" height={Math.max(200, data.stationUtilization.length * 40)}>
                <BarChart
                  data={data.stationUtilization.slice(0, 15)}
                  layout="vertical"
                  margin={{ left: 20 }}
                >
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis type="number" domain={[0, 100]} unit="%" className="text-xs" />
                  <YAxis
                    dataKey="stationName"
                    type="category"
                    width={150}
                    className="text-xs"
                    tick={{ fontSize: 12 }}
                  />
                  <Tooltip
                    formatter={(value) => [`${(value as number).toFixed(1)}%`, "Utilization"]}
                    contentStyle={{
                      backgroundColor: "var(--card)",
                      border: "1px solid var(--border)",
                    }}
                  />
                  <Bar
                    dataKey="utilizationPercent"
                    fill="hsl(var(--primary))"
                    radius={[0, 4, 4, 0]}
                  />
                </BarChart>
              </ResponsiveContainer>

              {/* Table */}
              <div className="rounded-md border">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th
                        className="px-4 py-3 text-left font-medium cursor-pointer select-none hover:bg-muted/80"
                        onClick={() => toggleSort("stationName")}
                      >
                        <span className="inline-flex items-center gap-1">
                          Station
                          <ArrowUpDown className="h-3 w-3 text-muted-foreground" />
                        </span>
                      </th>
                      <th
                        className="px-4 py-3 text-right font-medium cursor-pointer select-none hover:bg-muted/80"
                        onClick={() => toggleSort("totalSessions")}
                      >
                        <span className="inline-flex items-center justify-end gap-1">
                          Sessions
                          <ArrowUpDown className="h-3 w-3 text-muted-foreground" />
                        </span>
                      </th>
                      <th
                        className="px-4 py-3 text-right font-medium cursor-pointer select-none hover:bg-muted/80"
                        onClick={() => toggleSort("totalEnergyKwh")}
                      >
                        <span className="inline-flex items-center justify-end gap-1">
                          Energy
                          <ArrowUpDown className="h-3 w-3 text-muted-foreground" />
                        </span>
                      </th>
                      <th
                        className="px-4 py-3 text-right font-medium cursor-pointer select-none hover:bg-muted/80"
                        onClick={() => toggleSort("totalRevenue")}
                      >
                        <span className="inline-flex items-center justify-end gap-1">
                          Revenue
                          <ArrowUpDown className="h-3 w-3 text-muted-foreground" />
                        </span>
                      </th>
                      <th
                        className="px-4 py-3 text-right font-medium cursor-pointer select-none hover:bg-muted/80"
                        onClick={() => toggleSort("utilizationPercent")}
                      >
                        <span className="inline-flex items-center justify-end gap-1">
                          Utilization
                          <ArrowUpDown className="h-3 w-3 text-muted-foreground" />
                        </span>
                      </th>
                      <th
                        className="px-4 py-3 text-right font-medium cursor-pointer select-none hover:bg-muted/80"
                        onClick={() => toggleSort("onlinePercent")}
                      >
                        <span className="inline-flex items-center justify-end gap-1">
                          Online
                          <ArrowUpDown className="h-3 w-3 text-muted-foreground" />
                        </span>
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {sortedUtilization.map((station) => (
                      <tr key={station.stationId} className="border-b last:border-0">
                        <td className="px-4 py-3 font-medium">{station.stationName}</td>
                        <td className="px-4 py-3 text-right tabular-nums">{station.totalSessions}</td>
                        <td className="px-4 py-3 text-right tabular-nums">
                          {formatEnergy(station.totalEnergyKwh)}
                        </td>
                        <td className="px-4 py-3 text-right tabular-nums">
                          {formatCurrency(station.totalRevenue)}
                        </td>
                        <td className="px-4 py-3 text-right tabular-nums">
                          <Badge
                            variant={
                              station.utilizationPercent > 50
                                ? "success"
                                : station.utilizationPercent > 20
                                ? "default"
                                : "secondary"
                            }
                          >
                            {station.utilizationPercent.toFixed(1)}%
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-right tabular-nums">
                          <Badge
                            variant={
                              station.onlinePercent >= 95
                                ? "success"
                                : station.onlinePercent >= 90
                                ? "warning"
                                : "destructive"
                            }
                          >
                            {station.onlinePercent.toFixed(1)}%
                          </Badge>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ) : (
            <EmptyState
              icon={BarChart3}
              title="No station data"
              description="No utilization data available for the selected period"
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
