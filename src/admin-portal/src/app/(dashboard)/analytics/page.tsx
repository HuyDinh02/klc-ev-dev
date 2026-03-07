"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { monitoringApi } from "@/lib/api";
import { formatCurrency, formatEnergy } from "@/lib/utils";
import {
  AreaChart,
  Area,
  BarChart,
  Bar,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";
import {
  TrendingUp,
  Zap,
  DollarSign,
  Clock,
  Activity,
  ArrowUpRight,
  ArrowDownRight,
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
}

interface AnalyticsData {
  dailyStats: DailyStats[];
  stationUtilization: StationUtilization[];
  totalRevenue: number;
  totalEnergyKwh: number;
  totalSessions: number;
  averageSessionDurationMinutes: number;
  uptimePercent: number;
}

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

export default function AnalyticsPage() {
  const [range, setRange] = useState<DateRange>("30d");

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
  };

  // Compute averages for daily comparison
  const dailyAvgRevenue =
    data.dailyStats.length > 0
      ? data.totalRevenue / data.dailyStats.length
      : 0;
  const dailyAvgSessions =
    data.dailyStats.length > 0
      ? data.totalSessions / data.dailyStats.length
      : 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Analytics</h1>
          <p className="text-muted-foreground">
            Revenue trends, utilization rates, and performance KPIs
          </p>
        </div>
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
      </div>

      {/* KPI Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Revenue</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {formatCurrency(data.totalRevenue)}
            </div>
            <p className="text-xs text-muted-foreground">
              ~{formatCurrency(dailyAvgRevenue)}/day avg
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Energy Delivered
            </CardTitle>
            <Zap className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {formatEnergy(data.totalEnergyKwh)}
            </div>
            <p className="text-xs text-muted-foreground">
              {data.totalSessions} sessions
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Avg Session Duration
            </CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {data.averageSessionDurationMinutes.toFixed(0)} min
            </div>
            <p className="text-xs text-muted-foreground">
              ~{dailyAvgSessions.toFixed(1)} sessions/day
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Network Uptime
            </CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {data.uptimePercent.toFixed(1)}%
            </div>
            <p className="text-xs text-muted-foreground">
              Current station availability
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              Avg Revenue/kWh
            </CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {data.totalEnergyKwh > 0
                ? formatCurrency(data.totalRevenue / data.totalEnergyKwh)
                : "0đ"}
            </div>
            <p className="text-xs text-muted-foreground">Effective rate</p>
          </CardContent>
        </Card>
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
            <div className="flex h-[300px] items-center justify-center text-muted-foreground">
              {isLoading ? "Loading..." : "No data for selected period"}
            </div>
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
                      <stop offset="5%" stopColor="#22c55e" stopOpacity={0.3} />
                      <stop offset="95%" stopColor="#22c55e" stopOpacity={0} />
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
                    stroke="#22c55e"
                    fill="url(#energyGradient)"
                    strokeWidth={2}
                  />
                </AreaChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex h-[250px] items-center justify-center text-muted-foreground">
                {isLoading ? "Loading..." : "No data"}
              </div>
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
                  <Bar dataKey="sessions" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex h-[250px] items-center justify-center text-muted-foreground">
                {isLoading ? "Loading..." : "No data"}
              </div>
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
                      <th className="px-4 py-3 text-left font-medium">Station</th>
                      <th className="px-4 py-3 text-right font-medium">Sessions</th>
                      <th className="px-4 py-3 text-right font-medium">Energy</th>
                      <th className="px-4 py-3 text-right font-medium">Revenue</th>
                      <th className="px-4 py-3 text-right font-medium">Utilization</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.stationUtilization.map((station) => (
                      <tr key={station.stationId} className="border-b last:border-0">
                        <td className="px-4 py-3 font-medium">{station.stationName}</td>
                        <td className="px-4 py-3 text-right">{station.totalSessions}</td>
                        <td className="px-4 py-3 text-right">
                          {formatEnergy(station.totalEnergyKwh)}
                        </td>
                        <td className="px-4 py-3 text-right">
                          {formatCurrency(station.totalRevenue)}
                        </td>
                        <td className="px-4 py-3 text-right">
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
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ) : (
            <div className="flex h-32 items-center justify-center text-muted-foreground">
              {isLoading ? "Loading..." : "No station data for selected period"}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
