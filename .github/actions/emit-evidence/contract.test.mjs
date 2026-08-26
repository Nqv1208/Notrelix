#!/usr/bin/env node
// Contract test: emit.mjs records must satisfy the Python aggregator's
// tamper-evidence canonicalization exactly.
//   sha256(json.dumps(body, separators=(',',':'), sort_keys=True))
import { spawnSync } from "node:child_process";
import { mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const tmp = mkdtempSync(join(tmpdir(), "emit-evidence-"));
const out = join(tmp, "evidence");
const res = spawnSync(
  process.execPath,
  [
    new URL("./emit.mjs", import.meta.url).pathname,
    "--proof-id",
    "ui:foundation",
    "--component-id",
    "frontend-ui",
    "--metadata",
    "published=true",
    "--out",
    out,
  ],
  {
    env: {
      ...process.env,
      GITHUB_SHA: "a".repeat(40),
      GITHUB_RUN_ID: "123",
      GITHUB_RUN_ATTEMPT: "1",
      GITHUB_WORKFLOW: "Frontend CI",
      GITHUB_JOB: "ui-foundation",
    },
    encoding: "utf8",
  },
);
if (res.status !== 0) {
  console.error(res.stderr || res.stdout);
  process.exit(1);
}
const record = JSON.parse(
  readFileSync(join(out, "ui-foundation--frontend-ui.json"), "utf8"),
);

const py = spawnSync(
  "python3",
  [
    "-c",
    `
import hashlib, json, sys
record = json.loads(sys.stdin.read())
digest = record.pop("record_sha256")
body = json.dumps(record, separators=(",", ":"), sort_keys=True)
actual = hashlib.sha256(body.encode()).hexdigest()
sys.exit(0 if actual == digest else 1)
`,
  ],
  { input: JSON.stringify(record), encoding: "utf8" },
);

rmSync(tmp, { recursive: true, force: true });
if (py.status !== 0) {
  console.error(
    "FAIL: record_sha256 does not match aggregator canonicalization",
  );
  process.exit(1);
}
console.log("emit-evidence contract PASS");
