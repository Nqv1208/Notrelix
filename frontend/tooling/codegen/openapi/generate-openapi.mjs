#!/usr/bin/env node

/**
 * OpenAPI Code Generator
 *
 * Generates TypeScript types and API client from OpenAPI spec.
 * Target: packages/foundation/contracts/src/generated/rest/
 *
 * Usage: node openapi/generate-openapi.mjs
 */

import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = join(__dirname, '../../..');
const specPath = join(rootDir, 'openapi.json');
const outputDir = join(rootDir, 'packages/foundation/contracts/src/generated/rest');

console.log('OpenAPI Code Generator');
console.log('======================');
console.log(`Spec: ${specPath}`);
console.log(`Output: ${outputDir}`);

if (!existsSync(specPath)) {
  console.log('\nNo openapi.json found. Skipping generation.');
  console.log('Place your OpenAPI spec at the project root as openapi.json');
  process.exit(0);
}

// Ensure output directory exists
mkdirSync(outputDir, { recursive: true });

// Read and parse spec
const spec = JSON.parse(readFileSync(specPath, 'utf-8'));
console.log(`\nParsed spec: ${spec.info?.title || 'Unknown'} v${spec.info?.version || '?'}`);

// Generate types (placeholder - would use openapi-typescript or orval in production)
const typesContent = `/**
 * Auto-generated from OpenAPI spec
 * DO NOT EDIT MANUALLY
 *
 * Spec: ${spec.info?.title || 'Unknown'} v${spec.info?.version || '?'}
 * Generated: ${new Date().toISOString()}
 */

export type paths = Record<string, unknown>;
export type components = Record<string, unknown>;
export type operations = Record<string, unknown>;
`;

writeFileSync(join(outputDir, 'types.ts'), typesContent);
console.log('Generated types.ts');

// Generate client placeholder
const clientContent = `/**
 * Auto-generated API client
 * DO NOT EDIT MANUALLY
 */

import type { paths } from './types';

export type ApiClient = {
  get: <T>(path: string) => Promise<T>;
  post: <T>(path: string, body: unknown) => Promise<T>;
  put: <T>(path: string, body: unknown) => Promise<T>;
  patch: <T>(path: string, body: unknown) => Promise<T>;
  delete: <T>(path: string) => Promise<T>;
};
`;

writeFileSync(join(outputDir, 'client.ts'), clientContent);
console.log('Generated client.ts');

console.log('\nDone!');
