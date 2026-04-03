"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { Search, Zap, Clock, Battery, DollarSign, Wifi, Download } from "lucide-react";
import { useTableQuery } from "@/hooks/use-table-query";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { StatusBadge } from "@/components/ui/status-badge";
import { SkeletonTable } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { sessionsApi } from "@/lib/api";
import { formatCurrency, formatDateTime, formatEnergy, formatDuration, downloadCsv, parseAsUtc } from "@/lib/utils";
import { useTranslation } from "@/lib/i18n";
import { useMonitoringHub } from "@/lib/signalr";
import { useRequirePermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";

export default function SessionsPage() {
  const hasAccess = useRequirePermission("KLC.Sessions");
  const { t } = useTranslation();
  const router = useRouter();
  const queryClient = useQueryClient();

  // SignalR real-time updates — refresh sessions on updates
  const onSessionUpdated = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ["sessions"] });
  }, [queryClient]);

  const { status: hubStatus } = useMonitoringHub({
    onSessionUpdated,
  });

  const {
    data: sessionsData,
    items: sessions,
    filteredItems: filteredSessions,
    isLoading,
    search,
    setSearch,
    statusFilter,
    setStatusFilterAndReset,
    pageSize,
    goNextPage,
    goPrevPage,
    hasNextPage,
    hasPrevPage,
  } = useTableQuery({
    queryKey: "sessions",
    fetchFn: async (params) => {
      const { data } = await sessionsApi.getAll(params as Parameters<typeof sessionsApi.getAll>[0]);
      return data;
    },
    refetchInterval: 3000,
    searchFields: ["stationName", "userName"],
  });

  const computeDuration = (startTime?: string | null, endTime?: string | null) => {
    if (!startTime) return 0;
    const start = parseAsUtc(startTime).getTime();
    const end = endTime ? parseAsUtc(endTime).getTime() : Date.now();
    return Math.floor((end - start) / 60000);
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- API returns untyped axios data
  const sessionsAny = sessions as any[];
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const filteredSessionsAny = filteredSessions as any[];
  const activeSessions = sessionsAny.filter((s) => s.status === 1 || s.status === 2 || s.status === "InProgress" || s.status === "Starting").length;
  const totalEnergy = sessionsAny.reduce((acc: number, s) => acc + (s.totalEnergyKwh || s.energyDeliveredKwh || 0), 0);
  const totalRevenue = sessionsAny.reduce((acc: number, s) => acc + (s.totalCost || s.cost || 0), 0);

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      {/* Sticky header with filter bar */}
      <div className="sticky top-0 z-10 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="p-6 pb-4">
          <PageHeader
            title={t("sessions.title")}
            description={t("sessions.description")}
          >
            <div className="flex items-center gap-2">
              {hubStatus === "connected" && (
                <div className="flex items-center gap-1.5 text-green-600">
                  <Wifi className="h-3.5 w-3.5" aria-hidden="true" />
                  <span className="text-xs font-medium">{t("monitoring.live")}</span>
                  <span className="flex h-1.5 w-1.5 rounded-full bg-green-500 animate-pulse" aria-hidden="true" />
                </div>
              )}
              <Button
                variant="outline"
                size="sm"
                disabled={filteredSessionsAny.length === 0}
                onClick={() => {
                  const headers = [t("sessions.station"), t("sessions.user"), t("common.status"), t("sessions.startTime"), t("sessions.duration"), t("sessions.energy"), t("sessions.cost")];
                  const rows = filteredSessionsAny.map((s: any) => [
                    s.stationName || "—",
                    s.userName || "—",
                    String(s.status),
                    formatDateTime(s.startTime),
                    formatDuration(computeDuration(s.startTime, s.endTime)),
                    formatEnergy(s.totalEnergyKwh || s.energyDeliveredKwh || 0),
                    formatCurrency(s.totalCost || s.cost || 0),
                  ]);
                  downloadCsv(headers, rows, `sessions-${new Date().toISOString().slice(0, 10)}.csv`);
                }}
              >
                <Download className="mr-1.5 h-4 w-4" aria-hidden="true" />
                {t("common.export")}
              </Button>
            </div>
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
              onClick={() => setStatusFilterAndReset("all")}
            >
              {t("common.all")}
            </Button>
            <Button
              variant={statusFilter === "2" ? "default" : "outline"}
              size="sm"
              onClick={() => setStatusFilterAndReset("2")}
            >
              {t("common.active")}
            </Button>
            <Button
              variant={statusFilter === "5" ? "default" : "outline"}
              size="sm"
              onClick={() => setStatusFilterAndReset("5")}
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
              {filteredSessionsAny.length > 0 ? (
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
                      {filteredSessionsAny.map((session: any) => (
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
              {((sessionsData?.totalCount ?? 0) > pageSize || hasPrevPage) && (
                <div className="flex items-center justify-between border-t px-4 py-3">
                  <p className="text-sm text-muted-foreground">
                    {sessionsData?.totalCount ?? 0} {t("sessions.totalSessionsCount")}
                  </p>
                  <div className="flex items-center gap-2">
                    {hasPrevPage && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={goPrevPage}
                      >
                        {t("common.previous")}
                      </Button>
                    )}
                    {hasNextPage && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={goNextPage}
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
