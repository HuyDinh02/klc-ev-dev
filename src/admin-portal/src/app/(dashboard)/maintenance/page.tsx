"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { api } from "@/lib/api";
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
  Pause,
  X,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";

interface MaintenanceTask {
  id: string;
  stationId: string;
  stationName: string;
  connectorId?: number;
  type: "Scheduled" | "Emergency" | "Inspection";
  status: "Planned" | "InProgress" | "Completed" | "Cancelled";
  title: string;
  description: string;
  assignedTo: string;
  scheduledDate: string;
  startedAt?: string;
  completedAt?: string;
  notes?: string;
  createdAt: string;
}

interface MaintenanceStats {
  plannedCount: number;
  inProgressCount: number;
  completedCount: number;
  overdueCount: number;
}

export default function MaintenancePage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState("all");
  const [typeFilter, setTypeFilter] = useState("all");
  const [currentPage, setCurrentPage] = useState(1);
  const [isCreating, setIsCreating] = useState(false);
  const [formData, setFormData] = useState({
    stationId: "",
    connectorId: "",
    type: "Scheduled",
    title: "",
    description: "",
    assignedTo: "",
    scheduledDate: "",
  });
  const pageSize = 20;

  // Fetch tasks
  const { data: tasksData, isLoading } = useQuery({
    queryKey: ["maintenance-tasks", statusFilter, typeFilter, currentPage],
    queryFn: async () => {
      const params: Record<string, string | number> = {
        skipCount: (currentPage - 1) * pageSize,
        maxResultCount: pageSize,
      };
      if (statusFilter !== "all") params.status = statusFilter;
      if (typeFilter !== "all") params.type = typeFilter;

      // Mock data since maintenance API may not exist
      return {
        items: [],
        totalCount: 0,
      };
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
      return {
        plannedCount: 5,
        inProgressCount: 2,
        completedCount: 48,
        overdueCount: 1,
      };
    },
  });

  // Start task
  const startMutation = useMutation({
    mutationFn: async (id: string) => {
      // Would call actual API
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
    },
  });

  // Complete task
  const completeMutation = useMutation({
    mutationFn: async (id: string) => {
      // Would call actual API
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
    },
  });

  // Cancel task
  const cancelMutation = useMutation({
    mutationFn: async (id: string) => {
      // Would call actual API
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenance-tasks"] });
    },
  });

  const tasks: MaintenanceTask[] = tasksData?.items || [];
  const totalCount = tasksData?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const getStatusColor = (status: string): "secondary" | "default" | "success" | "destructive" => {
    switch (status) {
      case "Planned":
        return "secondary";
      case "InProgress":
        return "default";
      case "Completed":
        return "success";
      case "Cancelled":
        return "destructive";
      default:
        return "secondary";
    }
  };

  const getTypeColor = (type: string): "destructive" | "warning" | "default" | "secondary" => {
    switch (type) {
      case "Emergency":
        return "destructive";
      case "Inspection":
        return "warning";
      case "Scheduled":
        return "default";
      default:
        return "secondary";
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("vi-VN");
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // Would call create API
    setIsCreating(false);
    setFormData({
      stationId: "",
      connectorId: "",
      type: "Scheduled",
      title: "",
      description: "",
      assignedTo: "",
      scheduledDate: "",
    });
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
          className={`cursor-pointer ${
            statusFilter === "Planned" ? "ring-2 ring-primary" : ""
          }`}
          onClick={() =>
            setStatusFilter(statusFilter === "Planned" ? "all" : "Planned")
          }
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
          className={`cursor-pointer ${
            statusFilter === "InProgress" ? "ring-2 ring-blue-500" : ""
          }`}
          onClick={() =>
            setStatusFilter(statusFilter === "InProgress" ? "all" : "InProgress")
          }
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
          className={`cursor-pointer ${
            statusFilter === "Completed" ? "ring-2 ring-green-500" : ""
          }`}
          onClick={() =>
            setStatusFilter(statusFilter === "Completed" ? "all" : "Completed")
          }
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
                    <option value="Scheduled">Scheduled</option>
                    <option value="Inspection">Inspection</option>
                    <option value="Emergency">Emergency</option>
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
                <Button type="submit">Create Task</Button>
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
              onChange={(e) => setStatusFilter(e.target.value)}
              className="rounded-md border px-3 py-2"
            >
              <option value="all">All Status</option>
              <option value="Planned">Planned</option>
              <option value="InProgress">In Progress</option>
              <option value="Completed">Completed</option>
              <option value="Cancelled">Cancelled</option>
            </select>
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              className="rounded-md border px-3 py-2"
            >
              <option value="all">All Types</option>
              <option value="Scheduled">Scheduled</option>
              <option value="Inspection">Inspection</option>
              <option value="Emergency">Emergency</option>
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
                          {task.type}
                        </Badge>
                        <Badge variant={getStatusColor(task.status)}>
                          {task.status}
                        </Badge>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        {task.description}
                      </p>
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
                      </div>
                    </div>
                  </div>
                  <div className="flex gap-2">
                    {task.status === "Planned" && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => startMutation.mutate(task.id)}
                      >
                        <Play className="h-4 w-4" />
                      </Button>
                    )}
                    {task.status === "InProgress" && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => completeMutation.mutate(task.id)}
                      >
                        <CheckCircle2 className="h-4 w-4" />
                      </Button>
                    )}
                    {(task.status === "Planned" ||
                      task.status === "InProgress") && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => {
                          if (confirm("Cancel this task?")) {
                            cancelMutation.mutate(task.id);
                          }
                        }}
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
    </div>
  );
}
