"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useMutation } from "@tanstack/react-query";
import { Header } from "@/components/layout/header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { AccessDenied } from "@/components/ui/access-denied";
import { stationsApi, stationGroupsApi, tariffsApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission } from "@/lib/use-permission";
import { ArrowLeft } from "lucide-react";
import { StationPhotoUpload, LocationPicker } from "@/components/stations";
import type { PhotoItem } from "@/components/stations";

export default function CreateStationPage() {
  const hasAccess = useRequirePermission("KLC.Stations.Create");
  const router = useRouter();
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    stationCode: "",
    name: "",
    address: "",
    latitude: 21.0285,
    longitude: 105.8542,
    stationGroupId: "",
    tariffPlanId: "",
  });
  const [photos, setPhotos] = useState<PhotoItem[]>([]);

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

      // Link uploaded photos to the newly created station
      for (let i = 0; i < photos.length; i++) {
        await stationsApi.addPhoto(data.id, {
          url: photos[i].url,
          isPrimary: photos[i].isPrimary ?? i === 0,
          sortOrder: i,
        });
      }

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

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      <Header title={t("stations.createTitle")} description={t("stations.createDescription")} />

      <div className="flex-1 space-y-6 p-6">
        <Button variant="ghost" onClick={() => router.push("/stations")}>
          <ArrowLeft className="mr-2 h-4 w-4" /> {t("stations.backToStations")}
        </Button>

        <Card>
          <CardHeader>
            <CardTitle>{t("stations.stationDetails")}</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="text-sm font-medium">{t("stations.stationCode")} *</label>
                  <input
                    type="text"
                    value={formData.stationCode}
                    onChange={(e) => setFormData({ ...formData, stationCode: e.target.value })}
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder={t("stations.stationCodePlaceholder")}
                    required
                  />
                </div>
                <div>
                  <label className="text-sm font-medium">{t("stations.nameLabel")} *</label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder={t("stations.namePlaceholder")}
                    required
                  />
                </div>
              </div>

              <div>
                <label className="text-sm font-medium">{t("stations.addressLabel")} *</label>
                <input
                  type="text"
                  value={formData.address}
                  onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                  className="mt-1 w-full rounded-md border px-3 py-2"
                  placeholder={t("stations.addressPlaceholder")}
                  required
                />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="text-sm font-medium">{t("stations.latitudeLabel")} *</label>
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
                  <label className="text-sm font-medium">{t("stations.longitudeLabel")} *</label>
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

              <LocationPicker
                latitude={formData.latitude}
                longitude={formData.longitude}
                onLocationChange={(lat, lng) => setFormData({ ...formData, latitude: lat, longitude: lng })}
              />

              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <label className="text-sm font-medium">{t("stations.stationGroup")}</label>
                  <select
                    value={formData.stationGroupId}
                    onChange={(e) => setFormData({ ...formData, stationGroupId: e.target.value })}
                    className="mt-1 w-full rounded-md border px-3 py-2"
                  >
                    <option value="">{t("stations.none")}</option>
                    {(groupsData || []).map((g: { id: string; name: string }) => (
                      <option key={g.id} value={g.id}>{g.name}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="text-sm font-medium">{t("stations.tariffPlan")}</label>
                  <select
                    value={formData.tariffPlanId}
                    onChange={(e) => setFormData({ ...formData, tariffPlanId: e.target.value })}
                    className="mt-1 w-full rounded-md border px-3 py-2"
                  >
                    <option value="">{t("stations.default")}</option>
                    {(tariffsData || []).map((t: { id: string; name: string }) => (
                      <option key={t.id} value={t.id}>{t.name}</option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Photo Upload */}
              <StationPhotoUpload
                photos={photos}
                onChange={setPhotos}
              />

              {createMutation.isError && (
                <div className="text-sm text-red-500">
                  {t("stations.createFailed")}
                </div>
              )}

              <div className="flex gap-2">
                <Button type="submit" disabled={createMutation.isPending}>
                  {createMutation.isPending ? t("stations.creating") : t("stations.createStation")}
                </Button>
                <Button type="button" variant="outline" onClick={() => router.push("/stations")}>
                  {t("common.cancel")}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
