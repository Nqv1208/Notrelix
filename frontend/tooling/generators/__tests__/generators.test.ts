import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { execFileSync } from 'node:child_process';
import {
  existsSync,
  mkdtempSync,
  rmSync,
  readdirSync,
  readFileSync,
  copyFileSync,
  symlinkSync,
  mkdirSync,
  statSync,
  writeFileSync,
} from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const generatorsDir = join(dirname(fileURLToPath(import.meta.url)), '..');
const frontendRoot = resolve(generatorsDir, '../..');
const repoRoot = resolve(frontendRoot, '..');
const bin = (name: string) => join(frontendRoot, 'node_modules/.bin', name);

let tempDir: string;

beforeEach(() => {
  tempDir = mkdtempSync(join(tmpdir(), 'notrelix-gen-'));
});

afterEach(() => {
  rmSync(tempDir, { recursive: true, force: true });
});

function runGenerator(generator: string, ...args: string[]): void {
  execFileSync(process.execPath, [join(generatorsDir, generator, 'index.mjs'), ...args], {
    env: { ...process.env, GENERATOR_ROOT: tempDir },
    stdio: 'pipe',
  });
}

function runGeneratorIn(generator: string, rootDir: string, ...args: string[]): void {
  execFileSync(process.execPath, [join(generatorsDir, generator, 'index.mjs'), ...args], {
    env: { ...process.env, GENERATOR_ROOT: rootDir },
    stdio: 'pipe',
  });
}

// ── Fixture workspace for golden tests ────────────────────────────────
//
// Mirrors the real frontend workspace in a temp directory:
//   - node_modules, tooling (except dependency-rules and storybook) and
//     package dirs that generators never write to: symlinked.
//   - tooling/dependency-rules, packages/ui/web, packages/ui/mobile and
//     tooling/storybook: real copies, because generators edit them.
// The generator then runs against the fixture; gates (typecheck, lint,
// test, architecture) run against the fixture; GEN-030 asserts the real
// worktree is untouched.

const COPY_EXCLUDES = new Set([
  'node_modules',
  'dist',
  '.turbo',
  '.next',
  '.expo',
  'storybook-static',
  'tsconfig.tsbuildinfo',
  'coverage',
  'test-results',
]);

function copyDirRecursive(src: string, dest: string): void {
  for (const entry of readdirSync(src)) {
    if (COPY_EXCLUDES.has(entry)) continue;
    const from = join(src, entry);
    const to = join(dest, entry);
    if (statSync(from).isDirectory()) {
      mkdirSync(to, { recursive: true });
      copyDirRecursive(from, to);
    } else {
      copyFileSync(from, to);
    }
  }
}

function symlinkArea(area: string, excludes: string[] = []): void {
  const srcArea = join(frontendRoot, area);
  const destArea = join(tempDir, area);
  mkdirSync(destArea, { recursive: true });
  for (const entry of readdirSync(srcArea)) {
    if (excludes.includes(entry)) continue;
    symlinkSync(join(srcArea, entry), join(destArea, entry), 'dir');
  }
}

function buildFixtureWorkspace(): void {
  // Root config files (vitest.workspace.ts is intentionally excluded: it
  // would make the fixture vitest run pick up workspace projects)
  for (const file of [
    'package.json',
    'pnpm-workspace.yaml',
    'tsconfig.base.json',
    'turbo.json',
  ]) {
    copyFileSync(join(frontendRoot, file), join(tempDir, file));
  }

  // node_modules symlink
  symlinkSync(join(frontendRoot, 'node_modules'), join(tempDir, 'node_modules'), 'dir');

  // Apps: never written by generators
  symlinkArea('apps');

  // Packages: symlink everything except the ui packages generators write to
  const packagesSrc = join(frontendRoot, 'packages');
  mkdirSync(join(tempDir, 'packages'), { recursive: true });
  for (const sub of readdirSync(packagesSrc)) {
    mkdirSync(join(tempDir, 'packages', sub), { recursive: true });
    for (const pkg of readdirSync(join(packagesSrc, sub))) {
      const from = join(packagesSrc, sub, pkg);
      const to = join(tempDir, 'packages', sub, pkg);
      if (sub === 'ui' && ['web', 'mobile'].includes(pkg)) {
        mkdirSync(to, { recursive: true });
        copyDirRecursive(from, to);
        // pnpm links a package's deps into the package's own node_modules;
        // re-link them for the fixture copy so tsc/vitest resolve react etc.
        const realNodeModules = join(from, 'node_modules');
        if (existsSync(realNodeModules)) {
          symlinkSync(realNodeModules, join(to, 'node_modules'), 'dir');
        }
      } else {
        symlinkSync(from, to, 'dir');
      }
    }
  }

  // Tooling: copy dependency-rules (generator edits manifest + runs docs),
  // empty dir for storybook (component stories land here in the fixture),
  // symlink the rest
  const toolingSrc = join(frontendRoot, 'tooling');
  mkdirSync(join(tempDir, 'tooling'), { recursive: true });
  for (const entry of readdirSync(toolingSrc)) {
    const from = join(toolingSrc, entry);
    const to = join(tempDir, 'tooling', entry);
    if (entry === 'dependency-rules') {
      mkdirSync(to, { recursive: true });
      copyDirRecursive(from, to);
    } else if (entry === 'storybook') {
      mkdirSync(to, { recursive: true });
    } else {
      symlinkSync(from, to, 'dir');
    }
  }

  // Docs output location
  mkdirSync(join(tempDir, 'docs/client/architecture'), { recursive: true });
}

