import type { Editor } from "@tiptap/react";
import {
  isSafeHref,
  type BlockType,
  type ColorToken,
  type HighlightToken,
} from "@notrelix/docs-core";

type Align = "left" | "center" | "right" | "justify";

const NODE_OF: Readonly<Record<string, { name: string; attrs?: object }>> = {
  paragraph: { name: "paragraph" },
  heading_1: { name: "heading", attrs: { level: 1 } },
  heading_2: { name: "heading", attrs: { level: 2 } },
  heading_3: { name: "heading", attrs: { level: 3 } },
  bulleted_list: { name: "bulletedItem" },
  numbered_list: { name: "numberedItem" },
  todo: { name: "todoItem", attrs: { checked: false } },
  quote: { name: "quote" },
  callout: { name: "callout" },
  code: { name: "codeBlock" },
  toggle: { name: "toggle" },
};

/** The block type a node stands for (the toolbar's paragraph style). */
export function blockTypeOfNode(
  name: string,
  attrs: Record<string, unknown>,
): BlockType | null {
  switch (name) {
    case "paragraph":
      return "paragraph";
    case "heading":
      return `heading_${Number(attrs.level) || 1}` as BlockType;
    case "bulletedItem":
      return "bulleted_list";
    case "numberedItem":
      return "numbered_list";
    case "todoItem":
      return "todo";
    case "quote":
      return "quote";
    case "callout":
      return "callout";
    case "codeBlock":
      return "code";
    case "toggle":
      return "toggle";
    case "horizontalRule":
      return "divider";
    default:
      return null;
  }
}

/**
 * Retypes the block holding the selection in place: it keeps its id, so the
 * save is one retype, not a delete and a create.
 */
export function setBlockType(editor: Editor, type: BlockType): boolean {
  if (type === "divider") return insertDivider(editor);
  const target = NODE_OF[type];
  if (!target) return false;
  const { $from } = editor.state.selection;
  const keep = {
    blockId: $from.parent.attrs.blockId ?? null,
    rest: $from.parent.attrs.rest ?? null,
  };
  return editor
    .chain()
    .focus()
    .setNode(target.name, { ...target.attrs, ...keep })
    .run();
}

export function insertDivider(editor: Editor): boolean {
  return editor.chain().focus().setHorizontalRule().run();
}

/** Aligns every text block the selection touches. */
export function setAlign(editor: Editor, align: Align): boolean {
  return editor
    .chain()
    .focus()
    .command(({ tr, state, dispatch }) => {
      const { from, to } = state.selection;
      state.doc.nodesBetween(from, to, (node, pos) => {
        if (!node.isTextblock || !("align" in node.type.spec.attrs!)) return;
        if (node.type.name === "codeBlock") return;
        tr.setNodeMarkup(pos, undefined, {
          ...node.attrs,
          align: align === "left" ? null : align,
        });
      });
      if (dispatch) dispatch(tr);
      return true;
    })
    .run();
}

export function setTextColor(editor: Editor, token: ColorToken): boolean {
  const chain = editor.chain().focus();
  return token === "default"
    ? chain.unsetMark("colorToken").run()
    : chain.setMark("colorToken", { token }).run();
}

export function setHighlight(editor: Editor, token: HighlightToken): boolean {
  const chain = editor.chain().focus();
  return token === "none"
    ? chain.unsetMark("highlightToken").run()
    : chain.setMark("highlightToken", { token }).run();
}

/** Sets or replaces the link on the selection; only http, https and mailto. */
export function applyLink(editor: Editor, href: string): boolean {
  const value = /^[a-z][a-z0-9+.-]*:/i.test(href.trim())
    ? href.trim()
    : `https://${href.trim()}`;
  if (!isSafeHref(value)) return false;
  return editor
    .chain()
    .focus()
    .extendMarkRange("link")
    .setLink({ href: value })
    .run();
}

export function removeLink(editor: Editor): boolean {
  return editor.chain().focus().extendMarkRange("link").unsetLink().run();
}

/** Removes marks and alignment from the selection. */
export function clearFormatting(editor: Editor): boolean {
  setAlign(editor, "left");
  return editor.chain().focus().unsetAllMarks().run();
}

