import { readdirSync, statSync } from "node:fs";
import { join, relative, resolve } from "node:path";

const root = resolve(process.cwd());
const roots = ["apps", "packages", "tooling"];
const ignoredDirectories = new Set([
  "node_modules",
  "dist",
  ".next",
  "coverage",
  "storybook-static",
]);

const testFiles = [];

function walk(directory) {
  for (const name of readdirSync(directory)) {
    if (ignoredDirectories.has(name)) continue;

    const absolute = join(directory, name);
    const stat = statSync(absolute);

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
  const absolute = join(root, candidate);
  try {
    walk(absolute);
  } catch {
    // A workspace category may not exist in a reduced fixture.
  }
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

if (invalid.length > 0) {
  console.error(
    "Test taxonomy violation. Every Vitest file must have one explicit suite suffix:",
  );
  for (const file of invalid) console.error(`  - ${file}`);
  process.exit(1);
}

console.log(
  `Test taxonomy OK: ${testFiles.length} Vitest test files classified.`,
);
