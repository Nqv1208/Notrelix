import { mkdirSync, mkdtempSync, symlinkSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { tmpdir } from "node:os";
import { describe, expect, it } from "vitest";
import { enumerateGovernedSources } from "./governed-source-enumerator";

function createOwner(files: Record<string, string>) {
  const root = mkdtempSync(join(tmpdir(), "notrelix-governed-"));
  for (const [filePath, content] of Object.entries(files)) {
    const full = join(root, filePath);
    mkdirSync(join(full, ".."), { recursive: true });
    writeFileSync(full, content);
  }
  return root;
}

describe("enumerateGovernedSources", () => {
  it("enumerates only non-test, non-story .tsx sources under src, deterministically sorted", () => {
    const root = createOwner({
      "src/b/kanban-board.tsx": "export const K = () => null;\n",
      "src/a/table-main.tsx": "export const T = () => null;\n",
      "src/a/board.util.ts": "export const helper = 1;\n",
      "src/c/board-cell.tsx": "export const C = () => null;\n",
      "src/__tests__/kanban-board.component.test.tsx":
        "import {} from 'vitest';\n",
      "src/b/kanban-board.stories.tsx": "export const S = {};\n",
      "src/c/board-cell.spec.tsx": "export const S = {};\n",
      "src/verification/ui-web-critical-surfaces.tsx":
        "export const V = () => null;\n",
    });

    const result = enumerateGovernedSources(root);

    expect(result.diagnostics).toEqual([]);
    expect(result.sources).toEqual([
      "src/a/table-main.tsx",
      "src/b/kanban-board.tsx",
      "src/c/board-cell.tsx",
    ]);
  });

  it("returns empty sources when owner has no src directory", () => {
    const root = mkdtempSync(join(tmpdir(), "notrelix-governed-"));

    expect(enumerateGovernedSources(root)).toEqual({
      sources: [],
      diagnostics: [],
    });
  });

  it("reports symlinks that escape the owner root", () => {
    const outside = mkdtempSync(join(tmpdir(), "notrelix-outside-"));
    writeFileSync(join(outside, "leak.tsx"), "export const L = () => null;\n");
    const root = createOwner({
      "src/local.tsx": "export const L = () => null;\n",
    });
    symlinkSync(join(outside, "leak.tsx"), join(root, "src/leak.tsx"));

    const result = enumerateGovernedSources(root);

    expect(result.sources).toEqual(["src/local.tsx"]);
    expect(result.diagnostics).toEqual([
      "governed symlink escapes the owner root: src/leak.tsx",
    ]);
  });

  it("ignores dot-directories and structural directories", () => {
    const root = createOwner({
      "src/.hidden/hidden.tsx": "export const H = () => null;\n",
      "src/node_modules/react/index.tsx": "export const R = () => null;\n",
      "src/dist/bundle.tsx": "export const D = () => null;\n",
      "src/storybook-static/statics.tsx": "export const S = () => null;\n",
      "src/.turbo/cache.tsx": "export const T = () => null;\n",
    });

    expect(enumerateGovernedSources(root).sources).toEqual([]);
  });
});
