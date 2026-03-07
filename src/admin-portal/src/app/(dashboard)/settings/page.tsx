"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
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

export default function SettingsPage() {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState("general");
  const [localSettings, setLocalSettings] = useState<SystemSettings | null>(null);

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
    { id: "general", label: "General", icon: Settings },
    { id: "notifications", label: "Notifications", icon: Bell },
    { id: "ocpp", label: "OCPP", icon: Zap },
    { id: "payments", label: "Payments", icon: CreditCard },
    { id: "security", label: "Security", icon: Lock },
  ];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <RefreshCw className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error || !settings) {
    return (
      <div className="flex items-center justify-center h-64 text-destructive">
        <AlertCircle className="mr-2 h-5 w-5" />
        Failed to load settings
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Settings</h1>
          <p className="text-muted-foreground">
            Configure system settings and preferences
          </p>
        </div>
        <div className="flex items-center gap-2">
          {saveMutation.isError && (
            <span className="text-sm text-destructive">Save failed</span>
          )}
          <Button onClick={handleSave} disabled={saveMutation.isPending || !isDirty}>
            {saveMutation.isPending ? (
              <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Save className="mr-2 h-4 w-4" />
            )}
            Save Changes
          </Button>
        </div>
      </div>

      <div className="flex gap-6">
        {/* Sidebar */}
        <div className="w-64 space-y-1">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors ${
                  activeTab === tab.id
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                }`}
              >
                <Icon className="h-4 w-4" />
                {tab.label}
              </button>
            );
          })}
        </div>

        {/* Content */}
        <div className="flex-1">
          {/* General Settings */}
          {activeTab === "general" && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Settings className="h-5 w-5" />
                  General Settings
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium">Site Name</label>
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
                    <label className="text-sm font-medium">Timezone</label>
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
                    <label className="text-sm font-medium">Currency</label>
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
                    <label className="text-sm font-medium">Language</label>
                    <select
                      value={settings.language}
                      onChange={(e) =>
                        updateSettings({ language: e.target.value })
                      }
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
                  <Bell className="h-5 w-5" />
                  Notification Settings
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">Email Notifications</p>
                      <p className="text-sm text-muted-foreground">
                        Receive alerts via email
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
                      />
                      <div className="h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full"></div>
                    </label>
                  </div>

                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">SMS Notifications</p>
                      <p className="text-sm text-muted-foreground">
                        Receive critical alerts via SMS
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
                      />
                      <div className="h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full"></div>
                    </label>
                  </div>

                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">Push Notifications</p>
                      <p className="text-sm text-muted-foreground">
                        Browser push notifications
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
                      />
                      <div className="h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full"></div>
                    </label>
                  </div>
                </div>

                <div>
                  <label className="text-sm font-medium">Alert Email</label>
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
                  <Zap className="h-5 w-5" />
                  OCPP Settings
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium">WebSocket Port</label>
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
                      Heartbeat Interval (seconds)
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
                      Meter Value Interval (seconds)
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
                    <strong>Note:</strong> Changing OCPP settings may require
                    restarting connected charging stations to take effect.
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
                  <CreditCard className="h-5 w-5" />
                  Payment Settings
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium">
                      Default Payment Gateway
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
                      E-Invoice Provider
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
                    <p className="font-medium">Auto Invoice Generation</p>
                    <p className="text-sm text-muted-foreground">
                      Automatically generate invoices after payment
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
                    />
                    <div className="h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full"></div>
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
                  <Lock className="h-5 w-5" />
                  Security Settings
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium">
                      Session Timeout (minutes)
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
                      Minimum Password Length
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
                    <p className="font-medium">Require MFA</p>
                    <p className="text-sm text-muted-foreground">
                      Require multi-factor authentication for admin users
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
                    />
                    <div className="h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:bg-white after:transition-all after:content-[''] peer-checked:bg-primary peer-checked:after:translate-x-full"></div>
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
