#!/usr/bin/env node
/**
 * FE-FZ-00: Freeze Audit Script
 *
 * Runs all CI gates sequentially and reports pass/fail.
 * Does NOT stop on failure — audit needs the full picture.
 * Exits 1 if any gate fails.
 *
 * Usage: node ./scripts/freeze-audit.mjs
 */

import { execSync } from 'node:child_process';
import { writeFileSync, mkdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = join(__dirname, '..');

const GATES = [
  { name: 'TYPECHECK', command: 'pnpm typecheck' },
  { name: 'LINT', command: 'pnpm lint' },
  { name: 'TEST', command: 'pnpm test' },
  { name: 'CHECK_DEPS', command: 'pnpm check:deps' },
  { name: 'BUILD', command: 'pnpm build' },
  { name: 'VALIDATE', command: 'pnpm validate' },
];

const COLUMN_WIDTH = 12;
const results = [];

console.log('\n========================================');
console.log('  FE-FREEZE Audit Report');
console.log('========================================\n');
console.log(`  Date: ${new Date().toISOString()}`);
console.log(`  Dir:  ${rootDir}\n`);

let anyFailed = false;

for (const gate of GATES) {
  process.stdout.write(`  ${gate.name.padEnd(COLUMN_WIDTH)}`);

  const startMs = Date.now();
  let status = 'PASS';
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
    errorOutput =
      (err.stdout ?? '') + '\n' + (err.stderr ?? '');
  }

  const durationMs = Date.now() - startMs;
  const durationStr = `${(durationMs / 1000).toFixed(1)}s`;

  const statusColored =
    status === 'PASS'
      ? `\x1b[32m${status}\x1b[0m`
      : `\x1b[31m${status}\x1b[0m`;

  console.log(`${statusColored}  (${durationStr})`);

  results.push({
    gate: gate.name,
    command: gate.command,
    status,
    durationMs,
    errorOutput: status === 'FAIL' ? errorOutput.slice(0, 2000) : '',
  });
}

console.log('\n----------------------------------------');
const passCount = results.filter((r) => r.status === 'PASS').length;
const failCount = results.filter((r) => r.status === 'FAIL').length;
console.log(`  Summary: ${passCount} PASS, ${failCount} FAIL`);
console.log('========================================\n');

// Write structured report to docs/frontend-freeze/
const reportDir = join(rootDir, 'docs', 'frontend-freeze');
mkdirSync(reportDir, { recursive: true });

const reportPath = join(reportDir, 'last-audit-result.json');
writeFileSync(
  reportPath,
  JSON.stringify(
    {
      auditedAt: new Date().toISOString(),
      summary: { pass: passCount, fail: failCount },
      gates: results,
    },
    null,
    2,
  ),
  'utf-8',
);

console.log(`  Report written to: ${reportPath}\n`);

process.exit(anyFailed ? 1 : 0);
