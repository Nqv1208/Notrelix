import {
  useCallback,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
} from "react";
import { useEditorState, type Editor } from "@tiptap/react";
import { Check, ChevronRight } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { DocPageCopy } from "./doc-page.copy";
import type { DocPageCallbacks } from "./doc-page-models";
import { useMenuKeys } from "./use-menu-keys";
import type { InsertHosts } from "../editor/block-insert";
import { selectToolbarState } from "../editor/editor-state";
import {
  buildMenus,
  type MenuEntry,
  type MenuKey,
  type MenuViewState,
} from "../editor/menu-model";
import {
  ariaShortcut,
  shortcutKeys,
  shortcutLabel,
} from "../editor/shortcut-catalog";
import { selectTableState } from "../editor/table-commands";

const MENU_BUTTON =
  "h-7 shrink-0 rounded-[6px] max-md:h-11 max-md:rounded-none max-md:px-3 border-0 bg-transparent px-[9px] text-compact leading-[1.45] text-muted-foreground outline-none hover:bg-state-hover-strong hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default";

const PANEL =
  "absolute z-dropdown box-content min-w-[240px] rounded-[10px] border border-line-strong bg-doc-panel p-1.5 shadow-popover";

const ITEM =
  "flex h-8.5 w-full items-center gap-2.5 rounded-[7px] border-0 bg-transparent px-2.5 text-left text-nav leading-[1.45] font-normal text-foreground outline-none hover:bg-state-hover-strong focus-visible:bg-state-hover-strong aria-disabled:cursor-default aria-disabled:text-meta-foreground aria-disabled:hover:bg-transparent";

export const MENU_KEYS: readonly MenuKey[] = [
  "edit",
  "view",
  "insert",
  "format",
  "tools",
];

export interface DocMenuBarProps {
  readonly copy: DocPageCopy;
  readonly editor: Editor;
  readonly canEdit: boolean;
  readonly view: MenuViewState;
  readonly hosts: InsertHosts;
  readonly callbacks?: DocPageCallbacks;
  readonly fileMenuOpen: boolean;
  readonly onSpellcheck: (on: boolean) => void;
  /** The open menu, owned by the bar so the File menu can hand over to it. */
  readonly open: MenuKey | null;
  readonly onOpenChange: (key: MenuKey | null) => void;
  /** ArrowLeft from the first menu / ArrowRight from the last: the File menu. */
  readonly onLeaveToFile: () => void;
}

function Shortcut({
  id,
}: {
  id: NonNullable<Extract<MenuEntry, { kind: "item" }>["shortcut"]>;
}) {
  return (
    <span className="ml-auto pl-4 text-metadata leading-[1.45] tracking-normal text-meta-foreground">
      {shortcutLabel(shortcutKeys(id))}
    </span>
  );
}

function MenuItemRow({
  entry,
  onActivate,
}: {
  entry: Extract<MenuEntry, { kind: "item" }>;
  onActivate: (entry: Extract<MenuEntry, { kind: "item" }>) => void;
}) {
  const toggle = entry.checked !== undefined;
  const keys = entry.shortcut
    ? ariaShortcut(shortcutKeys(entry.shortcut))
    : undefined;
  if (entry.glyph) {
    return (
      <button
        type="button"
        role="menuitem"
        data-menu-item=""
        tabIndex={-1}
        aria-label={entry.label}
        aria-disabled={entry.disabled || undefined}
        title={entry.disabled ? entry.reason : entry.label}
        onMouseDown={(event) => event.preventDefault()}
        onClick={() => onActivate(entry)}
        className="grid size-8 place-items-center rounded-[7px] border-0 bg-transparent text-body outline-none hover:bg-state-hover-strong focus-visible:bg-state-hover-strong aria-disabled:cursor-default aria-disabled:opacity-50"
      >
        {entry.glyph}
      </button>
    );
  }
  return (
    <button
      type="button"
      role={toggle ? "menuitemcheckbox" : "menuitem"}
      aria-checked={toggle ? entry.checked : undefined}
      aria-keyshortcuts={keys}
      aria-disabled={entry.disabled || undefined}
      title={entry.disabled ? entry.reason : undefined}
      data-menu-item=""
      tabIndex={-1}
      // The editor keeps its selection while the menu is used.
      onMouseDown={(event) => event.preventDefault()}
      onClick={() => onActivate(entry)}
      className={ITEM}
    >
      <span
        aria-hidden="true"
        className="grid size-4 shrink-0 place-items-center text-brand-solid"
      >
        {toggle && entry.checked ? (
          <Check className="size-4 stroke-[2]" />
        ) : entry.swatch ? (
          <span
            className={cn(
              "size-3.5 rounded-full border border-border",
              entry.swatch,
            )}
          />
        ) : null}
      </span>
      <span className="min-w-0 flex-1 truncate">{entry.label}</span>
      {entry.shortcut ? <Shortcut id={entry.shortcut} /> : null}
    </button>
  );
}

