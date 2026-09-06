import { beforeAll, describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";

import {
  UiWebDataDisplayPrimitivesSurface,
  UiWebFormControlsSurface,
  UiWebNavigationPrimitivesSurface,
  UiWebOverlayPrimitivesSurface,
  UiWebSubmitStateSurface,
} from "../ui-web-critical-surfaces";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
  globalThis.IntersectionObserver = class IntersectionObserver {
    readonly root = null;
    readonly rootMargin = "";
    readonly thresholds = [];
    disconnect() {}
    observe() {}
    takeRecords() {
      return [];
    }
    unobserve() {}
  };
});

describe("ui-web critical surfaces", () => {
  it("renders submit state variants without application providers", () => {
    render(<UiWebSubmitStateSurface />);

    expect(screen.getByRole("button", { name: "Save changes" })).toBeDefined();
    expect(
      screen.getByRole("button", { name: /Saving changes/ }),
    ).toHaveProperty("disabled", true);
    expect(
      screen.getByRole("button", { name: "Disabled save" }),
    ).toHaveProperty("disabled", true);
  });

  it("supports basic form control interactions", () => {
    render(<UiWebFormControlsSurface />);

    const email = screen.getByLabelText("Email") as HTMLInputElement;
    fireEvent.change(email, { target: { value: "user@example.com" } });
    expect(email.value).toBe("user@example.com");

    const notes = screen.getByLabelText("Notes") as HTMLTextAreaElement;
    fireEvent.change(notes, { target: { value: "Fixture notes" } });
    expect(notes.value).toBe("Fixture notes");

    const checkbox = screen.getByRole("checkbox", { name: "Enabled" });
    fireEvent.click(checkbox);
    expect(checkbox.getAttribute("aria-checked")).toBe("false");

    expect(screen.getByRole("switch", { name: "Notifications" })).toBeDefined();
  });

  it("supports tab navigation primitives", () => {
    render(<UiWebNavigationPrimitivesSurface />);

    expect(screen.getByRole("tab", { name: "Overview" })).toBeDefined();
    expect(screen.getByRole("tab", { name: "Activity" })).toBeDefined();
    fireEvent.click(screen.getByRole("button", { name: /toggle sidebar/i }));
    expect(screen.getAllByText("Boards").length).toBeGreaterThan(0);
  });

  it("opens overlay primitives from deterministic triggers", () => {
    render(<UiWebOverlayPrimitivesSurface />);

    fireEvent.click(screen.getByRole("button", { name: "Open dialog" }));
    expect(screen.getByRole("dialog", { name: "Create board" })).toBeDefined();
  });

  it("supports disclosure and toggle data-display primitives", () => {
    render(<UiWebDataDisplayPrimitivesSurface />);

    fireEvent.click(screen.getByRole("button", { name: "What is tracked?" }));
    expect(screen.getByText("Workspace work items.")).toBeDefined();

    fireEvent.click(screen.getByRole("button", { name: "Toggle details" }));
    expect(screen.getByText("Details content.")).toBeDefined();
  });
});
