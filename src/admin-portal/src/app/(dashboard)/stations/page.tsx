"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Plus,
  Search,
  MoreHorizontal,
  MapPin,
  Power,
  PowerOff,
  Trash2,
  Eye,
  Edit,
} from "lucide-react";
import Link from "next/link";
import { Header } from "@/components/layout/header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { stationsApi } from "@/lib/api";

// Mock data for development
const mockStations = [
  {
    id: "1",
    name: "Station HCM-001",
    address: "123 Nguyen Van Linh, Q7, HCM",
    status: "Online",
    isEnabled: true,
    totalConnectors: 4,
    availableConnectors: 2,
    chargingConnectors: 2,
    todayRevenue: 2500000,
    todaySessions: 15,
  },
  {
    id: "2",
    name: "Station HCM-002",
    address: "456 Le Van Viet, Q9, HCM",
    status: "Online",
    isEnabled: true,
    totalConnectors: 6,
    availableConnectors: 4,
    chargingConnectors: 2,
    todayRevenue: 3200000,
    todaySessions: 22,
  },
  {
    id: "3",
    name: "Station HN-001",
    address: "789 Pham Hung, Nam Tu Liem, HN",
    status: "Offline",
    isEnabled: false,
    totalConnectors: 4,
    availableConnectors: 0,
    chargingConnectors: 0,
    todayRevenue: 0,
    todaySessions: 0,
  },
  {
    id: "4",
    name: "Station DN-001",
    address: "101 Vo Van Kiet, Hai Chau, DN",
    status: "Online",
    isEnabled: true,
    totalConnectors: 2,
    availableConnectors: 1,
    chargingConnectors: 1,
    todayRevenue: 1800000,
    todaySessions: 12,
  },
];

function getStatusBadge(status: string, isEnabled: boolean) {
  if (!isEnabled) {
    return <Badge variant="secondary">Disabled</Badge>;
  }
  switch (status) {
    case "Online":
      return <Badge variant="success">Online</Badge>;
    case "Offline":
      return <Badge variant="destructive">Offline</Badge>;
    default:
      return <Badge variant="warning">{status}</Badge>;
  }
}

export default function StationsPage() {
  const [search, setSearch] = useState("");
  const queryClient = useQueryClient();

  const { data: stations, isLoading } = useQuery({
    queryKey: ["stations"],
    queryFn: async () => {
      // In production: const { data } = await stationsApi.getAll();
      // return data.items;
      return mockStations;
    },
  });

  const enableMutation = useMutation({
    mutationFn: (id: string) => stationsApi.enable(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["stations"] }),
  });

  const disableMutation = useMutation({
    mutationFn: (id: string) => stationsApi.disable(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["stations"] }),
  });

  const filteredStations = stations?.filter((station) =>
    station.name.toLowerCase().includes(search.toLowerCase()) ||
    station.address.toLowerCase().includes(search.toLowerCase())
  );

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
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {filteredStations?.map((station) => (
            <Card key={station.id} className="relative">
              <CardHeader className="pb-2">
                <div className="flex items-start justify-between">
                  <div>
                    <CardTitle className="text-lg">{station.name}</CardTitle>
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
                  {/* Connector Status */}
                  <div className="grid grid-cols-3 gap-2 text-center">
                    <div className="rounded-md bg-muted p-2">
                      <div className="text-lg font-semibold">{station.totalConnectors}</div>
                      <div className="text-xs text-muted-foreground">Total</div>
                    </div>
                    <div className="rounded-md bg-green-500/10 p-2">
                      <div className="text-lg font-semibold text-green-600">
                        {station.availableConnectors}
                      </div>
                      <div className="text-xs text-muted-foreground">Available</div>
                    </div>
                    <div className="rounded-md bg-blue-500/10 p-2">
                      <div className="text-lg font-semibold text-blue-600">
                        {station.chargingConnectors}
                      </div>
                      <div className="text-xs text-muted-foreground">Charging</div>
                    </div>
                  </div>

                  {/* Today Stats */}
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Today</span>
                    <div className="flex items-center gap-4">
                      <span>{station.todaySessions} sessions</span>
                      <span className="font-medium">
                        {new Intl.NumberFormat("vi-VN").format(station.todayRevenue)}đ
                      </span>
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

        {filteredStations?.length === 0 && (
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
