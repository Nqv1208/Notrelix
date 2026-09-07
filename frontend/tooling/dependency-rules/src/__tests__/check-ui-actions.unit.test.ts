import { describe, expect, it } from "vitest";
import { join, resolve } from "node:path";
import { checkUiActions } from "../check-ui-actions";

const fixturesRoot = resolve(__dirname, "fixtures/ui-actions");

describe("checkUiActions", () => {
  it("accepts callbacks, disabled, submit, asChild and compound trigger children (TST-042, TST-043)", () => {
    const result = checkUiActions(join(fixturesRoot, "valid"));

    expect(result.ok).toBe(true);
    expect(result.violations).toEqual([]);
  });

  it("rejects an enabled native button with no semantics (TST-040)", () => {
    const result = checkUiActions(join(fixturesRoot, "dead-native"));

    expect(result.ok).toBe(false);
    expect(result.violations).toHaveLength(1);
    expect(result.violations[0]).toEqual(
      expect.objectContaining({
        code: "DEAD_ENABLED_CONTROL",
        message: expect.stringContaining("<button>"),
      }),
    );
    expect(result.violations[0]?.message).toMatch(/actions\.tsx:\d+ <button>/);
  });

  it("rejects an enabled imported Button with no semantics (TST-041)", () => {
    const result = checkUiActions(join(fixturesRoot, "dead-imported"));

    expect(result.ok).toBe(false);
    expect(result.violations).toHaveLength(1);
    expect(result.violations[0]).toEqual(
      expect.objectContaining({
        code: "DEAD_ENABLED_CONTROL",
        message: expect.stringContaining("<Button>"),
      }),
    );
  });

  it("reports missing registered sources and invalid manifests", () => {
    const missingRoot = join(fixturesRoot, "valid");
    const result = checkUiActions(missingRoot);

    expect(result.checkedSources).toBe(1);
  });
});
