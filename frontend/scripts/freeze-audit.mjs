#!/usr/bin/env node
/**
 * FE-FZ-00: Freeze Audit Script
 *
 * Runs all CI gates sequentially and reports pass/fail.
 * Does NOT stop on failure — audit needs the full picture.
 * Exits 1 if any gate fails.
 * Writes structured report to docs/frontend-freeze/last-audit-result.json.
 *
 * This script does not issue an immutable freeze certificate. Certificate
 * creation belongs to FE-FZ-17 after all platform gates are implemented.
 *
 * Usage: node ./scripts/freeze-audit.mjs
 */

import { execSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = join(__dirname, '..');

const GATES = [
  { name: 'TYPECHECK', command: 'pnpm typecheck' },
  { name: 'LINT', command: 'pnpm lint' },
  { name: 'TEST_NODE', command: 'pnpm test:node' },
  { name: 'TEST_WEB', command: 'pnpm test:web' },
  { name: 'ARCHITECTURE', command: 'pnpm check:architecture' },
  { name: 'CODEGEN_CHECK', command: 'pnpm codegen:check' },
  { name: 'STORYBOOK_BUILD', command: 'pnpm --filter @notrelix/storybook-web build' },
  { name: 'BUILD_WEB', command: 'pnpm --filter @notrelix/app-web build' },
  { name: 'BUILD_MARKETING', command: 'pnpm --filter @notrelix/app-marketing build' },
  { name: 'PRODUCTION_STARTUP', command: 'node ./scripts/check-production-startup.mjs' },
  { name: 'CRITICAL_E2E', command: 'pnpm e2e' },
];

function getToolVersion(cmd) {
  try {
    return execSync(cmd, { encoding: 'utf-8' }).trim();
  } catch {
    return 'unknown';
  }
}

function getGitOutput(command) {
  return execSync(command, { cwd: rootDir, encoding: 'utf-8' }).trim();
}

function sha256File(relativePath) {
  const absolutePath = join(rootDir, relativePath);
  if (!existsSync(absolutePath)) return null;
  return createHash('sha256').update(readFileSync(absolutePath)).digest('hex');
}

const nodeVersion = process.version;
const pnpmVersion = getToolVersion('pnpm --version');
const commit = getGitOutput('git rev-parse HEAD');
const workingTreeStatus = getGitOutput('git status --porcelain');
const workingTreeClean = workingTreeStatus.length === 0;

const COLUMN_WIDTH = 14;
const results = [];

console.log('\n========================================');
console.log('  FE-FREEZE Audit Report');
console.log('========================================\n');
console.log(`  Commit:    ${commit}`);
console.log(`  Worktree:  ${workingTreeClean ? 'clean' : 'dirty'}`);
console.log(`  Date:      ${new Date().toISOString()}`);
console.log(`  Node:      ${nodeVersion}`);
console.log(`  pnpm:      ${pnpmVersion}`);
console.log(`  Dir:       ${rootDir}\n`);

let anyFailed = !workingTreeClean;

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
  scope: 'frontend-web-platform-v1.0.0',
  commit,
  timestamp: new Date().toISOString(),
  workingTreeClean,
  node: nodeVersion,
  pnpm: pnpmVersion,
  lockfileSha256: sha256File('pnpm-lock.yaml'),
  openApiSpecSha256: sha256File('artifacts/contracts/openapi.v1.json'),
  realtimeSpecSha256: sha256File('artifacts/contracts/realtime.v1.json'),
  status: 'NOT_FROZEN',
  reason: anyFailed
    ? workingTreeClean
      ? 'One or more frontend freeze gates failed.'
      : 'Working tree is dirty; immutable frontend freeze certificate cannot be issued.'
    : 'Baseline gates passed, but platform freeze certificate is not issued until FE-FZ-17.',
  excludedScopes: ['mobile', 'marketing-feature-completeness'],
  summary: { pass: passCount, fail: failCount },
  commands: results,
};

// Write structured report to docs/frontend-freeze/
const reportDir = join(rootDir, 'docs', 'frontend-freeze');
mkdirSync(reportDir, { recursive: true });
const reportPath = join(reportDir, 'last-audit-result.json');
writeFileSync(reportPath, JSON.stringify(auditPayload, null, 2), 'utf-8');

console.log(`  Report written to: ${reportPath}`);

process.exit(anyFailed ? 1 : 0);
