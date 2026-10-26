import { beforeAll, describe, expect, it, vi } from "vitest";
import type { Editor } from "@tiptap/core";
import { TextSelection } from "@tiptap/pm/state";
import { CellSelection } from "@tiptap/pm/tables";
import { docToDrafts, draftsToDoc, nodeToTable } from "../block-mapping";
import { insertBlockAfterCaret } from "../block-insert";
import {
  canIndent,
  deleteCurrentBlock,
  indentBlocks,
  setAlign,
  setLineSpacing,
  toggleScript,
} from "../editor-commands";
import { selectToolbarState } from "../editor-state";
import { TABLE_COMMANDS, insertTableBlock } from "../table-commands";
import { headlessEditor, paragraph, stubLayout } from "./headless-editor";

beforeAll(stubLayout);

function press(editor: Editor, key: string, init: KeyboardEventInit = {}) {
  const event = new KeyboardEvent("keydown", {
    key,
    bubbles: true,
    cancelable: true,
    ...init,
  });
  return Boolean(
    editor.view.someProp("handleKeyDown", (handler) =>
      handler(editor.view, event),
    ),
  );
}

const drafts = (editor: Editor) => docToDrafts(editor.getJSON());

function select(editor: Editor, from: number, to = from) {
  editor.view.dispatch(
    editor.state.tr.setSelection(
      TextSelection.create(editor.state.doc, from, to),
    ),
  );
}

describe("superscript and subscript", () => {
  it("mark the selected text and exclude each other", () => {
    const editor = headlessEditor([paragraph("a", "H2O")]);
    select(editor, 2, 3);
    expect(toggleScript(editor, "subscript")).toBe(true);
    expect(selectToolbarState(editor).subscript).toBe(true);
    expect(drafts(editor)[0]?.content.runs).toEqual([
      { text: "H", marks: [] },
      { text: "2", marks: [{ type: "subscript" }] },
      { text: "O", marks: [] },
    ]);
    // Raising the same text takes the lowering off.
    toggleScript(editor, "superscript");
    const marks = drafts(editor)[0]?.content.runs[1]?.marks;
    expect(marks).toEqual([{ type: "superscript" }]);
    toggleScript(editor, "superscript");
    expect(drafts(editor)[0]?.content.runs).toEqual([
      { text: "H2O", marks: [] },
    ]);
  });

  it("have keyboard shortcuts", () => {
    const editor = headlessEditor([paragraph("a", "x2")]);
    select(editor, 2, 3);
    expect(press(editor, ".", { ctrlKey: true })).toBe(true);
    expect(selectToolbarState(editor).superscript).toBe(true);
    expect(press(editor, ",", { ctrlKey: true })).toBe(true);
    expect(selectToolbarState(editor).subscript).toBe(true);
    expect(selectToolbarState(editor).superscript).toBe(false);
  });
});

describe("indent and line spacing", () => {
  it("indents the block under the caret, up to eight steps, and saves it", () => {
    const editor = headlessEditor([paragraph("a", "Item")]);
    select(editor, 2);
    expect(canIndent(editor, -1)).toBe(false);
    expect(indentBlocks(editor, 1)).toBe(true);
    expect(drafts(editor)[0]?.properties.indent).toBe(1);
    for (let step = 0; step < 10; step += 1) indentBlocks(editor, 1);
    expect(drafts(editor)[0]?.properties.indent).toBe(8);
    expect(canIndent(editor, 1)).toBe(false);
    for (let step = 0; step < 10; step += 1) indentBlocks(editor, -1);
    expect(drafts(editor)[0]?.properties.indent).toBeUndefined();
  });

  it("indents every block of a selection", () => {
    const editor = headlessEditor([
      paragraph("a", "One"),
      paragraph("b", "Two"),
    ]);
    select(editor, 2, 8);
    indentBlocks(editor, 1);
    expect(drafts(editor).map((draft) => draft.properties.indent)).toEqual([
      1, 1,
    ]);
  });

  it("uses Mod+] and Mod+[, and Tab inside a list item only", () => {
    const editor = headlessEditor([
      {
        id: "l",
        type: "bulleted_list",
        content: { v: 1, runs: [{ text: "Item", marks: [] }] },
        properties: {},
      },
      paragraph("p", "Plain"),
    ]);
    select(editor, 2);
    expect(press(editor, "Tab")).toBe(true);
    expect(drafts(editor)[0]?.properties.indent).toBe(1);
    expect(press(editor, "Tab", { shiftKey: true })).toBe(true);
    expect(drafts(editor)[0]?.properties.indent).toBeUndefined();
    expect(press(editor, "]", { ctrlKey: true })).toBe(true);
    expect(press(editor, "]", { ctrlKey: true })).toBe(true);
    expect(drafts(editor)[0]?.properties.indent).toBe(2);
    expect(press(editor, "[", { ctrlKey: true })).toBe(true);
    expect(drafts(editor)[0]?.properties.indent).toBe(1);
    // In a plain paragraph Tab stays the browser's (moves focus on).
    select(editor, 10);
    expect(press(editor, "Tab")).toBe(false);
  });

  it("numbers a deeper item from 1 again", () => {
    const numbered = (id: string, indent?: number) => ({
      id,
      type: "numbered_list" as const,
      content: { v: 1 as const, runs: [{ text: id, marks: [] }] },
      properties: indent ? { indent } : {},
    });
    const editor = headlessEditor([
      numbered("a"),
      numbered("b"),
      numbered("c", 1),
      numbered("d", 1),
      numbered("e"),
    ]);
    const nums = Array.from(
      editor.view.dom.querySelectorAll<HTMLElement>("[data-num]"),
    ).map((node) => node.dataset.num);
    expect(nums).toEqual(["1.", "2.", "1.", "2.", "3."]);
  });

  it("sets and clears the line spacing", () => {
    const editor = headlessEditor([paragraph("a", "Spaced")]);
    select(editor, 2);
    expect(setLineSpacing(editor, "relaxed")).toBe(true);
    expect(drafts(editor)[0]?.properties.lineSpacing).toBe("relaxed");
    expect(selectToolbarState(editor).lineSpacing).toBe("relaxed");
    expect(editor.view.dom.querySelector("p")?.className).toContain(
      "leading-relaxed",
    );
    setLineSpacing(editor, "normal");
    expect(drafts(editor)[0]?.properties.lineSpacing).toBeUndefined();
  });
});

