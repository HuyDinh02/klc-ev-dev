import { useRouter } from "next/navigation";
import Link from "next/link";
import { Eye, Edit, Power, PowerOff, ChevronUp, ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { StatusBadge } from "@/components/ui/status-badge";
import { Card, CardContent } from "@/components/ui/card";
import { formatDateTime } from "@/lib/utils";
import { useTranslation } from "@/lib/i18n";
import type { StationListItem } from "./types";

interface StationListViewProps {
  stations: StationListItem[];
  sortBy: string;
  sortOrder: "asc" | "desc";
  onSort: (field: string) => void;
  onEnable: (id: string) => void;
  onDisable: (id: string) => void;
  isMutating: boolean;
}

function SortIndicator({ field, sortBy, sortOrder }: { field: string; sortBy: string; sortOrder: "asc" | "desc" }) {
  if (sortBy !== field) return null;
  return sortOrder === "asc" ? (
    <ChevronUp className="ml-1 inline h-3 w-3" />
  ) : (
    <ChevronDown className="ml-1 inline h-3 w-3" />
  );
}

export function StationListView({ stations, sortBy, sortOrder, onSort, onEnable, onDisable, isMutating }: StationListViewProps) {
  const { t } = useTranslation();
  const router = useRouter();

  const sortableHeader = (field: string, label: string, className?: string) => (
    <th
      className={`px-4 py-3 text-left text-sm font-medium cursor-pointer select-none hover:text-foreground ${className || ""}`}
      onClick={() => onSort(field)}
    >
      {label}
      <SortIndicator field={field} sortBy={sortBy} sortOrder={sortOrder} />
    </th>
  );

  return (
    <Card>
      <CardContent className="p-0">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b bg-muted/50">
                {sortableHeader("stationCode", t("stations.code"))}
                {sortableHeader("name", t("stations.nameLabel"))}
                <th className="px-4 py-3 text-left text-sm font-medium">{t("stations.address")}</th>
                {sortableHeader("status", t("common.status"))}
                <th className="px-4 py-3 text-center text-sm font-medium">{t("stations.connectors")}</th>
                <th className="px-4 py-3 text-left text-sm font-medium">{t("stations.lastHeartbeat")}</th>
                <th className="px-4 py-3 text-right text-sm font-medium">{t("common.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {stations.map((station) => (
                <tr
                  key={station.id}
                  className="border-b hover:bg-muted/50 cursor-pointer"
                  onClick={() => router.push(`/stations/${station.id}`)}
                >
                  <td className="px-4 py-3 font-mono text-sm">{station.stationCode}</td>
                  <td className="px-4 py-3 font-medium">{station.name}</td>
                  <td className="px-4 py-3 text-sm max-w-xs truncate">{station.address}</td>
                  <td className="px-4 py-3">
                    {!station.isEnabled ? (
                      <Badge variant="secondary">{t("stations.disabled")}</Badge>
                    ) : (
                      <StatusBadge type="station" value={station.status} />
                    )}
                  </td>
                  <td className="px-4 py-3 text-center tabular-nums">{station.connectorCount || 0}</td>
                  <td className="px-4 py-3 text-sm">
                    {station.lastHeartbeat ? formatDateTime(station.lastHeartbeat) : t("stations.never")}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-1" onClick={(e) => e.stopPropagation()}>
                      <Link href={`/stations/${station.id}`}>
                        <Button variant="ghost" size="icon" aria-label={t("stations.view")}>
                          <Eye className="h-4 w-4" />
                        </Button>
                      </Link>
                      <Link href={`/stations/${station.id}/edit`}>
                        <Button variant="ghost" size="icon" aria-label={t("common.edit")}>
                          <Edit className="h-4 w-4" />
                        </Button>
                      </Link>
                      {station.isEnabled ? (
                        <Button
                          variant="ghost"
                          size="icon"
                          aria-label={t("stations.disable")}
                          onClick={() => onDisable(station.id)}
                          disabled={isMutating}
                        >
                          <PowerOff className="h-4 w-4" />
                        </Button>
                      ) : (
                        <Button
                          variant="ghost"
                          size="icon"
                          aria-label={t("stations.enable")}
                          onClick={() => onEnable(station.id)}
                          disabled={isMutating}
                        >
                          <Power className="h-4 w-4" />
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}
