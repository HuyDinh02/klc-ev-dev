import Link from "next/link";
import { Eye, Edit, Power, PowerOff, MapPin } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { StatusBadge } from "@/components/ui/status-badge";
import { formatDateTime } from "@/lib/utils";
import { useTranslation } from "@/lib/i18n";
import type { StationListItem } from "./types";

interface StationBoardViewProps {
  stations: StationListItem[];
  onEnable: (id: string) => void;
  onDisable: (id: string) => void;
  isMutating: boolean;
}

export function StationBoardView({ stations, onEnable, onDisable, isMutating }: StationBoardViewProps) {
  const { t } = useTranslation();

  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {stations.map((station) => (
        <Card key={station.id} className="relative">
          <CardHeader className="pb-2">
            <div className="flex items-start justify-between">
              <div>
                <CardTitle className="text-lg">{station.name}</CardTitle>
                <p className="text-xs font-mono text-muted-foreground">{station.stationCode}</p>
                <div className="mt-1 flex items-center gap-1 text-sm text-muted-foreground">
                  <MapPin className="h-3 w-3" aria-hidden="true" />
                  <span className="line-clamp-1">{station.address}</span>
                </div>
              </div>
              {!station.isEnabled ? (
                <Badge variant="secondary">{t("stations.disabled")}</Badge>
              ) : (
                <StatusBadge type="station" value={station.status} />
              )}
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-2 text-center">
                <div className="rounded-md bg-muted p-2">
                  <div className="text-lg font-semibold tabular-nums">{station.connectorCount || 0}</div>
                  <div className="text-xs text-muted-foreground">{t("stations.connectors")}</div>
                </div>
                <div className="rounded-md bg-muted p-2">
                  <div className="text-xs font-medium">
                    {station.lastHeartbeat
                      ? formatDateTime(station.lastHeartbeat)
                      : t("stations.never")}
                  </div>
                  <div className="text-xs text-muted-foreground">{t("stations.lastHeartbeat")}</div>
                </div>
              </div>

              <div className="flex items-center gap-2">
                <Link href={`/stations/${station.id}`} className="flex-1">
                  <Button variant="outline" size="sm" className="w-full">
                    <Eye className="mr-2 h-4 w-4" aria-hidden="true" />
                    {t("stations.view")}
                  </Button>
                </Link>
                <Link href={`/stations/${station.id}/edit`}>
                  <Button variant="outline" size="icon" aria-label={t("common.edit")}>
                    <Edit className="h-4 w-4" />
                  </Button>
                </Link>
                {station.isEnabled ? (
                  <Button
                    variant="outline"
                    size="icon"
                    aria-label={t("stations.disable")}
                    onClick={() => onDisable(station.id)}
                    disabled={isMutating}
                  >
                    <PowerOff className="h-4 w-4" />
                  </Button>
                ) : (
                  <Button
                    variant="outline"
                    size="icon"
                    aria-label={t("stations.enable")}
                    onClick={() => onEnable(station.id)}
                    disabled={isMutating}
                  >
                    <Power className="h-4 w-4" />
                  </Button>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
