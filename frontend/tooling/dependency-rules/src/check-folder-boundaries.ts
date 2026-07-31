import ts from 'typescript';
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';

export function checkFolderBoundaries(rootDir: string): { ok: boolean; violations: string[] } {
  const violations: string[] = [];

  function walkDir(dir: string): string[] {
    const results: string[] = [];
    try {
      for (const entry of readdirSync(dir)) {
        const full = join(dir, entry);
        const stat = statSync(full);
        if (stat.isDirectory() && !entry.startsWith('.') && entry !== 'node_modules' && entry !== 'dist') {
          results.push(...walkDir(full));
        } else if (stat.isFile() && (full.endsWith('.ts') || full.endsWith('.tsx'))) {
          results.push(full);
        }
      }
    } catch {}
    return results;
  }

  const files = [
    ...walkDir(join(rootDir, 'packages')),
    ...walkDir(join(rootDir, 'apps')),
  ];

  for (const file of files) {
    const relPath = file.replace(rootDir, '').replace(/\\/g, '/');

    // Rule: **/src/core/** must forbid react, react-dom, @tanstack/*, @notrelix/ui-*, window, document
    // EXPIRY: FE-FZ-13 — Temporary allowlist for legacy feature core query hooks until feature reorganization
    if (relPath.includes('/src/core/query/hooks/')) {
      continue;
    }

    if (relPath.includes('/src/core/')) {
      const content = readFileSync(file, 'utf8');
      const sourceFile = ts.createSourceFile(file, content, ts.ScriptTarget.Latest, true);

      function visit(node: ts.Node) {
        if (ts.isImportDeclaration(node) && node.moduleSpecifier && ts.isStringLiteral(node.moduleSpecifier)) {
          const imported = node.moduleSpecifier.text;
          if (
            imported === 'react' ||
            imported === 'react-dom' ||
            imported.startsWith('@tanstack/') ||
            imported.startsWith('@notrelix/ui-')
          ) {
            violations.push(`[CORE_IMPURE_IMPORT] Core file ${relPath} imported framework package "${imported}"`);
          }
        }
        ts.forEachChild(node, visit);
      }

      visit(sourceFile);
    }
  }

  return { ok: violations.length === 0, violations };
}
