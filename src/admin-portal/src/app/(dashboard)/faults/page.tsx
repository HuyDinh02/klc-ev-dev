"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Search, AlertTriangle, CheckCircle, Clock, Wrench } from "lucide-react";
import { Header } from "@/components/layout/header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { formatDateTime } from "@/lib/utils";
import { faultsApi } from "@/lib/api";

// Mock data for development
const mockFaults = [
  {
    id: "1",
    stationName: "Station HCM-015",
    connectorNumber: 2,
    errorCode: "GroundFailure",
    description: "Ground fault detected on connector",
    status: "New",
    severity: "High",
    occurredAt: "2024-01-15T10:25:00",
    resolvedAt: null,
  },
  {
    id: "2",
    stationName: "Station HN-003",
    connectorNumber: 1,
    errorCode: "OverVoltage",
    description: "Voltage exceeds safe operating range",
    status: "InProgress",
    severity: "Critical",
    occurredAt: "2024-01-15T09:15:00",
    resolvedAt: null,
  },
  {
    id: "3",
    stationName: "Station DN-007",
    connectorNumber: 3,
    errorCode: "ConnectorLockFailure",
    description: "Unable to lock connector",
    status: "New",
    severity: "Medium",
    occurredAt: "2024-01-15T08:45:00",
    resolvedAt: null,
  },
  {
    id: "4",
    stationName: "Station HCM-002",
    connectorNumber: 1,
    errorCode: "OverTemperature",
    description: "Temperature sensor reading above threshold",
    status: "Resolved",
    severity: "High",
    occurredAt: "2024-01-14T15:30:00",
    resolvedAt: "2024-01-14T17:00:00",
  },
];

function getStatusBadge(status: string) {
  switch (status) {
    case "New":
      return <Badge variant="destructive">New</Badge>;
    case "InProgress":
      return <Badge variant="warning">In Progress</Badge>;
    case "Resolved":
      return <Badge variant="success">Resolved</Badge>;
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
}

function getSeverityBadge(severity: string) {
  switch (severity) {
    case "Critical":
      return <Badge variant="destructive">Critical</Badge>;
    case "High":
      return <Badge className="bg-orange-500 text-white">High</Badge>;
    case "Medium":
      return <Badge variant="warning">Medium</Badge>;
    case "Low":
      return <Badge variant="secondary">Low</Badge>;
    default:
      return <Badge variant="secondary">{severity}</Badge>;
  }
}

export default function FaultsPage() {
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const queryClient = useQueryClient();

  const { data: faults, isLoading } = useQuery({
    queryKey: ["faults", statusFilter],
    queryFn: async () => {
      // In production: const { data } = await faultsApi.getAll({ status: statusFilter });
      // return data.items;
      return mockFaults;
    },
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      faultsApi.updateStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["faults"] }),
  });

  const filteredFaults = faults?.filter((fault) => {
    const matchesSearch =
      fault.stationName.toLowerCase().includes(search.toLowerCase()) ||
      fault.errorCode.toLowerCase().includes(search.toLowerCase());
    const matchesStatus = statusFilter === "all" || fault.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const newFaults = faults?.filter((f) => f.status === "New").length || 0;
  const inProgressFaults = faults?.filter((f) => f.status === "InProgress").length || 0;
  const criticalFaults = faults?.filter((f) => f.severity === "Critical").length || 0;

  return (
    <div className="flex flex-col">
      <Header
        title="Fault Management"
        description="Monitor and manage station faults"
      />

      <div className="flex-1 space-y-6 p-6">
        {/* Stats */}
        <div className="grid gap-4 md:grid-cols-4">
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-4">
                <div className="rounded-full bg-red-500/10 p-3">
                  <AlertTriangle className="h-6 w-6 text-red-600" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">New Faults</p>
                  <p className="text-2xl font-bold">{newFaults}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-4">
                <div className="rounded-full bg-yellow-500/10 p-3">
                  <Clock className="h-6 w-6 text-yellow-600" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">In Progress</p>
                  <p className="text-2xl font-bold">{inProgressFaults}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-4">
                <div className="rounded-full bg-red-500/10 p-3">
                  <AlertTriangle className="h-6 w-6 text-red-600" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Critical</p>
                  <p className="text-2xl font-bold">{criticalFaults}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-4">
                <div className="rounded-full bg-green-500/10 p-3">
                  <CheckCircle className="h-6 w-6 text-green-600" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Total Faults</p>
                  <p className="text-2xl font-bold">{faults?.length || 0}</p>
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
              placeholder="Search faults..."
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
              variant={statusFilter === "New" ? "default" : "outline"}
              size="sm"
              onClick={() => setStatusFilter("New")}
            >
              New
            </Button>
            <Button
              variant={statusFilter === "InProgress" ? "default" : "outline"}
              size="sm"
              onClick={() => setStatusFilter("InProgress")}
            >
              In Progress
            </Button>
            <Button
              variant={statusFilter === "Resolved" ? "default" : "outline"}
              size="sm"
              onClick={() => setStatusFilter("Resolved")}
            >
              Resolved
            </Button>
          </div>
        </div>

        {/* Faults List */}
        <div className="space-y-4">
          {filteredFaults?.map((fault) => (
            <Card key={fault.id}>
              <CardContent className="p-6">
                <div className="flex items-start justify-between">
                  <div className="space-y-2">
                    <div className="flex items-center gap-3">
                      <h3 className="font-semibold">{fault.errorCode}</h3>
                      {getStatusBadge(fault.status)}
                      {getSeverityBadge(fault.severity)}
                    </div>
                    <p className="text-sm text-muted-foreground">{fault.description}</p>
                    <div className="flex items-center gap-4 text-sm text-muted-foreground">
                      <span>{fault.stationName}</span>
                      <span>Connector #{fault.connectorNumber}</span>
                      <span>Occurred: {formatDateTime(fault.occurredAt)}</span>
                      {fault.resolvedAt && (
                        <span>Resolved: {formatDateTime(fault.resolvedAt)}</span>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {fault.status === "New" && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() =>
                          updateStatusMutation.mutate({ id: fault.id, status: "InProgress" })
                        }
                        disabled={updateStatusMutation.isPending}
                      >
                        <Wrench className="mr-2 h-4 w-4" />
                        Start Work
                      </Button>
                    )}
                    {fault.status === "InProgress" && (
                      <Button
                        variant="default"
                        size="sm"
                        onClick={() =>
                          updateStatusMutation.mutate({ id: fault.id, status: "Resolved" })
                        }
                        disabled={updateStatusMutation.isPending}
                      >
                        <CheckCircle className="mr-2 h-4 w-4" />
                        Mark Resolved
                      </Button>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {filteredFaults?.length === 0 && (
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <CheckCircle className="h-12 w-12 text-green-500" />
            <h3 className="mt-4 text-lg font-semibold">No faults found</h3>
            <p className="text-muted-foreground">All systems are running smoothly</p>
          </div>
        )}
      </div>
    </div>
  );
}
