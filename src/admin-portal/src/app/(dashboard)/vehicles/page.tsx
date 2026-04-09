"use client";

import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard, SkeletonTable } from "@/components/ui/skeleton";
import { vehiclesApi } from "@/lib/api";
import { formatDate } from "@/lib/utils";
import { useTranslation } from "@/lib/i18n";
import { Car, Battery, Plug, Star, Search } from "lucide-react";

interface Vehicle {
  id: string;
  userId: string;
  make: string;
  model: string;
  licensePlate: string | null;
  color: string | null;
  year: number | null;
  batteryCapacityKwh: number | null;
  preferredConnectorType: number | null;
  isActive: boolean;
  isDefault: boolean;
  nickname: string | null;
  creationTime: string;
}

const connectorTypeLabels: Record<number, string> = {
  0: "Type 2",
  1: "CCS2",
  2: "CHAdeMO",
  3: "GB/T",
  4: "Type 1",
  5: "NACS",
};

export default function VehiclesPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState("");

  const { data, isLoading } = useQuery({
    queryKey: ["vehicles"],
    queryFn: async () => {
      const res = await vehiclesApi.getAll({ maxResultCount: 100 });
      return res.data;
    },
  });

  const vehicles: Vehicle[] = data?.items || data || [];

  const filtered = vehicles.filter((v) => {
    if (!search) return true;
    const q = search.toLowerCase();
    return (
      v.make.toLowerCase().includes(q) ||
      v.model.toLowerCase().includes(q) ||
      v.licensePlate?.toLowerCase().includes(q) ||
      v.nickname?.toLowerCase().includes(q)
    );
  });

  return (
    <div className="flex flex-col">
      {/* Sticky header with search */}
      <div className="sticky top-0 z-30 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="p-6 pb-4">
          <PageHeader
            title={t("vehicles.title")}
            description={t("vehicles.description")}
          >
            <Badge variant="secondary" className="text-sm">
              {vehicles.length} {t("common.total").toLowerCase()}
            </Badge>
          </PageHeader>
        </div>

        {/* Search */}
        <div className="px-6 pb-4">
          <div className="relative max-w-sm">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="search"
              placeholder={t("vehicles.searchPlaceholder")}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="h-10 w-full rounded-md border bg-background pl-9 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary"
            />
          </div>
        </div>
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* Stats */}
        {isLoading ? (
          <div className="grid gap-4 md:grid-cols-4">
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-4">
            <StatCard
              label={t("vehicles.totalVehicles")}
              value={vehicles.length}
              icon={Car}
              iconColor="bg-blue-50 text-blue-600"
            />
            <StatCard
              label={t("common.active")}
              value={vehicles.filter((v) => v.isActive).length}
              icon={Battery}
              iconColor="bg-green-50 text-green-600"
            />
            <StatCard
              label={t("vehicles.withCCS2")}
              value={vehicles.filter((v) => v.preferredConnectorType === 1).length}
              icon={Plug}
              iconColor="bg-purple-50 text-purple-600"
            />
            <StatCard
              label={t("vehicles.defaultSet")}
              value={vehicles.filter((v) => v.isDefault).length}
              icon={Star}
              iconColor="bg-[var(--color-warning-light)] text-[var(--color-warning)]"
            />
          </div>
        )}

        {/* Vehicle List */}
        {isLoading ? (
          <SkeletonTable rows={8} cols={6} />
        ) : (
          <Card>
            <CardContent className="p-0">
              {filtered.length === 0 ? (
                <EmptyState
                  icon={search ? Search : Car}
                  title={search ? t("vehicles.noVehiclesMatch") : t("vehicles.noVehiclesYet")}
                  description={
                    search
                      ? t("vehicles.noVehiclesMatchDesc")
                      : t("vehicles.noVehiclesYetDesc")
                  }
                />
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="px-4 py-3 text-left text-sm font-medium">{t("vehicles.vehicle")}</th>
                        <th className="px-4 py-3 text-left text-sm font-medium">{t("vehicles.licensePlate")}</th>
                        <th className="px-4 py-3 text-right text-sm font-medium">{t("vehicles.battery")}</th>
                        <th className="px-4 py-3 text-left text-sm font-medium">{t("vehicles.connector")}</th>
                        <th className="px-4 py-3 text-left text-sm font-medium">{t("common.status")}</th>
                        <th className="px-4 py-3 text-left text-sm font-medium">{t("vehicles.registered")}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filtered.map((vehicle) => (
                        <tr key={vehicle.id} className="border-b last:border-0 hover:bg-muted/50">
                          <td className="px-4 py-3">
                            <div className="flex items-center gap-2">
                              <div>
                                <p className="font-medium">
                                  {vehicle.nickname || `${vehicle.make} ${vehicle.model}`}
                                </p>
                                {vehicle.nickname && (
                                  <p className="text-xs text-muted-foreground">
                                    {vehicle.make} {vehicle.model}
                                    {vehicle.year ? ` (${vehicle.year})` : ""}
                                  </p>
                                )}
                                {!vehicle.nickname && vehicle.year && (
                                  <p className="text-xs text-muted-foreground">
                                    {vehicle.year}
                                    {vehicle.color ? ` · ${vehicle.color}` : ""}
                                  </p>
                                )}
                              </div>
                              {vehicle.isDefault && (
                                <Star className="h-4 w-4 text-[var(--color-warning)] fill-[var(--color-warning)]" />
                              )}
                            </div>
                          </td>
                          <td className="px-4 py-3">
                            {vehicle.licensePlate || (
                              <span className="text-muted-foreground">—</span>
                            )}
                          </td>
                          <td className="px-4 py-3 text-right tabular-nums">
                            {vehicle.batteryCapacityKwh
                              ? `${vehicle.batteryCapacityKwh} kWh`
                              : "—"}
                          </td>
                          <td className="px-4 py-3">
                            {vehicle.preferredConnectorType !== null &&
                            vehicle.preferredConnectorType !== undefined
                              ? connectorTypeLabels[vehicle.preferredConnectorType] ||
                                `Type ${vehicle.preferredConnectorType}`
                              : "—"}
                          </td>
                          <td className="px-4 py-3">
                            <Badge
                              variant={vehicle.isActive ? "success" : "secondary"}
                            >
                              {vehicle.isActive ? t("common.active") : t("common.inactive")}
                            </Badge>
                          </td>
                          <td className="px-4 py-3 text-muted-foreground">
                            {formatDate(vehicle.creationTime)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
