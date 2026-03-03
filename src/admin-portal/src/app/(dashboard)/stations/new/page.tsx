"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useMutation } from "@tanstack/react-query";
import { Header } from "@/components/layout/header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { stationsApi, stationGroupsApi, tariffsApi } from "@/lib/api";
import { ArrowLeft } from "lucide-react";

export default function CreateStationPage() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    stationCode: "",
    name: "",
    address: "",
    latitude: 21.0285,
    longitude: 105.8542,
    stationGroupId: "",
    tariffPlanId: "",
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

  const createMutation = useMutation({
    mutationFn: async () => {
      const payload = {
        stationCode: formData.stationCode,
        name: formData.name,
        address: formData.address,
        latitude: formData.latitude,
        longitude: formData.longitude,
        stationGroupId: formData.stationGroupId || undefined,
        tariffPlanId: formData.tariffPlanId || undefined,
      };
      const { data } = await stationsApi.create(payload);
      return data;
    },
    onSuccess: (data) => {
      router.push(`/stations/${data.id}`);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate();
  };

  return (
    <div className="flex flex-col">
      <Header title="Add Station" description="Create a new charging station" />

      <div className="flex-1 space-y-6 p-6">
        <Button variant="ghost" onClick={() => router.push("/stations")}>
          <ArrowLeft className="mr-2 h-4 w-4" /> Back to Stations
        </Button>

        <Card>
          <CardHeader>
            <CardTitle>Station Details</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="text-sm font-medium">Station Code *</label>
                  <input
                    type="text"
                    value={formData.stationCode}
                    onChange={(e) => setFormData({ ...formData, stationCode: e.target.value })}
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder="e.g., HCM-001"
                    required
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">Name *</label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder="e.g., Station HCM District 7"
                    required
                  />
                </div>
              </div>

              <div>
                <label className="text-sm font-medium">Address *</label>
                <input
                  type="text"
                  value={formData.address}
                  onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                  className="mt-1 w-full rounded-md border px-3 py-2"
                  placeholder="e.g., 123 Nguyen Van Linh, Q7, HCM"
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

              {createMutation.isError && (
                <div className="text-sm text-red-500">
                  Failed to create station. Please check your input and try again.
                </div>
              )}

              <div className="flex gap-2">
                <Button type="submit" disabled={createMutation.isPending}>
                  {createMutation.isPending ? "Creating..." : "Create Station"}
                </Button>
                <Button type="button" variant="outline" onClick={() => router.push("/stations")}>
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
