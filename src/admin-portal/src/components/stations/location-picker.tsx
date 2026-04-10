"use client";

import { useEffect, useRef, useState, useCallback } from "react";
import { MapPin } from "lucide-react";

// Import Leaflet CSS at module level so it's available before map init
import "leaflet/dist/leaflet.css";

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
  const onLocationChangeRef = useRef(onLocationChange);
  onLocationChangeRef.current = onLocationChange;

  const defaultLat = latitude || 10.7769;
  const defaultLng = longitude || 106.7009;

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!mounted || !mapRef.current || mapInstanceRef.current) return;

    // Dynamic import to avoid SSR issues
    const L = require("leaflet");

    // Fix default icon paths
    delete (L.Icon.Default.prototype as any)._getIconUrl;
    L.Icon.Default.mergeOptions({
      iconRetinaUrl: "https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png",
      iconUrl: "https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png",
      shadowUrl: "https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png",
    });

    const map = L.map(mapRef.current).setView([defaultLat, defaultLng], 15);

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 19,
    }).addTo(map);

    // Add draggable marker
    const marker = L.marker([defaultLat, defaultLng], { draggable: true }).addTo(map);

    marker.on("dragend", () => {
      const pos = marker.getLatLng();
      onLocationChangeRef.current(
        Math.round(pos.lat * 1000000) / 1000000,
        Math.round(pos.lng * 1000000) / 1000000
      );
    });

    // Click on map to move marker
    map.on("click", (e: any) => {
      const { lat, lng } = e.latlng;
      marker.setLatLng([lat, lng]);
      onLocationChangeRef.current(
        Math.round(lat * 1000000) / 1000000,
        Math.round(lng * 1000000) / 1000000
      );
    });

    mapInstanceRef.current = map;
    markerRef.current = marker;

    // Force tile recalculation after CSS + layout are ready
    setTimeout(() => {
      map.invalidateSize();
    }, 200);

    return () => {
      map.remove();
      mapInstanceRef.current = null;
      markerRef.current = null;
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
      <div className="h-[350px] rounded-lg border bg-muted flex items-center justify-center">
        <MapPin className="h-6 w-6 text-muted-foreground animate-pulse" />
      </div>
    );
  }

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <MapPin className="h-4 w-4" />
        <span>Click on the map or drag the marker to set location</span>
      </div>
      <div
        ref={mapRef}
        className="h-[350px] rounded-lg border overflow-hidden"
        style={{ zIndex: 0 }}
      />
    </div>
  );
}
