import { describe, it, expect, vi } from "vitest";
import { act, render, screen } from "@testing-library/react";
import { NotrelixBrandLoader } from "../components/brand/notrelix-logo";
import { Button } from "../components/ui/button";

describe("UI Web Component contract", () => {
  it("renders Button component with variant data attributes", () => {
    render(<Button variant="outline">Click Me</Button>);
    const button = screen.getByRole("button", { name: "Click Me" });
    expect(button).toBeDefined();
    expect(button.getAttribute("data-variant")).toBe("outline");
  });

  it("supports rendering asChild using Slot", () => {
    render(
      <Button asChild>
        <a href="/test">Link Button</a>
      </Button>,
    );
    const link = screen.getByRole("link", { name: "Link Button" });
    expect(link).toBeDefined();
    expect(link.getAttribute("data-slot")).toBe("button");
  });

  it("delays the branded loading animation to avoid short-loading flicker", () => {
    vi.useFakeTimers();

    try {
      render(<NotrelixBrandLoader aria-label="Loading workspace" />);

      expect(screen.queryByRole("status")).toBeNull();

      act(() => {
        vi.advanceTimersByTime(250);
      });

      const loader = screen.getByRole("status", { name: "Loading workspace" });
      expect(loader.getAttribute("data-slot")).toBe("notrelix-brand-loader");
      expect(
        loader.querySelector('animate[attributeName="stroke-dashoffset"]'),
      ).not.toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });
});
