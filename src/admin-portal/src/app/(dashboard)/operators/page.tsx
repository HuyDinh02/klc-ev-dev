"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page-header";
import { StatCard } from "@/components/ui/stat-card";
import { EmptyState } from "@/components/ui/empty-state";
import { SkeletonCard } from "@/components/ui/skeleton";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { operatorsApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";
import {
  Plus,
  Trash2,
  Building2,
  MapPin,
  Shield,
  X,
  Copy,
  RefreshCw,
  Pencil,
  Mail,
  Globe,
  Gauge,
} from "lucide-react";

// --- Types ---

interface OperatorListItem {
  id: string;
  name: string;
  contactEmail: string;
  isActive: boolean;
  stationCount: number;
  rateLimitPerMinute: number;
  description?: string;
  creationTime: string;
}

interface OperatorDetail {
  id: string;
  name: string;
  contactEmail: string;
  webhookUrl?: string;
  description?: string;
  rateLimitPerMinute: number;
  isActive: boolean;
  apiKeyPrefix?: string;
  stations: { id: string; stationCode: string; name: string }[];
  creationTime: string;
}

export default function OperatorsPage() {
  const hasAccess = useRequirePermission("KLC.Operators");
  const canCreate = useHasPermission("KLC.Operators.Create");
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [showApiKey, setShowApiKey] = useState<string | null>(null);
  const [showEdit, setShowEdit] = useState(false);
  const [showRegenConfirm, setShowRegenConfirm] = useState(false);
  const [selectedOperator, setSelectedOperator] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", contactEmail: "", description: "" });
  const [editForm, setEditForm] = useState({
    name: "",
    contactEmail: "",
    webhookUrl: "",
    description: "",
    rateLimitPerMinute: 100,
  });

  const { data: operators, isLoading } = useQuery({
    queryKey: ["operators"],
    queryFn: async () => {
      const res = await operatorsApi.getList({ pageSize: 50 });
      return res.data as OperatorListItem[];
    },
  });

  const { data: detail } = useQuery({
    queryKey: ["operator-detail", selectedOperator],
    queryFn: async () => {
      if (!selectedOperator) return null;
      const res = await operatorsApi.get(selectedOperator);
      return res.data as OperatorDetail;
    },
    enabled: !!selectedOperator,
  });

  const createMutation = useMutation({
    mutationFn: () => operatorsApi.create(form),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ["operators"] });
      setShowCreate(false);
      setForm({ name: "", contactEmail: "", description: "" });
      const apiKey = res.data?.apiKey;
      if (apiKey) {
        setShowApiKey(apiKey);
      }
    },
  });

  const updateMutation = useMutation({
    mutationFn: () => {
      if (!selectedOperator) return Promise.reject();
      return operatorsApi.update(selectedOperator, editForm);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["operators"] });
      queryClient.invalidateQueries({ queryKey: ["operator-detail"] });
      setShowEdit(false);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => operatorsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["operators"] });
      setSelectedOperator(null);
    },
  });

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      operatorsApi.update(id, {
        name: detail?.name ?? "",
        contactEmail: detail?.contactEmail ?? "",
        rateLimitPerMinute: detail?.rateLimitPerMinute ?? 100,
        description: detail?.description,
        webhookUrl: detail?.webhookUrl,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["operators"] });
      queryClient.invalidateQueries({ queryKey: ["operator-detail"] });
    },
  });

  const regenerateKeyMutation = useMutation({
    mutationFn: (id: string) => operatorsApi.regenerateApiKey(id),
    onSuccess: (res) => {
      setShowRegenConfirm(false);
      const apiKey = res.data?.apiKey;
      if (apiKey) {
        setShowApiKey(apiKey);
      }
      queryClient.invalidateQueries({ queryKey: ["operator-detail"] });
    },
  });

  const removeStationMutation = useMutation({
    mutationFn: ({ operatorId, stationId }: { operatorId: string; stationId: string }) =>
      operatorsApi.removeStation(operatorId, stationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["operator-detail"] });
      queryClient.invalidateQueries({ queryKey: ["operators"] });
    },
  });

  const totalOperators = operators?.length ?? 0;
  const activeOperators = operators?.filter((o) => o.isActive).length ?? 0;
  const totalStationsAssigned = operators?.reduce((sum, o) => sum + o.stationCount, 0) ?? 0;

  const openEdit = () => {
    if (detail) {
      setEditForm({
        name: detail.name,
        contactEmail: detail.contactEmail,
        webhookUrl: detail.webhookUrl ?? "",
        description: detail.description ?? "",
        rateLimitPerMinute: detail.rateLimitPerMinute,
      });
      setShowEdit(true);
    }
  };

  if (!hasAccess) return <AccessDenied />;

  return (
    <div className="space-y-6 p-6">
      <PageHeader
        title={t("operators.title")}
        description={t("operators.description")}
      >
        {canCreate && (
          <Button onClick={() => setShowCreate(true)} aria-label={t("operators.createOperator")}>
            <Plus className="mr-2 h-4 w-4" /> {t("operators.createOperator")}
          </Button>
        )}
      </PageHeader>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <StatCard label={t("operators.title")} value={totalOperators} icon={Building2} />
        <StatCard label={t("operators.active")} value={activeOperators} icon={Shield} />
        <StatCard label={t("operators.stationsAssigned")} value={totalStationsAssigned} icon={MapPin} />
      </div>

      {/* Operators list */}
      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3].map((i) => (
            <SkeletonCard key={i} />
          ))}
        </div>
      ) : !operators?.length ? (
        <EmptyState
          icon={Building2}
          title={t("operators.noOperators")}
          description={t("operators.noOperatorsDesc")}
          action={{ label: t("operators.createOperator"), onClick: () => setShowCreate(true) }}
        />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {operators.map((operator) => (
            <Card
              key={operator.id}
              className={`cursor-pointer transition-shadow hover:shadow-md ${selectedOperator === operator.id ? "ring-2 ring-primary" : ""}`}
              onClick={() => setSelectedOperator(operator.id)}
            >
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-lg">{operator.name}</CardTitle>
                  <Badge variant={operator.isActive ? "default" : "secondary"}>
                    {operator.isActive ? t("operators.active") : t("operators.inactive")}
                  </Badge>
                </div>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-3 gap-2 text-sm">
                  <div>
                    <p className="text-muted-foreground">{t("operators.email")}</p>
                    <p className="font-medium truncate">{operator.contactEmail}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground">{t("operators.stationsAssigned")}</p>
                    <p className="font-medium">{operator.stationCount}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground">{t("operators.rateLimit")}</p>
                    <p className="font-medium">{operator.rateLimitPerMinute}</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Detail panel */}
      {detail && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                {detail.name}
                <Badge variant={detail.isActive ? "default" : "secondary"}>
                  {detail.isActive ? t("operators.active") : t("operators.inactive")}
                </Badge>
              </CardTitle>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={openEdit}
                  aria-label={t("common.edit")}
                >
                  <Pencil className="mr-1 h-4 w-4" />
                  {t("common.edit")}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowRegenConfirm(true)}
                  aria-label={t("operators.regenerateKey")}
                >
                  <RefreshCw className="mr-1 h-4 w-4" />
                  {t("operators.regenerateKey")}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() =>
                    toggleActiveMutation.mutate({ id: detail.id, isActive: detail.isActive })
                  }
                  aria-label={detail.isActive ? t("common.inactive") : t("common.active")}
                >
                  {detail.isActive ? t("common.inactive") : t("common.active")}
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => {
                    if (confirm(t("common.confirm"))) deleteMutation.mutate(detail.id);
                  }}
                  aria-label={t("common.delete")}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6 text-sm">
              <div className="flex items-start gap-2">
                <Mail className="h-4 w-4 mt-0.5 text-muted-foreground" />
                <div>
                  <p className="text-muted-foreground">{t("operators.email")}</p>
                  <p className="font-semibold">{detail.contactEmail}</p>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <Globe className="h-4 w-4 mt-0.5 text-muted-foreground" />
                <div>
                  <p className="text-muted-foreground">{t("operators.webhookUrl")}</p>
                  <p className="font-semibold truncate">{detail.webhookUrl || t("common.na")}</p>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <Gauge className="h-4 w-4 mt-0.5 text-muted-foreground" />
                <div>
                  <p className="text-muted-foreground">{t("operators.rateLimit")}</p>
                  <p className="font-semibold">{detail.rateLimitPerMinute}</p>
                </div>
              </div>
              <div>
                <p className="text-muted-foreground">{t("operators.description_label")}</p>
                <p className="font-semibold">{detail.description || t("common.na")}</p>
              </div>
            </div>

            {/* Assigned stations table */}
            <h3 className="text-sm font-semibold mb-2">{t("operators.stationsAssigned")}</h3>
            {detail.stations.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-sm" role="table">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-2" scope="col">
                        {t("operators.stationCodeColumn")}
                      </th>
                      <th className="text-left py-2" scope="col">
                        {t("operators.name")}
                      </th>
                      <th className="text-right py-2" scope="col"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.stations.map((station) => (
                      <tr key={station.id} className="border-b last:border-0">
                        <td className="py-2">{station.stationCode}</td>
                        <td className="py-2">{station.name}</td>
                        <td className="py-2 text-right">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() =>
                              removeStationMutation.mutate({
                                operatorId: detail.id,
                                stationId: station.id,
                              })
                            }
                            aria-label={t("common.delete")}
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="text-muted-foreground text-center py-4">
                {t("operators.noOperatorsDesc")}
              </p>
            )}
          </CardContent>
        </Card>
      )}

      {/* Create Dialog */}
      <Dialog open={showCreate} onClose={() => setShowCreate(false)} title={t("operators.createOperator")}>
        <DialogHeader onClose={() => setShowCreate(false)}>
          {t("operators.createOperator")}
        </DialogHeader>
        <DialogContent>
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium">{t("operators.name")}</label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
                placeholder={t("operators.namePlaceholder")}
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("operators.email")}</label>
              <input
                type="email"
                value={form.contactEmail}
                onChange={(e) => setForm({ ...form, contactEmail: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
                placeholder={t("operators.emailPlaceholder")}
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("operators.description_label")}</label>
              <textarea
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
                rows={3}
              />
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowCreate(false)}>
            {t("common.cancel")}
          </Button>
          <Button
            onClick={() => createMutation.mutate()}
            disabled={!form.name || !form.contactEmail || createMutation.isPending}
          >
            {t("common.create")}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={showEdit} onClose={() => setShowEdit(false)} title={t("common.edit")}>
        <DialogHeader onClose={() => setShowEdit(false)}>{t("common.edit")}</DialogHeader>
        <DialogContent>
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium">{t("operators.name")}</label>
              <input
                type="text"
                value={editForm.name}
                onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("operators.email")}</label>
              <input
                type="email"
                value={editForm.contactEmail}
                onChange={(e) => setEditForm({ ...editForm, contactEmail: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("operators.webhookUrl")}</label>
              <input
                type="url"
                value={editForm.webhookUrl}
                onChange={(e) => setEditForm({ ...editForm, webhookUrl: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
                placeholder={t("operators.webhookUrlPlaceholder")}
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("operators.rateLimit")}</label>
              <input
                type="number"
                min={1}
                value={editForm.rateLimitPerMinute}
                onChange={(e) =>
                  setEditForm({ ...editForm, rateLimitPerMinute: parseInt(e.target.value) || 100 })
                }
                className="mt-1 w-full rounded-md border px-3 py-2"
              />
            </div>
            <div>
              <label className="text-sm font-medium">{t("operators.description_label")}</label>
              <textarea
                value={editForm.description}
                onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                className="mt-1 w-full rounded-md border px-3 py-2"
                rows={3}
              />
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowEdit(false)}>
            {t("common.cancel")}
          </Button>
          <Button
            onClick={() => updateMutation.mutate()}
            disabled={!editForm.name || !editForm.contactEmail || updateMutation.isPending}
          >
            {t("common.save")}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* API Key Display Dialog */}
      <Dialog open={!!showApiKey} onClose={() => setShowApiKey(null)} title={t("operators.apiKey")}>
        <DialogHeader onClose={() => setShowApiKey(null)}>{t("operators.apiKey")}</DialogHeader>
        <DialogContent>
          <div className="space-y-4">
            <div className="rounded-md bg-yellow-50 border border-yellow-200 p-3 text-sm text-yellow-800">
              {t("operators.apiKeyWarning")}
            </div>
            <div className="flex items-center gap-2">
              <code className="flex-1 rounded-md bg-muted px-3 py-2 text-sm font-mono break-all">
                {showApiKey}
              </code>
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  if (showApiKey) navigator.clipboard.writeText(showApiKey);
                }}
                aria-label="Copy API Key"
              >
                <Copy className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button onClick={() => setShowApiKey(null)}>{t("common.close")}</Button>
        </DialogFooter>
      </Dialog>

      {/* Regenerate API Key Confirmation Dialog */}
      <Dialog
        open={showRegenConfirm}
        onClose={() => setShowRegenConfirm(false)}
        size="sm"
        title={t("operators.regenerateKey")}
      >
        <DialogHeader onClose={() => setShowRegenConfirm(false)}>
          {t("operators.regenerateKey")}
        </DialogHeader>
        <DialogContent>
          <p className="text-sm">{t("operators.regenerateConfirm")}</p>
        </DialogContent>
        <DialogFooter>
          <Button variant="outline" onClick={() => setShowRegenConfirm(false)}>
            {t("common.cancel")}
          </Button>
          <Button
            variant="destructive"
            onClick={() => {
              if (selectedOperator) regenerateKeyMutation.mutate(selectedOperator);
            }}
            disabled={regenerateKeyMutation.isPending}
          >
            {t("common.confirm")}
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  );
}
