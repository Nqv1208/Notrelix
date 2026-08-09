import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ARCHITECTURE_POLICY_BY_PACKAGE } from './architecture-manifest';
import { FORBIDDEN_IMPORTS } from './forbidden-imports';
import { discoverWorkspacePackages } from './check-frontend-dependencies';

export function checkPackageManifests(rootDir: string): { ok: boolean; violations: string[] } {
  const violations: string[] = [];

  const discovered = discoverWorkspacePackages(rootDir);

  for (const pkg of discovered) {
    let pkgJson: {
      dependencies?: Record<string, string>;
      devDependencies?: Record<string, string>;
      peerDependencies?: Record<string, string>;
    };
    try {
      pkgJson = JSON.parse(readFileSync(join(pkg.dir, 'package.json'), 'utf8'));
    } catch {
      continue;
    }

    const policy = ARCHITECTURE_POLICY_BY_PACKAGE.get(pkg.name);
    const forbidden = FORBIDDEN_IMPORTS[pkg.name] ?? [];

    const declaredDeps = new Set([
      ...Object.keys(pkgJson.dependencies || {}),
      ...Object.keys(pkgJson.devDependencies || {}),
      ...Object.keys(pkgJson.peerDependencies || {}),
    ]);

    for (const dep of declaredDeps) {
      if (forbidden.includes(dep)) {
        violations.push(`[DECLARED_FORBIDDEN_DEPENDENCY] ${pkg.name} declared forbidden dependency "${dep}"`);
      }

      const internalPkg = dep.match(/^(@notrelix\/[^/]+)/)?.[1] ?? dep;
      const isTooling = [
        '@notrelix/tsconfig',
        '@notrelix/eslint-config',
        '@notrelix/testing',
        '@notrelix/dependency-rules',
      ].includes(internalPkg);

      // Closed world: unregistered packages are already reported by the
      // architecture preflight; do not cascade into declared-dependency noise.
      if (!policy) continue;

      if (internalPkg.startsWith('@notrelix/') && !isTooling) {
        const allowed = policy.allowedInternalImports;
        if (!allowed.some((a) => internalPkg === a || internalPkg.startsWith(a + '/'))) {
          violations.push(`[DECLARED_INTERNAL_DEP_NOT_ALLOWED] ${pkg.name} declared disallowed dependency "${internalPkg}"`);
        }
      }
    }
  }

  return { ok: violations.length === 0, violations };
}
