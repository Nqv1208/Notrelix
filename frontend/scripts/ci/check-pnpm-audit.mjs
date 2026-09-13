#!/usr/bin/env node
// Structured frontend vulnerability checker for captured pnpm audit JSON.
//
// Judges the captured output of `pnpm audit --json`. pnpm applies the
// package.json auditConfig (ignoreGhsas) before reporting, so the policy
// ignore list is preserved exactly. Any advisory at high or critical
// severity fails the check; malformed or missing JSON fails closed. The
// caller is responsible for failing the shell step when pnpm itself fails.
import { readFileSync, writeFileSync } from "node:fs";

function arg(name) {
  const i = process.argv.indexOf(name);
  if (i < 0 || i + 1 >= process.argv.length) return undefined;
  return process.argv[i + 1];
}

const inputPath = arg("--input");
let parsed;
try {
  parsed = JSON.parse(readFileSync(inputPath, "utf8"));
} catch (error) {
  console.error(
    `::error::[check-pnpm-audit] malformed audit report: ${error.message}`,
  );
  console.error(JSON.stringify({ ok: false, reason: "malformed report" }));
  process.exit(1);
}

let advisories = parsed?.advisories ?? [];
if (
  !Array.isArray(advisories) &&
  typeof advisories === "object" &&
  advisories !== null
) {
  advisories = Object.values(advisories);
}
if (!Array.isArray(advisories)) {
  console.error(
    "::error::[check-pnpm-audit] malformed audit report: advisories section missing",
  );
  console.error(
    JSON.stringify({ ok: false, reason: "advisories section missing" }),
  );
  process.exit(1);
}

const failing = advisories
  .filter(
    (advisory) =>
      advisory?.severity === "high" || advisory?.severity === "critical",
  )
  .map((advisory) => ({
    module: advisory.module_name ?? advisory.name ?? "unknown",
    severity: advisory.severity,
    title: advisory.title ?? advisory.github_advisory_id ?? advisory.id ?? "",
  }));

const summary = {
  ok: failing.length === 0,
  audited: advisories.length,
  failing,
};
console.log(JSON.stringify(summary, null, 2));
if (arg("--output")) {
  writeFileSync(arg("--output"), `${JSON.stringify(summary, null, 2)}\n`);
}
if (failing.length > 0) {
  for (const finding of failing) {
    console.error(
      `::error::[check-pnpm-audit] ${finding.severity}: ${finding.module} ${finding.title}`,
    );
  }
  process.exit(1);
}
