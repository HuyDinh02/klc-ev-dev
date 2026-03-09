"use client";

import { useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Search, Zap, Clock, Battery, DollarSign, Wifi } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { StatusBadge } from "@/components/ui/status-badge";
import { SkeletonTable } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { sessionsApi } from "@/lib/api";
import { formatCurrency, formatDateTime, formatEnergy, formatDuration } from "@/lib/utils";
import { useTranslation } from "@/lib/i18n";
import { useMonitoringHub } from "@/lib/signalr";

export default function SessionsPage() {
  const { t } = useTranslation();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [cursor, setCursor] = useState<string | null>(null);
  const [cursorStack, setCursorStack] = useState<(string | null)[]>([]);
  const pageSize = 20;

  // SignalR real-time updates — refresh sessions on updates
  const onSessionUpdated = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ["sessions"] });
  }, [queryClient]);

  const { status: hubStatus } = useMonitoringHub({
    onSessionUpdated,
  });

  const { data: sessionsData, isLoading } = useQuery({
    queryKey: ["sessions", statusFilter, cursor],
    queryFn: async () => {
      const params: Record<string, unknown> = {
        maxResultCount: pageSize,
      };
      if (statusFilter !== "all") params.status = Number(statusFilter);
      if (cursor) params.cursor = cursor;
      const { data } = await sessionsApi.getAll(params as Parameters<typeof sessionsApi.getAll>[0]);
      return data;
    },
    refetchInterval: 10000,
  });

  const sessions = sessionsData?.items || [];

  const filteredSessions = sessions.filter((session: { stationName?: string; userName?: string }) => {
    if (!search) return true;
    const s = search.toLowerCase();
    return (
      (session.stationName || "").toLowerCase().includes(s) ||
      (session.userName || "").toLowerCase().includes(s)
    );
  });

  const computeDuration = (startTime?: string | null, endTime?: string | null) => {
    if (!startTime) return 0;
    const start = new Date(startTime).getTime();
    const end = endTime ? new Date(endTime).getTime() : Date.now();
    return Math.floor((end - start) / 60000);
  };

  const activeSessions = sessions.filter((s: { status: number | string }) => s.status === 1 || s.status === 2 || s.status === "InProgress" || s.status === "Starting").length;
  const totalEnergy = sessions.reduce((acc: number, s: { totalEnergyKwh?: number; energyDeliveredKwh?: number }) => acc + (s.totalEnergyKwh || s.energyDeliveredKwh || 0), 0);
  const totalRevenue = sessions.reduce((acc: number, s: { totalCost?: number; cost?: number }) => acc + (s.totalCost || s.cost || 0), 0);

  return (
    <div className="flex flex-col">
      {/* Sticky header with filter bar */}
      <div className="sticky top-0 z-10 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="p-6 pb-4">
          <PageHeader
            title={t("sessions.title")}
            description={t("sessions.description")}
          >
            {hubStatus === "connected" && (
              <div className="flex items-center gap-1.5 text-green-600">
                <Wifi className="h-3.5 w-3.5" aria-hidden="true" />
                <span className="text-xs font-medium">{t("monitoring.live")}</span>
                <span className="flex h-1.5 w-1.5 rounded-full bg-green-500 animate-pulse" aria-hidden="true" />
              </div>
            )}
          </PageHeader>
        </div>

        {/* Filters */}
        <div className="flex items-center gap-4 px-6 pb-4">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
            <input
              type="search"
              placeholder={t("sessions.searchPlaceholder")}
              aria-label={t("sessions.searchPlaceholder")}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="h-10 w-full rounded-md border bg-background pl-9 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary"
            />
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant={statusFilter === "all" ? "default" : "outline"}
              size="sm"
              onClick={() => { setStatusFilter("all"); setCursor(null); setCursorStack([]); }}
            >
              {t("common.all")}
            </Button>
            <Button
              variant={statusFilter === "2" ? "default" : "outline"}
              size="sm"
              onClick={() => { setStatusFilter("2"); setCursor(null); setCursorStack([]); }}
            >
              {t("common.active")}
            </Button>
            <Button
              variant={statusFilter === "5" ? "default" : "outline"}
              size="sm"
              onClick={() => { setStatusFilter("5"); setCursor(null); setCursorStack([]); }}
            >
              {t("sessions.completed")}
            </Button>
          </div>
        </div>
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* Stats */}
        <div className="grid gap-4 md:grid-cols-4">
          <StatCard
            label={t("sessions.activeSessions")}
            value={activeSessions}
            icon={Zap}
            iconColor="bg-blue-50 text-blue-600"
          />
          <StatCard
            label={t("sessions.energyDelivered")}
            value={formatEnergy(totalEnergy)}
            icon={Battery}
            iconColor="bg-[var(--color-brand-orange)]/10 text-[var(--color-brand-orange-dark)]"
          />
          <StatCard
            label={t("sessions.totalRevenue")}
            value={formatCurrency(totalRevenue)}
            icon={DollarSign}
            iconColor="bg-green-50 text-green-600"
          />
          <StatCard
            label={t("sessions.totalSessions")}
            value={sessionsData?.totalCount || sessions.length}
            icon={Clock}
            iconColor="bg-purple-50 text-purple-600"
          />
        </div>

        {/* Sessions Table */}
        {isLoading ? (
          <SkeletonTable rows={8} cols={7} />
        ) : (
          <Card>
            <CardContent className="p-0">
              {filteredSessions.length > 0 ? (
                <div className="overflow-x-auto">
                  <table className="w-full">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="px-4 py-3 text-left text-sm font-medium">{t("sessions.station")}</th>
                        <th className="px-4 py-3 text-left text-sm font-medium">{t("sessions.user")}</th>
                        <th className="px-4 py-3 text-left text-sm font-medium">{t("common.status")}</th>
                        <th className="px-4 py-3 text-left text-sm font-medium">{t("sessions.startTime")}</th>
                        <th className="px-4 py-3 text-left text-sm font-medium">{t("sessions.duration")}</th>
                        <th className="px-4 py-3 text-right text-sm font-medium">{t("sessions.energy")}</th>
                        <th className="px-4 py-3 text-right text-sm font-medium">{t("sessions.cost")}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredSessions.map((session: { id: string; stationName?: string; connectorNumber?: number; userName?: string; status: number | string; startTime: string; endTime?: string | null; totalEnergyKwh?: number; energyDeliveredKwh?: number; totalCost?: number; cost?: number }) => (
                        <tr key={session.id} className="border-b hover:bg-muted/50 cursor-pointer" onClick={() => router.push(`/sessions/${session.id}`)}>
                          <td className="px-4 py-3">
                            <div>
                              <p className="font-medium">{session.stationName || "—"}</p>
                              {session.connectorNumber && (
                                <p className="text-xs text-muted-foreground">
                                  {t("sessions.connector")} #{session.connectorNumber}
                                </p>
                              )}
                            </div>
                          </td>
                          <td className="px-4 py-3">{session.userName || "—"}</td>
                          <td className="px-4 py-3">
                            <StatusBadge type="session" value={typeof session.status === "number" ? session.status : 0} />
                          </td>
                          <td className="px-4 py-3 text-sm">
                            {formatDateTime(session.startTime)}
                          </td>
                          <td className="px-4 py-3">
                            {formatDuration(computeDuration(session.startTime, session.endTime))}
                          </td>
                          <td className="px-4 py-3 text-right tabular-nums">
                            {formatEnergy(session.totalEnergyKwh || session.energyDeliveredKwh || 0)}
                          </td>
                          <td className="px-4 py-3 text-right tabular-nums font-medium">
                            {formatCurrency(session.totalCost || session.cost || 0)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <EmptyState
                  icon={Zap}
                  title={t("sessions.noSessionsFound")}
                  description={t("sessions.noSessionsDescription")}
                />
              )}

              {/* Pagination */}
              {((sessionsData?.totalCount ?? 0) > pageSize || cursorStack.length > 0) && (
                <div className="flex items-center justify-between border-t px-4 py-3">
                  <p className="text-sm text-muted-foreground">
                    {sessionsData?.totalCount ?? 0} {t("sessions.totalSessionsCount")}
                  </p>
                  <div className="flex items-center gap-2">
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
                        {t("common.previous")}
                      </Button>
                    )}
                    {sessions.length === pageSize && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          const lastId = sessions[sessions.length - 1]?.id;
                          if (lastId) {
                            setCursorStack([...cursorStack, cursor]);
                            setCursor(lastId);
                          }
                        }}
                      >
                        {t("common.next")}
                      </Button>
                    )}
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
