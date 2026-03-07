"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { api } from "@/lib/api";
import { AlertCircle, MessageSquare, Send, X } from "lucide-react";

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
  1: "Yêu cầu tính năng",
  2: "Lỗi sạc",
  3: "Lỗi thanh toán",
  4: "Chung",
};

const FeedbackStatusLabels: Record<number, string> = {
  0: "Mở",
  1: "Đang xem xét",
  2: "Đã giải quyết",
  3: "Đã đóng",
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
          setFormError("Không thể gửi phản hồi. Vui lòng thử lại.");
        }
      } else {
        setFormError("Không thể kết nối đến máy chủ. Vui lòng thử lại.");
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
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Quản lý phản hồi</h1>
          <p className="text-muted-foreground">
            Xem và phản hồi ý kiến từ người dùng
          </p>
        </div>
      </div>

      {/* Status Filter */}
      <div className="flex items-center gap-2">
        <Button
          variant={statusFilter === "all" ? "default" : "outline"}
          size="sm"
          onClick={() => setStatusFilter("all")}
        >
          Tất cả
        </Button>
        <Button
          variant={statusFilter === "0" ? "default" : "outline"}
          size="sm"
          onClick={() => setStatusFilter("0")}
        >
          Mở
        </Button>
        <Button
          variant={statusFilter === "1" ? "default" : "outline"}
          size="sm"
          onClick={() => setStatusFilter("1")}
        >
          Đang xem xét
        </Button>
        <Button
          variant={statusFilter === "2" ? "default" : "outline"}
          size="sm"
          onClick={() => setStatusFilter("2")}
        >
          Đã giải quyết
        </Button>
        <Button
          variant={statusFilter === "3" ? "default" : "outline"}
          size="sm"
          onClick={() => setStatusFilter("3")}
        >
          Đã đóng
        </Button>
      </div>

      {/* Feedback Table */}
      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Loại
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Tiêu đề
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Người dùng
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Trạng thái
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Ngày tạo
                  </th>
                  <th className="px-4 py-3 text-left text-sm font-medium">
                    Thao tác
                  </th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  <tr>
                    <td colSpan={6} className="px-4 py-8 text-center">
                      Đang tải...
                    </td>
                  </tr>
                ) : feedbackList && feedbackList.length > 0 ? (
                  feedbackList.map((feedback) => (
                    <tr
                      key={feedback.id}
                      className={`border-b hover:bg-muted/50 cursor-pointer ${
                        selectedId === feedback.id ? "bg-muted/50" : ""
                      }`}
                      onClick={() => handleSelect(feedback)}
                    >
                      <td className="px-4 py-3 text-sm">
                        {FeedbackTypeLabels[feedback.type] || "Khác"}
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
                  ))
                ) : (
                  <tr>
                    <td
                      colSpan={6}
                      className="px-4 py-8 text-center text-muted-foreground"
                    >
                      Không có phản hồi nào
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      {/* Detail / Respond Section */}
      {selectedId && selectedFeedback && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                <MessageSquare className="h-5 w-5" />
                Chi tiết phản hồi
              </CardTitle>
              <Button variant="ghost" size="icon" onClick={handleClose}>
                <X className="h-4 w-4" />
              </Button>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Feedback Info */}
            <div className="grid gap-4 md:grid-cols-3">
              <div>
                <p className="text-sm text-muted-foreground">Loại</p>
                <p className="font-medium">
                  {FeedbackTypeLabels[selectedFeedback.type] || "Khác"}
                </p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Trạng thái</p>
                <div className="mt-1">
                  {getStatusBadge(selectedFeedback.status)}
                </div>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Ngày tạo</p>
                <p className="font-medium">
                  {new Date(selectedFeedback.createdAt).toLocaleDateString(
                    "vi-VN"
                  )}
                </p>
              </div>
            </div>

            {/* Subject & Message */}
            <div>
              <p className="text-sm text-muted-foreground">Tiêu đề</p>
              <p className="font-medium">{selectedFeedback.subject}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Nội dung</p>
              <div className="mt-1 rounded-md border bg-muted/30 p-3 text-sm">
                {selectedFeedback.message}
              </div>
            </div>

            {/* Existing Admin Response */}
            {selectedFeedback.adminResponse && (
              <div>
                <p className="text-sm text-muted-foreground">
                  Phản hồi trước đó
                </p>
                <div className="mt-1 rounded-md border border-primary/20 bg-primary/5 p-3 text-sm">
                  {selectedFeedback.adminResponse}
                </div>
              </div>
            )}

            {/* Response Form */}
            <form onSubmit={handleRespond} className="space-y-4 border-t pt-4">
              <h3 className="text-sm font-semibold">Phản hồi từ quản trị</h3>

              {formError && (
                <div className="flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                  <AlertCircle className="h-4 w-4 flex-shrink-0" />
                  <span>{formError}</span>
                </div>
              )}

              <div>
                <label className="mb-1 block text-sm font-medium">
                  Cập nhật trạng thái
                </label>
                <select
                  value={responseStatus}
                  onChange={(e) =>
                    setResponseStatus(parseInt(e.target.value, 10))
                  }
                  className="h-10 w-full max-w-xs rounded-md border bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value={0}>Mở</option>
                  <option value={1}>Đang xem xét</option>
                  <option value={2}>Đã giải quyết</option>
                  <option value={3}>Đã đóng</option>
                </select>
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium">
                  Nội dung phản hồi
                </label>
                <textarea
                  value={responseText}
                  onChange={(e) => setResponseText(e.target.value)}
                  placeholder="Nhập phản hồi cho người dùng..."
                  rows={4}
                  className="w-full rounded-md border bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>

              <div className="flex gap-2">
                <Button
                  type="submit"
                  disabled={respondMutation.isPending}
                >
                  <Send className="mr-2 h-4 w-4" />
                  {respondMutation.isPending ? "Đang gửi..." : "Gửi phản hồi"}
                </Button>
                <Button type="button" variant="outline" onClick={handleClose}>
                  Hủy
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
