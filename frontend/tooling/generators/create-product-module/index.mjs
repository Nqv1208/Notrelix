#!/usr/bin/env node

/**
 * Product Module Generator
 *
 * Creates a new product module with the standard structure:
 *   packages/product/<name>/
 *     core/
 *     web/
 *     mobile/
 *     testing/ (optional)
 *
 * Usage: node index.mjs <product-name>
 */

import { mkdirSync, writeFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const rootDir = process.env.GENERATOR_ROOT ?? join(__dirname, '../../../..');

const productName = process.argv[2];

if (!productName) {
  console.error('Usage: node index.mjs <product-name>');
  console.error('Example: node index.mjs analytics');
  process.exit(1);
}

const productDir = join(rootDir, `packages/product/${productName}`);

if (existsSync(productDir)) {
  console.error(`Product "${productName}" already exists at ${productDir}`);
  process.exit(1);
}

console.log(`Creating product module: ${productName}`);

function makeTsconfig(extendsRelative) {
  return JSON.stringify({
    extends: extendsRelative,
    compilerOptions: {
      outDir: './dist',
      rootDir: './src',
      baseUrl: '.',
      paths: { '~/*': ['./src/*'] },
    },
    include: ['src/**/*'],
    exclude: ['node_modules', 'dist'],
  }, null, 2) + '\n';
}

// Create core
const coreDir = join(productDir, 'core');
mkdirSync(join(coreDir, 'src'), { recursive: true });
writeFileSync(join(coreDir, 'tsconfig.json'), makeTsconfig('../../../../tooling/tsconfig/base.json'));
writeFileSync(join(coreDir, 'package.json'), JSON.stringify({
  name: `@notrelix/${productName}-core`,
  version: '0.0.1',
  private: true,
  type: 'module',
  main: './src/index.ts',
  types: './src/index.ts',
  exports: { '.': './src/index.ts' },
  scripts: { 'type-check': 'tsc --noEmit', clean: 'rm -rf node_modules dist' },
  devDependencies: { typescript: '^5.0.0' },
}, null, 2));
writeFileSync(join(coreDir, 'src/index.ts'), `// @notrelix/${productName}-core\nexport {};\n`);

// Create web
const webDir = join(productDir, 'web');
mkdirSync(join(webDir, 'src'), { recursive: true });
writeFileSync(join(webDir, 'tsconfig.json'), makeTsconfig('../../../../tooling/tsconfig/react-library.json'));
writeFileSync(join(webDir, 'package.json'), JSON.stringify({
  name: `@notrelix/${productName}-web`,
  version: '0.0.1',
  private: true,
  type: 'module',
  main: './src/index.ts',
  types: './src/index.ts',
  exports: { '.': './src/index.ts' },
  scripts: { 'type-check': 'tsc --noEmit', clean: 'rm -rf node_modules dist' },
  dependencies: { [`@notrelix/${productName}-core`]: 'workspace:*' },
  devDependencies: { typescript: '^5.0.0', '@types/react': '^19.0.0', react: '^19.0.0' },
}, null, 2));
writeFileSync(join(webDir, 'src/index.ts'), `// @notrelix/${productName}-web\nexport {};\n`);

// Create mobile
const mobileDir = join(productDir, 'mobile');
mkdirSync(join(mobileDir, 'src'), { recursive: true });
writeFileSync(join(mobileDir, 'tsconfig.json'), makeTsconfig('../../../../tooling/tsconfig/react-library.json'));
writeFileSync(join(mobileDir, 'package.json'), JSON.stringify({
  name: `@notrelix/${productName}-mobile`,
  version: '0.0.1',
  private: true,
  type: 'module',
  main: './src/index.ts',
  types: './src/index.ts',
  exports: { '.': './src/index.ts' },
  scripts: { 'type-check': 'tsc --noEmit', clean: 'rm -rf node_modules dist' },
  dependencies: { [`@notrelix/${productName}-core`]: 'workspace:*' },
  devDependencies: { typescript: '^5.0.0' },
}, null, 2));
writeFileSync(join(mobileDir, 'src/index.ts'), `// @notrelix/${productName}-mobile\nexport {};\n`);

console.log(`\nCreated product module at: ${productDir}`);
console.log('Sub-packages: core, web, mobile');
