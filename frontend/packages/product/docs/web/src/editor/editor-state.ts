import type { Editor } from "@tiptap/react";
import type { BlockType } from "@notrelix/docs-core";
import { anchorFromSelection } from "./comment-anchors";
import { blockTypeOfNode, canIndent } from "./editor-commands";

/** What the toolbar and the bubble menu reflect, read from the editor state. */
export interface EditorToolbarState {
  readonly blockType: BlockType | null;
  readonly bold: boolean;
  readonly italic: boolean;
  readonly underline: boolean;
  readonly strike: boolean;
  readonly code: boolean;
  readonly superscript: boolean;
  readonly subscript: boolean;
  readonly indent: number;
  readonly lineSpacing: "tight" | "normal" | "relaxed" | "loose";
  readonly canIndent: boolean;
  readonly canOutdent: boolean;
  readonly canMerge: boolean;
  readonly canSplit: boolean;
  readonly link: string | null;
  readonly color: string | null;
  readonly highlight: string | null;
  readonly align: "left" | "center" | "right" | "justify";
  readonly inCodeBlock: boolean;
  readonly canUndo: boolean;
  readonly canRedo: boolean;
  readonly hasSelection: boolean;
  /** The selection is text of one block a comment can be anchored to. */
  readonly commentable: boolean;
}

export const EMPTY_TOOLBAR_STATE: EditorToolbarState = {
  blockType: null,
  bold: false,
  italic: false,
  underline: false,
  strike: false,
  code: false,
  superscript: false,
  subscript: false,
  indent: 0,
  lineSpacing: "normal",
  canIndent: false,
  canOutdent: false,
  canMerge: false,
  canSplit: false,
  link: null,
  color: null,
  highlight: null,
  align: "left",
  inCodeBlock: false,
  canUndo: false,
  canRedo: false,
  hasSelection: false,
  commentable: false,
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
    superscript: editor.isActive("superscript"),
    subscript: editor.isActive("subscript"),
    indent: Number(parent.attrs.indent) || 0,
    lineSpacing:
      (parent.attrs.lineSpacing as EditorToolbarState["lineSpacing"] | null) ??
      "normal",
    canIndent: canIndent(editor, 1),
    canOutdent: canIndent(editor, -1),
    canMerge: editor.can().mergeCells(),
    canSplit: editor.can().splitCell(),
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
    commentable: !selection.empty && anchorFromSelection(editor) !== null,
  };
}
