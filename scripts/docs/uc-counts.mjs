#!/usr/bin/env node
// Prints the use-case count table (Markdown) from the catalogs in docs/workstreams/use-cases; writes no file.
//   node scripts/docs/uc-counts.mjs             print the table (CI appends it to the job summary)
//   node scripts/docs/uc-counts.mjs --validate  exit 1 on an unknown State value or a duplicate UC id
// The State cell is found from each table's own header row (the index table has State as cell 9),
// so extra pipes in later prose cells do not move it. Rows in a table without a State column (retired lists) are skipped.
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const DIR = join(dirname(fileURLToPath(import.meta.url)), "../../docs/workstreams/use-cases");
const STATES = ["DEFINED", "IMPLEMENTING", "INTEGRATING", "HARDENING", "DONE", "BLOCKED"];
// Row order of the table. A file may hold several contexts, told apart by the UC id prefix.
const CONTEXTS = [
  ["Accounts", "accounts.md", "ACC"],
  ["Identity", "identity.md", "IDN"],
  ["Workspaces", "workspaces.md", "WSP"],
  ["Governance", "governance.md", "GOV"],
  ["Work Management", "work-management.md", "WM"],
  ["Documents", "documents.md", "DCT"],
  ["Collaboration", "collaboration.md", "COL"],
  ["Chat", "chat.md", "CHT"],
  ["Automation", "automation-integrations.md", "AUT"],
  ["Integrations", "automation-integrations.md", "INT"],
  ["Billing", "billing.md", "BIL"],
  ["Analytics", "analytics.md", "ANA"],
  ["Platform and Foundation", "platform-foundation.md", "PLT"],
];

const cells = (line) => line.split("|").slice(1).map((c) => c.trim());

/** Returns [{id, state}] for every `| UC-` row that sits under a header with a State column. */
export function parseRows(text) {
  const rows = [];
  let stateAt = -1;
  for (const line of text.split("\n")) {
    if (!line.startsWith("|")) { stateAt = -1; continue; } // a table ends at its first non-table line
    const c = cells(line);
    if (c[0] === "ID" || c[0] === "UC ID") stateAt = c.indexOf("State");
    else if (c[0].startsWith("UC-") && stateAt >= 0) {
      const state = c[stateAt];
      // The id cell may carry a note, e.g. "UC-WM-A27 (retired; ...)": the id is the first word.
      rows.push({ id: c[0].split(" ")[0], state });
    }
  }
  return rows;
}

export function tally(rowsByFile, notes = []) {
  const seen = new Set();
  const lines = [];
  const total = Array(STATES.length).fill(0);
  for (const [name, file, prefix] of CONTEXTS) {
    const n = Array(STATES.length).fill(0);
    for (const { id, state } of rowsByFile[file]) {
      if (id.split("-")[1] !== prefix) continue;
      if (seen.has(id)) { notes.push(`duplicate use case counted once: ${id}`); continue; }
      seen.add(id);
      const i = STATES.indexOf(state);
      if (i < 0) { notes.push(`unknown state "${state}" on ${id}, not counted`); continue; }
      n[i]++;
    }
    n.forEach((v, i) => (total[i] += v));
    lines.push([name, n.reduce((a, b) => a + b, 0), ...n]);
  }
  lines.push(["Total", total.reduce((a, b) => a + b, 0), ...total]);
  const row = (r) => `| ${r.map((v, i) => (r[0] === "Total" && i ? `**${v}**` : r[0] === "Total" ? `**${v}**` : v)).join(" | ")} |`;
  return [`| Context | Use cases | ${STATES.join(" | ")} |`, `|---|---|${STATES.map(() => "---").join("|")}|`, ...lines.map(row)].join("\n");
}

function selfTest() {
  const hdr = "| ID | Use case | Capability | Actors | BE | FE | Design | Real e2e | State | Gap |";
  const normal = "| UC-ACC-A01 | t | A | x | yes | yes | complete | none | DONE | g |";
  const wide = "| UC-WM-C01 | t | C | Editor | yes | yes | complete | none | HARDENING | Notes: a | b | Evidence |";
  const rows = parseRows(["| ID | Defect |", "| UC-X-1 | no state header |", hdr, normal, wide].join("\n"));
  assert.deepEqual(rows, [{ id: "UC-ACC-A01", state: "DONE" }, { id: "UC-WM-C01", state: "HARDENING" }]);
  const files = Object.fromEntries(CONTEXTS.map(([, f]) => [f, []]));
  files["accounts.md"] = [rows[0], rows[0]];
  const notes = [];
  assert.match(tally(files, notes), /\| Accounts \| 1 \| 0 \| 0 \| 0 \| 0 \| 1 \| 0 \|/);
  assert.equal(notes.length, 1);
}

function main() {
  selfTest();
  const rowsByFile = {};
  for (const [, file] of CONTEXTS) rowsByFile[file] ??= parseRows(readFileSync(join(DIR, file), "utf8"));
  const notes = [];
  const table = tally(rowsByFile, notes);
  if (process.argv.includes("--validate")) {
    notes.forEach((m) => console.error(`[uc-counts] ${m}`));
    if (notes.length) process.exit(1);
    return console.log("[uc-counts] PASS - catalog states are known and use case ids are unique");
  }
  notes.forEach((m) => console.error(`[uc-counts] note: ${m}`));
  console.log(table);
}
if (process.argv[1] === fileURLToPath(import.meta.url)) main();
