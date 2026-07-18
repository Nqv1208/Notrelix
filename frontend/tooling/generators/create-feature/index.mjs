#!/usr/bin/env node

/**
 * Feature Package Generator
 *
 * Creates a new feature package with the standard structure:
 *   packages/features/<name>/
 *     core/
 *     web/
 *     mobile/
 *     testing/
 *     index.ts
 *     package.json
 *     tsconfig.json
 *
 * Usage: node index.mjs <feature-name>
 */

import { mkdirSync, writeFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = join(__dirname, '../../../..');

const featureName = process.argv[2];

if (!featureName) {
  console.error('Usage: node index.mjs <feature-name>');
  console.error('Example: node index.mjs billing');
  process.exit(1);
}

const featureDir = join(rootDir, `packages/features/${featureName}`);
const pkgName = `@notrelix/features-${featureName}`;

if (existsSync(featureDir)) {
  console.error(`Feature "${featureName}" already exists at ${featureDir}`);
  process.exit(1);
}

console.log(`Creating feature: ${featureName}`);
console.log(`Package: ${pkgName}`);

// Create directories
mkdirSync(join(featureDir, 'src/core/api'), { recursive: true });
mkdirSync(join(featureDir, 'src/core/query'), { recursive: true });
mkdirSync(join(featureDir, 'src/core/mutations'), { recursive: true });
mkdirSync(join(featureDir, 'src/core/model'), { recursive: true });
mkdirSync(join(featureDir, 'src/core/schemas'), { recursive: true });
mkdirSync(join(featureDir, 'src/core/permissions'), { recursive: true });
mkdirSync(join(featureDir, 'src/web/screens'), { recursive: true });
mkdirSync(join(featureDir, 'src/web/components'), { recursive: true });
mkdirSync(join(featureDir, 'src/mobile/screens'), { recursive: true });
mkdirSync(join(featureDir, 'src/mobile/components'), { recursive: true });
mkdirSync(join(featureDir, 'src/testing'), { recursive: true });

// Create package.json
writeFileSync(join(featureDir, 'package.json'), JSON.stringify({
  name: pkgName,
  version: '0.0.1',
  private: true,
  type: 'module',
  main: './src/index.ts',
  types: './src/index.ts',
  exports: {
    '.': './src/index.ts',
    './core': './src/core/index.ts',
    './core/query/keys': './src/core/query/keys.ts',
    './web': './src/web/index.ts',
    './mobile': './src/mobile/index.ts',
  },
  scripts: {
    'type-check': 'tsc --noEmit',
    'test': 'bun test --pass-with-no-tests',
    clean: 'rm -rf node_modules dist',
  },
  dependencies: {},
  devDependencies: {
    typescript: '^5.0.0',
  },
}, null, 2));

// Create tsconfig.json
writeFileSync(join(featureDir, 'tsconfig.json'), JSON.stringify({
  extends: '../../../tooling/tsconfig/react-library.json',
  compilerOptions: {
    outDir: './dist',
    rootDir: './src',
    baseUrl: '.',
    paths: { '~/*': ['./src/*'] },
  },
  include: ['src/**/*'],
  exclude: ['node_modules', 'dist'],
}, null, 2));

// Create index.ts
writeFileSync(join(featureDir, 'src/index.ts'), `/**
 * @notrelix/features-${featureName} — ${featureName} feature package.
 */

// Core
export type {} from './core';

// Web
// export {} from './web';
`);

// Create core index.ts
writeFileSync(join(featureDir, 'src/core/index.ts'), `/**
 * @notrelix/features-${featureName}/core — Core types and API contracts.
 */

// Types
// export type {} from './model/${featureName}.types';

// API
// export { create${featureName.charAt(0).toUpperCase() + featureName.slice(1)}Api } from './api/${featureName}.api';
`);

// Create query keys
writeFileSync(join(featureDir, 'src/core/query/keys.ts'), `/**
 * @notrelix/features-${featureName}/core/query — Query keys.
 */

export const ${featureName}QueryKeys = {
  all: ['${featureName}'] as const,
  // detail: (id: string) => ['${featureName}', 'detail', id] as const,
} as const;
`);

// Create web index.ts
writeFileSync(join(featureDir, 'src/web/index.ts'), `/**
 * @notrelix/features-${featureName}/web — Web components and hooks.
 */
`);

// Create mobile index.ts
writeFileSync(join(featureDir, 'src/mobile/index.ts'), `/**
 * @notrelix/features-${featureName}/mobile — Mobile components and screens.
 */
`);

console.log(`\nCreated feature package at: ${featureDir}`);
console.log('\nNext steps:');
console.log(`1. Add dependencies to ${featureDir}/package.json`);
console.log(`2. Implement types in src/core/model/`);
console.log(`3. Implement API in src/core/api/`);
console.log(`4. Implement web screens in src/web/screens/`);
console.log(`5. Add workspace reference in root package.json if needed`);
