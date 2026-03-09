import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { StatCard } from "../stat-card";
import { Zap } from "lucide-react";

describe("StatCard", () => {
  it("renders label and value", () => {
    render(<StatCard label="Active Sessions" value={42} />);
    expect(screen.getByText("Active Sessions")).toBeInTheDocument();
    expect(screen.getByText("42")).toBeInTheDocument();
  });

  it("renders string value", () => {
    render(<StatCard label="Revenue" value="1.200.000đ" />);
    expect(screen.getByText("Revenue")).toBeInTheDocument();
    expect(screen.getByText("1.200.000đ")).toBeInTheDocument();
  });

  it("renders an icon when provided", () => {
    const { container } = render(
      <StatCard label="Charging" value={5} icon={Zap} />
    );
    const svg = container.querySelector("svg");
    expect(svg).toBeInTheDocument();
  });

  it("renders trend with green color for positive (up) direction", () => {
    render(
      <StatCard
        label="Sessions"
        value={100}
        trend={{ value: 12, direction: "up" }}
      />
    );
    const trendEl = screen.getByText("12%");
    expect(trendEl).toBeInTheDocument();
    // The parent wrapper should have green color class
    const wrapper = trendEl.closest("div");
    expect(wrapper?.className).toContain("text-green-600");
  });

  it("renders trend with red color for negative (down) direction", () => {
    render(
      <StatCard
        label="Sessions"
        value={80}
        trend={{ value: -5, direction: "down" }}
      />
    );
    const trendEl = screen.getByText("5%");
    expect(trendEl).toBeInTheDocument();
    const wrapper = trendEl.closest("div");
    expect(wrapper?.className).toContain("text-red-600");
  });

  it("renders trend label when provided", () => {
    render(
      <StatCard
        label="Sessions"
        value={100}
        trend={{ value: 10, direction: "up", label: "vs last week" }}
      />
    );
    expect(screen.getByText("vs last week")).toBeInTheDocument();
  });

  it("fires onClick handler when clicked", () => {
    const handleClick = vi.fn();
    render(
      <StatCard label="Clickable" value={1} onClick={handleClick} />
    );
    const card = screen.getByText("Clickable").closest("[class*='cursor-pointer']");
    expect(card).toBeInTheDocument();
    fireEvent.click(card!);
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it("does not apply cursor-pointer when onClick is not provided", () => {
    const { container } = render(
      <StatCard label="Static" value={0} />
    );
    // The root Card element should NOT have cursor-pointer
    const card = container.firstElementChild as HTMLElement;
    expect(card.className).not.toContain("cursor-pointer");
  });

  it("renders children content", () => {
    render(
      <StatCard label="With Children" value={0}>
        <span data-testid="child">Extra info</span>
      </StatCard>
    );
    expect(screen.getByTestId("child")).toBeInTheDocument();
    expect(screen.getByText("Extra info")).toBeInTheDocument();
  });

  it("applies custom className", () => {
    const { container } = render(
      <StatCard label="Custom" value={0} className="my-stat-class" />
    );
    const card = container.firstElementChild as HTMLElement;
    expect(card.className).toContain("my-stat-class");
  });
});
