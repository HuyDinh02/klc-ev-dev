import { describe, it, expect, beforeEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useTranslation, useI18nStore } from "../i18n";

describe("useTranslation", () => {
  beforeEach(() => {
    // Reset store to default locale ("en") before each test
    const { setState } = useI18nStore;
    setState({ locale: "en" });
  });

  it("returns a t function, locale, and setLocale", () => {
    const { result } = renderHook(() => useTranslation());
    expect(typeof result.current.t).toBe("function");
    expect(result.current.locale).toBe("en");
    expect(typeof result.current.setLocale).toBe("function");
  });

  it("resolves simple top-level key", () => {
    const { result } = renderHook(() => useTranslation());
    expect(result.current.t("common.save")).toBe("Save");
    expect(result.current.t("common.cancel")).toBe("Cancel");
  });

  it("resolves nested keys (e.g., nav.dashboard)", () => {
    const { result } = renderHook(() => useTranslation());
    expect(result.current.t("nav.dashboard")).toBe("Dashboard");
    expect(result.current.t("nav.stations")).toBe("Stations");
    expect(result.current.t("nav.monitoring")).toBe("Monitoring");
  });

  it("resolves deeply nested keys", () => {
    const { result } = renderHook(() => useTranslation());
    expect(result.current.t("dashboard.activeSessions")).toBe("Active Sessions");
    expect(result.current.t("stations.addStation")).toBe("Add Station");
  });

  it("returns the key itself when key does not exist", () => {
    const { result } = renderHook(() => useTranslation());
    expect(result.current.t("nonexistent.key")).toBe("nonexistent.key");
    expect(result.current.t("nav.nonexistent")).toBe("nav.nonexistent");
  });

  it("returns the key for a completely unknown path", () => {
    const { result } = renderHook(() => useTranslation());
    expect(result.current.t("a.b.c.d.e")).toBe("a.b.c.d.e");
  });

  it("switches locale to Vietnamese", () => {
    const { result } = renderHook(() => useTranslation());

    act(() => {
      result.current.setLocale("vi");
    });

    expect(result.current.locale).toBe("vi");
    // After switching locale, translations should resolve from vi.json
    // The exact value depends on what's in vi.json, but the key
    // should not be returned as-is if it exists in the file
    const translated = result.current.t("common.save");
    expect(translated).not.toBe("common.save");
  });

  it("returns key when resolving a non-leaf node (object, not string)", () => {
    const { result } = renderHook(() => useTranslation());
    // "common" is an object, not a string — should return the key
    expect(result.current.t("common")).toBe("common");
    expect(result.current.t("nav")).toBe("nav");
  });
});

describe("useI18nStore", () => {
  beforeEach(() => {
    useI18nStore.setState({ locale: "en" });
  });

  it("has default locale of en", () => {
    const { result } = renderHook(() => useI18nStore());
    expect(result.current.locale).toBe("en");
  });

  it("can set locale via setLocale", () => {
    const { result } = renderHook(() => useI18nStore());
    act(() => {
      result.current.setLocale("vi");
    });
    expect(result.current.locale).toBe("vi");
  });
});
