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
});
