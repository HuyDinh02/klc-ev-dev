"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/ui/page-header";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Settings,
  Bell,
  Lock,
  Zap,
  CreditCard,
  Save,
  RefreshCw,
  AlertCircle,
  CheckCircle2,
  Send,
} from "lucide-react";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { settingsApi, type SystemSettings } from "@/lib/api";
import { useTranslation, type Locale } from "@/lib/i18n";
import { useRequirePermission, useHasPermission } from "@/lib/use-permission";
import { AccessDenied } from "@/components/ui/access-denied";

export default function SettingsPage() {
  const hasAccess = useRequirePermission("KLC.Settings");
  const canUpdate = useHasPermission("KLC.Settings.Update");
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState("general");
  const [localSettings, setLocalSettings] = useState<SystemSettings | null>(null);
  const [showSaved, setShowSaved] = useState(false);
  const [showApplyResult, setShowApplyResult] = useState(false);
  const [applyResult, setApplyResult] = useState<{ successCount: number; failureCount: number } | null>(null);
  const { t, setLocale } = useTranslation();

  const { data, isLoading, error } = useQuery({
    queryKey: ["settings"],
    queryFn: async () => {
      const res = await settingsApi.get();
      return res.data;
    },
  });

  // Initialize local state from fetched data
  const settings = localSettings ?? data;

  const updateSettings = (patch: Partial<SystemSettings>) => {
    if (!settings) return;
    setLocalSettings({ ...settings, ...patch });
  };

  const saveMutation = useMutation({
    mutationFn: (data: SystemSettings) => settingsApi.update(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["settings"] });
      setLocalSettings(null);
      setShowSaved(true);
      setTimeout(() => setShowSaved(false), 3000);
    },
  });

  const applyToChargersMutation = useMutation({
    mutationFn: () => settingsApi.applyToChargers(),
    onSuccess: (res) => {
      setApplyResult(res.data);
      setShowApplyResult(true);
    },
    onError: () => {
      setApplyResult(null);
      setShowApplyResult(true);
    },
  });

  const handleSave = () => {
    if (settings) saveMutation.mutate(settings);
  };

  const isDirty = localSettings !== null;

  if (!hasAccess) return <AccessDenied />;

  const tabs = [
    { id: "general", label: t("settings.general"), icon: Settings },
    { id: "security", label: t("settings.security"), icon: Lock },
  ];

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
          <PageHeader title={t("settings.title")} description={t("settings.description")} />
        </div>
        <div className="flex gap-6 px-6">
          <div className="w-64 space-y-2">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-9 w-full rounded-lg" />
            ))}
          </div>
          <div className="flex-1 space-y-4">
            <Skeleton className="h-10 w-48 rounded-lg" />
            <div className="grid gap-4 md:grid-cols-2">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="space-y-2">
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-10 w-full rounded-md" />
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error || !settings) {
    return (
      <div className="space-y-6">
        <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
          <PageHeader title={t("settings.title")} description={t("settings.description")} />
        </div>
        <div className="flex items-center justify-center h-64 text-destructive">
          <AlertCircle className="mr-2 h-5 w-5" />
          {t("settings.failedToLoad")}
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col">
      {/* Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title={t("settings.title")} description={t("settings.description")}>
          {showSaved && (
            <span className="flex items-center gap-1 text-sm text-green-600">
              <CheckCircle2 className="h-4 w-4" />
              {t("settings.saved")}
            </span>
          )}
          {saveMutation.isError && (
            <span className="text-sm text-destructive">{t("settings.saveFailed")}</span>
          )}
          <Button onClick={handleSave} disabled={saveMutation.isPending || !isDirty || !canUpdate}>
            {saveMutation.isPending ? (
              <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Save className="mr-2 h-4 w-4" />
            )}
            {t("settings.saveChanges")}
          </Button>
        </PageHeader>
      </div>

      <div className="flex gap-6 p-6">
        {/* Sidebar */}
        <nav className="w-64 space-y-1" role="tablist" aria-label={t("settings.title")}>
          {tabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <button
                key={tab.id}
                role="tab"
                aria-selected={activeTab === tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors ${
                  activeTab === tab.id
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                }`}
              >
                <Icon className="h-4 w-4" aria-hidden="true" />
                {tab.label}
              </button>
            );
          })}
        </nav>

        {/* Content */}
        <div className="flex-1">
          {/* General Settings */}
          {activeTab === "general" && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Settings className="h-5 w-5" aria-hidden="true" />
                  {t("settings.generalSettings")}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium">{t("settings.siteName")}</label>
                    <input
                      type="text"
                      value={settings.siteName}
                      onChange={(e) =>
                        updateSettings({ siteName: e.target.value })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-medium">{t("settings.language")}</label>
                    <select
                      value={settings.language}
                      onChange={(e) => {
                        updateSettings({ language: e.target.value });
                        setLocale(e.target.value as Locale);
                      }}
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    >
                      <option value="vi">Tiếng Việt</option>
                      <option value="en">English</option>
                    </select>
                  </div>
                </div>
                <div className="mt-4 rounded-md bg-muted/50 px-4 py-3 text-sm text-muted-foreground">
                  Múi giờ: UTC+7 (Việt Nam) · Đơn vị tiền tệ: VNĐ
                </div>
              </CardContent>
            </Card>
          )}

          {/* Notification Settings */}
          {activeTab === "notifications" && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Bell className="h-5 w-5" aria-hidden="true" />
                  {t("settings.notificationSettings")}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">{t("settings.emailNotifications")}</p>
                      <p className="text-sm text-muted-foreground">
                        {t("settings.emailNotificationsDesc")}
                      </p>
                    </div>
                    <label className="relative inline-flex cursor-pointer items-center">
                      <input
                        type="checkbox"
                        checked={settings.emailNotifications}
                        onChange={(e) =>
                          updateSettings({
                            emailNotifications: e.target.checked,
                          })
                        }
                        className="peer sr-only"
                        aria-label={t("settings.emailNotifications")}
                      />
                      <div className="h-6 w-11 rounded-full bg-muted border border-border after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full"></div>
                    </label>
                  </div>

                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">{t("settings.smsNotifications")}</p>
                      <p className="text-sm text-muted-foreground">
                        {t("settings.smsNotificationsDesc")}
                      </p>
                    </div>
                    <label className="relative inline-flex cursor-pointer items-center">
                      <input
                        type="checkbox"
                        checked={settings.smsNotifications}
                        onChange={(e) =>
                          updateSettings({
                            smsNotifications: e.target.checked,
                          })
                        }
                        className="peer sr-only"
                        aria-label={t("settings.smsNotifications")}
                      />
                      <div className="h-6 w-11 rounded-full bg-muted border border-border after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full"></div>
                    </label>
                  </div>

                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">{t("settings.pushNotifications")}</p>
                      <p className="text-sm text-muted-foreground">
                        {t("settings.pushNotificationsDesc")}
                      </p>
                    </div>
                    <label className="relative inline-flex cursor-pointer items-center">
                      <input
                        type="checkbox"
                        checked={settings.pushNotifications}
                        onChange={(e) =>
                          updateSettings({
                            pushNotifications: e.target.checked,
                          })
                        }
                        className="peer sr-only"
                        aria-label={t("settings.pushNotifications")}
                      />
                      <div className="h-6 w-11 rounded-full bg-muted border border-border after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full"></div>
                    </label>
                  </div>
                </div>

                <div>
                  <label className="text-sm font-medium">{t("settings.alertEmail")}</label>
                  <input
                    type="email"
                    value={settings.alertEmail}
                    onChange={(e) =>
                      updateSettings({ alertEmail: e.target.value })
                    }
                    className="mt-1 w-full rounded-md border px-3 py-2"
                    placeholder={t("settings.alertEmailPlaceholder")}
                  />
                </div>
              </CardContent>
            </Card>
          )}

          {/* OCPP Settings */}
          {/* Security Settings */}
          {activeTab === "security" && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Lock className="h-5 w-5" aria-hidden="true" />
                  {t("settings.securitySettings")}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium">
                      {t("settings.sessionTimeout")}
                    </label>
                    <input
                      type="number"
                      value={settings.sessionTimeout}
                      onChange={(e) =>
                        updateSettings({
                          sessionTimeout: parseInt(e.target.value) || 0,
                        })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                      min={5}
                      max={1440}
                    />
                  </div>
                  <div>
                    <label className="text-sm font-medium">
                      {t("settings.minPasswordLength")}
                    </label>
                    <input
                      type="number"
                      value={settings.passwordMinLength}
                      onChange={(e) =>
                        updateSettings({
                          passwordMinLength: parseInt(e.target.value),
                        })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                      min={6}
                      max={24}
                    />
                  </div>
                </div>

                {/* MFA disabled for Vietnam launch */}
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Apply to Chargers Result Dialog */}
      <Dialog open={showApplyResult} onClose={() => setShowApplyResult(false)} size="sm">
        <DialogHeader onClose={() => setShowApplyResult(false)}>
          {t("settings.applyToChargersResult")}
        </DialogHeader>
        <DialogContent>
          {applyResult ? (
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                <Badge variant="success">{t("settings.successCount")}</Badge>
                <span className="text-lg font-semibold">{applyResult.successCount}</span>
              </div>
              <div className="flex items-center gap-3">
                <Badge variant="destructive">{t("settings.failureCount")}</Badge>
                <span className="text-lg font-semibold">{applyResult.failureCount}</span>
              </div>
              {applyResult.failureCount > 0 && (
                <p className="text-sm text-muted-foreground">
                  {t("settings.applyPartialFailure")}
                </p>
              )}
            </div>
          ) : (
            <div className="flex items-center gap-2 text-destructive">
              <AlertCircle className="h-5 w-5" />
              {t("settings.applyToChargersFailed")}
            </div>
          )}
        </DialogContent>
        <DialogFooter>
          <Button onClick={() => setShowApplyResult(false)}>
            {t("common.close")}
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  );
}