function writeVitestConfig(include: string[]): void {
  const patterns = include.map((p) => `'${p}'`).join(', ');
  const uiWebSrc = join(tempDir, 'packages/ui/web/src').replace(/\\/g, '/');
  writeConfig(
    'vitest.config.mjs',
    `import { defineConfig } from 'vitest/config';

export default defineConfig({
  resolve: {
    alias: {
      '~': '${uiWebSrc}',
    },
  },
  test: {
    environment: 'node',
    include: [${patterns}],
  },
});
`,
  );
}

function writeConfig(name: string, content: string): void {
  writeFileSync(join(tempDir, name), content, 'utf8');
}

function writeLintConfig(): void {
  // Relative import: @notrelix/eslint-config is only linked by pnpm after an
  // install, which the fixture never runs.
  writeConfig(
    'lint.config.mjs',
    `import { defineConfig } from "eslint/config";
import webConfig from "./tooling/eslint-config/web.js";

export default defineConfig([
  {
    ignores: ["**/node_modules/**", "**/dist/**", "**/.turbo/**"],
  },
  ...webConfig,
]);
`,
  );
}

function runTsc(project: string): void {
  execFileSync(bin('tsc'), ['--noEmit', '-p', project], { stdio: 'inherit' });
}

function runVitest(include: string[]): void {
  writeVitestConfig(include);
  execFileSync(bin('vitest'), ['run', '--config', join(tempDir, 'vitest.config.mjs')], {
    cwd: tempDir,
    stdio: 'inherit',
  });
}

function runEslint(relativeTarget: string): void {
  writeLintConfig();
  // ESLint 9.36+ ignores files outside the current working directory, so the
  // gate runs with cwd = fixture root and a workspace-relative target.
  execFileSync(
    bin('eslint'),
    ['--config', join(tempDir, 'lint.config.mjs'), relativeTarget],
    { cwd: tempDir, stdio: 'inherit' },
  );
}

function runArchitectureCheck(): void {
  const tsx = bin('tsx');
  execFileSync(
    tsx,
    [join(tempDir, 'tooling/dependency-rules/src/run-checks.ts'), '--root', tempDir],
    { stdio: 'inherit' },
  );
}

function gitPorcelain(): string {
  return execFileSync('git', ['status', '--porcelain'], { cwd: repoRoot, stdio: 'pipe' }).toString();
}

const realManifestPath = join(
  frontendRoot,
  'tooling/dependency-rules/src/architecture-manifest.ts',
);
const realDocsPath = join(
  frontendRoot,
  'docs/client/architecture/package-boundaries.generated.md',
);

// ── create-feature ────────────────────────────────────────────────────

