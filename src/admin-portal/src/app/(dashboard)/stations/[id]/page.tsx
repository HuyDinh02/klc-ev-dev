"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Header } from "@/components/layout/header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { stationsApi, connectorsApi, faultsApi } from "@/lib/api";
import { formatDateTime } from "@/lib/utils";
import {
  ArrowLeft,
  Edit,
  Power,
  PowerOff,
  Trash2,
  Plus,
  MapPin,
  Zap,
  AlertTriangle,
  Clock,
  X,
} from "lucide-react";

const StationStatusLabels: Record<number, string> = {
  0: "Offline", 1: "Available", 2: "Occupied", 3: "Unavailable", 4: "Faulted", 5: "Decommissioned",
};

const ConnectorTypeLabels: Record<number | string, string> = {
  0: "Type 2", 1: "CCS2", 2: "CHAdeMO", 3: "GBT", 4: "Type 1",
  "Type1": "Type 1", "Type2": "Type 2", "CCS2": "CCS2", "CHAdeMO": "CHAdeMO", "GBT": "GBT",
};

const ConnectorStatusLabels: Record<number, string> = {
  0: "Available", 1: "Preparing", 2: "Charging", 3: "SuspendedEV",
  4: "SuspendedEVSE", 5: "Finishing", 6: "Reserved", 7: "Unavailable", 8: "Faulted",
};

function getStatusBadge(status: number | string) {
  const label = typeof status === "number" ? (StationStatusLabels[status] || "Unknown") : status;
  switch (label) {
    case "Available": return <Badge variant="success">Available</Badge>;
    case "Occupied": return <Badge variant="default">Occupied</Badge>;
    case "Offline": return <Badge variant="destructive">Offline</Badge>;
    case "Faulted": return <Badge variant="warning">Faulted</Badge>;
    default: return <Badge variant="secondary">{label}</Badge>;
  }
}

function getConnectorStatusBadge(status: number | string) {
  const label = typeof status === "number" ? (ConnectorStatusLabels[status] || "Unknown") : status;
  switch (label) {
    case "Available": return <Badge variant="success">Available</Badge>;
    case "Charging": return <Badge variant="default">Charging</Badge>;
    case "Faulted": return <Badge variant="destructive">Faulted</Badge>;
    case "Unavailable": return <Badge variant="secondary">Unavailable</Badge>;
    default: return <Badge variant="warning">{label}</Badge>;
  }
}

