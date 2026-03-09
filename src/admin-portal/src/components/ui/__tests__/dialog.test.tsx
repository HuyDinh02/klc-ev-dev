import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { Dialog, DialogHeader, DialogContent, DialogFooter } from "../dialog";

describe("Dialog", () => {
  let onClose: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    onClose = vi.fn();
    // Reset body overflow before each test
    document.body.style.overflow = "";
  });

  afterEach(() => {
    // Clean up body overflow after each test
    document.body.style.overflow = "";
  });

  it("renders content when open=true", () => {
    render(
      <Dialog open={true} onClose={onClose} title="Test Dialog">
        <p>Dialog content here</p>
      </Dialog>
    );
    expect(screen.getByText("Dialog content here")).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });

  it("does not render when open=false", () => {
    render(
      <Dialog open={false} onClose={onClose}>
        <p>Hidden content</p>
      </Dialog>
    );
    expect(screen.queryByText("Hidden content")).not.toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("calls onClose when clicking the backdrop", () => {
    render(
      <Dialog open={true} onClose={onClose}>
        <p>Dialog body</p>
      </Dialog>
    );
    // The backdrop is the first child div with bg-black/50 class
    const backdrop = document.querySelector('[aria-hidden="true"]');
    expect(backdrop).toBeInTheDocument();
    fireEvent.click(backdrop!);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("calls onClose when Escape key is pressed", () => {
    render(
      <Dialog open={true} onClose={onClose}>
        <p>Dialog body</p>
      </Dialog>
    );
    fireEvent.keyDown(window, { key: "Escape" });
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("does not call onClose on Escape when dialog is closed", () => {
    render(
      <Dialog open={false} onClose={onClose}>
        <p>Hidden</p>
      </Dialog>
    );
    fireEvent.keyDown(window, { key: "Escape" });
    expect(onClose).not.toHaveBeenCalled();
  });

  it("sets body overflow to hidden when open", () => {
    render(
      <Dialog open={true} onClose={onClose}>
        <p>Content</p>
      </Dialog>
    );
    expect(document.body.style.overflow).toBe("hidden");
  });

  it("restores body overflow when unmounted", () => {
    const { unmount } = render(
      <Dialog open={true} onClose={onClose}>
        <p>Content</p>
      </Dialog>
    );
    expect(document.body.style.overflow).toBe("hidden");
    unmount();
    expect(document.body.style.overflow).toBe("");
  });

  it("sets aria-modal and aria-label attributes", () => {
    render(
      <Dialog open={true} onClose={onClose} title="Confirm Action">
        <p>Are you sure?</p>
      </Dialog>
    );
    const dialog = screen.getByRole("dialog");
    expect(dialog).toHaveAttribute("aria-modal", "true");
    expect(dialog).toHaveAttribute("aria-label", "Confirm Action");
  });

  it("applies size classes correctly", () => {
    const { container, rerender } = render(
      <Dialog open={true} onClose={onClose} size="sm">
        <p>Small</p>
      </Dialog>
    );
    expect(container.querySelector(".max-w-sm")).toBeInTheDocument();

    rerender(
      <Dialog open={true} onClose={onClose} size="xl">
        <p>Extra large</p>
      </Dialog>
    );
    expect(container.querySelector(".max-w-xl")).toBeInTheDocument();
  });

  it("applies custom className", () => {
    const { container } = render(
      <Dialog open={true} onClose={onClose} className="my-dialog-class">
        <p>Custom</p>
      </Dialog>
    );
    const dialogPanel = container.querySelector(".my-dialog-class");
    expect(dialogPanel).toBeInTheDocument();
  });
});

describe("DialogHeader", () => {
  it("renders children text", () => {
    render(<DialogHeader>My Dialog Title</DialogHeader>);
    expect(screen.getByText("My Dialog Title")).toBeInTheDocument();
  });

  it("renders close button when onClose is provided", () => {
    const onClose = vi.fn();
    render(<DialogHeader onClose={onClose}>Title</DialogHeader>);
    const closeBtn = screen.getByLabelText("Close");
    expect(closeBtn).toBeInTheDocument();
    fireEvent.click(closeBtn);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("does not render close button when onClose is not provided", () => {
    render(<DialogHeader>Title</DialogHeader>);
    expect(screen.queryByLabelText("Close")).not.toBeInTheDocument();
  });
});

describe("DialogContent", () => {
  it("renders children", () => {
    render(
      <DialogContent>
        <p>Content body</p>
      </DialogContent>
    );
    expect(screen.getByText("Content body")).toBeInTheDocument();
  });
});

describe("DialogFooter", () => {
  it("renders children", () => {
    render(
      <DialogFooter>
        <button>Cancel</button>
        <button>Confirm</button>
      </DialogFooter>
    );
    expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Confirm" })).toBeInTheDocument();
  });
});
