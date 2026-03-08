"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { Dialog, DialogHeader, DialogContent } from "@/components/ui/dialog";
import { SkeletonTable } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { api } from "@/lib/api";
import {
  FileText,
  Search,
  Download,
  Calendar,
  User,
  ChevronLeft,
  ChevronRight,
  Clock,
  Eye,
} from "lucide-react";

interface AuditLog {
  id: string;
  userId: string;
  userName: string;
  httpMethod: string;
  url: string;
  httpStatusCode: number;
  executionDuration: number;
  clientIpAddress: string;
  browserInfo: string;
  entityChanges?: EntityChange[];
  executionTime: string;
}

interface EntityChange {
  id: string;
  entityTypeFullName: string;
  changeType: "Created" | "Updated" | "Deleted";
  entityId: string;
  propertyChanges: PropertyChange[];
}

interface PropertyChange {
  propertyName: string;
  originalValue?: string;
  newValue?: string;
}

const METHOD_VARIANT: Record<string, "default" | "success" | "warning" | "destructive" | "secondary"> = {
  GET: "default",
  POST: "success",
  PUT: "warning",
  DELETE: "destructive",
};

function getHttpStatusVariant(status: number): "success" | "warning" | "destructive" | "secondary" {
  if (status >= 200 && status < 300) return "success";
  if (status >= 400 && status < 500) return "warning";
  if (status >= 500) return "destructive";
  return "secondary";
}

const CHANGE_TYPE_VARIANT: Record<string, "success" | "destructive" | "warning"> = {
  Created: "success",
  Deleted: "destructive",
  Updated: "warning",
};

