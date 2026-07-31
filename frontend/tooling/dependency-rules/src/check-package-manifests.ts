import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { ALLOWED_IMPORTS } from './allowed-imports';
import { FORBIDDEN_IMPORTS } from './forbidden-imports';

export function checkPackageManifests(rootDir: string): { ok: boolean; violations: string[] } {
  const violations: string[] = [];

  function findPackageDirs(base: string, depth = 0): string[] {
    const results: string[] = [];
    try {
      for (const entry of readdirSync(base)) {
        const full = join(base, entry);
        if (!statSync(full).isDirectory()) continue;
        if (entry.startsWith('.') || entry === 'node_modules' || entry === 'dist') continue;
        try {
          statSync(join(full, 'package.json'));
          results.push(full);
        } catch {
          if (depth < 4) results.push(...findPackageDirs(full, depth + 1));
        }
      }
    } catch {}
    return results;
  }

  const packageDirs = [
    ...findPackageDirs(join(rootDir, 'packages')),
    ...findPackageDirs(join(rootDir, 'apps')),
  ];

  for (const pkgDir of packageDirs) {
    let pkgJson: any;
    try {
      pkgJson = JSON.parse(readFileSync(join(pkgDir, 'package.json'), 'utf8'));
    } catch {
      continue;
    }
    const pkgName = pkgJson.name;
    const allowed = ALLOWED_IMPORTS[pkgName] ?? null;
    const forbidden = FORBIDDEN_IMPORTS[pkgName] ?? [];

    const declaredDeps = new Set([
      ...Object.keys(pkgJson.dependencies || {}),
      ...Object.keys(pkgJson.devDependencies || {}),
      ...Object.keys(pkgJson.peerDependencies || {}),
    ]);

    for (const dep of declaredDeps) {
      if (forbidden.includes(dep)) {
        violations.push(`[DECLARED_FORBIDDEN_DEPENDENCY] ${pkgName} declared forbidden dependency "${dep}"`);
      }

      const internalPkg = dep.match(/^(@notrelix\/[^/]+)/)?.[1] ?? dep;
      const isTooling = [
        '@notrelix/tsconfig',
        '@notrelix/eslint-config',
        '@notrelix/testing',
        '@notrelix/dependency-rules',
      ].includes(internalPkg);

      if (allowed !== null && internalPkg.startsWith('@notrelix/') && !isTooling) {
        if (!allowed.some((a) => internalPkg === a || internalPkg.startsWith(a + '/'))) {
          violations.push(`[DECLARED_DISALLOWED_DEPENDENCY] ${pkgName} declared disallowed dependency "${internalPkg}"`);
        }
      }
    }
  }

  return { ok: violations.length === 0, violations };
}
