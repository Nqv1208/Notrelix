import { mkdirSync, writeFileSync } from "node:fs";
import { mkdtempSync } from "node:fs";
import { join } from "node:path";
import { tmpdir } from "node:os";
import { describe, expect, it } from "vitest";
import { checkUiEvidence } from "./check-ui-evidence";

function writeJson(path: string, value: unknown) {
  writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`);
}

function createFixture() {
  const root = mkdtempSync(join(tmpdir(), "notrelix-ui-evidence-"));
  const ownerRoot = join(root, "packages/product/work-management/web");
  const verificationRoot = join(ownerRoot, "verification");
  const storybookRoot = join(root, "tooling/storybook/web/storybook-static");
  mkdirSync(verificationRoot, { recursive: true });
  mkdirSync(join(ownerRoot, "src"), { recursive: true });
  mkdirSync(join(ownerRoot, "src/__tests__"), { recursive: true });
  mkdirSync(storybookRoot, { recursive: true });
  writeFileSync(
    join(ownerRoot, "src/kanban-board.tsx"),
    "export const KanbanBoard = () => null;\n",
  );
  writeFileSync(
    join(ownerRoot, "src/__tests__/kanban-board.component.test.tsx"),
    "import { it } from 'vitest'; it('works', () => {});\n",
  );
  return {
    root,
    manifestPath: join(verificationRoot, "ui-evidence.manifest.json"),
    indexPath: join(storybookRoot, "index.json"),
  };
}

describe("checkUiEvidence", () => {
  it("binds schemaVersion 1 manifests to collected Storybook index entries", () => {
    const fixture = createFixture();
    writeJson(fixture.manifestPath, {
      schemaVersion: 1,
      surfaces: [
        {
          surfaceId: "work-management.kanban-board",
          owner: "@notrelix/work-management-web",
          pureEntry: "src/kanban-board.tsx",
          stories: [
            { id: "work-management-kanban-board--default", state: "Default" },
          ],
          requiredStates: ["Default"],
          checks: ["interaction", "a11y", "visual", "purity"],
          interactionTests: ["src/__tests__/kanban-board.component.test.tsx"],
          notApplicableStates: [],
        },
      ],
    });
    writeJson(fixture.indexPath, {
      entries: {
        "work-management-kanban-board--default": {
          id: "work-management-kanban-board--default",
          tags: [
            "fui-surface--work-management.kanban-board",
            "fui-state--Default",
          ],
        },
      },
    });

    expect(checkUiEvidence(fixture.root, fixture.indexPath)).toEqual({
      ok: true,
      diagnostics: [],
      surfaceCount: 1,
      requiredStateCount: 1,
    });
  });

  it("fails closed for schema drift and missing Storybook bindings", () => {
    const fixture = createFixture();
    writeJson(fixture.manifestPath, {
      schemaVersion: 2,
      surfaces: [],
    });
    writeJson(fixture.indexPath, { entries: {} });

    const result = checkUiEvidence(fixture.root, fixture.indexPath);

    expect(result.ok).toBe(false);
    expect(result.diagnostics.join("\n")).toContain("schemaVersion must be 1");
    expect(result.diagnostics.join("\n")).toContain(
      "no schemaVersion 1 UI evidence manifests",
    );
  });
});