export default function AuditLogsPage() {
  const [searchQuery, setSearchQuery] = useState("");
  const [httpMethod, setHttpMethod] = useState("all");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [cursor, setCursor] = useState<string | null>(null);
  const [cursorStack, setCursorStack] = useState<(string | null)[]>([]);
  const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null);
  const pageSize = 20;

  const resetPagination = () => { setCursor(null); setCursorStack([]); };

  // Fetch audit logs
  const { data: logsData, isLoading } = useQuery({
    queryKey: ["audit-logs", searchQuery, httpMethod, dateFrom, dateTo, cursor],
    queryFn: async () => {
      const params: Record<string, string | number> = {
        maxResultCount: pageSize,
      };
      if (searchQuery) params.url = searchQuery;
      if (httpMethod !== "all") params.httpMethod = httpMethod;
      if (dateFrom) params.startTime = dateFrom;
      if (dateTo) params.endTime = dateTo;
      if (cursor) params.cursor = cursor;

      const res = await api.get("/audit-logs", { params });
      return res.data;
    },
  });

  // Export audit logs
  const handleExport = async () => {
    try {
      const params: Record<string, string> = {};
      if (dateFrom) params.startTime = dateFrom;
      if (dateTo) params.endTime = dateTo;

      const res = await api.get("/audit-logs/export", {
        params,
        responseType: "blob",
      });

      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement("a");
      link.href = url;
      link.download = `audit-logs-${new Date().toISOString().split("T")[0]}.csv`;
      link.click();
    } catch (error) {
      console.error("Export failed:", error);
    }
  };

  const logs: AuditLog[] = logsData?.items || [];
  const totalCount = logsData?.totalCount || 0;

  const formatDuration = (ms: number) => {
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(2)}s`;
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString("vi-VN");
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title="Audit Logs" description="Track all system activities and changes">
          <Button variant="outline" onClick={handleExport}>
            <Download className="mr-2 h-4 w-4" />
            Export CSV
          </Button>
        </PageHeader>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-wrap gap-4">
            <div className="flex-1 min-w-[200px]">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Search by URL..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full rounded-md border pl-10 pr-3 py-2"
                />
              </div>
            </div>
            <select
              value={httpMethod}
              onChange={(e) => setHttpMethod(e.target.value)}
              className="rounded-md border px-3 py-2"
            >
              <option value="all">All Methods</option>
              <option value="GET">GET</option>
              <option value="POST">POST</option>
              <option value="PUT">PUT</option>
              <option value="DELETE">DELETE</option>
            </select>
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-muted-foreground" />
              <input
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="rounded-md border px-3 py-2"
              />
              <span>to</span>
              <input
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="rounded-md border px-3 py-2"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Logs Table */}
      {isLoading ? (
        <SkeletonTable rows={10} cols={8} />
      ) : logs.length === 0 ? (
        <Card>
          <CardContent className="p-0">
            <EmptyState
              icon={FileText}
              title="No audit logs found"
              description="Try adjusting your filters or check back later."
            />
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b bg-muted/50">
                  <tr>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Time
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      User
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Method
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      URL
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Status
                    </th>
                    <th className="px-4 py-3 text-right text-sm font-medium">
                      Duration
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      IP
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {logs.map((log) => (
                    <tr key={log.id} className="border-b hover:bg-muted/50">
                      <td className="px-4 py-3 text-sm">
                        <div className="flex items-center gap-2">
                          <Clock className="h-4 w-4 text-muted-foreground" />
                          {formatDate(log.executionTime)}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <User className="h-4 w-4 text-muted-foreground" />
                          <span>{log.userName || "System"}</span>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant={METHOD_VARIANT[log.httpMethod] ?? "secondary"}>
                          {log.httpMethod}
                        </Badge>
                      </td>
                      <td className="px-4 py-3">
                        <span className="font-mono text-sm truncate max-w-[300px] block">
                          {log.url}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant={getHttpStatusVariant(log.httpStatusCode)}>
                          {log.httpStatusCode}
                        </Badge>
                      </td>
                      <td className="px-4 py-3 text-sm text-right tabular-nums">
                        {formatDuration(log.executionDuration)}
                      </td>
                      <td className="px-4 py-3 text-sm font-mono">
                        {log.clientIpAddress}
                      </td>
                      <td className="px-4 py-3">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setSelectedLog(log)}
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {(totalCount > pageSize || cursorStack.length > 0) && (
              <div className="flex items-center justify-between border-t px-4 py-3">
                <div className="text-sm text-muted-foreground">
                  {totalCount} total audit logs
                </div>
                <div className="flex gap-2">
                  {cursorStack.length > 0 && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        const prev = [...cursorStack];
                        const prevCursor = prev.pop()!;
                        setCursorStack(prev);
                        setCursor(prevCursor);
                      }}
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </Button>
                  )}
                  {logs.length === pageSize && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        const lastId = logs[logs.length - 1]?.id;
                        if (lastId) {
                          setCursorStack([...cursorStack, cursor]);
                          setCursor(lastId);
                        }
                      }}
                    >
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Detail Dialog */}
      <Dialog open={!!selectedLog} onClose={() => setSelectedLog(null)} size="xl">
        <DialogHeader onClose={() => setSelectedLog(null)}>
          Audit Log Details
        </DialogHeader>
        <DialogContent className="max-h-[60vh] overflow-y-auto space-y-4">
          {selectedLog && (
            <>
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <p className="text-sm text-muted-foreground">Time</p>
                  <p className="font-medium">
                    {formatDate(selectedLog.executionTime)}
                  </p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">User</p>
                  <p className="font-medium">{selectedLog.userName || "System"}</p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Method</p>
                  <Badge variant={METHOD_VARIANT[selectedLog.httpMethod] ?? "secondary"}>
                    {selectedLog.httpMethod}
                  </Badge>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Status</p>
                  <Badge variant={getHttpStatusVariant(selectedLog.httpStatusCode)}>
                    {selectedLog.httpStatusCode}
                  </Badge>
                </div>
                <div className="col-span-2">
                  <p className="text-sm text-muted-foreground">URL</p>
                  <p className="font-mono text-sm break-all">{selectedLog.url}</p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Duration</p>
                  <p className="font-medium tabular-nums">
                    {formatDuration(selectedLog.executionDuration)}
                  </p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">IP Address</p>
                  <p className="font-mono">{selectedLog.clientIpAddress}</p>
                </div>
                <div className="col-span-2">
                  <p className="text-sm text-muted-foreground">Browser</p>
                  <p className="text-sm truncate">{selectedLog.browserInfo}</p>
                </div>
              </div>

              {/* Entity Changes */}
              {selectedLog.entityChanges && selectedLog.entityChanges.length > 0 && (
                <div className="border-t pt-4">
                  <h4 className="font-medium mb-2">Entity Changes</h4>
                  <div className="space-y-3">
                    {selectedLog.entityChanges.map((change) => (
                      <div
                        key={change.id}
                        className="rounded-lg border p-3 bg-muted/30"
                      >
                        <div className="flex items-center gap-2 mb-2">
                          <Badge variant={CHANGE_TYPE_VARIANT[change.changeType] ?? "secondary"}>
                            {change.changeType}
                          </Badge>
                          <span className="font-mono text-sm">
                            {change.entityTypeFullName.split(".").pop()}
                          </span>
                        </div>
                        {change.propertyChanges &&
                          change.propertyChanges.length > 0 && (
                            <div className="space-y-1">
                              {change.propertyChanges.map((prop, idx) => (
                                <div key={idx} className="text-sm">
                                  <span className="font-medium">
                                    {prop.propertyName}:
                                  </span>{" "}
                                  {prop.originalValue && (
                                    <span className="text-destructive line-through">
                                      {prop.originalValue}
                                    </span>
                                  )}{" "}
                                  {prop.newValue && (
                                    <span className="text-green-600 dark:text-green-400">
                                      {prop.newValue}
                                    </span>
                                  )}
                                </div>
                              ))}
                            </div>
                          )}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
