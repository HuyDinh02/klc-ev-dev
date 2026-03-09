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
} from "lucide-react";
import { settingsApi, type SystemSettings } from "@/lib/api";
import { useTranslation, type Locale } from "@/lib/i18n";

export default function SettingsPage() {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState("general");
  const [localSettings, setLocalSettings] = useState<SystemSettings | null>(null);
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
    },
  });

  const handleSave = () => {
    if (settings) saveMutation.mutate(settings);
  };

  const isDirty = localSettings !== null;

  const tabs = [
    { id: "general", label: t("settings.general"), icon: Settings },
    { id: "notifications", label: t("settings.notifications"), icon: Bell },
    { id: "ocpp", label: t("settings.ocpp"), icon: Zap },
    { id: "payments", label: t("settings.payments"), icon: CreditCard },
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
    <div className="space-y-6">
      {/* Header */}
      <div className="sticky top-0 z-30 flex h-16 items-center border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <PageHeader title={t("settings.title")} description={t("settings.description")}>
          {saveMutation.isError && (
            <span className="text-sm text-destructive">{t("settings.saveFailed")}</span>
          )}
          <Button onClick={handleSave} disabled={saveMutation.isPending || !isDirty}>
            {saveMutation.isPending ? (
              <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Save className="mr-2 h-4 w-4" />
            )}
            {t("settings.saveChanges")}
          </Button>
        </PageHeader>
      </div>

      <div className="flex gap-6">
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
                    <label className="text-sm font-medium">{t("settings.timezone")}</label>
                    <select
                      value={settings.timezone}
                      onChange={(e) =>
                        updateSettings({ timezone: e.target.value })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    >
                      <option value="Asia/Ho_Chi_Minh">
                        Asia/Ho_Chi_Minh (UTC+7)
                      </option>
                      <option value="Asia/Bangkok">Asia/Bangkok (UTC+7)</option>
                      <option value="Asia/Singapore">
                        Asia/Singapore (UTC+8)
                      </option>
                    </select>
                  </div>
                  <div>
                    <label className="text-sm font-medium">{t("settings.currency")}</label>
                    <select
                      value={settings.currency}
                      onChange={(e) =>
                        updateSettings({ currency: e.target.value })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    >
                      <option value="VND">VND - Vietnamese Dong</option>
                      <option value="USD">USD - US Dollar</option>
                    </select>
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
                    placeholder="admin@example.com"
                  />
                </div>
              </CardContent>
            </Card>
          )}

          {/* OCPP Settings */}
          {activeTab === "ocpp" && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Zap className="h-5 w-5" aria-hidden="true" />
                  {t("settings.ocppSettings")}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium">{t("settings.webSocketPort")}</label>
                    <input
                      type="number"
                      value={settings.ocppWebSocketPort}
                      onChange={(e) =>
                        updateSettings({
                          ocppWebSocketPort: parseInt(e.target.value),
                        })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-medium">
                      {t("settings.heartbeatInterval")}
                    </label>
                    <input
                      type="number"
                      value={settings.ocppHeartbeatInterval}
                      onChange={(e) =>
                        updateSettings({
                          ocppHeartbeatInterval: parseInt(e.target.value),
                        })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-medium">
                      {t("settings.meterValueInterval")}
                    </label>
                    <input
                      type="number"
                      value={settings.ocppMeterValueInterval}
                      onChange={(e) =>
                        updateSettings({
                          ocppMeterValueInterval: parseInt(e.target.value),
                        })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    />
                  </div>
                </div>
                <div className="rounded-lg border p-4 bg-muted/30">
                  <p className="text-sm text-muted-foreground">
                    <strong>Note:</strong> {t("settings.ocppNote")}
                  </p>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Payment Settings */}
          {activeTab === "payments" && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <CreditCard className="h-5 w-5" aria-hidden="true" />
                  {t("settings.paymentSettings")}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium">
                      {t("settings.defaultPaymentGateway")}
                    </label>
                    <select
                      value={settings.defaultPaymentGateway}
                      onChange={(e) =>
                        updateSettings({
                          defaultPaymentGateway: e.target.value,
                        })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    >
                      <option value="VNPay">VNPay</option>
                      <option value="MoMo">MoMo</option>
                      <option value="ZaloPay">ZaloPay</option>
                    </select>
                  </div>
                  <div>
                    <label className="text-sm font-medium">
                      {t("settings.eInvoiceProvider")}
                    </label>
                    <select
                      value={settings.eInvoiceProvider}
                      onChange={(e) =>
                        updateSettings({
                          eInvoiceProvider: e.target.value,
                        })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    >
                      <option value="MISA">MISA</option>
                      <option value="Viettel">Viettel</option>
                      <option value="VNPT">VNPT</option>
                    </select>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-medium">{t("settings.autoInvoiceGeneration")}</p>
                    <p className="text-sm text-muted-foreground">
                      {t("settings.autoInvoiceGenerationDesc")}
                    </p>
                  </div>
                  <label className="relative inline-flex cursor-pointer items-center">
                    <input
                      type="checkbox"
                      checked={settings.autoInvoiceGeneration}
                      onChange={(e) =>
                        updateSettings({
                          autoInvoiceGeneration: e.target.checked,
                        })
                      }
                      className="peer sr-only"
                      aria-label={t("settings.autoInvoiceGeneration")}
                    />
                    <div className="h-6 w-11 rounded-full bg-muted border border-border after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full"></div>
                  </label>
                </div>
              </CardContent>
            </Card>
          )}

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
                          sessionTimeout: parseInt(e.target.value),
                        })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
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

                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-medium">{t("settings.requireMfa")}</p>
                    <p className="text-sm text-muted-foreground">
                      {t("settings.requireMfaDesc")}
                    </p>
                  </div>
                  <label className="relative inline-flex cursor-pointer items-center">
                    <input
                      type="checkbox"
                      checked={settings.requireMfa}
                      onChange={(e) =>
                        updateSettings({
                          requireMfa: e.target.checked,
                        })
                      }
                      className="peer sr-only"
                      aria-label={t("settings.requireMfa")}
                    />
                    <div className="h-6 w-11 rounded-full bg-muted border border-border after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full"></div>
                  </label>
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
