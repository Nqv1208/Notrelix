/**
 * assert-playwright-mock-count.mjs
 *
 * MDF-10 compliance gate (thin CLI adapter over the shared parser —
 * frontend-ci-process-reorganization v1.1 DEC-FE-012: this file contains no
 * traversal implementation of its own).
 *
 * Gates on the captured Playwright JSON report:
 *   - failed == 0
 *   - executed logical tests > 0 (zero-execution fails closed)
 *   - (optionally) executed == expected and passed == expected
 *
 * Skipped specs are reported but not gated: mock scenario cells intentionally
 * skip the default-only accessibility spec outside the `default` state.
 *
 * Usage:
 *   node scripts/assert-playwright-mock-count.mjs [path-to-results.json] [expected-count]
 *
 * Example:
 *   node scripts/assert-playwright-mock-count.mjs test-results/mock-e2e-results.json 6
 */

import { readFileSync, existsSync } from "fs";
import { resolve } from "path";
import { summarizePlaywrightReport } from "./ci/playwright-result-summary.mjs";

const resultsPath = process.argv[2] ?? "test-results/mock-e2e-results.json";
const expectedCount = parseInt(process.argv[3] ?? "0", 10);

const resolvedPath = resolve(process.cwd(), resultsPath);

if (!existsSync(resolvedPath)) {
  console.error(
    `[assert-playwright-mock-count] ERROR: Results file not found: ${resolvedPath}`,
  );
  console.error(
    `  Run: pnpm e2e:mock first to generate Playwright JSON results.`,
  );
  process.exit(1);
}

let summary;
try {
  summary = summarizePlaywrightReport(
    JSON.parse(readFileSync(resolvedPath, "utf8")),
  );
} catch (error) {
  console.error(`[assert-playwright-mock-count] GATE FAILED: ${error.message}`);
  process.exit(1);
}

console.log(`[assert-playwright-mock-count] Mock E2E Results:`);
console.log(`  Logical tests : ${summary.logical_tests}`);
console.log(`  Executed      : ${summary.executed}`);
console.log(`  Passed        : ${summary.passed}`);
console.log(`  Failed        : ${summary.failed}`);
console.log(`  Skipped       : ${summary.skipped}`);

const errors = [];

if (summary.failed !== 0) {
  errors.push(`  FAIL: expected failed=0, got ${summary.failed}`);
}

if (summary.executed === 0) {
  errors.push(
    `  FAIL: zero executed logical tests (MDF-10: an inner run must execute the mock suite)`,
  );
}

if (expectedCount > 0 && summary.executed !== expectedCount) {
  errors.push(
    `  FAIL: expected executed=${expectedCount}, got ${summary.executed} (MDF-10: executed == expected)`,
  );
}

if (expectedCount > 0 && summary.passed !== expectedCount) {
  errors.push(
    `  FAIL: expected passed=${expectedCount}, got ${summary.passed}`,
  );
}

if (errors.length > 0) {
  console.error(`\n[assert-playwright-mock-count] GATE FAILED:`);
  for (const e of errors) console.error(e);
  process.exit(1);
} else {
  console.log(
    `\n[assert-playwright-mock-count] ALL MOCK E2E COUNT GATES PASSED.`,
  );
  process.exit(0);
}
