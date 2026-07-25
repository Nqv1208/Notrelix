#!/usr/bin/env node
/**
 * @notrelix/dependency-rules — Boundary Check Script
 * Validates that each package only imports from allowed dependencies.
 * Usage: node tooling/dependency-rules/src/check.mjs
 */
import { readFileSync, readdirSync, statSync } from "node:fs"
import { join, resolve } from "node:path"
import { ALLOWED_IMPORTS } from "./allowed-imports.ts"
import { FORBIDDEN_IMPORTS } from "./forbidden-imports.ts"

const DEFAULT_ROOT = resolve(import.meta.dirname, "../../..")

function getRoot() {
  const rootIndex = process.argv.indexOf("--root")
  if (rootIndex >= 0) {
    const value = process.argv[rootIndex + 1]
    if (!value) {
      console.error("Missing value for --root")
      process.exit(2)
    }
    return resolve(value)
  }
  return DEFAULT_ROOT
}

function walkDir(dir, exts = [".ts", ".tsx"]) {
  const results = []
  try {
    for (const entry of readdirSync(dir)) {
      const full = join(dir, entry)
      const stat = statSync(full)
      if (stat.isDirectory() && !entry.startsWith(".") && entry !== "node_modules" && entry !== "dist") {
        results.push(...walkDir(full, exts))
      } else if (stat.isFile() && exts.some((e) => full.endsWith(e))) {
        results.push(full)
      }
    }
  } catch {}
  return results
}

function stripComments(code) {
  return code
    .replace(/\/\*[\s\S]*?\*\//g, "")
    .replace(/\/\/.*/g, "")
}


function findPackageDirs(base, depth = 0) {
  const results = []
  try {
    for (const entry of readdirSync(base)) {
      const full = join(base, entry)
      if (!statSync(full).isDirectory()) continue
      if (entry.startsWith(".") || entry === "node_modules") continue
      try {
        statSync(join(full, "package.json"))
        results.push(full)
      } catch {
        if (depth < 4) results.push(...findPackageDirs(full, depth + 1))
      }
    }
  } catch {}
  return results
}

function findWorkspacePackages(root) {
  return [
    ...findPackageDirs(join(root, "packages")),
    ...findPackageDirs(join(root, "apps")),
  ]
}

function findImports(content) {
  const imports = []
  const importMatches = content.matchAll(/(?:import|from)\s+['"]([^'"]+)['"]/g)
  for (const match of importMatches) imports.push(match[1])

  const dynamicMatches = content.matchAll(/import\(\s*['"]([^'"]+)['"]\s*\)/g)
  for (const match of dynamicMatches) imports.push(match[1])

  return imports
}

function isPackageSourceFile(file, root) {
  return file.replace(root, "").startsWith("/packages/")
}

function findDirectEnvReads(content) {
  const reads = []
  const matches = content.matchAll(/\b(?:process\.env|import\.meta\.env)(?:\.([A-Z0-9_]+))?/g)
  for (const match of matches) {
    reads.push(match[1] ?? match[0])
  }
  return reads
}

function isForbiddenImport(imported, forbidden) {
  return forbidden.some((f) => imported === f || imported.startsWith(f + "/"))
}

function main() {
  const root = getRoot()
  console.log("🔍 Notrelix Dependency Boundary Check\n")
  const violations = []
  const packageDirs = findWorkspacePackages(root)

  for (const pkgDir of packageDirs) {
    let pkgJson;
    const pkgJsonPath = join(pkgDir, "package.json");
    try {
      pkgJson = JSON.parse(readFileSync(pkgJsonPath, "utf8"));
    } catch {
      continue;
    }
    const pkgName = pkgJson.name;

    const allowed = ALLOWED_IMPORTS[pkgName] ?? null;
    const forbidden = [
      ...(FORBIDDEN_IMPORTS[pkgName] ?? []),
      ...(pkgDir.replace(root, "").startsWith("/packages/") ? ["next"] : []),
    ];

    // Check declared dependencies in package.json
    const declaredDeps = new Set([
      ...Object.keys(pkgJson.dependencies || {}),
      ...Object.keys(pkgJson.devDependencies || {}),
      ...Object.keys(pkgJson.peerDependencies || {}),
      ...Object.keys(pkgJson.optionalDependencies || {}),
    ]);

    for (const dep of declaredDeps) {
      if (isForbiddenImport(dep, forbidden)) {
        violations.push({
          pkg: pkgName,
          file: pkgJsonPath.replace(root, ""),
          imported: dep,
          rule: "DECLARED_FORBIDDEN_DEPENDENCY",
        });
      }

      const internalPackage = dep.match(/^(@notrelix\/[^/]+)/)?.[1] ?? dep;
      const isTooling = [
        "@notrelix/tsconfig",
        "@notrelix/eslint-config",
        "@notrelix/testing",
        "@notrelix/dependency-rules"
      ].includes(internalPackage);

      if (allowed !== null && internalPackage.startsWith("@notrelix/") && !isTooling) {
        if (!allowed.some((a) => internalPackage === a || internalPackage.startsWith(a + "/"))) {
          violations.push({
            pkg: pkgName,
            file: pkgJsonPath.replace(root, ""),
            imported: internalPackage,
            rule: "DECLARED_NOT_ALLOWED_DEPENDENCY",
          });
        }
      }
    }

    const files = walkDir(pkgDir);

    for (const file of files) {
      const rawContent = readFileSync(file, "utf8")
      const content = stripComments(rawContent)
      const relPath = file.replace(root, "")

      if (isPackageSourceFile(file, root)) {
        for (const envName of findDirectEnvReads(content)) {
          violations.push({ pkg: pkgName, file: relPath, imported: envName, rule: "DIRECT_ENV_READ" })
        }
      }

      for (const imported of findImports(content)) {
        const internalPackage = imported.match(/^(@notrelix\/[^/]+)/)?.[1] ?? imported

        if (isForbiddenImport(imported, forbidden) || isForbiddenImport(internalPackage, forbidden)) {
          const rule = imported.startsWith("@notrelix/") ? "FORBIDDEN" : "EXTERNAL_FORBIDDEN"
          violations.push({ pkg: pkgName, file: relPath, imported, rule })
        }

        if (allowed !== null && internalPackage.startsWith("@notrelix/")) {
          if (!allowed.some((a) => internalPackage === a || internalPackage.startsWith(a + "/"))) {
            violations.push({ pkg: pkgName, file: relPath, imported: internalPackage, rule: "NOT_ALLOWED" })
          }
        }
      }
    }
  }

  if (violations.length === 0) {
    console.log("✅ No boundary violations found.")
    console.log(`   Scanned ${packageDirs.length} packages.`)
    process.exit(0)
  } else {
    console.error(`❌ Found ${violations.length} boundary violation(s):\n`)
    for (const v of violations) {
      console.error(`   [${v.rule}] ${v.pkg} → "${v.imported}" in ${v.file}`)
    }
    process.exit(1)
  }
}

main()
