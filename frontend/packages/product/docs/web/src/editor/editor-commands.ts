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
