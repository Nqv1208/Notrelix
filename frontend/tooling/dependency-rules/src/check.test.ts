import { afterEach, expect, test } from "vitest";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { spawnSync } from "node:child_process";

let currentRoot: string | null = null;

afterEach(() => {
  if (currentRoot) {
    rmSync(currentRoot, { recursive: true, force: true });
    currentRoot = null;
  }
});

function createFixtureRoot() {
  currentRoot = mkdtempSync(join(tmpdir(), "notrelix-boundary-"));
  return currentRoot;
}

function writePackage(root: string, packagePath: string, packageName: string, source: string) {
  const dir = join(root, packagePath);
  mkdirSync(join(dir, "src"), { recursive: true });
  writeFileSync(
    join(dir, "package.json"),
    JSON.stringify({ name: packageName, version: "0.0.0", type: "module" }, null, 2),
  );
  writeFileSync(join(dir, "src", "index.ts"), source);
}

function runChecker(root: string) {
  return spawnSync("node", ["src/check.mjs", "--root", root], {
    cwd: join(import.meta.dirname, ".."),
    encoding: "utf8",
  });
}

test("rejects Next.js imports inside shared packages", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/product/work-management/state",
    "@notrelix/work-management-state",
    'import { useRouter } from "next/navigation";\nexport const useBadRouter = useRouter;\n',
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("EXTERNAL_FORBIDDEN");
  expect(result.stderr).toContain("next/navigation");
});

test("rejects direct environment reads inside shared packages", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "packages/foundation/contracts",
    "@notrelix/contracts",
    "export const baseUrl = process.env.NEXT_PUBLIC_API_URL ?? '/api/v1';\n",
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("DIRECT_ENV_READ");
  expect(result.stderr).toContain("NEXT_PUBLIC_API_URL");
});

test("checks apps package boundaries, not only packages directories", () => {
  const root = createFixtureRoot();
  writePackage(
    root,
    "apps/marketing",
    "@notrelix/app-marketing",
    'import { boardApi } from "@notrelix/work-management-state";\nexport const leaked = boardApi;\n',
  );

  const result = runChecker(root);

  expect(result.status).toBe(1);
  expect(result.stderr).toContain("FORBIDDEN");
  expect(result.stderr).toContain("@notrelix/work-management-state");
});
