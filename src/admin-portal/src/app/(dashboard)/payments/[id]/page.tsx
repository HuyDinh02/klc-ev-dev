"use client";

import { useParams, useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  CreditCard,
  DollarSign,
  MapPin,
  Zap,
  Clock,
  FileText,
  User,
  Hash,
} from "lucide-react";
import { Header } from "@/components/layout/header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { StatusBadge } from "@/components/ui/status-badge";
import { Skeleton, SkeletonCard } from "@/components/ui/skeleton";
import { paymentsApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { formatCurrency, formatDateTime } from "@/lib/utils";

const PaymentGatewayLabels: Record<number, string> = {
  0: "ZaloPay",
  1: "MoMo",
  2: "OnePay",
  3: "Wallet",
  4: "VnPay",
  5: "QR Payment",
  6: "Voucher",
  7: "Urbox",
};

interface PaymentDetail {
  id: string;
  sessionId?: string;
  userId?: string;
  userName?: string;
  amount: number;
  status: number;
  gateway: number;
  referenceCode?: string;
  gatewayTransactionId?: string;
  stationName?: string;
  connectorNumber?: number;
  energyDeliveredKwh?: number;
  invoiceId?: string;
  invoiceNumber?: string;
  refundedAt?: string;
  refundReason?: string;
  creationTime?: string;
  lastModificationTime?: string;
}

export default function PaymentDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { t } = useTranslation();
  const paymentId = params.id as string;

  const { data: payment, isLoading } = useQuery({
    queryKey: ["payment", paymentId],
    queryFn: async () => {
      const { data } = await paymentsApi.getById(paymentId);
      return data as PaymentDetail;
    },
    enabled: !!paymentId,
  });

  if (isLoading) {
    return (
      <div className="flex flex-col">
        <Header title={t("payments.detailTitle")} description={t("payments.loadingPayment")} />
        <div className="flex-1 space-y-6 p-6">
          <Skeleton className="h-9 w-40" />
          <div className="grid gap-6 md:grid-cols-2">
            <SkeletonCard />
            <SkeletonCard />
          </div>
        </div>
      </div>
    );
  }

  if (!payment) {
    return (
      <div className="flex flex-col">
        <Header title={t("payments.detailTitle")} description={t("payments.paymentNotFound")} />
        <div className="flex-1 p-6">
          <Button variant="outline" onClick={() => router.push("/payments")}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            {t("payments.backToPayments")}
          </Button>
          <div className="flex items-center justify-center py-20 text-muted-foreground">
            {t("payments.paymentNotFound")}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col">
      <Header
        title={t("payments.detailTitle")}
        description={`${t("payments.transactionPrefix")} ${payment.referenceCode || payment.id.slice(0, 8)}`}
      />

      <div className="flex-1 space-y-6 p-6">
        <Button variant="outline" onClick={() => router.push("/payments")}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          {t("payments.backToPayments")}
        </Button>

        <div className="grid gap-6 md:grid-cols-2">
          {/* Transaction Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <CreditCard className="h-5 w-5" />
                {t("payments.transactionInfo")}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Hash className="h-4 w-4" />
                    {t("payments.reference")}
                  </span>
                  <span className="font-mono font-medium">
                    {payment.referenceCode || payment.id.slice(0, 12)}
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <DollarSign className="h-4 w-4" />
                    {t("payments.amount")}
                  </span>
                  <span className="text-xl font-bold text-primary">
                    {formatCurrency(payment.amount)}
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">{t("common.status")}</span>
                  <StatusBadge type="payment" value={payment.status} />
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">{t("payments.paymentMethod")}</span>
                  <span className="font-medium">
                    {PaymentGatewayLabels[payment.gateway] || t("payments.unknown")}
                  </span>
                </div>
                {payment.gatewayTransactionId && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("payments.gatewayTxId")}</span>
                    <span className="font-mono text-sm">{payment.gatewayTransactionId}</span>
                  </div>
                )}
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Clock className="h-4 w-4" />
                    {t("payments.created")}
                  </span>
                  <span className="text-sm">{formatDateTime(payment.creationTime)}</span>
                </div>
                {payment.lastModificationTime && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("payments.lastUpdated")}</span>
                    <span className="text-sm">{formatDateTime(payment.lastModificationTime)}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Session & Station Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <Zap className="h-5 w-5" />
                {t("payments.sessionAndStation")}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {payment.userName && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                      <User className="h-4 w-4" />
                      {t("payments.user")}
                    </span>
                    <span className="font-medium">{payment.userName}</span>
                  </div>
                )}
                {payment.stationName && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                      <MapPin className="h-4 w-4" />
                      {t("payments.station")}
                    </span>
                    <span className="font-medium">{payment.stationName}</span>
                  </div>
                )}
                {payment.connectorNumber != null && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("payments.connector")}</span>
                    <span className="font-medium">#{payment.connectorNumber}</span>
                  </div>
                )}
                {payment.sessionId && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("payments.sessionId")}</span>
                    <Button
                      variant="link"
                      className="h-auto p-0 font-mono text-sm"
                      onClick={() => router.push(`/sessions/${payment.sessionId}`)}
                    >
                      {payment.sessionId.slice(0, 12)}...
                    </Button>
                  </div>
                )}
                {payment.energyDeliveredKwh != null && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("payments.energyDelivered")}</span>
                    <span className="font-medium">{payment.energyDeliveredKwh.toFixed(2)} kWh</span>
                  </div>
                )}
                {payment.invoiceNumber && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                      <FileText className="h-4 w-4" />
                      {t("payments.invoice")}
                    </span>
                    <span className="font-mono text-sm">{payment.invoiceNumber}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Refund Info */}
        {payment.status === 4 && (
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">{t("payments.refundDetails")}</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {payment.refundedAt && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t("payments.refundedAt")}</span>
                    <span className="text-sm">{formatDateTime(payment.refundedAt)}</span>
                  </div>
                )}
                {payment.refundReason && (
                  <div>
                    <span className="text-sm text-muted-foreground">{t("payments.reason")}</span>
                    <p className="mt-1 text-sm">{payment.refundReason}</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
