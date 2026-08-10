import { afterEach, describe, expect, it } from "vitest";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { checkLintCoverage } from "../../../../scripts/assert-lint-coverage.mjs";

let currentRoot: string | null = null;

afterEach(() => {
  if (currentRoot) {
    rmSync(currentRoot, { recursive: true, force: true });
    currentRoot = null;
  }
});

function createFixtureRoot() {
  currentRoot = mkdtempSync(join(tmpdir(), "notrelix-lint-coverage-"));
  return currentRoot;
}

function writeManifestPackage(root: string, lintScript?: string) {
  const dir = join(root, "packages/product/freeze/core");
  mkdirSync(join(dir, "src"), { recursive: true });
  writeFileSync(join(dir, "src/index.ts"), "export const ok = true;\n");
  writeFileSync(
    join(dir, "package.json"),
    JSON.stringify(
      {
        name: "@notrelix/freeze-core",
        version: "0.0.0",
        type: "module",
        scripts:
          lintScript === undefined
            ? {}
            : {
                lint: lintScript,
              },
      },
      null,
      2,
    ),
  );
}

const manifest = [
  {
    packageName: "@notrelix/freeze-core",
    relativePath: "packages/product/freeze/core",
    freezeScope: "core-production",
  },
];

describe("assert-lint-coverage", () => {
  it("passes when a source-bearing manifest package has lint", () => {
    const root = createFixtureRoot();
    writeManifestPackage(root, "eslint .");

    const result = checkLintCoverage(root, manifest);

    expect(result.ok).toBe(true);
    expect(result.checked).toBe(1);
  });

  it("fails when a source-bearing manifest package is missing lint", () => {
    const root = createFixtureRoot();
    writeManifestPackage(root);

    const result = checkLintCoverage(root, manifest);

    expect(result.ok).toBe(false);
    expect(result.missing).toEqual([
      expect.objectContaining({
        name: "@notrelix/freeze-core",
        reason: "scripts.lint is missing or empty",
      }),
    ]);
  });

  it("fails when a source-bearing manifest package has empty lint", () => {
    const root = createFixtureRoot();
    writeManifestPackage(root, "   ");

    const result = checkLintCoverage(root, manifest);

    expect(result.ok).toBe(false);
    expect(result.missing[0]?.path).toBe("packages/product/freeze/core");
  });
});
