import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { EmptyState } from "../empty-state";
import { Search, AlertTriangle } from "lucide-react";

describe("EmptyState", () => {
  it("renders title", () => {
    render(<EmptyState title="No results found" />);
    expect(screen.getByText("No results found")).toBeInTheDocument();
  });

  it("renders title and description", () => {
    render(
      <EmptyState
        title="No stations"
        description="Get started by adding a new station"
      />
    );
    expect(screen.getByText("No stations")).toBeInTheDocument();
    expect(
      screen.getByText("Get started by adding a new station")
    ).toBeInTheDocument();
  });

  it("does not render description when not provided", () => {
    const { container } = render(<EmptyState title="Empty" />);
    // Only the title h3 and icon should be present; no <p> for description
    const paragraphs = container.querySelectorAll("p");
    expect(paragraphs).toHaveLength(0);
  });

  it("renders the default Inbox icon when no icon is provided", () => {
    const { container } = render(<EmptyState title="Default Icon" />);
    // The default icon is Inbox - it renders an SVG
    const svg = container.querySelector("svg");
    expect(svg).toBeInTheDocument();
  });

  it("renders a custom icon when provided", () => {
    const { container } = render(
      <EmptyState title="Search" icon={Search} />
    );
    const svg = container.querySelector("svg");
    expect(svg).toBeInTheDocument();
  });

  it("renders action button when action is provided", () => {
    const handleClick = vi.fn();
    render(
      <EmptyState
        title="No data"
        action={{ label: "Add Item", onClick: handleClick }}
      />
    );
    const button = screen.getByRole("button", { name: "Add Item" });
    expect(button).toBeInTheDocument();
  });

  it("fires action button click handler", () => {
    const handleClick = vi.fn();
    render(
      <EmptyState
        title="No data"
        action={{ label: "Create New", onClick: handleClick }}
      />
    );
    const button = screen.getByRole("button", { name: "Create New" });
    fireEvent.click(button);
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it("does not render action button when action is not provided", () => {
    render(<EmptyState title="No action" />);
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("applies custom className", () => {
    const { container } = render(
      <EmptyState title="Custom" className="my-empty-class" />
    );
    const root = container.firstElementChild as HTMLElement;
    expect(root.className).toContain("my-empty-class");
  });

  it("renders with all props combined", () => {
    const handleClick = vi.fn();
    render(
      <EmptyState
        icon={AlertTriangle}
        title="Something went wrong"
        description="Please try again later"
        action={{ label: "Retry", onClick: handleClick }}
        className="error-state"
      />
    );
    expect(screen.getByText("Something went wrong")).toBeInTheDocument();
    expect(screen.getByText("Please try again later")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Retry" })).toBeInTheDocument();
  });
});
