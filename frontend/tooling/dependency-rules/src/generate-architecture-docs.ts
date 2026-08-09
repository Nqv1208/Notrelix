#!/usr/bin/env tsx
/**
 * Generates the exact package boundary table from the closed-world
 * architecture manifest. The output is deterministic; docs must never keep a
 * second hand-maintained package matrix.
 *
 * Usage: tsx src/generate-architecture-docs.ts [--check]
 *   --check  exit non-zero if the committed file differs from generation
 */
import { readFileSync, writeFileSync, existsSync, realpathSync } from "node:fs";
import { join, resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { ARCHITECTURE_MANIFEST } from "./architecture-manifest";

const __dirname = dirname(fileURLToPath(import.meta.url));
const FRONTEND_ROOT = resolve(__dirname, "../../..");

/**
 * Workspace root for docs output. Generators run in a temporary workspace
 * fixture (GENERATOR_ROOT) and must never write into the real worktree.
 */
export function getFrontendRoot(): string {
  return process.env.GENERATOR_ROOT
    ? resolve(process.env.GENERATOR_ROOT)
    : FRONTEND_ROOT;
}

export function getArchitectureDocsPath(
  rootDir: string = getFrontendRoot(),
): string {
  return join(
    rootDir,
    "docs/client/architecture/package-boundaries.generated.md",
  );
}

export function generateArchitectureDocs(): string {
  const sorted = [...ARCHITECTURE_MANIFEST].sort((a, b) =>
    a.relativePath.localeCompare(b.relativePath, "en"),
  );

  const lines: string[] = [
    "# Notrelix Client — Package Boundaries (generated)",
    "",
    "<!-- GENERATED FILE — do not edit. -->",
    "<!-- Source of truth: tooling/dependency-rules/src/architecture-manifest.ts -->",
    "<!-- Regenerate: pnpm --filter @notrelix/dependency-rules docs:generate -->",
    "",
    `Package count: ${sorted.length}`,
    "",
    "| Relative path | Package | Layer | Freeze scope | Allowed internal imports |",
    "|:---|:---|:---|:---|:---|",
  ];

  for (const entry of sorted) {
    const allowed =
      entry.allowedInternalImports.length > 0
        ? entry.allowedInternalImports.map((name) => `\`${name}\``).join(", ")
        : "_(none)_";
    lines.push(
      `| \`${entry.relativePath}\` | \`${entry.packageName}\` | \`${entry.layer}\` | \`${entry.freezeScope}\` | ${allowed} |`,
    );
  }

  lines.push("");
  return lines.join("\n");
}

export function checkArchitectureDocs(rootDir: string = getFrontendRoot()): {
  ok: boolean;
  violations: string[];
} {
  const expected = generateArchitectureDocs();
  const outputPath = getArchitectureDocsPath(rootDir);
  if (!existsSync(outputPath)) {
    return {
      ok: false,
      violations: [
        `[ARCHITECTURE_DOCS_MISSING] generated boundary table not found at ${outputPath}; run docs:generate`,
      ],
    };
  }
  const actual = readFileSync(outputPath, "utf8");
  if (actual !== expected) {
    return {
      ok: false,
      violations: [
        `[ARCHITECTURE_DOCS_DRIFT] ${outputPath} is out of date with architecture-manifest.ts; run docs:generate`,
      ],
    };
  }
  return { ok: true, violations: [] };
}

const isDirectRun =
  !!process.argv[1] &&
  realpathSync(process.argv[1]) ===
    realpathSync(fileURLToPath(import.meta.url));

if (isDirectRun) {
  const checkMode = process.argv.includes("--check");
  const outputPath = getArchitectureDocsPath();
  if (checkMode) {
    const result = checkArchitectureDocs();
    if (!result.ok) {
      for (const violation of result.violations) console.error(violation);
      process.exit(1);
    }
    console.log("✅ Architecture docs are in sync with the manifest.");
  } else {
    writeFileSync(outputPath, generateArchitectureDocs(), "utf8");
    console.log(`✅ Wrote ${outputPath}`);
  }
}
