#!/usr/bin/env node
// Mock E2E shard runner (delivery plan v2 §10.9).
//
// One GitHub runner per shard. Scenarios are assigned by `index % shard_count`
// over the canonical manifest in mock-scenarios.mjs. Each scenario invocation
// sets VITE_MOCK_PERSONA/VITE_MOCK_STATE before `pnpm e2e:mock` starts the dev
// server (playwright.mock.config.ts reads them per process), then runs the
// existing `pnpm e2e:mock:count` guard and additionally fails a scenario whose
// inner Playwright run executed zero tests. The shard summary is always
// written so the completeness verifier can prove exact 20/20 coverage.

import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { SCENARIOS } from "./mock-scenarios.mjs";

function intArg(name) {
  const i = process.argv.indexOf(name);
  if (i < 0 || i + 1 >= process.argv.length) return undefined;
  const value = Number(process.argv[i + 1]);
  return Number.isInteger(value) ? value : Number.NaN;
}

const shardCount = intArg("--shard-count");
const shardIndex = intArg("--shard-index");

if (!Number.isInteger(shardCount) || shardCount < 1) {
  console.error(
    `[run-mock-shard] invalid --shard-count: ${process.argv[process.argv.indexOf("--shard-count") + 1]}`,
  );
  process.exit(1);
}
if (
  !Number.isInteger(shardIndex) ||
  shardIndex < 0 ||
  shardIndex >= shardCount
) {
  console.error(
    `[run-mock-shard] invalid --shard-index: ${process.argv[process.argv.indexOf("--shard-index") + 1]}`,
  );
  process.exit(1);
}

function countPlaywrightTests(results) {
  let total = 0;
  const walk = (suite) => {
    total += (suite.tests ?? []).length;
    for (const child of suite.suites ?? []) walk(child);
  };
  for (const suite of results.suites ?? []) walk(suite);
  return total;
}

const assigned = SCENARIOS.filter(
  (_, index) => index % shardCount === shardIndex,
);
const outDir = "test-results/mock-shards";
mkdirSync(outDir, { recursive: true });

const results = [];
for (const scenario of assigned) {
  console.log(`[run-mock-shard] scenario ${scenario.id}`);
  const env = {
    ...process.env,
    VITE_MOCK_PERSONA: scenario.persona,
    VITE_MOCK_STATE: scenario.state,
  };
  let status = "passed";
  const e2e = spawnSync("pnpm", ["e2e:mock"], { stdio: "inherit", env });
  if (e2e.error) {
    console.error(
      `[run-mock-shard] ${scenario.id} spawn error: ${e2e.error.message}`,
    );
  }
  if (e2e.status !== 0) {
    status = "failed";
  }
  if (status === "passed") {
    const count = spawnSync("pnpm", ["e2e:mock:count"], {
      stdio: "inherit",
      env,
    });
    if (count.status !== 0) status = "failed";
  }
  if (status === "passed") {
    try {
      const parsed = JSON.parse(
        readFileSync("test-results/mock-e2e-results.json", "utf8"),
      );
      const total = countPlaywrightTests(parsed);
      if (total === 0) {
        console.error(
          `[run-mock-shard] zero-test inner run for ${scenario.id}`,
        );
        status = "failed";
      }
    } catch (error) {
      console.error(
        `[run-mock-shard] ${scenario.id} results unreadable: ${error.message}`,
      );
      status = "failed";
    }
  }
  results.push({
    id: scenario.id,
    persona: scenario.persona,
    state: scenario.state,
    status,
  });
}

const summary = {
  shard: { index: shardIndex, count: shardCount },
  assigned: assigned.map((scenario) => scenario.id),
  scenarios: results,
};
const summaryPath = `${outDir}/mock-shard-summary-${shardIndex}.json`;
// Each `pnpm e2e:mock` run wipes Playwright's test-results directory at
// startup, so the shard directory must be recreated immediately before the
// summary is written after the final scenario.
mkdirSync(outDir, { recursive: true });
writeFileSync(summaryPath, `${JSON.stringify(summary, null, 2)}\n`);
console.log(JSON.stringify(summary));

const failed = results.filter((result) => result.status !== "passed");
if (failed.length > 0) {
  console.error(
    `[run-mock-shard] failed scenarios: ${failed.map((f) => f.id).join(", ")}`,
  );
  process.exit(1);
}