function MenuList({
  entries,
  label,
  onActivate,
  onClose,
  onSide,
  onBack,
  grid = false,
  autoFocus = true,
  className,
}: {
  entries: readonly MenuEntry[];
  label: string;
  onActivate: (entry: Extract<MenuEntry, { kind: "item" }>) => void;
  onClose: (reason: "escape" | "tab") => void;
  onSide?: (direction: -1 | 1) => void;
  onBack?: () => void;
  grid?: boolean;
  autoFocus?: boolean;
  className?: string;
}) {
  const ref = useRef<HTMLDivElement | null>(null);
  const [open, setOpenState] = useState<string | null>(null);
  const [byKey, setByKey] = useState(false);
  const setOpen = (id: string | null, viaKey = false) => {
    setOpenState(id);
    setByKey(viaKey);
  };
  const keys = useMenuKeys(ref, {
    autoFocus,
    onClose,
    onSide,
    onBack,
    columns: grid ? 7 : undefined,
  });
  return (
    <div
      ref={ref}
      role="menu"
      aria-label={label}
      data-menu-panel=""
      onKeyDown={keys}
      className={cn(
        PANEL,
        grid && "grid w-[260px] grid-cols-7 gap-0.5 p-2",
        className,
      )}
    >
      {entries.map((entry, index) => {
        if (entry.kind === "separator") {
          return (
            <div
              key={`sep-${index}`}
              role="separator"
              className="mx-1 my-[5px] h-px bg-border"
            />
          );
        }
        if (entry.kind === "sub") {
          const expanded = open === entry.id;
          return (
            <div
              key={entry.id}
              className="relative"
              onMouseEnter={() => !entry.disabled && setOpen(entry.id)}
              onMouseLeave={() => setOpen(null)}
            >
              <button
                type="button"
                role="menuitem"
                aria-haspopup="menu"
                aria-expanded={expanded}
                aria-disabled={entry.disabled || undefined}
                title={entry.disabled ? entry.reason : undefined}
                data-menu-item=""
                data-menu-sub={entry.id}
                tabIndex={-1}
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => !entry.disabled && setOpen(entry.id)}
                onKeyDown={(event) => {
                  if (
                    !entry.disabled &&
                    (event.key === "ArrowRight" ||
                      event.key === "Enter" ||
                      event.key === " ")
                  ) {
                    event.preventDefault();
                    event.stopPropagation();
                    setOpen(entry.id, true);
                  }
                }}
                className={cn(ITEM, expanded && "bg-state-hover-strong")}
              >
                <span aria-hidden="true" className="size-4 shrink-0" />
                <span className="min-w-0 flex-1 truncate">{entry.label}</span>
                <ChevronRight
                  aria-hidden="true"
                  className="size-3.5 shrink-0 stroke-[1.8] text-meta-foreground"
                />
              </button>
              {expanded ? (
                <MenuList
                  entries={entry.entries}
                  label={entry.label}
                  grid={entry.grid}
                  onActivate={onActivate}
                  onClose={onClose}
                  onSide={() => undefined}
                  onBack={() => {
                    setOpen(null);
                    const trigger = ref.current?.querySelector<HTMLElement>(
                      `[data-menu-sub="${entry.id}"]`,
                    );
                    trigger?.focus();
                  }}
                  className="top-0 left-full ml-0.5"
                  autoFocus={byKey}
                />
              ) : null}
            </div>
          );
        }
        return (
          <MenuItemRow key={entry.id} entry={entry} onActivate={onActivate} />
        );
      })}
    </div>
  );
}

/**
 * Edit, View, Insert, Format and Tools of the document menu bar (the File
 * menu is the page's own overlay). Each entry runs an editor command or opens
 * a page panel; toggles state their value; disabled entries say why.
 * `role="menubar"` is owned by the parent bar; this renders the menus' buttons
 * and the open panel.
 */
