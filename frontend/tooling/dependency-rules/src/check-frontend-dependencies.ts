import ts from 'typescript';
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  ARCHITECTURE_MANIFEST,
  ARCHITECTURE_POLICY_BY_PACKAGE,
  validateArchitectureManifest,
} from './architecture-manifest';
import { FORBIDDEN_IMPORTS } from './forbidden-imports';
import {
  isForbiddenClientCall,
  isForbiddenWebSocketInstantiation,
  isForbiddenQueryClientInstantiation,
  isDeepSrcImport,
} from './forbidden-source-patterns';
import { classifyLayer } from './layer-classifier';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const DEFAULT_ROOT = resolve(__dirname, '../../..');

export interface DiscoveredPackage {
  readonly name: string;
  readonly dir: string;
  readonly relativePath: string;
}

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

export function discoverWorkspacePackages(rootDir: string): DiscoveredPackage[] {
  const dirs = [
    ...findPackageDirs(join(rootDir, 'packages')),
    ...findPackageDirs(join(rootDir, 'apps')),
  ];

  const discovered: DiscoveredPackage[] = [];
  for (const dir of dirs) {
    let pkgJson: { name?: string };
    try {
      pkgJson = JSON.parse(readFileSync(join(dir, 'package.json'), 'utf8'));
    } catch {
      continue;
    }
    if (!pkgJson.name) continue;
    discovered.push({
      name: pkgJson.name,
      dir,
      relativePath: dir.replace(rootDir, '').replace(/^[\\/]+/, '').replace(/\\/g, '/'),
    });
  }
  return discovered;
}

/**
 * Closed-world preflight: the discovered workspace package set must equal the
 * manifest set exactly. Violations carry stable codes so tests and CI can
 * assert them.
 */
export function preflightArchitectureManifest(
  rootDir: string,
): { violations: string[]; registered: Map<string, DiscoveredPackage> } {
  const violations: string[] = [];

  for (const violation of validateArchitectureManifest(ARCHITECTURE_MANIFEST)) {
    violations.push(
      `[${violation.code}] manifest defect for "${violation.packageName}": ${violation.message}`,
    );
  }

  const discovered = discoverWorkspacePackages(rootDir);
  const discoveredByPath = new Map(discovered.map((pkg) => [pkg.relativePath, pkg]));
  const discoveredByName = new Map(discovered.map((pkg) => [pkg.name, pkg]));
  const registered = new Map<string, DiscoveredPackage>();

  const seenDiscoveredNames = new Map<string, number>();
  for (const pkg of discovered) {
    seenDiscoveredNames.set(pkg.name, (seenDiscoveredNames.get(pkg.name) ?? 0) + 1);
  }
  for (const [name, count] of seenDiscoveredNames) {
    if (count > 1) {
      violations.push(`[DUPLICATE_PACKAGE_NAME] discovered workspace contains ${count} packages named "${name}"`);
    }
  }

  for (const pkg of discovered) {
    const policy = ARCHITECTURE_POLICY_BY_PACKAGE.get(pkg.name);
    if (!policy) {
      violations.push(
        `[UNREGISTERED_PACKAGE] workspace package "${pkg.name}" at ${pkg.relativePath} has no architecture-manifest entry`,
      );
      continue;
    }
    if (policy.relativePath !== pkg.relativePath) {
      violations.push(
        `[PACKAGE_NAME_MISMATCH] package "${pkg.name}" lives at ${pkg.relativePath} but the manifest declares ${policy.relativePath}`,
      );
      continue;
    }
    registered.set(pkg.name, pkg);
  }

  for (const entry of ARCHITECTURE_MANIFEST) {
    if (discoveredByPath.has(entry.relativePath) || discoveredByName.has(entry.packageName)) {
      continue;
    }
    violations.push(
      `[STALE_PACKAGE_POLICY] manifest entry "${entry.packageName}" (${entry.relativePath}) matches no workspace package`,
    );
  }

  for (const entry of ARCHITECTURE_MANIFEST) {
    const atPath = discoveredByPath.get(entry.relativePath);
    if (atPath && atPath.name !== entry.packageName) {
      violations.push(
        `[PACKAGE_NAME_MISMATCH] manifest expects "${entry.packageName}" at ${entry.relativePath} but found "${atPath.name}"`,
      );
    } else if (!atPath && discoveredByName.has(entry.packageName)) {
      violations.push(
        `[MISSING_PACKAGE_PATH] manifest path ${entry.relativePath} for "${entry.packageName}" does not exist`,
      );
    }
  }

  return { violations, registered };
}

function walkDir(dir: string, exts = ['.ts', '.tsx']): string[] {
  const results: string[] = [];
  try {
    for (const entry of readdirSync(dir)) {
      const full = join(dir, entry);
      const stat = statSync(full);
      if (stat.isDirectory() && !entry.startsWith('.') && entry !== 'node_modules' && entry !== 'dist') {
        results.push(...walkDir(full));
      } else if (stat.isFile() && exts.some((e) => full.endsWith(e))) {
        results.push(full);
      }
    }
  } catch {}
  return results;
}

