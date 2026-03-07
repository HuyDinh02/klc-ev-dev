"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { api } from "@/lib/api";
import {
  Plug,
  Wifi,
  WifiOff,
  Play,
  Square,
  RefreshCw,
  FileText,
  Clock,
  Cpu,
  X,
  RotateCcw,
  Unlock,
  ToggleLeft,
  Settings,
  Zap,
} from "lucide-react";

interface OcppConnection {
  chargePointId: string;
  connectedAt: string;
  lastHeartbeat: string;
  isRegistered: boolean;
  stationId: string | null;
  vendorProfile: number;
}

interface OcppConnectionDetail {
  chargePointId: string;
  isOnline: boolean;
  connectedAt: string | null;
  lastHeartbeat: string | null;
  isRegistered: boolean;
  stationId: string | null;
  vendorProfile: number;
  vendor: string | null;
  model: string | null;
  firmwareVersion: string | null;
  serialNumber: string | null;
}

interface OcppRawEvent {
  id: string;
  chargePointId: string;
  action: string;
  uniqueId: string;
  messageType: number;
  payload: string;
  latencyMs: number | null;
  vendorProfile: number;
  receivedAt: string;
}

const VendorProfileMap: Record<number, string> = {
  0: "Generic",
  1: "Chargecore Global",
  2: "JUHANG",
};

const vendorProfileColor = (vp: number) => {
  switch (vp) {
    case 1: return "bg-blue-100 text-blue-800";
    case 2: return "bg-orange-100 text-orange-800";
    default: return "bg-gray-100 text-gray-800";
  }
};

