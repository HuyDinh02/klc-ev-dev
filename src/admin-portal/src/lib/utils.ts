import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatCurrency(amount?: number | null): string {
  return new Intl.NumberFormat("vi-VN", {
    style: "decimal",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount ?? 0) + "đ";
}

/**
 * Parse a date string as UTC. Backend returns timestamps without Z suffix
 * (Npgsql legacy mode), so we append Z if missing to ensure UTC interpretation.
 */
function parseAsUtc(date: string | Date): Date {
  if (date instanceof Date) return date;
  // If string lacks timezone info, treat as UTC by appending Z
  if (!date.endsWith("Z") && !date.includes("+") && !/\d{2}:\d{2}$/.test(date.slice(-6))) {
    return new Date(date + "Z");
  }
  return new Date(date);
}

export function formatDate(date?: string | Date | null): string {
  if (!date) return "—";
  return new Intl.DateTimeFormat("vi-VN", {
    timeZone: "Asia/Ho_Chi_Minh",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(parseAsUtc(date));
}

export function formatDateTime(date?: string | Date | null): string {
  if (!date) return "—";
  return new Intl.DateTimeFormat("vi-VN", {
    timeZone: "Asia/Ho_Chi_Minh",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(parseAsUtc(date));
}

export function formatEnergy(kwh?: number | null): string {
  return `${(kwh ?? 0).toFixed(2)} kWh`;
}

export function formatDuration(minutes?: number | null): string {
  const m = minutes ?? 0;
  const hours = Math.floor(m / 60);
  const mins = m % 60;
  const secs = Math.round((m - Math.floor(m)) * 60);
  return `${String(hours).padStart(2, "0")}:${String(mins).padStart(2, "0")}:${String(secs).padStart(2, "0")}`;
}

export function formatDurationFromSeconds(seconds?: number | null): string {
  const s = seconds ?? 0;
  const hours = Math.floor(s / 3600);
  const mins = Math.floor((s % 3600) / 60);
  const secs = s % 60;
  return `${String(hours).padStart(2, "0")}:${String(mins).padStart(2, "0")}:${String(secs).padStart(2, "0")}`;
}

export { parseAsUtc };

export function formatDistanceToNow(date?: string | Date | null): string {
  if (!date) return "—";
  const now = Date.now();
  const then = parseAsUtc(date).getTime();
  const diffMs = now - then;
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return "Just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHour = Math.floor(diffMin / 60);
  if (diffHour < 24) return `${diffHour}h ago`;
  const diffDay = Math.floor(diffHour / 24);
  return `${diffDay}d ago`;
}

/**
 * Download data as a CSV file. Escapes values containing commas, quotes, or newlines.
 */
export function downloadCsv(
  headers: string[],
  rows: string[][],
  filename: string,
) {
  const escape = (v: string) => {
    if (v.includes(",") || v.includes('"') || v.includes("\n")) {
      return `"${v.replace(/"/g, '""')}"`;
    }
    return v;
  };
  const csv = [
    headers.map(escape).join(","),
    ...rows.map((row) => row.map(escape).join(",")),
  ].join("\n");
  const blob = new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}
