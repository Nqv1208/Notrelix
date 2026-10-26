import { useEffect, useRef, useState } from "react";
import { useEditorState, type Editor } from "@tiptap/react";
import {
  Code2,
  FileText,
  LayoutList,
  List,
  ListChecks,
  ListOrdered,
  Minus,
  Pilcrow,
  Quote,
  Table2,
  TextQuote,
  Type,
  type LucideIcon,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { BlockType } from "@notrelix/docs-core";
import { fillDocCopy, type DocPageCopy } from "../components/doc-page.copy";
import { foldText } from "../components/doc-popups-surface";
import { setBlockType } from "./editor-commands";
import { insertTableBlock } from "./table-commands";

interface SlashItem {
  readonly type: BlockType;
  readonly icon: LucideIcon;
  /** The markdown shortcut that makes the same block. */
  readonly shortcut?: string;
  readonly keywords: string;
}

export const SLASH_ITEMS: readonly SlashItem[] = [
  { type: "paragraph", icon: Pilcrow, keywords: "doan van text paragraph" },
  {
    type: "heading_1",
    icon: Type,
    shortcut: "#",
    keywords: "tieu de heading h1",
  },
  {
    type: "heading_2",
    icon: Type,
    shortcut: "##",
    keywords: "tieu de heading h2",
  },
  {
    type: "heading_3",
    icon: Type,
    shortcut: "###",
    keywords: "tieu de heading h3",
  },
  {
    type: "bulleted_list",
    icon: List,
    shortcut: "-",
    keywords: "danh sach gach dau dong bullet",
  },
  {
    type: "numbered_list",
    icon: ListOrdered,
    shortcut: "1.",
    keywords: "danh sach so number",
  },
  {
    type: "todo",
    icon: ListChecks,
    shortcut: "[]",
    keywords: "danh sach kiem checklist todo",
  },
  { type: "quote", icon: Quote, shortcut: ">", keywords: "trich dan quote" },
  { type: "callout", icon: TextQuote, keywords: "ghi chu noi bat callout" },
  { type: "code", icon: Code2, shortcut: "```", keywords: "ma code" },
  {
    type: "divider",
    icon: Minus,
    shortcut: "---",
    keywords: "duong ke divider",
  },
  { type: "table", icon: Table2, keywords: "bang table" },
  {
    type: "page_reference",
    icon: FileText,
    keywords: "lien ket trang page link",
  },
  {
    type: "board_reference",
    icon: LayoutList,
    keywords: "board bang cong viec viec nhung embed task",
  },
];

interface SlashMatch {
  readonly from: number;
  readonly to: number;
  readonly query: string;
}

/** The "/query" the caret sits at the end of, at the start of a block or after a space. */
export function selectSlash(editor: Editor | null): SlashMatch | null {
  if (!editor || editor.isDestroyed || !editor.isEditable) return null;
  const { selection } = editor.state;
  if (!selection.empty) return null;
  const { $from } = selection;
  if (!$from.parent.isTextblock || $from.parent.type.name === "codeBlock") {
    return null;
  }
  // Blocks are inserted between blocks, never inside a table cell.
  if ($from.depth > 1) return null;
  const before = $from.parent.textBetween(
    0,
    $from.parentOffset,
    undefined,
    "￼",
  );
  const match = /(?:^|\s)\/([^\s/]*)$/.exec(before);
  if (!match) return null;
  const length = match[1]!.length + 1;
  return {
    from: selection.from - length,
    to: selection.from,
    query: match[1]!,
  };
}

export function matchSlashItems(
  copy: DocPageCopy,
  query: string,
): readonly SlashItem[] {
  const needle = foldText(query.trim());
  return SLASH_ITEMS.filter((item) => {
    if (needle === "") return true;
    const name = foldText(
      copy.slash.items[item.type as keyof typeof copy.slash.items],
    );
    return name.includes(needle) || item.keywords.includes(needle);
  });
}

export interface SlashMenuProps {
  readonly editor: Editor;
  readonly copy: DocPageCopy;
  /** Opens the page chooser for a page-reference block. */
  readonly onPickPage?: (blockId: string) => void;
  /** Opens the board chooser for a board-reference block; omitted hides the entry. */
  readonly onPickBoard?: (blockId: string) => void;
}

/** The "/" block menu: every block type with its markdown shortcut. */
export function SlashMenu({
  editor,
  copy,
  onPickPage,
  onPickBoard,
}: SlashMenuProps) {
  const match = useEditorState({
    editor,
    selector: ({ editor: current }) => selectSlash(current),
  });
  const [dismissed, setDismissed] = useState<number | null>(null);
  const items = (match ? matchSlashItems(copy, match.query) : []).filter(
    (item) => item.type !== "board_reference" || onPickBoard,
  );
  const [active, setActive] = useState(0);
  const latest = useRef({ items, match, active, dismissed });
  latest.current = { items, match, active, dismissed };
  const open = match !== null && dismissed !== match.from && items.length > 0;

  useEffect(() => setActive(0), [match?.query]);

  const pick = (item: SlashItem) => {
    if (!match) return;
    editor
      .chain()
      .focus()
      .deleteRange({ from: match.from, to: match.to })
      .run();
    if (item.type === "page_reference" || item.type === "board_reference") {
      const board = item.type === "board_reference";
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
      if (blockId) (board ? onPickBoard : onPickPage)?.(blockId);
      return;
    }
    if (item.type === "table") {
      insertTableBlock(editor);
      return;
    }
    setBlockType(editor, item.type);
  };

  useEffect(() => {
    if (!open) return;
    const onKey = (event: KeyboardEvent) => {
      const { items: list, match: current, active: at } = latest.current;
      if (!current) return;
      if (event.key === "ArrowDown") {
        event.preventDefault();
        setActive((at + 1) % list.length);
      } else if (event.key === "ArrowUp") {
        event.preventDefault();
        setActive((at - 1 + list.length) % list.length);
      } else if (event.key === "Enter" || event.key === "Tab") {
        event.preventDefault();
        event.stopPropagation();
        const item = list[at];
        if (item) pick(item);
      } else if (event.key === "Escape") {
        event.preventDefault();
        setDismissed(current.from);
      }
    };
    window.addEventListener("keydown", onKey, true);
    return () => window.removeEventListener("keydown", onKey, true);
    // `pick` reads only the editor and the latest match.
  }, [open, editor]);

  if (!open || !match) return null;
  const coords = editor.view.coordsAtPos(match.from);
  const host = editor.view.dom.parentElement?.getBoundingClientRect();
  return (
    <div
      role="listbox"
      aria-label={copy.slash.label}
      style={{
        top: coords.bottom - (host?.top ?? 0) + 6,
        left: Math.max(0, coords.left - (host?.left ?? 0)),
      }}
      className="absolute z-dropdown max-h-[320px] w-[320px] overflow-auto rounded-[12px] border border-line-strong bg-doc-panel p-1.5 shadow-popover"
    >
      <span className="mx-2 my-1 block text-micro font-semibold text-meta-foreground">
        {match.query
          ? fillDocCopy(copy.slash.heading, { query: match.query })
          : copy.slash.headingAll}
      </span>
      {items.map((item, index) => {
        const Icon = item.icon;
        const name =
          copy.slash.items[item.type as keyof typeof copy.slash.items];
        return (
          <button
            key={item.type}
            type="button"
            role="option"
            aria-selected={index === active}
            onMouseDown={(event) => event.preventDefault()}
            onMouseEnter={() => setActive(index)}
            onClick={() => pick(item)}
            className={cn(
              "box-border flex min-h-10 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2 py-1 text-left text-nav text-foreground outline-none",
              index === active && "bg-state-hover-strong",
            )}
          >
            <Icon
              aria-hidden="true"
              className="size-4 shrink-0 stroke-[1.8] text-muted-foreground"
            />
            <span className="min-w-0 flex-1 truncate">{name}</span>
            {item.shortcut ? (
              <kbd className="rounded-[4px] border border-line-strong px-1.5 font-mono text-micro text-meta-foreground">
                {item.shortcut}
              </kbd>
            ) : null}
          </button>
        );
      })}
    </div>
  );
}
