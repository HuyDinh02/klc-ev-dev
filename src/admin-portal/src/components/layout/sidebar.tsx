"use client";

import { useCallback, useSyncExternalStore } from "react";
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
  Gift,
  Smartphone,
  MessageSquare,
  X,
  type LucideIcon,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { cn } from "@/lib/utils";
import { useSidebarStore, useAuthStore, useAlertsStore } from "@/lib/store";
import { useTranslation } from "@/lib/i18n";
import { Button } from "@/components/ui/button";

// ---- Mobile detection hook (SSR-safe) ----
const MD_BREAKPOINT = 768;

function subscribeToMediaQuery(callback: () => void) {
  const mql = window.matchMedia(`(max-width: ${MD_BREAKPOINT - 1}px)`);
  mql.addEventListener("change", callback);
  return () => mql.removeEventListener("change", callback);
}

function getIsMobileSnapshot() {
  return window.matchMedia(`(max-width: ${MD_BREAKPOINT - 1}px)`).matches;
}

function getIsMobileServerSnapshot() {
  return false; // assume desktop on SSR
}

function useIsMobile() {
  return useSyncExternalStore(
    subscribeToMediaQuery,
    getIsMobileSnapshot,
    getIsMobileServerSnapshot,
  );
}

// ---- Navigation config ----

interface NavItem {
  href: string;
  labelKey: string;
  icon: LucideIcon;
  permission?: string; // KLC permission required to see this item (undefined = always visible)
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
      { href: "/stations", labelKey: "nav.stations", icon: MapPin, permission: "KLC.Stations" },
      { href: "/monitoring", labelKey: "nav.monitoring", icon: Activity, permission: "KLC.Monitoring" },
      { href: "/sessions", labelKey: "nav.sessions", icon: Zap, permission: "KLC.Sessions" },
      { href: "/power-sharing", labelKey: "nav.powerSharing", icon: Cable, permission: "KLC.PowerSharing" },
    ],
  },
  {
    titleKey: "nav.incidents",
    items: [
      { href: "/faults", labelKey: "nav.faults", icon: AlertTriangle, permission: "KLC.Faults" },
      { href: "/maintenance", labelKey: "nav.maintenance", icon: Wrench, permission: "KLC.Maintenance" },
    ],
  },
  {
    titleKey: "nav.business",
    items: [
      { href: "/tariffs", labelKey: "nav.tariffs", icon: DollarSign, permission: "KLC.Tariffs" },
      { href: "/payments", labelKey: "nav.payments", icon: CreditCard, permission: "KLC.Payments" },
      { href: "/vouchers", labelKey: "nav.marketing", icon: Ticket, permission: "KLC.Vouchers" },
      { href: "/promotions", labelKey: "nav.promotions", icon: Gift, permission: "KLC.Promotions" },
      { href: "/operators", labelKey: "nav.operators", icon: Building2, permission: "KLC.Operators" },
      { href: "/fleets", labelKey: "nav.fleets", icon: Truck, permission: "KLC.Fleets" },
    ],
  },
  {
    titleKey: "nav.users",
    items: [
      { href: "/user-management", labelKey: "nav.userManagement", icon: Users, permission: "KLC.UserManagement" },
      { href: "/mobile-users", labelKey: "nav.mobileUsers", icon: Smartphone, permission: "KLC.MobileUsers" },
    ],
  },
  {
    titleKey: "nav.system",
    items: [
      { href: "/groups", labelKey: "nav.stationGroups", icon: MapPin, permission: "KLC.StationGroups" },
      { href: "/analytics", labelKey: "nav.reports", icon: BarChart3 },
      { href: "/audit-logs", labelKey: "nav.auditLogs", icon: FileText, permission: "KLC.AuditLogs" },
      { href: "/e-invoices", labelKey: "nav.eInvoices", icon: FileText, permission: "KLC.EInvoices" },
      { href: "/notifications", labelKey: "nav.notifications", icon: Bell, permission: "KLC.Notifications" },
      { href: "/feedback", labelKey: "nav.feedback", icon: MessageSquare, permission: "KLC.Feedback" },
    ],
  },
];

