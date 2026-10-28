import { beforeAll, describe, expect, it } from "vitest";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import type { Editor } from "@tiptap/core";
import { docToDrafts } from "../block-mapping";
import { collapsedRuns, toggleCollapseAtSelection } from "../toggle-collapse";
import { headlessEditor, paragraph, stubLayout } from "./headless-editor";

beforeAll(stubLayout);

const toggle = (id: string, collapsed: boolean, indent = 0): BlockDraft => ({
  id,
  type: "toggle",
  content: richText([{ text: id, marks: [] }]),
  properties: { collapsed, ...(indent ? { indent } : {}) },
});

const child = (id: string, indent: number): BlockDraft => ({
  ...paragraph(id, id),
  properties: { indent },
});

const pageOf = (collapsed: boolean): BlockDraft[] => [
  paragraph("intro", "intro"),
  toggle("t", collapsed),
  child("c1", 1),
  child("c2", 2),
  paragraph("after", "after"),
];

const hiddenIds = (editor: Editor): string[] =>
  Array.from(
    editor.view.dom.querySelectorAll<HTMLElement>(
      '[data-collapsed-child="true"]',
    ),
  ).map((el) => el.getAttribute("data-block") ?? "");

describe("toggle collapse", () => {
  it("hides the deeper-indented blocks after a collapsed toggle and nothing else", () => {
    const editor = headlessEditor(pageOf(true));
    const runs = collapsedRuns(editor.state.doc);
    expect(runs).toHaveLength(1);
    expect(runs[0]!.children).toHaveLength(2);
    expect(hiddenIds(editor)).toEqual(["c1", "c2"]);
    expect(
      editor.view.dom
        .querySelector('[data-doc-node="toggle"]')
        ?.getAttribute("aria-expanded"),
    ).toBe("false");
  });

  it("shows every child of an expanded toggle", () => {
    const editor = headlessEditor(pageOf(false));
    expect(collapsedRuns(editor.state.doc)).toHaveLength(0);
    expect(hiddenIds(editor)).toEqual([]);
  });

  it("collapses and expands from the caret and keeps children and order in the saved blocks", () => {
    const editor = headlessEditor(pageOf(false));
    editor.commands.setTextSelection(editor.state.doc.child(0).nodeSize + 2);
    expect(toggleCollapseAtSelection(editor.view)).toBe(true);
    expect(hiddenIds(editor)).toEqual(["c1", "c2"]);
    const saved = docToDrafts(editor.getJSON());
    expect(saved.map((d) => d.id)).toEqual(["intro", "t", "c1", "c2", "after"]);
    expect(saved[1]!.properties.collapsed).toBe(true);
    expect(saved[2]!.properties.indent).toBe(1);
    expect(toggleCollapseAtSelection(editor.view)).toBe(true);
    expect(hiddenIds(editor)).toEqual([]);
    expect(docToDrafts(editor.getJSON())[1]!.properties.collapsed).toBeFalsy();
  });

  it("does nothing when the caret is not in a toggle", () => {
    const editor = headlessEditor(pageOf(true));
    editor.commands.setTextSelection(2);
    expect(toggleCollapseAtSelection(editor.view)).toBe(false);
  });

  it("scopes a nested toggle's children by its own indent", () => {
    const editor = headlessEditor([
      toggle("outer", true),
      toggle("inner", false, 1),
      child("deep", 2),
      paragraph("end", "end"),
    ]);
    const runs = collapsedRuns(editor.state.doc);
    expect(runs).toHaveLength(1);
    expect(hiddenIds(editor)).toEqual(["inner", "deep"]);
  });
});
