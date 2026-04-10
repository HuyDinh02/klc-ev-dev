"use client";

import { useEffect, useRef, useState } from "react";
import { MapPin } from "lucide-react";

interface LocationPickerProps {
  latitude: number;
  longitude: number;
  onLocationChange: (lat: number, lng: number) => void;
}

/**
 * OpenStreetMap location picker using Leaflet.
 * Click on map to set location. Draggable marker.
 * Default center: Ho Chi Minh City.
 */
export function LocationPicker({ latitude, longitude, onLocationChange }: LocationPickerProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<any>(null);
  const markerRef = useRef<any>(null);
  const [mounted, setMounted] = useState(false);

  const defaultLat = latitude || 10.7769;
  const defaultLng = longitude || 106.7009;

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!mounted || !mapRef.current) return;

    // Dynamic import to avoid SSR issues
    const L = require("leaflet");

    // Fix default icon paths
    delete (L.Icon.Default.prototype as any)._getIconUrl;
    L.Icon.Default.mergeOptions({
      iconRetinaUrl: "https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png",
      iconUrl: "https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png",
      shadowUrl: "https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png",
    });

    // Create map if not already created
    if (!mapInstanceRef.current) {
      const map = L.map(mapRef.current).setView([defaultLat, defaultLng], 15);

      L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        maxZoom: 19,
      }).addTo(map);

      // Add draggable marker
      const marker = L.marker([defaultLat, defaultLng], { draggable: true }).addTo(map);

      marker.on("dragend", () => {
        const pos = marker.getLatLng();
        onLocationChange(Math.round(pos.lat * 1000000) / 1000000, Math.round(pos.lng * 1000000) / 1000000);
      });

      // Click on map to move marker
      map.on("click", (e: any) => {
        const { lat, lng } = e.latlng;
        marker.setLatLng([lat, lng]);
        onLocationChange(Math.round(lat * 1000000) / 1000000, Math.round(lng * 1000000) / 1000000);
      });

      mapInstanceRef.current = map;
      markerRef.current = marker;
    }

    return () => {
      // Cleanup on unmount
      if (mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
        markerRef.current = null;
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mounted]);

  // Update marker when lat/lng changes from form inputs
  useEffect(() => {
    if (markerRef.current && mapInstanceRef.current && latitude && longitude) {
      markerRef.current.setLatLng([latitude, longitude]);
      mapInstanceRef.current.setView([latitude, longitude], mapInstanceRef.current.getZoom());
    }
  }, [latitude, longitude]);

  if (!mounted) {
    return (
      <div className="h-[300px] rounded-lg border bg-muted flex items-center justify-center">
        <MapPin className="h-6 w-6 text-muted-foreground animate-pulse" />
      </div>
    );
  }

  return (
    <div className="space-y-2">
      <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.css" />
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <MapPin className="h-4 w-4" />
        <span>Click on the map or drag the marker to set location</span>
      </div>
      <div
        ref={mapRef}
        className="h-[300px] rounded-lg border overflow-hidden z-0"
      />
    </div>
  );
}
