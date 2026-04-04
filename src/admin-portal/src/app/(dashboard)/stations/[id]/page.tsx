"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Header } from "@/components/layout/header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { StatusBadge } from "@/components/ui/status-badge";
import { Skeleton } from "@/components/ui/skeleton";
import { stationsApi, connectorsApi, faultsApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import { formatDateTime } from "@/lib/utils";
import {
  ArrowLeft,
  Edit,
  Power,
  PowerOff,
  Trash2,
  Plus,
  Zap,
  AlertTriangle,
  ImageIcon,
  Star,
  MapPin,
  Wifi,
  Coffee,
  Car,
  UtensilsCrossed,
  Store,
  Armchair,
  Umbrella,
  ShieldCheck,
  X,
} from "lucide-react";

const ConnectorTypeLabels: Record<number | string, string> = {
  0: "Type 2", 1: "CCS2", 2: "CHAdeMO", 3: "GBT", 4: "Type 1", 5: "NACS",
  "Type1": "Type 1", "Type2": "Type 2", "CCS2": "CCS2", "CHAdeMO": "CHAdeMO", "GBT": "GBT", "NACS": "NACS",
};

const AMENITY_TYPES = [
  { value: 0, labelKey: "stations.amenityWifi", icon: Wifi },
  { value: 1, labelKey: "stations.amenityRestroom", icon: MapPin },
  { value: 2, labelKey: "stations.amenityCoffeeShop", icon: Coffee },
  { value: 3, labelKey: "stations.amenityParking", icon: Car },
  { value: 4, labelKey: "stations.amenityRestaurant", icon: UtensilsCrossed },
  { value: 5, labelKey: "stations.amenityConvenienceStore", icon: Store },
  { value: 6, labelKey: "stations.amenityWaitingRoom", icon: Armchair },
  { value: 7, labelKey: "stations.amenityCanopy", icon: Umbrella },
  { value: 8, labelKey: "stations.amenitySecurity24h", icon: ShieldCheck },
];

export default function StationDetailPage() {
  const hasAccess = useRequirePermission("KLC.Stations");
  const canUpdate = useHasPermission("KLC.Stations.Update");
  const canDecommission = useHasPermission("KLC.Stations.Decommission");
  const canCreateConnector = useHasPermission("KLC.Connectors.Create");
  const canToggleConnector = useHasPermission("KLC.Connectors.Enable");
  const canDeleteConnector = useHasPermission("KLC.Connectors.Delete");
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { t } = useTranslation();
  const [showAddConnector, setShowAddConnector] = useState(false);
  const [newConnector, setNewConnector] = useState({ connectorNumber: 1, connectorType: 0, maxPowerKw: 22 });
  const [showAddAmenity, setShowAddAmenity] = useState(false);

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

  const { data: photosData } = useQuery({
    queryKey: ["station-photos", id],
    queryFn: async () => {
      const { data } = await stationsApi.getPhotos(id);
      return data;
    },
  });

  const { data: amenitiesData } = useQuery({
    queryKey: ["station-amenities", id],
    queryFn: async () => {
      const { data } = await stationsApi.getAmenities(id);
      return data as { id: string; stationId: string; amenityType: number }[];
    },
  });

  const addAmenityMutation = useMutation({
    mutationFn: (amenityType: number) => stationsApi.addAmenity(id, { amenityType }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["station-amenities", id] });
      setShowAddAmenity(false);
    },
  });

  const removeAmenityMutation = useMutation({
    mutationFn: (amenityId: string) => stationsApi.removeAmenity(id, amenityId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["station-amenities", id] }),
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

  const deleteMutation = useMutation({
    mutationFn: () => stationsApi.delete(id),
    onSuccess: () => { router.push("/stations"); },
    onError: (err: any) => {
      const msg = err?.response?.data?.error?.message || "Cannot delete station. Disable it first and ensure no active sessions.";
      alert(msg);
    },
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

  if (!hasAccess) return <AccessDenied />;

  if (isLoading) {
    return (
      <div className="flex flex-col">
        <div className="border-b px-6 py-4 space-y-2">
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-4 w-32" />
        </div>
        <div className="flex-1 space-y-6 p-6">
          <div className="flex items-center justify-between">
            <Skeleton className="h-9 w-36" />
            <div className="flex items-center gap-2">
              <Skeleton className="h-9 w-20" />
              <Skeleton className="h-9 w-24" />
              <Skeleton className="h-9 w-32" />
            </div>
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="rounded-lg border bg-card p-5 space-y-3">
              <Skeleton className="h-5 w-40" />
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="flex justify-between">
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 w-32" />
                </div>
              ))}
            </div>
            <div className="rounded-lg border bg-card p-5 space-y-3">
              <Skeleton className="h-5 w-40" />
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="flex justify-between">
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 w-32" />
                </div>
              ))}
            </div>
          </div>
          <div className="rounded-lg border bg-card p-5 space-y-3">
            <Skeleton className="h-5 w-32" />
            {Array.from({ length: 3 }).map((_, i) => (
              <Skeleton key={i} className="h-12 w-full" />
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (!station) {
    return <div className="p-6 text-center">{t("stations.notFound")}</div>;
  }

  const connectors = station.connectors || [];
  const faults = faultsData?.items || faultsData || [];
  const stationPhotos: { id: string; url: string; isPrimary: boolean }[] = Array.isArray(photosData) ? photosData : [];

  return (
    <div className="flex flex-col">
      <Header title={station.name} description={station.stationCode || ""} />

      <div className="flex-1 space-y-6 p-6">
        {/* Back + Actions */}
        <div className="flex items-center justify-between">
          <Button variant="ghost" onClick={() => router.push("/stations")}>
            <ArrowLeft className="mr-2 h-4 w-4" /> {t("stations.backToStations")}
          </Button>
          <div className="flex items-center gap-2">
            {canUpdate && (
              <Link href={`/stations/${id}/edit`}>
                <Button variant="outline"><Edit className="mr-2 h-4 w-4" /> {t("stations.edit")}</Button>
              </Link>
            )}
            {canUpdate && (station.isEnabled ? (
              <Button variant="outline" onClick={() => disableMutation.mutate()} disabled={disableMutation.isPending}>
                <PowerOff className="mr-2 h-4 w-4" /> {t("stations.disable")}
              </Button>
            ) : (
              <Button variant="outline" onClick={() => enableMutation.mutate()} disabled={enableMutation.isPending}>
                <Power className="mr-2 h-4 w-4" /> {t("stations.enable")}
              </Button>
            ))}
            {canDecommission && !station?.isEnabled && (
              <Button variant="destructive" size="sm" onClick={() => {
                if (confirm("Delete this station? Historical data (sessions, faults) will be preserved but the station will be hidden from all lists.")) {
                  deleteMutation.mutate();
                }
              }}>
                <Trash2 className="mr-2 h-4 w-4" /> Delete
              </Button>
            )}
          </div>
        </div>

        {/* Station Info */}
        <div className="grid gap-4 md:grid-cols-2">
          <Card>
            <CardHeader><CardTitle>{t("stations.stationInformation")}</CardTitle></CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between"><span className="text-muted-foreground">{t("common.status")}</span><StatusBadge type="station" value={typeof station.status === "number" ? station.status : 0} /></div>
              <div className="flex justify-between"><span className="text-muted-foreground">{t("stations.code")}</span><span className="font-mono">{station.stationCode}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">{t("stations.address")}</span><span className="text-right max-w-[200px]">{station.address}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">{t("stations.coordinates")}</span><span>{station.latitude?.toFixed(6)}, {station.longitude?.toFixed(6)}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">{t("stations.enabled")}</span><Badge variant={station.isEnabled ? "success" : "secondary"}>{station.isEnabled ? t("stations.yes") : t("stations.no")}</Badge></div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader><CardTitle>{t("stations.technicalDetails")}</CardTitle></CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between"><span className="text-muted-foreground">{t("stations.vendor")}</span><span>{station.vendor || "—"}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">{t("stations.model")}</span><span>{station.model || "—"}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">{t("stations.serialNumber")}</span><span className="font-mono">{station.serialNumber || "—"}</span></div>
              <div className="flex justify-between"><span className="text-muted-foreground">{t("stations.firmware")}</span><span>{station.firmwareVersion || "—"}</span></div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t("stations.lastHeartbeat")}</span>
                <span>{station.lastHeartbeat ? formatDateTime(station.lastHeartbeat) : t("stations.never")}</span>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Connectors */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="flex items-center gap-2"><Zap className="h-5 w-5" /> {t("stations.connectors")} ({connectors.length})</CardTitle>
            {canCreateConnector && (
              <Button size="sm" onClick={() => setShowAddConnector(true)} disabled={showAddConnector}>
                <Plus className="mr-2 h-4 w-4" /> {t("stations.addConnector")}
              </Button>
            )}
          </CardHeader>
          <CardContent>
            {showAddConnector && (
              <div className="mb-4 rounded-lg border p-4 bg-muted/30">
                <h4 className="font-medium mb-3">{t("stations.newConnector")}</h4>
                <div className="grid gap-3 md:grid-cols-3">
                  <div>
                    <label className="text-sm font-medium">{t("stations.connectorNumber")}</label>
                    <input type="number" min={1} value={newConnector.connectorNumber}
                      onChange={(e) => setNewConnector({ ...newConnector, connectorNumber: parseInt(e.target.value) || 1 })}
                      className="mt-1 w-full rounded-md border px-3 py-2" />
                  </div>
                  <div>
                    <label className="text-sm font-medium">{t("stations.connectorType")}</label>
                    <select value={newConnector.connectorType}
                      onChange={(e) => setNewConnector({ ...newConnector, connectorType: parseInt(e.target.value) })}
                      className="mt-1 w-full rounded-md border px-3 py-2">
                      <option value={0}>Type 2</option>
                      <option value={1}>CCS2</option>
                      <option value={2}>CHAdeMO</option>
                      <option value={3}>GBT</option>
                      <option value={4}>Type 1</option>
                      <option value={5}>NACS</option>
                    </select>
                  </div>
                  <div>
                    <label className="text-sm font-medium">{t("stations.maxPower")}</label>
                    <input type="number" min={1} value={newConnector.maxPowerKw}
                      onChange={(e) => setNewConnector({ ...newConnector, maxPowerKw: parseFloat(e.target.value) || 22 })}
                      className="mt-1 w-full rounded-md border px-3 py-2" />
                  </div>
                </div>
                <div className="mt-3 flex gap-2">
                  <Button size="sm" onClick={() => addConnectorMutation.mutate(newConnector)} disabled={addConnectorMutation.isPending}>{t("stations.add")}</Button>
                  <Button size="sm" variant="outline" onClick={() => setShowAddConnector(false)}>{t("common.cancel")}</Button>
                </div>
              </div>
            )}

            {connectors.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-4 py-3 text-left text-sm font-medium">#</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("stations.connectorType")}</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("stations.maxPowerShort")}</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("common.status")}</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("stations.connectorEnabled")}</th>
                      <th className="px-4 py-3 text-left text-sm font-medium">{t("common.actions")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {connectors.map((conn: { id: string; connectorNumber: number; connectorType: number | string; maxPowerKw: number; status: number | string; isEnabled: boolean }) => (
                      <tr key={conn.id} className="border-b hover:bg-muted/50">
                        <td className="px-4 py-3 font-medium">#{conn.connectorNumber}</td>
                        <td className="px-4 py-3">{ConnectorTypeLabels[conn.connectorType] || conn.connectorType}</td>
                        <td className="px-4 py-3">{conn.maxPowerKw} kW</td>
                        <td className="px-4 py-3"><StatusBadge type="connector" value={typeof conn.status === "number" ? conn.status : 0} /></td>
                        <td className="px-4 py-3"><Badge variant={conn.isEnabled ? "success" : "secondary"}>{conn.isEnabled ? t("stations.yes") : t("stations.no")}</Badge></td>
                        <td className="px-4 py-3">
                          <div className="flex gap-1">
                            {canToggleConnector && (conn.isEnabled ? (
                              <Button variant="ghost" size="sm" onClick={() => disableConnectorMutation.mutate(conn.id)}>
                                <PowerOff className="h-4 w-4" />
                              </Button>
                            ) : (
                              <Button variant="ghost" size="sm" onClick={() => enableConnectorMutation.mutate(conn.id)}>
                                <Power className="h-4 w-4" />
                              </Button>
                            ))}
                            {canDeleteConnector && (
                              <Button variant="ghost" size="sm" onClick={() => { if (confirm(t("stations.deleteConnectorConfirm"))) deleteConnectorMutation.mutate(conn.id); }}>
                                <Trash2 className="h-4 w-4 text-red-500" />
                              </Button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground text-center py-4">{t("stations.noConnectors")}</p>
            )}
          </CardContent>
        </Card>

        {/* Photos */}
        {stationPhotos.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2"><ImageIcon className="h-5 w-5" /> {t("stations.photos")} ({stationPhotos.length})</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5">
                {stationPhotos.map((photo, index) => (
                  <div key={photo.id} className="relative aspect-[4/3] overflow-hidden rounded-lg border">
                    <img
                      src={photo.url}
                      alt={`${station.name} ${index + 1}`}
                      className="h-full w-full object-cover"
                    />
                    {photo.isPrimary && (
                      <div className="absolute bottom-1 left-1 flex items-center gap-1 rounded bg-amber-500/90 px-1.5 py-0.5 text-[10px] font-medium text-white">
                        <Star className="h-2.5 w-2.5" />
                        {t("stations.primary")}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        {/* Amenities */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="flex items-center gap-2"><MapPin className="h-5 w-5" /> {t("stations.amenities")} ({amenitiesData?.length ?? 0})</CardTitle>
            {canUpdate && (
              <Button size="sm" onClick={() => setShowAddAmenity(!showAddAmenity)}>
                <Plus className="mr-2 h-4 w-4" /> {t("stations.addAmenity")}
              </Button>
            )}
          </CardHeader>
          <CardContent>
            {showAddAmenity && (
              <div className="mb-4 rounded-lg border p-4 bg-muted/30">
                <h4 className="font-medium mb-3">{t("stations.addAmenity")}</h4>
                <div className="grid gap-2 grid-cols-3 sm:grid-cols-4 md:grid-cols-5">
                  {AMENITY_TYPES.filter(a => !(amenitiesData ?? []).some(ea => ea.amenityType === a.value)).map((amenity) => (
                    <button
                      key={amenity.value}
                      className="flex flex-col items-center gap-1 rounded-lg border p-3 hover:bg-primary/10 hover:border-primary transition-colors text-sm"
                      onClick={() => addAmenityMutation.mutate(amenity.value)}
                      disabled={addAmenityMutation.isPending}
                    >
                      <amenity.icon className="h-5 w-5" />
                      <span className="text-xs text-center">{t(amenity.labelKey)}</span>
                    </button>
                  ))}
                </div>
                <div className="mt-3">
                  <Button size="sm" variant="outline" onClick={() => setShowAddAmenity(false)}>{t("common.cancel")}</Button>
                </div>
              </div>
            )}
            {(amenitiesData?.length ?? 0) > 0 ? (
              <div className="flex flex-wrap gap-2">
                {amenitiesData!.map((amenity) => {
                  const type = AMENITY_TYPES.find(a => a.value === amenity.amenityType);
                  const Icon = type?.icon ?? MapPin;
                  return (
                    <div key={amenity.id} className="flex items-center gap-2 rounded-full border px-3 py-1.5 text-sm bg-muted/30">
                      <Icon className="h-4 w-4" />
                      <span>{t(type?.labelKey ?? "stations.amenities")}</span>
                      {canUpdate && (
                        <button
                          onClick={() => removeAmenityMutation.mutate(amenity.id)}
                          className="ml-1 text-muted-foreground hover:text-red-500 transition-colors"
                          title={t("stations.removeAmenity")}
                        >
                          <X className="h-3.5 w-3.5" />
                        </button>
                      )}
                    </div>
                  );
                })}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground text-center py-4">{t("stations.noAmenities")}</p>
            )}
          </CardContent>
        </Card>

        {/* Recent Faults */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2"><AlertTriangle className="h-5 w-5" /> {t("stations.recentFaults")}</CardTitle>
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
              <p className="text-sm text-muted-foreground text-center py-4">{t("stations.noRecentFaults")}</p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
