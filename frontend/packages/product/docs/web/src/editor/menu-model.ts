import type { Editor } from "@tiptap/react";
import {
  COLOR_TOKENS,
  HIGHLIGHT_TOKENS,
  type BlockType,
} from "@notrelix/docs-core";
import { fillDocCopy, type DocPageCopy } from "../components/doc-page.copy";
import type {
  DocPageCallbacks,
  DocPanelTab,
} from "../components/doc-page-models";
import {
  insertBlockAfterCaret,
  startBlockHere,
  type InsertHosts,
} from "./block-insert";
import {
  clearFormatting,
  deleteCurrentBlock,
  indentBlocks,
  insertText,
  insertToday,
  setAlign,
  setBlockType,
  setHighlight,
  setLineSpacing,
  setTextColor,
  toggleScript,
  type LineSpacing,
} from "./editor-commands";
import type { EditorToolbarState } from "./editor-state";
import { COLOR_SWATCH, HIGHLIGHT_CLASS } from "./mark-classes";
import { mediaOf } from "./media-commands";
import type { ShortcutId } from "./shortcut-catalog";
import {
  TABLE_COMMANDS,
  insertTableBlock,
  type TableState,
} from "./table-commands";

/** One line of a menu. */
export type MenuEntry =
  | {
      readonly kind: "item";
      readonly id: string;
      readonly label: string;
      /** The catalog entry whose keys are shown beside the label. */
      readonly shortcut?: ShortcutId;
      /** A toggle: `true`/`false` shows its state, absent is a plain action. */
      readonly checked?: boolean;
      readonly disabled?: boolean;
      /** Why the item is off (shown as its tooltip). */
      readonly reason?: string;
      /** A literal class that paints a colour dot before the label. */
      readonly swatch?: string;
      /** A glyph shown in a grid cell (special characters, emoji). */
      readonly glyph?: string;
      readonly onSelect: () => void;
    }
  | { readonly kind: "separator" }
  | {
      readonly kind: "sub";
      readonly id: string;
      readonly label: string;
      readonly entries: readonly MenuEntry[];
      readonly disabled?: boolean;
      readonly reason?: string;
      /** The sub-entries are glyph cells in a grid, not rows. */
      readonly grid?: boolean;
    };

export type MenuKey = "edit" | "view" | "insert" | "format" | "tools";

export interface MenuViewState {
  readonly panelOpen: boolean;
  readonly panelTab: DocPanelTab;
  readonly fullWidth: boolean;
  readonly focusMode: boolean;
  readonly toolbarShown: boolean;
  readonly spellcheck: boolean;
}

export interface MenuContext {
  readonly editor: Editor;
  readonly copy: DocPageCopy;
  readonly state: EditorToolbarState;
  readonly table: TableState;
  readonly callbacks?: DocPageCallbacks;
  readonly view: MenuViewState;
  readonly canEdit: boolean;
  readonly hosts: InsertHosts;
  readonly onSpellcheck: (on: boolean) => void;
}

const SPECIAL_CHARS = [
  "—",
  "–",
  "…",
  "•",
  "·",
  "→",
  "←",
  "↑",
  "↓",
  "≤",
  "≥",
  "≠",
  "±",
  "×",
  "÷",
  "©",
  "®",
  "™",
  "°",
  "§",
  "¶",
  "€",
  "£",
  "¥",
  "µ",
  "∞",
  "√",
  "≈",
] as const;

const EMOJI = [
  "👍",
  "👎",
  "👏",
  "🙏",
  "🎉",
  "✅",
  "❌",
  "⚠️",
  "❗",
  "❓",
  "💡",
  "📌",
  "📎",
  "📅",
  "⏰",
  "🔥",
  "⭐",
  "❤️",
  "😀",
  "😅",
  "🤔",
  "👀",
  "🚀",
  "🐞",
] as const;

const STYLES: readonly BlockType[] = [
  "paragraph",
  "heading_1",
  "heading_2",
  "heading_3",
  "quote",
  "callout",
  "code",
];

const LINE_SPACINGS: readonly LineSpacing[] = [
  "tight",
  "normal",
  "relaxed",
  "loose",
];

const TABLE_SIZES: readonly (readonly [number, number])[] = [
  [2, 2],
  [3, 3],
  [4, 4],
  [5, 5],
];

/** Opens the side panel on a tab. */
function showPanel(callbacks: DocPageCallbacks | undefined, tab: DocPanelTab) {
  callbacks?.onPanelOpenChange?.(true);
  callbacks?.onPanelTab?.(tab);
}

