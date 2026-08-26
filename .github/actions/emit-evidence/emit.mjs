#!/usr/bin/env node
// Execution-safe evidence emitter.
//
// Dependency-free Node primitive so proof records can be written from any
// provider environment — including Playwright renderer containers that ship no
// jq/sha256sum. Canonicalization must stay byte-identical to the aggregator
// contract in tools/deliveryctl/evidence.py:
//   sha256(json.dumps(body, separators=(',', ':'), sort_keys=True))
// where body is the record without record_sha256.
import { createHash } from "node:crypto";
import { mkdirSync, writeFileSync, appendFileSync } from "node:fs";
import { dirname } from "node:path";

function arg(name) {
  const i = process.argv.indexOf(`--${name}`);
  return i < 0 ? undefined : process.argv[i + 1];
}

function slug(value) {
  return String(value)
    .replace(/[^A-Za-z0-9._-]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .toLowerCase();
}

function sortDeep(value) {
  if (Array.isArray(value)) return value.map(sortDeep);
  if (value && typeof value === "object") {
    const out = {};
    for (const key of Object.keys(value).sort())
      out[key] = sortDeep(value[key]);
    return out;
  }
  return value;
}

const proofId = arg("proof-id");
if (!proofId) throw new Error("missing --proof-id");
const componentId = arg("component-id") ?? "repository";
const metadataInput = arg("metadata") ?? "";
const outDir = arg("out");
if (!outDir) throw new Error("missing --out");

let metadata = {};
if (metadataInput !== "") {
  const eq = metadataInput.indexOf("=");
  if (eq <= 0) throw new Error(`invalid metadata ${metadataInput}`);
  metadata = { [metadataInput.slice(0, eq)]: metadataInput.slice(eq + 1) };
}

// Compact RFC3339 UTC clock, matching the previous jq date format.
const createdAt = new Date().toISOString().replace(/\.\d{3}Z$/, "Z");

const body = sortDeep({
  api_version: "delivery.notrelix.dev/v1",
  kind: "EvidenceRecord",
  proof_id: proofId,
  component_id: componentId,
  status: "passed",
  source_sha: process.env.GITHUB_SHA ?? "",
  run_id: process.env.GITHUB_RUN_ID ?? "",
  run_attempt: process.env.GITHUB_RUN_ATTEMPT ?? "",
  workflow: process.env.GITHUB_WORKFLOW ?? "",
  job: process.env.GITHUB_JOB ?? "",
  created_at: createdAt,
  metadata,
});

const canonical = JSON.stringify(body);
const digest = createHash("sha256").update(canonical, "utf8").digest("hex");
const record = sortDeep({ ...body, record_sha256: digest });

const path = `${outDir}/${slug(proofId)}--${slug(componentId)}.json`;
mkdirSync(dirname(path), { recursive: true });
writeFileSync(path, `${JSON.stringify(record, null, 2)}\n`);

if (process.env.GITHUB_OUTPUT) {
  appendFileSync(process.env.GITHUB_OUTPUT, `path=${path}\n`);
  appendFileSync(
    process.env.GITHUB_OUTPUT,
    `artifact_name=ci-evidence-${slug(proofId)}-${slug(componentId)}\n`,
  );
} else {
  console.log(`path=${path}`);
}
