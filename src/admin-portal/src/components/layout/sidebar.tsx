"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import Image from "next/image";
import {
  LayoutDashboard,
  MapPin,
  Activity,
  Zap,
  DollarSign,
  AlertTriangle,
  Wrench,
  FileText,
  Bell,
  ChevronLeft,
  ChevronRight,
  Settings,
  LogOut,
  Users,
  Ticket,
  CreditCard,
  BarChart3,
  Shield,
  Cable,
  Building2,
  Truck,
  type LucideIcon,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { cn } from "@/lib/utils";
import { useSidebarStore, useAuthStore, useAlertsStore } from "@/lib/store";
import { useTranslation } from "@/lib/i18n";
import { Button } from "@/components/ui/button";

interface NavItem {
  href: string;
  labelKey: string;
  icon: LucideIcon;
}

interface NavSection {
  titleKey: string;
  items: NavItem[];
}

const navigation: NavSection[] = [
  {
    titleKey: "nav.operations",
    items: [
      { href: "/", labelKey: "nav.dashboard", icon: LayoutDashboard },
      { href: "/stations", labelKey: "nav.stations", icon: MapPin },
      { href: "/monitoring", labelKey: "nav.monitoring", icon: Activity },
      { href: "/sessions", labelKey: "nav.sessions", icon: Zap },
      { href: "/power-sharing", labelKey: "nav.powerSharing", icon: Cable },
    ],
  },
  {
    titleKey: "nav.incidents",
    items: [
      { href: "/faults", labelKey: "nav.faults", icon: AlertTriangle },
      { href: "/maintenance", labelKey: "nav.maintenance", icon: Wrench },
    ],
  },
  {
    titleKey: "nav.business",
    items: [
      { href: "/tariffs", labelKey: "nav.tariffs", icon: DollarSign },
      { href: "/payments", labelKey: "nav.payments", icon: CreditCard },
      { href: "/vouchers", labelKey: "nav.marketing", icon: Ticket },
      { href: "/operators", labelKey: "nav.operators", icon: Building2 },
      { href: "/fleets", labelKey: "nav.fleets", icon: Truck },
    ],
  },
  {
    titleKey: "nav.users",
    items: [
      { href: "/user-management", labelKey: "nav.userManagement", icon: Users },
    ],
  },
  {
    titleKey: "nav.system",
    items: [
      { href: "/analytics", labelKey: "nav.reports", icon: BarChart3 },
      { href: "/audit-logs", labelKey: "nav.auditLogs", icon: FileText },
    ],
  },
];

export function Sidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const { isCollapsed, toggle } = useSidebarStore();
  const { logout, user } = useAuthStore();
  const { unreadCount } = useAlertsStore();
  const { t } = useTranslation();

  const isActive = (href: string) => {
    if (href === "/") return pathname === "/";
    return pathname === href || pathname.startsWith(`${href}/`);
  };

  return (
    <aside
      aria-label="Main navigation"
      className={cn(
        "fixed left-0 top-0 z-40 h-screen border-r bg-card transition-all duration-300",
        isCollapsed ? "w-16" : "w-64"
      )}
    >
      <div className="flex h-full flex-col">
        {/* Brand */}
        <div className="flex h-16 items-center justify-between border-b px-4">
          {!isCollapsed && (
            <Link href="/" className="flex items-center gap-2.5">
              <Image src="/logo-icon.png" alt="K-Charge" width={32} height={32} className="h-8 w-8" priority />
              <div className="flex flex-col">
                <span className="text-sm font-bold tracking-tight text-[var(--color-brand-green)]">K-Charge</span>
                <span className="text-[10px] text-muted-foreground">by KLC Energy</span>
              </div>
            </Link>
          )}
          {isCollapsed && (
            <Image src="/logo-icon.png" alt="K-Charge" width={32} height={32} className="mx-auto h-8 w-8" priority />
          )}
          {!isCollapsed && (
            <Button variant="ghost" size="icon" onClick={toggle} className="h-7 w-7" aria-label="Collapse sidebar">
              <ChevronLeft className="h-4 w-4" />
            </Button>
          )}
        </div>

        {/* Expand button when collapsed */}
        {isCollapsed && (
          <div className="flex justify-center py-2">
            <Button variant="ghost" size="icon" onClick={toggle} className="h-7 w-7" aria-label="Expand sidebar">
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        )}

        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto px-2 py-1">
          {navigation.map((section) => (
            <div key={section.titleKey} className="mb-1">
              {!isCollapsed && (
                <p className="mb-1 px-3 pt-3 pb-1 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                  {t(section.titleKey)}
                </p>
              )}
              {isCollapsed && <div className="my-1 mx-2 border-t" />}
              <div className="space-y-0.5">
                {section.items.map((item) => {
                  const active = isActive(item.href);
                  const Icon = item.icon;
                  const label = t(item.labelKey);
                  return (
                    <Link
                      key={item.href}
                      href={item.href}
                      title={isCollapsed ? label : undefined}
                      className={cn(
                        "flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors",
                        active
                          ? "bg-primary text-primary-foreground"
                          : "text-muted-foreground hover:bg-accent/50 hover:text-foreground"
                      )}
                    >
                      <Icon className="h-[18px] w-[18px] flex-shrink-0" />
                      {!isCollapsed && <span>{label}</span>}
                    </Link>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        {/* Alerts */}
        <div className="border-t px-2 py-1.5">
          <Link
            href="/alerts"
            className={cn(
              "flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors",
              pathname === "/alerts"
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:bg-accent/50 hover:text-foreground"
            )}
          >
            <div className="relative">
              <Bell className="h-[18px] w-[18px] flex-shrink-0" />
              {unreadCount > 0 && (
                <span className="absolute -right-1 -top-1 flex h-4 w-4 items-center justify-center rounded-full bg-destructive text-[10px] font-bold text-white">
                  {unreadCount > 9 ? "9+" : unreadCount}
                </span>
              )}
            </div>
            {!isCollapsed && <span>{t("nav.alerts")}</span>}
          </Link>
        </div>

        {/* User section */}
        <div className="border-t px-2 py-2">
          {user && !isCollapsed && (
            <div className="mb-1.5 px-3 py-1.5">
              <p className="text-sm font-medium">{user.username}</p>
              <p className="text-xs text-muted-foreground">{user.role}</p>
            </div>
          )}
          <div className="space-y-0.5">
            <Link
              href="/settings"
              className={cn(
                "flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors",
                pathname === "/settings"
                  ? "bg-primary text-primary-foreground"
                  : "text-muted-foreground hover:bg-accent/50 hover:text-foreground"
              )}
            >
              <Settings className="h-[18px] w-[18px] flex-shrink-0" />
              {!isCollapsed && <span>{t("nav.settings")}</span>}
            </Link>
            <button
              onClick={() => { logout(); router.push("/login"); }}
              className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent/50 hover:text-foreground"
            >
              <LogOut className="h-[18px] w-[18px] flex-shrink-0" />
              {!isCollapsed && <span>{t("nav.logout")}</span>}
            </button>
          </div>
        </div>
      </div>
    </aside>
  );
}
