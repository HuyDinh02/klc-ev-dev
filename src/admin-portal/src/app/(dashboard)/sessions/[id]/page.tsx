"use client";

import { useParams, useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  Zap,
  Clock,
  Battery,
  DollarSign,
  MapPin,
  Plug,
  Activity,
  Hash,
} from "lucide-react";
import { Header } from "@/components/layout/header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { sessionsApi } from "@/lib/api";
import {
  formatCurrency,
  formatDateTime,
  formatEnergy,
  formatDuration,
} from "@/lib/utils";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";

const SessionStatusLabels: Record<number, string> = {
  0: "Pending",
  1: "Starting",
  2: "InProgress",
  3: "Suspended",
  4: "Stopping",
  5: "Completed",
  6: "Failed",
};

function getStatusBadge(status: number | string) {
  const label =
    typeof status === "number"
      ? SessionStatusLabels[status] || "Unknown"
      : status;
  switch (label) {
    case "InProgress":
      return <Badge variant="default">Charging</Badge>;
    case "Starting":
      return <Badge variant="default">Starting</Badge>;
    case "Completed":
      return <Badge variant="success">Completed</Badge>;
    case "Failed":
      return <Badge variant="destructive">Failed</Badge>;
    case "Pending":
      return <Badge variant="secondary">Pending</Badge>;
    case "Suspended":
      return <Badge variant="warning">Suspended</Badge>;
    case "Stopping":
      return <Badge variant="secondary">Stopping</Badge>;
    default:
      return <Badge variant="secondary">{label}</Badge>;
  }
}

interface SessionDetail {
  id: string;
  userId: string;
  vehicleId?: string;
  stationId: string;
  connectorNumber: number;
  ocppTransactionId?: number;
  status: number;
  startTime?: string;
  endTime?: string;
  meterStart?: number;
  meterStop?: number;
  totalEnergyKwh: number;
  totalCost: number;
  tariffPlanId?: string;
  ratePerKwh: number;
  stopReason?: string;
  idTag?: string;
  stationName?: string;
  vehicleName?: string;
}

interface MeterValue {
  id: string;
  timestamp: string;
  energyKwh: number;
  currentAmps?: number;
  voltageVolts?: number;
  powerKw?: number;
  socPercent?: number;
}

function computeDuration(startTime?: string | null, endTime?: string | null): number {
  if (!startTime) return 0;
  const start = new Date(startTime).getTime();
  const end = endTime ? new Date(endTime).getTime() : Date.now();
  return Math.floor((end - start) / 60000);
}

function formatChartTime(timestamp: string): string {
  return new Intl.DateTimeFormat("vi-VN", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  }).format(new Date(timestamp));
}

