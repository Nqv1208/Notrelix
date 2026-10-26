import type { Editor } from "@tiptap/react";
import type { BlockType } from "@notrelix/docs-core";
import { setBlockType } from "./editor-commands";
import { pickMedia } from "./media-commands";
import { insertTableBlock } from "./table-commands";

export interface InsertHosts {
  /** Opens the page chooser for a page-reference block. */
  readonly onPickPage?: (blockId: string) => void;
  /** Opens the board chooser for a board-reference block. */
  readonly onPickBoard?: (blockId: string) => void;
}

/**
 * Puts the caret in an empty text block: the block it is in when that is
 * empty, otherwise a new paragraph right after the top-level block (after the
 * table or the image when the caret is inside one).
 */
export function startBlockHere(editor: Editor): boolean {
  const { selection } = editor.state;
  const { $to } = selection;
  if (
    $to.depth === 1 &&
    $to.parent.isTextblock &&
    $to.parent.type.name !== "codeBlock" &&
    $to.parent.content.size === 0
  ) {
    return true;
  }
  const at = $to.depth >= 1 ? $to.after(1) : selection.to;
  return editor
    .chain()
    .focus()
    .insertContentAt(at, { type: "paragraph" })
    .setTextSelection(at + 1)
    .run();
}

/**
 * Makes the block of `type` in the empty text block under the caret (the slash
 * menu has already removed its "/query"). References open their chooser.
 */
export function insertBlockType(
  editor: Editor,
  type: BlockType,
  hosts: InsertHosts = {},
): boolean {
  if (type === "page_reference" || type === "board_reference") {
    const board = type === "board_reference";
    const { $from } = editor.state.selection;
    const blockId = $from.parent.attrs.blockId as string | undefined;
    const pos = $from.before();
    editor
      .chain()
      .focus()
      .insertContentAt(
        { from: pos, to: pos + $from.parent.nodeSize },
        {
          type: board ? "boardReference" : "pageReference",
          attrs: board
            ? { blockId, linkedBoardId: null }
            : { blockId, linkedPageId: null },
        },
      )
      .run();
    if (blockId) (board ? hosts.onPickBoard : hosts.onPickPage)?.(blockId);
    return true;
  }
  if (type === "table_of_contents") {
    const { $from } = editor.state.selection;
    const blockId = $from.parent.attrs.blockId as string | undefined;
    const pos = $from.before();
    // The empty block becomes the outline; a paragraph after it keeps the caret out of an atom.
    return editor
      .chain()
      .focus()
      .insertContentAt({ from: pos, to: pos + $from.parent.nodeSize }, [
        { type: "tableOfContents", attrs: { blockId } },
        { type: "paragraph" },
      ])
      .run();
  }
  if (type === "table") return insertTableBlock(editor);
  if (type === "image" || type === "file") {
    void pickMedia(editor, type);
    return true;
  }
  return setBlockType(editor, type);
}

/** The menu bar's "Insert": a block after the current one (or in it when empty). */
export function insertBlockAfterCaret(
  editor: Editor,
  type: BlockType,
  hosts: InsertHosts = {},
): boolean {
  if (!editor.isEditable) return false;
  if (!startBlockHere(editor)) return false;
  return insertBlockType(editor, type, hosts);
}
