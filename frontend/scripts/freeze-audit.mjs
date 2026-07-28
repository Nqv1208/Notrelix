#!/usr/bin/env node
/**
 * FE-RF-00: Freeze Audit Script
 *
 * Runs all CI gates sequentially and reports pass/fail.
 * Does NOT stop on failure — audit needs the full picture.
 * Exits 1 if any gate fails.
 * Writes structured report to docs/frontend-freeze/last-audit-result.json
 * and .freeze/baseline-69eafb1.json.
 *
 * Usage: node ./scripts/freeze-audit.mjs
 */

import { execSync } from 'node:child_process';
import { writeFileSync, mkdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = join(__dirname, '..');

const COMMIT_HASH = '69eafb110e32be040d82c1d87c9df8245e249345';

const GATES = [
  { name: 'TYPECHECK', command: 'pnpm typecheck' },
  { name: 'LINT', command: 'pnpm lint' },
  { name: 'TEST_NODE', command: 'pnpm test:node' },
  { name: 'TEST_WEB', command: 'pnpm test:web' },
  { name: 'CHECK_DEPS', command: 'pnpm check:deps' },
  { name: 'BUILD', command: 'pnpm build' },
  { name: 'VALIDATE', command: 'pnpm validate' },
];

function getToolVersion(cmd) {
  try {
    return execSync(cmd, { encoding: 'utf-8' }).trim();
  } catch {
    return 'unknown';
  }
}

const nodeVersion = process.version;
const pnpmVersion = getToolVersion('pnpm --version');

const COLUMN_WIDTH = 14;
const results = [];

console.log('\n========================================');
console.log('  FE-FREEZE Audit Report');
console.log('========================================\n');
console.log(`  Commit:    ${COMMIT_HASH}`);
console.log(`  Date:      ${new Date().toISOString()}`);
console.log(`  Node:      ${nodeVersion}`);
console.log(`  pnpm:      ${pnpmVersion}`);
console.log(`  Dir:       ${rootDir}\n`);

let anyFailed = false;

for (const gate of GATES) {
  process.stdout.write(`  ${gate.name.padEnd(COLUMN_WIDTH)}`);

  const startMs = Date.now();
  let status = 'PASS';
  let exitCode = 0;
  let errorOutput = '';

  try {
    execSync(gate.command, {
      cwd: rootDir,
      stdio: 'pipe',
      encoding: 'utf-8',
    });
  } catch (err) {
    status = 'FAIL';
    anyFailed = true;
    exitCode = err.status ?? 1;
    errorOutput = (err.stdout ?? '') + '\n' + (err.stderr ?? '');
  }

  const durationMs = Date.now() - startMs;
  const durationStr = `${(durationMs / 1000).toFixed(1)}s`;

  const statusColored =
    status === 'PASS'
      ? `\x1b[32m${status}\x1b[0m`
      : `\x1b[31m${status}\x1b[0m`;

  console.log(`${statusColored}  (${durationStr})`);

  results.push({
    name: gate.name.toLowerCase(),
    gate: gate.name,
    command: gate.command,
    status: status === 'PASS' ? 'passed' : 'failed',
    exitCode,
    durationMs,
    errorOutput: status === 'FAIL' ? errorOutput.slice(0, 2000) : '',
  });
}

console.log('\n----------------------------------------');
const passCount = results.filter((r) => r.status === 'passed').length;
const failCount = results.filter((r) => r.status === 'failed').length;
console.log(`  Summary: ${passCount} PASS, ${failCount} FAIL`);
console.log('========================================\n');

const auditPayload = {
  commit: COMMIT_HASH,
  timestamp: new Date().toISOString(),
  node: nodeVersion,
  pnpm: pnpmVersion,
  status: anyFailed ? 'CONDITIONAL' : 'FROZEN',
  summary: { pass: passCount, fail: failCount },
  commands: results,
};

// Write structured report to docs/frontend-freeze/
const reportDir = join(rootDir, 'docs', 'frontend-freeze');
mkdirSync(reportDir, { recursive: true });
const reportPath = join(reportDir, 'last-audit-result.json');
writeFileSync(reportPath, JSON.stringify(auditPayload, null, 2), 'utf-8');

// Write baseline record to .freeze/
const freezeDir = join(rootDir, '.freeze');
mkdirSync(freezeDir, { recursive: true });
const baselinePath = join(freezeDir, 'baseline-69eafb1.json');
writeFileSync(baselinePath, JSON.stringify(auditPayload, null, 2), 'utf-8');

console.log(`  Report written to: ${reportPath}`);
console.log(`  Baseline written to: ${baselinePath}\n`);

process.exit(anyFailed ? 1 : 0);
