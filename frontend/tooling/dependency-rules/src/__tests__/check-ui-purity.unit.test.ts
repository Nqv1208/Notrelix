import { describe, expect, it } from "vitest";
import { join, resolve } from "node:path";
import { checkUiPurity } from "../check-ui-purity";

const fixturesRoot = resolve(__dirname, "fixtures/ui-purity");

describe("checkUiPurity", () => {
  it("accepts a pure UI entry with relative and @notrelix transitive imports", () => {
    const result = checkUiPurity(join(fixturesRoot, "valid"));

    expect(result.ok).toBe(true);
    expect(result.checkedEntries).toBe(1);
    expect(result.violations).toEqual([]);
  });

  it("rejects forbidden package imports through barrels", () => {
    const result = checkUiPurity(join(fixturesRoot, "forbidden-import"));

    expect(result.ok).toBe(false);
    expect(result.violations).toContainEqual(
      expect.objectContaining({
        code: "FORBIDDEN_IMPORT",
        message: expect.stringContaining("@tanstack/react-query"),
      }),
    );
    expect(result.violations[0]?.chain.join(" -> ")).toContain("barrel");
  });

  it("rejects network primitives in the transitive pure UI graph", () => {
    const result = checkUiPurity(join(fixturesRoot, "forbidden-network"));

    expect(result.ok).toBe(false);
    expect(result.violations).toContainEqual(
      expect.objectContaining({
        code: "FORBIDDEN_SOURCE",
        message: expect.stringContaining("fetch"),
      }),
    );
  });

  it("independently rejects locked forbidden internal ownership imports", () => {
    const result = checkUiPurity(join(fixturesRoot, "forbidden-packages"));
    const messages = result.violations
      .map((violation) => violation.message)
      .join("\n");

    expect(result.ok).toBe(false);
    expect(messages).toContain("@notrelix/work-management-state");
    expect(messages).toContain("@notrelix/contracts");
    expect(messages).toContain("@notrelix/runtime-web");
    expect(messages).toContain("@notrelix/dev-mock-backend");
    expect(messages).toContain("@notrelix/features-auth");
  });

  it("independently rejects XHR and WebSocket primitives", () => {
    const result = checkUiPurity(
      join(fixturesRoot, "forbidden-browser-network"),
    );
    const messages = result.violations
      .map((violation) => violation.message)
      .join("\n");

    expect(result.ok).toBe(false);
    expect(messages).toContain("XMLHttpRequest");
    expect(messages).toContain("WebSocket");
  });

  it("rejects a forbidden dynamic import with its chain (TST-044)", () => {
    const result = checkUiPurity(join(fixturesRoot, "forbidden-dynamic-import"));

    expect(result.ok).toBe(false);
    expect(result.violations).toContainEqual(
      expect.objectContaining({
        code: "FORBIDDEN_IMPORT",
        message: expect.stringContaining("@notrelix/work-management-state"),
      }),
    );
  });

  it("rejects a literal require of a forbidden owner (TST-045)", () => {
    const result = checkUiPurity(join(fixturesRoot, "forbidden-require"));

    expect(result.ok).toBe(false);
    expect(result.violations).toContainEqual(
      expect.objectContaining({
        code: "FORBIDDEN_IMPORT",
        message: expect.stringContaining("@notrelix/work-management-state"),
      }),
    );
  });

  it("fails closed on a non-literal dynamic module edge (TST-046)", () => {
    const result = checkUiPurity(join(fixturesRoot, "non-literal-module"));

    expect(result.ok).toBe(false);
    expect(result.violations).toContainEqual(
      expect.objectContaining({
        code: "UNRESOLVED_IMPORT",
        message: expect.stringContaining("non-literal"),
      }),
    );
  });

  it("rejects cookie, location, and history side effects (TST-047)", () => {
    const result = checkUiPurity(join(fixturesRoot, "forbidden-side-effect"));
    const messages = result.violations
      .map((violation) => violation.message)
      .join("\n");

    expect(result.ok).toBe(false);
    expect(messages).toContain("document.cookie");
    expect(messages).toContain("window.location");
    expect(messages).toContain("history navigation");
  });
});
