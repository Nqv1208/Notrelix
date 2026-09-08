import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { mkdtempSync } from "node:fs";
import { tmpdir } from "node:os";
import { describe, expect, it } from "vitest";
import { checkUiEvidence } from "./check-ui-evidence";

function writeJson(path: string, value: unknown) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`);
}

function createOwner(
  root: string,
  relativeRoot: string,
  files: Record<string, string>,
) {
  const ownerRoot = join(root, relativeRoot);
  mkdirSync(ownerRoot, { recursive: true });
  for (const [filePath, content] of Object.entries(files)) {
    const full = join(ownerRoot, filePath);
    mkdirSync(join(full, ".."), { recursive: true });
    writeFileSync(full, content);
  }
  return ownerRoot;
}

function v2Manifest(overrides: {
  owner: string;
  surfaceId: string;
  sourceName: string;
}) {
  return {
    schemaVersion: 2,
    owner: overrides.owner,
    inventory: { excludedSources: [] },
    surfaces: [
      {
        surfaceId: overrides.surfaceId,
        owner: overrides.owner,
        surfaceKind: "data",
        pureEntry: `src/${overrides.sourceName}.tsx`,
        coveredSources: [],
        stories: [
          {
            id: `${overrides.sourceName}--default`,
            state: "Default",
            visualTargets: [{ viewport: "desktop", theme: "light" }],
          },
        ],
        stateCoverage: {
          required: ["Default"],
          delegated: [],
          notApplicable: [
            {
              state: "Loading",
              reason:
                "Loading presentation is owned by a dedicated loading surface.",
              authority: "docs/product/work-management.md",
            },
            {
              state: "Empty",
              reason: "Empty board is a distinct board-level story.",
              authority: "docs/product/work-management.md",
            },
            {
              state: "Error",
              reason:
                "Fetch failure is owned by a dedicated unavailable surface.",
              authority: "docs/product/work-management.md",
            },
            {
              state: "EdgeData",
              reason: "Edge-value input is a distinct board-level story.",
              authority: "docs/product/work-management.md",
            },
            {
              state: "HighDensity",
              reason: "High-density input is a distinct board-level story.",
              authority: "docs/product/work-management.md",
            },
            {
              state: "ReadOnly",
              reason:
                "No product-authoritative read-only contract exists at this baseline.",
              authority: "docs/product/work-management.md",
            },
            {
              state: "PermissionLimited",
              reason:
                "No permission-limited capability contract exists at this baseline.",
              authority: "docs/product/work-management.md",
            },
          ],
        },
        responsive: false,
        themeAware: false,
        checks: ["interaction", "a11y", "visual", "purity"],
        interactionCases: [
          {
            id: "board",
            testFile:
              "src/__tests__/kanban-board.interaction.component.test.tsx",
          },
        ],
      },
    ],
  };
}

describe("checkUiEvidence", () => {
  it("passes when a single manifest classifies its governed source once and the Storybook index binds its story", () => {
    const root = mkdtempSync(join(tmpdir(), "notrelix-ui-evidence-"));
    const ownerRoot = createOwner(root, "packages/features/wm", {
      "src/kanban-board.tsx": "export const KanbanBoard = () => null;\n",
      "src/__tests__/kanban-board.interaction.component.test.tsx":
        "import { it } from 'vitest'; it('works', () => {});\n",
    });
    writeJson(join(ownerRoot, "verification/ui-evidence.manifest.json"), {
      schemaVersion: 2,
      owner: "@notrelix/wm",
      inventory: { excludedSources: [] },
      surfaces: [
        {
          surfaceId: "wm.board",
          owner: "@notrelix/wm",
          surfaceKind: "data",
          pureEntry: "src/kanban-board.tsx",
          coveredSources: [],
          stories: [
            {
              id: "kanban-board--default",
              state: "Default",
              visualTargets: [{ viewport: "desktop", theme: "light" }],
            },
          ],
          stateCoverage: {
            required: ["Default"],
            delegated: [],
            notApplicable: [
              {
                state: "Loading",
                reason:
                  "Loading presentation is owned by a dedicated loading surface.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "Empty",
                reason: "Empty board is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "Error",
                reason:
                  "Fetch failure is owned by a dedicated unavailable surface.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "EdgeData",
                reason: "Edge-value input is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "HighDensity",
                reason: "High-density input is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "ReadOnly",
                reason:
                  "No product-authoritative read-only contract exists at this baseline.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "PermissionLimited",
                reason:
                  "No permission-limited capability contract exists at this baseline.",
                authority: "docs/product/work-management.md",
              },
            ],
          },
          responsive: false,
          themeAware: false,
          checks: ["interaction", "a11y", "visual", "purity"],
          interactionCases: [
            {
              id: "board",
              testFile:
                "src/__tests__/kanban-board.interaction.component.test.tsx",
            },
          ],
        },
      ],
    });
    const indexPath = join(
      root,
      "tooling/storybook/web/storybook-static/index.json",
    );
    writeJson(indexPath, {
      entries: {
        "kanban-board--default": {
          id: "kanban-board--default",
          tags: ["fui-surface--wm.board", "fui-state--Default"],
        },
      },
    });

    expect(checkUiEvidence(root, indexPath)).toEqual({
      ok: true,
      diagnostics: [],
      surfaceCount: 1,
      requiredStateCount: 1,
    });
  });

  it("rejects schemaVersion 1 and reports a dedicated no-manifests message", () => {
    const root = mkdtempSync(join(tmpdir(), "notrelix-ui-evidence-"));
    const ownerRoot = createOwner(root, "packages/features/wm", {
      "src/kanban-board.tsx": "export const KanbanBoard = () => null;\n",
    });
    writeJson(join(ownerRoot, "verification/ui-evidence.manifest.json"), {
      schemaVersion: 1,
      surfaces: [],
    });
    const indexPath = join(
      root,
      "tooling/storybook/web/storybook-static/index.json",
    );
    writeJson(indexPath, { entries: {} });

    const result = checkUiEvidence(root, indexPath);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain("schemaVersion must be 2");
  });

  it("fails when no manifests are discovered", () => {
    const root = mkdtempSync(join(tmpdir(), "notrelix-ui-evidence-"));
    createOwner(root, "packages/features/wm", {
      "src/kanban-board.tsx": "export const KanbanBoard = () => null;\n",
    });
    const indexPath = join(
      root,
      "tooling/storybook/web/storybook-static/index.json",
    );
    writeJson(indexPath, { entries: {} });

    const result = checkUiEvidence(root, indexPath);

    expect(result.ok).toBe(false);
    expect(result.diagnostics).toContain(
      "no UI evidence manifests were discovered",
    );
  });

  it("reports every governed source classified exactly once and names both owners on duplication", () => {
    const root = mkdtempSync(join(tmpdir(), "notrelix-ui-evidence-"));
    const ownerARoot = createOwner(root, "packages/features/a", {
      "src/a/file.tsx": "export const A = () => null;\n",
    });
    const ownerBRoot = createOwner(root, "packages/features/b", {
      "src/b/file.tsx": "export const B = () => null;\n",
    });
    const indexPath = join(
      root,
      "tooling/storybook/web/storybook-static/index.json",
    );
    writeJson(indexPath, { entries: {} });

    const manifest = v2Manifest({
      owner: "@notrelix/a",
      surfaceId: "a.first",
      sourceName: "a/file",
    });
    writeJson(
      join(ownerARoot, "verification/ui-evidence.manifest.json"),
      manifest,
    );

    const dupManifest = v2Manifest({
      owner: "@notrelix/b",
      surfaceId: "b.first",
      sourceName: "b/file",
    });
    dupManifest.surfaces = [
      { ...dupManifest.surfaces[0], pureEntry: "../a/src/a/file.tsx" },
    ];
    writeJson(
      join(ownerBRoot, "verification/ui-evidence.manifest.json"),
      dupManifest,
    );

    const result = checkUiEvidence(root, indexPath);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain(
      "must not traverse outside the owner root",
    );
  });

  it("reports unclassified governed sources", () => {
    const root = mkdtempSync(join(tmpdir(), "notrelix-ui-evidence-"));
    const ownerRoot = createOwner(root, "packages/features/wm", {
      "src/kanban-board.tsx": "export const KanbanBoard = () => null;\n",
      "src/oto-board.tsx": "export const OtoBoard = () => null;\n",
      "src/__tests__/kanban-board.interaction.component.test.tsx":
        "import { it } from 'vitest'; it('works', () => {});\n",
    });
    writeJson(join(ownerRoot, "verification/ui-evidence.manifest.json"), {
      schemaVersion: 2,
      owner: "@notrelix/wm",
      inventory: { excludedSources: [] },
      surfaces: [
        {
          surfaceId: "wm.board",
          owner: "@notrelix/wm",
          surfaceKind: "data",
          pureEntry: "src/kanban-board.tsx",
          coveredSources: [],
          stories: [
            {
              id: "kanban-board--default",
              state: "Default",
              visualTargets: [{ viewport: "desktop", theme: "light" }],
            },
          ],
          stateCoverage: {
            required: ["Default"],
            delegated: [],
            notApplicable: [
              {
                state: "Loading",
                reason:
                  "Loading presentation is owned by a dedicated loading surface.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "Empty",
                reason: "Empty board is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "Error",
                reason:
                  "Fetch failure is owned by a dedicated unavailable surface.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "EdgeData",
                reason: "Edge-value input is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "HighDensity",
                reason: "High-density input is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "ReadOnly",
                reason:
                  "No product-authoritative read-only contract exists at this baseline.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "PermissionLimited",
                reason:
                  "No permission-limited capability contract exists at this baseline.",
                authority: "docs/product/work-management.md",
              },
            ],
          },
          responsive: false,
          themeAware: false,
          checks: ["interaction", "a11y", "visual", "purity"],
          interactionCases: [
            {
              id: "board",
              testFile:
                "src/__tests__/kanban-board.interaction.component.test.tsx",
            },
          ],
        },
      ],
    });
    const indexPath = join(
      root,
      "tooling/storybook/web/storybook-static/index.json",
    );
    writeJson(indexPath, {
      entries: {
        "kanban-board--default": {
          id: "kanban-board--default",
          tags: ["fui-surface--wm.board", "fui-state--Default"],
        },
      },
    });

    const result = checkUiEvidence(root, indexPath);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain(
      "unclassified governed source",
    );
  });

  it("rejects duplicate classification and unregistered collected surfaces", () => {
    const root = mkdtempSync(join(tmpdir(), "notrelix-ui-evidence-"));
    const ownerRoot = createOwner(root, "packages/features/wm", {
      "src/kanban-board.tsx": "export const KanbanBoard = () => null;\n",
      "src/__tests__/kanban-board.interaction.component.test.tsx":
        "import { it } from 'vitest'; it('works', () => {});\n",
    });
    writeJson(join(ownerRoot, "verification/ui-evidence.manifest.json"), {
      schemaVersion: 2,
      owner: "@notrelix/wm",
      inventory: { excludedSources: [] },
      surfaces: [
        {
          surfaceId: "wm.board",
          owner: "@notrelix/wm",
          surfaceKind: "feedback",
          pureEntry: "src/kanban-board.tsx",
          coveredSources: [],
          stories: [],
          stateCoverage: {
            required: [],
            delegated: [],
            notApplicable: [
              {
                state: "Default",
                reason: "Default presentation is owned by the board surface.",
                authority: "docs/product/work-management.md",
              },
            ],
          },
          responsive: false,
          themeAware: false,
          checks: [],
          interactionCases: [],
        },
        {
          surfaceId: "wm.board.dup",
          owner: "@notrelix/wm",
          surfaceKind: "feedback",
          pureEntry: "src/kanban-board.tsx",
          coveredSources: [],
          stories: [],
          stateCoverage: {
            required: [],
            delegated: [],
            notApplicable: [
              {
                state: "Default",
                reason: "Default presentation is owned by the board surface.",
                authority: "docs/product/work-management.md",
              },
            ],
          },
          responsive: false,
          themeAware: false,
          checks: [],
          interactionCases: [],
        },
      ],
    });
    const indexPath = join(
      root,
      "tooling/storybook/web/storybook-static/index.json",
    );
    writeJson(indexPath, {
      entries: {
        "unknown--default": {
          id: "unknown--default",
          tags: ["fui-surface--not-registered", "fui-state--Default"],
        },
      },
    });

    const result = checkUiEvidence(root, indexPath);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain("classified 2 times");
    expect(result.diagnostics.join("\n")).toContain(
      "unregistered collected surface: not-registered",
    );
  });

  it("detects missing Storybook index and missing collected binding/story", () => {
    const root = mkdtempSync(join(tmpdir(), "notrelix-ui-evidence-"));
    const ownerRoot = createOwner(root, "packages/features/wm", {
      "src/kanban-board.tsx": "export const KanbanBoard = () => null;\n",
      "src/__tests__/kanban-board.interaction.component.test.tsx":
        "import { it } from 'vitest'; it('works', () => {});\n",
    });
    writeJson(join(ownerRoot, "verification/ui-evidence.manifest.json"), {
      schemaVersion: 2,
      owner: "@notrelix/wm",
      inventory: { excludedSources: [] },
      surfaces: [
        {
          surfaceId: "wm.board",
          owner: "@notrelix/wm",
          surfaceKind: "data",
          pureEntry: "src/kanban-board.tsx",
          coveredSources: [],
          stories: [
            {
              id: "kanban-board--default",
              state: "Default",
              visualTargets: [{ viewport: "desktop", theme: "light" }],
            },
          ],
          stateCoverage: {
            required: ["Default"],
            delegated: [],
            notApplicable: [
              {
                state: "Loading",
                reason:
                  "Loading presentation is owned by a dedicated loading surface.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "Empty",
                reason: "Empty board is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "Error",
                reason:
                  "Fetch failure is owned by a dedicated unavailable surface.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "EdgeData",
                reason: "Edge-value input is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "HighDensity",
                reason: "High-density input is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "ReadOnly",
                reason:
                  "No product-authoritative read-only contract exists at this baseline.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "PermissionLimited",
                reason:
                  "No permission-limited capability contract exists at this baseline.",
                authority: "docs/product/work-management.md",
              },
            ],
          },
          responsive: false,
          themeAware: false,
          checks: ["interaction", "a11y", "visual", "purity"],
          interactionCases: [
            {
              id: "board",
              testFile:
                "src/__tests__/kanban-board.interaction.component.test.tsx",
            },
          ],
        },
      ],
    });
    const indexPath = join(
      root,
      "tooling/storybook/web/storybook-static/index.json",
    );
    writeJson(indexPath, { entries: {} });

    const result = checkUiEvidence(root, indexPath);

    expect(result.ok).toBe(false);
    expect(result.diagnostics).toContain(
      "missing collected binding: wm.board::Default",
    );
    expect(result.diagnostics).toContain(
      "missing collected story id: kanban-board--default",
    );
    expect(result.diagnostics.length).toBeGreaterThan(0);
  });

  it("requires a collected manifest story to bind its declared surface and state tags", () => {
    const root = mkdtempSync(join(tmpdir(), "notrelix-ui-evidence-"));
    const ownerRoot = createOwner(root, "packages/features/wm", {
      "src/kanban-board.tsx": "export const KanbanBoard = () => null;\n",
      "src/__tests__/kanban-board.interaction.component.test.tsx":
        "import { it } from 'vitest'; it('works', () => {});\n",
    });
    writeJson(join(ownerRoot, "verification/ui-evidence.manifest.json"), {
      schemaVersion: 2,
      owner: "@notrelix/wm",
      inventory: { excludedSources: [] },
      surfaces: [
        {
          surfaceId: "wm.board",
          owner: "@notrelix/wm",
          surfaceKind: "data",
          pureEntry: "src/kanban-board.tsx",
          coveredSources: [],
          stories: [
            {
              id: "kanban-board--default",
              state: "Default",
              visualTargets: [{ viewport: "desktop", theme: "light" }],
            },
          ],
          stateCoverage: {
            required: ["Default"],
            delegated: [],
            notApplicable: [
              {
                state: "Loading",
                reason:
                  "Loading presentation is owned by a dedicated loading surface.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "Empty",
                reason: "Empty board is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "Error",
                reason:
                  "Fetch failure is owned by a dedicated unavailable surface.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "EdgeData",
                reason: "Edge-value input is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "HighDensity",
                reason: "High-density input is a distinct board-level story.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "ReadOnly",
                reason:
                  "No product-authoritative read-only contract exists at this baseline.",
                authority: "docs/product/work-management.md",
              },
              {
                state: "PermissionLimited",
                reason:
                  "No permission-limited capability contract exists at this baseline.",
                authority: "docs/product/work-management.md",
              },
            ],
          },
          responsive: false,
          themeAware: false,
          checks: ["interaction", "a11y", "visual", "purity"],
          interactionCases: [
            {
              id: "board",
              testFile:
                "src/__tests__/kanban-board.interaction.component.test.tsx",
            },
          ],
        },
      ],
    });
    const indexPath = join(
      root,
      "tooling/storybook/web/storybook-static/index.json",
    );
    writeJson(indexPath, {
      entries: {
        "kanban-board--default": {
          id: "kanban-board--default",
          tags: ["fui-surface--wm.wrong", "fui-state--Empty"],
        },
      },
    });

    const result = checkUiEvidence(root, indexPath);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain(
      "story kanban-board--default must bind surface wm.board state Default",
    );
  });
});
