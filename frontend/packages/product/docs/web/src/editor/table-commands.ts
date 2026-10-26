import type { Editor } from "@tiptap/react";
import type { HighlightToken } from "@notrelix/docs-core";
import { newBlockId } from "./block-mapping";

/**
 * Inserts a table in place of the empty block under the caret, with a
 * paragraph after it so the page can be continued below.
 */
export function insertTableBlock(editor: Editor, rows = 3, cols = 3): boolean {
  const done = editor
    .chain()
    .focus()
    .insertTable({ rows, cols, withHeaderRow: true })
    .run();
  if (!done) return false;
  const caret = editor.state.selection.from;
  const { doc } = editor.state;
  if (doc.lastChild?.type.name === "table") {
    editor
      .chain()
      .insertContentAt(doc.content.size, {
        type: "paragraph",
        attrs: { blockId: newBlockId() },
      })
      .setTextSelection(caret)
      .run();
  }
  return true;
}

export const TABLE_COMMANDS = {
  rowBefore: (editor: Editor) => editor.chain().focus().addRowBefore().run(),
  rowAfter: (editor: Editor) => editor.chain().focus().addRowAfter().run(),
  deleteRow: (editor: Editor) => editor.chain().focus().deleteRow().run(),
  columnBefore: (editor: Editor) =>
    editor.chain().focus().addColumnBefore().run(),
  columnAfter: (editor: Editor) =>
    editor.chain().focus().addColumnAfter().run(),
  deleteColumn: (editor: Editor) => editor.chain().focus().deleteColumn().run(),
  toggleHeaderRow: (editor: Editor) =>
    editor.chain().focus().toggleHeaderRow().run(),
  deleteTable: (editor: Editor) => editor.chain().focus().deleteTable().run(),
} as const;

/** Sets the background of the selected cells (`none` clears it). */
export function setCellBackground(
  editor: Editor,
  token: HighlightToken,
): boolean {
  return editor
    .chain()
    .focus()
    .setCellAttribute("bg", token === "none" ? null : token)
    .run();
}

export interface TableState {
  readonly active: boolean;
  readonly headerRow: boolean;
  readonly bg: HighlightToken | null;
  /** Document position of the table, for placing the menu. */
  readonly pos: number | null;
}

const OUTSIDE: TableState = {
  active: false,
  headerRow: false,
  bg: null,
  pos: null,
};

export function selectTableState(editor: Editor | null): TableState {
  if (!editor || editor.isDestroyed || !editor.isEditable) return OUTSIDE;
  const { $from } = editor.state.selection;
  for (let depth = $from.depth; depth > 0; depth -= 1) {
    const node = $from.node(depth);
    if (node.type.name !== "table") continue;
    const cell = $from.node(depth + 2);
    const header = node.firstChild?.firstChild?.type.name === "tableHeader";
    const bg = (cell?.attrs?.bg ?? null) as HighlightToken | null;
    return { active: true, headerRow: header, bg, pos: $from.before(depth) };
  }
  return OUTSIDE;
}
