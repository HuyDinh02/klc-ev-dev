"use client";

import { useState, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import { useQuery, useMutation } from "@tanstack/react-query";
import { Header } from "@/components/layout/header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { stationsApi, stationGroupsApi, tariffsApi } from "@/lib/api";
import { ArrowLeft } from "lucide-react";

export default function EditStationPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [formData, setFormData] = useState({
    name: "",
    address: "",
    latitude: 0,
    longitude: 0,
    stationGroupId: "",
    tariffPlanId: "",
    concurrencyStamp: "",
  });

  const { data: station, isLoading } = useQuery({
    queryKey: ["station", id],
    queryFn: async () => {
      const { data } = await stationsApi.getById(id);
      return data;
    },
  });

  const { data: groupsData } = useQuery({
    queryKey: ["station-groups-select"],
    queryFn: async () => {
      const { data } = await stationGroupsApi.getAll({ maxResultCount: 100 });
      return data.items || [];
    },
  });

  const { data: tariffsData } = useQuery({
    queryKey: ["tariffs-select"],
    queryFn: async () => {
      const { data } = await tariffsApi.getAll({ maxResultCount: 100 });
      return data.items || [];
    },
  });

  useEffect(() => {
    if (station) {
      setFormData({
        name: station.name || "",
        address: station.address || "",
        latitude: station.latitude || 0,
        longitude: station.longitude || 0,
        stationGroupId: station.stationGroupId || station.groupId || "",
        tariffPlanId: station.tariffPlanId || "",
        concurrencyStamp: station.concurrencyStamp || "",
      });
    }
  }, [station]);

  const updateMutation = useMutation({
    mutationFn: async () => {
      const payload = {
        name: formData.name,
        address: formData.address,
        latitude: formData.latitude,
        longitude: formData.longitude,
        stationGroupId: formData.stationGroupId || undefined,
        tariffPlanId: formData.tariffPlanId || undefined,
        concurrencyStamp: formData.concurrencyStamp || undefined,
      };
      await stationsApi.update(id, payload);
    },
    onSuccess: () => {
      router.push(`/stations/${id}`);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMutation.mutate();
  };

  if (isLoading) {
    return (
      <div className="flex flex-col">
        <div className="border-b px-6 py-4 space-y-2">
          <Skeleton className="h-6 w-32" />
          <Skeleton className="h-4 w-24" />
        </div>
        <div className="flex-1 space-y-6 p-6">
          <Skeleton className="h-9 w-36" />
          <div className="rounded-lg border bg-card p-5 space-y-4">
            <Skeleton className="h-5 w-32" />
            {Array.from({ length: 2 }).map((_, i) => (
              <div key={i} className="space-y-1">
                <Skeleton className="h-4 w-20" />
                <Skeleton className="h-10 w-full" />
              </div>
            ))}
            <div className="grid gap-4 md:grid-cols-2">
              {Array.from({ length: 2 }).map((_, i) => (
                <div key={i} className="space-y-1">
                  <Skeleton className="h-4 w-20" />
                  <Skeleton className="h-10 w-full" />
                </div>
              ))}
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              {Array.from({ length: 2 }).map((_, i) => (
                <div key={i} className="space-y-1">
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-10 w-full" />
                </div>
              ))}
            </div>
            <div className="flex gap-2">
              <Skeleton className="h-10 w-28" />
              <Skeleton className="h-10 w-20" />
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col">
      <Header title="Edit Station" description={station?.stationCode || ""} />

      <div className="flex-1 space-y-6 p-6">
        <Button variant="ghost" onClick={() => router.push(`/stations/${id}`)}>
          <ArrowLeft className="mr-2 h-4 w-4" /> Back to Station
        </Button>

        <Card>
          <CardHeader>
            <CardTitle>Station Details</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="text-sm font-medium">Name *</label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="mt-1 w-full rounded-md border px-3 py-2"
                  required
                />
              </div>

              <div>
                <label className="text-sm font-medium">Address *</label>
                <input
                  type="text"
                  value={formData.address}
                  onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                  className="mt-1 w-full rounded-md border px-3 py-2"
                  required
                />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="text-sm font-medium">Latitude *</label>
                  <input
                    type="number"
                    step="0.000001"
                    value={formData.latitude}
                    onChange={(e) => setFormData({ ...formData, latitude: parseFloat(e.target.value) || 0 })}
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    required
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">Longitude *</label>
                  <input
                    type="number"
                    step="0.000001"
                    value={formData.longitude}
                    onChange={(e) => setFormData({ ...formData, longitude: parseFloat(e.target.value) || 0 })}
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    required
                  />
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="text-sm font-medium">Station Group</label>
                  <select
                    value={formData.stationGroupId}
                    onChange={(e) => setFormData({ ...formData, stationGroupId: e.target.value })}
                    className="mt-1 w-full rounded-md border px-3 py-2"
                  >
                    <option value="">None</option>
                    {(groupsData || []).map((g: { id: string; name: string }) => (
                      <option key={g.id} value={g.id}>{g.name}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="text-sm font-medium">Tariff Plan</label>
                  <select
                    value={formData.tariffPlanId}
                    onChange={(e) => setFormData({ ...formData, tariffPlanId: e.target.value })}
                    className="mt-1 w-full rounded-md border px-3 py-2"
                  >
                    <option value="">Default</option>
                    {(tariffsData || []).map((t: { id: string; name: string }) => (
                      <option key={t.id} value={t.id}>{t.name}</option>
                    ))}
                  </select>
                </div>
              </div>

              {updateMutation.isError && (
                <div className="text-sm text-red-500">
                  Failed to update station. Please check your input and try again.
                </div>
              )}

              <div className="flex gap-2">
                <Button type="submit" disabled={updateMutation.isPending}>
                  {updateMutation.isPending ? "Saving..." : "Save Changes"}
                </Button>
                <Button type="button" variant="outline" onClick={() => router.push(`/stations/${id}`)}>
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
