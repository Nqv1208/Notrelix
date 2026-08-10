import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const scriptPath = join(
  dirname(fileURLToPath(import.meta.url)),
  "../../../../scripts/freeze-audit.mjs",
);
const source = readFileSync(scriptPath, "utf8");

describe("FREEZE freeze:audit script contract", () => {
  it("defines the mandatory gates in the spec order", () => {
    const order = [
      '"VALIDATE"',
      '"UI_FREEZE"',
      '"BUILD"',
      '"PRODUCTION_STARTUP"',
      '"E2E"',
    ];

    for (const gate of order) {
      const index = source.indexOf(gate);
      expect(index, `missing gate ${gate}`).toBeGreaterThan(-1);
    }
  });

  it("exits non-zero on failure", () => {
    expect(source).toMatch(/process\.exit\(failed \? 1 : 0\)/);
  });

  it("prints PASS/FAIL per gate", () => {
    expect(source).toMatch(/\[freeze:audit\] \$\{name\}: PASS/);
    expect(source).toMatch(/\[freeze:audit\] \$\{name\}: FAIL/);
  });

  it("creates no hidden artifact, certificate, docs or git mutation", () => {
    expect(source).not.toMatch(
      /writeFileSync|mkdirSync|appendFileSync|createWriteStream/,
    );
    expect(source).not.toMatch(
      /\.freeze-artifacts|last-audit-result\.json|cert\.json/i,
    );
    expect(source).not.toMatch(/git (add|commit|checkout|reset)/);
  });
});
