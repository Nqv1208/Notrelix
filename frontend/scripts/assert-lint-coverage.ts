import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join, resolve } from "node:path";
import { ARCHITECTURE_MANIFEST } from "../tooling/dependency-rules/src/architecture-manifest";

const EXECUTABLE_SOURCE_EXTENSIONS = [".ts", ".tsx", ".js", ".mjs"];
const IGNORED_DIRECTORIES = new Set([
  "node_modules",
  "dist",
  ".next",
  ".turbo",
  "coverage",
]);

function hasExecutableSourceFile(directory: string): boolean {
  let entries: string[] = [];
  try {
    entries = readdirSync(directory);
  } catch {
    return false;
  }

  for (const entry of entries) {
    if (IGNORED_DIRECTORIES.has(entry) || entry.startsWith(".")) continue;

    const fullPath = join(directory, entry);
    let stat;
    try {
      stat = statSync(fullPath);
    } catch {
      continue;
    }

    if (stat.isDirectory()) {
      if (hasExecutableSourceFile(fullPath)) return true;
      continue;
    }

    if (
      EXECUTABLE_SOURCE_EXTENSIONS.some((extension) =>
        entry.endsWith(extension),
      )
    ) {
      return true;
    }
  }

  return false;
}

function hasExecutableSource(packageDir: string): boolean {
  return ["src", "app"].some((sourceDir) =>
    hasExecutableSourceFile(join(packageDir, sourceDir)),
  );
}

export interface LintCoverageEntry {
  readonly packageName: string;
  readonly relativePath: string;
  readonly freezeScope?: string;
}

export function checkLintCoverage(
  rootDir = process.cwd(),
  manifest: readonly LintCoverageEntry[] = ARCHITECTURE_MANIFEST,
) {
  const root = resolve(rootDir);
  const missing: Array<{ name: string; path: string; reason: string }> = [];
  let checked = 0;

  if (manifest.length === 0) {
    missing.push({
      name: "architecture-manifest",
      path: "tooling/dependency-rules/src/architecture-manifest.ts",
      reason: "manifest resolved with zero packages",
    });
    return { ok: false, checked, missing };
  }

  for (const entry of manifest) {
    const packageDir = join(root, entry.relativePath);
    const packageJsonPath = join(packageDir, "package.json");
    if (!existsSync(packageJsonPath) || !hasExecutableSource(packageDir)) {
      continue;
    }

    checked += 1;

    let packageJson;
    try {
      packageJson = JSON.parse(readFileSync(packageJsonPath, "utf8"));
    } catch {
      missing.push({
        name: entry.packageName,
        path: entry.relativePath,
        reason: "package.json is unreadable",
      });
      continue;
    }

    const lintScript = packageJson.scripts?.lint;
    if (
      !lintScript ||
      typeof lintScript !== "string" ||
      lintScript.trim().length === 0
    ) {
      missing.push({
        name: entry.packageName,
        path: entry.relativePath,
        reason: "scripts.lint is missing or empty",
      });
    }
  }

  return {
    ok: missing.length === 0,
    checked,
    missing,
  };
}

const invokedPath = process.argv[1] ? resolve(process.argv[1]) : "";
const currentModulePath = resolve(new URL(import.meta.url).pathname);

if (invokedPath === currentModulePath) {
  const result = checkLintCoverage(process.cwd());

  if (!result.ok) {
    console.error(
      "Lint coverage violation. Every source-bearing manifest package must declare a non-empty `scripts.lint`:",
    );
    for (const item of result.missing) {
      console.error(`  - ${item.name} (${item.path}): ${item.reason}`);
    }
    process.exit(1);
  }

  console.log(`Lint coverage OK: ${result.checked} manifest packages checked.`);
}
