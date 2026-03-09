"use client";

import { useState, useCallback } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Plus,
  Search,
  MapPin,
  Power,
  PowerOff,
  Eye,
  Edit,
  Wifi,
} from "lucide-react";
import Link from "next/link";
import { PageHeader } from "@/components/ui/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { StatusBadge } from "@/components/ui/status-badge";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard } from "@/components/ui/skeleton";
import { stationsApi } from "@/lib/api";
import { formatDateTime } from "@/lib/utils";
import { useTranslation } from "@/lib/i18n";
import { useMonitoringHub } from "@/lib/signalr";

export default function StationsPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState("");
  const queryClient = useQueryClient();

  // SignalR real-time updates — refresh station list on status changes
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

  const { data: stationsData, isLoading } = useQuery({
    queryKey: ["stations", search],
    queryFn: async () => {
      const { data } = await stationsApi.getAll({ maxResultCount: 50, search: search || undefined });
      return data;
    },
  });

  const stations = stationsData?.items || [];

  const enableMutation = useMutation({
    mutationFn: (id: string) => stationsApi.enable(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["stations"] }),
  });

  const disableMutation = useMutation({
    mutationFn: (id: string) => stationsApi.disable(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["stations"] }),
  });

  return (
    <div className="flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center justify-between border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title={t("stations.title")} description={t("stations.description")}>
          {hubStatus === "connected" && (
            <div className="flex items-center gap-1.5 text-green-600">
              <Wifi className="h-3.5 w-3.5" />
              <span className="text-xs font-medium">{t("monitoring.live")}</span>
              <span className="flex h-1.5 w-1.5 rounded-full bg-green-500 animate-pulse" />
            </div>
          )}
        </PageHeader>
        <div className="flex items-center gap-3">
          <div className="relative w-72">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="search"
              placeholder={t("stations.searchPlaceholder")}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="h-10 w-full rounded-md border bg-background pl-9 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary"
            />
          </div>
          <Link href="/stations/new">
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              {t("stations.addStation")}
            </Button>
          </Link>
        </div>
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* Stations Grid */}
        {isLoading ? (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <SkeletonCard key={i} />
            ))}
          </div>
        ) : stations.length === 0 ? (
          <EmptyState
            icon={MapPin}
            title={t("stations.noStationsFound")}
            description={search ? t("stations.tryDifferentSearch") : t("stations.getStarted")}
          />
        ) : (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {stations.map((station: { id: string; stationCode: string; name: string; address: string; status: number; isEnabled: boolean; connectorCount?: number; lastHeartbeat?: string }) => (
              <Card key={station.id} className="relative">
                <CardHeader className="pb-2">
                  <div className="flex items-start justify-between">
                    <div>
                      <CardTitle className="text-lg">{station.name}</CardTitle>
                      <p className="text-xs font-mono text-muted-foreground">{station.stationCode}</p>
                      <div className="mt-1 flex items-center gap-1 text-sm text-muted-foreground">
                        <MapPin className="h-3 w-3" />
                        <span className="line-clamp-1">{station.address}</span>
                      </div>
                    </div>
                    {!station.isEnabled ? (
                      <Badge variant="secondary">{t("stations.disabled")}</Badge>
                    ) : (
                      <StatusBadge type="station" value={station.status} />
                    )}
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div className="grid grid-cols-2 gap-2 text-center">
                      <div className="rounded-md bg-muted p-2">
                        <div className="text-lg font-semibold tabular-nums">{station.connectorCount || 0}</div>
                        <div className="text-xs text-muted-foreground">{t("stations.connectors")}</div>
                      </div>
                      <div className="rounded-md bg-muted p-2">
                        <div className="text-xs font-medium">
                          {station.lastHeartbeat
                            ? formatDateTime(station.lastHeartbeat)
                            : t("stations.never")}
                        </div>
                        <div className="text-xs text-muted-foreground">{t("stations.lastHeartbeat")}</div>
                      </div>
                    </div>

                    {/* Actions */}
                    <div className="flex items-center gap-2">
                      <Link href={`/stations/${station.id}`} className="flex-1">
                        <Button variant="outline" size="sm" className="w-full">
                          <Eye className="mr-2 h-4 w-4" />
                          {t("stations.view")}
                        </Button>
                      </Link>
                      <Link href={`/stations/${station.id}/edit`}>
                        <Button variant="outline" size="icon">
                          <Edit className="h-4 w-4" />
                        </Button>
                      </Link>
                      {station.isEnabled ? (
                        <Button
                          variant="outline"
                          size="icon"
                          onClick={() => disableMutation.mutate(station.id)}
                          disabled={disableMutation.isPending}
                        >
                          <PowerOff className="h-4 w-4" />
                        </Button>
                      ) : (
                        <Button
                          variant="outline"
                          size="icon"
                          onClick={() => enableMutation.mutate(station.id)}
                          disabled={enableMutation.isPending}
                        >
                          <Power className="h-4 w-4" />
                        </Button>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
