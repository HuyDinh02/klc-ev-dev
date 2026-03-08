"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { api, maintenanceApi } from "@/lib/api";
import {
  Wrench,
  Plus,
  Calendar,
  MapPin,
  Clock,
  User,
  CheckCircle2,
  AlertTriangle,
  Play,
  X,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";

interface MaintenanceTask {
  id: string;
  stationId: string;
  stationName: string;
  connectorNumber?: number;
  type: number;
  status: number;
  title: string;
  description: string;
  assignedTo: string;
  scheduledDate: string;
  startedAt?: string;
  completedAt?: string;
  notes?: string;
  creationTime: string;
}

interface MaintenanceStats {
  plannedCount: number;
  inProgressCount: number;
  completedCount: number;
  overdueCount: number;
}

const StatusLabels: Record<number, string> = {
  0: "Planned",
  1: "InProgress",
  2: "Completed",
  3: "Cancelled",
};

const TypeLabels: Record<number, string> = {
  0: "Scheduled",
  1: "Inspection",
  2: "Emergency",
};

export default function MaintenancePage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState("all");
  const [typeFilter, setTypeFilter] = useState("all");
  const [cursor, setCursor] = useState<string | null>(null);
  const [cursorStack, setCursorStack] = useState<(string | null)[]>([]);
  const [isCreating, setIsCreating] = useState(false);
  const [formData, setFormData] = useState({
    stationId: "",
    connectorId: "",
    type: "0",
    title: "",
    description: "",
    assignedTo: "",
    scheduledDate: "",
  });
  const pageSize = 20;

  const resetPagination = () => { setCursor(null); setCursorStack([]); };

  // Fetch tasks
  const { data: tasksData, isLoading } = useQuery({
    queryKey: ["maintenance-tasks", statusFilter, typeFilter, cursor],
    queryFn: async () => {
      const params: Record<string, string | number> = {
        maxResultCount: pageSize,
      };
      if (statusFilter !== "all") params.status = statusFilter;
      if (typeFilter !== "all") params.type = typeFilter;
      if (cursor) params.cursor = cursor;

      const res = await maintenanceApi.getAll(params as Parameters<typeof maintenanceApi.getAll>[0]);
      return res.data;
    },
  });

  // Fetch stations for dropdown
  const { data: stations } = useQuery({
    queryKey: ["stations-list"],
    queryFn: async () => {
      const res = await api.get("/stations", {
        params: { maxResultCount: 100 },
      });
      return res.data.items || [];
    },
    enabled: isCreating,
  });

  // Fetch stats
  const { data: stats } = useQuery<MaintenanceStats>({
    queryKey: ["maintenance-stats"],
    queryFn: async () => {
      const res = await maintenanceApi.getStats();
      return res.data;
    },
  });

  // Create task
  const createMutation = useMutation({
    mutationFn: async () => {
      await maintenanceApi.create({
        stationId: formData.stationId,
        type: parseInt(formData.type),
        title: formData.title,
        description: formData.description || undefined,
        assignedTo: formData.assignedTo,
        scheduledDate: new Date(formData.scheduledDate).toISOString(),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
      queryClient.invalidateQueries({ queryKey: ["maintenance-stats"] });
      setIsCreating(false);
      setFormData({
        stationId: "",
        connectorId: "",
        type: "0",
        title: "",
        description: "",
        assignedTo: "",
        scheduledDate: "",
      });
    },
  });

  // Start task
  const startMutation = useMutation({
    mutationFn: async (id: string) => {
      await maintenanceApi.start(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
      queryClient.invalidateQueries({ queryKey: ["maintenance-stats"] });
    },
  });

  // Complete task
  const completeMutation = useMutation({
    mutationFn: async (id: string) => {
      await maintenanceApi.complete(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
      queryClient.invalidateQueries({ queryKey: ["maintenance-stats"] });
    },
  });

  // Cancel task
  const cancelMutation = useMutation({
    mutationFn: async (id: string) => {
      await maintenanceApi.cancel(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
      queryClient.invalidateQueries({ queryKey: ["maintenance-stats"] });
    },
  });

  const tasks: MaintenanceTask[] = tasksData?.items || [];
  const totalCount = tasksData?.totalCount || 0;

  const getStatusColor = (status: number): "secondary" | "default" | "success" | "destructive" => {
    switch (status) {
      case 0: return "secondary";
      case 1: return "default";
      case 2: return "success";
      case 3: return "destructive";
      default: return "secondary";
    }
  };

  const getTypeColor = (type: number): "destructive" | "warning" | "default" | "secondary" => {
    switch (type) {
      case 2: return "destructive";
      case 1: return "warning";
      case 0: return "default";
      default: return "secondary";
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("vi-VN");
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate();
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Maintenance</h1>
          <p className="text-muted-foreground">
            Schedule and track maintenance tasks
          </p>
        </div>
        <Button onClick={() => setIsCreating(true)} disabled={isCreating}>
          <Plus className="mr-2 h-4 w-4" />
          New Task
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card
          className={`cursor-pointer ${statusFilter === "0" ? "ring-2 ring-primary" : ""}`}
          onClick={() => setStatusFilter(statusFilter === "0" ? "all" : "0")}
        >
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Planned</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats?.plannedCount || 0}</div>
          </CardContent>
        </Card>

        <Card
          className={`cursor-pointer ${statusFilter === "1" ? "ring-2 ring-blue-500" : ""}`}
          onClick={() => setStatusFilter(statusFilter === "1" ? "all" : "1")}
        >
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">In Progress</CardTitle>
            <Wrench className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">
              {stats?.inProgressCount || 0}
            </div>
          </CardContent>
        </Card>

        <Card
          className={`cursor-pointer ${statusFilter === "2" ? "ring-2 ring-green-500" : ""}`}
          onClick={() => setStatusFilter(statusFilter === "2" ? "all" : "2")}
        >
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Completed</CardTitle>
            <CheckCircle2 className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              {stats?.completedCount || 0}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Overdue</CardTitle>
            <AlertTriangle className="h-4 w-4 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">
              {stats?.overdueCount || 0}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Create Form */}
      {isCreating && (
        <Card>
          <CardHeader>
            <CardTitle>New Maintenance Task</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="text-sm font-medium">Station</label>
                  <select
                    value={formData.stationId}
                    onChange={(e) =>
                      setFormData({ ...formData, stationId: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    required
                  >
                    <option value="">Select station...</option>
                    {(stations || []).map((station: { id: string; name: string }) => (
                      <option key={station.id} value={station.id}>
                        {station.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="text-sm font-medium">Type</label>
                  <select
                    value={formData.type}
                    onChange={(e) =>
                      setFormData({ ...formData, type: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                  >
                    <option value="0">Scheduled</option>
                    <option value="1">Inspection</option>
                    <option value="2">Emergency</option>
                  </select>
                </div>
              </div>
              <div>
                <label className="text-sm font-medium">Title</label>
                <input
                  type="text"
                  value={formData.title}
                  onChange={(e) =>
                    setFormData({ ...formData, title: e.target.value })
                  }
                  className="mt-1 w-full rounded-md border px-3 py-2"
                  placeholder="e.g., Quarterly inspection"
                  required
                />
              </div>
              <div>
                <label className="text-sm font-medium">Description</label>
                <textarea
                  value={formData.description}
                  onChange={(e) =>
                    setFormData({ ...formData, description: e.target.value })
                  }
                  className="mt-1 w-full rounded-md border px-3 py-2"
                  rows={3}
                  placeholder="Describe the maintenance task..."
                />
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="text-sm font-medium">Assigned To</label>
                  <input
                    type="text"
                    value={formData.assignedTo}
                    onChange={(e) =>
                      setFormData({ ...formData, assignedTo: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder="Technician name"
                    required
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">Scheduled Date</label>
                  <input
                    type="date"
                    value={formData.scheduledDate}
                    onChange={(e) =>
                      setFormData({ ...formData, scheduledDate: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    required
                  />
                </div>
              </div>
              <div className="flex gap-2">
                <Button type="submit" disabled={createMutation.isPending}>
                  {createMutation.isPending ? "Creating..." : "Create Task"}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setIsCreating(false)}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      {/* Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex gap-4">
            <select
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value); resetPagination(); }}
              className="rounded-md border px-3 py-2"
            >
              <option value="all">All Status</option>
              <option value="0">Planned</option>
              <option value="1">In Progress</option>
              <option value="2">Completed</option>
              <option value="3">Cancelled</option>
            </select>
            <select
              value={typeFilter}
              onChange={(e) => { setTypeFilter(e.target.value); resetPagination(); }}
              className="rounded-md border px-3 py-2"
            >
              <option value="all">All Types</option>
              <option value="0">Scheduled</option>
              <option value="1">Inspection</option>
              <option value="2">Emergency</option>
            </select>
          </div>
        </CardContent>
      </Card>

      {/* Tasks List */}
      <div className="space-y-3">
        {isLoading ? (
          <div className="text-center py-8">Loading...</div>
        ) : tasks.length > 0 ? (
          tasks.map((task) => (
            <Card key={task.id}>
              <CardContent className="py-4">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-4">
                    <Wrench className="h-5 w-5 text-muted-foreground mt-1" />
                    <div className="space-y-1">
                      <div className="flex items-center gap-2">
                        <h3 className="font-semibold">{task.title}</h3>
                        <Badge variant={getTypeColor(task.type)}>
                          {TypeLabels[task.type] || "Unknown"}
                        </Badge>
                        <Badge variant={getStatusColor(task.status)}>
                          {StatusLabels[task.status] || "Unknown"}
                        </Badge>
                      </div>
                      {task.description && (
                        <p className="text-sm text-muted-foreground">
                          {task.description}
                        </p>
                      )}
                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <MapPin className="h-3 w-3" />
                          {task.stationName}
                        </span>
                        <span className="flex items-center gap-1">
                          <Calendar className="h-3 w-3" />
                          {formatDate(task.scheduledDate)}
                        </span>
                        <span className="flex items-center gap-1">
                          <User className="h-3 w-3" />
                          {task.assignedTo}
                        </span>
                        {task.completedAt && (
                          <span className="flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            Done: {formatDate(task.completedAt)}
                          </span>
                        )}
                      </div>
                      {task.notes && (
                        <p className="text-xs text-muted-foreground italic">
                          Notes: {task.notes}
                        </p>
                      )}
                    </div>
                  </div>
                  <div className="flex gap-2">
                    {task.status === 0 && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => startMutation.mutate(task.id)}
                        disabled={startMutation.isPending}
                        title="Start"
                      >
                        <Play className="h-4 w-4" />
                      </Button>
                    )}
                    {task.status === 1 && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => completeMutation.mutate(task.id)}
                        disabled={completeMutation.isPending}
                        title="Complete"
                      >
                        <CheckCircle2 className="h-4 w-4" />
                      </Button>
                    )}
                    {(task.status === 0 || task.status === 1) && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => {
                          if (confirm("Cancel this task?")) {
                            cancelMutation.mutate(task.id);
                          }
                        }}
                        disabled={cancelMutation.isPending}
                        title="Cancel"
                      >
                        <X className="h-4 w-4 text-red-500" />
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
              <Wrench className="h-12 w-12 mx-auto mb-4 opacity-50" />
              <p>No maintenance tasks found</p>
              <p className="text-sm">
                Create a new task to schedule maintenance
              </p>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Pagination */}
      {(totalCount > pageSize || cursorStack.length > 0) && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-muted-foreground">
            {totalCount} total tasks
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
            {tasks.length === pageSize && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  const lastId = tasks[tasks.length - 1]?.id;
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
    </div>
  );
}
