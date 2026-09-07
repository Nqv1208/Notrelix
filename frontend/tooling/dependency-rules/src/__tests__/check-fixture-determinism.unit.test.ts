import { describe, expect, it } from "vitest";
import { join, resolve } from "node:path";
import { checkFixtureDeterminism } from "../check-fixture-determinism";

const fixturesRoot = resolve(__dirname, "fixtures/fixture-determinism");

describe("checkFixtureDeterminism", () => {
  it("accepts deterministic fixture/scenario/controller authorities (TST-056, TST-057)", () => {
    const result = checkFixtureDeterminism(join(fixturesRoot, "valid"));

    expect(result.ok).toBe(true);
    expect(result.scannedFiles).toBe(2);
    expect(result.violations).toEqual([]);
  });

  it("rejects ambient random IDs and wall-clock reads in fixture authorities (TST-056, TST-057)", () => {
    const result = checkFixtureDeterminism(join(fixturesRoot, "nondeterministic"));
    const messages = result.violations
      .map((violation) => violation.message)
      .join("\n");

    expect(result.ok).toBe(false);
    expect(messages).toContain("Math.random");
    expect(messages).toContain("new Date()");
    expect(messages).toContain("Date.now()");
  });
});
