"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { SkeletonTable } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import {
  Dialog,
  DialogHeader,
  DialogContent,
  DialogFooter,
} from "@/components/ui/dialog";
import { api } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import { AlertCircle, MessageSquare, Send } from "lucide-react";

interface Feedback {
  id: string;
  userId: string;
  userName?: string;
  type: number;
  subject: string;
  message?: string;
  status: number;
  adminResponse?: string;
  respondedAt?: string;
  createdAt: string;
}

const FeedbackTypeBadgeVariants: Record<number, "warning" | "default" | "success" | "secondary"> = {
  0: "warning",
  1: "default",
  2: "success",
  3: "secondary",
};

export default function FeedbackPage() {
  const hasAccess = useRequirePermission("KLC.Feedback");
  const queryClient = useQueryClient();
  const { t } = useTranslation();
  const [statusFilter, setStatusFilter] = useState<string>("all");

  const FeedbackTypeLabels: Record<number, string> = {
    0: t("feedback.typeBug"),
    1: t("feedback.typeFeatureRequest"),
    2: t("feedback.typeChargingIssue"),
    3: t("feedback.typePaymentIssue"),
    4: t("feedback.typeGeneral"),
  };

  const FeedbackStatusLabels: Record<number, string> = {
    0: t("feedback.statusOpen"),
    1: t("feedback.statusInReview"),
    2: t("feedback.statusResolved"),
    3: t("feedback.statusClosed"),
  };

  function getStatusBadge(status: number) {
    const label = FeedbackStatusLabels[status] || t("feedback.typeOther");
    const variant = FeedbackTypeBadgeVariants[status] ?? "secondary";
    return <Badge variant={variant}>{label}</Badge>;
  }
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [responseText, setResponseText] = useState("");
  const [responseStatus, setResponseStatus] = useState<number>(2);
  const [formError, setFormError] = useState("");

  // Fetch feedback list
  const { data: feedbackList, isLoading } = useQuery<Feedback[]>({
    queryKey: ["feedback", statusFilter],
    queryFn: async () => {
      const params: Record<string, unknown> = { pageSize: 20 };
      if (statusFilter !== "all") params.status = statusFilter;
      const res = await api.get("/admin/feedback", { params });
      return res.data.data || [];
    },
  });

  // Fetch single feedback detail
  const { data: selectedFeedback } = useQuery<Feedback>({
    queryKey: ["feedback", selectedId],
    queryFn: async () => {
      const res = await api.get(`/admin/feedback/${selectedId}`);
      return res.data;
    },
    enabled: !!selectedId,
  });

  // Respond to feedback
  const respondMutation = useMutation({
    mutationFn: async ({
      id,
      data,
    }: {
      id: string;
      data: { status: number; adminResponse: string };
    }) => {
      // Status 3 = Closed: call close endpoint, otherwise respond
      if (data.status === 3) {
        await api.put(`/admin/feedback/${id}/close`);
      } else {
        await api.put(`/admin/feedback/${id}/respond`, {
          response: data.adminResponse,
        });
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["feedback"] });
      setSelectedId(null);
      setResponseText("");
      setFormError("");
    },
    onError: (err: unknown) => {
      if (err && typeof err === "object" && "response" in err) {
        const axiosError = err as {
          response?: {
            data?: {
              error?: {
                message?: string;
                details?: string;
                validationErrors?: Array<{ message: string }>;
              };
            };
          };
        };
        const apiError = axiosError.response?.data?.error;
        if (apiError?.validationErrors?.length) {
          setFormError(
            apiError.validationErrors.map((e) => e.message).join(". ")
          );
        } else if (apiError?.details) {
          setFormError(apiError.details);
        } else if (apiError?.message) {
          setFormError(apiError.message);
        } else {
          setFormError(t("feedback.sendFailed"));
        }
      } else {
        setFormError(t("feedback.connectionFailed"));
      }
    },
  });

  const handleRespond = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedId) return;
    respondMutation.mutate({
      id: selectedId,
      data: { status: responseStatus, adminResponse: responseText },
    });
  };

  const handleSelect = (feedback: Feedback) => {
    setSelectedId(feedback.id);
    setResponseText(feedback.adminResponse || "");
    setResponseStatus(feedback.status >= 2 ? feedback.status : 2);
    setFormError("");
  };

  const handleClose = () => {
    setSelectedId(null);
    setResponseText("");
    setFormError("");
  };

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-30 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="p-6 pb-4">
          <PageHeader
            title={t("feedback.title")}
            description={t("feedback.description")}
          />
        </div>

        {/* Status Filter */}
        <div className="flex items-center gap-2 px-6 pb-4">
          <Button
            variant={statusFilter === "all" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("all")}
          >
            {t("common.all")}
          </Button>
          <Button
            variant={statusFilter === "0" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("0")}
          >
            {t("feedback.statusOpen")}
          </Button>
          <Button
            variant={statusFilter === "1" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("1")}
          >
            {t("feedback.statusInReview")}
          </Button>
          <Button
            variant={statusFilter === "2" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("2")}
          >
            {t("feedback.statusResolved")}
          </Button>
          <Button
            variant={statusFilter === "3" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("3")}
          >
            {t("feedback.statusClosed")}
          </Button>
        </div>
      </div>

      <div className="flex-1 space-y-6 p-6">
        {/* Feedback Table */}
        {isLoading ? (
          <SkeletonTable rows={8} cols={6} />
        ) : feedbackList && feedbackList.length > 0 ? (
          <Card>
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        {t("feedback.tableType")}
                      </th>
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        {t("feedback.tableSubject")}
                      </th>
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        {t("feedback.tableUser")}
                      </th>
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        {t("feedback.tableStatus")}
                      </th>
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        {t("feedback.tableCreatedAt")}
                      </th>
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        {t("feedback.tableActions")}
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {feedbackList.map((feedback) => (
                      <tr
                        key={feedback.id}
                        className={`border-b hover:bg-muted/50 cursor-pointer ${
                          selectedId === feedback.id ? "bg-muted/50" : ""
                        }`}
                        onClick={() => handleSelect(feedback)}
                      >
                        <td className="px-4 py-3 text-sm">
                          {FeedbackTypeLabels[feedback.type] || t("feedback.typeOther")}
                        </td>
                        <td className="px-4 py-3">
                          <p className="font-medium">{feedback.subject}</p>
                        </td>
                        <td className="px-4 py-3 text-sm text-muted-foreground">
                          {feedback.userName || feedback.userId.substring(0, 8) + "..."}
                        </td>
                        <td className="px-4 py-3">
                          {getStatusBadge(feedback.status)}
                        </td>
                        <td className="px-4 py-3 text-sm">
                          {new Date(feedback.createdAt).toLocaleDateString(
                            "vi-VN"
                          )}
                        </td>
                        <td className="px-4 py-3">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleSelect(feedback);
                            }}
                          >
                            <MessageSquare className="h-4 w-4" />
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        ) : (
          <EmptyState
            icon={MessageSquare}
            title={t("feedback.noFeedback")}
            description={t("feedback.noFeedbackDescription")}
          />
        )}
      </div>

      {/* Detail / Respond Dialog */}
      <Dialog
        open={!!selectedId && !!selectedFeedback}
        onClose={handleClose}
        size="lg"
      >
        <DialogHeader onClose={handleClose}>
          <div className="flex items-center gap-2">
            <MessageSquare className="h-5 w-5" />
            {t("feedback.feedbackDetail")}
          </div>
        </DialogHeader>
        <DialogContent className="space-y-4 max-h-[60vh] overflow-y-auto">
          {selectedFeedback && (
            <>
              {/* Feedback Info */}
              <div className="grid gap-4 md:grid-cols-3">
                <div>
                  <p className="text-sm text-muted-foreground">{t("feedback.tableType")}</p>
                  <p className="font-medium">
                    {FeedbackTypeLabels[selectedFeedback.type] || t("feedback.typeOther")}
                  </p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t("feedback.tableStatus")}</p>
                  <div className="mt-1">
                    {getStatusBadge(selectedFeedback.status)}
                  </div>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{t("feedback.tableCreatedAt")}</p>
                  <p className="font-medium">
                    {new Date(selectedFeedback.createdAt).toLocaleDateString(
                      "vi-VN"
                    )}
                  </p>
                </div>
              </div>

              {/* Subject & Message */}
              <div>
                <p className="text-sm text-muted-foreground">{t("feedback.tableSubject")}</p>
                <p className="font-medium">{selectedFeedback.subject}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">{t("feedback.content")}</p>
                <div className="mt-1 rounded-md border bg-muted/30 p-3 text-sm">
                  {selectedFeedback.message}
                </div>
              </div>

              {/* Existing Admin Response */}
              {selectedFeedback.adminResponse && (
                <div>
                  <p className="text-sm text-muted-foreground">
                    {t("feedback.previousResponse")}
                  </p>
                  <div className="mt-1 rounded-md border border-primary/20 bg-primary/5 p-3 text-sm">
                    {selectedFeedback.adminResponse}
                  </div>
                </div>
              )}

              {/* Response Form */}
              <form
                id="feedback-response-form"
                onSubmit={handleRespond}
                className="space-y-4 border-t pt-4"
              >
                <h3 className="text-sm font-semibold">{t("feedback.adminResponse")}</h3>

                {formError && (
                  <div className="flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                    <AlertCircle className="h-4 w-4 flex-shrink-0" />
                    <span>{formError}</span>
                  </div>
                )}

                <div>
                  <label className="mb-1 block text-sm font-medium">
                    {t("feedback.updateStatus")}
                  </label>
                  <select
                    value={responseStatus}
                    onChange={(e) =>
                      setResponseStatus(parseInt(e.target.value, 10))
                    }
                    className="h-10 w-full max-w-xs rounded-md border bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    <option value={0}>{t("feedback.statusOpen")}</option>
                    <option value={1}>{t("feedback.statusInReview")}</option>
                    <option value={2}>{t("feedback.statusResolved")}</option>
                    <option value={3}>{t("feedback.statusClosed")}</option>
                  </select>
                </div>

                <div>
                  <label className="mb-1 block text-sm font-medium">
                    {t("feedback.responseContent")}
                  </label>
                  <textarea
                    value={responseText}
                    onChange={(e) => setResponseText(e.target.value)}
                    placeholder={t("feedback.responsePlaceholder")}
                    rows={4}
                    className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                </div>
              </form>
            </>
          )}
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={handleClose}>
            {t("common.cancel")}
          </Button>
          <Button
            type="submit"
            form="feedback-response-form"
            disabled={respondMutation.isPending}
          >
            <Send className="mr-2 h-4 w-4" />
            {respondMutation.isPending ? t("feedback.sending") : t("feedback.sendResponse")}
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  );
}
