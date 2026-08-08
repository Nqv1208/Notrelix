import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { execFileSync } from 'node:child_process';
import { existsSync, mkdtempSync, rmSync, readdirSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const generatorsDir = join(dirname(fileURLToPath(import.meta.url)), '..');

let tempDir: string;

beforeEach(() => {
  tempDir = mkdtempSync(join(tmpdir(), 'notrelix-gen-'));
});

afterEach(() => {
  rmSync(tempDir, { recursive: true, force: true });
});

function runGenerator(generator: string, name: string): void {
  execFileSync(process.execPath, [join(generatorsDir, generator, 'index.mjs'), name], {
    env: { ...process.env, GENERATOR_ROOT: tempDir },
    stdio: 'pipe',
  });
}

describe('create-feature generator', () => {
  it('scaffolds a feature package with the canonical structure', () => {
    runGenerator('create-feature', 'billing');

    const featureDir = join(tempDir, 'packages/features/billing');
    expect(existsSync(featureDir)).toBe(true);

    for (const dir of [
      'src/core/api',
      'src/core/query',
      'src/core/mutations',
      'src/core/model',
      'src/core/schemas',
      'src/core/permissions',
      'src/web/screens',
      'src/web/components',
      'src/mobile/screens',
      'src/mobile/components',
      'src/testing',
    ]) {
      expect(existsSync(join(featureDir, dir)), dir).toBe(true);
    }

    const pkg = JSON.parse(readFileSync(join(featureDir, 'package.json'), 'utf8'));
    expect(pkg.name).toBe('@notrelix/features-billing');
    expect(pkg.exports['./core/query/keys']).toBe('./src/core/query/keys.ts');

    const keys = readFileSync(join(featureDir, 'src/core/query/keys.ts'), 'utf8');
    expect(keys).toContain('billingQueryKeys');
    expect(keys).toContain("all: ['billing']");
  });

  it('refuses to overwrite an existing feature', () => {
    runGenerator('create-feature', 'billing');

    expect(() => runGenerator('create-feature', 'billing')).toThrow(
      /already exists/,
    );
  });
});

describe('create-product-module generator', () => {
  it('scaffolds core, web, and mobile sub-packages', () => {
    runGenerator('create-product-module', 'analytics');

    const moduleDir = join(tempDir, 'packages/product/analytics');
    expect(existsSync(join(moduleDir, 'core/src/index.ts'))).toBe(true);
    expect(existsSync(join(moduleDir, 'web/src/index.ts'))).toBe(true);
    expect(existsSync(join(moduleDir, 'mobile/src/index.ts'))).toBe(true);

    const corePkg = JSON.parse(
      readFileSync(join(moduleDir, 'core/package.json'), 'utf8'),
    );
    expect(corePkg.name).toBe('@notrelix/analytics-core');

    const webPkg = JSON.parse(
      readFileSync(join(moduleDir, 'web/package.json'), 'utf8'),
    );
    expect(webPkg.dependencies['@notrelix/analytics-core']).toBe('workspace:*');
  });

  it('refuses to overwrite an existing product module', () => {
    runGenerator('create-product-module', 'analytics');

    expect(() => runGenerator('create-product-module', 'analytics')).toThrow(
      /already exists/,
    );
  });
});

describe('create-ui-component generator', () => {
  it('scaffolds a component under packages/ui/web/src/components/ui', () => {
    runGenerator('create-ui-component', 'alert');

    const componentDir = join(tempDir, 'packages/ui/web/src/components/ui');
    const files = readdirSync(componentDir);
    expect(files).toContain('alert.tsx');

    const source = readFileSync(join(componentDir, 'alert.tsx'), 'utf8');
    expect(source).toContain('React.forwardRef');
    expect(source).toContain('Alert');
    expect(source).toContain('displayName');
  });

  it('refuses to overwrite an existing component', () => {
    runGenerator('create-ui-component', 'alert');

    expect(() => runGenerator('create-ui-component', 'alert')).toThrow(
      /already exists/,
    );
  });
});
