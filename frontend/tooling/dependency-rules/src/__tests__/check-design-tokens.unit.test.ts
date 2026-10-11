import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  compareToBaseline,
  countViolations,
  findRawZIndex,
  RAW_Z_INDEX_EXCLUDED,
  scanRawZIndex,
  zIndexScanRoots,
  readBaseline,
  scanDesignTokenUsage,
  scanRoots,
} from "../check-design-tokens";

const frontendRoot = resolve(__dirname, "../../../..");

describe("check-design-tokens rules", () => {
  it("flags hex literals, primitive palette colors and arbitrary text sizes", () => {
    const source = `
      const a = "#6161ff"; const b = '#FFF'; const c = "#00000080";
      <div className="bg-blue-500/10 hover:text-emerald-600 border-l-red-200 text-[11px] text-[0.8rem]" />
    `;
    expect(countViolations(source)).toEqual({
      "hex-color": 3,
      "palette-color": 3,
      "arbitrary-text-size": 2,
    });
  });

  it("ignores semantic tokens, entities, URLs anchors and non-size arbitrary values", () => {
    const source = `
      <a href="/docs#section" className="bg-primary text-muted-foreground text-sm text-[var(--x)] bg-black/50 text-white border-border" />
      const entity = "&#123;"; const id = "#main";
    `;
    expect(countViolations(source)).toEqual({});
  });

  it("flags hex colours followed by an underscore inside Tailwind arbitrary classes", () => {
    const source = `
      <button className="bg-[linear-gradient(135deg,#FF1E56_0%,#FC744C_35%,#1E90FF_100%)] text-white" />
    `;
    expect(countViolations(source)).toEqual({ "hex-color": 3 });
  });

  it("does not mistake words that start with hex letters for colours", () => {
    const source = `<a href="#add" /> <a href="#face-lift" /> <a href="#address" />`;
    expect(countViolations(source)).toEqual({});
  });

  it("checks only hex literals in CSS", () => {
    expect(
      countViolations(".x { color: #fff; } .bg-blue-500 {}", true),
    ).toEqual({
      "hex-color": 1,
    });
  });

  it("scans apps/web/src and every feature package src", () => {
    const roots = scanRoots(frontendRoot).map((root) =>
      root.replace(frontendRoot, "").replace(/\\/g, "/"),
    );
    expect(roots).toContain("/apps/web/src");
    expect(roots).toContain("/packages/features/workspace/src");
  });
});

describe("check-design-tokens ratchet", () => {
  it("fails only on counts above the baseline", () => {
    const baseline = { "a.tsx": { "hex-color": 2 } };
    expect(
      compareToBaseline({ "a.tsx": { "hex-color": 2 } }, baseline),
    ).toEqual({
      regressions: [],
      improvements: [],
    });
    expect(
      compareToBaseline({ "a.tsx": { "hex-color": 1 } }, baseline).improvements,
    ).toHaveLength(1);
    expect(
      compareToBaseline({ "a.tsx": { "hex-color": 3 } }, baseline).regressions,
    ).toHaveLength(1);
    expect(
      compareToBaseline({ "new.tsx": { "palette-color": 1 } }, baseline)
        .regressions,
    ).toEqual([
      expect.stringContaining("new.tsx: palette-color 1 > baseline 0"),
    ]);
  });

  it("the repository has no violations beyond the committed baseline", () => {
    const { regressions } = compareToBaseline(
      scanDesignTokenUsage(frontendRoot),
      readBaseline().files,
    );
    expect(regressions).toEqual([]);
  });
});

describe("check-design-tokens raw z-index rule", () => {
  it("flags numeric and arbitrary z-index utilities above local layering", () => {
    const source = `
      <div className="fixed inset-0 z-50" />
      <div className="absolute z-[100] focus-visible:z-40 -z-30" />
      <div className="z-[1]" />
    `;
    expect(findRawZIndex(source)).toEqual([
      "z-50",
      "z-[100]",
      "z-40",
      "-z-30",
      "z-[1]",
    ]);
  });

  it("allows the semantic roles and local in-component layering", () => {
    const source = `
      <div className="z-dropdown z-sticky z-backdrop z-modal z-toast z-tooltip" />
      <div className="relative z-0 z-10 focus:z-10 data-[active=true]:z-10 z-20" />
      <div style={{ zIndex: "var(--z-toast)" }} className="z-index-note" />
    `;
    expect(findRawZIndex(source)).toEqual([]);
  });

  it("scans the UI primitives, features, product packages and app, minus the excluded owners", () => {
    const roots = zIndexScanRoots(frontendRoot).map((root) =>
      root.replace(frontendRoot, "").replace(/\\/g, "/"),
    );
    expect(roots).toContain("/packages/ui/web/src");
    expect(roots).toContain("/packages/features/workspace/src");
    expect(roots).toContain("/packages/product/docs/web/src");
    expect(RAW_Z_INDEX_EXCLUDED).toContain("packages/product/work-management/");
    expect(RAW_Z_INDEX_EXCLUDED).toContain(
      "apps/web/src/shell/sidebar/sidebar.tsx",
    );
  });

  it("the repository has no raw z-index utilities outside the exclusions", () => {
    expect(scanRawZIndex(frontendRoot)).toEqual({});
  });
});
