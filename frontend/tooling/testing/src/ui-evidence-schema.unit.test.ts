import { describe, expect, it } from "vitest";
import { validateUiEvidenceManifest } from "./ui-evidence-schema";

const validManifest = {
  schemaVersion: 2,
  owner: "@notrelix/work-management-web",
  inventory: { excludedSources: [] },
  surfaces: [
    {
      surfaceId: "wm.kanban.board",
      owner: "@notrelix/work-management-web",
      surfaceKind: "data",
      pureEntry: "src/components/views/kanban/kanban-board.tsx",
      coveredSources: [],
      stories: [
        {
          id: "work-management-kanban-board--default",
          state: "Default",
          visualTargets: [{ viewport: "desktop", theme: "light" }],
        },
      ],
      stateCoverage: {
        required: ["Default"],
        delegated: [],
        notApplicable: [
          {
            state: "ReadOnly",
            reason: "No product-authoritative read-only contract exists.",
            authority: "docs/product/work-management.md",
          },
          {
            state: "Loading",
            reason: "Board-level loading is owned by wm.kanban.loading.",
            authority: "docs/product/work-management.md",
          },
          {
            state: "Error",
            reason:
              "Board-level fetch failure is owned by wm.kanban.unavailable.",
            authority: "docs/product/work-management.md",
          },
          {
            state: "HighDensity",
            reason: "High-density board input is evidenced at board level.",
            authority: "docs/product/work-management.md",
          },
          {
            state: "Empty",
            reason: "An empty board is a distinct board-level story.",
            authority: "docs/product/work-management.md",
          },
          {
            state: "EdgeData",
            reason: "Edge-value input is a distinct board-level story.",
            authority: "docs/product/work-management.md",
          },
          {
            state: "PermissionLimited",
            reason: "No permission-limited capability contract exists.",
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
            "src/components/views/kanban/kanban-board.interaction.component.test.tsx",
        },
      ],
    },
  ],
};

describe("validateUiEvidenceManifest", () => {
  it("accepts schemaVersion 2 manifests with owner-local paths", () => {
    expect(validateUiEvidenceManifest(validManifest).ok).toBe(true);
  });

  it("rejects schemaVersion 1", () => {
    const result = validateUiEvidenceManifest({
      schemaVersion: 1,
      surfaces: [],
    });

    expect(result.ok).toBe(false);
    expect(result.diagnostics).toContain("schemaVersion must be 2");
  });

  it("rejects unknown state/check, traversal, missing N/A authority and bad interaction paths", () => {
    const result = validateUiEvidenceManifest({
      schemaVersion: 2,
      owner: "@notrelix/work-management-web",
      inventory: { excludedSources: [] },
      surfaces: [
        {
          surfaceId: "bad",
          owner: "@notrelix/work-management-web",
          surfaceKind: "data",
          pureEntry: "../bad.tsx",
          coveredSources: [],
          stories: [{ id: "story", state: "Mystery", visualTargets: [] }],
          stateCoverage: {
            required: ["Default"],
            delegated: [],
            notApplicable: [{ state: "ReadOnly", reason: "" }],
          },
          responsive: false,
          themeAware: false,
          checks: ["network"],
          interactionCases: [{ id: "x", testFile: "bad.test.ts" }],
        },
      ],
    });

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain(
      "stories[0].state must be one of",
    );
    expect(result.diagnostics.join("\n")).toContain("checks[0] must be one of");
    expect(result.diagnostics.join("\n")).toContain("authority");
    expect(result.diagnostics.join("\n")).toContain("pureEntry");
  });

  it("rejects duplicate story ids and interaction check mismatches", () => {
    const manifest = structuredClone(validManifest);
    manifest.surfaces.push({
      ...structuredClone(validManifest.surfaces[0]),
      surfaceId: "wm.kanban.card",
      checks: ["a11y"],
    });

    const result = validateUiEvidenceManifest(manifest);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain("duplicate story id");
    expect(result.diagnostics.join("\n")).toContain(
      "interactionCases must be empty",
    );
  });

  it("rejects story states outside stateCoverage.required", () => {
    const manifest = structuredClone(validManifest);
    manifest.surfaces[0].stories.push({
      id: "work-management-kanban-board--empty",
      state: "Empty",
      visualTargets: [{ viewport: "desktop", theme: "light" }],
    });

    const result = validateUiEvidenceManifest(manifest);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain(
      "state Empty is not in stateCoverage.required",
    );
  });

  it("rejects generic exclusion reasons and visual policy violations", () => {
    const manifest = structuredClone(validManifest);
    manifest.inventory.excludedSources.push({
      path: "src/components/views/kanban/kanban-view.tsx",
      category: "container",
      reason: "covered elsewhere",
    });
    manifest.surfaces[0].stories[0].visualTargets = [];
    manifest.surfaces[0].responsive = true;

    const result = validateUiEvidenceManifest(manifest);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain("must be concrete");
    expect(result.diagnostics.join("\n")).toContain(
      "must declare visualTargets",
    );
  });

  it("enforces responsive Default targets only for responsive surfaces", () => {
    const responsiveManifest = structuredClone(validManifest);
    responsiveManifest.surfaces[0].responsive = true;
    responsiveManifest.surfaces[0].stories[0].visualTargets = [
      { viewport: "desktop", theme: "light" },
    ];

    const result = validateUiEvidenceManifest(responsiveManifest);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain("mobile/light");
    expect(result.diagnostics.join("\n")).toContain("tablet/light");
  });

  it("requires every surface-kind universe state to be accounted (TST-052)", () => {
    const manifest = structuredClone(validManifest);
    manifest.surfaces[0].stateCoverage.notApplicable =
      manifest.surfaces[0].stateCoverage.notApplicable.filter(
        (entry) => entry.state !== "Loading",
      );

    const result = validateUiEvidenceManifest(manifest);

    expect(result.diagnostics.join("\n")).toContain(
      "surfaceKind data state Loading must be required, delegated, or notApplicable",
    );
  });

  it("passes when a universe state is delegated to a valid sibling (TST-054)", () => {
    const manifest = structuredClone(validManifest);
    manifest.surfaces.push({
      ...structuredClone(validManifest.surfaces[0]),
      surfaceId: "wm.kanban.loading",
      surfaceKind: "feedback",
      stories: [],
      checks: ["purity"],
      interactionCases: [],
      stateCoverage: {
        required: ["Loading"],
        delegated: [],
        notApplicable: [
          {
            state: "Default",
            reason: "Loading-only presentation; Default is owned by the board.",
            authority: "docs/product/work-management.md",
          },
        ],
      },
    });
    manifest.surfaces[0].stateCoverage.notApplicable =
      manifest.surfaces[0].stateCoverage.notApplicable.filter(
        (entry) => entry.state !== "Loading",
      );
    manifest.surfaces[0].stateCoverage.delegated.push({
      state: "Loading",
      surfaceId: "wm.kanban.loading",
      targetState: "Loading",
    });

    const result = validateUiEvidenceManifest(manifest);

    expect(result.ok).toBe(true);
  });

  it("rejects delegation to a missing/cross-owner surface or absent target state (TST-054)", () => {
    const missingSurface = structuredClone(validManifest);
    missingSurface.surfaces[0].stateCoverage.delegated.push({
      state: "Loading",
      surfaceId: "wm.does.not.exist",
      targetState: "Loading",
    });
    expect(
      validateUiEvidenceManifest(missingSurface).diagnostics.join("\n"),
    ).toContain("missing or cross-owner surface");

    const absentTarget = structuredClone(validManifest);
    absentTarget.surfaces[0].stateCoverage.notApplicable =
      absentTarget.surfaces[0].stateCoverage.notApplicable.filter(
        (entry) => entry.state !== "Loading",
      );
    absentTarget.surfaces[0].stateCoverage.delegated.push({
      state: "Loading",
      surfaceId: "wm.kanban.loading",
      targetState: "Loading",
    });
    absentTarget.surfaces.push({
      ...structuredClone(validManifest.surfaces[0]),
      surfaceId: "wm.kanban.loading",
      surfaceKind: "feedback",
      stateCoverage: {
        required: ["Loading"],
        delegated: [],
        notApplicable: [],
      },
    });
    absentTarget.surfaces[0].stateCoverage.delegated.push({
      state: "Error",
      surfaceId: "wm.kanban.loading",
      targetState: "Error",
    });
    const absentResult = validateUiEvidenceManifest(absentTarget);
    expect(absentResult.diagnostics.join("\n")).toContain(
      "state Error which is not required by the target",
    );
  });

  it("rejects N/A entries with generic reasons (TST-055)", () => {
    const manifest = structuredClone(validManifest);
    manifest.surfaces[0].stateCoverage.notApplicable.push({
      state: "Loading",
      reason: "covered elsewhere",
      authority: "docs/product/work-management.md",
    });

    const result = validateUiEvidenceManifest(manifest);

    expect(result.diagnostics.join("\n")).toContain(
      "notApplicable Loading reason must be concrete",
    );
  });

  it("rejects a state accounted in two buckets", () => {
    const manifest = structuredClone(validManifest);
    manifest.surfaces[0].stateCoverage.notApplicable.push({
      state: "Default",
      reason: "Already the Default story.",
      authority: "docs/product/work-management.md",
    });

    const result = validateUiEvidenceManifest(manifest);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain(
      "state Default is both required and notApplicable",
    );
  });
});