export default function OcppManagementPage() {
  const queryClient = useQueryClient();
  const [selectedCp, setSelectedCp] = useState<string | null>(null);
  const [showRemoteStart, setShowRemoteStart] = useState(false);
  const [showRemoteStop, setShowRemoteStop] = useState(false);
  const [remoteStartForm, setRemoteStartForm] = useState({ connectorId: 1, idTag: "" });
  const [remoteStopForm, setRemoteStopForm] = useState({ transactionId: 0 });
  const [eventFilter, setEventFilter] = useState<string>("");
  const [showReset, setShowReset] = useState(false);
  const [showUnlock, setShowUnlock] = useState(false);
  const [showAvailability, setShowAvailability] = useState(false);
  const [showConfig, setShowConfig] = useState(false);
  const [showChangeConfig, setShowChangeConfig] = useState(false);
  const [showTrigger, setShowTrigger] = useState(false);
  const [resetForm, setResetForm] = useState({ type: "Soft" });
  const [unlockForm, setUnlockForm] = useState({ connectorId: 1 });
  const [availabilityForm, setAvailabilityForm] = useState({ connectorId: 0, type: "Operative" });
  const [changeConfigForm, setChangeConfigForm] = useState({ key: "", value: "" });
  const [triggerForm, setTriggerForm] = useState({ requestedMessage: "StatusNotification", connectorId: undefined as number | undefined });
  const [configData, setConfigData] = useState<{ key: string; value: string | null; readonly: boolean }[] | null>(null);
  const [commandResult, setCommandResult] = useState<{ success: boolean; message: string } | null>(null);

  const { data: connections = [], isLoading: connectionsLoading } = useQuery<OcppConnection[]>({
    queryKey: ["ocpp-connections"],
    queryFn: async () => (await api.get("/ocpp/connections")).data,
    refetchInterval: 10000,
  });

  const { data: detail } = useQuery<OcppConnectionDetail>({
    queryKey: ["ocpp-connection", selectedCp],
    queryFn: async () => (await api.get(`/ocpp/connections/${selectedCp}`)).data,
    enabled: !!selectedCp,
    refetchInterval: 10000,
  });

  const { data: events = [], isLoading: eventsLoading } = useQuery<OcppRawEvent[]>({
    queryKey: ["ocpp-events", eventFilter],
    queryFn: async () => {
      const params = new URLSearchParams({ limit: "50" });
      if (eventFilter) params.set("chargePointId", eventFilter);
      return (await api.get(`/ocpp/events?${params}`)).data;
    },
    refetchInterval: 15000,
  });

  const remoteStartMutation = useMutation({
    mutationFn: async () => {
      return (await api.post(`/ocpp/connections/${selectedCp}/remote-start`, remoteStartForm)).data;
    },
    onSuccess: () => {
      setShowRemoteStart(false);
      queryClient.invalidateQueries({ queryKey: ["ocpp-connections"] });
    },
  });

  const remoteStopMutation = useMutation({
    mutationFn: async () => {
      return (await api.post(`/ocpp/connections/${selectedCp}/remote-stop`, remoteStopForm)).data;
    },
    onSuccess: () => {
      setShowRemoteStop(false);
      queryClient.invalidateQueries({ queryKey: ["ocpp-connections"] });
    },
  });

  const resetMutation = useMutation({
    mutationFn: async () => (await api.post(`/ocpp/connections/${selectedCp}/reset`, resetForm)).data,
    onSuccess: (data) => { setShowReset(false); setCommandResult(data); },
  });

  const unlockMutation = useMutation({
    mutationFn: async () => (await api.post(`/ocpp/connections/${selectedCp}/unlock`, unlockForm)).data,
    onSuccess: (data) => { setShowUnlock(false); setCommandResult(data); },
  });

  const availabilityMutation = useMutation({
    mutationFn: async () => (await api.post(`/ocpp/connections/${selectedCp}/availability`, availabilityForm)).data,
    onSuccess: (data) => { setShowAvailability(false); setCommandResult(data); },
  });

  const getConfigMutation = useMutation({
    mutationFn: async () => (await api.get(`/ocpp/connections/${selectedCp}/configuration`)).data,
    onSuccess: (data) => { setConfigData(data.configurationKey || []); setShowConfig(true); },
  });

  const changeConfigMutation = useMutation({
    mutationFn: async () => (await api.post(`/ocpp/connections/${selectedCp}/configuration`, changeConfigForm)).data,
    onSuccess: (data) => { setShowChangeConfig(false); setCommandResult(data); },
  });

  const triggerMutation = useMutation({
    mutationFn: async () => (await api.post(`/ocpp/connections/${selectedCp}/trigger`, triggerForm)).data,
    onSuccess: (data) => { setShowTrigger(false); setCommandResult(data); },
  });

  const formatTime = (ts: string | null) => {
    if (!ts) return "-";
    return new Date(ts).toLocaleString("vi-VN");
  };

  const timeSince = (ts: string) => {
    const diff = Date.now() - new Date(ts).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return "just now";
    if (mins < 60) return `${mins}m ago`;
    return `${Math.floor(mins / 60)}h ${mins % 60}m ago`;
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">OCPP Management</h1>
          <p className="text-muted-foreground">
            Connected chargers: {connections.length}
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => queryClient.invalidateQueries({ queryKey: ["ocpp-connections"] })}
        >
          <RefreshCw className="mr-2 h-4 w-4" />
          Refresh
        </Button>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Connections List */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Plug className="h-5 w-5" />
                Connected Chargers
              </CardTitle>
            </CardHeader>
            <CardContent>
              {connectionsLoading ? (
                <p className="text-muted-foreground">Loading...</p>
              ) : connections.length === 0 ? (
                <p className="text-muted-foreground">No chargers currently connected</p>
              ) : (
                <div className="space-y-2">
                  {connections.map((conn) => (
                    <div
                      key={conn.chargePointId}
                      className={`flex items-center justify-between rounded-lg border p-3 cursor-pointer transition-colors ${
                        selectedCp === conn.chargePointId
                          ? "border-primary bg-primary/5"
                          : "hover:bg-muted/50"
                      }`}
                      onClick={() => setSelectedCp(conn.chargePointId)}
                    >
                      <div className="flex items-center gap-3">
                        <Wifi className="h-4 w-4 text-green-500" />
                        <div>
                          <div className="font-medium">{conn.chargePointId}</div>
                          <div className="text-xs text-muted-foreground">
                            Last heartbeat: {timeSince(conn.lastHeartbeat)}
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge className={vendorProfileColor(conn.vendorProfile)}>
                          {VendorProfileMap[conn.vendorProfile] || "Unknown"}
                        </Badge>
                        {conn.isRegistered ? (
                          <Badge variant="outline" className="text-green-600 border-green-600">
                            Registered
                          </Badge>
                        ) : (
                          <Badge variant="outline" className="text-yellow-600 border-yellow-600">
                            Pending
                          </Badge>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Event Log */}
          <Card className="mt-6">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center gap-2">
                  <FileText className="h-5 w-5" />
                  OCPP Event Log
                </CardTitle>
                <Input
                  placeholder="Filter by cpId..."
                  value={eventFilter}
                  onChange={(e) => setEventFilter(e.target.value)}
                  className="w-48"
                />
              </div>
            </CardHeader>
            <CardContent>
              {eventsLoading ? (
                <p className="text-muted-foreground">Loading...</p>
              ) : events.length === 0 ? (
                <p className="text-muted-foreground">No events recorded</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b text-left text-muted-foreground">
                        <th className="pb-2">Time</th>
                        <th className="pb-2">Charger</th>
                        <th className="pb-2">Action</th>
                        <th className="pb-2">Profile</th>
                        <th className="pb-2 text-right">Latency</th>
                      </tr>
                    </thead>
                    <tbody>
                      {events.map((evt) => (
                        <tr key={evt.id} className="border-b">
                          <td className="py-2 text-xs text-muted-foreground whitespace-nowrap">
                            {formatTime(evt.receivedAt)}
                          </td>
                          <td className="py-2 font-mono text-xs">{evt.chargePointId}</td>
                          <td className="py-2">
                            <Badge variant="outline">{evt.action}</Badge>
                          </td>
                          <td className="py-2">
                            <span className={`text-xs px-1.5 py-0.5 rounded ${vendorProfileColor(evt.vendorProfile)}`}>
                              {VendorProfileMap[evt.vendorProfile] || "?"}
                            </span>
                          </td>
                          <td className="py-2 text-right text-xs">
                            {evt.latencyMs != null ? `${evt.latencyMs}ms` : "-"}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Detail Panel */}
        <div>
          {selectedCp && detail ? (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Cpu className="h-5 w-5" />
                  {detail.chargePointId}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center gap-2">
                  {detail.isOnline ? (
                    <Wifi className="h-4 w-4 text-green-500" />
                  ) : (
                    <WifiOff className="h-4 w-4 text-red-500" />
                  )}
                  <span className={detail.isOnline ? "text-green-600" : "text-red-600"}>
                    {detail.isOnline ? "Online" : "Offline"}
                  </span>
                </div>

                <div className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Vendor</span>
                    <span>{detail.vendor || "-"}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Model</span>
                    <span>{detail.model || "-"}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Firmware</span>
                    <span>{detail.firmwareVersion || "-"}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Serial</span>
                    <span className="font-mono text-xs">{detail.serialNumber || "-"}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Profile</span>
                    <Badge className={vendorProfileColor(detail.vendorProfile)}>
                      {VendorProfileMap[detail.vendorProfile] || "Unknown"}
                    </Badge>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Connected</span>
                    <span className="text-xs">{formatTime(detail.connectedAt)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Last Heartbeat</span>
                    <span className="text-xs">{formatTime(detail.lastHeartbeat)}</span>
                  </div>
                </div>

                {detail.isOnline && (
                  <div className="space-y-2 pt-4 border-t">
                    <h4 className="font-medium text-sm">Remote Commands</h4>
                    <div className="grid grid-cols-2 gap-2">
                      <Button size="sm" onClick={() => setShowRemoteStart(true)}>
                        <Play className="mr-1 h-3 w-3" />
                        Start
                      </Button>
                      <Button size="sm" variant="destructive" onClick={() => setShowRemoteStop(true)}>
                        <Square className="mr-1 h-3 w-3" />
                        Stop
                      </Button>
                      <Button size="sm" variant="outline" onClick={() => setShowReset(true)}>
                        <RotateCcw className="mr-1 h-3 w-3" />
                        Reset
                      </Button>
                      <Button size="sm" variant="outline" onClick={() => setShowUnlock(true)}>
                        <Unlock className="mr-1 h-3 w-3" />
                        Unlock
                      </Button>
                      <Button size="sm" variant="outline" onClick={() => setShowAvailability(true)}>
                        <ToggleLeft className="mr-1 h-3 w-3" />
                        Availability
                      </Button>
                      <Button size="sm" variant="outline" onClick={() => setShowTrigger(true)}>
                        <Zap className="mr-1 h-3 w-3" />
                        Trigger
                      </Button>
                      <Button size="sm" variant="outline" className="col-span-2" onClick={() => getConfigMutation.mutate()}>
                        <Settings className="mr-1 h-3 w-3" />
                        {getConfigMutation.isPending ? "Loading..." : "Get Configuration"}
                      </Button>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          ) : (
            <Card>
              <CardContent className="py-12 text-center text-muted-foreground">
                <Plug className="mx-auto h-8 w-8 mb-2 opacity-50" />
                <p>Select a charger to view details</p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Remote Start Dialog */}
      {showRemoteStart && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg p-6 w-96 space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">Remote Start Transaction</h3>
              <Button variant="ghost" size="icon" onClick={() => setShowRemoteStart(false)}>
                <X className="h-4 w-4" />
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">
              Charger: <strong>{selectedCp}</strong>
            </p>
            <div className="space-y-3">
              <div>
                <label className="text-sm font-medium">Connector ID</label>
                <Input
                  type="number"
                  value={remoteStartForm.connectorId}
                  onChange={(e) =>
                    setRemoteStartForm({ ...remoteStartForm, connectorId: parseInt(e.target.value) || 1 })
                  }
                />
              </div>
              <div>
                <label className="text-sm font-medium">ID Tag</label>
                <Input
                  placeholder="User ID or RFID tag"
                  value={remoteStartForm.idTag}
                  onChange={(e) =>
                    setRemoteStartForm({ ...remoteStartForm, idTag: e.target.value })
                  }
                />
              </div>
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" onClick={() => setShowRemoteStart(false)}>
                Cancel
              </Button>
              <Button
                onClick={() => remoteStartMutation.mutate()}
                disabled={remoteStartMutation.isPending || !remoteStartForm.idTag}
              >
                {remoteStartMutation.isPending ? "Sending..." : "Send Start"}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Remote Stop Dialog */}
      {showRemoteStop && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg p-6 w-96 space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">Remote Stop Transaction</h3>
              <Button variant="ghost" size="icon" onClick={() => setShowRemoteStop(false)}>
                <X className="h-4 w-4" />
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">
              Charger: <strong>{selectedCp}</strong>
            </p>
            <div>
              <label className="text-sm font-medium">Transaction ID</label>
              <Input
                type="number"
                value={remoteStopForm.transactionId}
                onChange={(e) =>
                  setRemoteStopForm({ transactionId: parseInt(e.target.value) || 0 })
                }
              />
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" onClick={() => setShowRemoteStop(false)}>
                Cancel
              </Button>
              <Button
                variant="destructive"
                onClick={() => remoteStopMutation.mutate()}
                disabled={remoteStopMutation.isPending || !remoteStopForm.transactionId}
              >
                {remoteStopMutation.isPending ? "Sending..." : "Send Stop"}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Reset Dialog */}
      {showReset && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg p-6 w-96 space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">Reset Charger</h3>
              <Button variant="ghost" size="icon" onClick={() => setShowReset(false)}>
                <X className="h-4 w-4" />
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">
              Charger: <strong>{selectedCp}</strong>
            </p>
            <div>
              <label className="text-sm font-medium">Reset Type</label>
              <select
                className="w-full mt-1 rounded-md border p-2 text-sm"
                value={resetForm.type}
                onChange={(e) => setResetForm({ type: e.target.value })}
              >
                <option value="Soft">Soft (graceful restart)</option>
                <option value="Hard">Hard (immediate restart)</option>
              </select>
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" onClick={() => setShowReset(false)}>Cancel</Button>
              <Button
                variant="destructive"
                onClick={() => resetMutation.mutate()}
                disabled={resetMutation.isPending}
              >
                {resetMutation.isPending ? "Sending..." : "Send Reset"}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Unlock Connector Dialog */}
      {showUnlock && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg p-6 w-96 space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">Unlock Connector</h3>
              <Button variant="ghost" size="icon" onClick={() => setShowUnlock(false)}>
                <X className="h-4 w-4" />
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">
              Charger: <strong>{selectedCp}</strong>
            </p>
            <div>
              <label className="text-sm font-medium">Connector ID</label>
              <Input
                type="number"
                value={unlockForm.connectorId}
                onChange={(e) => setUnlockForm({ connectorId: parseInt(e.target.value) || 1 })}
              />
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" onClick={() => setShowUnlock(false)}>Cancel</Button>
              <Button onClick={() => unlockMutation.mutate()} disabled={unlockMutation.isPending}>
                {unlockMutation.isPending ? "Sending..." : "Unlock"}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Change Availability Dialog */}
      {showAvailability && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg p-6 w-96 space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">Change Availability</h3>
              <Button variant="ghost" size="icon" onClick={() => setShowAvailability(false)}>
                <X className="h-4 w-4" />
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">
              Charger: <strong>{selectedCp}</strong>
            </p>
            <div className="space-y-3">
              <div>
                <label className="text-sm font-medium">Connector ID</label>
                <Input
                  type="number"
                  value={availabilityForm.connectorId}
                  onChange={(e) => setAvailabilityForm({ ...availabilityForm, connectorId: parseInt(e.target.value) || 0 })}
                />
                <p className="text-xs text-muted-foreground mt-1">0 = entire charger</p>
              </div>
              <div>
                <label className="text-sm font-medium">Availability</label>
                <select
                  className="w-full mt-1 rounded-md border p-2 text-sm"
                  value={availabilityForm.type}
                  onChange={(e) => setAvailabilityForm({ ...availabilityForm, type: e.target.value })}
                >
                  <option value="Operative">Operative (available)</option>
                  <option value="Inoperative">Inoperative (disabled)</option>
                </select>
              </div>
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" onClick={() => setShowAvailability(false)}>Cancel</Button>
              <Button onClick={() => availabilityMutation.mutate()} disabled={availabilityMutation.isPending}>
                {availabilityMutation.isPending ? "Sending..." : "Change"}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Trigger Message Dialog */}
      {showTrigger && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg p-6 w-96 space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">Trigger Message</h3>
              <Button variant="ghost" size="icon" onClick={() => setShowTrigger(false)}>
                <X className="h-4 w-4" />
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">
              Charger: <strong>{selectedCp}</strong>
            </p>
            <div className="space-y-3">
              <div>
                <label className="text-sm font-medium">Message Type</label>
                <select
                  className="w-full mt-1 rounded-md border p-2 text-sm"
                  value={triggerForm.requestedMessage}
                  onChange={(e) => setTriggerForm({ ...triggerForm, requestedMessage: e.target.value })}
                >
                  <option value="StatusNotification">StatusNotification</option>
                  <option value="MeterValues">MeterValues</option>
                  <option value="Heartbeat">Heartbeat</option>
                  <option value="BootNotification">BootNotification</option>
                  <option value="DiagnosticsStatusNotification">DiagnosticsStatusNotification</option>
                  <option value="FirmwareStatusNotification">FirmwareStatusNotification</option>
                </select>
              </div>
              <div>
                <label className="text-sm font-medium">Connector ID (optional)</label>
                <Input
                  type="number"
                  placeholder="Leave empty for all"
                  onChange={(e) => setTriggerForm({ ...triggerForm, connectorId: e.target.value ? parseInt(e.target.value) : undefined })}
                />
              </div>
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" onClick={() => setShowTrigger(false)}>Cancel</Button>
              <Button onClick={() => triggerMutation.mutate()} disabled={triggerMutation.isPending}>
                {triggerMutation.isPending ? "Sending..." : "Trigger"}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Configuration Viewer Dialog */}
      {showConfig && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg p-6 w-[500px] max-h-[80vh] space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">Charger Configuration</h3>
              <div className="flex gap-2">
                <Button size="sm" variant="outline" onClick={() => { setShowConfig(false); setShowChangeConfig(true); }}>
                  Edit Key
                </Button>
                <Button variant="ghost" size="icon" onClick={() => setShowConfig(false)}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
            </div>
            <div className="overflow-y-auto max-h-[60vh]">
              {configData && configData.length > 0 ? (
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b text-left text-muted-foreground">
                      <th className="pb-2">Key</th>
                      <th className="pb-2">Value</th>
                      <th className="pb-2 text-center">RO</th>
                    </tr>
                  </thead>
                  <tbody>
                    {configData.map((entry) => (
                      <tr key={entry.key} className="border-b">
                        <td className="py-1.5 font-mono text-xs">{entry.key}</td>
                        <td className="py-1.5 text-xs break-all">{entry.value ?? "-"}</td>
                        <td className="py-1.5 text-center text-xs">{entry.readonly ? "Yes" : "No"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <p className="text-muted-foreground">No configuration keys returned</p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Change Configuration Dialog */}
      {showChangeConfig && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg p-6 w-96 space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">Change Configuration</h3>
              <Button variant="ghost" size="icon" onClick={() => setShowChangeConfig(false)}>
                <X className="h-4 w-4" />
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">
              Charger: <strong>{selectedCp}</strong>
            </p>
            <div className="space-y-3">
              <div>
                <label className="text-sm font-medium">Configuration Key</label>
                <Input
                  placeholder="e.g. MeterValueSampleInterval"
                  value={changeConfigForm.key}
                  onChange={(e) => setChangeConfigForm({ ...changeConfigForm, key: e.target.value })}
                />
              </div>
              <div>
                <label className="text-sm font-medium">Value</label>
                <Input
                  placeholder="New value"
                  value={changeConfigForm.value}
                  onChange={(e) => setChangeConfigForm({ ...changeConfigForm, value: e.target.value })}
                />
              </div>
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" onClick={() => setShowChangeConfig(false)}>Cancel</Button>
              <Button
                onClick={() => changeConfigMutation.mutate()}
                disabled={changeConfigMutation.isPending || !changeConfigForm.key}
              >
                {changeConfigMutation.isPending ? "Sending..." : "Save"}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Command Result Toast */}
      {commandResult && (
        <div className="fixed bottom-4 right-4 z-50">
          <div className={`rounded-lg p-4 shadow-lg ${commandResult.success ? "bg-green-50 border border-green-200" : "bg-red-50 border border-red-200"}`}>
            <div className="flex items-center gap-2">
              <span className={commandResult.success ? "text-green-800" : "text-red-800"}>
                {commandResult.success ? "✓" : "✗"} {commandResult.message}
              </span>
              <Button variant="ghost" size="icon" className="h-6 w-6" onClick={() => setCommandResult(null)}>
                <X className="h-3 w-3" />
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
