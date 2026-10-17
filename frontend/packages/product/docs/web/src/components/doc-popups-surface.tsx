import { useEffect, useRef, useState, type ReactNode } from "react";
import {
  Code2,
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
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type { DocTaskRef } from "./doc-page-models";
import { DocStatusDot, ICON_CLASS } from "./doc-page-ui";

const POPUP =
  "absolute box-content z-10 w-[320px] rounded-[12px] border border-line-strong bg-doc-panel p-1.5 leading-[1.4] shadow-popover";

const HEADING =
  "mx-2 my-1 block text-micro leading-[1.4] font-semibold text-meta-foreground";

const OPTION =
  "box-border flex min-h-11 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2 py-1 text-left text-nav leading-[1.4] font-normal text-foreground outline-none";

/** Slash menu entries in the order the design lists them. */
export const SLASH_TYPES: ReadonlyArray<{
  readonly type: BlockType;
  readonly icon: LucideIcon;
  readonly keywords: string;
}> = [
  {
    type: "board_reference",
    icon: Table2,
    keywords: "danh sach viec list task bang table",
  },
  { type: "todo", icon: ListChecks, keywords: "danh sach kiem checklist todo" },
  {
    type: "bulleted_list",
    icon: List,
    keywords: "danh sach gach dau dong bullet",
  },
  { type: "numbered_list", icon: ListOrdered, keywords: "danh sach so number" },
  { type: "paragraph", icon: Pilcrow, keywords: "doan van text paragraph" },
  { type: "heading_1", icon: Type, keywords: "tieu de heading h1" },
  { type: "heading_2", icon: Type, keywords: "tieu de heading h2" },
  { type: "heading_3", icon: Type, keywords: "tieu de heading h3" },
  { type: "quote", icon: Quote, keywords: "trich dan quote" },
  { type: "callout", icon: TextQuote, keywords: "ghi chu noi bat callout" },
  { type: "code", icon: Code2, keywords: "ma code" },
  { type: "divider", icon: Minus, keywords: "duong ke divider" },
];

/** Lower-cases and strips Vietnamese diacritics so "danh" finds "Danh sách". */
export function foldText(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase();
}

/** Shows at most this many block matches; the footer names the rest. */
const SLASH_VISIBLE = 3;

export function matchSlashTypes(
  copy: DocPageCopy,
  query: string,
): ReadonlyArray<(typeof SLASH_TYPES)[number]> {
  const needle = foldText(query.trim());
  return SLASH_TYPES.filter((entry) => {
    if (needle === "") return true;
    const name = foldText(
      copy.slash.items[entry.type as keyof typeof copy.slash.items],
    );
    return name.includes(needle) || entry.keywords.includes(needle);
  });
}

/** Arrow/Enter/Escape handling shared by both popups. */
function usePopupKeys({
  count,
  onPick,
  onClose,
}: {
  count: number;
  onPick: (index: number) => void;
  onClose: () => void;
}) {
  const [active, setActive] = useState(0);
  const latest = useRef({ count, onPick, onClose, active });
  latest.current = { count, onPick, onClose, active };

  useEffect(() => {
    setActive(0);
  }, [count]);

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      const {
        count: total,
        onPick: pick,
        onClose: close,
        active: at,
      } = latest.current;
      if (event.key === "ArrowDown" && total > 0) {
        event.preventDefault();
        setActive((at + 1) % total);
      } else if (event.key === "ArrowUp" && total > 0) {
        event.preventDefault();
        setActive((at - 1 + total) % total);
      } else if (event.key === "Enter" && total > 0) {
        event.preventDefault();
        event.stopPropagation();
        pick(at);
      } else if (event.key === "Escape") {
        event.preventDefault();
        close();
      }
    };
    window.addEventListener("keydown", onKey, true);
    return () => window.removeEventListener("keydown", onKey, true);
  }, []);

  return [active, setActive] as const;
}

export interface DocSlashMenuSurfaceProps {
  readonly copy: DocPageCopy;
  readonly query: string;
  /** Index of the highlighted entry when the menu is controlled (stories). */
  readonly activeIndex?: number;
  readonly className?: string;
  readonly onSelect?: (type: BlockType) => void;
  readonly onClose?: () => void;
}

