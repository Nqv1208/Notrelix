import { describe, expect, it } from "vitest";
import { validateUiEvidenceManifest } from "./ui-evidence-schema";

const validManifest = {
  schemaVersion: 1,
  surfaces: [
    {
      surfaceId: "wm.kanban.board",
      owner: "@notrelix/work-management-web",
      pureEntry: "src/components/views/kanban/kanban-board.tsx",
      stories: [
        { id: "work-management-kanban-board--default", state: "Default" },
      ],
      requiredStates: ["Default"],
      checks: ["interaction", "a11y", "visual", "purity"],
      interactionTests: [
        "src/components/views/kanban/kanban-board.component.test.tsx",
      ],
      notApplicableStates: [
        {
          state: "ReadOnly",
          reason: "No product-authoritative read-only contract exists.",
          authority: "docs/product/work-management.md",
        },
      ],
    },
  ],
};

describe("validateUiEvidenceManifest", () => {
  it("accepts schemaVersion 1 manifests with owner-local interaction tests", () => {
    expect(validateUiEvidenceManifest(validManifest).ok).toBe(true);
  });

  it("rejects unknown state/check, missing N/A authority and bad interaction paths", () => {
    const result = validateUiEvidenceManifest({
      schemaVersion: 1,
      surfaces: [
        {
          surfaceId: "bad",
          owner: "@notrelix/work-management-web",
          pureEntry: "../bad.tsx",
          stories: [{ id: "story", state: "Mystery" }],
          requiredStates: ["Default"],
          checks: ["network"],
          interactionTests: ["bad.test.ts"],
          notApplicableStates: [{ state: "ReadOnly", reason: "" }],
        },
      ],
    });

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain("Mystery");
    expect(result.diagnostics.join("\n")).toContain("network");
    expect(result.diagnostics.join("\n")).toContain("authority");
    expect(result.diagnostics.join("\n")).toContain("pureEntry");
  });

  it("rejects duplicate story ids and interaction check mismatches", () => {
    const manifest = structuredClone(validManifest);
    manifest.surfaces.push({
      ...structuredClone(validManifest.surfaces[0]),
      surfaceId: "wm.kanban.card",
    });
    manifest.surfaces[1].checks = ["a11y"];

    const result = validateUiEvidenceManifest(manifest);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain("duplicate story id");
    expect(result.diagnostics.join("\n")).toContain(
      "interactionTests must be empty",
    );
  });
});
