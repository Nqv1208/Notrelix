import { readdirSync, readFileSync, statSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

/**
 * UC-ACC-G01: every account-scoped query key is built by the helper (`accountQueryKey` or a
 * `*QueryKeys` factory over it), never by a raw `["account", ...]` tuple. This scans the production
 * sources of every package and app; the helper itself and test files are the only exceptions.
 */
const FRONTEND_ROOT = fileURLToPath(
  new URL("../../../../../", import.meta.url),
);
const SCAN_ROOTS = ["packages", "apps"].map((dir) => join(FRONTEND_ROOT, dir));
const RAW_ACCOUNT_TUPLE = /\[\s*["'`]account["'`]\s*,/;
const HELPER = "query-key-scope.ts";

function productionSources(dir: string): string[] {
  const found: string[] = [];
  for (const entry of readdirSync(dir)) {
    if (entry === "node_modules" || entry === "dist" || entry === "__tests__")
      continue;
    const path = join(dir, entry);
    if (statSync(path).isDirectory()) {
      found.push(...productionSources(path));
      continue;
    }
    if (!/\.(ts|tsx)$/.test(entry)) continue;
    if (/\.(test|stories|spec)\.(ts|tsx)$/.test(entry) || entry === HELPER)
      continue;
    found.push(path);
  }
  return found;
}

describe("account-scoped query keys (UC-ACC-G01)", () => {
  it("no production source builds an account-scoped key outside the helper", () => {
    const offenders = SCAN_ROOTS.flatMap(productionSources)
      .filter((file) => RAW_ACCOUNT_TUPLE.test(readFileSync(file, "utf8")))
      .map((file) => file.slice(FRONTEND_ROOT.length));

    expect(offenders).toEqual([]);
  });

  it("the scan reaches the packages that own account-scoped keys", () => {
    const files = SCAN_ROOTS.flatMap(productionSources).map((file) =>
      file.slice(FRONTEND_ROOT.length),
    );

    expect(files).toContain("packages/features/account/src/query/keys.ts");
    expect(files).toContain("packages/features/workspace/src/query/keys.ts");
    expect(files).toContain(
      "packages/features/notifications/src/query/keys.ts",
    );
    expect(files).toContain("apps/web/src/shell/workspace/work-feed-bridge.ts");
  });
});
