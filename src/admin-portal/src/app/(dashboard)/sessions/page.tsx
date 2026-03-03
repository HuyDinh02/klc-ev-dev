"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search, Zap, Clock, Battery, DollarSign } from "lucide-react";
import { Header } from "@/components/layout/header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { sessionsApi } from "@/lib/api";
import { formatCurrency, formatDateTime, formatEnergy, formatDuration } from "@/lib/utils";

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
  const label = typeof status === "number" ? (SessionStatusLabels[status] || "Unknown") : status;
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

export default function SessionsPage() {
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 20;

  const { data: sessionsData, isLoading } = useQuery({
    queryKey: ["sessions", statusFilter, currentPage],
    queryFn: async () => {
      const params: Record<string, unknown> = {
        skipCount: (currentPage - 1) * pageSize,
        maxResultCount: pageSize,
      };
      if (statusFilter !== "all") params.status = statusFilter;
      const { data } = await sessionsApi.getAll(params as { skip?: number; maxResultCount?: number; stationId?: string });
      return data;
    },
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
                  <p className="text-2xl font-bold">{sessionsData?.totalCount || sessions.length}</p>
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
              variant={statusFilter === "2" ? "default" : "outline"}
              size="sm"
              onClick={() => setStatusFilter("2")}
            >
              Active
            </Button>
            <Button
              variant={statusFilter === "5" ? "default" : "outline"}
              size="sm"
              onClick={() => setStatusFilter("5")}
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
                    <th className="px-4 py-3 text-left text-sm font-medium">Status</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Start Time</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Duration</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Energy</th>
                    <th className="px-4 py-3 text-left text-sm font-medium">Cost</th>
                  </tr>
                </thead>
                <tbody>
                  {isLoading ? (
                    <tr>
                      <td colSpan={7} className="px-4 py-8 text-center">Loading...</td>
                    </tr>
                  ) : filteredSessions.length > 0 ? (
                    filteredSessions.map((session: { id: string; stationName?: string; connectorNumber?: number; userName?: string; status: number | string; startTime: string; endTime?: string | null; totalEnergyKwh?: number; energyDeliveredKwh?: number; totalCost?: number; cost?: number }) => (
                      <tr key={session.id} className="border-b hover:bg-muted/50">
                        <td className="px-4 py-3">
                          <div>
                            <p className="font-medium">{session.stationName || "—"}</p>
                            {session.connectorNumber && (
                              <p className="text-xs text-muted-foreground">
                                Connector #{session.connectorNumber}
                              </p>
                            )}
                          </div>
                        </td>
                        <td className="px-4 py-3">{session.userName || "—"}</td>
                        <td className="px-4 py-3">{getStatusBadge(session.status)}</td>
                        <td className="px-4 py-3 text-sm">
                          {formatDateTime(session.startTime)}
                        </td>
                        <td className="px-4 py-3">
                          {formatDuration(computeDuration(session.startTime, session.endTime))}
                        </td>
                        <td className="px-4 py-3">
                          {formatEnergy(session.totalEnergyKwh || session.energyDeliveredKwh || 0)}
                        </td>
                        <td className="px-4 py-3 font-medium">
                          {formatCurrency(session.totalCost || session.cost || 0)}
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">
                        No sessions found
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
