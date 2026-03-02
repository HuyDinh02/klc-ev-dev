"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { api } from "@/lib/api";
import {
  Bell,
  AlertTriangle,
  AlertCircle,
  Info,
  CheckCircle2,
  Clock,
  MapPin,
  Zap,
  X,
  Eye,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";

interface Alert {
  id: string;
  type: "Critical" | "Warning" | "Info";
  title: string;
  message: string;
  stationId?: string;
  stationName?: string;
  connectorId?: number;
  isAcknowledged: boolean;
  acknowledgedBy?: string;
  acknowledgedAt?: string;
  createdAt: string;
}

interface AlertStats {
  criticalCount: number;
  warningCount: number;
  infoCount: number;
  unacknowledgedCount: number;
}

export default function AlertsPage() {
  const queryClient = useQueryClient();
  const [typeFilter, setTypeFilter] = useState("all");
  const [acknowledgedFilter, setAcknowledgedFilter] = useState("all");
  const [currentPage, setCurrentPage] = useState(1);
  const [selectedAlert, setSelectedAlert] = useState<Alert | null>(null);
  const pageSize = 20;

  // Fetch alerts
  const { data: alertsData, isLoading } = useQuery({
    queryKey: ["alerts", typeFilter, acknowledgedFilter, currentPage],
    queryFn: async () => {
      const params: Record<string, string | number | boolean> = {
        skipCount: (currentPage - 1) * pageSize,
        maxResultCount: pageSize,
      };
      if (typeFilter !== "all") params.type = typeFilter;
      if (acknowledgedFilter !== "all") {
        params.isAcknowledged = acknowledgedFilter === "acknowledged";
      }

      const res = await api.get("/alerts", { params });
      return res.data;
    },
  });

  // Fetch stats
  const { data: stats } = useQuery<AlertStats>({
    queryKey: ["alert-stats"],
    queryFn: async () => {
      // Mock stats - would come from API in real implementation
      return {
        criticalCount: 2,
        warningCount: 5,
        infoCount: 12,
        unacknowledgedCount: 7,
      };
    },
  });

  // Acknowledge alert
  const acknowledgeMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.post(`/api/v1/alerts/${id}/acknowledge`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["alerts"] });
      queryClient.invalidateQueries({ queryKey: ["alert-stats"] });
    },
  });

  // Acknowledge all alerts
  const acknowledgeAllMutation = useMutation({
    mutationFn: async () => {
      await api.post("/alerts/acknowledge-all");
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["alerts"] });
      queryClient.invalidateQueries({ queryKey: ["alert-stats"] });
    },
  });

  const alerts: Alert[] = alertsData?.items || [];
  const totalCount = alertsData?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const getTypeIcon = (type: string) => {
    switch (type) {
      case "Critical":
        return <AlertCircle className="h-5 w-5 text-red-500" />;
      case "Warning":
        return <AlertTriangle className="h-5 w-5 text-yellow-500" />;
      case "Info":
        return <Info className="h-5 w-5 text-blue-500" />;
      default:
        return <Bell className="h-5 w-5" />;
    }
  };

  const getTypeColor = (type: string): "destructive" | "warning" | "default" | "secondary" => {
    switch (type) {
      case "Critical":
        return "destructive";
      case "Warning":
        return "warning";
      case "Info":
        return "default";
      default:
        return "secondary";
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString("vi-VN");
  };

  const getTimeAgo = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (days > 0) return `${days}d ago`;
    if (hours > 0) return `${hours}h ago`;
    if (minutes > 0) return `${minutes}m ago`;
    return "Just now";
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Alerts</h1>
          <p className="text-muted-foreground">
            System alerts and notifications
          </p>
        </div>
        <Button
          onClick={() => acknowledgeAllMutation.mutate()}
          disabled={
            acknowledgeAllMutation.isPending ||
            (stats?.unacknowledgedCount || 0) === 0
          }
        >
          <CheckCircle2 className="mr-2 h-4 w-4" />
          Acknowledge All
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card
          className={`cursor-pointer ${
            typeFilter === "Critical" ? "ring-2 ring-red-500" : ""
          }`}
          onClick={() =>
            setTypeFilter(typeFilter === "Critical" ? "all" : "Critical")
          }
        >
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Critical</CardTitle>
            <AlertCircle className="h-4 w-4 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">
              {stats?.criticalCount || 0}
            </div>
          </CardContent>
        </Card>

        <Card
          className={`cursor-pointer ${
            typeFilter === "Warning" ? "ring-2 ring-yellow-500" : ""
          }`}
          onClick={() =>
            setTypeFilter(typeFilter === "Warning" ? "all" : "Warning")
          }
        >
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Warning</CardTitle>
            <AlertTriangle className="h-4 w-4 text-yellow-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">
              {stats?.warningCount || 0}
            </div>
          </CardContent>
        </Card>

        <Card
          className={`cursor-pointer ${
            typeFilter === "Info" ? "ring-2 ring-blue-500" : ""
          }`}
          onClick={() => setTypeFilter(typeFilter === "Info" ? "all" : "Info")}
        >
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Info</CardTitle>
            <Info className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">
              {stats?.infoCount || 0}
            </div>
          </CardContent>
        </Card>

        <Card
          className={`cursor-pointer ${
            acknowledgedFilter === "unacknowledged"
              ? "ring-2 ring-primary"
              : ""
          }`}
          onClick={() =>
            setAcknowledgedFilter(
              acknowledgedFilter === "unacknowledged" ? "all" : "unacknowledged"
            )
          }
        >
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Unacknowledged</CardTitle>
            <Bell className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {stats?.unacknowledgedCount || 0}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex gap-4">
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              className="rounded-md border px-3 py-2"
            >
              <option value="all">All Types</option>
              <option value="Critical">Critical</option>
              <option value="Warning">Warning</option>
              <option value="Info">Info</option>
            </select>
            <select
              value={acknowledgedFilter}
              onChange={(e) => setAcknowledgedFilter(e.target.value)}
              className="rounded-md border px-3 py-2"
            >
              <option value="all">All Status</option>
              <option value="unacknowledged">Unacknowledged</option>
              <option value="acknowledged">Acknowledged</option>
            </select>
          </div>
        </CardContent>
      </Card>

      {/* Alerts List */}
      <div className="space-y-3">
        {isLoading ? (
          <div className="text-center py-8">Loading...</div>
        ) : alerts.length > 0 ? (
          alerts.map((alert) => (
            <Card
              key={alert.id}
              className={`${
                !alert.isAcknowledged
                  ? alert.type === "Critical"
                    ? "border-l-4 border-l-red-500"
                    : alert.type === "Warning"
                    ? "border-l-4 border-l-yellow-500"
                    : "border-l-4 border-l-blue-500"
                  : ""
              } ${!alert.isAcknowledged ? "bg-muted/30" : ""}`}
            >
              <CardContent className="py-4">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-4">
                    {getTypeIcon(alert.type)}
                    <div className="space-y-1">
                      <div className="flex items-center gap-2">
                        <h3 className="font-semibold">{alert.title}</h3>
                        <Badge variant={getTypeColor(alert.type)}>
                          {alert.type}
                        </Badge>
                        {alert.isAcknowledged && (
                          <Badge variant="secondary">Acknowledged</Badge>
                        )}
                      </div>
                      <p className="text-sm text-muted-foreground">
                        {alert.message}
                      </p>
                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          {getTimeAgo(alert.createdAt)}
                        </span>
                        {alert.stationName && (
                          <span className="flex items-center gap-1">
                            <MapPin className="h-3 w-3" />
                            {alert.stationName}
                          </span>
                        )}
                        {alert.connectorId && (
                          <span className="flex items-center gap-1">
                            <Zap className="h-3 w-3" />
                            Connector #{alert.connectorId}
                          </span>
                        )}
                      </div>
                      {alert.isAcknowledged && alert.acknowledgedBy && (
                        <p className="text-xs text-muted-foreground">
                          Acknowledged by {alert.acknowledgedBy} at{" "}
                          {formatDate(alert.acknowledgedAt!)}
                        </p>
                      )}
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setSelectedAlert(alert)}
                    >
                      <Eye className="h-4 w-4" />
                    </Button>
                    {!alert.isAcknowledged && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => acknowledgeMutation.mutate(alert.id)}
                      >
                        <CheckCircle2 className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))
        ) : (
          <Card>
            <CardContent className="py-8 text-center text-muted-foreground">
              <Bell className="h-12 w-12 mx-auto mb-4 opacity-50" />
              <p>No alerts found</p>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-muted-foreground">
            Showing {(currentPage - 1) * pageSize + 1} -{" "}
            {Math.min(currentPage * pageSize, totalCount)} of {totalCount}
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Detail Modal */}
      {selectedAlert && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <Card className="w-full max-w-lg m-4">
            <CardHeader className="flex flex-row items-center justify-between">
              <div className="flex items-center gap-2">
                {getTypeIcon(selectedAlert.type)}
                <CardTitle>Alert Details</CardTitle>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSelectedAlert(null)}
              >
                <X className="h-4 w-4" />
              </Button>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <p className="text-sm text-muted-foreground">Title</p>
                <p className="font-medium">{selectedAlert.title}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Message</p>
                <p>{selectedAlert.message}</p>
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <p className="text-sm text-muted-foreground">Type</p>
                  <Badge variant={getTypeColor(selectedAlert.type)}>
                    {selectedAlert.type}
                  </Badge>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Status</p>
                  <Badge
                    variant={
                      selectedAlert.isAcknowledged ? "secondary" : "default"
                    }
                  >
                    {selectedAlert.isAcknowledged
                      ? "Acknowledged"
                      : "Pending"}
                  </Badge>
                </div>
              </div>
              {selectedAlert.stationName && (
                <div>
                  <p className="text-sm text-muted-foreground">Station</p>
                  <p className="flex items-center gap-2">
                    <MapPin className="h-4 w-4" />
                    {selectedAlert.stationName}
                  </p>
                </div>
              )}
              <div>
                <p className="text-sm text-muted-foreground">Created</p>
                <p>{formatDate(selectedAlert.createdAt)}</p>
              </div>
              {selectedAlert.isAcknowledged && (
                <div>
                  <p className="text-sm text-muted-foreground">Acknowledged</p>
                  <p>
                    {selectedAlert.acknowledgedBy} at{" "}
                    {formatDate(selectedAlert.acknowledgedAt!)}
                  </p>
                </div>
              )}

              {!selectedAlert.isAcknowledged && (
                <Button
                  className="w-full"
                  onClick={() => {
                    acknowledgeMutation.mutate(selectedAlert.id);
                    setSelectedAlert(null);
                  }}
                >
                  <CheckCircle2 className="mr-2 h-4 w-4" />
                  Acknowledge Alert
                </Button>
              )}
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
