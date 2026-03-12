"use client";

import { useParams, useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  AlertTriangle,
  CheckCircle,
  Clock,
  Wrench,
  MapPin,
  Plug,
  Hash,
} from "lucide-react";
import { Header } from "@/components/layout/header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { StatusBadge } from "@/components/ui/status-badge";
import { Skeleton, SkeletonCard } from "@/components/ui/skeleton";
import { faultsApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { formatDateTime } from "@/lib/utils";

interface FaultDetail {
  id: string;
  stationId: string;
  stationName?: string;
  connectorNumber?: number;
  errorCode: string;
  errorInfo?: string;
  status: number;
  priority?: number;
  detectedAt?: string;
  resolvedAt?: string;
  resolutionNotes?: string;
  ocppVendorId?: string;
  ocppVendorErrorCode?: string;
  creationTime?: string;
  lastModificationTime?: string;
}

export default function FaultDetailPage() {
  const params = useParams();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { t } = useTranslation();
  const faultId = params.id as string;

  const { data: fault, isLoading } = useQuery({
    queryKey: ["fault", faultId],
    queryFn: async () => {
      const { data } = await faultsApi.getById(faultId);
      return data as FaultDetail;
    },
    enabled: !!faultId,
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ status, notes }: { status: number; notes?: string }) =>
      faultsApi.updateStatus(faultId, status, notes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["fault", faultId] });
      queryClient.invalidateQueries({ queryKey: ["faults"] });
    },
  });

  if (isLoading) {
    return (
      <div className="flex flex-col">
        <Header title={t("faults.detailTitle")} description={t("faults.loadingFault")} />
        <div className="flex-1 space-y-6 p-6">
          <Skeleton className="h-9 w-36" />
          <div className="grid gap-6 md:grid-cols-2">
            <SkeletonCard />
            <SkeletonCard />
          </div>
        </div>
      </div>
    );
  }

  if (!fault) {
    return (
      <div className="flex flex-col">
        <Header title={t("faults.detailTitle")} description={t("faults.faultNotFound")} />
        <div className="flex-1 p-6">
          <Button variant="outline" onClick={() => router.push("/faults")}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            {t("faults.backToFaults")}
          </Button>
          <div className="flex items-center justify-center py-20 text-muted-foreground">
            {t("faults.faultNotFound")}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col">
      <Header
        title={t("faults.detailTitle")}
        description={`${fault.errorCode} — ${fault.stationName || t("faults.unknownStation")}`}
      />

      <div className="flex-1 space-y-6 p-6">
        <div className="flex items-center justify-between">
          <Button variant="outline" onClick={() => router.push("/faults")}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            {t("faults.backToFaults")}
          </Button>

          <div className="flex gap-2">
            {fault.status === 0 && (
              <Button
                variant="outline"
                onClick={() => updateStatusMutation.mutate({ status: 1 })}
                disabled={updateStatusMutation.isPending}
              >
                <Wrench className="mr-2 h-4 w-4" />
                {t("faults.startInvestigation")}
              </Button>
            )}
            {fault.status === 1 && (
              <Button
                onClick={() => updateStatusMutation.mutate({ status: 2 })}
                disabled={updateStatusMutation.isPending}
              >
                <CheckCircle className="mr-2 h-4 w-4" />
                {t("faults.markResolved")}
              </Button>
            )}
            {fault.status === 2 && (
              <Button
                variant="secondary"
                onClick={() => updateStatusMutation.mutate({ status: 3 })}
                disabled={updateStatusMutation.isPending}
              >
                {t("faults.closeFault")}
              </Button>
            )}
          </div>
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          {/* Fault Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <AlertTriangle className="h-5 w-5" />
                {t("faults.faultInfo")}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Hash className="h-4 w-4" />
                    {t("faults.errorCode")}
                  </span>
                  <span className="font-mono font-semibold">{fault.errorCode}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">{t("common.status")}</span>
                  <StatusBadge type="faultStatus" value={fault.status} />
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">{t("faults.priority")}</span>
                  <StatusBadge type="faultSeverity" value={fault.priority ?? 4} />
                </div>
                {fault.errorInfo && (
                  <div>
                    <span className="text-sm text-muted-foreground">{t("faults.errorInfo")}</span>
                    <p className="mt-1 text-sm">{fault.errorInfo}</p>
                  </div>
                )}
                {fault.ocppVendorId && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("faults.vendorId")}</span>
                    <span className="font-mono text-sm">{fault.ocppVendorId}</span>
                  </div>
                )}
                {fault.ocppVendorErrorCode && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("faults.vendorErrorCode")}</span>
                    <span className="font-mono text-sm">{fault.ocppVendorErrorCode}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Location & Timeline */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <Clock className="h-5 w-5" />
                {t("faults.locationTimeline")}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {fault.stationName && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                      <MapPin className="h-4 w-4" />
                      {t("faults.station")}
                    </span>
                    <Button
                      variant="link"
                      className="h-auto p-0"
                      onClick={() => router.push(`/stations/${fault.stationId}`)}
                    >
                      {fault.stationName}
                    </Button>
                  </div>
                )}
                {fault.connectorNumber != null && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                      <Plug className="h-4 w-4" />
                      {t("faults.connector")}
                    </span>
                    <span className="font-medium">#{fault.connectorNumber}</span>
                  </div>
                )}
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">{t("faults.detected")}</span>
                  <span className="text-sm">{formatDateTime(fault.detectedAt || fault.creationTime)}</span>
                </div>
                {fault.resolvedAt && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("faults.resolved")}</span>
                    <span className="text-sm">{formatDateTime(fault.resolvedAt)}</span>
                  </div>
                )}
                {fault.lastModificationTime && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("faults.lastUpdated")}</span>
                    <span className="text-sm">{formatDateTime(fault.lastModificationTime)}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Resolution Notes */}
        {fault.resolutionNotes && (
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">{t("faults.resolutionNotes")}</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm">{fault.resolutionNotes}</p>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
