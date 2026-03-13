"use client";

import { Bell, Search, User, Sun, Moon, Monitor } from "lucide-react";
import { useState, useRef, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { useAlertsStore, useThemeStore } from "@/lib/store";
import { useTranslation } from "@/lib/i18n";
import Link from "next/link";

interface HeaderProps {
  title: string;
  description?: string;
}

function ThemeToggle() {
  const { theme, setTheme } = useThemeStore();
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  const options: { value: "light" | "dark" | "system"; icon: typeof Sun; label: string }[] = [
    { value: "light", icon: Sun, label: t("theme.light") },
    { value: "dark", icon: Moon, label: t("theme.dark") },
    { value: "system", icon: Monitor, label: t("theme.system") },
  ];

  const current = options.find((o) => o.value === theme) ?? options[2];
  const Icon = current.icon;

  return (
    <div className="relative" ref={ref}>
      <Button variant="ghost" size="icon" onClick={() => setOpen(!open)} aria-label={t("theme.toggle")}>
        <Icon className="h-5 w-5" />
      </Button>
      {open && (
        <div className="absolute right-0 top-full mt-1 w-36 rounded-md border bg-popover p-1 shadow-md z-50">
          {options.map((opt) => {
            const OptIcon = opt.icon;
            return (
              <button
                key={opt.value}
                className={`flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm transition-colors hover:bg-muted ${theme === opt.value ? "bg-muted font-medium" : ""}`}
                onClick={() => { setTheme(opt.value); setOpen(false); }}
              >
                <OptIcon className="h-4 w-4" />
                {opt.label}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

export function Header({ title, description }: HeaderProps) {
  const { unreadCount } = useAlertsStore();
  const { t } = useTranslation();

  return (
    <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b bg-background/95 px-6 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div>
        <h1 className="text-xl font-semibold">{title}</h1>
        {description && <p className="text-sm text-muted-foreground">{description}</p>}
      </div>

      <div className="flex items-center gap-2">
        {/* Search */}
        <div className="relative hidden md:block">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="search"
            placeholder={t("header.searchPlaceholder")}
            className="h-9 w-64 rounded-md border bg-background pl-9 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>

        {/* Theme toggle */}
        <ThemeToggle />

        {/* Alerts */}
        <Link href="/alerts">
          <Button variant="ghost" size="icon" className="relative">
            <Bell className="h-5 w-5" />
            {unreadCount > 0 && (
              <span className="absolute -right-1 -top-1 flex h-5 w-5 items-center justify-center rounded-full bg-destructive text-xs text-white">
                {unreadCount > 9 ? "9+" : unreadCount}
              </span>
            )}
          </Button>
        </Link>

        {/* User menu */}
        <Button variant="ghost" size="icon">
          <User className="h-5 w-5" />
        </Button>
      </div>
    </header>
  );
}
