"use client";

import { useEffect, useState, useRef, useCallback } from "react";
import { useRouter, usePathname } from "next/navigation";
import { Menu } from "lucide-react";
import { Sidebar } from "@/components/layout/sidebar";
import { useSidebarStore, useAuthStore } from "@/lib/store";
import { authApi } from "@/lib/api";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const { isCollapsed, setMobileOpen } = useSidebarStore();
  const { isAuthenticated, permissions, setPermissions } = useAuthStore();
  const [hydrated, setHydrated] = useState(false);
  const permsFetched = useRef(false);

  const refreshPermissions = useCallback(() => {
    if (!isAuthenticated) return;
    authApi.getMyPermissions()
      .then(({ data }) => setPermissions(data))
      .catch(() => {});
  }, [isAuthenticated, setPermissions]);

  useEffect(() => {
    setHydrated(true);
  }, []);

  useEffect(() => {
    if (hydrated && !isAuthenticated) {
      router.push(`/login?returnUrl=${encodeURIComponent(pathname)}`);
    }
  }, [hydrated, isAuthenticated, router, pathname]);

  // Load current user's permissions once after auth
  useEffect(() => {
    if (hydrated && isAuthenticated && !permsFetched.current && permissions.length === 0) {
      permsFetched.current = true;
      refreshPermissions();
    }
  }, [hydrated, isAuthenticated, permissions.length, refreshPermissions]);

  // Refresh permissions when tab regains focus (catches role changes by admin)
  useEffect(() => {
    const onVisibilityChange = () => {
      if (document.visibilityState === "visible" && isAuthenticated) {
        refreshPermissions();
      }
    };
    document.addEventListener("visibilitychange", onVisibilityChange);
    return () => document.removeEventListener("visibilitychange", onVisibilityChange);
  }, [isAuthenticated, refreshPermissions]);

  if (!hydrated || !isAuthenticated) {
    return null;
  }

  return (
    <div className="min-h-screen bg-background">
      <a href="#main-content" className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-50 focus:rounded-lg focus:bg-primary focus:px-4 focus:py-2 focus:text-primary-foreground">
        Skip to content
      </a>
      <Sidebar />

      {/* Mobile top bar with hamburger toggle */}
      <div className="sticky top-0 z-30 flex h-14 items-center border-b bg-background/95 px-4 backdrop-blur supports-[backdrop-filter]:bg-background/60 md:hidden">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => setMobileOpen(true)}
          aria-label="Open navigation menu"
        >
          <Menu className="h-5 w-5" />
        </Button>
      </div>

      <main
        id="main-content"
        className={cn(
          "transition-all duration-300",
          // No left margin on mobile (sidebar overlays); margin on desktop
          isCollapsed ? "md:ml-16" : "md:ml-64"
        )}
      >
        {children}
      </main>
    </div>
  );
}