describe('create-feature generator', () => {
  it('scaffolds a core feature with the canonical structure', () => {
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
      'src/testing',
    ]) {
      expect(existsSync(join(featureDir, dir)), dir).toBe(true);
    }

    // No UI dirs unless --ui is passed
    expect(existsSync(join(featureDir, 'src/web'))).toBe(false);
    expect(existsSync(join(featureDir, 'src/mobile'))).toBe(false);

    const pkg = JSON.parse(readFileSync(join(featureDir, 'package.json'), 'utf8'));
    expect(pkg.name).toBe('@notrelix/features-billing');
    expect(pkg.exports['./core/query/keys']).toBe('./src/core/query/keys.ts');
    expect(pkg.exports['./web']).toBeUndefined();
    expect(pkg.scripts.typecheck).toBe('tsc --noEmit');
    expect(pkg.scripts.test).toBe('vitest run');
    expect(existsSync(join(featureDir, 'eslint.config.js'))).toBe(true);

    const keys = readFileSync(join(featureDir, 'src/core/query/keys.ts'), 'utf8');
    expect(keys).toContain('billingQueryKeys');
    expect(keys).toContain("all: ['billing']");
    expect(existsSync(join(featureDir, 'src/core/query/keys.test.ts'))).toBe(true);
  });

  it('scaffolds UI subfolders per --ui flag', () => {
    runGenerator('create-feature', 'billing', '--ui', 'web');

    const featureDir = join(tempDir, 'packages/features/billing');
    expect(existsSync(join(featureDir, 'src/web/screens'))).toBe(true);
    expect(existsSync(join(featureDir, 'src/mobile'))).toBe(false);

    const pkg = JSON.parse(readFileSync(join(featureDir, 'package.json'), 'utf8'));
    expect(pkg.exports['./web']).toBe('./src/web/index.ts');
    expect(pkg.exports['./mobile']).toBeUndefined();
  });

  it('scaffolds both UI targets with --ui both', () => {
    runGenerator('create-feature', 'billing', '--ui', 'both');

    const featureDir = join(tempDir, 'packages/features/billing');
    expect(existsSync(join(featureDir, 'src/web'))).toBe(true);
    expect(existsSync(join(featureDir, 'src/mobile'))).toBe(true);
  });

  it('rejects an invalid --ui target', () => {
    expect(() => runGenerator('create-feature', 'billing', '--ui', 'desktop')).toThrow(
      /Invalid --ui target/,
    );
  });

  it('refuses to overwrite an existing feature', () => {
    runGenerator('create-feature', 'billing');

    expect(() => runGenerator('create-feature', 'billing')).toThrow(
      /already exists/,
    );
  });
});

// ── create-product-module ─────────────────────────────────────────────

describe('create-product-module generator', () => {
  it('scaffolds core and adapters per flags', () => {
    runGenerator('create-product-module', 'analytics', '--adapters', 'web');

    const moduleDir = join(tempDir, 'packages/product/analytics');
    expect(existsSync(join(moduleDir, 'core/src/index.ts'))).toBe(true);
    expect(existsSync(join(moduleDir, 'web/src/index.ts'))).toBe(true);
    expect(existsSync(join(moduleDir, 'mobile'))).toBe(false);
    expect(existsSync(join(moduleDir, 'state'))).toBe(false);

    const corePkg = JSON.parse(
      readFileSync(join(moduleDir, 'core/package.json'), 'utf8'),
    );
    expect(corePkg.name).toBe('@notrelix/analytics-core');
    expect(corePkg.scripts.test).toBe('vitest run');
    expect(existsSync(join(moduleDir, 'core/src/__tests__/smoke.test.ts'))).toBe(true);
    expect(existsSync(join(moduleDir, 'core/eslint.config.js'))).toBe(true);

    const webPkg = JSON.parse(
      readFileSync(join(moduleDir, 'web/package.json'), 'utf8'),
    );
    expect(webPkg.dependencies['@notrelix/analytics-core']).toBe('workspace:*');
  });

  it('scaffolds state, testing, and extension sub-packages per flags', () => {
    runGenerator(
      'create-product-module',
      'analytics',
      '--adapters',
      'both',
      '--state',
      '--testing',
      '--extension',
      'plugins',
    );

    const moduleDir = join(tempDir, 'packages/product/analytics');
    for (const sub of ['core', 'web', 'mobile', 'state', 'testing', 'plugins']) {
      expect(existsSync(join(moduleDir, sub, 'src/index.ts')), sub).toBe(true);
    }
    expect(existsSync(join(moduleDir, 'collaboration'))).toBe(false);

    const statePkg = JSON.parse(
      readFileSync(join(moduleDir, 'state/package.json'), 'utf8'),
    );
    expect(statePkg.name).toBe('@notrelix/analytics-state');
    expect(statePkg.dependencies['@notrelix/analytics-core']).toBe('workspace:*');
  });

  it('rejects an invalid --extension target', () => {
    expect(() =>
      runGenerator('create-product-module', 'analytics', '--extension', 'widgets'),
    ).toThrow(/Invalid --extension/);
  });

  it('refuses to overwrite an existing product module', () => {
    runGenerator('create-product-module', 'analytics');

    expect(() => runGenerator('create-product-module', 'analytics')).toThrow(
      /already exists/,
    );
  });
});

