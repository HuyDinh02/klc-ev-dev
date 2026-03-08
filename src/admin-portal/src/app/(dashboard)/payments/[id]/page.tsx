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
import { Badge } from "@/components/ui/badge";
import { paymentsApi } from "@/lib/api";
import { formatCurrency, formatDateTime } from "@/lib/utils";

const PaymentStatusLabels: Record<number, string> = {
  0: "Pending",
  1: "Processing",
  2: "Completed",
  3: "Failed",
  4: "Refunded",
  5: "Cancelled",
};

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

function getStatusBadge(status: number) {
  const label = PaymentStatusLabels[status] || "Unknown";
  switch (status) {
    case 2:
      return <Badge variant="success">{label}</Badge>;
    case 0:
      return <Badge variant="warning">{label}</Badge>;
    case 1:
      return <Badge variant="default">{label}</Badge>;
    case 3:
      return <Badge variant="destructive">{label}</Badge>;
    case 4:
      return <Badge variant="secondary">{label}</Badge>;
    case 5:
      return <Badge variant="secondary">{label}</Badge>;
    default:
      return <Badge variant="secondary">{label}</Badge>;
  }
}

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
        <Header title="Payment Detail" description="Loading payment data..." />
        <div className="flex-1 p-6">
          <div className="flex items-center justify-center py-20 text-muted-foreground">
            Loading...
          </div>
        </div>
      </div>
    );
  }

  if (!payment) {
    return (
      <div className="flex flex-col">
        <Header title="Payment Detail" description="Payment not found" />
        <div className="flex-1 p-6">
          <Button variant="outline" onClick={() => router.push("/payments")}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Payments
          </Button>
          <div className="flex items-center justify-center py-20 text-muted-foreground">
            Payment not found
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col">
      <Header
        title="Payment Detail"
        description={`Transaction ${payment.referenceCode || payment.id.slice(0, 8)}`}
      />

      <div className="flex-1 space-y-6 p-6">
        <Button variant="outline" onClick={() => router.push("/payments")}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to Payments
        </Button>

        <div className="grid gap-6 md:grid-cols-2">
          {/* Transaction Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <CreditCard className="h-5 w-5" />
                Transaction Info
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Hash className="h-4 w-4" />
                    Reference
                  </span>
                  <span className="font-mono font-medium">
                    {payment.referenceCode || payment.id.slice(0, 12)}
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <DollarSign className="h-4 w-4" />
                    Amount
                  </span>
                  <span className="text-xl font-bold text-primary">
                    {formatCurrency(payment.amount)}
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Status</span>
                  {getStatusBadge(payment.status)}
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Payment Method</span>
                  <span className="font-medium">
                    {PaymentGatewayLabels[payment.gateway] || "Unknown"}
                  </span>
                </div>
                {payment.gatewayTransactionId && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Gateway TX ID</span>
                    <span className="font-mono text-sm">{payment.gatewayTransactionId}</span>
                  </div>
                )}
                <div className="flex items-center justify-between">
                  <span className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Clock className="h-4 w-4" />
                    Created
                  </span>
                  <span className="text-sm">{formatDateTime(payment.creationTime)}</span>
                </div>
                {payment.lastModificationTime && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Last Updated</span>
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
                Session & Station
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {payment.userName && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                      <User className="h-4 w-4" />
                      User
                    </span>
                    <span className="font-medium">{payment.userName}</span>
                  </div>
                )}
                {payment.stationName && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                      <MapPin className="h-4 w-4" />
                      Station
                    </span>
                    <span className="font-medium">{payment.stationName}</span>
                  </div>
                )}
                {payment.connectorNumber != null && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Connector</span>
                    <span className="font-medium">#{payment.connectorNumber}</span>
                  </div>
                )}
                {payment.sessionId && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Session ID</span>
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
                    <span className="text-sm text-muted-foreground">Energy Delivered</span>
                    <span className="font-medium">{payment.energyDeliveredKwh.toFixed(2)} kWh</span>
                  </div>
                )}
                {payment.invoiceNumber && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-2 text-sm text-muted-foreground">
                      <FileText className="h-4 w-4" />
                      Invoice
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
              <CardTitle className="text-lg">Refund Details</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {payment.refundedAt && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Refunded At</span>
                    <span className="text-sm">{formatDateTime(payment.refundedAt)}</span>
                  </div>
                )}
                {payment.refundReason && (
                  <div>
                    <span className="text-sm text-muted-foreground">Reason</span>
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
