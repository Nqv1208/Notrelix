import ts from "typescript";
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join } from "node:path";
import { classifyLayer } from "./layer-classifier";

const CORE_FORBIDDEN_IMPORTS = [
  "react",
  "react-dom",
  "react-native",
  "@notrelix/query",
  "@notrelix/runtime-",
  "@notrelix/ui-",
  "sonner",
  "next",
];

const CORE_FORBIDDEN_GLOBALS = new Set([
  "window",
  "document",
  "localStorage",
  "sessionStorage",
  "navigator",
  "WebSocket",
]);

export function checkFolderBoundaries(rootDir: string): {
  ok: boolean;
  violations: string[];
} {
  const violations: string[] = [];

  function walkDir(dir: string): string[] {
    const results: string[] = [];
    try {
      for (const entry of readdirSync(dir)) {
        const full = join(dir, entry);
        const stat = statSync(full);
        if (
          stat.isDirectory() &&
          !entry.startsWith(".") &&
          entry !== "node_modules" &&
          entry !== "dist"
        ) {
          results.push(...walkDir(full));
        } else if (
          stat.isFile() &&
          (full.endsWith(".ts") || full.endsWith(".tsx"))
        ) {
          results.push(full);
        }
      }
    } catch {}
    return results;
  }

  function findPackageDirs(base: string, depth = 0): string[] {
    const results: string[] = [];
    try {
      for (const entry of readdirSync(base)) {
        const full = join(base, entry);
        if (!statSync(full).isDirectory()) continue;
        if (
          entry.startsWith(".") ||
          entry === "node_modules" ||
          entry === "dist"
        )
          continue;
        try {
          statSync(join(full, "package.json"));
          results.push(full);
        } catch {
          if (depth < 4) results.push(...findPackageDirs(full, depth + 1));
        }
      }
    } catch {}
    return results;
  }

  const packageDirs = [
    ...findPackageDirs(join(rootDir, "packages")),
    ...findPackageDirs(join(rootDir, "apps")),
  ];

  for (const pkgDir of packageDirs) {
    let pkgName = "";
    try {
      pkgName = JSON.parse(
        readFileSync(join(pkgDir, "package.json"), "utf8"),
      ).name;
    } catch {
      continue;
    }

    for (const file of walkDir(pkgDir)) {
      const relPath = file.replace(rootDir, "").replace(/\\/g, "/");
      const layer = classifyLayer(relPath, pkgName);
      if (layer !== "core") continue;

      const content = readFileSync(file, "utf8");
      const sourceFile = ts.createSourceFile(
        file,
        content,
        ts.ScriptTarget.Latest,
        true,
      );

      function visit(node: ts.Node) {
        if (
          ts.isImportDeclaration(node) &&
          node.moduleSpecifier &&
          ts.isStringLiteral(node.moduleSpecifier)
        ) {
          const imported = node.moduleSpecifier.text;
          if (
            CORE_FORBIDDEN_IMPORTS.some(
              (forbidden) =>
                imported === forbidden || imported.startsWith(forbidden),
            ) ||
            imported.startsWith("@tanstack/") ||
            imported.startsWith("next/")
          ) {
            violations.push(
              `[CORE_IMPURE_IMPORT] Core file ${relPath} imported framework package "${imported}"`,
            );
          }
        }
        if (ts.isIdentifier(node) && CORE_FORBIDDEN_GLOBALS.has(node.text)) {
          violations.push(
            `[CORE_BROWSER_GLOBAL] Core file ${relPath} referenced browser global "${node.text}"`,
          );
        }
        ts.forEachChild(node, visit);
      }

      visit(sourceFile);
    }
  }

  return { ok: violations.length === 0, violations };
}
