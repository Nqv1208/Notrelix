import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, resolve } from "node:path";
import { validateUiEvidenceManifest } from "../../../tooling/testing/src/ui-evidence-schema";

export interface UiEvidenceTarget {
  readonly surfaceId: string;
  readonly storyId: string;
  readonly state: string;
  readonly checks: readonly string[];
}

const MANIFEST_FILE_NAME = "ui-evidence.manifest.json";
const FRONTEND_ROOT = resolve(__dirname, "../../..");

function walkDirectories(root: string): string[] {
  const directories = [root];
  for (const entry of readdirSync(root, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    if (
      entry.name.startsWith(".") ||
      entry.name === "node_modules" ||
      entry.name === "dist"
    )
      continue;
    const full = join(root, entry.name);
    try {
      statSync(full);
      directories.push(...walkDirectories(full));
    } catch {
      // ignored
    }
  }
  return directories;
}

function manifestPaths(): string[] {
  return [
    join(FRONTEND_ROOT, "packages/ui/web"),
    join(FRONTEND_ROOT, "packages/product"),
    join(FRONTEND_ROOT, "packages/features"),
  ].flatMap((root) =>
    walkDirectories(root)
      .map((directory) => join(directory, "verification", MANIFEST_FILE_NAME))
      .filter((path) => {
        try {
          return statSync(path).isFile();
        } catch {
          return false;
        }
      }),
  );
}

export function uiEvidenceTargets(
  check: "a11y" | "visual" | "purity",
): UiEvidenceTarget[] {
  const targets: UiEvidenceTarget[] = [];
  for (const manifestPath of manifestPaths()) {
    const validation = validateUiEvidenceManifest(
      JSON.parse(readFileSync(manifestPath, "utf8")),
    );
    if (!validation.ok || !validation.manifest) {
      throw new Error(
        `Invalid UI evidence manifest ${manifestPath}: ${validation.diagnostics.join("; ")}`,
      );
    }
    for (const surface of validation.manifest.surfaces) {
      if (!surface.checks.includes(check)) continue;
      for (const story of surface.stories) {
        if (!surface.requiredStates.includes(story.state)) continue;
        targets.push({
          surfaceId: surface.surfaceId,
          storyId: story.id,
          state: story.state,
          checks: surface.checks,
        });
      }
    }
  }
  if (targets.length === 0)
    throw new Error(`No UI evidence targets for ${check}`);
  return targets;
}

export function storybookIframeUrl(storyId: string): string {
  return `/iframe.html?id=${storyId}`;
}
