// PARSER-01..06 acceptance fixtures for the shared Playwright report parser.
// Run: node scripts/ci/playwright-result-summary.test.mjs
import assert from "node:assert/strict";
import { summarizePlaywrightReport } from "./playwright-result-summary.mjs";

function spec(title, statuses) {
  return {
    title,
    tests: statuses.map((status) => ({ status })),
  };
}

function report(...suites) {
  return { suites };
}

// PARSER-01 — actual nested reporter structure
{
  const nested = report({
    title: "a.spec.ts",
    specs: [spec("top level", ["expected"])],
    suites: [
      {
        title: "describe block",
        specs: [spec("nested", ["expected"]), spec("deep", ["expected"])],
        suites: [{ title: "inner", specs: [spec("deepest", ["expected"])] }],
      },
    ],
  });
  const summary = summarizePlaywrightReport(nested);
  assert.equal(summary.logical_tests, 4);
  assert.equal(summary.executed, 4);
  assert.equal(summary.passed, 4);
  assert.equal(summary.failed, 0);
  assert.equal(summary.skipped, 0);
  console.log("PARSER-01 nested structure PASS");
}

// PARSER-02 — passed/failed/skipped classification
{
  const summary = summarizePlaywrightReport(
    report({
      title: "mixed.spec.ts",
      specs: [
        spec("ok", ["expected"]),
        spec("broken", ["unexpected"]),
        spec("timed out", ["timedOut"]),
        spec("by design", ["skipped"]),
        spec("flaky then green", ["flaky"]),
      ],
    }),
  );
  assert.equal(summary.passed, 2);
  assert.equal(summary.failed, 2);
  assert.equal(summary.skipped, 1);
  assert.equal(summary.executed, 4);
  console.log("PARSER-02 classification PASS");
}

// PARSER-03 — retries do not inflate the logical-test count
{
  const retried = report({
    title: "retry.spec.ts",
    specs: [
      {
        title: "attempted twice",
        tests: [
          { status: "failed" },
          { status: "failed" },
          { status: "expected" },
        ],
      },
    ],
  });
  const summary = summarizePlaywrightReport(retried);
  assert.equal(summary.logical_tests, 1);
  assert.equal(summary.executed, 1);
  assert.equal(summary.passed, 1);
  console.log("PARSER-03 retry PASS");
}

// PARSER-04 — declared tests without result attempts are not executed
{
  const declared = report({
    title: "declared.spec.ts",
    specs: [spec("declared only", []), spec("ran", ["expected"])],
  });
  const summary = summarizePlaywrightReport(declared);
  assert.equal(summary.logical_tests, 2);
  assert.equal(summary.executed, 1);
  assert.equal(summary.declared_without_attempts, 1);
  console.log("PARSER-04 declared-without-attempts PASS");
}

// PARSER-05 — malformed/unsupported structures fail closed
{
  const malformed = [
    null,
    {},
    { suites: "not-an-array" },
    { suites: [{ specs: "nope" }] },
    { suites: [{ specs: [{ title: "no tests array" }] }] },
    { suites: [{ specs: [spec("bad status", ["mysteryStatus"])] }] },
    { suites: [{ specs: [{ title: "bad outcome", tests: [{}] }] }] },
  ];
  for (const fixture of malformed) {
    assert.throws(
      () => summarizePlaywrightReport(fixture),
      /malformed Playwright report/,
    );
  }
  console.log("PARSER-05 fail-closed PASS");
}

// PARSER-06 — zero executed logical tests are detectable by the caller
{
  const empty = summarizePlaywrightReport(
    report({ title: "empty.spec.ts", specs: [] }),
  );
  assert.equal(empty.executed, 0);
  const allSkipped = summarizePlaywrightReport(
    report({ title: "skipped.spec.ts", specs: [spec("s", ["skipped"])] }),
  );
  assert.equal(allSkipped.executed, 0);
  assert.equal(allSkipped.skipped, 1);
  console.log("PARSER-06 zero-executed detection PASS");
}

console.log("playwright-result-summary tests: ALL PASS");
