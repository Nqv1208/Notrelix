// Fast source-level mirror of assert-ui-interaction-coverage.mjs for the pre-push
// hook: fails when a test carries an FUI[surface:case] marker that no
// ui-evidence manifest declares, without running Vitest. CI still runs the
// report-based check; this only catches the drift before a push.
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const manifestFileName = "ui-evidence.manifest.json";
// Tests write a marker literally or through a `fui("surface:case", ...)` helper.
const CASE_MARKER_PATTERN =
  /(?:FUI\[|\bfui\(\s*["'`])([A-Za-z0-9._-]+):([A-Za-z0-9._-]+)(?:\]|["'`])/g;
const TEST_FILE_PATTERN = /\.test\.tsx?$/;

function walk(root, onFile) {
  if (!existsSync(root)) return;
  for (const entry of readdirSync(root, { withFileTypes: true })) {
    if (
      entry.name.startsWith(".") ||
      entry.name === "node_modules" ||
      entry.name === "dist"
    )
      continue;
    const path = join(root, entry.name);
    if (entry.isDirectory()) walk(path, onFile);
    else onFile(path);
  }
}

const roots = ["packages/ui/web", "packages/product", "packages/features"].map(
  (root) => join(frontendRoot, root),
);

const declared = new Set();
for (const root of roots) {
  walk(root, (path) => {
    if (!path.endsWith(join("verification", manifestFileName))) return;
    if (!statSync(path).isFile()) return;
    const manifest = JSON.parse(readFileSync(path, "utf8"));
    for (const surface of manifest.surfaces ?? []) {
      if (!surface.checks?.includes("interaction")) continue;
      for (const interactionCase of surface.interactionCases ?? []) {
        declared.add(`${surface.surfaceId}:${interactionCase.id}`);
      }
    }
  });
}

const written = new Map();
for (const root of [...roots, join(frontendRoot, "apps")]) {
  walk(root, (path) => {
    if (!TEST_FILE_PATTERN.test(path)) return;
    const source = readFileSync(path, "utf8");
    for (const match of source.matchAll(CASE_MARKER_PATTERN)) {
      const marker = `${match[1]}:${match[2]}`;
      const files = written.get(marker) ?? [];
      files.push(relative(frontendRoot, path));
      written.set(marker, files);
    }
  });
}

const errors = [];
for (const [marker, files] of written) {
  if (!declared.has(marker)) {
    errors.push(`unknown case marker FUI[${marker}] in ${files[0]}`);
  }
}
// Missing markers are left to CI: some tests build their marker at runtime,
// which a source scan cannot see.

if (errors.length > 0) {
  console.error("UI interaction markers: drift detected:");
  for (const error of errors.sort()) console.error(`- ${error}`);
  process.exit(1);
}

console.log(
  `UI interaction markers: all ${written.size} markers in test sources are declared by a ui-evidence manifest.`,
);
