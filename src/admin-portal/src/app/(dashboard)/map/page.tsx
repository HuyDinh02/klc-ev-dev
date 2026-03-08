"use client";

import { useQuery } from "@tanstack/react-query";
import { useEffect, useState, useRef } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { Skeleton } from "@/components/ui/skeleton";
import { monitoringApi } from "@/lib/api";
import { STATION_STATUS } from "@/lib/constants";
import { MapPin, WifiOff } from "lucide-react";

interface StationSummary {
  stationId: string;
  stationName: string;
  status: number;
  latitude: number | null;
  longitude: number | null;
  totalConnectors: number;
  availableConnectors: number;
  chargingConnectors: number;
  lastHeartbeat: string | null;
}

const DEFAULT_STATUS_COLOR = "#9CA3AF";

// Hanoi center
const DEFAULT_CENTER: [number, number] = [21.0285, 105.8542];
const DEFAULT_ZOOM = 12;

function StationMapInner({ stations }: { stations: StationSummary[] }) {
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const markersRef = useRef<L.Marker[]>([]);

  useEffect(() => {
    if (!mapRef.current || mapInstanceRef.current) return;

    const L = require("leaflet");

    // Fix default marker icons in webpack/next.js
    delete (L.Icon.Default.prototype as Record<string, unknown>)._getIconUrl;
    L.Icon.Default.mergeOptions({
      iconRetinaUrl: "https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png",
      iconUrl: "https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png",
      shadowUrl: "https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png",
    });

    const map = L.map(mapRef.current).setView(DEFAULT_CENTER, DEFAULT_ZOOM);
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 19,
    }).addTo(map);

    mapInstanceRef.current = map;

    return () => {
      map.remove();
      mapInstanceRef.current = null;
    };
  }, []);

  // Update markers when stations change
  useEffect(() => {
    const map = mapInstanceRef.current;
    if (!map) return;

    const L = require("leaflet");

    // Clear existing markers
    markersRef.current.forEach((m) => m.remove());
    markersRef.current = [];

    const validStations = stations.filter((s) => s.latitude && s.longitude);

    validStations.forEach((station) => {
      const statusConfig = STATION_STATUS[station.status];
      const color = statusConfig?.dotColor || DEFAULT_STATUS_COLOR;
      const statusLabel = statusConfig?.label || "Unknown";

      const icon = L.divIcon({
        className: "custom-marker",
        html: `<div style="
          background-color: ${color};
          width: 28px;
          height: 28px;
          border-radius: 50%;
          border: 3px solid white;
          box-shadow: 0 2px 6px rgba(0,0,0,0.3);
          display: flex;
          align-items: center;
          justify-content: center;
        "><div style="
          width: 8px;
          height: 8px;
          border-radius: 50%;
          background: white;
        "></div></div>`,
        iconSize: [28, 28],
        iconAnchor: [14, 14],
        popupAnchor: [0, -16],
      });

      const faulted = station.totalConnectors - station.availableConnectors - station.chargingConnectors;
      const heartbeat = station.lastHeartbeat
        ? new Date(station.lastHeartbeat).toLocaleString("vi-VN")
        : "N/A";

      const popup = `
        <div style="min-width: 200px; font-family: system-ui, sans-serif;">
          <div style="font-weight: 600; font-size: 14px; margin-bottom: 6px;">${station.stationName}</div>
          <div style="display: inline-block; padding: 2px 8px; border-radius: 9999px; font-size: 11px; font-weight: 500; color: white; background-color: ${color}; margin-bottom: 8px;">
            ${statusLabel}
          </div>
          <div style="font-size: 12px; color: #555; line-height: 1.6;">
            <div><strong>Connectors:</strong> ${station.totalConnectors}</div>
            <div style="color: ${STATION_STATUS[1].dotColor};">Available: ${station.availableConnectors}</div>
            <div style="color: ${STATION_STATUS[2].dotColor};">Charging: ${station.chargingConnectors}</div>
            ${faulted > 0 ? `<div style="color: ${STATION_STATUS[4].dotColor};">Faulted/Other: ${faulted}</div>` : ""}
            <div style="margin-top: 4px; color: #888;">Last heartbeat: ${heartbeat}</div>
          </div>
        </div>
      `;

      const marker = L.marker([station.latitude, station.longitude], { icon })
        .addTo(map)
        .bindPopup(popup);

      markersRef.current.push(marker);
    });

    // Fit bounds if we have stations
    if (validStations.length > 0) {
      const bounds = L.latLngBounds(
        validStations.map((s) => [s.latitude!, s.longitude!])
      );
      map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
    }
  }, [stations]);

  return <div ref={mapRef} style={{ height: "100%", width: "100%", minHeight: 500 }} />;
}

