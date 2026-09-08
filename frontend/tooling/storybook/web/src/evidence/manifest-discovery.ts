import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { validateUiEvidenceManifest } from "../../../../testing/src/ui-evidence-schema";

export interface DiscoveredUiEvidenceManifest {
  manifestPath: string;
  ownerRoot: string;
  manifest: NonNullable<
    ReturnType<typeof validateUiEvidenceManifest>["manifest"]
  >;
}

export interface UiEvidenceDiscoveryResult {
  manifests: DiscoveredUiEvidenceManifest[];
  diagnostics: string[];
}

const MANIFEST_FILE_NAME = "ui-evidence.manifest.json";

function walkDirectories(root: string): string[] {
  if (!existsSync(root)) return [];
  const directories = [root];
  for (const entry of readdirSync(root, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    if (
      entry.name === "node_modules" ||
      entry.name === "dist" ||
      entry.name === "storybook-static"
    )
      continue;
    directories.push(...walkDirectories(join(root, entry.name)));
  }
  return directories;
}

export function discoverUiEvidenceManifests(
  frontendRoot: string,
): UiEvidenceDiscoveryResult {
  const roots = [
    join(frontendRoot, "packages/ui/web"),
    join(frontendRoot, "packages/product"),
    join(frontendRoot, "packages/features"),
  ];
  const diagnostics: string[] = [];
  const manifests: DiscoveredUiEvidenceManifest[] = [];

  for (const root of roots) {
    for (const directory of walkDirectories(root)) {
      const manifestPath = join(directory, "verification", MANIFEST_FILE_NAME);
      if (!existsSync(manifestPath)) continue;

      let parsed: unknown;
      try {
        parsed = JSON.parse(readFileSync(manifestPath, "utf8"));
      } catch (error) {
        diagnostics.push(
          `${relative(frontendRoot, manifestPath)} is not valid JSON: ${(error as Error).message}`,
        );
        continue;
      }

      const validation = validateUiEvidenceManifest(parsed);
      if (!validation.ok || !validation.manifest) {
        diagnostics.push(
          ...validation.diagnostics.map(
            (diagnostic: string) =>
              `${relative(frontendRoot, manifestPath)}: ${diagnostic}`,
          ),
        );
        continue;
      }

      manifests.push({
        manifestPath,
        ownerRoot: dirname(dirname(manifestPath)),
        manifest: validation.manifest,
      });
    }
  }

  return { manifests, diagnostics };
}

export function resolveFrontendRootFromHere(): string {
  return resolve(dirname(new URL(import.meta.url).pathname), "../../../../");
}