export default function SessionDetailPage() {
  const params = useParams();
  const router = useRouter();
  const sessionId = params.id as string;

  const { data: session, isLoading: sessionLoading } = useQuery({
    queryKey: ["session", sessionId],
    queryFn: async () => {
      const { data } = await sessionsApi.getById(sessionId);
      return data as SessionDetail;
    },
    enabled: !!sessionId,
  });

  const { data: meterValues, isLoading: meterLoading } = useQuery({
    queryKey: ["session-meter-values", sessionId],
    queryFn: async () => {
      const { data } = await sessionsApi.getMeterValues(sessionId);
      return (Array.isArray(data) ? data : data.items || []) as MeterValue[];
    },
    enabled: !!sessionId,
  });

  const hasSocData = meterValues?.some((mv) => mv.socPercent != null) ?? false;

  const chartData = (meterValues || []).map((mv) => ({
    time: formatChartTime(mv.timestamp),
    energyKwh: Number(mv.energyKwh?.toFixed(2) ?? 0),
    powerKw: mv.powerKw != null ? Number(mv.powerKw.toFixed(2)) : undefined,
    socPercent: mv.socPercent != null ? Number(mv.socPercent.toFixed(1)) : undefined,
  }));

  if (sessionLoading) {
    return (
      <div className="flex flex-col">
        <Header title="Session Detail" description="Loading session data..." />
        <div className="flex-1 p-6">
          <div className="flex items-center justify-center py-20 text-muted-foreground">
            Loading...
          </div>
        </div>
      </div>
    );
  }

  if (!session) {
    return (
      <div className="flex flex-col">
        <Header title="Session Detail" description="Session not found" />
        <div className="flex-1 p-6">
          <Button variant="outline" onClick={() => router.push("/sessions")}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Sessions
          </Button>
          <div className="flex items-center justify-center py-20 text-muted-foreground">
            Session not found
          </div>
        </div>
      </div>
    );
  }

  const duration = computeDuration(session.startTime, session.endTime);

  return (
    <div className="flex flex-col">
      <Header
        title="Session Detail"
        description={`Session at ${session.stationName || "Unknown Station"}`}
      />

      <div className="flex-1 space-y-6 p-6">
        {/* Back Button */}
        <Button variant="outline" onClick={() => router.push("/sessions")}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to Sessions
        </Button>

        {/* Session Overview & Cost Breakdown */}
        <div className="grid gap-6 md:grid-cols-2">
          {/* Session Overview */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <Activity className="h-5 w-5" />
                Session Overview
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <MapPin className="h-4 w-4" />
                    Station
                  </span>
                  <span className="font-medium">{session.stationName || "—"}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Plug className="h-4 w-4" />
                    Connector
                  </span>
                  <span className="font-medium">#{session.connectorNumber}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Zap className="h-4 w-4" />
                    Status
                  </span>
                  {getStatusBadge(session.status)}
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Clock className="h-4 w-4" />
                    Start Time
                  </span>
                  <span className="text-sm">{formatDateTime(session.startTime)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Clock className="h-4 w-4" />
                    End Time
                  </span>
                  <span className="text-sm">{formatDateTime(session.endTime)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Clock className="h-4 w-4" />
                    Duration
                  </span>
                  <span className="font-medium">{formatDuration(duration)}</span>
                </div>
                {session.vehicleName && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Vehicle</span>
                    <span className="font-medium">{session.vehicleName}</span>
                  </div>
                )}
                {session.idTag && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                      <Hash className="h-4 w-4" />
                      ID Tag
                    </span>
                    <span className="font-mono text-sm">{session.idTag}</span>
                  </div>
                )}
                {session.stopReason && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Stop Reason</span>
                    <span className="text-sm">{session.stopReason}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Cost Breakdown */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <DollarSign className="h-5 w-5" />
                Cost Breakdown
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Battery className="h-4 w-4" />
                    Energy Delivered
                  </span>
                  <span className="font-medium">{formatEnergy(session.totalEnergyKwh)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Rate per kWh</span>
                  <span className="font-medium">{formatCurrency(session.ratePerKwh)}</span>
                </div>
                {session.meterStart != null && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Meter Start</span>
                    <span className="font-mono text-sm">{session.meterStart} Wh</span>
                  </div>
                )}
                {session.meterStop != null && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Meter Stop</span>
                    <span className="font-mono text-sm">{session.meterStop} Wh</span>
                  </div>
                )}
                <div className="border-t pt-4">
                  <div className="flex items-center justify-between">
                    <span className="text-base font-semibold">Total Cost</span>
                    <span className="text-xl font-bold text-primary">
                      {formatCurrency(session.totalCost)}
                    </span>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Meter Values Chart */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <Activity className="h-5 w-5" />
              Meter Values Chart
            </CardTitle>
          </CardHeader>
          <CardContent>
            {meterLoading ? (
              <div className="flex items-center justify-center py-12 text-muted-foreground">
                Loading meter data...
              </div>
            ) : chartData.length > 0 ? (
              <ResponsiveContainer width="100%" height={350}>
                <LineChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis
                    dataKey="time"
                    className="text-xs"
                    tick={{ fontSize: 11 }}
                    interval="preserveStartEnd"
                  />
                  <YAxis
                    yAxisId="energy"
                    className="text-xs"
                    tick={{ fontSize: 11 }}
                    label={{
                      value: "kWh / kW",
                      angle: -90,
                      position: "insideLeft",
                      style: { fontSize: 11 },
                    }}
                  />
                  {hasSocData && (
                    <YAxis
                      yAxisId="soc"
                      orientation="right"
                      domain={[0, 100]}
                      className="text-xs"
                      tick={{ fontSize: 11 }}
                      label={{
                        value: "SoC %",
                        angle: 90,
                        position: "insideRight",
                        style: { fontSize: 11 },
                      }}
                    />
                  )}
                  <Tooltip
                    contentStyle={{
                      backgroundColor: "var(--card)",
                      border: "1px solid var(--border)",
                      borderRadius: "8px",
                      fontSize: "12px",
                    }}
                  />
                  <Legend />
                  <Line
                    yAxisId="energy"
                    type="monotone"
                    dataKey="energyKwh"
                    name="Energy (kWh)"
                    stroke="#3b82f6"
                    strokeWidth={2}
                    dot={false}
                    connectNulls
                  />
                  <Line
                    yAxisId="energy"
                    type="monotone"
                    dataKey="powerKw"
                    name="Power (kW)"
                    stroke="#f59e0b"
                    strokeWidth={2}
                    dot={false}
                    connectNulls
                  />
                  {hasSocData && (
                    <Line
                      yAxisId="soc"
                      type="monotone"
                      dataKey="socPercent"
                      name="SoC (%)"
                      stroke="#22c55e"
                      strokeWidth={2}
                      dot={false}
                      connectNulls
                    />
                  )}
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center py-12 text-muted-foreground">
                No meter data available for this session
              </div>
            )}
          </CardContent>
        </Card>

        {/* Meter Values Table */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Meter Values</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {meterLoading ? (
              <div className="flex items-center justify-center py-12 text-muted-foreground">
                Loading...
              </div>
            ) : (meterValues || []).length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-4 py-3 text-left text-sm font-medium">Timestamp</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Energy (kWh)</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Power (kW)</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Current (A)</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Voltage (V)</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">SoC (%)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(meterValues || []).map((mv) => (
                      <tr key={mv.id} className="border-b hover:bg-muted/50">
                        <td className="px-4 py-3 text-sm">{formatDateTime(mv.timestamp)}</td>
                        <td className="px-4 py-3 text-sm font-mono">
                          {mv.energyKwh?.toFixed(2) ?? "—"}
                        </td>
                        <td className="px-4 py-3 text-sm font-mono">
                          {mv.powerKw != null ? mv.powerKw.toFixed(2) : "—"}
                        </td>
                        <td className="px-4 py-3 text-sm font-mono">
                          {mv.currentAmps != null ? mv.currentAmps.toFixed(2) : "—"}
                        </td>
                        <td className="px-4 py-3 text-sm font-mono">
                          {mv.voltageVolts != null ? mv.voltageVolts.toFixed(1) : "—"}
                        </td>
                        <td className="px-4 py-3 text-sm font-mono">
                          {mv.socPercent != null ? `${mv.socPercent.toFixed(1)}%` : "—"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="flex items-center justify-center py-12 text-muted-foreground">
                No meter data available
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
