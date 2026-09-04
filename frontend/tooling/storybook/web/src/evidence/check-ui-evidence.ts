import { existsSync, readFileSync } from "node:fs";
import { join, relative, resolve } from "node:path";
import {
  discoverUiEvidenceManifests,
  resolveFrontendRootFromHere,
} from "./manifest-discovery";

interface StorybookIndex {
  entries?: Record<string, { id: string; tags?: string[] }>;
}

interface UiEvidenceCheckResult {
  ok: boolean;
  diagnostics: string[];
  surfaceCount: number;
  requiredStateCount: number;
}

const SURFACE_TAG_PREFIX = "fui-surface--";
const STATE_TAG_PREFIX = "fui-state--";

function loadStorybookIndex(indexPath: string): StorybookIndex | undefined {
  if (!existsSync(indexPath)) return undefined;
  return JSON.parse(readFileSync(indexPath, "utf8")) as StorybookIndex;
}

export function checkUiEvidence(
  frontendRoot = resolveFrontendRootFromHere(),
  indexPath = join(
    frontendRoot,
    "tooling/storybook/web/storybook-static/index.json",
  ),
): UiEvidenceCheckResult {
  const diagnostics: string[] = [];
  const discovery = discoverUiEvidenceManifests(frontendRoot);
  diagnostics.push(...discovery.diagnostics);

  if (discovery.manifests.length === 0) {
    diagnostics.push(
      "no schemaVersion 1 UI evidence manifests were discovered",
    );
  }

  const index = loadStorybookIndex(indexPath);
  if (!index) {
    diagnostics.push(
      `missing Storybook index: ${relative(frontendRoot, indexPath)}`,
    );
  }

  const registeredSurfaces = new Set<string>();
  const requiredBindings = new Set<string>();
  const requiredStoryIds = new Set<string>();
  let requiredStateCount = 0;

  for (const discovered of discovery.manifests) {
    for (const surface of discovered.manifest.surfaces) {
      if (registeredSurfaces.has(surface.surfaceId)) {
        diagnostics.push(
          `duplicate registered surfaceId across manifests: ${surface.surfaceId}`,
        );
      }
      registeredSurfaces.add(surface.surfaceId);

      const pureEntryPath = join(discovered.ownerRoot, surface.pureEntry);
      if (!existsSync(pureEntryPath)) {
        diagnostics.push(
          `${relative(frontendRoot, discovered.manifestPath)}: missing pureEntry ${surface.pureEntry}`,
        );
      }

      for (const interactionTest of surface.interactionTests) {
        if (!existsSync(join(discovered.ownerRoot, interactionTest))) {
          diagnostics.push(
            `${relative(frontendRoot, discovered.manifestPath)}: missing interaction test ${interactionTest}`,
          );
        }
      }

      for (const state of surface.requiredStates) {
        requiredStateCount += 1;
        requiredBindings.add(`${surface.surfaceId}::${state}`);
      }
      for (const story of surface.stories) {
        requiredStoryIds.add(story.id);
      }
    }
  }

  const seenBindings = new Set<string>();
  const seenStoryIds = new Set<string>();
  for (const story of Object.values(index?.entries ?? {})) {
    if (requiredStoryIds.has(story.id)) {
      if (seenStoryIds.has(story.id)) {
        diagnostics.push(`duplicate collected story id: ${story.id}`);
      }
      seenStoryIds.add(story.id);
    }

    const tags = story.tags ?? [];
    const surfaceTags = tags.filter((tag) =>
      tag.startsWith(SURFACE_TAG_PREFIX),
    );
    const stateTags = tags.filter((tag) => tag.startsWith(STATE_TAG_PREFIX));
    if (surfaceTags.length === 0 && stateTags.length === 0) continue;
    if (surfaceTags.length !== 1 || stateTags.length !== 1) {
      diagnostics.push(
        `story ${story.id} must have exactly one FUI surface tag and one FUI state tag`,
      );
      continue;
    }

    const surfaceTag = surfaceTags[0];
    const stateTag = stateTags[0];
    if (!surfaceTag || !stateTag) continue;
    const surfaceId = surfaceTag.slice(SURFACE_TAG_PREFIX.length);
    const state = stateTag.slice(STATE_TAG_PREFIX.length);
    if (!registeredSurfaces.has(surfaceId)) {
      diagnostics.push(`unregistered collected surface: ${surfaceId}`);
    }

    const binding = `${surfaceId}::${state}`;
    if (seenBindings.has(binding)) {
      diagnostics.push(`duplicate collected binding: ${binding}`);
    }
    seenBindings.add(binding);
  }

  for (const binding of requiredBindings) {
    if (!seenBindings.has(binding))
      diagnostics.push(`missing collected binding: ${binding}`);
  }
  for (const storyId of requiredStoryIds) {
    if (!seenStoryIds.has(storyId))
      diagnostics.push(`missing collected story id: ${storyId}`);
  }

  return {
    ok: diagnostics.length === 0,
    diagnostics,
    surfaceCount: registeredSurfaces.size,
    requiredStateCount,
  };
}

if (process.argv[1]?.endsWith("check-ui-evidence.ts")) {
  const result = checkUiEvidence(resolve(process.cwd()), process.argv[2]);
  if (!result.ok) {
    for (const diagnostic of result.diagnostics)
      console.error(`[UI_EVIDENCE_INVALID] ${diagnostic}`);
    process.exitCode = 1;
  } else {
    console.log(
      `UI evidence valid: ${result.surfaceCount} surfaces, ${result.requiredStateCount} required states.`,
    );
  }
}