export default function StationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [showAddConnector, setShowAddConnector] = useState(false);
  const [newConnector, setNewConnector] = useState({ connectorNumber: 1, connectorType: 0, maxPowerKw: 22 });

  const { data: station, isLoading } = useQuery({
    queryKey: ["station", id],
    queryFn: async () => {
      const { data } = await stationsApi.getById(id);
      return data;
    },
  });

  const { data: faultsData } = useQuery({
    queryKey: ["station-faults", id],
    queryFn: async () => {
      const { data } = await faultsApi.getByStation(id);
      return data;
    },
  });

  const enableMutation = useMutation({
    mutationFn: () => stationsApi.enable(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["station", id] }),
  });

  const disableMutation = useMutation({
    mutationFn: () => stationsApi.disable(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["station", id] }),
  });

  const decommissionMutation = useMutation({
    mutationFn: () => stationsApi.decommission(id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["station", id] }); router.push("/stations"); },
  });

  const addConnectorMutation = useMutation({
    mutationFn: (data: { connectorNumber: number; connectorType: number; maxPowerKw: number }) =>
      connectorsApi.create(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station", id] });
      setShowAddConnector(false);
      setNewConnector({ connectorNumber: 1, connectorType: 0, maxPowerKw: 22 });
    },
  });

  const enableConnectorMutation = useMutation({
    mutationFn: (connId: string) => connectorsApi.enable(connId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["station", id] }),
  });

  const disableConnectorMutation = useMutation({
    mutationFn: (connId: string) => connectorsApi.disable(connId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["station", id] }),
  });

  const deleteConnectorMutation = useMutation({
    mutationFn: (connId: string) => connectorsApi.delete(connId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["station", id] }),
  });

  if (isLoading) {
    return <div className="p-6 text-center">Loading...</div>;
  }

  if (!station) {
    return <div className="p-6 text-center">Station not found</div>;
  }

  const connectors = station.connectors || [];
  const faults = faultsData?.items || faultsData || [];

  return (
    <div className="flex flex-col">
      <Header title={station.name} description={station.stationCode || ""} />

      <div className="flex-1 space-y-6 p-6">
        {/* Back + Actions */}
        <div className="flex items-center justify-between">
          <Button variant="ghost" onClick={() => router.push("/stations")}>
            <ArrowLeft className="mr-2 h-4 w-4" /> Back to Stations
          </Button>
          <div className="flex items-center gap-2">
            <Link href={`/stations/${id}/edit`}>
              <Button variant="outline"><Edit className="mr-2 h-4 w-4" /> Edit</Button>
            </Link>
            {station.isEnabled ? (
              <Button variant="outline" onClick={() => disableMutation.mutate()} disabled={disableMutation.isPending}>
                <PowerOff className="mr-2 h-4 w-4" /> Disable
              </Button>
            ) : (
              <Button variant="outline" onClick={() => enableMutation.mutate()} disabled={enableMutation.isPending}>
                <Power className="mr-2 h-4 w-4" /> Enable
              </Button>
            )}
            <Button variant="destructive" onClick={() => { if (confirm("Decommission this station?")) decommissionMutation.mutate(); }}>
              Decommission
            </Button>
          </div>
        </div>

        {/* Station Info */}
        <div className="grid gap-4 md:grid-cols-2">
          <Card>
            <CardHeader><CardTitle>Station Information</CardTitle></CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between"><span className="text-muted-foreground">Status</span>{getStatusBadge(station.status)}</div>
              <div className="flex justify-between"><span className="text-muted-foreground">Code</span><span className="font-mono">{station.stationCode}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">Address</span><span className="text-right max-w-[200px]">{station.address}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">Coordinates</span><span>{station.latitude?.toFixed(6)}, {station.longitude?.toFixed(6)}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">Enabled</span><Badge variant={station.isEnabled ? "success" : "secondary"}>{station.isEnabled ? "Yes" : "No"}</Badge></div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle>Technical Details</CardTitle></CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between"><span className="text-muted-foreground">Vendor</span><span>{station.vendor || "—"}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">Model</span><span>{station.model || "—"}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">Serial Number</span><span className="font-mono">{station.serialNumber || "—"}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">Firmware</span><span>{station.firmwareVersion || "—"}</span></div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Last Heartbeat</span>
                <span>{station.lastHeartbeat ? formatDateTime(station.lastHeartbeat) : "Never"}</span>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Connectors */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="flex items-center gap-2"><Zap className="h-5 w-5" /> Connectors ({connectors.length})</CardTitle>
            <Button size="sm" onClick={() => setShowAddConnector(true)} disabled={showAddConnector}>
              <Plus className="mr-2 h-4 w-4" /> Add Connector
            </Button>
          </CardHeader>
          <CardContent>
            {showAddConnector && (
              <div className="mb-4 rounded-lg border p-4 bg-muted/30">
                <h4 className="font-medium mb-3">New Connector</h4>
                <div className="grid gap-3 md:grid-cols-3">
                  <div>
                    <label className="text-sm font-medium">Number</label>
                    <input type="number" min={1} value={newConnector.connectorNumber}
                      onChange={(e) => setNewConnector({ ...newConnector, connectorNumber: parseInt(e.target.value) || 1 })}
                      className="mt-1 w-full rounded-md border px-3 py-2" />
                  </div>
                  <div>
                    <label className="text-sm font-medium">Type</label>
                    <select value={newConnector.connectorType}
                      onChange={(e) => setNewConnector({ ...newConnector, connectorType: parseInt(e.target.value) })}
                      className="mt-1 w-full rounded-md border px-3 py-2">
                      <option value={0}>Type 2</option>
                      <option value={1}>CCS2</option>
                      <option value={2}>CHAdeMO</option>
                      <option value={3}>GBT</option>
                      <option value={4}>Type 1</option>
                    </select>
                  </div>
                  <div>
                    <label className="text-sm font-medium">Max Power (kW)</label>
                    <input type="number" min={1} value={newConnector.maxPowerKw}
                      onChange={(e) => setNewConnector({ ...newConnector, maxPowerKw: parseFloat(e.target.value) || 22 })}
                      className="mt-1 w-full rounded-md border px-3 py-2" />
                  </div>
                </div>
                <div className="mt-3 flex gap-2">
                  <Button size="sm" onClick={() => addConnectorMutation.mutate(newConnector)} disabled={addConnectorMutation.isPending}>Add</Button>
                  <Button size="sm" variant="outline" onClick={() => setShowAddConnector(false)}>Cancel</Button>
                </div>
              </div>
            )}

            {connectors.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-4 py-3 text-left text-sm font-medium">#</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Type</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Max Power</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Status</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Enabled</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {connectors.map((conn: { id: string; connectorNumber: number; connectorType: number | string; maxPowerKw: number; status: number | string; isEnabled: boolean }) => (
                      <tr key={conn.id} className="border-b hover:bg-muted/50">
                        <td className="px-4 py-3 font-medium">#{conn.connectorNumber}</td>
                        <td className="px-4 py-3">{ConnectorTypeLabels[conn.connectorType] || conn.connectorType}</td>
                        <td className="px-4 py-3">{conn.maxPowerKw} kW</td>
                        <td className="px-4 py-3">{getConnectorStatusBadge(conn.status)}</td>
                        <td className="px-4 py-3"><Badge variant={conn.isEnabled ? "success" : "secondary"}>{conn.isEnabled ? "Yes" : "No"}</Badge></td>
                        <td className="px-4 py-3">
                          <div className="flex gap-1">
                            {conn.isEnabled ? (
                              <Button variant="ghost" size="sm" onClick={() => disableConnectorMutation.mutate(conn.id)}>
                                <PowerOff className="h-4 w-4" />
                              </Button>
                            ) : (
                              <Button variant="ghost" size="sm" onClick={() => enableConnectorMutation.mutate(conn.id)}>
                                <Power className="h-4 w-4" />
                              </Button>
                            )}
                            <Button variant="ghost" size="sm" onClick={() => { if (confirm("Delete this connector?")) deleteConnectorMutation.mutate(conn.id); }}>
                              <Trash2 className="h-4 w-4 text-red-500" />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground text-center py-4">No connectors. Add one to get started.</p>
            )}
          </CardContent>
        </Card>

        {/* Recent Faults */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2"><AlertTriangle className="h-5 w-5" /> Recent Faults</CardTitle>
          </CardHeader>
          <CardContent>
            {Array.isArray(faults) && faults.length > 0 ? (
              <div className="space-y-3">
                {faults.slice(0, 5).map((fault: { id: string; errorCode: string; errorInfo?: string; status: number | string; detectedAt?: string }) => (
                  <div key={fault.id} className="flex items-center justify-between rounded-lg border p-3">
                    <div>
                      <p className="font-medium">{fault.errorCode}</p>
                      <p className="text-sm text-muted-foreground">{fault.errorInfo}</p>
                    </div>
                    <div className="text-sm text-muted-foreground">
                      {fault.detectedAt ? formatDateTime(fault.detectedAt) : ""}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground text-center py-4">No recent faults</p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
