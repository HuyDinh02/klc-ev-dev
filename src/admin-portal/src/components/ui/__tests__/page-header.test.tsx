import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { PageHeader } from "../page-header";

describe("PageHeader", () => {
  it("renders title", () => {
    render(<PageHeader title="Station Management" />);
    expect(
      screen.getByRole("heading", { name: "Station Management" })
    ).toBeInTheDocument();
  });

  it("renders title and description", () => {
    render(
      <PageHeader
        title="Stations"
        description="Manage charging stations and connectors"
      />
    );
    expect(screen.getByRole("heading", { name: "Stations" })).toBeInTheDocument();
    expect(
      screen.getByText("Manage charging stations and connectors")
    ).toBeInTheDocument();
  });

  it("does not render description paragraph when not provided", () => {
    const { container } = render(<PageHeader title="Title Only" />);
    const description = container.querySelector("p");
    expect(description).not.toBeInTheDocument();
  });

  it("renders children (action buttons)", () => {
    render(
      <PageHeader title="Stations">
        <button>Add Station</button>
        <button>Export</button>
      </PageHeader>
    );
    expect(screen.getByRole("button", { name: "Add Station" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Export" })).toBeInTheDocument();
  });

  it("does not render action wrapper when no children provided", () => {
    const { container } = render(<PageHeader title="No Actions" />);
    // The flex container for children should not be rendered
    const actionWrappers = container.querySelectorAll(
      "div > div.flex.items-center.gap-2"
    );
    expect(actionWrappers).toHaveLength(0);
  });

  it("applies custom className to the root container", () => {
    const { container } = render(
      <PageHeader title="Custom" className="sticky-header-class" />
    );
    const root = container.firstElementChild as HTMLElement;
    expect(root.className).toContain("sticky-header-class");
  });

  it("has responsive flex layout classes", () => {
    const { container } = render(<PageHeader title="Layout Test" />);
    const root = container.firstElementChild as HTMLElement;
    expect(root.className).toContain("flex");
    expect(root.className).toContain("flex-col");
    expect(root.className).toContain("sm:flex-row");
  });
});
