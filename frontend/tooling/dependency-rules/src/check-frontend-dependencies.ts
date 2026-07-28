import ts from 'typescript';
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { ALLOWED_IMPORTS } from './allowed-imports';
import { FORBIDDEN_IMPORTS } from './forbidden-imports';
import {
  isForbiddenClientCall,
  isForbiddenWebSocketInstantiation,
  isForbiddenQueryClientInstantiation,
  isDeepSrcImport,
} from './forbidden-source-patterns';

const DEFAULT_ROOT = resolve(__dirname, '../../..');

export function checkArchitecture(rootDir = DEFAULT_ROOT): { ok: boolean; violations: string[] } {
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

  function walkDir(dir: string, exts = ['.ts', '.tsx']): string[] {
    const results: string[] = [];
    try {
      for (const entry of readdirSync(dir)) {
        const full = join(dir, entry);
        const stat = statSync(full);
        if (stat.isDirectory() && !entry.startsWith('.') && entry !== 'node_modules' && entry !== 'dist') {
          results.push(...walkDir(full, exts));
        } else if (stat.isFile() && exts.some((e) => full.endsWith(e))) {
          results.push(full);
        }
      }
    } catch {}
    return results;
  }

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

    const files = walkDir(pkgDir);

    for (const file of files) {
      const relPath = file.replace(rootDir, '');
      const content = readFileSync(file, 'utf8');

      // Create AST SourceFile
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

          const internalPkg = imported.match(/^(@notrelix\/[^/]+)/)?.[1] ?? imported;
          if (forbidden.includes(imported) || forbidden.includes(internalPkg)) {
            violations.push(`[FORBIDDEN_IMPORT] ${pkgName} → "${imported}" in ${relPath}`);
          }

          if (allowed !== null && internalPkg.startsWith('@notrelix/')) {
            if (!allowed.some((a) => internalPkg === a || internalPkg.startsWith(a + '/'))) {
              violations.push(`[NOT_ALLOWED_IMPORT] ${pkgName} → "${internalPkg}" in ${relPath}`);
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

        // Check direct PropertyAccessExpression (import.meta.env or process.env outside approved adapters)
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
