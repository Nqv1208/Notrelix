import { existsSync, readdirSync, realpathSync } from "node:fs";
import { join, relative, sep } from "node:path";

export interface GovernedSourceResult {
  sources: string[];
  diagnostics: string[];
}

const STRUCTURAL_DIRECTORY_EXCLUSIONS = new Set([
  "__tests__",
  "verification",
  "node_modules",
  "dist",
  "storybook-static",
  ".turbo",
]);

const STRUCTURAL_FILE_EXCLUSION = /(\.test\.tsx|\.spec\.tsx|\.stories\.tsx)$/;

function isStructurallyExcludedDirectory(name: string): boolean {
  return name.startsWith(".") || STRUCTURAL_DIRECTORY_EXCLUSIONS.has(name);
}

function isStructurallyExcludedFile(name: string): boolean {
  return name.startsWith(".") || STRUCTURAL_FILE_EXCLUSION.test(name);
}

function walk(
  directory: string,
  ownerRoot: string,
  realSrcRoot: string,
  sources: string[],
  diagnostics: string[],
): void {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    if (entry.isSymbolicLink()) {
      const linkPath = join(directory, entry.name);
      const display = relative(ownerRoot, linkPath);
      let target: string;
      try {
        target = realpathSync(linkPath);
      } catch {
        diagnostics.push(`governed symlink is broken: ${display}`);
        continue;
      }
      if (target.startsWith(realSrcRoot + sep)) {
        continue;
      }
      diagnostics.push(`governed symlink escapes the owner root: ${display}`);
      continue;
    }
    if (entry.isDirectory()) {
      if (isStructurallyExcludedDirectory(entry.name)) continue;
      walk(
        join(directory, entry.name),
        ownerRoot,
        realSrcRoot,
        sources,
        diagnostics,
      );
      continue;
    }
    if (!entry.isFile()) continue;
    if (isStructurallyExcludedFile(entry.name)) continue;
    if (!entry.name.endsWith(".tsx")) continue;

    const absPath = join(directory, entry.name);
    sources.push(relative(ownerRoot, absPath).split(sep).join("/"));
  }
}

export function enumerateGovernedSources(
  ownerRoot: string,
): GovernedSourceResult {
  const diagnostics: string[] = [];
  const sources: string[] = [];
  const srcRoot = join(ownerRoot, "src");
  if (!existsSync(srcRoot)) return { sources, diagnostics };
  const realSrcRoot = realpathSync(srcRoot);
  walk(srcRoot, ownerRoot, realSrcRoot, sources, diagnostics);
  sources.sort();
  return { sources, diagnostics };
}