// ── create-ui-component ───────────────────────────────────────────────

describe('create-ui-component generator', () => {
  it('scaffolds a web component with export, test, and story', () => {
    runGenerator('create-ui-component', 'alert', '--target', 'web');

    const componentFile = join(tempDir, 'packages/ui/web/src/components/ui/alert.tsx');
    expect(existsSync(componentFile)).toBe(true);

    const source = readFileSync(componentFile, 'utf8');
    expect(source).toContain('React.forwardRef');
    expect(source).toContain('Alert');
    expect(source).toContain('displayName');
    expect(source).toContain('~/lib/cn');

    expect(existsSync(join(tempDir, 'packages/ui/web/src/components/ui/__tests__/alert.test.tsx'))).toBe(true);
    expect(existsSync(join(tempDir, 'tooling/storybook/web/stories/alert.stories.tsx'))).toBe(true);

    const index = readFileSync(join(tempDir, 'packages/ui/web/src/index.ts'), 'utf8');
    expect(index).toContain('export { Alert } from "./components/ui/alert"');
  });

  it('scaffolds a mobile contract with export and test', () => {
    runGenerator('create-ui-component', 'toggle', '--target', 'mobile');

    const contractFile = join(tempDir, 'packages/ui/mobile/src/components/toggle.ts');
    expect(existsSync(contractFile)).toBe(true);
    expect(existsSync(join(tempDir, 'packages/ui/mobile/src/__tests__/toggle.test.ts'))).toBe(true);

    const index = readFileSync(join(tempDir, 'packages/ui/mobile/src/index.ts'), 'utf8');
    expect(index).toContain('export type { ToggleProps } from "./components/toggle"');
  });

  it('rejects an invalid --target', () => {
    expect(() => runGenerator('create-ui-component', 'alert', '--target', 'server')).toThrow(
      /Invalid --target/,
    );
  });

  it('refuses to overwrite an existing component', () => {
    runGenerator('create-ui-component', 'alert');

    expect(() => runGenerator('create-ui-component', 'alert')).toThrow(
      /already exists/,
    );
  });
});

// ── Golden path: fixture workspace + real gates + GEN-030 ─────────────

