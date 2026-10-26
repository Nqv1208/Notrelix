import type { Editor } from "@tiptap/react";
import type { BlockType } from "@notrelix/docs-core";
import { blockTypeOfNode } from "./editor-commands";

/** What the toolbar and the bubble menu reflect, read from the editor state. */
export interface EditorToolbarState {
  readonly blockType: BlockType | null;
  readonly bold: boolean;
  readonly italic: boolean;
  readonly underline: boolean;
  readonly strike: boolean;
  readonly code: boolean;
  readonly link: string | null;
  readonly color: string | null;
  readonly highlight: string | null;
  readonly align: "left" | "center" | "right" | "justify";
  readonly inCodeBlock: boolean;
  readonly canUndo: boolean;
  readonly canRedo: boolean;
  readonly hasSelection: boolean;
}

export const EMPTY_TOOLBAR_STATE: EditorToolbarState = {
  blockType: null,
  bold: false,
  italic: false,
  underline: false,
  strike: false,
  code: false,
  link: null,
  color: null,
  highlight: null,
  align: "left",
  inCodeBlock: false,
  canUndo: false,
  canRedo: false,
  hasSelection: false,
};

export function selectToolbarState(editor: Editor | null): EditorToolbarState {
  if (!editor || editor.isDestroyed) return EMPTY_TOOLBAR_STATE;
  const { selection } = editor.state;
  const parent = selection.$from.parent;
  const align = parent.attrs.align as EditorToolbarState["align"] | null;
  const inCodeBlock = parent.type.name === "codeBlock";
  return {
    blockType: blockTypeOfNode(parent.type.name, parent.attrs),
    bold: editor.isActive("bold"),
    italic: editor.isActive("italic"),
    underline: editor.isActive("underline"),
    strike: editor.isActive("strike"),
    code: editor.isActive("code"),
    link: (editor.getAttributes("link").href as string | undefined) ?? null,
    color:
      (editor.getAttributes("colorToken").token as string | undefined) ?? null,
    highlight:
      (editor.getAttributes("highlightToken").token as string | undefined) ??
      null,
    align: align ?? "left",
    inCodeBlock,
    canUndo: editor.can().undo(),
    canRedo: editor.can().redo(),
    hasSelection: !selection.empty,
  };
}
