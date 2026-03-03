"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Plus,
  Search,
  MapPin,
  Power,
  PowerOff,
  Eye,
  Edit,
} from "lucide-react";
import Link from "next/link";
import { Header } from "@/components/layout/header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { stationsApi } from "@/lib/api";
import { formatDateTime } from "@/lib/utils";

const StationStatusLabels: Record<number, string> = {
  0: "Offline",
  1: "Available",
  2: "Occupied",
  3: "Unavailable",
  4: "Faulted",
  5: "Decommissioned",
};

function getStatusBadge(status: number | string, isEnabled: boolean) {
  if (!isEnabled) {
    return <Badge variant="secondary">Disabled</Badge>;
  }
  const label = typeof status === "number" ? (StationStatusLabels[status] || "Unknown") : status;
  switch (label) {
    case "Available":
      return <Badge variant="success">Available</Badge>;
    case "Occupied":
      return <Badge variant="default">Occupied</Badge>;
    case "Offline":
      return <Badge variant="destructive">Offline</Badge>;
    case "Unavailable":
      return <Badge variant="secondary">Unavailable</Badge>;
    case "Faulted":
      return <Badge variant="warning">Faulted</Badge>;
    case "Decommissioned":
      return <Badge variant="secondary">Decommissioned</Badge>;
    default:
      return <Badge variant="secondary">{label}</Badge>;
  }
}

export default function StationsPage() {
  const [search, setSearch] = useState("");
  const queryClient = useQueryClient();

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
      <Header
        title="Station Management"
        description="Manage charging stations and connectors"
      />

      <div className="flex-1 space-y-6 p-6">
        {/* Actions Bar */}
        <div className="flex items-center justify-between">
          <div className="relative w-72">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="search"
              placeholder="Search stations..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="h-10 w-full rounded-md border bg-background pl-9 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <Link href="/stations/new">
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              Add Station
            </Button>
          </Link>
        </div>

        {/* Stations Grid */}
        {isLoading ? (
          <div className="text-center py-8">Loading...</div>
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
                    {getStatusBadge(station.status, station.isEnabled)}
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div className="grid grid-cols-2 gap-2 text-center">
                      <div className="rounded-md bg-muted p-2">
                        <div className="text-lg font-semibold">{station.connectorCount || 0}</div>
                        <div className="text-xs text-muted-foreground">Connectors</div>
                      </div>
                      <div className="rounded-md bg-muted p-2">
                        <div className="text-xs font-medium">
                          {station.lastHeartbeat
                            ? formatDateTime(station.lastHeartbeat)
                            : "Never"}
                        </div>
                        <div className="text-xs text-muted-foreground">Last Heartbeat</div>
                      </div>
                    </div>

                    {/* Actions */}
                    <div className="flex items-center gap-2">
                      <Link href={`/stations/${station.id}`} className="flex-1">
                        <Button variant="outline" size="sm" className="w-full">
                          <Eye className="mr-2 h-4 w-4" />
                          View
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

        {!isLoading && stations.length === 0 && (
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <MapPin className="h-12 w-12 text-muted-foreground" />
            <h3 className="mt-4 text-lg font-semibold">No stations found</h3>
            <p className="text-muted-foreground">
              {search ? "Try a different search term" : "Get started by adding a new station"}
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
