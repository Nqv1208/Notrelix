import { existsSync, readFileSync } from "node:fs";
import { join, relative, resolve } from "node:path";
import { discoverUiEvidenceManifests, resolveFrontendRootFromHere } from "./manifest-discovery";
import { enumerateGovernedSources } from "./governed-source-enumerator";

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
    diagnostics.push("no UI evidence manifests were discovered");
  }

  const index = loadStorybookIndex(indexPath);
  if (!index) {
    diagnostics.push(
      `missing Storybook index: ${relative(frontendRoot, indexPath)}`,
    );
  }

  interface ClassificationEntry {
    absPath: string;
    displayPath: string;
    ownerRoot: string;
    owner: string;
    role: "pureEntry" | "coveredSource" | "excludedSource";
    surfaceId?: string;
    manifestPath: string;
  }

  const classificationEntries: ClassificationEntry[] = [];
  const registeredSurfaces = new Set<string>();
  const requiredBindings = new Set<string>();
  const requiredStoryIds = new Set<string>();
  const storyExpectations = new Map<string, { surfaceId: string; state: string }>();
  const ownerRoots = new Set<string>();
  let requiredStateCount = 0;

  for (const discovered of discovery.manifests) {
    const ownerRoot = discovered.ownerRoot;
    ownerRoots.add(ownerRoot);

    const register = (
      role: ClassificationEntry["role"],
      relativePath: string,
      surfaceId?: string,
    ): void => {
      const absPath = join(ownerRoot, relativePath);
      classificationEntries.push({
        absPath,
        displayPath: relativePath,
        ownerRoot,
        owner: discovered.manifest.owner,
        role,
        surfaceId,
        manifestPath: relative(frontendRoot, discovered.manifestPath),
      });
      if (!existsSync(absPath)) {
        diagnostics.push(
          `${relative(frontendRoot, discovered.manifestPath)}: missing ${role} ${relativePath}`,
        );
      }
    };

    for (const surface of discovered.manifest.surfaces) {
      if (registeredSurfaces.has(surface.surfaceId)) {
        diagnostics.push(
          `duplicate registered surfaceId across manifests: ${surface.surfaceId}`,
        );
      }
      registeredSurfaces.add(surface.surfaceId);

      if (surface.stories.length === 0) {
        diagnostics.push(
          `${relative(frontendRoot, discovered.manifestPath)}: surface ${surface.surfaceId} has no stories`,
        );
      }

      register("pureEntry", surface.pureEntry, surface.surfaceId);
      for (const coveredSource of surface.coveredSources) {
        register("coveredSource", coveredSource, surface.surfaceId);
      }
      for (const interactionCase of surface.interactionCases) {
        const absPath = join(ownerRoot, interactionCase.testFile);
        if (!existsSync(absPath)) {
          diagnostics.push(
            `${relative(frontendRoot, discovered.manifestPath)}: missing interaction test ${interactionCase.testFile}`,
          );
        }
      }

      for (const state of surface.stateCoverage.required) {
        requiredStateCount += 1;
        requiredBindings.add(`${surface.surfaceId}::${state}`);
      }
      for (const story of surface.stories) {
        requiredStoryIds.add(story.id);
        requiredBindings.add(`${surface.surfaceId}::${story.state}`);
        if (storyExpectations.has(story.id)) {
          diagnostics.push(`duplicate manifest story id: ${story.id}`);
        }
        storyExpectations.set(story.id, {
          surfaceId: surface.surfaceId,
          state: story.state,
        });
      }
    }

    for (const excluded of discovered.manifest.inventory.excludedSources) {
      register("excludedSource", excluded.path);
    }
  }

  const governedByOwner = new Map<string, string>();
  for (const ownerRoot of ownerRoots) {
    const enumeration = enumerateGovernedSources(ownerRoot);
    diagnostics.push(...enumeration.diagnostics);
    for (const source of enumeration.sources) {
      governedByOwner.set(join(ownerRoot, source), ownerRoot);
    }
  }

  for (const entry of classificationEntries) {
    const owningRoot = governedByOwner.get(entry.absPath);
    if (owningRoot && owningRoot !== entry.ownerRoot) {
      diagnostics.push(
        `${relative(frontendRoot, entry.manifestPath)}: ${entry.displayPath} is governed by a different owner and cannot be claimed by ${entry.owner}`,
      );
    }
  }

  const classificationByPath = new Map<string, ClassificationEntry[]>();
  for (const entry of classificationEntries) {
    const entries = classificationByPath.get(entry.absPath) ?? [];
    entries.push(entry);
    classificationByPath.set(entry.absPath, entries);
  }

  for (const [absPath, entries] of classificationByPath) {
    if (entries.length <= 1) continue;
    if (!governedByOwner.has(absPath)) continue;
    const owners = [...new Set(entries.map((entry) => entry.owner))];
    const surfaces = [...new Set(entries.map((entry) => entry.surfaceId).filter(Boolean))];
    diagnostics.push(
      `source ${relative(frontendRoot, absPath)} is classified ${entries.length} times (owners: ${owners.join(", ")}${
        surfaces.length > 0 ? `; surfaces: ${surfaces.join(", ")}` : ""
      })`,
    );
  }

  for (const absPath of governedByOwner.keys()) {
    const entries = classificationByPath.get(absPath) ?? [];
    if (entries.length === 0) {
      diagnostics.push(
        `unclassified governed source: ${relative(frontendRoot, absPath)}`,
      );
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

      const expectation = storyExpectations.get(story.id);
      if (expectation) {
        const tags = story.tags ?? [];
        if (
          !tags.includes(`${SURFACE_TAG_PREFIX}${expectation.surfaceId}`) ||
          !tags.includes(`${STATE_TAG_PREFIX}${expectation.state}`)
        ) {
          diagnostics.push(
            `story ${story.id} must bind surface ${expectation.surfaceId} state ${expectation.state}`,
          );
        }
      }
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
    if (!requiredBindings.has(binding) && !seenBindings.has(binding)) {
      diagnostics.push(`unregistered collected binding: ${binding}`);
    }
    seenBindings.add(binding);
  }

  for (const binding of requiredBindings) {
    if (!seenBindings.has(binding)) {
      diagnostics.push(`missing collected binding: ${binding}`);
    }
  }
  for (const storyId of requiredStoryIds) {
    if (!seenStoryIds.has(storyId)) {
      diagnostics.push(`missing collected story id: ${storyId}`);
    }
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