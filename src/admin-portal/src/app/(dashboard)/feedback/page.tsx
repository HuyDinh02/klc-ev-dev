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

const FeedbackTypeLabels: Record<number, string> = {
  0: "Bug",
  1: "Y\u00eau c\u1ea7u t\u00ednh n\u0103ng",
  2: "L\u1ed7i s\u1ea1c",
  3: "L\u1ed7i thanh to\u00e1n",
  4: "Chung",
};

const FeedbackStatusLabels: Record<number, string> = {
  0: "M\u1edf",
  1: "\u0110ang xem x\u00e9t",
  2: "\u0110\u00e3 gi\u1ea3i quy\u1ebft",
  3: "\u0110\u00e3 \u0111\u00f3ng",
};

function getStatusBadge(status: number) {
  const label = FeedbackStatusLabels[status] || "Unknown";
  switch (status) {
    case 0:
      return <Badge variant="warning">{label}</Badge>;
    case 1:
      return <Badge variant="default">{label}</Badge>;
    case 2:
      return <Badge variant="success">{label}</Badge>;
    case 3:
      return <Badge variant="secondary">{label}</Badge>;
    default:
      return <Badge variant="secondary">{label}</Badge>;
  }
}

export default function FeedbackPage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<string>("all");
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
          setFormError("Kh\u00f4ng th\u1ec3 g\u1eedi ph\u1ea3n h\u1ed3i. Vui l\u00f2ng th\u1eed l\u1ea1i.");
        }
      } else {
        setFormError("Kh\u00f4ng th\u1ec3 k\u1ebft n\u1ed1i \u0111\u1ebfn m\u00e1y ch\u1ee7. Vui l\u00f2ng th\u1eed l\u1ea1i.");
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

  return (
    <div className="flex flex-col">
      {/* Sticky Header */}
      <div className="sticky top-0 z-30 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="p-6 pb-4">
          <PageHeader
            title="Qu\u1ea3n l\u00fd ph\u1ea3n h\u1ed3i"
            description="Xem v\u00e0 ph\u1ea3n h\u1ed3i \u00fd ki\u1ebfn t\u1eeb ng\u01b0\u1eddi d\u00f9ng"
          />
        </div>

        {/* Status Filter */}
        <div className="flex items-center gap-2 px-6 pb-4">
          <Button
            variant={statusFilter === "all" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("all")}
          >
            T\u1ea5t c\u1ea3
          </Button>
          <Button
            variant={statusFilter === "0" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("0")}
          >
            M\u1edf
          </Button>
          <Button
            variant={statusFilter === "1" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("1")}
          >
            \u0110ang xem x\u00e9t
          </Button>
          <Button
            variant={statusFilter === "2" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("2")}
          >
            \u0110\u00e3 gi\u1ea3i quy\u1ebft
          </Button>
          <Button
            variant={statusFilter === "3" ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter("3")}
          >
            \u0110\u00e3 \u0111\u00f3ng
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
                        Lo\u1ea1i
                      </th>
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        Ti\u00eau \u0111\u1ec1
                      </th>
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        Ng\u01b0\u1eddi d\u00f9ng
                      </th>
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        Tr\u1ea1ng th\u00e1i
                      </th>
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        Ng\u00e0y t\u1ea1o
                      </th>
                      <th className="px-4 py-3 text-left text-sm font-medium">
                        Thao t\u00e1c
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
                          {FeedbackTypeLabels[feedback.type] || "Kh\u00e1c"}
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
            title="Kh\u00f4ng c\u00f3 ph\u1ea3n h\u1ed3i n\u00e0o"
            description="Ch\u01b0a c\u00f3 ph\u1ea3n h\u1ed3i n\u00e0o t\u1eeb ng\u01b0\u1eddi d\u00f9ng"
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
            Chi ti\u1ebft ph\u1ea3n h\u1ed3i
          </div>
        </DialogHeader>
        <DialogContent className="space-y-4 max-h-[60vh] overflow-y-auto">
          {selectedFeedback && (
            <>
              {/* Feedback Info */}
              <div className="grid gap-4 md:grid-cols-3">
                <div>
                  <p className="text-sm text-muted-foreground">Lo\u1ea1i</p>
                  <p className="font-medium">
                    {FeedbackTypeLabels[selectedFeedback.type] || "Kh\u00e1c"}
                  </p>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Tr\u1ea1ng th\u00e1i</p>
                  <div className="mt-1">
                    {getStatusBadge(selectedFeedback.status)}
                  </div>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">Ng\u00e0y t\u1ea1o</p>
                  <p className="font-medium">
                    {new Date(selectedFeedback.createdAt).toLocaleDateString(
                      "vi-VN"
                    )}
                  </p>
                </div>
              </div>

              {/* Subject & Message */}
              <div>
                <p className="text-sm text-muted-foreground">Ti\u00eau \u0111\u1ec1</p>
                <p className="font-medium">{selectedFeedback.subject}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">N\u1ed9i dung</p>
                <div className="mt-1 rounded-md border bg-muted/30 p-3 text-sm">
                  {selectedFeedback.message}
                </div>
              </div>

              {/* Existing Admin Response */}
              {selectedFeedback.adminResponse && (
                <div>
                  <p className="text-sm text-muted-foreground">
                    Ph\u1ea3n h\u1ed3i tr\u01b0\u1edbc \u0111\u00f3
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
                <h3 className="text-sm font-semibold">Ph\u1ea3n h\u1ed3i t\u1eeb qu\u1ea3n tr\u1ecb</h3>

                {formError && (
                  <div className="flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                    <AlertCircle className="h-4 w-4 flex-shrink-0" />
                    <span>{formError}</span>
                  </div>
                )}

                <div>
                  <label className="mb-1 block text-sm font-medium">
                    C\u1eadp nh\u1eadt tr\u1ea1ng th\u00e1i
                  </label>
                  <select
                    value={responseStatus}
                    onChange={(e) =>
                      setResponseStatus(parseInt(e.target.value, 10))
                    }
                    className="h-10 w-full max-w-xs rounded-md border bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    <option value={0}>M\u1edf</option>
                    <option value={1}>\u0110ang xem x\u00e9t</option>
                    <option value={2}>\u0110\u00e3 gi\u1ea3i quy\u1ebft</option>
                    <option value={3}>\u0110\u00e3 \u0111\u00f3ng</option>
                  </select>
                </div>

                <div>
                  <label className="mb-1 block text-sm font-medium">
                    N\u1ed9i dung ph\u1ea3n h\u1ed3i
                  </label>
                  <textarea
                    value={responseText}
                    onChange={(e) => setResponseText(e.target.value)}
                    placeholder="Nh\u1eadp ph\u1ea3n h\u1ed3i cho ng\u01b0\u1eddi d\u00f9ng..."
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
            H\u1ee7y
          </Button>
          <Button
            type="submit"
            form="feedback-response-form"
            disabled={respondMutation.isPending}
          >
            <Send className="mr-2 h-4 w-4" />
            {respondMutation.isPending ? "\u0110ang g\u1eedi..." : "G\u1eedi ph\u1ea3n h\u1ed3i"}
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  );
}
