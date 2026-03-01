"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { api } from "@/lib/api";
import {
  Plus,
  Edit,
  Trash2,
  FolderTree,
  MapPin,
  X,
  Check,
  ChevronDown,
  ChevronRight,
} from "lucide-react";

interface StationGroup {
  id: string;
  name: string;
  description: string;
  stationCount: number;
  stations: GroupStation[];
  createdAt: string;
}

interface GroupStation {
  id: string;
  name: string;
  address: string;
  status: string;
}

interface Station {
  id: string;
  name: string;
  address: string;
  groupId?: string;
}

export default function StationGroupsPage() {
  const queryClient = useQueryClient();
  const [isCreating, setIsCreating] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set());
  const [assigningGroupId, setAssigningGroupId] = useState<string | null>(null);
  const [formData, setFormData] = useState({ name: "", description: "" });

  // Fetch groups
  const { data: groups, isLoading } = useQuery<StationGroup[]>({
    queryKey: ["station-groups"],
    queryFn: async () => {
      const res = await api.get("/api/v1/station-groups");
      return res.data.items || [];
    },
  });

  // Fetch unassigned stations
  const { data: unassignedStations } = useQuery<Station[]>({
    queryKey: ["unassigned-stations"],
    queryFn: async () => {
      const res = await api.get("/api/v1/stations", {
        params: { groupId: null, maxResultCount: 100 },
      });
      return (res.data.items || []).filter((s: Station) => !s.groupId);
    },
    enabled: !!assigningGroupId,
  });

  // Create group
  const createMutation = useMutation({
    mutationFn: async (data: { name: string; description: string }) => {
      const res = await api.post("/api/v1/station-groups", data);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-groups"] });
      setIsCreating(false);
      resetForm();
    },
  });

  // Update group
  const updateMutation = useMutation({
    mutationFn: async ({
      id,
      data,
    }: {
      id: string;
      data: { name: string; description: string };
    }) => {
      const res = await api.put(`/api/v1/station-groups/${id}`, data);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-groups"] });
      setEditingId(null);
      resetForm();
    },
  });

  // Delete group
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/v1/station-groups/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-groups"] });
    },
  });

  // Assign station to group
  const assignMutation = useMutation({
    mutationFn: async ({
      groupId,
      stationId,
    }: {
      groupId: string;
      stationId: string;
    }) => {
      await api.post(`/api/v1/station-groups/${groupId}/assign`, { stationId });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-groups"] });
      queryClient.invalidateQueries({ queryKey: ["unassigned-stations"] });
    },
  });

  // Unassign station from group
  const unassignMutation = useMutation({
    mutationFn: async ({
      groupId,
      stationId,
    }: {
      groupId: string;
      stationId: string;
    }) => {
      await api.delete(
        `/api/v1/station-groups/${groupId}/stations/${stationId}`
      );
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-groups"] });
      queryClient.invalidateQueries({ queryKey: ["unassigned-stations"] });
    },
  });

  const resetForm = () => {
    setFormData({ name: "", description: "" });
  };

  const handleEdit = (group: StationGroup) => {
    setEditingId(group.id);
    setFormData({
      name: group.name,
      description: group.description,
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (editingId) {
      updateMutation.mutate({ id: editingId, data: formData });
    } else {
      createMutation.mutate(formData);
    }
  };

  const toggleExpand = (groupId: string) => {
    const newExpanded = new Set(expandedGroups);
    if (newExpanded.has(groupId)) {
      newExpanded.delete(groupId);
    } else {
      newExpanded.add(groupId);
    }
    setExpandedGroups(newExpanded);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Station Groups</h1>
          <p className="text-muted-foreground">
            Organize stations into logical groups for easier management
          </p>
        </div>
        <Button onClick={() => setIsCreating(true)} disabled={isCreating}>
          <Plus className="mr-2 h-4 w-4" />
          Add Group
        </Button>
      </div>

      {/* Create/Edit Form */}
      {(isCreating || editingId) && (
        <Card>
          <CardHeader>
            <CardTitle>{editingId ? "Edit Group" : "New Group"}</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="text-sm font-medium">Group Name</label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) =>
                      setFormData({ ...formData, name: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder="e.g., Downtown Stations"
                    required
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">Description</label>
                  <input
                    type="text"
                    value={formData.description}
                    onChange={(e) =>
                      setFormData({ ...formData, description: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder="e.g., All stations in downtown area"
                  />
                </div>
              </div>
              <div className="flex gap-2">
                <Button
                  type="submit"
                  disabled={createMutation.isPending || updateMutation.isPending}
                >
                  {editingId ? "Update" : "Create"} Group
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => {
                    setIsCreating(false);
                    setEditingId(null);
                    resetForm();
                  }}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      {/* Groups List */}
      <div className="space-y-4">
        {isLoading ? (
          <div className="text-center py-8">Loading...</div>
        ) : groups && groups.length > 0 ? (
          groups.map((group) => (
            <Card key={group.id}>
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <button
                      onClick={() => toggleExpand(group.id)}
                      className="p-1 hover:bg-muted rounded"
                    >
                      {expandedGroups.has(group.id) ? (
                        <ChevronDown className="h-5 w-5" />
                      ) : (
                        <ChevronRight className="h-5 w-5" />
                      )}
                    </button>
                    <FolderTree className="h-5 w-5 text-primary" />
                    <div>
                      <CardTitle className="text-lg">{group.name}</CardTitle>
                      {group.description && (
                        <p className="text-sm text-muted-foreground">
                          {group.description}
                        </p>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <Badge variant="secondary">
                      {group.stationCount} station
                      {group.stationCount !== 1 ? "s" : ""}
                    </Badge>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() =>
                        setAssigningGroupId(
                          assigningGroupId === group.id ? null : group.id
                        )
                      }
                    >
                      <Plus className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleEdit(group)}
                    >
                      <Edit className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => {
                        if (confirm("Delete this group?")) {
                          deleteMutation.mutate(group.id);
                        }
                      }}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>

              {/* Assign Station Dropdown */}
              {assigningGroupId === group.id && (
                <CardContent className="pt-0 pb-2">
                  <div className="border rounded-lg p-3 bg-muted/30">
                    <p className="text-sm font-medium mb-2">
                      Add station to this group:
                    </p>
                    <div className="flex flex-wrap gap-2">
                      {unassignedStations && unassignedStations.length > 0 ? (
                        unassignedStations.map((station) => (
                          <Button
                            key={station.id}
                            variant="outline"
                            size="sm"
                            onClick={() =>
                              assignMutation.mutate({
                                groupId: group.id,
                                stationId: station.id,
                              })
                            }
                          >
                            <MapPin className="mr-1 h-3 w-3" />
                            {station.name}
                          </Button>
                        ))
                      ) : (
                        <p className="text-sm text-muted-foreground">
                          No unassigned stations available
                        </p>
                      )}
                    </div>
                  </div>
                </CardContent>
              )}

              {/* Stations List */}
              {expandedGroups.has(group.id) && (
                <CardContent className="pt-0">
                  {group.stations && group.stations.length > 0 ? (
                    <div className="space-y-2">
                      {group.stations.map((station) => (
                        <div
                          key={station.id}
                          className="flex items-center justify-between rounded-lg border p-3"
                        >
                          <div className="flex items-center gap-3">
                            <MapPin className="h-4 w-4 text-muted-foreground" />
                            <div>
                              <p className="font-medium">{station.name}</p>
                              <p className="text-sm text-muted-foreground">
                                {station.address}
                              </p>
                            </div>
                          </div>
                          <div className="flex items-center gap-2">
                            <Badge
                              variant={
                                station.status === "Online"
                                  ? "success"
                                  : "secondary"
                              }
                            >
                              {station.status}
                            </Badge>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() =>
                                unassignMutation.mutate({
                                  groupId: group.id,
                                  stationId: station.id,
                                })
                              }
                            >
                              <X className="h-4 w-4" />
                            </Button>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground py-2">
                      No stations in this group yet
                    </p>
                  )}
                </CardContent>
              )}
            </Card>
          ))
        ) : (
          <Card>
            <CardContent className="py-8 text-center text-muted-foreground">
              No groups found. Create your first group to organize stations.
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
