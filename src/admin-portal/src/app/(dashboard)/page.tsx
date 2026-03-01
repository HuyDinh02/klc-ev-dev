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
import { monitoringApi } from "@/lib/api";
import { formatCurrency, formatEnergy } from "@/lib/utils";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
} from "recharts";

// Mock data for development
const mockDashboardData = {
  totalStations: 45,
  onlineStations: 42,
  totalConnectors: 156,
  availableConnectors: 89,
  chargingConnectors: 52,
  faultedConnectors: 5,
  activeSessions: 52,
  todayRevenue: 15750000,
  todayEnergy: 1250.5,
  activeAlerts: 3,
};

const mockRevenueChart = [
  { date: "Mon", revenue: 12500000, sessions: 145 },
  { date: "Tue", revenue: 14200000, sessions: 162 },
  { date: "Wed", revenue: 13800000, sessions: 158 },
  { date: "Thu", revenue: 15100000, sessions: 175 },
  { date: "Fri", revenue: 16800000, sessions: 192 },
  { date: "Sat", revenue: 18500000, sessions: 215 },
  { date: "Sun", revenue: 15750000, sessions: 180 },
];

const mockStationStatus = [
  { name: "Available", value: 89, color: "#22c55e" },
  { name: "Charging", value: 52, color: "#3b82f6" },
  { name: "Preparing", value: 10, color: "#eab308" },
  { name: "Faulted", value: 5, color: "#ef4444" },
];

export default function DashboardPage() {
  const { data: dashboardData, isLoading } = useQuery({
    queryKey: ["dashboard"],
    queryFn: async () => {
      // In production, use: const { data } = await monitoringApi.getDashboard();
      // return data;
      return mockDashboardData;
    },
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  const data = dashboardData || mockDashboardData;

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
              <div className="text-2xl font-bold">{formatCurrency(data.todayRevenue)}</div>
              <p className="text-xs text-muted-foreground">
                {formatEnergy(data.todayEnergy)} delivered
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Active Alerts</CardTitle>
              <AlertTriangle className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{data.activeAlerts}</div>
              <p className="text-xs text-muted-foreground">
                {data.faultedConnectors} faulted connectors
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Charts */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
          {/* Revenue Chart */}
          <Card className="col-span-4">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <TrendingUp className="h-5 w-5" />
                Weekly Revenue
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={mockRevenueChart}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="date" className="text-xs" />
                  <YAxis
                    tickFormatter={(value) => `${(value / 1000000).toFixed(0)}M`}
                    className="text-xs"
                  />
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value)}
                    labelStyle={{ color: "var(--foreground)" }}
                    contentStyle={{
                      backgroundColor: "var(--card)",
                      border: "1px solid var(--border)",
                    }}
                  />
                  <Line
                    type="monotone"
                    dataKey="revenue"
                    stroke="hsl(var(--primary))"
                    strokeWidth={2}
                    dot={{ fill: "hsl(var(--primary))" }}
                  />
                </LineChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* Connector Status */}
          <Card className="col-span-3">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="h-5 w-5" />
                Connector Status
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={mockStationStatus} layout="vertical">
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
        </div>

        {/* Recent Activity */}
        <Card>
          <CardHeader>
            <CardTitle>Recent Activity</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {[
                { type: "session", station: "Station HCM-001", event: "Charging started", time: "2 min ago", status: "info" },
                { type: "fault", station: "Station HCM-015", event: "Connector #2 faulted", time: "5 min ago", status: "error" },
                { type: "session", station: "Station HN-003", event: "Charging completed", time: "8 min ago", status: "success" },
                { type: "alert", station: "Station DN-007", event: "High temperature warning", time: "12 min ago", status: "warning" },
                { type: "session", station: "Station HCM-022", event: "Payment processed", time: "15 min ago", status: "success" },
              ].map((activity, index) => (
                <div key={index} className="flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <Badge
                      variant={
                        activity.status === "error"
                          ? "destructive"
                          : activity.status === "warning"
                          ? "warning"
                          : activity.status === "success"
                          ? "success"
                          : "secondary"
                      }
                    >
                      {activity.type}
                    </Badge>
                    <div>
                      <p className="text-sm font-medium">{activity.event}</p>
                      <p className="text-xs text-muted-foreground">{activity.station}</p>
                    </div>
                  </div>
                  <span className="text-xs text-muted-foreground">{activity.time}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
