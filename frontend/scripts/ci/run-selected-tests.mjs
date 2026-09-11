#!/usr/bin/env node
// Selected frontend test runner (delivery plan v2 §10.3).
//
// Consumes the selected capability list and maps each selected test
// capability to its guarded pnpm suite. Unknown capabilities and zero
// selections fail closed. All selected suites are attempted even when an
// earlier suite fails, and the aggregate exit code is nonzero if any suite
// failed. A stable machine-readable summary is written to --summary-out
// (when provided) and printed to stdout.

import { writeFileSync } from "node:fs";
import { spawnSync } from "node:child_process";

const SUITES = {
  "node-tests": "test:node:guarded",
  "web-tests": "test:web:guarded",
  "integration-tests": "test:integration:guarded",
  "mobile-tests": "test:mobile:guarded",
  "tooling-tests": "test:generators:guarded",
};

function arg(name) {
  const i = process.argv.indexOf(name);
  if (i < 0 || i + 1 >= process.argv.length) return undefined;
  return process.argv[i + 1];
}

const raw = arg("--capabilities-json");
if (!raw) {
  console.error("[run-selected-tests] missing --capabilities-json");
  process.exit(1);
}

let capabilities;
try {
  capabilities = JSON.parse(raw);
} catch (error) {
  console.error(
    `[run-selected-tests] invalid capabilities JSON: ${error.message}`,
  );
  process.exit(1);
}
if (
  !Array.isArray(capabilities) ||
  capabilities.some((capability) => typeof capability !== "string")
) {
  console.error(
    "[run-selected-tests] capabilities must be a JSON array of strings",
  );
  process.exit(1);
}

const unknown = capabilities.filter((capability) => !(capability in SUITES));
if (unknown.length > 0) {
  console.error(
    `[run-selected-tests] unknown selected suite(s): ${unknown.join(", ")}`,
  );
  process.exit(1);
}

const selected = Object.keys(SUITES).filter((capability) =>
  capabilities.includes(capability),
);
if (selected.length === 0) {
  console.error(
    "[run-selected-tests] zero selected suites while tests are expected",
  );
  process.exit(1);
}

const results = [];
let failed = false;
for (const capability of selected) {
  const script = SUITES[capability];
  console.log(`[run-selected-tests] ${capability} -> pnpm ${script}`);
  const run = spawnSync("pnpm", [script], {
    stdio: "inherit",
    env: process.env,
  });
  if (run.error) {
    console.error(
      `[run-selected-tests] ${capability} spawn error: ${run.error.message}`,
    );
  }
  const status = run.status === 0 ? "passed" : "failed";
  if (run.status !== 0) failed = true;
  results.push({ capability, script, status });
}

const summary = { selected, results, failed };
const summaryOut = arg("--summary-out");
if (summaryOut) {
  writeFileSync(summaryOut, `${JSON.stringify(summary, null, 2)}\n`);
}
console.log(JSON.stringify(summary));
if (failed) process.exit(1);
