#!/usr/bin/env tsx
/**
 * Generates the exact frontend package-boundary table from the closed-world
 * architecture manifest.
 *
 * Authority model:
 *
 *   architecture-manifest.ts
 *     -> exact governed package inventory and allowed internal imports
 *
 *   generate-architecture-docs.ts
 *     -> deterministic producer
 *
 *   docs/generated/package-boundaries.md
 *     -> generated readable evidence
 *
 * The generated Markdown MUST NOT become a second hand-maintained package
 * matrix. Change the manifest when package architecture changes, then
 * regenerate this artifact.
 *
 * Usage:
 *
 *   tsx src/generate-architecture-docs.ts
 *   tsx src/generate-architecture-docs.ts --check
 *
 * --check exits non-zero when the committed generated file differs from the
 * deterministic output.
 */

import { existsSync, readFileSync, realpathSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { ARCHITECTURE_MANIFEST } from "./architecture-manifest";

const __dirname = dirname(fileURLToPath(import.meta.url));
const FRONTEND_ROOT = resolve(__dirname, "../../..");

const GENERATED_RELATIVE_PATH = "docs/generated/package-boundaries.md";
const MANIFEST_RELATIVE_PATH =
  "tooling/dependency-rules/src/architecture-manifest.ts";
const GENERATOR_RELATIVE_PATH =
  "tooling/dependency-rules/src/generate-architecture-docs.ts";

const GENERATED_FRONTMATTER = [
  "---",
  "document_id: FE-GEN-PACKAGE-BOUNDARIES",
  "document_type: generated",
  "status: generated",
  "owner: frontend-architecture",
  "applies_to:",
  "  - frontend-package-graph",
  "  - frontend-import-boundaries",
  "  - frontend-architecture-evidence",
  "evidence:",
  `  - ${MANIFEST_RELATIVE_PATH}`,
  `  - ${GENERATOR_RELATIVE_PATH}`,
  "review_on:",
  "  - architecture-manifest-change",
  "  - package-layer-change",
  "  - package-freeze-scope-change",
  "  - package-allowed-import-change",
  "  - package-boundary-generator-change",
  "---",
  "",
] as const;

/**
 * Workspace root for generated docs output.
 *
 * Generator tests may set GENERATOR_ROOT to a temporary fixture. In that case
 * this producer MUST write/check only inside the fixture and MUST NOT mutate
 * the real repository worktree.
 */
export function getFrontendRoot(): string {
  return process.env.GENERATOR_ROOT
    ? resolve(process.env.GENERATOR_ROOT)
    : FRONTEND_ROOT;
}

export function getArchitectureDocsPath(
  rootDir: string = getFrontendRoot(),
): string {
  return join(rootDir, GENERATED_RELATIVE_PATH);
}

function renderAllowedImports(imports: readonly string[]): string {
  return imports.length > 0
    ? imports.map((name) => `\`${name}\``).join(", ")
    : "_(none)_";
}

/**
 * Produce deterministic Markdown.
 *
 * Sorting by relativePath prevents declaration order in the manifest from
 * becoming generated-document order authority.
 */
export function generateArchitectureDocs(): string {
  const sorted = [...ARCHITECTURE_MANIFEST].sort((a, b) =>
    a.relativePath.localeCompare(b.relativePath, "en"),
  );

  const lines: string[] = [
    ...GENERATED_FRONTMATTER,
    "# Notrelix Frontend — Package Boundaries",
    "",
    "<!-- GENERATED FILE — DO NOT EDIT. -->",
    `<!-- Source of truth: ${MANIFEST_RELATIVE_PATH} -->`,
    `<!-- Producer: ${GENERATOR_RELATIVE_PATH} -->`,
    "<!-- Regenerate: pnpm --filter @notrelix/dependency-rules docs:generate -->",
    "<!-- Check drift: pnpm --filter @notrelix/dependency-rules docs:check -->",
    "",
    "> This file is generated evidence. It is not the semantic architecture owner.",
    "> Read `../architecture/dependency-boundaries.md` for package-boundary meaning and policy.",
    "",
    `Package count: ${sorted.length}`,
    "",
    "| Relative path | Package | Layer | Freeze scope | Allowed internal imports | Verification-only internal imports |",
    "|:---|:---|:---|:---|:---|:---|",
  ];

  for (const entry of sorted) {
    lines.push(
      `| \`${entry.relativePath}\` | \`${entry.packageName}\` | ` +
        `\`${entry.layer}\` | \`${entry.freezeScope}\` | ` +
        `${renderAllowedImports(entry.allowedInternalImports)} | ` +
        `${renderAllowedImports(entry.allowedVerificationInternalImports)} |`,
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
        `[ARCHITECTURE_DOCS_MISSING] generated boundary table not found at ` +
          `${outputPath}; run docs:generate`,
      ],
    };
  }

  const actual = readFileSync(outputPath, "utf8");

  if (actual !== expected) {
    return {
      ok: false,
      violations: [
        `[ARCHITECTURE_DOCS_DRIFT] ${outputPath} is out of date with ` +
          `${MANIFEST_RELATIVE_PATH}; run docs:generate`,
      ],
    };
  }

  return { ok: true, violations: [] };
}

function writeArchitectureDocs(rootDir: string = getFrontendRoot()): void {
  const outputPath = getArchitectureDocsPath(rootDir);
  writeFileSync(outputPath, generateArchitectureDocs(), "utf8");
  console.log(`✅ Wrote ${outputPath}`);
}

const isDirectRun =
  !!process.argv[1] &&
  realpathSync(process.argv[1]) ===
    realpathSync(fileURLToPath(import.meta.url));

if (isDirectRun) {
  const checkMode = process.argv.includes("--check");

  if (checkMode) {
    const result = checkArchitectureDocs();

    if (!result.ok) {
      for (const violation of result.violations) {
        console.error(violation);
      }
      process.exit(1);
    }

    console.log(
      "✅ Generated frontend package-boundary docs are in sync with the manifest.",
    );
  } else {
    writeArchitectureDocs();
  }
}
