"use client";

import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Settings,
  Bell,
  Lock,
  Globe,
  Mail,
  CreditCard,
  FileText,
  Zap,
  Save,
  RefreshCw,
} from "lucide-react";

interface SystemSettings {
  // General
  siteName: string;
  timezone: string;
  currency: string;
  language: string;

  // Notifications
  emailNotifications: boolean;
  smsNotifications: boolean;
  pushNotifications: boolean;
  alertEmail: string;

  // OCPP
  ocppWebSocketPort: number;
  ocppHeartbeatInterval: number;
  ocppMeterValueInterval: number;

  // Payments
  defaultPaymentGateway: string;
  autoInvoiceGeneration: boolean;
  eInvoiceProvider: string;

  // Security
  sessionTimeout: number;
  requireMfa: boolean;
  passwordMinLength: number;
}

export default function SettingsPage() {
  const [settings, setSettings] = useState<SystemSettings>({
    siteName: "KCharge CSMS",
    timezone: "Asia/Ho_Chi_Minh",
    currency: "VND",
    language: "vi",
    emailNotifications: true,
    smsNotifications: false,
    pushNotifications: true,
    alertEmail: "admin@kcharge.vn",
    ocppWebSocketPort: 5002,
    ocppHeartbeatInterval: 60,
    ocppMeterValueInterval: 30,
    defaultPaymentGateway: "VNPay",
    autoInvoiceGeneration: true,
    eInvoiceProvider: "MISA",
    sessionTimeout: 30,
    requireMfa: false,
    passwordMinLength: 8,
  });

  const [isSaving, setIsSaving] = useState(false);
  const [activeTab, setActiveTab] = useState("general");

  const handleSave = async () => {
    setIsSaving(true);
    // Would call API to save settings
    await new Promise((resolve) => setTimeout(resolve, 1000));
    setIsSaving(false);
  };

  const tabs = [
    { id: "general", label: "General", icon: Settings },
    { id: "notifications", label: "Notifications", icon: Bell },
    { id: "ocpp", label: "OCPP", icon: Zap },
    { id: "payments", label: "Payments", icon: CreditCard },
    { id: "security", label: "Security", icon: Lock },
  ];

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
        <Button onClick={handleSave} disabled={isSaving}>
          {isSaving ? (
            <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
          ) : (
            <Save className="mr-2 h-4 w-4" />
          )}
          Save Changes
        </Button>
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
                        setSettings({ ...settings, siteName: e.target.value })
                      }
                      className="mt-1 w-full rounded-md border px-3 py-2"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-medium">Timezone</label>
                    <select
                      value={settings.timezone}
                      onChange={(e) =>
                        setSettings({ ...settings, timezone: e.target.value })
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
                        setSettings({ ...settings, currency: e.target.value })
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
                        setSettings({ ...settings, language: e.target.value })
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
                          setSettings({
                            ...settings,
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
                          setSettings({
                            ...settings,
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
                          setSettings({
                            ...settings,
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
                      setSettings({ ...settings, alertEmail: e.target.value })
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
                        setSettings({
                          ...settings,
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
                        setSettings({
                          ...settings,
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
                        setSettings({
                          ...settings,
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
                        setSettings({
                          ...settings,
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
                        setSettings({
                          ...settings,
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
                        setSettings({
                          ...settings,
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
                        setSettings({
                          ...settings,
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
                        setSettings({
                          ...settings,
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
                        setSettings({
                          ...settings,
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
