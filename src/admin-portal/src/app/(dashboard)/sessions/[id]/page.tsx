"use client";

import { useState, useEffect } from "react";
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
import { StatusBadge } from "@/components/ui/status-badge";
import { Skeleton, SkeletonCard } from "@/components/ui/skeleton";
import { sessionsApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import {
  formatCurrency,
  formatDateTime,
  formatEnergy,
  formatDurationFromSeconds,
  parseAsUtc,
} from "@/lib/utils";
import { CHART_COLORS } from "@/lib/constants";
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

function computeDurationSeconds(startTime?: string | null, endTime?: string | null): number {
  if (!startTime) return 0;
  const start = new Date(startTime).getTime();
  const end = endTime ? new Date(endTime).getTime() : Date.now();
  return Math.floor((end - start) / 1000);
}

function formatChartTime(timestamp: string): string {
  return new Intl.DateTimeFormat("vi-VN", {
    timeZone: "Asia/Ho_Chi_Minh",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  }).format(parseAsUtc(timestamp));
}

export default function SessionDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { t } = useTranslation();
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
        <Header title={t("sessions.detailTitle")} description={t("sessions.loadingSession")} />
        <div className="flex-1 space-y-6 p-6">
          <Skeleton className="h-9 w-40" />
          <div className="grid gap-6 md:grid-cols-2">
            <SkeletonCard />
            <SkeletonCard />
          </div>
          <Skeleton className="h-[350px] w-full rounded-lg" />
        </div>
      </div>
    );
  }

  if (!session) {
    return (
      <div className="flex flex-col">
        <Header title={t("sessions.detailTitle")} description={t("sessions.sessionNotFound")} />
        <div className="flex-1 p-6">
          <Button variant="outline" onClick={() => router.push("/sessions")}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            {t("sessions.backToSessions")}
          </Button>
          <div className="flex items-center justify-center py-20 text-muted-foreground">
            {t("sessions.sessionNotFound")}
          </div>
        </div>
      </div>
    );
  }

  // Live duration timer for InProgress sessions
  const isActive = session.status === 2 || session.status === 3; // InProgress or Suspended
  const [now, setNow] = useState(Date.now());
  useEffect(() => {
    if (!isActive) return;
    const interval = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(interval);
  }, [isActive]);

  const duration = isActive
    ? (session.startTime ? Math.floor((now - new Date(session.startTime).getTime()) / 1000) : 0)
    : computeDurationSeconds(session.startTime, session.endTime);

  return (
    <div className="flex flex-col">
      <Header
        title={t("sessions.detailTitle")}
        description={t("sessions.sessionAt").replace("{stationName}", session.stationName || t("sessions.unknownStation"))}
      />

      <div className="flex-1 space-y-6 p-6">
        {/* Back Button */}
        <Button variant="outline" onClick={() => router.push("/sessions")}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          {t("sessions.backToSessions")}
        </Button>

        {/* Session Overview & Cost Breakdown */}
        <div className="grid gap-6 md:grid-cols-2">
          {/* Session Overview */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <Activity className="h-5 w-5" />
                {t("sessions.sessionOverview")}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <MapPin className="h-4 w-4" />
                    {t("sessions.station")}
                  </span>
                  <span className="font-medium">{session.stationName || "—"}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Plug className="h-4 w-4" />
                    {t("sessions.connector")}
                  </span>
                  <span className="font-medium">#{session.connectorNumber}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Zap className="h-4 w-4" />
                    {t("common.status")}
                  </span>
                  <StatusBadge type="session" value={session.status} />
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Clock className="h-4 w-4" />
                    {t("sessions.startTime")}
                  </span>
                  <span className="text-sm">{formatDateTime(session.startTime)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Clock className="h-4 w-4" />
                    {t("sessions.endTime")}
                  </span>
                  <span className="text-sm">{formatDateTime(session.endTime)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Clock className="h-4 w-4" />
                    {t("sessions.duration")}
                  </span>
                  <span className="font-medium">{formatDurationFromSeconds(duration)}</span>
                </div>
                {session.vehicleName && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("sessions.vehicle")}</span>
                    <span className="font-medium">{session.vehicleName}</span>
                  </div>
                )}
                {session.idTag && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                      <Hash className="h-4 w-4" />
                      {t("sessions.idTag")}
                    </span>
                    <span className="font-mono text-sm">{session.idTag}</span>
                  </div>
                )}
                {session.stopReason && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("sessions.stopReason")}</span>
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
                {t("sessions.costBreakdown")}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Battery className="h-4 w-4" />
                    {t("sessions.energyDelivered")}
                  </span>
                  <span className="font-medium">{formatEnergy(session.totalEnergyKwh)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">{t("sessions.ratePerKwh")}</span>
                  <span className="font-medium">{formatCurrency(session.ratePerKwh)}</span>
                </div>
                {session.meterStart != null && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("sessions.meterStart")}</span>
                    <span className="font-mono text-sm">{session.meterStart} Wh</span>
                  </div>
                )}
                {session.meterStop != null && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("sessions.meterStop")}</span>
                    <span className="font-mono text-sm">{session.meterStop} Wh</span>
                  </div>
                )}
                <div className="border-t pt-4">
                  <div className="flex items-center justify-between">
                    <span className="text-base font-semibold">{t("sessions.totalCost")}</span>
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
              {t("sessions.meterValuesChart")}
            </CardTitle>
          </CardHeader>
          <CardContent>
            {meterLoading ? (
              <div className="space-y-4 py-4">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-[300px] w-full" />
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
                    name={t("sessions.energyKwh")}
                    stroke={CHART_COLORS.blue}
                    strokeWidth={2}
                    dot={false}
                    connectNulls
                  />
                  <Line
                    yAxisId="energy"
                    type="monotone"
                    dataKey="powerKw"
                    name={t("sessions.powerKw")}
                    stroke={CHART_COLORS.orange}
                    strokeWidth={2}
                    dot={false}
                    connectNulls
                  />
                  {hasSocData && (
                    <Line
                      yAxisId="soc"
                      type="monotone"
                      dataKey="socPercent"
                      name={t("sessions.socPercent")}
                      stroke={CHART_COLORS.green}
                      strokeWidth={2}
                      dot={false}
                      connectNulls
                    />
                  )}
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center py-12 text-muted-foreground">
                {t("sessions.noMeterData")}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Meter Values Table */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t("sessions.meterValues")}</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {meterLoading ? (
              <div className="space-y-3 p-4">
                {Array.from({ length: 5 }).map((_, i) => (
                  <Skeleton key={i} className="h-4 w-full" />
                ))}
              </div>
            ) : (meterValues || []).length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("sessions.timestamp")}</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("sessions.energyKwh")}</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("sessions.powerKw")}</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("sessions.currentA")}</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("sessions.voltageV")}</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("sessions.socPercent")}</th>
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
                {t("sessions.noMeterDataShort")}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
