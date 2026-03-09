import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { StatusBadge, StatusDot, getStatusConfig } from "../status-badge";
import {
  CONNECTOR_STATUS,
  STATION_STATUS,
  SESSION_STATUS,
  PAYMENT_STATUS,
  FAULT_SEVERITY,
  FAULT_STATUS,
  MAINTENANCE_STATUS,
  EINVOICE_STATUS,
} from "@/lib/constants";

describe("StatusBadge", () => {
  // --- Renders correct label for each status type ---

  it("renders correct label for connector statuses", () => {
    for (const [value, config] of Object.entries(CONNECTOR_STATUS)) {
      const { unmount } = render(
        <StatusBadge type="connector" value={Number(value)} />
      );
      expect(screen.getByText(config.label)).toBeInTheDocument();
      unmount();
    }
  });

  it("renders correct label for station statuses", () => {
    for (const [value, config] of Object.entries(STATION_STATUS)) {
      const { unmount } = render(
        <StatusBadge type="station" value={Number(value)} />
      );
      expect(screen.getByText(config.label)).toBeInTheDocument();
      unmount();
    }
  });

  it("renders correct label for session statuses", () => {
    for (const [value, config] of Object.entries(SESSION_STATUS)) {
      const { unmount } = render(
        <StatusBadge type="session" value={Number(value)} />
      );
      expect(screen.getByText(config.label)).toBeInTheDocument();
      unmount();
    }
  });

  it("renders correct label for payment statuses", () => {
    for (const [value, config] of Object.entries(PAYMENT_STATUS)) {
      const { unmount } = render(
        <StatusBadge type="payment" value={Number(value)} />
      );
      expect(screen.getByText(config.label)).toBeInTheDocument();
      unmount();
    }
  });

  it("renders correct label for fault severity", () => {
    for (const [value, config] of Object.entries(FAULT_SEVERITY)) {
      const { unmount } = render(
        <StatusBadge type="faultSeverity" value={Number(value)} />
      );
      expect(screen.getByText(config.label)).toBeInTheDocument();
      unmount();
    }
  });

  it("renders correct label for fault statuses", () => {
    for (const [value, config] of Object.entries(FAULT_STATUS)) {
      const { unmount } = render(
        <StatusBadge type="faultStatus" value={Number(value)} />
      );
      expect(screen.getByText(config.label)).toBeInTheDocument();
      unmount();
    }
  });

  it("renders correct label for maintenance statuses", () => {
    for (const [value, config] of Object.entries(MAINTENANCE_STATUS)) {
      const { unmount } = render(
        <StatusBadge type="maintenance" value={Number(value)} />
      );
      expect(screen.getByText(config.label)).toBeInTheDocument();
      unmount();
    }
  });

  it("renders correct label for eInvoice statuses", () => {
    for (const [value, config] of Object.entries(EINVOICE_STATUS)) {
      const { unmount } = render(
        <StatusBadge type="eInvoice" value={Number(value)} />
      );
      expect(screen.getByText(config.label)).toBeInTheDocument();
      unmount();
    }
  });

  // --- Badge variant (CSS class) tests ---

  it("applies success badge variant for Available connector (0)", () => {
    const { container } = render(
      <StatusBadge type="connector" value={0} />
    );
    // The Badge component uses "bg-green-500" for success variant
    const badge = container.firstElementChild as HTMLElement;
    expect(badge.className).toContain("bg-green-500");
  });

  it("applies warning badge variant for Preparing connector (1)", () => {
    const { container } = render(
      <StatusBadge type="connector" value={1} />
    );
    const badge = container.firstElementChild as HTMLElement;
    expect(badge.className).toContain("bg-amber-500");
  });

  it("applies info badge variant for Charging connector (2)", () => {
    const { container } = render(
      <StatusBadge type="connector" value={2} />
    );
    const badge = container.firstElementChild as HTMLElement;
    expect(badge.className).toContain("bg-blue-500");
  });

  it("applies destructive badge variant for Faulted connector (8)", () => {
    const { container } = render(
      <StatusBadge type="connector" value={8} />
    );
    const badge = container.firstElementChild as HTMLElement;
    expect(badge.className).toContain("bg-destructive");
  });

  it("applies secondary badge variant for unknown status values", () => {
    const { container } = render(
      <StatusBadge type="connector" value={999} />
    );
    expect(screen.getByText("Unknown")).toBeInTheDocument();
    const badge = container.firstElementChild as HTMLElement;
    expect(badge.className).toContain("bg-secondary");
  });

  // --- Custom className ---

  it("renders with custom className", () => {
    const { container } = render(
      <StatusBadge type="connector" value={0} className="my-custom-class" />
    );
    const badge = container.firstElementChild as HTMLElement;
    expect(badge.className).toContain("my-custom-class");
  });

  // --- Dot and icon display ---

  it("shows a status dot by default (showDot=true, showIcon=false)", () => {
    const { container } = render(
      <StatusBadge type="connector" value={0} />
    );
    const dot = container.querySelector(".status-dot");
    expect(dot).toBeInTheDocument();
    expect(dot).toHaveStyle({ backgroundColor: "#22C55E" });
  });

  it("hides dot and shows icon when showIcon=true", () => {
    const { container } = render(
      <StatusBadge type="connector" value={0} showIcon />
    );
    const dot = container.querySelector(".status-dot");
    expect(dot).not.toBeInTheDocument();
    // An SVG icon should be rendered
    const svg = container.querySelector("svg");
    expect(svg).toBeInTheDocument();
  });
});

describe("StatusDot", () => {
  it("renders a dot with the correct color", () => {
    const { container } = render(<StatusDot type="station" value={1} />);
    const dot = container.querySelector(".status-dot");
    expect(dot).toBeInTheDocument();
    expect(dot).toHaveStyle({ backgroundColor: "#22C55E" });
  });

  it("renders with pulse class when pulse=true", () => {
    const { container } = render(
      <StatusDot type="station" value={1} pulse />
    );
    const dot = container.querySelector(".status-dot");
    expect(dot).toHaveClass("status-dot-pulse");
  });

  it("returns null for unknown status value", () => {
    const { container } = render(<StatusDot type="station" value={999} />);
    expect(container.firstChild).toBeNull();
  });
});

describe("getStatusConfig", () => {
  it("returns config for valid type and value", () => {
    const config = getStatusConfig("connector", 0);
    expect(config).toBeDefined();
    expect(config!.label).toBe("Available");
    expect(config!.badgeVariant).toBe("success");
  });

  it("returns undefined for invalid value", () => {
    const config = getStatusConfig("connector", 999);
    expect(config).toBeUndefined();
  });
});
