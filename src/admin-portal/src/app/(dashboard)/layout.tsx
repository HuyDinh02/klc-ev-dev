"use client";

import { useEffect, useState, useRef } from "react";
import { useRouter, usePathname } from "next/navigation";
import { Sidebar } from "@/components/layout/sidebar";
import { useSidebarStore, useAuthStore } from "@/lib/store";
import { authApi } from "@/lib/api";
import { cn } from "@/lib/utils";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const { isCollapsed } = useSidebarStore();
  const { isAuthenticated, permissions, setPermissions } = useAuthStore();
  const [hydrated, setHydrated] = useState(false);
  const permsFetched = useRef(false);

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
      authApi.getMyPermissions()
        .then(({ data }) => setPermissions(data))
        .catch(() => { /* permissions will remain empty — pages still accessible, just sidebar won't filter */ });
    }
  }, [hydrated, isAuthenticated, permissions.length, setPermissions]);

  if (!hydrated || !isAuthenticated) {
    return null;
  }

  return (
    <div className="min-h-screen bg-background">
      <a href="#main-content" className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-50 focus:rounded-lg focus:bg-primary focus:px-4 focus:py-2 focus:text-primary-foreground">
        Skip to content
      </a>
      <Sidebar />
      <main
        id="main-content"
        className={cn(
          "transition-all duration-300",
          isCollapsed ? "ml-16" : "ml-64"
        )}
      >
        {children}
      </main>
    </div>
  );
}
