import { Extension } from "@tiptap/core";
import type { BlockType } from "@notrelix/docs-core";
import type { ContextHolder } from "./extensions";
import {
  clearFormatting,
  indentBlocks,
  setAlign,
  setBlockType,
  toggleScript,
} from "./editor-commands";
import { SHORTCUTS, type ShortcutId } from "./shortcut-catalog";

/** Shortcuts that open something the page owns (a panel, a dialog, a mode). */
export type EditorCommandName =
  "link" | "comment" | "wordCount" | "shortcuts" | "focusMode";

const LIST_NODES: ReadonlySet<string> = new Set([
  "bulletedItem",
  "numberedItem",
  "todoItem",
]);

type Handler = (editor: import("@tiptap/core").Editor) => boolean;

function block(type: BlockType): Handler {
  return (editor) => {
    if (!editor.isEditable) return false;
    const current = editor.state.selection.$from.parent;
    if (current.type.name === "codeBlock" && type !== "code") return false;
    setBlockType(editor, type);
    return true;
  };
}

/** A list item goes back to text when its list is already the one asked for. */
function listBlock(type: "bulleted_list" | "numbered_list" | "todo"): Handler {
  const nodeName =
    type === "bulleted_list"
      ? "bulletedItem"
      : type === "numbered_list"
        ? "numberedItem"
        : "todoItem";
  return (editor) => {
    if (!editor.isEditable) return false;
    const inList = editor.state.selection.$from.parent.type.name === nodeName;
    setBlockType(editor, inList ? "paragraph" : type);
    return true;
  };
}

function mark(kind: "superscript" | "subscript"): Handler {
  return (editor) => {
    if (!editor.isEditable) return false;
    if (editor.state.selection.$from.parent.type.name === "codeBlock") {
      return false;
    }
    toggleScript(editor, kind);
    return true;
  };
}

/**
 * The shortcuts the editor itself does not bring (indent, alignment, lists,
 * scripts, clear formatting, link and the page-level ones). The keys come from
 * the shortcut catalog, so the menu and the sheet always show what is bound.
 */
export const DocKeys = Extension.create<{ context?: ContextHolder }>({
  name: "docKeys",
  addOptions() {
    return { context: undefined };
  },
  addKeyboardShortcuts() {
    const run = (name: EditorCommandName) => () => {
      const callback = this.options.context?.current.callbacks?.onEditorCommand;
      if (!callback) return false;
      callback(name);
      return true;
    };
    const handlers: Partial<Record<ShortcutId, Handler>> = {
      link: (editor) => {
        if (!editor.isEditable) return false;
        return run("link")();
      },
      clearFormat: (editor) => editor.isEditable && clearFormatting(editor),
      superscript: mark("superscript"),
      subscript: mark("subscript"),
      alignLeft: (editor) => editor.isEditable && setAlign(editor, "left"),
      alignCenter: (editor) => editor.isEditable && setAlign(editor, "center"),
      alignRight: (editor) => editor.isEditable && setAlign(editor, "right"),
      alignJustify: (editor) =>
        editor.isEditable && setAlign(editor, "justify"),
      paragraph: block("paragraph"),
      heading1: block("heading_1"),
      heading2: block("heading_2"),
      heading3: block("heading_3"),
      bulleted: listBlock("bulleted_list"),
      numbered: listBlock("numbered_list"),
      checklist: listBlock("todo"),
      quote: block("quote"),
      codeBlock: block("code"),
      indent: (editor) => {
        if (!editor.isEditable) return false;
        indentBlocks(editor, 1);
        return true;
      },
      outdent: (editor) => {
        if (!editor.isEditable) return false;
        indentBlocks(editor, -1);
        return true;
      },
      comment: () => run("comment")(),
      wordCount: () => run("wordCount")(),
      shortcuts: () => run("shortcuts")(),
      focusMode: () => run("focusMode")(),
    };
    const keys: Record<string, () => boolean> = {};
    for (const entry of SHORTCUTS) {
      const handler = handlers[entry.id];
      if (handler) keys[entry.keys] = () => handler(this.editor);
    }
    // Tab and Shift+Tab move a list item in and out, as in other editors.
    const inListItem = () =>
      LIST_NODES.has(this.editor.state.selection.$from.parent.type.name) &&
      this.editor.isEditable;
    keys.Tab = () =>
      inListItem() ? (indentBlocks(this.editor, 1), true) : false;
    keys["Shift-Tab"] = () =>
      inListItem() ? (indentBlocks(this.editor, -1), true) : false;
    return keys;
  },
});
