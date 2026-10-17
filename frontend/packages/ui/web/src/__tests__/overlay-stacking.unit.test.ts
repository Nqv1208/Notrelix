import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { Z_INDEX_ORDER, zIndex } from "@notrelix/ui-tokens";
import { Z_ROLES } from "../lib/cn";

const ui = (file: string) =>
  readFileSync(resolve(__dirname, "../components/ui", file), "utf8");

/** Class tokens that start a z utility, e.g. `z-modal`, `z-50`. */
const zClasses = (source: string) =>
  [...source.matchAll(/(?<![\w-])z-([a-z0-9[\]-]+)/g)].map((m) => m[1] ?? "");

describe("overlay stacking (WX-QUAL-001)", () => {
  it("cn() knows exactly the token roles, in order", () => {
    expect([...Z_ROLES]).toEqual([...Z_INDEX_ORDER]);
    expect(Z_INDEX_ORDER.map((role) => zIndex[role])).toEqual(
      [...Z_INDEX_ORDER.map((role) => zIndex[role])].sort((a, b) => a - b),
    );
  });

  it("dialogs, alert dialogs, sheets and drawers: backdrop below modal", () => {
    for (const file of [
      "dialog.tsx",
      "alert-dialog.tsx",
      "sheet.tsx",
      "drawer.tsx",
    ]) {
      const classes = zClasses(ui(file));
      expect(classes, file).toContain("backdrop");
      expect(classes, file).toContain("modal");
      expect(
        classes.filter((c) => /^\d/.test(c)),
        file,
      ).toEqual([]);
    }
  });

  it("portalled floating layers share the modal role so a popover opened from a dialog stacks above it", () => {
    for (const file of [
      "popover.tsx",
      "select.tsx",
      "dropdown-menu.tsx",
      "context-menu.tsx",
      "menubar.tsx",
      "hover-card.tsx",
    ]) {
      expect(zClasses(ui(file)), file).toContain("modal");
    }
  });

  it("tooltips are the top role and the toaster uses the toast role", () => {
    expect(zClasses(ui("tooltip.tsx"))).toContain("tooltip");
    expect(ui("sonner.tsx")).toContain('zIndex: "var(--z-toast)"');
  });

  it("the stacking order is dropdown < sticky < backdrop < modal < toast < tooltip", () => {
    expect(zIndex.dropdown).toBeLessThan(zIndex.sticky);
    expect(zIndex.sticky).toBeLessThan(zIndex.backdrop);
    expect(zIndex.backdrop).toBeLessThan(zIndex.modal);
    expect(zIndex.modal).toBeLessThan(zIndex.toast);
    expect(zIndex.toast).toBeLessThan(zIndex.tooltip);
  });
});
