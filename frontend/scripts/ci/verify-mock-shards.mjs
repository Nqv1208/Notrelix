#!/usr/bin/env node
// Mock shard completeness verifier (delivery plan v2 §10.10).
//
// Reads every shard summary and proves the executed scenario space equals the
// canonical manifest exactly: no missing scenario, no duplicate execution, no
// unknown scenario result, and no failed scenario. The manifest itself must
// remain exactly 4 personas x 5 states = 20 unique scenarios.

import { existsSync, readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { SCENARIOS, validateManifest } from "./mock-scenarios.mjs";

function arg(name) {
  const i = process.argv.indexOf(name);
  if (i < 0 || i + 1 >= process.argv.length) return undefined;
  return process.argv[i + 1];
}

const dir = arg("--dir");
const shardCountRaw = arg("--shard-count");
const shardCount = Number(shardCountRaw);

const errors = [];

for (const manifestError of validateManifest()) {
  errors.push(`manifest ${manifestError}`);
}

if (!Number.isInteger(shardCount) || shardCount < 1) {
  errors.push(`invalid --shard-count: ${shardCountRaw}`);
}

const expected = new Map(SCENARIOS.map((scenario) => [scenario.id, scenario]));
const executed = new Map();

if (!dir || !existsSync(dir)) {
  errors.push(`summaries directory missing: ${dir}`);
} else if (Number.isInteger(shardCount) && shardCount >= 1) {
  const files = readdirSync(dir)
    .filter((file) => /^mock-shard-summary-\d+\.json$/.test(file))
    .sort();
  const seenShards = new Set();
  for (const file of files) {
    let parsed;
    try {
      parsed = JSON.parse(readFileSync(join(dir, file), "utf8"));
    } catch (error) {
      errors.push(`${file}: unparseable summary (${error.message})`);
      continue;
    }
    const shard = parsed?.shard ?? {};
    if (
      !Number.isInteger(shard.index) ||
      !Number.isInteger(shard.count) ||
      shard.count !== shardCount ||
      shard.index < 0 ||
      shard.index >= shardCount
    ) {
      errors.push(`${file}: invalid shard metadata`);
      continue;
    }
    if (seenShards.has(shard.index)) {
      errors.push(`${file}: duplicate shard index ${shard.index}`);
    }
    seenShards.add(shard.index);
    for (const scenario of parsed.scenarios ?? []) {
      if (!expected.has(scenario.id)) {
        errors.push(`unknown scenario result: ${scenario.id}`);
        continue;
      }
      if (executed.has(scenario.id)) {
        errors.push(`duplicate scenario execution: ${scenario.id}`);
        continue;
      }
      executed.set(scenario.id, scenario.status);
    }
  }
  for (let index = 0; index < shardCount; index += 1) {
    if (!seenShards.has(index)) {
      errors.push(`missing shard summary: ${index}`);
    }
  }
  for (const id of expected.keys()) {
    if (!executed.has(id)) {
      errors.push(`scenario not executed: ${id}`);
    }
  }
  for (const [id, status] of executed) {
    if (status !== "passed") {
      errors.push(`failed scenario: ${id}`);
    }
  }
}

const failedScenarios = [...executed.entries()]
  .filter(([, status]) => status !== "passed")
  .map(([id]) => id);

const verdict = {
  shard_count: shardCount,
  expected: expected.size,
  executed: executed.size,
  failed: failedScenarios,
  ok: errors.length === 0,
};
console.log(JSON.stringify(verdict, null, 2));

if (errors.length > 0) {
  for (const error of errors) {
    console.error(`::error::[verify-mock-shards] ${error}`);
  }
  process.exit(1);
}