export function Sidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const { isCollapsed, toggle, isMobileOpen, setMobileOpen } = useSidebarStore();
  const { logout, user, permissions } = useAuthStore();
  const { unreadCount } = useAlertsStore();
  const { t } = useTranslation();
  const isMobile = useIsMobile();

  // On mobile the sidebar is always full-width; collapse only applies on desktop.
  const collapsed = !isMobile && isCollapsed;

  const isActive = (href: string) => {
    if (href === "/") return pathname === "/";
    return pathname === href || pathname.startsWith(`${href}/`);
  };

  // Check if user has access to a nav item.
  // If permissions haven't loaded yet (empty array), show all items to avoid flash.
  const canAccess = (item: NavItem) => {
    if (!item.permission) return true;
    if (permissions.length === 0) return true; // not loaded yet — show all
    return permissions.includes(item.permission);
  };

  // Close sidebar on mobile when navigating
  const handleNavClick = useCallback(() => {
    if (isMobile) {
      setMobileOpen(false);
    }
  }, [isMobile, setMobileOpen]);

  return (
    <>
      {/* Mobile backdrop */}
      {isMobileOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 md:hidden"
          onClick={() => setMobileOpen(false)}
          aria-hidden="true"
        />
      )}

      <aside
        aria-label="Main navigation"
        className={cn(
          "fixed left-0 top-0 h-screen border-r bg-card transition-all duration-300",
          // Mobile: always w-64, slide in/out, higher z-index
          "max-md:z-50 max-md:w-64",
          isMobileOpen ? "max-md:translate-x-0" : "max-md:-translate-x-full",
          // Desktop: z-40, respect collapsed width
          "md:z-40",
          collapsed ? "md:w-16" : "md:w-64",
        )}
      >
        <div className="flex h-full flex-col">
          {/* Brand */}
          <div className="flex h-16 items-center justify-between border-b px-4">
            {!collapsed && (
              <Link href="/" onClick={handleNavClick} className="flex items-center gap-2.5">
                <Image src="/logo-icon.png" alt="K-Charge" width={32} height={32} className="h-8 w-8" priority />
                <div className="flex flex-col">
                  <span className="text-sm font-bold tracking-tight text-[var(--color-brand-green)]">K-Charge</span>
                  <span className="text-[10px] text-muted-foreground">by KLC Energy</span>
                </div>
              </Link>
            )}
            {collapsed && (
              <Image src="/logo-icon.png" alt="K-Charge" width={32} height={32} className="mx-auto h-8 w-8" priority />
            )}
            {/* Desktop: collapse toggle | Mobile: close button */}
            {isMobile ? (
              <Button variant="ghost" size="icon" onClick={() => setMobileOpen(false)} className="h-7 w-7" aria-label="Close sidebar">
                <X className="h-4 w-4" />
              </Button>
            ) : (
              !collapsed && (
                <Button variant="ghost" size="icon" onClick={toggle} className="h-7 w-7" aria-label="Collapse sidebar">
                  <ChevronLeft className="h-4 w-4" />
                </Button>
              )
            )}
          </div>

          {/* Expand button when collapsed (desktop only) */}
          {collapsed && (
            <div className="flex justify-center py-2">
              <Button variant="ghost" size="icon" onClick={toggle} className="h-7 w-7" aria-label="Expand sidebar">
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          )}

          {/* Navigation */}
          <nav className="flex-1 overflow-y-auto px-2 py-1">
            {navigation.map((section) => {
              const visibleItems = section.items.filter(canAccess);
              if (visibleItems.length === 0) return null;
              return (
                <div key={section.titleKey} className="mb-1">
                  {!collapsed && (
                    <p className="mb-1 px-3 pt-3 pb-1 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                      {t(section.titleKey)}
                    </p>
                  )}
                  {collapsed && <div className="my-1 mx-2 border-t" />}
                  <div className="space-y-0.5">
                    {visibleItems.map((item) => {
                      const active = isActive(item.href);
                      const Icon = item.icon;
                      const label = t(item.labelKey);
                      return (
                        <Link
                          key={item.href}
                          href={item.href}
                          title={collapsed ? label : undefined}
                          onClick={handleNavClick}
                          className={cn(
                            "flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors",
                            active
                              ? "bg-primary text-primary-foreground"
                              : "text-muted-foreground hover:bg-accent/50 hover:text-foreground"
                          )}
                        >
                          <Icon className="h-[18px] w-[18px] flex-shrink-0" />
                          {!collapsed && <span>{label}</span>}
                        </Link>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </nav>

          {/* Alerts */}
          {canAccess({ href: "/alerts", labelKey: "nav.alerts", icon: Bell, permission: "KLC.Alerts" }) && (
          <div className="border-t px-2 py-1.5">
            <Link
              href="/alerts"
              onClick={handleNavClick}
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
              {!collapsed && <span>{t("nav.alerts")}</span>}
            </Link>
          </div>
          )}

          {/* User section */}
          <div className="border-t px-2 py-2">
            {user && !collapsed && (
              <div className="mb-1.5 px-3 py-1.5">
                <p className="text-sm font-medium">{user.username}</p>
                <p className="text-xs text-muted-foreground">{user.role}</p>
              </div>
            )}
            <div className="space-y-0.5">
              <Link
                href="/settings"
                onClick={handleNavClick}
                className={cn(
                  "flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors",
                  pathname === "/settings"
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-accent/50 hover:text-foreground"
                )}
              >
                <Settings className="h-[18px] w-[18px] flex-shrink-0" />
                {!collapsed && <span>{t("nav.settings")}</span>}
              </Link>
              <button
                onClick={() => { handleNavClick(); logout(); router.push("/login"); }}
                className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent/50 hover:text-foreground"
              >
                <LogOut className="h-[18px] w-[18px] flex-shrink-0" />
                {!collapsed && <span>{t("nav.logout")}</span>}
              </button>
            </div>
          </div>
        </div>
      </aside>
    </>
  );
}
