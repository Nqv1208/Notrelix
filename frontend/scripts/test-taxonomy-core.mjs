import { readdirSync, statSync } from "node:fs";
import { join, relative, resolve } from "node:path";

const ignoredDirectories = new Set([
  "node_modules",
  "dist",
  ".next",
  "coverage",
  "storybook-static",
]);

export function validateTestTaxonomy(targetDir = process.cwd()) {
  const root = resolve(targetDir);
  const roots = ["apps", "packages", "tooling"];
  const testFiles = [];

  function walk(directory) {
    let entries = [];
    try {
      entries = readdirSync(directory);
    } catch {
      return;
    }

    for (const name of entries) {
      if (ignoredDirectories.has(name)) continue;

      const absolute = join(directory, name);
      let stat;
      try {
        stat = statSync(absolute);
      } catch {
        continue;
      }

      if (stat.isDirectory()) {
        walk(absolute);
        continue;
      }

      if (/\.test\.(ts|tsx)$/.test(name)) {
        testFiles.push(relative(root, absolute).replaceAll("\\", "/"));
      }
    }
  }

  for (const candidate of roots) {
    walk(join(root, candidate));
  }

  const invalid = [];
  for (const file of testFiles) {
    if (file.startsWith("tooling/generators/")) continue;

    const valid =
      /\.unit\.test\.(ts|tsx)$/.test(file) ||
      /\.component\.test\.(ts|tsx)$/.test(file) ||
      /\.integration\.test\.(ts|tsx)$/.test(file) ||
      /\.mobile\.test\.(ts|tsx)$/.test(file);

    if (!valid) invalid.push(file);
  }

  return {
    valid: invalid.length === 0,
    testFiles,
    invalid,
  };
}