describe('generator golden path (13-TEAM-FANOUT-GOLDEN-PATH-SPEC)', () => {
  it(
    'create-feature: registers in manifest, refreshes docs, passes all gates, leaves real worktree untouched',
    { timeout: 180_000 },
    () => {
      const manifestBefore = readFileSync(realManifestPath, 'utf8');
      const docsBefore = readFileSync(realDocsPath, 'utf8');
      const gitBefore = gitPorcelain();

      buildFixtureWorkspace();
      runGeneratorIn('create-feature', tempDir, 'freeze-smoke-feature', '--ui', 'both', '--realtime');

      const fixtureManifest = readFileSync(
        join(tempDir, 'tooling/dependency-rules/src/architecture-manifest.ts'),
        'utf8',
      );
      expect(fixtureManifest).toContain("packageName: '@notrelix/features-freeze-smoke-feature'");
      expect(fixtureManifest).toContain("relativePath: 'packages/features/freeze-smoke-feature'");
      expect(fixtureManifest).toContain("[...FEATURE_BASE_IMPORTS, '@notrelix/realtime']");

      const fixtureDocs = readFileSync(
        join(tempDir, 'docs/client/architecture/package-boundaries.generated.md'),
        'utf8',
      );
      expect(fixtureDocs).toContain('freeze-smoke-feature');

      const featureDir = join(tempDir, 'packages/features/freeze-smoke-feature');

      // Gate: typecheck
      runTsc(join(featureDir, 'tsconfig.json'));

      // Gate: test
      runVitest(['packages/features/freeze-smoke-feature/src/**/*.test.ts']);

      // Gate: lint
      runEslint('packages/features/freeze-smoke-feature/src');

      // Gate: architecture (manifest set equality, imports, boundaries, docs)
      runArchitectureCheck();

      // GEN-030: real worktree untouched
      expect(readFileSync(realManifestPath, 'utf8')).toBe(manifestBefore);
      expect(readFileSync(realDocsPath, 'utf8')).toBe(docsBefore);
      expect(gitPorcelain()).toBe(gitBefore);
    },
  );

  it(
    'create-product-module: registers sub-packages, refreshes docs, passes gates, leaves real worktree untouched',
    { timeout: 180_000 },
    () => {
      const manifestBefore = readFileSync(realManifestPath, 'utf8');
      const docsBefore = readFileSync(realDocsPath, 'utf8');
      const gitBefore = gitPorcelain();

      buildFixtureWorkspace();
      runGeneratorIn(
        'create-product-module',
        tempDir,
        'freeze-smoke-analytics',
        '--adapters',
        'both',
        '--state',
        '--testing',
        '--extension',
        'plugins',
      );

      const fixtureManifest = readFileSync(
        join(tempDir, 'tooling/dependency-rules/src/architecture-manifest.ts'),
        'utf8',
      );
      for (const pkg of [
        '@notrelix/freeze-smoke-analytics-core',
        '@notrelix/freeze-smoke-analytics-web',
        '@notrelix/freeze-smoke-analytics-mobile',
        '@notrelix/freeze-smoke-analytics-state',
        '@notrelix/freeze-smoke-analytics-testing',
        '@notrelix/freeze-smoke-analytics-plugins',
      ]) {
        expect(fixtureManifest).toContain(`packageName: '${pkg}'`);
      }
      expect(fixtureManifest).toContain("layer: 'product-state'");
      expect(fixtureManifest).toContain("layer: 'product-testing'");
      expect(fixtureManifest).toContain("layer: 'product-plugin'");

      const fixtureDocs = readFileSync(
        join(tempDir, 'docs/client/architecture/package-boundaries.generated.md'),
        'utf8',
      );
      expect(fixtureDocs).toContain('freeze-smoke-analytics');

      const coreDir = join(tempDir, 'packages/product/freeze-smoke-analytics/core');

      // Gate: typecheck
      runTsc(join(coreDir, 'tsconfig.json'));

      // Gate: test
      runVitest(['packages/product/freeze-smoke-analytics/core/src/**/*.test.ts']);

      // Gate: lint
      runEslint('packages/product/freeze-smoke-analytics/core/src');

      // GEN-030: real worktree untouched
      expect(readFileSync(realManifestPath, 'utf8')).toBe(manifestBefore);
      expect(readFileSync(realDocsPath, 'utf8')).toBe(docsBefore);
      expect(gitPorcelain()).toBe(gitBefore);
    },
  );

  it(
    'create-ui-component: updates public export, passes gates, leaves real worktree untouched',
    { timeout: 180_000 },
    () => {
      const gitBefore = gitPorcelain();
      const realIndexBefore = readFileSync(
        join(frontendRoot, 'packages/ui/web/src/index.ts'),
        'utf8',
      );

      buildFixtureWorkspace();
      runGeneratorIn('create-ui-component', tempDir, 'freeze-smoke-badge', '--target', 'web');

      const componentFile = join(
        tempDir,
        'packages/ui/web/src/components/ui/freeze-smoke-badge.tsx',
      );
      expect(existsSync(componentFile)).toBe(true);
      expect(
        readFileSync(join(tempDir, 'packages/ui/web/src/index.ts'), 'utf8'),
      ).toContain('export { FreezeSmokeBadge } from "./components/ui/freeze-smoke-badge"');

      // Gate: typecheck (whole ui-web package)
      runTsc(join(tempDir, 'packages/ui/web/tsconfig.json'));

      // Gate: test
      runVitest(['packages/ui/web/src/components/ui/__tests__/freeze-smoke-badge.test.tsx']);

      // Gate: lint
      runEslint('packages/ui/web/src/components/ui/freeze-smoke-badge.tsx');

      // GEN-030: real worktree untouched
      expect(readFileSync(join(frontendRoot, 'packages/ui/web/src/index.ts'), 'utf8')).toBe(
        realIndexBefore,
      );
      expect(gitPorcelain()).toBe(gitBefore);
    },
  );
});