export function DocMenuBarMenus({
  copy,
  editor,
  canEdit,
  view,
  hosts,
  callbacks,
  fileMenuOpen,
  onSpellcheck,
  open,
  onOpenChange: setOpen,
  onLeaveToFile,
  labels,
  tabStop,
}: DocMenuBarProps & {
  readonly labels: Readonly<Record<MenuKey, string>>;
  /** The menu bar's one tab stop (a menu key or `file`). */
  readonly tabStop: string;
}) {
  const bar = useRef<HTMLSpanElement | null>(null);
  const idBase = useId();
  const state = useEditorState({
    editor,
    selector: ({ editor: current }) => selectToolbarState(current),
  });
  const table = useEditorState({
    editor,
    selector: ({ editor: current }) => selectTableState(current),
  });

  // The File menu took over: close ours.
  useEffect(() => {
    if (fileMenuOpen) setOpen(null);
  }, [fileMenuOpen]);

  // A press outside closes the menu.
  useEffect(() => {
    if (!open) return;
    const onDown = (event: PointerEvent) => {
      if (!bar.current?.contains(event.target as Node)) setOpen(null);
    };
    document.addEventListener("pointerdown", onDown);
    return () => document.removeEventListener("pointerdown", onDown);
  }, [open]);

  const entries = useMemo(
    () =>
      open
        ? buildMenus({
            editor,
            copy,
            state,
            table,
            callbacks,
            view,
            canEdit,
            hosts,
            onSpellcheck,
          })[open]
        : null,
    [
      open,
      editor,
      copy,
      state,
      table,
      callbacks,
      view,
      canEdit,
      hosts,
      onSpellcheck,
    ],
  );

  const triggers = useRef(new Map<MenuKey, HTMLButtonElement>());
  const focusTrigger = useCallback((key: MenuKey) => {
    triggers.current.get(key)?.focus();
  }, []);
  const switchTo = (from: MenuKey, direction: -1 | 1) => {
    const at = MENU_KEYS.indexOf(from) + direction;
    if (at < 0 || at >= MENU_KEYS.length) {
      setOpen(null);
      onLeaveToFile();
      return;
    }
    const key = MENU_KEYS[at] as MenuKey;
    setOpen(key);
    focusTrigger(key);
  };

  const onTriggerKey = (
    event: KeyboardEvent<HTMLButtonElement>,
    key: MenuKey,
  ) => {
    if (
      event.key === "ArrowDown" ||
      event.key === "Enter" ||
      event.key === " "
    ) {
      event.preventDefault();
      setOpen(key);
    } else if (event.key === "Escape" && open) {
      event.preventDefault();
      setOpen(null);
    }
  };

  const closeAndRefocus = (key: MenuKey) => {
    setOpen(null);
    focusTrigger(key);
  };

  return (
    <span ref={bar} className="relative inline-flex">
      {MENU_KEYS.map((key) => {
        const expanded = open === key;
        return (
          <span key={key} className="relative inline-flex">
            <button
              ref={(node) => {
                if (node) triggers.current.set(key, node);
                else triggers.current.delete(key);
              }}
              type="button"
              role="menuitem"
              id={`${idBase}-${key}`}
              aria-haspopup="menu"
              aria-expanded={expanded}
              data-menubar-item={key}
              tabIndex={tabStop === key ? 0 : -1}
              onClick={() => setOpen(expanded ? null : key)}
              onMouseEnter={() => {
                if (open && open !== key) setOpen(key);
              }}
              onKeyDown={(event) => onTriggerKey(event, key)}
              className={cn(
                MENU_BUTTON,
                expanded && "bg-state-hover-strong text-foreground",
              )}
            >
              {labels[key]}
            </button>
            {expanded && entries ? (
              <MenuList
                entries={entries}
                label={labels[key]}
                onActivate={(entry) => {
                  if (entry.disabled) return;
                  setOpen(null);
                  entry.onSelect();
                }}
                onClose={(reason) => {
                  if (reason === "escape") closeAndRefocus(key);
                  else setOpen(null);
                }}
                onSide={(direction) => switchTo(key, direction)}
                onBack={undefined}
                className="top-[30px] left-0 max-md:fixed max-md:inset-x-2 max-md:top-auto max-md:mt-11 max-md:w-auto"
              />
            ) : null}
          </span>
        );
      })}
    </span>
  );
}
