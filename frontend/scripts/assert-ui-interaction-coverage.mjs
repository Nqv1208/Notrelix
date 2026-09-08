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

const frontendRoot = process.argv[3]
  ? resolve(process.argv[3])
  : resolve(dirname(fileURLToPath(import.meta.url)), "..");
const manifestFileName = "ui-evidence.manifest.json";
const CASE_MARKER_PATTERN = /FUI\[([A-Za-z0-9._-]+):([A-Za-z0-9._-]+)\]/g;

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

function requiredCases() {
  const required = new Map();
  for (const manifestPath of manifestPaths()) {
    const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));
    for (const surface of manifest.surfaces ?? []) {
      if (!surface.checks?.includes("interaction")) continue;
      const declared = new Set();
      for (const interactionCase of surface.interactionCases ?? []) {
        const marker = `${surface.surfaceId}:${interactionCase.id}`;
        declared.add(marker);
        required.set(marker, {
          manifestPath,
          testFile: interactionCase.testFile,
        });
      }
      if (declared.size === 0) {
        console.error(
          `UI interaction coverage: surface ${surface.surfaceId} declares zero interaction cases.`,
        );
        process.exit(1);
      }
    }
  }
  return required;
}

function collectReportFacts(report) {
  const passingMarkers = new Map();
  const failedOrSkipped = [];
  function visit(value) {
    if (!value || typeof value !== "object") return;
    if (Array.isArray(value.assertionResults)) {
      for (const assertion of value.assertionResults) {
        const title = assertion.title ?? assertion.fullName ?? "";
        const status = assertion.status;
        const markers = [...title.matchAll(CASE_MARKER_PATTERN)].map(
          (match) => `${match[1]}:${match[2]}`,
        );
        if (markers.length > 0 && status !== "passed") {
          failedOrSkipped.push(`${status}: ${title}`);
        }
        for (const marker of markers) {
          if (status === "passed") {
            const occurrences = passingMarkers.get(marker) ?? [];
            occurrences.push(title);
            passingMarkers.set(marker, occurrences);
          }
        }
      }
    }
    for (const child of Object.values(value)) {
      if (Array.isArray(child)) child.forEach(visit);
      else if (child && typeof child === "object") visit(child);
    }
  }
  visit(report);
  return { passingMarkers, failedOrSkipped };
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

const required = requiredCases();
if (required.size === 0) {
  console.error(
    "UI interaction coverage: zero manifest-declared interaction cases.",
  );
  process.exit(1);
}

const { passingMarkers, failedOrSkipped } = collectReportFacts(report);

const errors = [];

for (const title of failedOrSkipped) {
  errors.push(`required case marker assertion is not passing: ${title}`);
}

for (const marker of required.keys()) {
  const occurrences = passingMarkers.get(marker);
  if (!occurrences || occurrences.length === 0) {
    errors.push(`missing passing case marker: FUI[${marker}]`);
  } else if (occurrences.length > 1) {
    errors.push(
      `duplicate passing case marker: FUI[${marker}] (${occurrences.length} assertions)`,
    );
  }
}

for (const marker of passingMarkers.keys()) {
  if (!required.has(marker)) {
    errors.push(`unknown case marker in report: FUI[${marker}]`);
  }
}

if (errors.length > 0) {
  console.error("UI interaction coverage: drift detected:");
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log(
  `UI interaction coverage: ${required.size} manifest-declared interaction cases satisfied by exact passing markers.`,
);
