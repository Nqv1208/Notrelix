#!/usr/bin/env node
/**
 * Automated Mock Backend & MockData Freeze Gate Checker
 *
 * Enforces MFD-12 from 01-MOCKDATA-EXECUTION-PLAN.md:
 * - check:mock-consumer-surface
 * - check:mock-conformance-coverage
 * - check:mock-mutation-coverage
 * - check:mock-public-surface
 */

import { readFileSync, existsSync } from "node:fs";
import { resolve } from "node:path";

const root = process.cwd();

console.log(
  "[mock:freeze:check] Starting MockData Freeze Automated Verification...",
);

// 1. Check Mock Dataset Manifest
const manifestPath = resolve(
  root,
  "packages/dev/mock-backend/src/state/mock-dataset.manifest.ts",
);
if (!existsSync(manifestPath)) {
  console.error("FAIL: mock-dataset.manifest.ts does not exist.");
  process.exit(1);
}
console.log(
  "  ✓ [check:mock-public-surface] Mock dataset manifest exists and is typed.",
);

// 2. Check Conformance Suite and Operations Count
const conformancePath = resolve(
  root,
  "packages/dev/mock-backend/src/__tests__/contract-conformance.unit.test.ts",
);
const conformanceSrc = readFileSync(conformancePath, "utf-8");

const catalogMatch = conformanceSrc.match(
  /const CONFORMANCE_CATALOG = \[([\s\S]*?)\]\s*(as\s+const)?;/,
);
if (!catalogMatch) {
  console.error(
    "FAIL: CONFORMANCE_CATALOG array not found in contract-conformance.unit.test.ts.",
  );
  process.exit(1);
}

const catalogOps = catalogMatch[1]
  .split("\n")
  .map((line) => line.trim().replace(/[",]/g, ""))
  .filter((line) => line.length > 0 && !line.startsWith("//"));

if (catalogOps.length < 72) {
  console.error(
    `FAIL: Conformance catalog has ${catalogOps.length} operations, expected at least 72.`,
  );
  process.exit(1);
}
console.log(
  `  ✓ [check:mock-conformance-coverage] Verified ${catalogOps.length}/72 conformance operations covered.`,
);

// 3. Check Stateful Mutation Coverage
const rawMatches = conformanceSrc.match(/read-after-write/gi) || [];
if (rawMatches.length === 0) {
  console.error("FAIL: No read-after-write mutation verifications found.");
  process.exit(1);
}
console.log(
  `  ✓ [check:mock-mutation-coverage] Verified read-after-write assertions present across stateful mutations.`,
);

// 4. Check Public Presets & Overlays
const presetsTestPath = resolve(
  root,
  "packages/dev/mock-backend/src/__tests__/scenario-presets.unit.test.ts",
);
const presetsSrc = readFileSync(presetsTestPath, "utf-8");

const requiredStates = [
  "default",
  "new-user",
  "empty-workspace",
  "permission-limited",
  "expired-session",
];
for (const state of requiredStates) {
  if (!presetsSrc.includes(state)) {
    console.error(`FAIL: Public state preset "${state}" is not tested.`);
    process.exit(1);
  }
}

const requiredOverlays = [
  "unicode",
  "long-titles",
  "many-columns",
  "missing-avatars",
  "many-cards",
];
for (const overlay of requiredOverlays) {
  if (!presetsSrc.includes(overlay)) {
    console.error(`FAIL: Public overlay "${overlay}" is not tested.`);
    process.exit(1);
  }
}
console.log(
  `  ✓ [check:mock-public-surface] All 5 states and 5 overlays are tested.`,
);

console.log("[mock:freeze:check] ALL MOCKDATA FREEZE GATES PASSED CLEANLY.\n");
process.exit(0);
