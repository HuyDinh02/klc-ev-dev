"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { Sidebar } from "@/components/layout/sidebar";
import { useSidebarStore, useAuthStore } from "@/lib/store";
import { cn } from "@/lib/utils";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const { isCollapsed } = useSidebarStore();
  const { isAuthenticated } = useAuthStore();

  useEffect(() => {
    // For development, skip auth check
    // In production, uncomment this:
    // if (!isAuthenticated) {
    //   router.push("/login");
    // }
  }, [isAuthenticated, router]);

  return (
    <div className="min-h-screen bg-background">
      <Sidebar />
      <main
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
