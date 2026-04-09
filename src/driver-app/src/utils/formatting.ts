/**
 * Shared formatting utilities for the Driver App.
 * All date functions use Asia/Ho_Chi_Minh timezone.
 */

const TIMEZONE = 'Asia/Ho_Chi_Minh';

/**
 * Parse a UTC date string, appending 'Z' if missing.
 * Backend (Npgsql legacy mode) may return timestamps without the Z suffix.
 */
export function parseUtc(dateString: string): Date {
  return new Date(dateString.endsWith('Z') ? dateString : dateString + 'Z');
}

/** Format amount as VND with dong suffix: "1.000d" */
export function formatCurrency(amount: number): string {
  return (
    new Intl.NumberFormat('vi-VN', {
      style: 'decimal',
      maximumFractionDigits: 0,
    }).format(amount) + '\u0111'
  );
}

/** Format date string as dd/MM/yyyy in VN timezone */
export function formatDate(dateString: string): string {
  return parseUtc(dateString).toLocaleDateString('vi-VN', {
    timeZone: TIMEZONE,
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}

/** Format date string as dd/MM/yyyy HH:mm in VN timezone */
export function formatDateTime(dateString: string): string {
  const d = parseUtc(dateString);
  const date = d.toLocaleDateString('vi-VN', {
    timeZone: TIMEZONE,
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
  const time = d.toLocaleTimeString('vi-VN', {
    timeZone: TIMEZONE,
    hour: '2-digit',
    minute: '2-digit',
  });
  return `${date} ${time}`;
}

/** Format date string as HH:mm in VN timezone */
export function formatTime(dateString: string): string {
  return parseUtc(dateString).toLocaleTimeString('vi-VN', {
    timeZone: TIMEZONE,
    hour: '2-digit',
    minute: '2-digit',
  });
}

/** Format duration in minutes as "Xh Ym" or "Ym" */
export function formatDuration(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  if (hours > 0) {
    return `${hours}h ${mins}m`;
  }
  return `${mins}m`;
}
