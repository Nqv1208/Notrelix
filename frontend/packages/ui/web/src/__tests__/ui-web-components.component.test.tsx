import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { Button } from "../components/ui/button";
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "../components/ui/tabs";

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

  it("shows a focus ring on a focusable tab panel instead of dropping the outline", () => {
    render(
      <Tabs defaultValue="a">
        <TabsList>
          <TabsTrigger value="a">A</TabsTrigger>
        </TabsList>
        <TabsContent value="a">Panel A</TabsContent>
      </Tabs>,
    );
    const panel = screen.getByRole("tabpanel");
    expect(panel.getAttribute("tabindex")).toBe("0");
    expect(panel.className).toContain("focus-visible:ring-[3px]");
  });
});
