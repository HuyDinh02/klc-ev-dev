"use client";

import { useState, useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Plus,
  Search,
  MapPin,
  LayoutGrid,
  List,
  Wifi,
} from "lucide-react";
import { useTableQuery } from "@/hooks/use-table-query";
import Link from "next/link";
import { PageHeader } from "@/components/ui/page-header";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard, SkeletonTable } from "@/components/ui/skeleton";
import { stationsApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useMonitoringHub } from "@/lib/signalr";
import { usePreferencesStore } from "@/lib/store";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import { StationBoardView, StationListView } from "@/components/stations";
import type { StationListItem } from "@/components/stations";

export default function StationsPage() {
  const hasAccess = useRequirePermission("KLC.Stations");
  const canCreate = useHasPermission("KLC.Stations.Create");
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const { stationsViewMode, setStationsViewMode } = usePreferencesStore();
  const [serverSearch, setServerSearch] = useState("");
  const [sortBy, setSortBy] = useState("name");
  const [sortOrder, setSortOrder] = useState<"asc" | "desc">("asc");

  // SignalR real-time updates
  const onStationStatusChanged = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ["stations"] });
  }, [queryClient]);

  const onConnectorStatusChanged = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ["stations"] });
  }, [queryClient]);

  const { status: hubStatus } = useMonitoringHub({
    onStationStatusChanged,
    onConnectorStatusChanged,
  });

  const {
    data: stationsData,
    items: stations,
    isLoading,
    statusFilter,
    setStatusFilterAndReset,
    pageSize,
    goNextPage,
    goPrevPage,
    hasNextPage,
    hasPrevPage,
    resetPage,
  } = useTableQuery<StationListItem>({
    queryKey: "stations",
    fetchFn: async (params) => {
      params.sortBy = sortBy;
      params.sortOrder = sortOrder;
      if (serverSearch) params.search = serverSearch;
      const { data } = await stationsApi.getAll(params as Parameters<typeof stationsApi.getAll>[0]);
      return data;
    },
    extraQueryKeys: [serverSearch, sortBy, sortOrder],
  });

  const enableMutation = useMutation({
    mutationFn: (id: string) => stationsApi.enable(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["stations"] }),
  });

  const disableMutation = useMutation({
    mutationFn: (id: string) => stationsApi.disable(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["stations"] }),
  });

  const isMutating = enableMutation.isPending || disableMutation.isPending;

  const handleSort = (field: string) => {
    if (sortBy === field) {
      setSortOrder(sortOrder === "asc" ? "desc" : "asc");
    } else {
      setSortBy(field);
      setSortOrder("asc");
    }
    resetPage();
  };

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center justify-between border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title={t("stations.title")} description={t("stations.description")}>
          {hubStatus === "connected" && (
            <div className="flex items-center gap-1.5 text-green-600">
              <Wifi className="h-3.5 w-3.5" aria-hidden="true" />
              <span className="text-xs font-medium">{t("monitoring.live")}</span>
              <span className="flex h-1.5 w-1.5 rounded-full bg-green-500 animate-pulse" aria-hidden="true" />
            </div>
          )}
        </PageHeader>
        <div className="flex items-center gap-3">
          <div className="relative w-72">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
            <input
              type="search"
              placeholder={t("stations.searchPlaceholder")}
              aria-label={t("stations.searchPlaceholder")}
              value={serverSearch}
              onChange={(e) => {
                setServerSearch(e.target.value);
                resetPage();
              }}
              className="h-10 w-full rounded-md border bg-background pl-9 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary"
            />
          </div>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilterAndReset(e.target.value)}
            className="h-10 rounded-md border bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary"
            aria-label={t("common.status")}
          >
            <option value="all">{t("stations.allStatuses")}</option>
            <option value="0">{t("stations.offline")}</option>
            <option value="1">{t("stations.online")}</option>
            <option value="2">{t("stations.disabled")}</option>
            <option value="3">{t("stations.decommissioned")}</option>
          </select>
          <div className="flex items-center rounded-md border">
            <Button
              variant={stationsViewMode === "board" ? "default" : "ghost"}
              size="icon"
              className="rounded-r-none"
              onClick={() => setStationsViewMode("board")}
              aria-label={t("stations.boardView")}
            >
              <LayoutGrid className="h-4 w-4" />
            </Button>
            <Button
              variant={stationsViewMode === "list" ? "default" : "ghost"}
              size="icon"
              className="rounded-l-none"
              onClick={() => setStationsViewMode("list")}
              aria-label={t("stations.listView")}
            >
              <List className="h-4 w-4" />
            </Button>
          </div>
          {canCreate && (
            <Link href="/stations/new">
              <Button>
                <Plus className="mr-2 h-4 w-4" aria-hidden="true" />
                {t("stations.addStation")}
              </Button>
            </Link>
          )}
        </div>
      </div>

      <div className="flex-1 space-y-6 p-6" aria-live="polite">
        {isLoading ? (
          stationsViewMode === "board" ? (
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3" role="status" aria-label="Loading stations">
              {Array.from({ length: 6 }).map((_, i) => (
                <SkeletonCard key={i} />
              ))}
            </div>
          ) : (
            <SkeletonTable rows={8} cols={7} />
          )
        ) : stations.length === 0 ? (
          <EmptyState
            icon={MapPin}
            title={t("stations.noStationsFound")}
            description={serverSearch || statusFilter !== "all" ? t("stations.tryDifferentSearch") : t("stations.getStarted")}
          />
        ) : (
          <>
            {stationsViewMode === "board" ? (
              <StationBoardView
                stations={stations}
                onEnable={(id) => enableMutation.mutate(id)}
                onDisable={(id) => disableMutation.mutate(id)}
                isMutating={isMutating}
              />
            ) : (
              <StationListView
                stations={stations}
                sortBy={sortBy}
                sortOrder={sortOrder}
                onSort={handleSort}
                onEnable={(id) => enableMutation.mutate(id)}
                onDisable={(id) => disableMutation.mutate(id)}
                isMutating={isMutating}
              />
            )}

            {/* Pagination */}
            {((stationsData?.totalCount ?? 0) > pageSize || hasPrevPage) && (
              <div className="flex items-center justify-between">
                <p className="text-sm text-muted-foreground">
                  {stationsData?.totalCount ?? 0} {t("stations.totalStations")}
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
          </>
        )}
      </div>
    </div>
  );
}
