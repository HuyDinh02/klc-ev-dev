"use client";

import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { vehiclesApi } from "@/lib/api";
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
};

export default function VehiclesPage() {
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
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Vehicles</h1>
          <p className="text-muted-foreground">
            Registered EV vehicles across all users
          </p>
        </div>
        <Badge variant="secondary" className="text-sm">
          {vehicles.length} total
        </Badge>
      </div>

      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <input
          type="text"
          placeholder="Search by make, model, plate..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full rounded-md border bg-background pl-9 pr-4 py-2 text-sm"
        />
      </div>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Vehicles</CardTitle>
            <Car className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{vehicles.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active</CardTitle>
            <Battery className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {vehicles.filter((v) => v.isActive).length}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">With CCS2</CardTitle>
            <Plug className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {vehicles.filter((v) => v.preferredConnectorType === 1).length}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Default Set</CardTitle>
            <Star className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {vehicles.filter((v) => v.isDefault).length}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Vehicle List */}
      <Card>
        <CardHeader>
          <CardTitle>All Vehicles</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8 text-muted-foreground">
              Loading vehicles...
            </div>
          ) : filtered.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              {search ? "No vehicles match your search." : "No vehicles registered yet."}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left text-muted-foreground">
                    <th className="pb-3 pr-4 font-medium">Vehicle</th>
                    <th className="pb-3 pr-4 font-medium">License Plate</th>
                    <th className="pb-3 pr-4 font-medium">Battery</th>
                    <th className="pb-3 pr-4 font-medium">Connector</th>
                    <th className="pb-3 pr-4 font-medium">Status</th>
                    <th className="pb-3 font-medium">Registered</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((vehicle) => (
                    <tr key={vehicle.id} className="border-b last:border-0">
                      <td className="py-3 pr-4">
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
                            <Star className="h-4 w-4 text-yellow-500 fill-yellow-500" />
                          )}
                        </div>
                      </td>
                      <td className="py-3 pr-4">
                        {vehicle.licensePlate || (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </td>
                      <td className="py-3 pr-4">
                        {vehicle.batteryCapacityKwh
                          ? `${vehicle.batteryCapacityKwh} kWh`
                          : "—"}
                      </td>
                      <td className="py-3 pr-4">
                        {vehicle.preferredConnectorType !== null &&
                        vehicle.preferredConnectorType !== undefined
                          ? connectorTypeLabels[vehicle.preferredConnectorType] ||
                            `Type ${vehicle.preferredConnectorType}`
                          : "—"}
                      </td>
                      <td className="py-3 pr-4">
                        <Badge
                          variant={vehicle.isActive ? "success" : "secondary"}
                        >
                          {vehicle.isActive ? "Active" : "Inactive"}
                        </Badge>
                      </td>
                      <td className="py-3 text-muted-foreground">
                        {new Date(vehicle.creationTime).toLocaleDateString(
                          "vi-VN"
                        )}
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
  );
}
