#!/usr/bin/env node
/**
 * Full Frontend Core Freeze Audit
 *
 * Runs the mandatory freeze gates sequentially in the order defined by the
 * CI/Freeze Gates spec and reports PASS/FAIL per gate. Exits non-zero when any
 * gate fails or is missing.
 *
 * Freeze audit order (from the pack):
 *   codegen:check -> check:architecture -> check:architecture-docs ->
 *   typecheck -> lint -> test:node -> test:web -> test:mobile ->
 *   test:integration -> test:generators -> test:fanout -> build -> e2e
 *
 * Constraints:
 *   - prints PASS/FAIL per gate plus a final verdict;
 *   - non-zero exit on failure;
 *   - no hidden artifact directory, no certificate, no docs mutation,
 *     no git mutation.
 *
 * Usage: node ./scripts/freeze-audit.mjs
 */

import { execSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = join(__dirname, '..');

// Mirrors the CI workflow (fe-ci.yml build-web / production-e2e env block) so
// the production build and E2E gates run with valid URLs even outside CI.
const CI_MIRROR_ENV = {
  VITE_API_URL: 'https://api.example.invalid/api/v1',
  VITE_WS_URL: 'wss://api.example.invalid/realtime',
  VITE_APP_URL: 'http://127.0.0.1:4173',
  VITE_MOCK_API: 'false',
};

const GATES = [
  { name: 'CODEGEN_CHECK', script: 'codegen:check', required: true },
  { name: 'ARCHITECTURE', script: 'check:architecture', required: true },
  { name: 'ARCHITECTURE_DOCS', script: 'check:architecture-docs', required: true },
  { name: 'TYPECHECK', script: 'typecheck', required: true },
  { name: 'LINT', script: 'lint', required: true },
  { name: 'TEST_NODE', script: 'test:node', required: true },
  { name: 'TEST_WEB', script: 'test:web', required: true },
  { name: 'TEST_MOBILE', script: 'test:mobile', required: true },
  { name: 'TEST_INTEGRATION', script: 'test:integration', required: true },
  { name: 'TEST_GENERATORS', script: 'test:generators', required: true },
  { name: 'TEST_FANOUT', script: 'test:fanout', required: true },
  { name: 'BUILD', script: 'build', required: true },
  { name: 'E2E', script: 'e2e', required: true },
];

function readRootPackageJson() {
  const raw = readFileSync(join(rootDir, 'package.json'), 'utf8');
  return JSON.parse(raw);
}

function scriptExists(pkg, script) {
  return typeof pkg.scripts?.[script] === 'string';
}

function runGate(gate) {
  const pkg = readRootPackageJson();

  if (!scriptExists(pkg, gate.script)) {
    return { gate, status: 'MISSING', detail: `script "${gate.script}" not defined` };
  }

  try {
    execSync(`pnpm ${gate.script}`, {
      cwd: rootDir,
      stdio: ['ignore', 'pipe', 'pipe'],
      env: { ...process.env, ...CI_MIRROR_ENV, CI: '1' },
      timeout: 60 * 60 * 1000,
    });
    return { gate, status: 'PASS' };
  } catch (error) {
    const tail = String(error.stdout || '').split('\n').slice(-8).join('\n');
    return { gate, status: 'FAIL', detail: tail.trim() || String(error.message) };
  }
}

function main() {
  console.log('\n========================================');
  console.log('  FULL FRONTEND CORE FREEZE AUDIT');
  console.log('========================================\n');

  const results = GATES.map(runGate);

  let failed = 0;
  let missing = 0;

  console.log('  Gate                       Result');
  console.log('  ' + '-'.repeat(38));
  for (const { gate, status, detail } of results) {
    const pad = gate.name.padEnd(27);
    console.log(`  ${pad} ${status}`);
    if (status === 'FAIL') {
      failed += 1;
      if (detail) {
        console.log(`    → ${detail.split('\n').join('\n    → ')}`);
      }
    } else if (status === 'MISSING') {
      missing += 1;
      console.log(`    → ${detail}`);
    }
  }
  console.log('  ' + '-'.repeat(38));

  const passed = results.length - failed - missing;
  console.log(`\n  Passed: ${passed} | Failed: ${failed} | Missing: ${missing}`);

  if (failed > 0 || missing > 0) {
    console.log('\n  VERDICT: NOT FROZEN\n');
    process.exit(1);
  }

  console.log('\n  VERDICT: FROZEN — all mandatory gates pass\n');
  process.exit(0);
}

main();
