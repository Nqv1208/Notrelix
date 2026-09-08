import { describe, expect, it } from "vitest";
import {
  UI_EVIDENCE_THEME_IDS,
  UI_EVIDENCE_VIEWPORT_SIZES,
  isUiEvidenceThemeId,
  isUiEvidenceViewportId,
  requireViewportSize,
} from "./ui-visual-registry";

describe("UI evidence visual registry", () => {
  it("contains exactly the canonical viewport ids and sizes (TST-090)", () => {
    expect(Object.keys(UI_EVIDENCE_VIEWPORT_SIZES).sort()).toEqual([
      "desktop",
      "mobile",
      "tablet",
    ]);
    expect(UI_EVIDENCE_VIEWPORT_SIZES.mobile).toEqual({
      width: 390,
      height: 844,
    });
    expect(UI_EVIDENCE_VIEWPORT_SIZES.tablet).toEqual({
      width: 768,
      height: 1024,
    });
    expect(UI_EVIDENCE_VIEWPORT_SIZES.desktop).toEqual({
      width: 1440,
      height: 900,
    });
  });

  it("contains exactly light/dark theme ids and rejects unknown ids (TST-091)", () => {
    expect([...UI_EVIDENCE_THEME_IDS].sort()).toEqual(["dark", "light"]);
    expect(isUiEvidenceThemeId("light")).toBe(true);
    expect(isUiEvidenceThemeId("dark")).toBe(true);
    expect(isUiEvidenceThemeId("midnight")).toBe(false);
    expect(isUiEvidenceThemeId("")).toBe(false);
  });

  it("resolves canonical viewport sizes and rejects unknown viewports", () => {
    expect(requireViewportSize("desktop")).toEqual({
      width: 1440,
      height: 900,
    });
    expect(isUiEvidenceViewportId("mobile")).toBe(true);
    expect(isUiEvidenceViewportId("wide")).toBe(false);
    expect(() => requireViewportSize("wide")).toThrow(
      "Unknown UI evidence viewport id: wide",
    );
  });
});
