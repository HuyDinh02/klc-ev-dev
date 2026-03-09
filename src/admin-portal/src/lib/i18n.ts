import { create } from "zustand";
import { persist } from "zustand/middleware";
import vi from "@/locales/vi.json";
import en from "@/locales/en.json";

export type Locale = "vi" | "en";

const messages: Record<Locale, Record<string, unknown>> = { vi, en };

interface I18nState {
  locale: Locale;
  setLocale: (locale: Locale) => void;
}

export const useI18nStore = create<I18nState>()(
  persist(
    (set) => ({
      locale: "en",
      setLocale: (locale) => set({ locale }),
    }),
    { name: "i18n-storage" }
  )
);

/**
 * Resolve a dot-path key like "nav.dashboard" from the locale messages.
 */
function resolve(obj: Record<string, unknown>, path: string): string {
  const parts = path.split(".");
  let current: unknown = obj;
  for (const part of parts) {
    if (current && typeof current === "object" && part in current) {
      current = (current as Record<string, unknown>)[part];
    } else {
      return path; // fallback to key if not found
    }
  }
  return typeof current === "string" ? current : path;
}

/**
 * Translation hook. Returns `t` function bound to the current locale.
 *
 * Usage:
 *   const { t, locale, setLocale } = useTranslation();
 *   t("nav.dashboard") // → "Dashboard" or "Tổng quan"
 *   t("common.showing") // → "Showing" or "Hiển thị"
 */
export function useTranslation() {
  const { locale, setLocale } = useI18nStore();
  const t = (key: string): string => resolve(messages[locale], key);
  return { t, locale, setLocale };
}