export function checkArchitecture(rootDir = DEFAULT_ROOT): { ok: boolean; violations: string[] } {
  const { violations, registered } = preflightArchitectureManifest(rootDir);

  // Unregistered packages already failed preflight; skip their detailed import
  // scan to avoid cascading noise, but keep scanning every registered package.
  for (const [pkgName, pkg] of registered) {
    const policy = ARCHITECTURE_POLICY_BY_PACKAGE.get(pkgName)!;
    const allowed = policy.allowedInternalImports;
    const forbidden = FORBIDDEN_IMPORTS[pkgName] ?? [];

    const files = walkDir(pkg.dir);

    for (const file of files) {
      const relPath = file.replace(rootDir, '');
      const content = readFileSync(file, 'utf8');
      const layer = classifyLayer(relPath, pkgName);

      const sourceFile = ts.createSourceFile(
        file,
        content,
        ts.ScriptTarget.Latest,
        /*setParentNodes */ true
      );

      function visitNode(node: ts.Node) {
        // Check ImportDeclarations
        if (ts.isImportDeclaration(node) && node.moduleSpecifier && ts.isStringLiteral(node.moduleSpecifier)) {
          const imported = node.moduleSpecifier.text;

          if (isDeepSrcImport(imported)) {
            violations.push(`[DEEP_IMPORT] ${pkgName} → "${imported}" in ${relPath}`);
          }

          const basePkg = imported.startsWith('next/')
            ? 'next'
            : (imported.match(/^(@notrelix\/[^/]+)/)?.[1] ?? imported);

          if (forbidden.includes(imported) || forbidden.includes(basePkg)) {
            const tag = imported.startsWith('@notrelix/') ? '[FORBIDDEN]' : '[EXTERNAL_FORBIDDEN]';
            violations.push(`${tag} ${pkgName} → "${imported}" in ${relPath}`);
          }

          if (basePkg.startsWith('@notrelix/')) {
            const allowedMatch = allowed.some((a) => basePkg === a || basePkg.startsWith(a + '/'));
            if (!allowedMatch) {
              violations.push(`[NOT_ALLOWED_IMPORT] ${pkgName} → "${basePkg}" in ${relPath}`);
            }
          }

          if (layer === 'data' && (imported === 'sonner' || imported.startsWith('@notrelix/ui-'))) {
            violations.push(`[DATA_UI_SIDE_EFFECT] ${pkgName} data layer imported UI side-effect package "${imported}" in ${relPath}`);
          }
        }

        if (ts.isVariableStatement(node) && node.modifiers?.some((m) => m.kind === ts.SyntaxKind.ExportKeyword)) {
          for (const declaration of node.declarationList.declarations) {
            if (!ts.isIdentifier(declaration.name) || !declaration.initializer) continue;
            if (!/(Api|Repository)$/.test(declaration.name.text)) continue;

            let initializer = declaration.initializer;
            if (ts.isAsExpression(initializer)) {
              initializer = initializer.expression;
            }
            if (!ts.isCallExpression(initializer)) continue;

            const expressionText = initializer.expression.getText(sourceFile);
            if (/^create[A-Z].*(Api|Repository)$/.test(expressionText)) {
              violations.push(`[EXPORTED_API_INSTANCE] ${pkgName} exported production API/repository instance "${declaration.name.text}" in ${relPath}`);
            }
          }
        }

        // Check CallExpressions (factory calls, env reads, dynamic imports)
        if (ts.isCallExpression(node)) {
          const expressionText = node.expression.getText(sourceFile);
          if (expressionText === 'createNotrelixClient' && isForbiddenClientCall(relPath)) {
            violations.push(`[FORBIDDEN_CLIENT_CREATION] ${pkgName} called createNotrelixClient in ${relPath}`);
          }
        }

        // Check NewExpressions (new WebSocket, new QueryClient)
        if (ts.isNewExpression(node)) {
          const expressionText = node.expression.getText(sourceFile);
          if (expressionText === 'WebSocket' && isForbiddenWebSocketInstantiation(relPath)) {
            violations.push(`[FORBIDDEN_WEBSOCKET_INSTANTIATION] ${pkgName} instantiated WebSocket in ${relPath}`);
          }
          if (expressionText === 'QueryClient' && isForbiddenQueryClientInstantiation(relPath)) {
            violations.push(`[FORBIDDEN_QUERYCLIENT_INSTANTIATION] ${pkgName} instantiated QueryClient in ${relPath}`);
          }
        }

        // Check direct PropertyAccessExpression (process.env / import.meta.env inside packages outside app config adapters)
        if (ts.isPropertyAccessExpression(node)) {
          const propText = node.getText(sourceFile);
          if (
            (propText.startsWith('process.env.') || propText.startsWith('import.meta.env.')) &&
            relPath.startsWith('/packages/')
          ) {
            violations.push(`[DIRECT_ENV_READ] ${pkgName} accessed ${propText} in ${relPath}`);
          }
        }

        ts.forEachChild(node, visitNode);
      }

      visitNode(sourceFile);
    }
  }

  return { ok: violations.length === 0, violations };
}