/** The "/" block menu (Final-Docs `go-lenh`): 320 px, three matches and a footer. */
export function DocSlashMenuSurface({
  copy,
  query,
  activeIndex,
  className,
  onSelect,
  onClose,
}: DocSlashMenuSurfaceProps) {
  const matches = matchSlashTypes(copy, query);
  const shown = matches.slice(0, SLASH_VISIBLE);
  const [keyActive, setKeyActive] = usePopupKeys({
    count: shown.length,
    onPick: (index) => {
      const entry = shown[index];
      if (entry) onSelect?.(entry.type);
    },
    onClose: () => onClose?.(),
  });
  const active = activeIndex ?? keyActive;
  const s = copy.slash;

  return (
    <div role="listbox" aria-label={s.label} className={cn(POPUP, className)}>
      <span className={HEADING}>
        {query.trim() === ""
          ? s.headingAll
          : fillDocCopy(s.heading, { query: query.trim() })}
      </span>
      {shown.length === 0 ? (
        <span className={HEADING}>{s.none}</span>
      ) : (
        shown.map((entry, index) => {
          const name = s.items[entry.type as keyof typeof s.items];
          const hint = (s.hints as Record<string, string>)[entry.type];
          const Icon = entry.icon;
          return (
            <button
              key={entry.type}
              type="button"
              role="option"
              aria-selected={index === active}
              // Keep the caret in the block while picking.
              onMouseDown={(event) => event.preventDefault()}
              onMouseEnter={() => setKeyActive(index)}
              onClick={() => onSelect?.(entry.type)}
              className={cn(
                OPTION,
                index === active && "bg-state-hover-strong",
              )}
            >
              <span className="box-content grid size-7.5 shrink-0 place-items-center rounded-[7px] border border-line-strong bg-doc-page text-muted-foreground">
                <Icon aria-hidden="true" className={ICON_CLASS} />
              </span>
              <span>
                <span className="font-medium">{name}</span>
                {hint ? (
                  <small className="block text-metadata leading-[1.4] tracking-normal text-meta-foreground">
                    {hint}
                  </small>
                ) : null}
              </span>
            </button>
          );
        })
      )}
      <span className={cn(HEADING, "mt-1.5")}>{s.more}</span>
    </div>
  );
}

export interface DocMentionMenuSurfaceProps {
  readonly copy: DocPageCopy;
  readonly query: string;
  readonly tasks: readonly DocTaskRef[];
  readonly activeIndex?: number;
  readonly className?: string;
  readonly onSelect?: (taskId: string) => void;
  readonly onClose?: () => void;
}

/** The task mention list (Final-Docs `nhac-viec`): key, title and a footer hint. */
export function DocMentionMenuSurface({
  copy,
  query,
  tasks,
  activeIndex,
  className,
  onSelect,
  onClose,
}: DocMentionMenuSurfaceProps): ReactNode {
  const m = copy.mention;
  const [keyActive, setKeyActive] = usePopupKeys({
    count: tasks.length,
    onPick: (index) => {
      const task = tasks[index];
      if (task) onSelect?.(task.id);
    },
    onClose: () => onClose?.(),
  });
  const active = activeIndex ?? keyActive;

  return (
    <div role="listbox" aria-label={m.label} className={cn(POPUP, className)}>
      <span className={HEADING}>
        {fillDocCopy(m.heading, { query: query.trim() })}
      </span>
      {tasks.length === 0 ? (
        <span className={HEADING}>{m.none}</span>
      ) : (
        tasks.map((task, index) => (
          <button
            key={task.id}
            type="button"
            role="option"
            aria-selected={index === active}
            onMouseDown={(event) => event.preventDefault()}
            onMouseEnter={() => setKeyActive(index)}
            onClick={() => onSelect?.(task.id)}
            className={cn(OPTION, index === active && "bg-state-hover-strong")}
          >
            <DocStatusDot status={task.status} />
            <span className="font-mono text-metadata leading-[1.4] tracking-normal text-meta-foreground">
              {task.key}
            </span>
            {task.title}
          </button>
        ))
      )}
      <span className={cn(HEADING, "mt-2")}>{m.footer}</span>
    </div>
  );
}
