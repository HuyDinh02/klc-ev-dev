"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  MapPin,
  Activity,
  Zap,
  DollarSign,
  AlertTriangle,
  Wrench,
  FolderTree,
  FileText,
  Receipt,
  Bell,
  ChevronLeft,
  ChevronRight,
  Settings,
  LogOut,
  Users,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { cn } from "@/lib/utils";
import { useSidebarStore, useAuthStore, useAlertsStore } from "@/lib/store";
import { Button } from "@/components/ui/button";

const menuItems = [
  { href: "/", label: "Dashboard", icon: LayoutDashboard },
  { href: "/stations", label: "Stations", icon: MapPin },
  { href: "/monitoring", label: "Monitoring", icon: Activity },
  { href: "/sessions", label: "Sessions", icon: Zap },
  { href: "/tariffs", label: "Tariffs", icon: DollarSign },
  { href: "/payments", label: "Payments", icon: Receipt },
  { href: "/faults", label: "Faults", icon: AlertTriangle },
  { href: "/maintenance", label: "Maintenance", icon: Wrench },
  { href: "/groups", label: "Station Groups", icon: FolderTree },
  { href: "/audit-logs", label: "Audit Logs", icon: FileText },
  { href: "/e-invoices", label: "E-Invoices", icon: Receipt },
  { href: "/user-management", label: "User Management", icon: Users },
];

export function Sidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const { isCollapsed, toggle } = useSidebarStore();
  const { logout, user } = useAuthStore();
  const { unreadCount } = useAlertsStore();

  return (
    <aside
      className={cn(
        "fixed left-0 top-0 z-40 h-screen border-r bg-card transition-all duration-300",
        isCollapsed ? "w-16" : "w-64"
      )}
    >
      <div className="flex h-full flex-col">
        {/* Logo */}
        <div className="flex h-16 items-center justify-between border-b px-4">
          {!isCollapsed && (
            <Link href="/" className="flex items-center gap-2">
              <Zap className="h-6 w-6 text-primary" />
              <span className="text-lg font-bold">KLC</span>
            </Link>
          )}
          <Button variant="ghost" size="icon" onClick={toggle} className="ml-auto">
            {isCollapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
          </Button>
        </div>

        {/* Navigation */}
        <nav className="flex-1 space-y-1 overflow-y-auto p-2">
          {menuItems.map((item) => {
            const isActive = item.href === "/"
              ? pathname === "/"
              : pathname === item.href || pathname.startsWith(`${item.href}/`);
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors",
                  isActive
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                )}
              >
                <Icon className="h-5 w-5 flex-shrink-0" />
                {!isCollapsed && <span>{item.label}</span>}
              </Link>
            );
          })}
        </nav>

        {/* Alerts */}
        <div className="border-t p-2">
          <Link
            href="/alerts"
            className={cn(
              "flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors",
              pathname === "/alerts"
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
            )}
          >
            <div className="relative">
              <Bell className="h-5 w-5 flex-shrink-0" />
              {unreadCount > 0 && (
                <span className="absolute -right-1 -top-1 flex h-4 w-4 items-center justify-center rounded-full bg-destructive text-[10px] text-white">
                  {unreadCount > 9 ? "9+" : unreadCount}
                </span>
              )}
            </div>
            {!isCollapsed && <span>Alerts</span>}
          </Link>
        </div>

        {/* User section */}
        <div className="border-t p-2">
          {user && !isCollapsed && (
            <div className="mb-2 px-3 py-2">
              <p className="text-sm font-medium">{user.username}</p>
              <p className="text-xs text-muted-foreground">{user.role}</p>
            </div>
          )}
          <Link
            href="/settings"
            className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
          >
            <Settings className="h-5 w-5 flex-shrink-0" />
            {!isCollapsed && <span>Settings</span>}
          </Link>
          <button
            onClick={() => { logout(); router.push("/login"); }}
            className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
          >
            <LogOut className="h-5 w-5 flex-shrink-0" />
            {!isCollapsed && <span>Logout</span>}
          </button>
        </div>
      </div>
    </aside>
  );
}
