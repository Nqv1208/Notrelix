#!/usr/bin/env node

/**
 * assert-no-mock-artifact.mjs — Gate G-MFB-002: Production Artifact Isolation
 *
 * Verifies that the production web build output (dist/) contains zero traces
 * of dev mock backend packages, mock actors, mock presets, or mock error types.
 *
 * Plan: 01-FREEZE-SPEC.md §FZ-S14, 02-IMPLEMENTATION-PLAN.md §MFB-FZ-08
 */

import { readdirSync, readFileSync, statSync, existsSync } from "node:fs";
import { join, resolve } from "node:path";

const FORBIDDEN_SIGNATURES = [
  "@notrelix/dev-mock-backend",
  "mock-user-owner",
  "mock-user-viewer",
  "ui-default",
  "MockUnhandledOperationError",
  "packages/dev/mock-backend",
];

const webDistDir = resolve(process.cwd(), "apps/web/dist");

if (!existsSync(webDistDir)) {
  console.error(
    `[check:production-mock-isolation] ERROR: ${webDistDir} does not exist. Run 'pnpm build' before running this gate.`,
  );
  process.exit(1);
}

function getAllFiles(dir, fileList = []) {
  const files = readdirSync(dir);
  for (const file of files) {
    const filePath = join(dir, file);
    const stat = statSync(filePath);
    if (stat.isDirectory()) {
      getAllFiles(filePath, fileList);
    } else {
      fileList.push(filePath);
    }
  }
  return fileList;
}

const allFiles = getAllFiles(webDistDir);
let violations = 0;

for (const filePath of allFiles) {
  const relativeName = filePath.replace(process.cwd(), "");

  // Check filename
  if (relativeName.includes("mock-backend")) {
    console.error(
      `[check:production-mock-isolation] VIOLATION: Mock chunk filename detected: ${relativeName}`,
    );
    violations++;
  }

  // Check file content for JS, CSS, JSON, MAP, HTML
  if (/\.(js|mjs|css|json|map|html)$/i.test(filePath)) {
    const content = readFileSync(filePath, "utf-8");
    for (const signature of FORBIDDEN_SIGNATURES) {
      if (content.includes(signature)) {
        console.error(
          `[check:production-mock-isolation] VIOLATION: Forbidden signature "${signature}" found in ${relativeName}`,
        );
        violations++;
      }
    }
  }
}

if (violations > 0) {
  console.error(
    `[check:production-mock-isolation] FAILED with ${violations} violation(s).`,
  );
  process.exit(1);
}

console.log(
  `[check:production-mock-isolation] PASS: All ${allFiles.length} production artifact files are clean of mock signatures.`,
);
process.exit(0);
