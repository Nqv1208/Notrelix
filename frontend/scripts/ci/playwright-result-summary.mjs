// Shared Playwright JSON report parser (delivery: frontend CI process
// reorganization v1.1, F3 / DEC-FE-012).
//
// Single source of truth for interpreting Playwright JSON reporter output.
// Consumers: assert-playwright-mock-count.mjs and run-mock-shard.mjs. The
// parser is pure: no filesystem, no process, no CLI concerns.
//
// Semantics:
// - one logical test per spec (nested suites are traversed recursively);
// - retries never inflate the logical-test count (the spec's final outcome
//   decides);
// - declared specs without result attempts are counted separately and are not
//   executed;
// - malformed or unsupported structures throw so consumers fail closed.

const STATUS_CLASSES = {
  expected: "passed",
  passed: "passed",
  flaky: "passed",
  unexpected: "failed",
  failed: "failed",
  timedOut: "failed",
  skipped: "skipped",
};

function fail(message) {
  throw new Error(`malformed Playwright report: ${message}`);
}

function walkSuite(suite, summary, path) {
  if (!suite || typeof suite !== "object" || Array.isArray(suite)) {
    fail(`suite at ${path} is not an object`);
  }
  if (suite.suites !== undefined && !Array.isArray(suite.suites)) {
    fail(`suite.suites at ${path} is not an array`);
  }
  if (suite.specs !== undefined && !Array.isArray(suite.specs)) {
    fail(`suite.specs at ${path} is not an array`);
  }
  for (const spec of suite.specs ?? []) {
    if (!spec || typeof spec !== "object" || Array.isArray(spec)) {
      fail(`spec at ${path} is not an object`);
    }
    if (!Array.isArray(spec.tests)) {
      fail(`spec "${spec.title ?? "?"}" at ${path} has no tests array`);
    }
    summary.logical_tests += 1;
    if (spec.tests.length === 0) {
      summary.declared_without_attempts += 1;
      continue;
    }
    const outcome = spec.tests[spec.tests.length - 1];
    if (
      !outcome ||
      typeof outcome !== "object" ||
      typeof outcome.status !== "string"
    ) {
      fail(
        `spec "${spec.title ?? "?"}" at ${path} has an outcome without status`,
      );
    }
    const statusClass = STATUS_CLASSES[outcome.status];
    if (!statusClass) {
      fail(`unsupported outcome status "${outcome.status}" at ${path}`);
    }
    if (statusClass === "skipped") {
      summary.skipped += 1;
      continue;
    }
    summary.executed += 1;
    summary[statusClass] += 1;
  }
  for (const child of suite.suites ?? []) {
    walkSuite(child, summary, `${path}/${child?.title ?? "?"}`);
  }
}

export function summarizePlaywrightReport(report) {
  if (!report || typeof report !== "object" || Array.isArray(report)) {
    fail("report is not an object");
  }
  if (!Array.isArray(report.suites)) {
    fail("report.suites is missing or not an array");
  }
  const summary = {
    logical_tests: 0,
    executed: 0,
    passed: 0,
    failed: 0,
    skipped: 0,
    declared_without_attempts: 0,
  };
  for (const suite of report.suites) {
    walkSuite(suite, summary, suite?.title ?? "?");
  }
  return summary;
}