describe("keyboard formatting", () => {
  it("aligns with Mod+Shift+E / R / J / L", () => {
    const editor = headlessEditor([paragraph("a", "Aligned")]);
    select(editor, 2);
    expect(press(editor, "e", { ctrlKey: true, shiftKey: true })).toBe(true);
    expect(drafts(editor)[0]?.properties.align).toBe("center");
    press(editor, "r", { ctrlKey: true, shiftKey: true });
    expect(drafts(editor)[0]?.properties.align).toBe("right");
    press(editor, "j", { ctrlKey: true, shiftKey: true });
    expect(drafts(editor)[0]?.properties.align).toBe("justify");
    press(editor, "l", { ctrlKey: true, shiftKey: true });
    expect(drafts(editor)[0]?.properties.align).toBeUndefined();
  });

  it("makes and leaves lists, quotes and code blocks", () => {
    const editor = headlessEditor([paragraph("a", "Line")]);
    select(editor, 2);
    press(editor, "8", { ctrlKey: true, shiftKey: true });
    expect(drafts(editor)[0]?.type).toBe("bulleted_list");
    press(editor, "8", { ctrlKey: true, shiftKey: true });
    expect(drafts(editor)[0]?.type).toBe("paragraph");
    press(editor, "7", { ctrlKey: true, shiftKey: true });
    expect(drafts(editor)[0]?.type).toBe("numbered_list");
    press(editor, "9", { ctrlKey: true, shiftKey: true });
    expect(drafts(editor)[0]?.type).toBe("todo");
    press(editor, "b", { ctrlKey: true, shiftKey: true });
    expect(drafts(editor)[0]?.type).toBe("quote");
    press(editor, "0", { ctrlKey: true, altKey: true });
    expect(drafts(editor)[0]?.type).toBe("paragraph");
    // The block keeps its id through every change.
    expect(drafts(editor)[0]?.id).toBe("a");
  });

  it("clears marks and alignment with Mod+\\", () => {
    const editor = headlessEditor([paragraph("a", "Styled")]);
    select(editor, 1, 7);
    editor.chain().toggleBold().run();
    setAlign(editor, "center");
    expect(press(editor, "\\", { ctrlKey: true })).toBe(true);
    expect(drafts(editor)[0]?.content.runs[0]?.marks).toEqual([]);
    expect(drafts(editor)[0]?.properties.align).toBeUndefined();
  });

  it("asks the page to open the link form, the comment box and the sheets", () => {
    const onEditorCommand = vi.fn();
    const editor = headlessEditor([paragraph("a", "Text")], {
      onEditorCommand,
    });
    select(editor, 1, 3);
    press(editor, "k", { ctrlKey: true });
    press(editor, "m", { ctrlKey: true, altKey: true });
    press(editor, "/", { ctrlKey: true });
    press(editor, "c", { ctrlKey: true, shiftKey: true });
    press(editor, "f", { ctrlKey: true, shiftKey: true });
    expect(onEditorCommand.mock.calls.map((call) => call[0])).toEqual([
      "link",
      "comment",
      "shortcuts",
      "wordCount",
      "focusMode",
    ]);
  });
});

