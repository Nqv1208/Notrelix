import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  compareToBaseline,
  countViolations,
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
