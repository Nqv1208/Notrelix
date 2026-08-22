#!/usr/bin/env node
/**
 * assert-playwright-mock-count.mjs
 *
 * MDF-10 compliance gate: Parse Playwright JSON results and assert that:
 *   - executed == expected
 *   - passed == expected
 *   - failed == 0
 *   - skipped == 0
 *
 * Usage:
 *   node scripts/assert-playwright-mock-count.mjs [path-to-results.json] [expected-count]
 *
 * Example:
 *   node scripts/assert-playwright-mock-count.mjs test-results/mock-e2e-results.json 6
 */

import { readFileSync, existsSync } from "fs";
import { resolve } from "path";

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

let results;
try {
  results = JSON.parse(readFileSync(resolvedPath, "utf8"));
} catch (e) {
  console.error(
    `[assert-playwright-mock-count] ERROR: Could not parse results file: ${resolvedPath}`,
  );
  console.error(e.message);
  process.exit(1);
}

// Playwright JSON reporter format
const stats = results.stats ?? {};
const suites = results.suites ?? [];

// Count tests across all suites recursively
function countTests(suites) {
  let total = 0;
  let passed = 0;
  let failed = 0;
  let skipped = 0;
  let flaky = 0;

  function walk(suite) {
    for (const test of suite.tests ?? []) {
      total++;
      const outcomes = test.results ?? [];
      const lastOutcome = outcomes[outcomes.length - 1];
      const status = lastOutcome?.status ?? "unknown";
      if (status === "passed") passed++;
      else if (status === "failed" || status === "timedOut") failed++;
      else if (status === "skipped") skipped++;
      else if (status === "flaky") flaky++;
    }
    for (const child of suite.suites ?? []) {
      walk(child);
    }
  }

  for (const s of suites) {
    walk(s);
  }
  return { total, passed, failed, skipped, flaky };
}

const counts = countTests(suites);

console.log(`[assert-playwright-mock-count] Mock E2E Results:`);
console.log(`  Total    : ${counts.total}`);
console.log(`  Passed   : ${counts.passed}`);
console.log(`  Failed   : ${counts.failed}`);
console.log(`  Skipped  : ${counts.skipped}`);
console.log(`  Flaky    : ${counts.flaky}`);

const errors = [];

if (counts.failed !== 0) {
  errors.push(`  FAIL: expected failed=0, got ${counts.failed}`);
}

if (counts.skipped !== 0) {
  errors.push(
    `  FAIL: expected skipped=0, got ${counts.skipped} (MDF-10: no required E2E skips allowed)`,
  );
}

if (expectedCount > 0 && counts.total !== expectedCount) {
  errors.push(
    `  FAIL: expected total=${expectedCount}, got ${counts.total} (MDF-10: executed == expected)`,
  );
}

if (expectedCount > 0 && counts.passed !== expectedCount) {
  errors.push(`  FAIL: expected passed=${expectedCount}, got ${counts.passed}`);
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
