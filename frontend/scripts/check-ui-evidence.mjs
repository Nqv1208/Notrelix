import { existsSync, readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const manifestPath = resolve(
  frontendRoot,
  process.argv[2] ??
    "packages/product/work-management/web/verification/ui-evidence.manifest.json",
);
const indexPath = resolve(
  frontendRoot,
  process.argv[3] ?? "tooling/storybook/web/storybook-static/index.json",
);
const allowedStates = new Set([
  "Default",
  "Loading",
  "Empty",
  "Error",
  "PermissionLimited",
  "ReadOnly",
  "EdgeData",
  "HighDensity",
]);
const allowedChecks = new Set(["a11y", "visual", "interaction"]);

function fail(message) {
  console.error(`[UI_EVIDENCE_INVALID] ${message}`);
  process.exitCode = 1;
}

if (!existsSync(manifestPath)) {
  fail(`missing manifest: ${manifestPath}`);
} else if (!existsSync(indexPath)) {
  fail(`missing Storybook index: ${indexPath}`);
} else {
  const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));
  const index = JSON.parse(readFileSync(indexPath, "utf8"));
  if (manifest.schemaVersion !== 2 || !Array.isArray(manifest.surfaces))
    fail("unsupported schema");
  if (manifest.surfaces?.length === 0)
    fail("adopted manifest registers zero surfaces");
  const surfaceIds = new Set();
  const required = new Set();
  for (const surface of manifest.surfaces ?? []) {
    if (surfaceIds.has(surface.id)) fail(`duplicate surface id: ${surface.id}`);
    surfaceIds.add(surface.id);
    const packageRoot = join(
      frontendRoot,
      "packages/product",
      manifest.product,
      manifest.platform,
    );
    if (!existsSync(join(packageRoot, surface.storyFile)))
      fail(`missing story file: ${surface.storyFile}`);
    for (const state of surface.requiredStates ?? []) {
      if (!allowedStates.has(state)) fail(`unsupported state: ${state}`);
      required.add(`${surface.id}::${state}`);
    }
    for (const check of surface.checks ?? [])
      if (!allowedChecks.has(check)) fail(`unsupported check: ${check}`);
    if (
      surface.checks?.includes("interaction") &&
      (!surface.interactionTestFile ||
        !existsSync(join(packageRoot, surface.interactionTestFile)))
    ) {
      fail(`missing interaction test: ${surface.id}`);
    }
    for (const entry of surface.notApplicableStates ?? []) {
      if (!entry.state || !entry.reason || !entry.authority)
        fail(`malformed N/A entry: ${surface.id}`);
    }
  }

  const seen = new Set();
  for (const story of Object.values(index.entries ?? {})) {
    const tags = story.tags ?? [];
    const surfaceTag = tags.find((tag) => tag.startsWith("fui-surface--"));
    const stateTag = tags.find((tag) => tag.startsWith("fui-state--"));
    if (!surfaceTag && !stateTag) continue;
    if (!surfaceTag || !stateTag)
      fail(`story ${story.id} must have one surface and one state tag`);
    const surface = surfaceTag?.slice("fui-surface--".length);
    const state = stateTag?.slice("fui-state--".length);
    if (!surfaceIds.has(surface))
      fail(`unregistered collected surface: ${surface}`);
    if (!allowedStates.has(state))
      fail(`unsupported collected state: ${state}`);
    const binding = `${surface}::${state}`;
    if (seen.has(binding)) fail(`duplicate binding: ${binding}`);
    seen.add(binding);
  }
  for (const binding of required)
    if (!seen.has(binding)) fail(`missing collected binding: ${binding}`);
  if (!process.exitCode)
    console.log(
      `UI evidence valid: ${surfaceIds.size} surfaces, ${required.size} required states.`,
    );
}
