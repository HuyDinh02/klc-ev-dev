"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonTable } from "@/components/ui/skeleton";
import { api } from "@/lib/api";
import {
  Plug,
  Wifi,
  WifiOff,
  Play,
  Square,
  RefreshCw,
  FileText,
  Cpu,
  RotateCcw,
  Unlock,
  ToggleLeft,
  Settings,
  Zap,
  Gauge,
  Download,
  FileSearch,
  ShieldCheck,
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

const vendorProfileVariant = (vp: number): "info" | "warning" | "secondary" => {
  switch (vp) {
    case 1: return "info";
    case 2: return "warning";
    default: return "secondary";
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
  const [showPowerLimit, setShowPowerLimit] = useState(false);
  const [showUpdateFirmware, setShowUpdateFirmware] = useState(false);
  const [showGetDiagnostics, setShowGetDiagnostics] = useState(false);
  const [resetForm, setResetForm] = useState({ type: "Soft" });
  const [unlockForm, setUnlockForm] = useState({ connectorId: 1 });
  const [availabilityForm, setAvailabilityForm] = useState({ connectorId: 0, type: "Operative" });
  const [changeConfigForm, setChangeConfigForm] = useState({ key: "", value: "" });
  const [triggerForm, setTriggerForm] = useState({ requestedMessage: "StatusNotification", connectorId: undefined as number | undefined });
  const [powerLimitForm, setPowerLimitForm] = useState({ connectorId: 1, maxPowerKw: 22 });
  const [updateFirmwareForm, setUpdateFirmwareForm] = useState({ location: "", retrieveDate: "", retries: undefined as number | undefined, retryInterval: undefined as number | undefined });
  const [getDiagnosticsForm, setGetDiagnosticsForm] = useState({ location: "", startTime: "", stopTime: "" });
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

  const powerLimitMutation = useMutation({
    mutationFn: async () => (await api.post(`/ocpp/connections/${selectedCp}/set-power-limit`, powerLimitForm)).data,
    onSuccess: (data) => { setShowPowerLimit(false); setCommandResult(data); },
  });

  const updateFirmwareMutation = useMutation({
    mutationFn: async () => {
      const payload: Record<string, unknown> = {
        location: updateFirmwareForm.location,
        retrieveDate: new Date(updateFirmwareForm.retrieveDate).toISOString(),
      };
      if (updateFirmwareForm.retries != null) payload.retries = updateFirmwareForm.retries;
      if (updateFirmwareForm.retryInterval != null) payload.retryInterval = updateFirmwareForm.retryInterval;
      return (await api.post(`/ocpp/connections/${selectedCp}/update-firmware`, payload)).data;
    },
    onSuccess: (data) => { setShowUpdateFirmware(false); setCommandResult(data); },
  });

  const getDiagnosticsMutation = useMutation({
    mutationFn: async () => {
      const payload: Record<string, unknown> = { location: getDiagnosticsForm.location };
      if (getDiagnosticsForm.startTime) payload.startTime = new Date(getDiagnosticsForm.startTime).toISOString();
      if (getDiagnosticsForm.stopTime) payload.stopTime = new Date(getDiagnosticsForm.stopTime).toISOString();
      return (await api.post(`/ocpp/connections/${selectedCp}/get-diagnostics`, payload)).data;
    },
    onSuccess: (data) => { setShowGetDiagnostics(false); setCommandResult(data); },
  });

  const syncLocalListMutation = useMutation({
    mutationFn: async () => (await api.post(`/ocpp/connections/${selectedCp}/sync-local-list`)).data,
    onSuccess: (data) => { setCommandResult(data); },
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
      <PageHeader
        title="OCPP Management"
        description={`Connected chargers: ${connections.length}`}
        className="sticky top-0 z-10 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 pb-4"
      >
        <Button
          variant="outline"
          size="sm"
          onClick={() => queryClient.invalidateQueries({ queryKey: ["ocpp-connections"] })}
        >
          <RefreshCw className="mr-2 h-4 w-4" />
          Refresh
        </Button>
      </PageHeader>

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
                <SkeletonTable rows={3} cols={3} />
              ) : connections.length === 0 ? (
                <EmptyState
                  icon={Plug}
                  title="No chargers connected"
                  description="No chargers are currently connected via OCPP"
                />
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
                        <Badge variant={vendorProfileVariant(conn.vendorProfile)}>
                          {VendorProfileMap[conn.vendorProfile] || "Unknown"}
                        </Badge>
                        <Badge variant={conn.isRegistered ? "success" : "warning"}>
                          {conn.isRegistered ? "Registered" : "Pending"}
                        </Badge>
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
                <SkeletonTable rows={5} cols={5} />
              ) : events.length === 0 ? (
                <EmptyState
                  icon={FileText}
                  title="No events recorded"
                  description="OCPP events will appear here when chargers communicate"
                />
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
                            <Badge variant={vendorProfileVariant(evt.vendorProfile)} className="text-xs">
                              {VendorProfileMap[evt.vendorProfile] || "?"}
                            </Badge>
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
                  <Badge variant={detail.isOnline ? "success" : "destructive"}>
                    {detail.isOnline ? "Online" : "Offline"}
                  </Badge>
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
                    <Badge variant={vendorProfileVariant(detail.vendorProfile)}>
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
                      <Button size="sm" variant="outline" onClick={() => setShowPowerLimit(true)}>
                        <Gauge className="mr-1 h-3 w-3" />
                        Power Limit
                      </Button>
                      <Button size="sm" variant="outline" onClick={() => setShowUpdateFirmware(true)}>
                        <Download className="mr-1 h-3 w-3" />
                        Firmware
                      </Button>
                      <Button size="sm" variant="outline" onClick={() => setShowGetDiagnostics(true)}>
                        <FileSearch className="mr-1 h-3 w-3" />
                        Diagnostics
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          if (confirm("Sync local authorization list to this charger?")) {
                            syncLocalListMutation.mutate();
                          }
                        }}
                        disabled={syncLocalListMutation.isPending}
                      >
                        <ShieldCheck className="mr-1 h-3 w-3" />
                        {syncLocalListMutation.isPending ? "Syncing..." : "Sync Auth"}
                      </Button>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          ) : (
            <Card>
              <CardContent className="py-0">
                <EmptyState
                  icon={Plug}
                  title="Select a charger"
                  description="Choose a charger from the list to view details and send commands"
                />
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Remote Start Dialog */}
      <Dialog open={showRemoteStart} onClose={() => setShowRemoteStart(false)} size="sm">
        <DialogHeader onClose={() => setShowRemoteStart(false)}>Remote Start Transaction</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-4">
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
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowRemoteStart(false)}>
            Cancel
          </Button>
          <Button
            onClick={() => remoteStartMutation.mutate()}
            disabled={remoteStartMutation.isPending || !remoteStartForm.idTag}
          >
            {remoteStartMutation.isPending ? "Sending..." : "Send Start"}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Remote Stop Dialog */}
      <Dialog open={showRemoteStop} onClose={() => setShowRemoteStop(false)} size="sm">
        <DialogHeader onClose={() => setShowRemoteStop(false)}>Remote Stop Transaction</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-4">
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
        </DialogContent>
        <DialogFooter>
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
        </DialogFooter>
      </Dialog>

      {/* Reset Dialog */}
      <Dialog open={showReset} onClose={() => setShowReset(false)} size="sm">
        <DialogHeader onClose={() => setShowReset(false)}>Reset Charger</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-4">
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
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowReset(false)}>Cancel</Button>
          <Button
            variant="destructive"
            onClick={() => resetMutation.mutate()}
            disabled={resetMutation.isPending}
          >
            {resetMutation.isPending ? "Sending..." : "Send Reset"}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Unlock Connector Dialog */}
      <Dialog open={showUnlock} onClose={() => setShowUnlock(false)} size="sm">
        <DialogHeader onClose={() => setShowUnlock(false)}>Unlock Connector</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-4">
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
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowUnlock(false)}>Cancel</Button>
          <Button onClick={() => unlockMutation.mutate()} disabled={unlockMutation.isPending}>
            {unlockMutation.isPending ? "Sending..." : "Unlock"}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Change Availability Dialog */}
      <Dialog open={showAvailability} onClose={() => setShowAvailability(false)} size="sm">
        <DialogHeader onClose={() => setShowAvailability(false)}>Change Availability</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-4">
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
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowAvailability(false)}>Cancel</Button>
          <Button onClick={() => availabilityMutation.mutate()} disabled={availabilityMutation.isPending}>
            {availabilityMutation.isPending ? "Sending..." : "Change"}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Trigger Message Dialog */}
      <Dialog open={showTrigger} onClose={() => setShowTrigger(false)} size="sm">
        <DialogHeader onClose={() => setShowTrigger(false)}>Trigger Message</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-4">
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
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowTrigger(false)}>Cancel</Button>
          <Button onClick={() => triggerMutation.mutate()} disabled={triggerMutation.isPending}>
            {triggerMutation.isPending ? "Sending..." : "Trigger"}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Configuration Viewer Dialog */}
      <Dialog open={showConfig} onClose={() => setShowConfig(false)} size="lg">
        <DialogHeader onClose={() => setShowConfig(false)}>
          <div className="flex items-center gap-2">
            Charger Configuration
            <Button size="sm" variant="outline" onClick={() => { setShowConfig(false); setShowChangeConfig(true); }}>
              Edit Key
            </Button>
          </div>
        </DialogHeader>
        <DialogContent className="max-h-[60vh] overflow-y-auto">
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
            <EmptyState
              icon={Settings}
              title="No configuration keys"
              description="No configuration keys were returned from the charger"
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Change Configuration Dialog */}
      <Dialog open={showChangeConfig} onClose={() => setShowChangeConfig(false)} size="sm">
        <DialogHeader onClose={() => setShowChangeConfig(false)}>Change Configuration</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-4">
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
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowChangeConfig(false)}>Cancel</Button>
          <Button
            onClick={() => changeConfigMutation.mutate()}
            disabled={changeConfigMutation.isPending || !changeConfigForm.key}
          >
            {changeConfigMutation.isPending ? "Sending..." : "Save"}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Set Power Limit Dialog */}
      <Dialog open={showPowerLimit} onClose={() => setShowPowerLimit(false)} size="sm">
        <DialogHeader onClose={() => setShowPowerLimit(false)}>Set Power Limit</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-4">
            Charger: <strong>{selectedCp}</strong>
          </p>
          <div className="space-y-3">
            <div>
              <label className="text-sm font-medium">Connector ID</label>
              <Input
                type="number"
                value={powerLimitForm.connectorId}
                onChange={(e) => setPowerLimitForm({ ...powerLimitForm, connectorId: parseInt(e.target.value) || 1 })}
              />
            </div>
            <div>
              <label className="text-sm font-medium">Max Power (kW)</label>
              <Input
                type="number"
                step="0.1"
                value={powerLimitForm.maxPowerKw}
                onChange={(e) => setPowerLimitForm({ ...powerLimitForm, maxPowerKw: parseFloat(e.target.value) || 0 })}
              />
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowPowerLimit(false)}>Cancel</Button>
          <Button
            onClick={() => powerLimitMutation.mutate()}
            disabled={powerLimitMutation.isPending || powerLimitForm.maxPowerKw <= 0}
          >
            {powerLimitMutation.isPending ? "Sending..." : "Set Limit"}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Update Firmware Dialog */}
      <Dialog open={showUpdateFirmware} onClose={() => setShowUpdateFirmware(false)} size="sm">
        <DialogHeader onClose={() => setShowUpdateFirmware(false)}>Update Firmware</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-4">
            Charger: <strong>{selectedCp}</strong>
          </p>
          <div className="space-y-3">
            <div>
              <label className="text-sm font-medium">Download URL</label>
              <Input
                placeholder="https://example.com/firmware.bin"
                value={updateFirmwareForm.location}
                onChange={(e) => setUpdateFirmwareForm({ ...updateFirmwareForm, location: e.target.value })}
              />
            </div>
            <div>
              <label className="text-sm font-medium">Retrieve Date</label>
              <Input
                type="datetime-local"
                value={updateFirmwareForm.retrieveDate}
                onChange={(e) => setUpdateFirmwareForm({ ...updateFirmwareForm, retrieveDate: e.target.value })}
              />
            </div>
            <div>
              <label className="text-sm font-medium">Retries (optional)</label>
              <Input
                type="number"
                placeholder="Leave empty for default"
                onChange={(e) => setUpdateFirmwareForm({ ...updateFirmwareForm, retries: e.target.value ? parseInt(e.target.value) : undefined })}
              />
            </div>
            <div>
              <label className="text-sm font-medium">Retry Interval (seconds, optional)</label>
              <Input
                type="number"
                placeholder="Leave empty for default"
                onChange={(e) => setUpdateFirmwareForm({ ...updateFirmwareForm, retryInterval: e.target.value ? parseInt(e.target.value) : undefined })}
              />
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowUpdateFirmware(false)}>Cancel</Button>
          <Button
            onClick={() => updateFirmwareMutation.mutate()}
            disabled={updateFirmwareMutation.isPending || !updateFirmwareForm.location || !updateFirmwareForm.retrieveDate}
          >
            {updateFirmwareMutation.isPending ? "Sending..." : "Update Firmware"}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Get Diagnostics Dialog */}
      <Dialog open={showGetDiagnostics} onClose={() => setShowGetDiagnostics(false)} size="sm">
        <DialogHeader onClose={() => setShowGetDiagnostics(false)}>Get Diagnostics</DialogHeader>
        <DialogContent>
          <p className="text-sm text-muted-foreground mb-4">
            Charger: <strong>{selectedCp}</strong>
          </p>
          <div className="space-y-3">
            <div>
              <label className="text-sm font-medium">Upload URL</label>
              <Input
                placeholder="https://example.com/upload"
                value={getDiagnosticsForm.location}
                onChange={(e) => setGetDiagnosticsForm({ ...getDiagnosticsForm, location: e.target.value })}
              />
            </div>
            <div>
              <label className="text-sm font-medium">Start Time (optional)</label>
              <Input
                type="datetime-local"
                value={getDiagnosticsForm.startTime}
                onChange={(e) => setGetDiagnosticsForm({ ...getDiagnosticsForm, startTime: e.target.value })}
              />
            </div>
            <div>
              <label className="text-sm font-medium">Stop Time (optional)</label>
              <Input
                type="datetime-local"
                value={getDiagnosticsForm.stopTime}
                onChange={(e) => setGetDiagnosticsForm({ ...getDiagnosticsForm, stopTime: e.target.value })}
              />
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowGetDiagnostics(false)}>Cancel</Button>
          <Button
            onClick={() => getDiagnosticsMutation.mutate()}
            disabled={getDiagnosticsMutation.isPending || !getDiagnosticsForm.location}
          >
            {getDiagnosticsMutation.isPending ? "Sending..." : "Get Diagnostics"}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Command Result Toast */}
      {commandResult && (
        <div className="fixed bottom-4 right-4 z-50">
          <Card className="shadow-lg">
            <CardContent className="p-4">
              <div className="flex items-center gap-2">
                <Badge variant={commandResult.success ? "success" : "destructive"}>
                  {commandResult.success ? "Success" : "Failed"}
                </Badge>
                <span className="text-sm">{commandResult.message}</span>
                <Button variant="ghost" size="icon" className="h-6 w-6 ml-2" onClick={() => setCommandResult(null)}>
                  <span className="sr-only">Dismiss</span>
                  <span className="text-xs">&times;</span>
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
