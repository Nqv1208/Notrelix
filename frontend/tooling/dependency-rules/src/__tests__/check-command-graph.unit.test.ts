import { describe, expect, it } from "vitest";
import { join, resolve } from "node:path";
import {
  checkCommandGraph,
  loadScriptCatalog,
  traverseCommandGraph,
} from "../check-command-graph";

const fixturesRoot = resolve(__dirname, "fixtures/command-graph");

function fixtureCatalog(name: string) {
  return loadScriptCatalog(join(fixturesRoot, name));
}

describe("command graph resolvers", () => {
  it("validates a positive validate:ui composition through pnpm indirection (TST-110)", () => {
    const catalog = fixtureCatalog("positive");
    const result = checkCommandGraph("validate:ui", catalog);

    expect(result.ok).toBe(true);
    expect(result.violations).toEqual([]);
    const traversed = traverseCommandGraph("validate:ui", catalog);
    for (const required of [
      "check:architecture",
      "check:ui-purity",
      "check:ui-actions",
      "check:ui-fixtures",
      "check:ui-evidence",
      "typecheck",
      "lint",
      "format:check",
      "test:web:guarded",
      "test:ui:freeze",
    ]) {
      expect(traversed.traversedScripts.has(required)).toBe(true);
    }
  });

  it("rejects when a required command is missing (TST-110 negative)", () => {
    const catalog = fixtureCatalog("missing-required");
    const result = checkCommandGraph("validate:ui", catalog);

    expect(result.ok).toBe(false);
    expect(result.violations.some((v) => v.includes("test:ui:freeze"))).toBe(
      true,
    );
  });

  it("rejects forbidden codegen/mock/real commands in the graph (TST-111)", () => {
    const catalog = fixtureCatalog("has-forbidden");
    const result = checkCommandGraph("validate:ui", catalog);

    expect(result.ok).toBe(false);
    expect(result.violations.some((v) => v.includes("mock:contract"))).toBe(
      true,
    );
  });

  it("catches a forbidden command hidden behind multi-level script indirection (TST-116)", () => {
    const catalog = fixtureCatalog("indirect-forbidden");
    const result = checkCommandGraph("validate:ui", catalog);

    expect(result.ok).toBe(false);
    expect(result.violations.some((v) => v.includes("codegen:check"))).toBe(
      true,
    );
  });

  it("resolves every transitively referenced script (no dangling refs)", () => {
    const catalog = fixtureCatalog("positive");
    const traversed = traverseCommandGraph("validate:ui", catalog);
    expect(traversed.traversedScripts.size).toBeGreaterThan(0);
  });
});
