"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Search, AlertTriangle, CheckCircle, Clock, Wrench } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { StatusBadge, getStatusConfig } from "@/components/ui/status-badge";
import { SkeletonCard } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { formatDateTime } from "@/lib/utils";
import { faultsApi } from "@/lib/api";

export default function FaultsPage() {
  const router = useRouter();
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
      <div className="sticky top-0 z-10 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 px-6 py-4">
        <PageHeader
          title="Fault Management"
          description="Monitor and manage station faults"
        />
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* Stats */}
        {isLoading ? (
          <div className="grid gap-4 md:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <SkeletonCard key={i} />
            ))}
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-4">
            <StatCard
              label="Open Faults"
              value={newFaults}
              icon={AlertTriangle}
              iconColor="bg-red-50 text-red-600"
            />
            <StatCard
              label="Investigating"
              value={inProgressFaults}
              icon={Clock}
              iconColor="bg-amber-50 text-amber-600"
            />
            <StatCard
              label="Critical"
              value={criticalFaults}
              icon={AlertTriangle}
              iconColor="bg-red-50 text-red-600"
            />
            <StatCard
              label="Total Faults"
              value={faultsData?.totalCount || faults.length}
              icon={CheckCircle}
              iconColor="bg-primary/10 text-primary"
            />
          </div>
        )}

        {/* Filters */}
        <div className="flex items-center gap-4">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="search"
              placeholder="Search faults..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="h-10 w-full rounded-md border bg-background pl-9 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
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
          <div className="space-y-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <SkeletonCard key={i} />
            ))}
          </div>
        ) : filteredFaults.length === 0 ? (
          <EmptyState
            icon={CheckCircle}
            title="No faults found"
            description="All systems are running smoothly"
          />
        ) : (
          <div className="space-y-4">
            {filteredFaults.map((fault: { id: string; errorCode: string; status: number | string; priority?: number; errorInfo?: string; stationName?: string; connectorNumber?: number; detectedAt?: string; resolvedAt?: string | null }) => {
              const severityConfig = getStatusConfig("faultSeverity", fault.priority ?? 4);
              return (
                <Card
                  key={fault.id}
                  className="cursor-pointer hover:bg-muted/50 transition-colors border-l-4"
                  style={{ borderLeftColor: severityConfig?.dotColor }}
                  onClick={() => router.push(`/faults/${fault.id}`)}
                >
                  <CardContent className="p-6">
                    <div className="flex items-start justify-between">
                      <div className="space-y-2">
                        <div className="flex items-center gap-3">
                          <h3 className="font-semibold">{fault.errorCode}</h3>
                          <StatusBadge type="faultStatus" value={typeof fault.status === "number" ? fault.status : 0} />
                          <StatusBadge type="faultSeverity" value={fault.priority ?? 4} />
                        </div>
                        <p className="text-sm text-muted-foreground">{fault.errorInfo}</p>
                        <div className="flex items-center gap-4 text-sm text-muted-foreground">
                          <span>{fault.stationName || "\u2014"}</span>
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
                            onClick={(e) => { e.stopPropagation(); updateStatusMutation.mutate({ id: fault.id, status: 1 }); }}
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
                            onClick={(e) => { e.stopPropagation(); updateStatusMutation.mutate({ id: fault.id, status: 2 }); }}
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
              );
            })}
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
      </div>
    </div>
  );
}
