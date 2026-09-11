#!/usr/bin/env node
// Mock E2E shard runner (frontend-ci-process-reorganization v1.1 §14.2).
//
// One GitHub runner per shard. Scenarios are assigned by `index % shard_count`
// over the canonical manifest in mock-scenarios.mjs. Each scenario invocation
// sets VITE_MOCK_PERSONA/VITE_MOCK_STATE before `pnpm e2e:mock` starts the dev
// server (playwright.mock.config.ts reads them per process), then interprets
// the Playwright report through the shared parser
// (playwright-result-summary.mjs) and requires executed logical tests > 0.
// Remaining scenarios still run for diagnostics; the shard exits nonzero if
// any scenario failed. Summaries are written to a durable directory outside
// Playwright-owned cleanup paths.

import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { resolve } from "node:path";
import { summarizePlaywrightReport } from "./playwright-result-summary.mjs";
import { SCENARIOS } from "./mock-scenarios.mjs";

function intArg(name) {
  const i = process.argv.indexOf(name);
  if (i < 0 || i + 1 >= process.argv.length) return undefined;
  const value = Number(process.argv[i + 1]);
  return Number.isInteger(value) ? value : Number.NaN;
}

function arg(name) {
  const i = process.argv.indexOf(name);
  if (i < 0 || i + 1 >= process.argv.length) return undefined;
  return process.argv[i + 1];
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

const outDir = resolve(arg("--out-dir") ?? "artifacts/frontend/ci/mock-shards");
mkdirSync(outDir, { recursive: true });

const assigned = SCENARIOS.filter(
  (_, index) => index % shardCount === shardIndex,
);

const results = [];
for (const scenario of assigned) {
  console.log(`[run-mock-shard] scenario ${scenario.id}`);
  const env = {
    ...process.env,
    VITE_MOCK_PERSONA: scenario.persona,
    VITE_MOCK_STATE: scenario.state,
  };
  let status = "passed";
  let executed = 0;
  let failed = 0;
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
  try {
    const summary = summarizePlaywrightReport(
      JSON.parse(readFileSync("test-results/mock-e2e-results.json", "utf8")),
    );
    executed = summary.executed;
    failed = summary.failed;
    if (status === "passed" && summary.executed === 0) {
      console.error(`[run-mock-shard] zero-test inner run for ${scenario.id}`);
      status = "failed";
    }
  } catch (error) {
    console.error(
      `[run-mock-shard] ${scenario.id} results unreadable: ${error.message}`,
    );
    status = "failed";
  }
  results.push({
    id: scenario.id,
    persona: scenario.persona,
    state: scenario.state,
    executed,
    failed,
    status,
  });
}

const summary = {
  shard: { index: shardIndex, count: shardCount },
  assigned: assigned.map((scenario) => scenario.id),
  scenarios: results,
};
const summaryPath = resolve(outDir, `mock-shard-summary-${shardIndex}.json`);
// Durable summaries live outside Playwright's test-results directory, which is
// wiped at the start of every `pnpm e2e:mock` run.
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
