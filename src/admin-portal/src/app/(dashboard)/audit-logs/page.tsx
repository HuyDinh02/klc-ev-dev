"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTableQuery } from "@/hooks/use-table-query";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { Dialog, DialogHeader, DialogContent } from "@/components/ui/dialog";
import { SkeletonTable } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { api } from "@/lib/api";
import { formatDateTime } from "@/lib/utils";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
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
  AlertTriangle,
  Database,
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
  hasException: boolean;
  exceptionMessage?: string;
  entityChanges?: EntityChange[];
  entityChangeCount: number;
  executionTime: string;
  comments?: string;
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

// Extract short entity name from full namespace
function shortEntityName(fullName: string): string {
  const parts = fullName.split(".");
  return parts[parts.length - 1];
}

export default function AuditLogsPage() {
  const hasAccess = useRequirePermission("KLC.AuditLogs");
  const { t } = useTranslation();
  const [searchQuery, setSearchQuery] = useState("");
  const [httpMethod, setHttpMethod] = useState("all");
  const [userName, setUserName] = useState("");
  const [hasException, setHasException] = useState<string>("all");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null);

  // Fetch audit logs
  const {
    items: logs,
    totalCount,
    isLoading,
    pageSize,
    goNextPage,
    goPrevPage,
    hasNextPage,
    hasPrevPage,
    resetPage,
  } = useTableQuery<AuditLog>({
    queryKey: "audit-logs",
    fetchFn: async (params) => {
      if (searchQuery) params.url = searchQuery;
      if (httpMethod !== "all") params.httpMethod = httpMethod;
      if (userName) params.userName = userName;
      if (hasException === "yes") params.hasException = true;
      if (hasException === "no") params.hasException = false;
      if (dateFrom) params.startTime = dateFrom;
      if (dateTo) params.endTime = dateTo;

      const res = await api.get("/audit-logs", { params });
      return res.data;
    },
    extraQueryKeys: [searchQuery, httpMethod, userName, hasException, dateFrom, dateTo],
  });

  // Fetch detail with entity changes when log is selected
  const { data: detailData } = useQuery({
    queryKey: ["audit-log-detail", selectedLog?.id],
    queryFn: async () => {
      if (!selectedLog) return null;
      const res = await api.get(`/audit-logs/${selectedLog.id}`);
      return res.data as AuditLog;
    },
    enabled: !!selectedLog,
  });

  // Export audit logs
  const handleExport = async () => {
    try {
      const params: Record<string, string> = {};
      if (dateFrom) params.startTime = dateFrom;
      if (dateTo) params.endTime = dateTo;
      if (searchQuery) params.url = searchQuery;
      if (httpMethod !== "all") params.httpMethod = httpMethod;
      if (userName) params.userName = userName;

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

  const detail = detailData || selectedLog;

  const formatDuration = (ms: number) => {
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(2)}s`;
  };

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      {/* Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title={t("auditLogs.title")} description={t("auditLogs.description")}>
          <Button variant="outline" onClick={handleExport}>
            <Download className="mr-2 h-4 w-4" aria-hidden="true" />
            {t("auditLogs.exportCsv")}
          </Button>
        </PageHeader>
      </div>

      <div className="flex-1 space-y-6 p-6">
      {/* Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-wrap gap-4">
            <div className="flex-1 min-w-[200px]">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
                <input
                  type="text"
                  placeholder={t("auditLogs.searchByUrl")}
                  value={searchQuery}
                  onChange={(e) => { setSearchQuery(e.target.value); resetPage(); }}
                  className="w-full rounded-md border pl-10 pr-3 py-2"
                  aria-label={t("auditLogs.searchByUrl")}
                />
              </div>
            </div>
            <div className="min-w-[140px]">
              <div className="relative">
                <User className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
                <input
                  type="text"
                  placeholder={t("auditLogs.searchByUser")}
                  value={userName}
                  onChange={(e) => { setUserName(e.target.value); resetPage(); }}
                  className="w-full rounded-md border pl-10 pr-3 py-2"
                  aria-label={t("auditLogs.searchByUser")}
                />
              </div>
            </div>
            <select
              value={httpMethod}
              onChange={(e) => { setHttpMethod(e.target.value); resetPage(); }}
              className="rounded-md border px-3 py-2"
              aria-label={t("auditLogs.filterByMethod")}
            >
              <option value="all">{t("auditLogs.allMethods")}</option>
              <option value="GET">GET</option>
              <option value="POST">POST</option>
              <option value="PUT">PUT</option>
              <option value="DELETE">DELETE</option>
            </select>
            <select
              value={hasException}
              onChange={(e) => { setHasException(e.target.value); resetPage(); }}
              className="rounded-md border px-3 py-2"
              aria-label={t("auditLogs.filterByStatus")}
            >
              <option value="all">{t("auditLogs.allStatuses")}</option>
              <option value="no">{t("auditLogs.successOnly")}</option>
              <option value="yes">{t("auditLogs.errorsOnly")}</option>
            </select>
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
              <input
                type="date"
                value={dateFrom}
                onChange={(e) => { setDateFrom(e.target.value); resetPage(); }}
                className="rounded-md border px-3 py-2"
                aria-label={t("auditLogs.dateFrom")}
              />
              <span>{t("auditLogs.to")}</span>
              <input
                type="date"
                value={dateTo}
                onChange={(e) => { setDateTo(e.target.value); resetPage(); }}
                className="rounded-md border px-3 py-2"
                aria-label={t("auditLogs.dateTo")}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Logs Table */}
      {isLoading && <SkeletonTable rows={10} cols={8} />}
      {!isLoading && logs.length === 0 && (
        <Card>
          <CardContent className="p-0">
            <EmptyState
              icon={FileText}
              title={t("auditLogs.noLogsFound")}
              description={t("auditLogs.noLogsDescription")}
            />
          </CardContent>
        </Card>
      )}
      {!isLoading && logs.length > 0 && (
        <Card>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b bg-muted/50">
                  <tr>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("auditLogs.time")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("auditLogs.user")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("auditLogs.method")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("auditLogs.url")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("auditLogs.statusCode")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-right text-sm font-medium">
                      {t("auditLogs.duration")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("auditLogs.changes")}
                    </th>
                    <th scope="col" className="px-4 py-3 text-left text-sm font-medium">
                      {t("common.actions")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {logs.map((log) => (
                    <tr
                      key={log.id}
                      className={`border-b hover:bg-muted/50 ${log.hasException ? "bg-destructive/5" : ""}`}
                    >
                      <td className="px-4 py-3 text-sm">
                        <div className="flex items-center gap-2">
                          <Clock className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
                          {formatDateTime(log.executionTime)}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <User className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
                          <span>{log.userName || t("auditLogs.system")}</span>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant={METHOD_VARIANT[log.httpMethod] ?? "secondary"}>
                          {log.httpMethod}
                        </Badge>
                      </td>
                      <td className="px-4 py-3">
                        <span className="font-mono text-sm truncate max-w-[300px] block" title={log.url}>
                          {log.url}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-1">
                          <Badge variant={getHttpStatusVariant(log.httpStatusCode)}>
                            {log.httpStatusCode}
                          </Badge>
                          {log.hasException && (
                            <AlertTriangle className="h-4 w-4 text-destructive" />
                          )}
                        </div>
                      </td>
                      <td className="px-4 py-3 text-sm text-right tabular-nums">
                        {formatDuration(log.executionDuration)}
                      </td>
                      <td className="px-4 py-3">
                        {log.entityChangeCount > 0 && (
                          <Badge variant="secondary" className="gap-1">
                            <Database className="h-3 w-3" />
                            {log.entityChangeCount}
                          </Badge>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setSelectedLog(log)}
                          aria-label={t("auditLogs.viewDetails")}
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
            {(totalCount > pageSize || hasPrevPage) && (
              <div className="flex items-center justify-between border-t px-4 py-3">
                <div className="text-sm text-muted-foreground">
                  {totalCount} {t("auditLogs.totalLogs")}
                </div>
                <div className="flex gap-2">
                  {hasPrevPage && (
                    <Button
                      variant="outline"
                      size="sm"
                      aria-label={t("common.previous")}
                      onClick={goPrevPage}
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </Button>
                  )}
                  {hasNextPage && (
                    <Button
                      variant="outline"
                      size="sm"
                      aria-label={t("common.next")}
                      onClick={goNextPage}
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
          {t("auditLogs.logDetails")}
        </DialogHeader>
        <DialogContent className="max-h-[70vh] overflow-y-auto space-y-4">
          {detail && (
            <>
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <p className="text-sm text-muted-foreground">{t("auditLogs.time")}</p>
                  <p className="font-medium">
                    {formatDateTime(detail.executionTime)}
                  </p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t("auditLogs.user")}</p>
                  <p className="font-medium">{detail.userName || t("auditLogs.system")}</p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t("auditLogs.method")}</p>
                  <Badge variant={METHOD_VARIANT[detail.httpMethod] ?? "secondary"}>
                    {detail.httpMethod}
                  </Badge>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t("auditLogs.statusCode")}</p>
                  <div className="flex items-center gap-2">
                    <Badge variant={getHttpStatusVariant(detail.httpStatusCode)}>
                      {detail.httpStatusCode}
                    </Badge>
                    {detail.hasException && (
                      <Badge variant="destructive">{t("auditLogs.error")}</Badge>
                    )}
                  </div>
                </div>
                <div className="col-span-2">
                  <p className="text-sm text-muted-foreground">{t("auditLogs.url")}</p>
                  <p className="font-mono text-sm break-all">{detail.url}</p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t("auditLogs.duration")}</p>
                  <p className="font-medium tabular-nums">
                    {formatDuration(detail.executionDuration)}
                  </p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t("auditLogs.ipAddress")}</p>
                  <p className="font-mono">{detail.clientIpAddress || "—"}</p>
                </div>
                {detail.browserInfo && (
                  <div className="col-span-2">
                    <p className="text-sm text-muted-foreground">{t("auditLogs.browser")}</p>
                    <p className="text-sm truncate">{detail.browserInfo}</p>
                  </div>
                )}
              </div>

              {/* Exception Message */}
              {detail.hasException && detail.exceptionMessage && (
                <div className="border-t pt-4">
                  <h4 className="font-medium mb-2 flex items-center gap-2 text-destructive">
                    <AlertTriangle className="h-4 w-4" />
                    {t("auditLogs.exception")}
                  </h4>
                  <pre className="rounded-lg border border-destructive/20 bg-destructive/5 p-3 text-sm font-mono whitespace-pre-wrap break-all">
                    {detail.exceptionMessage}
                  </pre>
                </div>
              )}

              {/* Entity Changes */}
              {detail.entityChanges && detail.entityChanges.length > 0 && (
                <div className="border-t pt-4">
                  <h4 className="font-medium mb-3 flex items-center gap-2">
                    <Database className="h-4 w-4" />
                    {t("auditLogs.entityChanges")} ({detail.entityChanges.length})
                  </h4>
                  <div className="space-y-3">
                    {detail.entityChanges.map((change) => (
                      <div
                        key={change.id}
                        className="rounded-lg border p-3 bg-muted/30"
                      >
                        <div className="flex items-center gap-2 mb-2">
                          <Badge variant={CHANGE_TYPE_VARIANT[change.changeType] ?? "secondary"}>
                            {change.changeType}
                          </Badge>
                          <span className="font-mono text-sm font-medium">
                            {shortEntityName(change.entityTypeFullName)}
                          </span>
                          <span className="text-xs text-muted-foreground font-mono">
                            #{change.entityId.slice(0, 8)}
                          </span>
                        </div>
                        {change.propertyChanges &&
                          change.propertyChanges.length > 0 && (
                            <div className="space-y-1 mt-2">
                              {change.propertyChanges.map((prop, idx) => (
                                <div key={idx} className="text-sm flex gap-2 items-baseline">
                                  <span className="font-medium text-muted-foreground min-w-[120px]">
                                    {prop.propertyName}
                                  </span>
                                  {prop.originalValue && (
                                    <span className="text-destructive line-through text-xs">
                                      {prop.originalValue.length > 100 ? prop.originalValue.slice(0, 100) + "..." : prop.originalValue}
                                    </span>
                                  )}
                                  {prop.originalValue && prop.newValue && (
                                    <span className="text-muted-foreground">→</span>
                                  )}
                                  {prop.newValue && (
                                    <span className="text-green-600 dark:text-green-400 text-xs">
                                      {prop.newValue.length > 100 ? prop.newValue.slice(0, 100) + "..." : prop.newValue}
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
    </div>
  );
}