export const MAX_INDENT = 8;

export type LineSpacing = "tight" | "normal" | "relaxed" | "loose";

/** Text blocks the selection touches (code blocks and atoms take neither indent nor spacing). */
function textBlocksIn(
  editor: Editor,
  visit: (
    node: import("@tiptap/pm/model").Node,
    pos: number,
  ) => import("@tiptap/pm/model").Node["attrs"] | null,
): boolean {
  const { from, to } = editor.state.selection;
  const { tr } = editor.state;
  let changed = false;
  editor.state.doc.nodesBetween(from, to, (node, pos) => {
    if (!node.isTextblock || node.type.name === "codeBlock") return;
    if (!("indent" in node.type.spec.attrs!)) return;
    const next = visit(node, pos);
    if (next) {
      tr.setNodeMarkup(pos, undefined, next);
      changed = true;
    }
  });
  if (changed) editor.view.dispatch(tr);
  return changed;
}

/** Moves every text block of the selection one indent step in or out. */
export function indentBlocks(editor: Editor, delta: 1 | -1): boolean {
  if (!editor.isEditable) return false;
  const done = textBlocksIn(editor, (node) => {
    const level = Number(node.attrs.indent) || 0;
    const next = Math.min(MAX_INDENT, Math.max(0, level + delta));
    return next === level ? null : { ...node.attrs, indent: next };
  });
  editor.view.focus();
  return done;
}

/** Whether an indent step in that direction would change a block. */
export function canIndent(editor: Editor | null, delta: 1 | -1): boolean {
  if (!editor || editor.isDestroyed || !editor.isEditable) return false;
  const { from, to } = editor.state.selection;
  let possible = false;
  editor.state.doc.nodesBetween(from, to, (node) => {
    if (!node.isTextblock || node.type.name === "codeBlock") return;
    if (!("indent" in node.type.spec.attrs!)) return;
    const level = Number(node.attrs.indent) || 0;
    if (delta > 0 ? level < MAX_INDENT : level > 0) possible = true;
  });
  return possible;
}

/** Sets the line spacing of every text block the selection touches. */
export function setLineSpacing(editor: Editor, spacing: LineSpacing): boolean {
  if (!editor.isEditable) return false;
  const done = textBlocksIn(editor, (node) => {
    const next = spacing === "normal" ? null : spacing;
    return (node.attrs.lineSpacing ?? null) === next
      ? null
      : { ...node.attrs, lineSpacing: next };
  });
  editor.view.focus();
  return done;
}

/** Raised or lowered text; the two exclude each other. */
export function toggleScript(
  editor: Editor,
  kind: "superscript" | "subscript",
): boolean {
  return editor.chain().focus().toggleMark(kind).run();
}

/** The date as the page's language writes it, inserted as plain text. */
export function insertToday(editor: Editor): boolean {
  const lang = document.documentElement.lang || undefined;
  let text: string;
  try {
    text = new Intl.DateTimeFormat(lang, { dateStyle: "medium" }).format(
      new Date(),
    );
  } catch {
    text = new Date().toISOString().slice(0, 10);
  }
  return editor.chain().focus().insertContent(text).run();
}

export function insertText(editor: Editor, text: string): boolean {
  return editor.chain().focus().insertContent(text).run();
}

/** Removes the top-level block holding the selection (the last one is emptied instead). */
export function deleteCurrentBlock(editor: Editor): boolean {
  if (!editor.isEditable) return false;
  const { selection, doc } = editor.state;
  const depth = selection.$from.depth;
  const from = depth >= 1 ? selection.$from.before(1) : selection.from;
  const node =
    depth >= 1 ? selection.$from.node(1) : doc.nodeAt(selection.from);
  if (!node) return false;
  if (doc.childCount <= 1) {
    return editor
      .chain()
      .focus()
      .insertContentAt(
        { from, to: from + node.nodeSize },
        { type: "paragraph" },
      )
      .run();
  }
  return editor
    .chain()
    .focus()
    .deleteRange({ from, to: from + node.nodeSize })
    .run();
}