export function buildMenus(ctx: MenuContext): Record<MenuKey, MenuEntry[]> {
  const { editor, copy, state, table, callbacks, view, canEdit, hosts } = ctx;
  const m = copy.menubar;
  const reason = canEdit ? undefined : m.needsEditor;
  const writable = canEdit && !state.inCodeBlock;
  const item = (
    id: string,
    label: string,
    onSelect: () => void,
    extra: Partial<Extract<MenuEntry, { kind: "item" }>> = {},
  ): MenuEntry => ({ kind: "item", id, label, onSelect, ...extra });
  const edit = (
    id: string,
    label: string,
    onSelect: () => void,
    extra: Partial<Extract<MenuEntry, { kind: "item" }>> = {},
  ): MenuEntry =>
    item(id, label, onSelect, {
      disabled: !canEdit,
      reason,
      ...extra,
    });
  const SEP: MenuEntry = { kind: "separator" };
  const blockItem = (type: BlockType, label: string): MenuEntry =>
    edit(
      `insert-${type}`,
      label,
      () => void insertBlockAfterCaret(editor, type, hosts),
    );

  const editMenu: MenuEntry[] = [
    edit("undo", m.edit.undo, () => editor.chain().focus().undo().run(), {
      shortcut: "undo",
      disabled: !canEdit || !state.canUndo,
    }),
    edit("redo", m.edit.redo, () => editor.chain().focus().redo().run(), {
      shortcut: "redo",
      disabled: !canEdit || !state.canRedo,
    }),
    SEP,
    edit("cut", m.edit.cut, () => callbacks?.onClipboardAction?.("cut"), {
      disabled:
        !canEdit || !state.hasSelection || !callbacks?.onClipboardAction,
    }),
    item("copy", m.edit.copy, () => callbacks?.onClipboardAction?.("copy"), {
      disabled: !state.hasSelection || !callbacks?.onClipboardAction,
    }),
    edit("paste", m.edit.paste, () => callbacks?.onClipboardAction?.("paste"), {
      disabled: !canEdit || !callbacks?.onClipboardAction,
    }),
    edit(
      "pastePlain",
      m.edit.pastePlain,
      () => callbacks?.onClipboardAction?.("pastePlain"),
      {
        shortcut: "pastePlain",
        disabled: !canEdit || !callbacks?.onClipboardAction,
      },
    ),
    SEP,
    item(
      "selectAll",
      m.edit.selectAll,
      () => editor.chain().focus().selectAll().run(),
      { shortcut: "selectAll" },
    ),
    edit("deleteBlock", m.edit.deleteBlock, () => deleteCurrentBlock(editor)),
    SEP,
    item("find", m.edit.find, () => editor.commands.openFind(false), {
      shortcut: "find",
    }),
    edit("replace", m.edit.replace, () => editor.commands.openFind(true), {
      shortcut: "replace",
    }),
  ];

  const viewMenu: MenuEntry[] = [
    item(
      "sidePanel",
      m.view.sidePanel,
      () => callbacks?.onPanelOpenChange?.(!view.panelOpen),
      { checked: view.panelOpen },
    ),
    item("outline", m.view.outline, () => showPanel(callbacks, "toc")),
    item("comments", m.view.comments, () => showPanel(callbacks, "comments"), {
      shortcut: undefined,
    }),
    item("links", m.view.links, () => showPanel(callbacks, "links")),
    SEP,
    item("wordCount", m.view.wordCount, () => callbacks?.onShowWordCount?.(), {
      shortcut: "wordCount",
    }),
    SEP,
    item(
      "fullWidth",
      m.view.fullWidth,
      () => callbacks?.onToggleFullWidth?.(),
      {
        checked: view.fullWidth,
      },
    ),
    item("toolbar", m.view.toolbar, () => callbacks?.onToggleToolbar?.(), {
      checked: view.toolbarShown,
    }),
    item(
      "focusMode",
      m.view.focusMode,
      () => callbacks?.onToggleFocusMode?.(),
      {
        checked: view.focusMode,
        shortcut: "focusMode",
      },
    ),
  ];

  const glyphs = (list: readonly string[]): MenuEntry[] =>
    list.map((glyph) =>
      edit(`glyph-${glyph}`, glyph, () => insertText(editor, glyph), {
        glyph,
      }),
    );

  const insertMenu: MenuEntry[] = [
    ...(mediaOf(editor)
      ? [
          blockItem("image", m.insert.image),
          blockItem("file", m.insert.file),
          SEP,
        ]
      : []),
    {
      kind: "sub",
      id: "insert-table",
      label: m.insert.table,
      disabled: !canEdit,
      reason,
      entries: TABLE_SIZES.map(([rows, cols]) =>
        edit(
          `insert-table-${rows}x${cols}`,
          fillDocCopy(m.insert.tableSized, {
            rows: String(rows),
            cols: String(cols),
          }),
          () => {
            if (startBlockHere(editor)) insertTableBlock(editor, rows, cols);
          },
        ),
      ),
    },
    blockItem("divider", m.insert.divider),
    blockItem("callout", m.insert.callout),
    blockItem("quote", m.insert.quote),
    blockItem("toggle", m.insert.toggle),
    blockItem("code", m.insert.codeBlock),
    SEP,
    blockItem("todo", m.insert.checklist),
    blockItem("bulleted_list", m.insert.bulleted),
    blockItem("numbered_list", m.insert.numbered),
    SEP,
    edit("link", m.insert.link, () => callbacks?.onEditorCommand?.("link"), {
      shortcut: "link",
      disabled: !writable,
    }),
    edit(
      "mention",
      m.insert.mention,
      () => void editor.chain().focus().insertContent("@").run(),
      { disabled: !writable },
    ),
    ...(hosts.onPickPage
      ? [blockItem("page_reference", m.insert.pageLink)]
      : []),
    ...(hosts.onPickBoard
      ? [blockItem("board_reference", m.insert.board)]
      : []),
    SEP,
    edit("date", m.insert.date, () => insertToday(editor), {
      disabled: !writable,
    }),
    {
      kind: "sub",
      id: "special",
      label: m.insert.specialChars,
      grid: true,
      disabled: !writable,
      reason,
      entries: glyphs(SPECIAL_CHARS),
    },
    {
      kind: "sub",
      id: "emoji",
      label: m.insert.emoji,
      grid: true,
      disabled: !writable,
      reason,
      entries: glyphs(EMOJI),
    },
    SEP,
    item(
      "comment",
      m.insert.comment,
      () => callbacks?.onEditorCommand?.("comment"),
      { shortcut: "comment" },
    ),
  ];

  const mark = (
    id: string,
    label: string,
    pressed: boolean,
    run: () => void,
    shortcut?: ShortcutId,
  ): MenuEntry =>
    edit(id, label, run, { checked: pressed, shortcut, disabled: !writable });

  const formatMenu: MenuEntry[] = [
    {
      kind: "sub",
      id: "textStyle",
      label: m.format.textStyle,
      disabled: !canEdit,
      reason,
      entries: STYLES.map((type) =>
        edit(
          `style-${type}`,
          copy.slash.items[type as keyof typeof copy.slash.items],
          () => setBlockType(editor, type),
          {
            checked: state.blockType === type,
            shortcut: STYLE_SHORTCUT[type],
          },
        ),
      ),
    },
    {
      kind: "sub",
      id: "text",
      label: m.format.text,
      disabled: !writable,
      reason,
      entries: [
        mark(
          "bold",
          m.format.bold,
          state.bold,
          () => editor.chain().focus().toggleBold().run(),
          "bold",
        ),
        mark(
          "italic",
          m.format.italic,
          state.italic,
          () => editor.chain().focus().toggleItalic().run(),
          "italic",
        ),
        mark(
          "underline",
          m.format.underline,
          state.underline,
          () => editor.chain().focus().toggleUnderline().run(),
          "underline",
        ),
        mark(
          "strike",
          m.format.strike,
          state.strike,
          () => editor.chain().focus().toggleStrike().run(),
          "strike",
        ),
        mark(
          "code",
          m.format.code,
          state.code,
          () => editor.chain().focus().toggleCode().run(),
          "code",
        ),
        SEP,
        mark(
          "superscript",
          m.format.superscript,
          state.superscript,
          () => toggleScript(editor, "superscript"),
          "superscript",
        ),
        mark(
          "subscript",
          m.format.subscript,
          state.subscript,
          () => toggleScript(editor, "subscript"),
          "subscript",
        ),
      ],
    },
    {
      kind: "sub",
      id: "color",
      label: m.format.color,
      disabled: !writable,
      reason,
      entries: COLOR_TOKENS.map((token) =>
        edit(
          `color-${token}`,
          copy.toolbar.colorNames[token],
          () => setTextColor(editor, token),
          {
            swatch: COLOR_SWATCH[token],
            checked: (state.color ?? "default") === token,
          },
        ),
      ),
    },
    {
      kind: "sub",
      id: "highlight",
      label: m.format.highlight,
      disabled: !writable,
      reason,
      entries: HIGHLIGHT_TOKENS.map((token) =>
        edit(
          `highlight-${token}`,
          copy.toolbar.highlightNames[token],
          () => setHighlight(editor, token),
          {
            swatch: HIGHLIGHT_CLASS[token] || "bg-transparent",
            checked: (state.highlight ?? "none") === token,
          },
        ),
      ),
    },
    SEP,
    {
      kind: "sub",
      id: "align",
      label: m.format.align,
      disabled: !writable,
      reason,
      entries: (
        [
          ["left", copy.toolbar.alignLeft, "alignLeft"],
          ["center", copy.toolbar.alignCenter, "alignCenter"],
          ["right", copy.toolbar.alignRight, "alignRight"],
          ["justify", copy.toolbar.alignJustify, "alignJustify"],
        ] as const
      ).map(([value, label, shortcut]) =>
        edit(`align-${value}`, label, () => setAlign(editor, value), {
          checked: state.align === value,
          shortcut,
        }),
      ),
    },
    {
      kind: "sub",
      id: "lists",
      label: m.format.lists,
      disabled: !canEdit,
      reason,
      entries: [
        edit(
          "list-bulleted",
          m.insert.bulleted,
          () =>
            setBlockType(
              editor,
              state.blockType === "bulleted_list"
                ? "paragraph"
                : "bulleted_list",
            ),
          {
            checked: state.blockType === "bulleted_list",
            shortcut: "bulleted",
          },
        ),
        edit(
          "list-numbered",
          m.insert.numbered,
          () =>
            setBlockType(
              editor,
              state.blockType === "numbered_list"
                ? "paragraph"
                : "numbered_list",
            ),
          {
            checked: state.blockType === "numbered_list",
            shortcut: "numbered",
          },
        ),
        edit(
          "list-todo",
          m.insert.checklist,
          () =>
            setBlockType(
              editor,
              state.blockType === "todo" ? "paragraph" : "todo",
            ),
          { checked: state.blockType === "todo", shortcut: "checklist" },
        ),
      ],
    },
    edit("indent", m.format.indent, () => indentBlocks(editor, 1), {
      shortcut: "indent",
      disabled: !canEdit || !state.canIndent,
    }),
    edit("outdent", m.format.outdent, () => indentBlocks(editor, -1), {
      shortcut: "outdent",
      disabled: !canEdit || !state.canOutdent,
    }),
    {
      kind: "sub",
      id: "lineSpacing",
      label: m.format.lineSpacing,
      disabled: !writable,
      reason,
      entries: LINE_SPACINGS.map((value) =>
        edit(
          `spacing-${value}`,
          copy.toolbar.lineSpacingNames[value],
          () => setLineSpacing(editor, value),
          { checked: state.lineSpacing === value },
        ),
      ),
    },
    SEP,
    {
      kind: "sub",
      id: "table",
      label: m.format.table,
      disabled: !canEdit || !table.active,
      reason: canEdit ? m.table.outside : reason,
      entries: [
        edit("row-before", m.table.rowBefore, () =>
          TABLE_COMMANDS.rowBefore(editor),
        ),
        edit("row-after", m.table.rowAfter, () =>
          TABLE_COMMANDS.rowAfter(editor),
        ),
        edit("col-before", m.table.columnBefore, () =>
          TABLE_COMMANDS.columnBefore(editor),
        ),
        edit("col-after", m.table.columnAfter, () =>
          TABLE_COMMANDS.columnAfter(editor),
        ),
        SEP,
        edit("merge", m.table.merge, () => TABLE_COMMANDS.merge(editor), {
          disabled: !canEdit || !state.canMerge,
        }),
        edit("split", m.table.split, () => TABLE_COMMANDS.split(editor), {
          disabled: !canEdit || !state.canSplit,
        }),
        edit(
          "header-row",
          m.table.headerRow,
          () => TABLE_COMMANDS.toggleHeaderRow(editor),
          {
            checked: table.headerRow,
          },
        ),
        SEP,
        edit("delete-row", m.table.deleteRow, () =>
          TABLE_COMMANDS.deleteRow(editor),
        ),
        edit("delete-col", m.table.deleteColumn, () =>
          TABLE_COMMANDS.deleteColumn(editor),
        ),
        edit("delete-table", m.table.deleteTable, () =>
          TABLE_COMMANDS.deleteTable(editor),
        ),
      ],
    },
    SEP,
    edit("clear", m.format.clear, () => clearFormatting(editor), {
      shortcut: "clearFormat",
    }),
  ];

  const toolsMenu: MenuEntry[] = [
    item(
      "tools-wordCount",
      m.tools.wordCount,
      () => callbacks?.onShowWordCount?.(),
      {
        shortcut: "wordCount",
      },
    ),
    item(
      "tools-shortcuts",
      m.tools.shortcuts,
      () => callbacks?.onShowShortcuts?.(),
      {
        shortcut: "shortcuts",
      },
    ),
    SEP,
    item(
      "tools-spellcheck",
      m.tools.spellcheck,
      () => ctx.onSpellcheck(!view.spellcheck),
      { checked: view.spellcheck },
    ),
  ];

  return {
    edit: editMenu,
    view: viewMenu,
    insert: insertMenu,
    format: formatMenu,
    tools: toolsMenu,
  };
}

const STYLE_SHORTCUT: Partial<Record<BlockType, ShortcutId>> = {
  paragraph: "paragraph",
  heading_1: "heading1",
  heading_2: "heading2",
  heading_3: "heading3",
  quote: "quote",
  code: "codeBlock",
};
