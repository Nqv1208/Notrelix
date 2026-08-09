/**
 * Shared workspace helpers for the Notrelix generators.
 *
 * Registration contract (pack 13-TEAM-FANOUT-GOLDEN-PATH-SPEC):
 *   - Every generated package MUST be registered in the architecture manifest.
 *   - The generated package-boundary docs MUST be refreshed afterwards.
 *   - All writes honor GENERATOR_ROOT so golden tests run in a temporary
 *     workspace fixture and never modify the real worktree (GEN-030).
 */

import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { execFileSync } from 'node:child_process';

export const MANIFEST_RELATIVE_PATH =
  'tooling/dependency-rules/src/architecture-manifest.ts';
export const DOCS_SCRIPT_RELATIVE_PATH =
  'tooling/dependency-rules/src/generate-architecture-docs.ts';

export const FEATURES_SECTION_ANCHOR = /(\s*\/\/ ── Apps ─)/;
export const PRODUCT_SECTION_ANCHOR = /(\s*\/\/ ── Features ─)/;

export function manifestPath(rootDir) {
  return join(rootDir, MANIFEST_RELATIVE_PATH);
}

export function hasManifest(rootDir) {
  return existsSync(manifestPath(rootDir));
}

function formatAllowedImports(allowedInternalImports) {
  if (Array.isArray(allowedInternalImports)) {
    return `[${allowedInternalImports.map((name) => `'${name}'`).join(', ')}]`;
  }
  return allowedInternalImports;
}

function formatEntry(entry) {
  const lines = [
    '  {',
    `    packageName: '${entry.packageName}',`,
    `    relativePath: '${entry.relativePath}',`,
    `    layer: '${entry.layer}',`,
    `    freezeScope: '${entry.freezeScope}',`,
    `    allowedInternalImports: ${formatAllowedImports(entry.allowedInternalImports)},`,
    '  },',
  ];
  return lines.join('\n');
}

/**
 * Inserts manifest entries immediately before the given section anchor.
 * Returns true when the manifest was found and updated.
 */
export function registerManifestEntries(rootDir, entries, anchor) {
  const path = manifestPath(rootDir);
  if (!existsSync(path)) return false;

  const source = readFileSync(path, 'utf8');
  const index = source.search(anchor);
  if (index === -1) {
    throw new Error(
      `Manifest section anchor not found in ${path}; registration aborted`,
    );
  }

  const block = entries.map(formatEntry).join('\n');
  const updated = source.slice(0, index) + block + '\n' + source.slice(index);
  writeFileSync(path, updated);
  return true;
}

/**
 * Regenerates the package-boundary docs from the (already updated) manifest.
 * Returns false when the docs script is not present (bare temp fixture).
 */
export function refreshArchitectureDocs(rootDir) {
  const script = join(rootDir, DOCS_SCRIPT_RELATIVE_PATH);
  if (!existsSync(script)) return false;

  const tsxBin = join(rootDir, 'node_modules/.bin', process.platform === 'win32' ? 'tsx.cmd' : 'tsx');
  execFileSync(tsxBin, [script], {
    env: { ...process.env, GENERATOR_ROOT: resolve(rootDir) },
    stdio: 'pipe',
  });
  return true;
}
