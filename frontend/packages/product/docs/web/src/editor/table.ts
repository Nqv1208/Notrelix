import {
  Table,
  TableCell,
  TableHeader,
  TableRow,
} from "@tiptap/extension-table";
import { HIGHLIGHT_TOKENS, type HighlightToken } from "@notrelix/docs-core";
import { HIGHLIGHT_CLASS } from "./mark-classes";

/**
 * Tables: a table is one block; a cell holds one paragraph of inline runs
 * (Shift+Enter or Enter makes a line break). Cells never merge or split, so
 * the table maps to rows of rich-text cells with a background token.
 */

/** The ProseMirror table styling, with the editor's tokens only. */
const TABLE_CLASS =
  "my-2 w-auto min-w-full table-fixed border-collapse text-doc-prose [&_.column-resize-handle]:pointer-events-none [&_.column-resize-handle]:absolute [&_.column-resize-handle]:top-0 [&_.column-resize-handle]:-right-px [&_.column-resize-handle]:bottom-0 [&_.column-resize-handle]:w-0.5 [&_.column-resize-handle]:bg-focus-ring [&_.selectedCell]:after:pointer-events-none [&_.selectedCell]:after:absolute [&_.selectedCell]:after:inset-0 [&_.selectedCell]:after:z-10 [&_.selectedCell]:after:bg-brand-subtle [&_.selectedCell]:after:opacity-60 [&_.selectedCell]:after:content-['']";
const CELL_CLASS =
  "relative box-border min-w-10 border border-line-strong px-2.5 py-1.5 text-left align-top text-doc-body [&>p]:mb-0 [&>p]:max-w-none";
const HEADER_CLASS = `${CELL_CLASS} bg-state-hover font-semibold`;

function bgAttribute() {
  return {
    bg: {
      default: null as HighlightToken | null,
      parseHTML: (element: HTMLElement) => {
        const token = element.getAttribute("data-bg");
        return (
          HIGHLIGHT_TOKENS.find((candidate) => candidate === token) ?? null
        );
      },
      renderHTML: (attrs: { bg?: HighlightToken | null }) =>
        attrs.bg && attrs.bg !== "none"
          ? { "data-bg": attrs.bg, class: HIGHLIGHT_CLASS[attrs.bg] }
          : {},
    },
  };
}

type CellContext = { editor: import("@tiptap/core").Editor };

/** Enter in a cell is a line break (a cell holds one paragraph). */
const cellKeys = {
  Enter: ({ editor }: CellContext) => {
    const { $from } = editor.state.selection;
    const inCell = ["tableCell", "tableHeader"].includes(
      $from.node(Math.max(0, $from.depth - 1)).type.name,
    );
    return inCell ? editor.commands.setHardBreak() : false;
  },
};

export const DocTable = Table.configure({
  resizable: true,
  cellMinWidth: 40,
  lastColumnResizable: true,
  allowTableNodeSelection: true,
  HTMLAttributes: { class: TABLE_CLASS },
});

export const DocTableRow = TableRow;

export const DocTableCell = TableCell.extend({
  content: "paragraph",
  addAttributes() {
    return { ...this.parent?.(), ...bgAttribute() };
  },
  addKeyboardShortcuts() {
    return cellKeys;
  },
}).configure({ HTMLAttributes: { class: CELL_CLASS } });

export const DocTableHeader = TableHeader.extend({
  content: "paragraph",
  addAttributes() {
    return { ...this.parent?.(), ...bgAttribute() };
  },
  addKeyboardShortcuts() {
    return cellKeys;
  },
}).configure({ HTMLAttributes: { class: HEADER_CLASS } });

export const TABLE_EXTENSIONS = [
  DocTable,
  DocTableRow,
  DocTableCell,
  DocTableHeader,
];
