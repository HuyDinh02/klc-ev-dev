"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search, Filter, Zap, Clock, Battery, DollarSign } from "lucide-react";
import { Header } from "@/components/layout/header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatDateTime, formatEnergy, formatDuration } from "@/lib/utils";

// Mock data for development
const mockSessions = [
  {
    id: "1",
    stationName: "Station HCM-001",
    connectorNumber: 1,
    userName: "Nguyen Van A",
    vehiclePlate: "51A-12345",
    status: "Charging",
    startTime: "2024-01-15T10:30:00",
    endTime: null,
    durationMinutes: 45,
    energyDelivered: 25.5,
    cost: 127500,
    soc: 65,
  },
  {
    id: "2",
    stationName: "Station HCM-002",
    connectorNumber: 2,
    userName: "Tran Thi B",
    vehiclePlate: "51A-67890",
    status: "Completed",
    startTime: "2024-01-15T09:00:00",
    endTime: "2024-01-15T10:15:00",
    durationMinutes: 75,
    energyDelivered: 42.3,
    cost: 211500,
    soc: 100,
  },
  {
    id: "3",
    stationName: "Station HN-001",
    connectorNumber: 1,
    userName: "Le Van C",
    vehiclePlate: "30A-11111",
    status: "Charging",
    startTime: "2024-01-15T10:45:00",
    endTime: null,
    durationMinutes: 30,
    energyDelivered: 18.2,
    cost: 91000,
    soc: 45,
  },
  {
    id: "4",
    stationName: "Station DN-001",
    connectorNumber: 1,
    userName: "Pham Van D",
    vehiclePlate: "43A-22222",
    status: "Completed",
    startTime: "2024-01-15T08:00:00",
    endTime: "2024-01-15T09:30:00",
    durationMinutes: 90,
    energyDelivered: 55.0,
    cost: 275000,
    soc: 100,
  },
];

function getStatusBadge(status: string) {
  switch (status) {
    case "Charging":
      return <Badge variant="default">Charging</Badge>;
    case "Completed":
      return <Badge variant="success">Completed</Badge>;
    case "Failed":
      return <Badge variant="destructive">Failed</Badge>;
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
}

export default function SessionsPage() {
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");

  const { data: sessions, isLoading } = useQuery({
    queryKey: ["sessions", statusFilter],
    queryFn: async () => {
      // In production: const { data } = await sessionsApi.getAll({ status: statusFilter });
      // return data.items;
      return mockSessions;
    },
  });

  const filteredSessions = sessions?.filter((session) => {
    const matchesSearch =
      session.stationName.toLowerCase().includes(search.toLowerCase()) ||
      session.userName.toLowerCase().includes(search.toLowerCase()) ||
      session.vehiclePlate.toLowerCase().includes(search.toLowerCase());
    const matchesStatus = statusFilter === "all" || session.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const activeSessions = sessions?.filter((s) => s.status === "Charging").length || 0;
  const totalEnergy = sessions?.reduce((acc, s) => acc + s.energyDelivered, 0) || 0;
  const totalRevenue = sessions?.reduce((acc, s) => acc + s.cost, 0) || 0;

  return (
    <div className="flex flex-col">
      <Header
        title="Charging Sessions"
        description="Monitor active and historical charging sessions"
      />

      <div className="flex-1 space-y-6 p-6">
        {/* Stats */}
        <div className="grid gap-4 md:grid-cols-4">
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-4">
                <div className="rounded-full bg-blue-500/10 p-3">
                  <Zap className="h-6 w-6 text-blue-600" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Active Sessions</p>
                  <p className="text-2xl font-bold">{activeSessions}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-4">
                <div className="rounded-full bg-green-500/10 p-3">
                  <Battery className="h-6 w-6 text-green-600" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Energy Delivered</p>
                  <p className="text-2xl font-bold">{formatEnergy(totalEnergy)}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-4">
                <div className="rounded-full bg-yellow-500/10 p-3">
                  <DollarSign className="h-6 w-6 text-yellow-600" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Total Revenue</p>
                  <p className="text-2xl font-bold">{formatCurrency(totalRevenue)}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-4">
                <div className="rounded-full bg-purple-500/10 p-3">
                  <Clock className="h-6 w-6 text-purple-600" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Total Sessions</p>
                  <p className="text-2xl font-bold">{sessions?.length || 0}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Filters */}
        <div className="flex items-center gap-4">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="search"
              placeholder="Search sessions..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="h-10 w-full rounded-md border bg-background pl-9 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant={statusFilter === "all" ? "default" : "outline"}
              size="sm"
              onClick={() => setStatusFilter("all")}
            >
              All
            </Button>
            <Button
              variant={statusFilter === "Charging" ? "default" : "outline"}
              size="sm"
              onClick={() => setStatusFilter("Charging")}
            >
              Active
            </Button>
            <Button
              variant={statusFilter === "Completed" ? "default" : "outline"}
              size="sm"
              onClick={() => setStatusFilter("Completed")}
            >
              Completed
            </Button>
          </div>
        </div>

        {/* Sessions Table */}
        <Card>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="px-4 py-3 text-left text-sm font-medium">Station</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">User</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Vehicle</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Status</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Start Time</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Duration</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Energy</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Cost</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredSessions?.map((session) => (
                    <tr key={session.id} className="border-b hover:bg-muted/50">
                      <td className="px-4 py-3">
                        <div>
                          <p className="font-medium">{session.stationName}</p>
                          <p className="text-xs text-muted-foreground">
                            Connector #{session.connectorNumber}
                          </p>
                        </div>
                      </td>
                      <td className="px-4 py-3">{session.userName}</td>
                      <td className="px-4 py-3">{session.vehiclePlate}</td>
                      <td className="px-4 py-3">{getStatusBadge(session.status)}</td>
                      <td className="px-4 py-3 text-sm">
                        {formatDateTime(session.startTime)}
                      </td>
                      <td className="px-4 py-3">{formatDuration(session.durationMinutes)}</td>
                      <td className="px-4 py-3">{formatEnergy(session.energyDelivered)}</td>
                      <td className="px-4 py-3 font-medium">
                        {formatCurrency(session.cost)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