describe("inserting blocks from the menu", () => {
  it("puts a new block after a block that has text, or in an empty one", () => {
    const editor = headlessEditor([paragraph("a", "Text"), paragraph("b")]);
    select(editor, 2);
    expect(insertBlockAfterCaret(editor, "callout")).toBe(true);
    expect(drafts(editor).map((draft) => draft.type)).toEqual([
      "paragraph",
      "callout",
      "paragraph",
    ]);
    // The caret is now in the empty callout: another insert reuses nothing new.
    expect(insertBlockAfterCaret(editor, "quote")).toBe(true);
    expect(drafts(editor).map((draft) => draft.type)).toEqual([
      "paragraph",
      "quote",
      "paragraph",
    ]);
  });

  it("opens the page chooser for a page link", () => {
    const onPickPage = vi.fn();
    const editor = headlessEditor([paragraph("a", "Text")]);
    select(editor, 2);
    insertBlockAfterCaret(editor, "page_reference", { onPickPage });
    expect(onPickPage).toHaveBeenCalledTimes(1);
    expect(drafts(editor).map((draft) => draft.type)).toEqual([
      "paragraph",
      "page_reference",
    ]);
  });

  it("does nothing in a read-only page", () => {
    const editor = headlessEditor([paragraph("a", "Text")]);
    editor.setEditable(false);
    expect(insertBlockAfterCaret(editor, "callout")).toBe(false);
    expect(drafts(editor)).toHaveLength(1);
  });

  it("deletes the block under the caret, and empties the last one", () => {
    const editor = headlessEditor([
      paragraph("a", "One"),
      paragraph("b", "Two"),
    ]);
    select(editor, 2);
    expect(deleteCurrentBlock(editor)).toBe(true);
    expect(drafts(editor).map((draft) => draft.id)).toEqual(["b"]);
    deleteCurrentBlock(editor);
    expect(drafts(editor)).toHaveLength(1);
    expect(drafts(editor)[0]?.type).toBe("paragraph");
    expect(drafts(editor)[0]?.content.runs).toEqual([]);
  });
});

describe("merging and splitting table cells", () => {
  function tableEditor() {
    const editor = headlessEditor([paragraph("a")]);
    editor.commands.focus("end");
    insertTableBlock(editor, 2, 3);
    return editor;
  }

  function cellPositions(editor: Editor) {
    const positions: number[] = [];
    editor.state.doc.descendants((node, pos) => {
      if (node.type.name === "tableCell" || node.type.name === "tableHeader") {
        positions.push(pos);
      }
    });
    return positions;
  }

  it("merges two cells into one that spans both, and splits them again", () => {
    const editor = tableEditor();
    const [first, second] = cellPositions(editor);
    editor.view.dispatch(
      editor.state.tr.setSelection(
        CellSelection.create(editor.state.doc, first!, second!),
      ),
    );
    expect(selectToolbarState(editor).canMerge).toBe(true);
    expect(TABLE_COMMANDS.merge(editor)).toBe(true);
    const merged = drafts(editor).find((draft) => draft.type === "table");
    expect(merged?.properties.table?.rows[0]).toHaveLength(2);
    expect(merged?.properties.table?.rows[0]?.[0]?.colSpan).toBe(2);
    expect(merged?.properties.table?.colWidths).toHaveLength(3);

    // The merged table survives a round trip through the stored shape.
    const again = docToDrafts(draftsToDoc(drafts(editor)));
    expect(
      again.find((draft) => draft.type === "table")?.properties.table
        ?.rows[0]?.[0]?.colSpan,
    ).toBe(2);

    expect(selectToolbarState(editor).canSplit).toBe(true);
    expect(TABLE_COMMANDS.split(editor)).toBe(true);
    const split = drafts(editor).find((draft) => draft.type === "table");
    expect(split?.properties.table?.rows[0]).toHaveLength(3);
    expect(split?.properties.table?.rows[0]?.[0]?.colSpan).toBeUndefined();
  });

  it("merges cells down a column (a row span) and keeps the grid whole", () => {
    const editor = tableEditor();
    const cells = cellPositions(editor);
    editor.view.dispatch(
      editor.state.tr.setSelection(
        CellSelection.create(editor.state.doc, cells[0]!, cells[3]!),
      ),
    );
    TABLE_COMMANDS.merge(editor);
    const table = drafts(editor).find((draft) => draft.type === "table")
      ?.properties.table;
    expect(table?.rows[0]?.[0]?.rowSpan).toBe(2);
    expect(table?.rows[0]?.[0]?.colSpan).toBeUndefined();
    expect(table?.rows[1]).toHaveLength(2);
    const node = nodeToTable(
      (editor.getJSON().content ?? []).find((item) => item.type === "table")!,
    );
    expect(node.colWidths).toHaveLength(3);
  });

  it("cannot merge a single cell", () => {
    const editor = tableEditor();
    expect(selectToolbarState(editor).canMerge).toBe(false);
  });
});