export default function StationMapPage() {
  const [isClient, setIsClient] = useState(false);

  useEffect(() => {
    setIsClient(true);
  }, []);

  const { data: dashboard } = useQuery({
    queryKey: ["monitoring-dashboard"],
    queryFn: async () => {
      const res = await monitoringApi.getDashboard();
      return res.data;
    },
    refetchInterval: 30000,
  });

  const stations: StationSummary[] = dashboard?.stationSummaries || [];
  const validStations = stations.filter((s) => s.latitude && s.longitude);

  const statusCounts = {
    online: stations.filter((s) => s.status === 1 || s.status === 2).length,
    offline: stations.filter((s) => s.status === 0).length,
    faulted: stations.filter((s) => s.status === 4).length,
    other: stations.filter((s) => s.status === 3 || s.status === 5).length,
  };

  return (
    <div className="space-y-6">
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title="Station Map" description="Geographic overview of all charging stations" />
      </div>

      {/* Legend & Stats */}
      <div className="flex flex-wrap gap-3 px-6">
        <Badge variant={STATION_STATUS[1].badgeVariant} className="gap-1">
          <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: STATION_STATUS[1].dotColor }} />
          {STATION_STATUS[1].label} ({statusCounts.online})
        </Badge>
        <Badge variant={STATION_STATUS[2].badgeVariant} className="gap-1">
          <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: STATION_STATUS[2].dotColor }} />
          {STATION_STATUS[2].label} ({stations.filter((s) => s.status === 2).length})
        </Badge>
        <Badge variant={STATION_STATUS[0].badgeVariant} className="gap-1">
          <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: STATION_STATUS[0].dotColor }} />
          {STATION_STATUS[0].label} ({statusCounts.offline})
        </Badge>
        <Badge variant={STATION_STATUS[4].badgeVariant} className="gap-1">
          <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: STATION_STATUS[4].dotColor }} />
          {STATION_STATUS[4].label} ({statusCounts.faulted})
        </Badge>
        <div className="ml-auto text-sm text-muted-foreground flex items-center gap-1">
          <MapPin className="h-4 w-4" />
          {validStations.length} / {stations.length} stations with coordinates
        </div>
      </div>

      {/* Map */}
      <Card>
        <CardContent className="p-0 overflow-hidden rounded-lg" style={{ height: 600 }}>
          {isClient ? (
            <>
              <link
                rel="stylesheet"
                href="https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.css"
              />
              <StationMapInner stations={stations} />
            </>
          ) : (
            <div className="flex h-full flex-col items-center justify-center gap-4 p-6">
              <Skeleton className="h-full w-full" />
            </div>
          )}
        </CardContent>
      </Card>

      {/* Station List */}
      <Card>
        <CardHeader>
          <CardTitle>Stations without coordinates</CardTitle>
        </CardHeader>
        <CardContent>
          {stations.filter((s) => !s.latitude || !s.longitude).length > 0 ? (
            <div className="space-y-2">
              {stations
                .filter((s) => !s.latitude || !s.longitude)
                .map((station) => (
                  <div
                    key={station.stationId}
                    className="flex items-center justify-between rounded-lg border p-3"
                  >
                    <div className="flex items-center gap-3">
                      <WifiOff className="h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">{station.stationName}</span>
                    </div>
                    <Badge variant={STATION_STATUS[station.status]?.badgeVariant || "secondary"}>
                      {STATION_STATUS[station.status]?.label || "Unknown"}
                    </Badge>
                  </div>
                ))}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              All stations have coordinates configured.
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
