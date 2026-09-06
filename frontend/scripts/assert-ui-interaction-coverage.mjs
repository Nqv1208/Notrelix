import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const reportPath = process.argv[2];
if (!reportPath) {
  console.error(
    "Usage: node scripts/assert-ui-interaction-coverage.mjs <vitest-json-output-file>",
  );
  process.exit(1);
}

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const manifestFileName = "ui-evidence.manifest.json";

function walkDirectories(root) {
  if (!existsSync(root)) return [];
  const directories = [root];
  for (const entry of readdirSync(root, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    if (
      entry.name.startsWith(".") ||
      entry.name === "node_modules" ||
      entry.name === "dist"
    )
      continue;
    directories.push(...walkDirectories(join(root, entry.name)));
  }
  return directories;
}

function manifestPaths() {
  return [
    join(frontendRoot, "packages/ui/web"),
    join(frontendRoot, "packages/product"),
    join(frontendRoot, "packages/features"),
  ].flatMap((root) =>
    walkDirectories(root)
      .map((directory) => join(directory, "verification", manifestFileName))
      .filter((path) => existsSync(path) && statSync(path).isFile()),
  );
}

function requiredInteractionTests() {
  const required = new Set();
  for (const manifestPath of manifestPaths()) {
    const ownerRoot = dirname(dirname(manifestPath));
    const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));
    for (const surface of manifest.surfaces ?? []) {
      if (!surface.checks?.includes("interaction")) continue;
      for (const testPath of surface.interactionTests ?? []) {
        required.add(resolve(ownerRoot, testPath));
      }
    }
  }
  return required;
}

function collectExecutedPassingFiles(report) {
  const files = new Set();
  function visit(value) {
    if (!value || typeof value !== "object") return;
    const candidatePath = value.filepath ?? value.name ?? value.file;
    const assertions = value.assertionResults ?? value.tests ?? value.tasks;
    const hasPassingTest =
      Array.isArray(assertions) &&
      assertions.some((item) => {
        const status = item.status ?? item.result?.state;
        return status === "passed" || status === "pass";
      });
    if (typeof candidatePath === "string" && hasPassingTest) {
      files.add(resolve(frontendRoot, candidatePath));
    }
    for (const child of Object.values(value)) {
      if (Array.isArray(child)) child.forEach(visit);
      else if (child && typeof child === "object") visit(child);
    }
  }
  visit(report);
  return files;
}

let report;
try {
  report = JSON.parse(readFileSync(reportPath, "utf8"));
} catch {
  console.error(
    `UI interaction coverage: could not read Vitest JSON report at ${reportPath}.`,
  );
  process.exit(1);
}

const required = requiredInteractionTests();
if (required.size === 0) {
  console.error(
    "UI interaction coverage: zero manifest-declared interaction tests.",
  );
  process.exit(1);
}

const executed = collectExecutedPassingFiles(report);
const missing = [...required].filter((path) => !executed.has(path));

if (missing.length > 0) {
  console.error("UI interaction coverage: missing executed passing tests:");
  for (const path of missing)
    console.error(`- ${relative(frontendRoot, path)}`);
  process.exit(1);
}

console.log(
  `UI interaction coverage: ${required.size} manifest-declared test files executed.`,
);
