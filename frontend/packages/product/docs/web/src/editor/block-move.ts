import { Extension, type Editor } from "@tiptap/core";
import {
  NodeSelection,
  Selection,
  TextSelection,
  type Transaction,
} from "@tiptap/pm/state";

/** Transaction meta set when a block was moved: where it went. */
export const BLOCK_MOVE_META = "docBlockMove";

export interface BlockMoveInfo {
  readonly index: number;
  readonly total: number;
}

/** The index of the top-level block that holds `pos`. */
export function blockIndexAt(editor: Editor, pos: number): number {
  const { doc } = editor.state;
  const $pos = doc.resolve(Math.min(Math.max(pos, 0), doc.content.size));
  return Math.min($pos.index(0), doc.childCount - 1);
}

/**
 * Moves one top-level block to another index. The block keeps its node (and
 * so its block id), the caret keeps its place inside it, and the page's saver
 * sees the new order as a move of this one block.
 */
export function moveBlock(editor: Editor, from: number, to: number): boolean {
  const { state } = editor;
  const { doc } = state;
  if (
    !editor.isEditable ||
    from === to ||
    from < 0 ||
    to < 0 ||
    from >= doc.childCount ||
    to >= doc.childCount
  ) {
    return false;
  }
  let start = 0;
  for (let index = 0; index < from; index += 1) {
    start += doc.child(index).nodeSize;
  }
  const node = doc.child(from);
  const offset = state.selection.from - (start + 1);
  const inside = offset >= 0 && state.selection.from <= start + node.nodeSize;
  const tr = state.tr.delete(start, start + node.nodeSize);
  let at = 0;
  for (let index = 0; index < to; index += 1) {
    at += tr.doc.child(index).nodeSize;
  }
  tr.insert(at, node);
  restoreCaret(tr, at, node.content.size, inside ? offset : 0);
  tr.setMeta(BLOCK_MOVE_META, {
    index: to,
    total: doc.childCount,
  } satisfies BlockMoveInfo);
  editor.view.dispatch(tr.scrollIntoView());
  return true;
}

function restoreCaret(
  tr: Transaction,
  at: number,
  size: number,
  offset: number,
): void {
  const inner = at + 1 + Math.min(Math.max(offset, 0), size);
  try {
    const $pos = tr.doc.resolve(inner);
    tr.setSelection(
      $pos.parent.inlineContent
        ? TextSelection.create(tr.doc, inner)
        : Selection.near($pos, 1),
    );
  } catch {
    tr.setSelection(Selection.near(tr.doc.resolve(at + 1), 1));
  }
}

/**
 * ProseMirror reads a click's caret on the browser's `selectionchange` event,
 * which a busy page delivers after the next key. A key right after a click
 * would then move the block of the old caret, so the caret is read from the
 * page here first.
 */
function syncCaretFromDom(editor: Editor): void {
  const { view, state } = editor;
  if (state.selection instanceof NodeSelection || !view.hasFocus()) return;
  const dom = view.dom.ownerDocument.getSelection();
  if (!dom?.anchorNode || !view.dom.contains(dom.anchorNode)) return;
  try {
    const pos = view.posAtDOM(dom.anchorNode, dom.anchorOffset);
    if (pos === state.selection.from && state.selection.empty) return;
    view.dispatch(
      state.tr.setSelection(TextSelection.near(state.doc.resolve(pos))),
    );
  } catch {
    // The page's selection is outside the text: the editor's own caret stands.
  }
}

/** Moves the block holding the caret one place up (-1) or down (1). */
export function moveBlockBy(editor: Editor, direction: 1 | -1): boolean {
  syncCaretFromDom(editor);
  const from = blockIndexAt(editor, editor.state.selection.from);
  return moveBlock(editor, from, from + direction);
}

/** Alt+Shift+Arrow Up / Down: the keyboard way to reorder blocks. */
export const BlockMove = Extension.create({
  name: "blockMove",
  addKeyboardShortcuts() {
    return {
      "Alt-Shift-ArrowUp": () => moveBlockBy(this.editor, -1),
      "Alt-Shift-ArrowDown": () => moveBlockBy(this.editor, 1),
    };
  },
});
