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

const FaultStatusLabels: Record<number, string> = {
  0: "Open",
  1: "Investigating",
  2: "Resolved",
  3: "Closed",
};

function getStatusBadge(status: number | string) {
  const label = typeof status === "number" ? (FaultStatusLabels[status] || "Unknown") : status;
  switch (label) {
    case "Open":
      return <Badge variant="destructive">Open</Badge>;
    case "Investigating":
      return <Badge variant="warning">Investigating</Badge>;
    case "Resolved":
      return <Badge variant="success">Resolved</Badge>;
    case "Closed":
      return <Badge variant="secondary">Closed</Badge>;
    default:
      return <Badge variant="secondary">{label}</Badge>;
  }
}

function getSeverityBadge(severity: number | string) {
  // Backend priority: 1=Critical, 2=High, 3=Medium, 4=Low
  const priorityMap: Record<number, string> = { 1: "Critical", 2: "High", 3: "Medium", 4: "Low" };
  const label = typeof severity === "number"
    ? (priorityMap[severity] || "Unknown")
    : severity;
  switch (label) {
    case "Critical":
      return <Badge variant="destructive">Critical</Badge>;
    case "High":
      return <Badge className="bg-orange-500 text-white">High</Badge>;
    case "Medium":
      return <Badge variant="warning">Medium</Badge>;
    case "Low":
      return <Badge variant="secondary">Low</Badge>;
    default:
      return <Badge variant="secondary">{label}</Badge>;
  }
}

export default function FaultsPage() {
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [cursor, setCursor] = useState<string | null>(null);
  const [cursorStack, setCursorStack] = useState<(string | null)[]>([]);
  const pageSize = 20;
  const queryClient = useQueryClient();

  const resetPagination = () => { setCursor(null); setCursorStack([]); };

  const { data: faultsData, isLoading } = useQuery({
    queryKey: ["faults", statusFilter, cursor],
    queryFn: async () => {
      const params: Record<string, unknown> = { maxResultCount: pageSize };
      if (statusFilter !== "all") params.status = Number(statusFilter);
      if (cursor) params.cursor = cursor;
      const { data } = await faultsApi.getAll(params as Parameters<typeof faultsApi.getAll>[0]);
      return data;
    },
  });

  const faults = faultsData?.items || [];

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: number }) =>
      faultsApi.updateStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["faults"] }),
  });

  const filteredFaults = faults.filter((fault: { stationName?: string; errorCode?: string }) => {
    if (!search) return true;
    const s = search.toLowerCase();
    return (
      (fault.stationName || "").toLowerCase().includes(s) ||
      (fault.errorCode || "").toLowerCase().includes(s)
    );
  });

  const newFaults = faults.filter((f: { status: number | string }) => f.status === 0 || f.status === "Open").length;
  const inProgressFaults = faults.filter((f: { status: number | string }) => f.status === 1 || f.status === "Investigating").length;
  const criticalFaults = faults.filter((f: { priority?: number }) => f.priority === 1).length;

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
                  <p className="text-sm text-muted-foreground">Open Faults</p>
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
                  <p className="text-sm text-muted-foreground">Investigating</p>
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
                  <p className="text-2xl font-bold">{faultsData?.totalCount || faults.length}</p>
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
            <Button variant={statusFilter === "all" ? "default" : "outline"} size="sm" onClick={() => { setStatusFilter("all"); resetPagination(); }}>All</Button>
            <Button variant={statusFilter === "0" ? "default" : "outline"} size="sm" onClick={() => { setStatusFilter("0"); resetPagination(); }}>Open</Button>
            <Button variant={statusFilter === "1" ? "default" : "outline"} size="sm" onClick={() => { setStatusFilter("1"); resetPagination(); }}>Investigating</Button>
            <Button variant={statusFilter === "2" ? "default" : "outline"} size="sm" onClick={() => { setStatusFilter("2"); resetPagination(); }}>Resolved</Button>
            <Button variant={statusFilter === "3" ? "default" : "outline"} size="sm" onClick={() => { setStatusFilter("3"); resetPagination(); }}>Closed</Button>
          </div>
        </div>

        {/* Faults List */}
        {isLoading ? (
          <div className="text-center py-8">Loading...</div>
        ) : (
          <div className="space-y-4">
            {filteredFaults.map((fault: { id: string; errorCode: string; status: number | string; priority?: number; errorInfo?: string; stationName?: string; connectorNumber?: number; detectedAt?: string; resolvedAt?: string | null }) => (
              <Card key={fault.id}>
                <CardContent className="p-6">
                  <div className="flex items-start justify-between">
                    <div className="space-y-2">
                      <div className="flex items-center gap-3">
                        <h3 className="font-semibold">{fault.errorCode}</h3>
                        {getStatusBadge(fault.status)}
                        {getSeverityBadge(fault.priority ?? 4)}
                      </div>
                      <p className="text-sm text-muted-foreground">{fault.errorInfo}</p>
                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <span>{fault.stationName || "—"}</span>
                        {fault.connectorNumber != null && <span>Connector #{fault.connectorNumber}</span>}
                        <span>Detected: {formatDateTime(fault.detectedAt || "")}</span>
                        {fault.resolvedAt && <span>Resolved: {formatDateTime(fault.resolvedAt)}</span>}
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      {(fault.status === 0 || fault.status === "Open") && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => updateStatusMutation.mutate({ id: fault.id, status: 1 })}
                          disabled={updateStatusMutation.isPending}
                        >
                          <Wrench className="mr-2 h-4 w-4" />
                          Investigate
                        </Button>
                      )}
                      {(fault.status === 1 || fault.status === "Investigating") && (
                        <Button
                          variant="default"
                          size="sm"
                          onClick={() => updateStatusMutation.mutate({ id: fault.id, status: 2 })}
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
        )}

        {/* Pagination */}
        {!isLoading && ((faultsData?.totalCount ?? 0) > pageSize || cursorStack.length > 0) && (
          <div className="flex items-center justify-between">
            <p className="text-sm text-muted-foreground">
              {faultsData?.totalCount ?? 0} total faults
            </p>
            <div className="flex items-center gap-2">
              {cursorStack.length > 0 && (
                <Button variant="outline" size="sm" onClick={() => {
                  const prev = [...cursorStack];
                  const prevCursor = prev.pop()!;
                  setCursorStack(prev);
                  setCursor(prevCursor);
                }}>
                  Previous
                </Button>
              )}
              {faults.length === pageSize && (
                <Button variant="outline" size="sm" onClick={() => {
                  const lastId = faults[faults.length - 1]?.id;
                  if (lastId) {
                    setCursorStack([...cursorStack, cursor]);
                    setCursor(lastId);
                  }
                }}>
                  Next
                </Button>
              )}
            </div>
          </div>
        )}

        {!isLoading && filteredFaults.length === 0 && (
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
