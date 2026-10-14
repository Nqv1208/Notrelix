import * as React from "react";
import {
  ArrowRight,
  BarChart3,
  ChevronDown,
  CircleCheck,
  File,
  House,
  Inbox,
  Keyboard,
  Library,
  Moon,
  Plus,
  Search,
  Settings,
  SquareCheckBig,
  Sun,
  User,
  UserRoundCheck,
  Users,
  UsersRound,
  LayoutGrid,
  type LucideIcon,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import {
  highlightSegments,
  moveSelection,
  nextPaletteFilter,
  parsePaletteQuery,
  PALETTE_FILTERS,
  type PaletteFilter,
} from "../../core/model/command-palette";
import { initialsFor } from "../../core/model/navigation";
import { DepartmentMark } from "../home/work-row-parts";
import {
  formatCopy,
  IDENTITY_MARK_CLASS,
  type ShellAppearance,
  type ShellPaletteSelection,
} from "../shell/shell-models";
import {
  buildPalette,
  firstEnabledId,
  flattenRows,
  type PaletteCapabilities,
  type PaletteCommandId,
  type PaletteCtxPiece,
  type PaletteIcon,
  type PaletteRow,
  type PaletteView,
} from "./command-build";
import type {
  PaletteBoardEntry,
  PaletteDocEntry,
  PalettePerson,
  PaletteRecentEntry,
  PaletteRunning,
  PaletteWorkEntry,
  PaletteWorkspace,
} from "./command-models";
import type { WorkspaceCommandCopy } from "./workspace-command.copy";

export interface WorkspaceCommandPaletteSurfaceProps {
  readonly copy: WorkspaceCommandCopy;
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly workspace: PaletteWorkspace;
  /** Items the member can open (the My work read). */
  readonly work: readonly PaletteWorkEntry[];
  /** Authorized Docs and Boards, already loaded by the host. */
  readonly docs: readonly PaletteDocEntry[];
  readonly boards: readonly PaletteBoardEntry[];
  readonly people: readonly PalettePerson[];
  /** The member's recently opened Boards and Docs, newest first. */
  readonly recents: readonly PaletteRecentEntry[];
  readonly appearance: ShellAppearance;
  readonly canCreateWorkspace: boolean;
  readonly canCreateDoc: boolean;
  readonly canSwitchWorkspace: boolean;
  /** The commands the host offers; absent = all it can run. */
  readonly commandIds?: readonly PaletteCommandId[];
  readonly onSelect: (selection: ShellPaletteSelection) => void;
  /** Initial text and filter tab (stories, tests). */
  readonly defaultQuery?: string;
  readonly defaultFilter?: PaletteFilter;
  /** A command that is running: the result list gives way to its progress. */
  readonly running?: PaletteRunning | null;
}

/** The Board glyph of the design (a frame with a long and a short column). */
const BoardGlyph = React.forwardRef<
  SVGSVGElement,
  React.ComponentProps<LucideIcon>
>(function BoardGlyph(props, ref) {
  const { strokeWidth = 2, className, ...rest } = props;
  return (
    <svg
      ref={ref}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      {...rest}
    >
      <rect x="3" y="4" width="18" height="16" rx="2" />
      <path d="M9 4v16M15 4v10" />
    </svg>
  );
}) as LucideIcon;

const ICONS: Readonly<Record<PaletteIcon, LucideIcon>> = {
  work: SquareCheckBig,
  doc: File,
  board: BoardGlyph,
  person: User,
  plus: Plus,
  home: House,
  "my-work": UserRoundCheck,
  inbox: Inbox,
  search: Search,
  library: Library,
  reports: BarChart3,
  members: Users,
  teams: UsersRound,
  settings: Settings,
  "theme-dark": Moon,
  "theme-light": Sun,
  keyboard: Keyboard,
  status: CircleCheck,
  assignee: User,
  arrow: ArrowRight,
  workspaces: LayoutGrid,
};

const KEY_CLASS =
  "shrink-0 rounded-[4px] border border-line-strong bg-keycap px-[5px] font-mono text-key leading-[18px] whitespace-nowrap text-muted-foreground";

const PILL_CLASS = {
  todo: "bg-status-chip-todo",
  doing: "bg-status-chip-doing",
  review: "bg-status-chip-review",
  done: "bg-status-chip-done",
} as const;

const DOT_CLASS = {
  todo: "shadow-[inset_0_0_0_2px_var(--color-work-status-todo)]",
  doing: "bg-work-status-doing",
  review: "bg-work-status-review",
  done: "bg-work-status-done",
} as const;

function Key({ children }: { children: React.ReactNode }) {
  return (
    <kbd data-f="kbd" className={KEY_CLASS}>
      {children}
    </kbd>
  );
}

/** Text with the typed term underlined in the accent colour. */
function Highlighted({ text, term }: { text: string; term: string }) {
  const segments = highlightSegments(text, term);
  return (
    <>
      {segments.map((segment, index) =>
        segment.hit ? (
          <b
            key={index}
            data-f="hl"
            className="font-bold underline decoration-primary underline-offset-3"
          >
            {segment.text}
          </b>
        ) : (
          <React.Fragment key={index}>{segment.text}</React.Fragment>
        ),
      )}
    </>
  );
}

function CtxLine({ pieces }: { pieces: readonly PaletteCtxPiece[] }) {
  if (pieces.length === 0) return null;
  return (
    <span
      data-f="ctx"
      className="mt-px flex items-center gap-1.5 overflow-hidden text-metadata leading-[1.45] font-normal tracking-normal whitespace-nowrap text-meta-foreground"
    >
      {pieces.map((piece, index) => {
        if (piece.type === "mark") {
          return (
            <span key={index} data-f="ctxdm" className="inline-flex shrink-0">
              <DepartmentMark
                name={piece.department.name}
                markTone={piece.department.markTone}
                small
              />
            </span>
          );
        }
        if (piece.type === "mono") {
          return (
            <span key={index} data-f="mono" className="font-mono">
              {piece.text}
            </span>
          );
        }
        return piece.hit ? (
          <b key={index} data-f="hl" className="font-bold text-foreground">
            {piece.text}
          </b>
        ) : (
          <React.Fragment key={index}>{piece.text}</React.Fragment>
        );
      })}
    </span>
  );
}

function RowIcon({ row }: { row: PaletteRow }) {
  if (row.person) {
    return (
      <span
        data-f="ic"
        aria-hidden="true"
        className="grid size-8 shrink-0 place-items-center rounded-[8px] border border-border bg-surface-canvas"
      >
        <span
          className={cn(
            "grid size-5 place-items-center rounded-full text-mark-xs font-bold",
            IDENTITY_MARK_CLASS[row.person.identity],
          )}
        >
          {initialsFor(row.person.name)}
        </span>
      </span>
    );
  }
  const Icon = ICONS[row.icon];
  return (
    <span
      data-f="ic"
      aria-hidden="true"
      className={cn(
        "grid size-8 shrink-0 place-items-center rounded-[8px] border border-border bg-surface-canvas",
        row.accent ? "text-state-selected-foreground" : "text-muted-foreground",
      )}
    >
      <Icon className="size-4" strokeWidth={1.8} />
    </span>
  );
}

function RowEnd({
  row,
  selected,
  copy,
}: {
  row: PaletteRow;
  selected: boolean;
  copy: WorkspaceCommandCopy;
}) {
  const hint = selected && row.openHint && !row.unavailable;
  if (!row.status && !row.due && row.keys.length === 0 && !hint) return null;
  return (
    <span
      data-f="end"
      className="flex shrink-0 items-center gap-2 text-metadata leading-[1.45] font-normal tracking-normal text-meta-foreground"
    >
      {row.status ? (
        <span
          data-f="stp"
          className={cn(
            "inline-flex h-[22px] items-center gap-1.5 rounded-full px-2 text-micro leading-[1.45] font-semibold text-foreground",
            PILL_CLASS[row.status.bucket],
          )}
        >
          <span
            data-f="dot"
            aria-hidden="true"
            className={cn(
              "inline-block size-[7px] rounded-full",
              DOT_CLASS[row.status.bucket],
            )}
          />
          {row.status.label}
        </span>
      ) : null}
      {row.due ? (
        <span
          data-f="due"
          className={cn(
            row.due.tone === "today" && "font-semibold text-warning-text",
            row.due.tone === "overdue" && "font-semibold text-danger-text",
          )}
        >
          {row.due.label}
        </span>
      ) : null}
      {hint ? (
        <span
          data-f="hintsel"
          className="font-semibold whitespace-nowrap text-state-selected-foreground"
        >
          {copy.open}
        </span>
      ) : null}
      {row.keys.map((key) => (
        <Key key={key}>{key}</Key>
      ))}
    </span>
  );
}

/**
 * The command palette (Final-Command): a 700 px dialog 92 px from the top over
 * a scrim, holding the query row, the type filters, the grouped results and
 * the key hints. Radix owns the dialog role, the focus trap and Escape; the
 * input is the combobox of the listbox below it (`aria-activedescendant`).
 * Everything it lists comes from the host's authorized lists; a row without a
 * producer is drawn like the design but `aria-disabled` with its reason.
 */
export function WorkspaceCommandPaletteSurface({
  copy,
  open,
  onOpenChange,
  workspace,
  work,
  docs,
  boards,
  people,
  recents,
  appearance,
  canCreateWorkspace,
  canCreateDoc,
  canSwitchWorkspace,
  commandIds,
  onSelect,
  defaultQuery = "",
  defaultFilter = "all",
  running = null,
}: WorkspaceCommandPaletteSurfaceProps) {
  const [query, setQuery] = React.useState(defaultQuery);
  const [filter, setFilter] = React.useState<PaletteFilter>(defaultFilter);
  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const inputRef = React.useRef<HTMLInputElement | null>(null);
  const listRef = React.useRef<HTMLDivElement | null>(null);
  // Focus goes back to what opened the palette when it is dismissed, but not
  // when a result was chosen (the choice moves focus or the page itself).
  const openerRef = React.useRef<HTMLElement | null>(null);
  const chosenRef = React.useRef(false);
  const baseId = React.useId();
  const listId = `${baseId}-results`;

  React.useEffect(() => {
    if (!open) {
      setQuery(defaultQuery);
      setFilter(defaultFilter);
      setSelectedId(null);
    }
  }, [open, defaultQuery, defaultFilter]);

  const capabilities = React.useMemo<PaletteCapabilities>(
    () => ({
      appearance,
      canCreateWorkspace,
      canCreateDoc,
      canSwitchWorkspace,
      commandIds,
      otherWorkspaces: workspace.others,
    }),
    [
      appearance,
      canCreateDoc,
      canCreateWorkspace,
      canSwitchWorkspace,
      commandIds,
      workspace.others,
    ],
  );
  const view: PaletteView = React.useMemo(
    () =>
      buildPalette({
        copy,
        query: running ? "" : query,
        filter,
        sources: { work, docs, boards, people, recents },
        capabilities,
        workspaceName: workspace.name,
      }),
    [
      boards,
      capabilities,
      copy,
      docs,
      filter,
      people,
      query,
      recents,
      running,
      work,
      workspace.name,
    ],
  );

  const rows = React.useMemo(() => flattenRows(view.groups), [view]);
  const activeId =
    selectedId && rows.some((row) => row.id === selectedId)
      ? selectedId
      : firstEnabledId(view.groups);
  const parsed = parsePaletteQuery(query);
  const term = parsed.term;
  const showHint = !running && !parsed.prefix && query === "";

  React.useEffect(() => {
    if (!activeId) return;
    const node = listRef.current?.querySelector<HTMLElement>(
      `[data-row-id="${CSS.escape(activeId)}"]`,
    );
    node?.scrollIntoView?.({ block: "nearest" });
  }, [activeId]);

  const optionDomId = (id: string) =>
    `${baseId}-${id.replace(/[^a-zA-Z0-9_-]/g, "_")}`;

  const choose = (row: PaletteRow) => {
    if (row.unavailable || !row.selection) return;
    chosenRef.current = true;
    onOpenChange(false);
    onSelect(row.selection);
  };

  const changeQuery = (next: string) => {
    setQuery(next);
    setSelectedId(null);
    setFilter("all");
  };

  const onInputKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.nativeEvent.isComposing) return;
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      const at = rows.findIndex((row) => row.id === activeId);
      const next = moveSelection(
        at,
        rows.length,
        event.key === "ArrowDown" ? 1 : -1,
      );
      setSelectedId(rows[next]?.id ?? null);
      return;
    }
    if (event.key === "Enter") {
      const row = rows.find((candidate) => candidate.id === activeId);
      if (!row) return;
      event.preventDefault();
      choose(row);
      return;
    }
    if (event.key === "Tab" && view.state === "results") {
      // Tab is the filter key here: keep the dialog's focus loop out of it.
      event.preventDefault();
      event.stopPropagation();
      setFilter(nextPaletteFilter(filter, event.shiftKey ? -1 : 1));
      setSelectedId(null);
    }
  };

  const none = view.state === "none";
  const listLabel =
    view.state === "recent"
      ? copy.listLabels.recent
      : view.state === "commands"
        ? copy.listLabels.commands
        : view.state === "none"
          ? copy.listLabels.suggestions
          : copy.listLabels.results;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          chosenRef.current = false;
          const active = document.activeElement;
          openerRef.current =
            active instanceof HTMLElement && active !== document.body
              ? active
              : null;
          inputRef.current?.focus();
        }}
        onCloseAutoFocus={(event) => {
          event.preventDefault();
          if (!chosenRef.current) openerRef.current?.focus();
        }}
        data-f="pal"
        className="top-[92px] box-border flex w-[702px] max-w-[calc(100%-30px)] translate-y-0 flex-col gap-0 overflow-hidden rounded-[14px] border border-line-strong bg-popover p-0 text-body leading-[1.45] shadow-palette ring-0 sm:max-w-[calc(100%-30px)] dark:bg-card"
      >
        <DialogTitle className="sr-only">{copy.title}</DialogTitle>
        <DialogDescription className="sr-only">
          {copy.description}
        </DialogDescription>

        <div
          data-f="qrow"
          className="box-border flex h-[57px] items-center gap-2.5 border-b border-border pr-3.5 pl-[18px]"
        >
          <Search
            data-f="qicon"
            aria-hidden="true"
            className="size-[19px] shrink-0 text-meta-foreground"
            strokeWidth={1.8}
          />
          <div
            data-f="q"
            className="relative flex min-w-0 flex-1 items-center text-palette-query text-foreground"
          >
            {parsed.prefix || running ? (
              <span
                data-f="qpre"
                aria-hidden="true"
                className="mr-1 font-mono font-semibold text-state-selected-foreground"
              >
                {running ? ">" : parsed.prefix}
              </span>
            ) : null}
            <input
              ref={inputRef}
              type="text"
              role="combobox"
              aria-label={copy.placeholder}
              aria-expanded="true"
              aria-controls={listId}
              aria-autocomplete="list"
              aria-activedescendant={
                activeId && !running ? optionDomId(activeId) : undefined
              }
              autoComplete="off"
              spellCheck={false}
              readOnly={Boolean(running)}
              value={
                running
                  ? running.typed
                  : parsed.prefix
                    ? query.trimStart().slice(1)
                    : query
              }
              onChange={(event) => {
                const typed = event.target.value;
                // A prefix typed over a chip starts that mode instead.
                changeQuery(
                  parsed.prefix && !/^[>@#]/.test(typed)
                    ? `${parsed.prefix}${typed}`
                    : typed,
                );
              }}
              onKeyDown={(event) => {
                // Backspace on an empty term removes the prefix chip.
                if (
                  event.key === "Backspace" &&
                  parsed.prefix &&
                  !query.trimStart().slice(1)
                ) {
                  event.preventDefault();
                  changeQuery("");
                  return;
                }
                onInputKeyDown(event);
              }}
              className={cn(
                "m-0 min-w-0 border-0 bg-transparent p-0 text-ellipsis text-palette-query text-foreground caret-primary outline-none",
                // The hint wraps onto lines of its own, so the field lies over it.
                showHint ? "absolute inset-0 size-full" : "flex-1",
              )}
            />
            {showHint ? (
              <span
                data-f="ph"
                aria-hidden="true"
                className="pointer-events-none min-w-0 flex-1 text-meta-foreground"
              >
                {copy.placeholder}
              </span>
            ) : null}
          </div>
          <button
            type="button"
            data-f="scope"
            aria-haspopup="listbox"
            aria-disabled="true"
            aria-label={`${copy.scope.label}: ${workspace.name}`}
            title={copy.scope.unavailable}
            className="box-border inline-flex h-7 cursor-not-allowed items-center gap-1.5 rounded-[7px] border border-line-strong bg-surface-canvas px-[9px] text-crumb leading-[1.45] font-medium whitespace-nowrap text-muted-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <span data-f="scopedm" className="inline-flex shrink-0">
              <DepartmentMark
                name={workspace.name}
                markTone={workspace.markTone}
                small
              />
            </span>
            <span className="max-w-[180px] truncate">{workspace.name}</span>
            <ChevronDown
              data-f="scopechev"
              aria-hidden="true"
              className="size-[13px] text-meta-foreground"
              strokeWidth={1.8}
            />
          </button>
          <kbd data-f="esc" className={KEY_CLASS}>
            {copy.esc}
          </kbd>
        </div>

        {running ? (
          <div
            data-f="running"
            id={listId}
            role="status"
            aria-live="polite"
            className="flex items-center gap-3 px-[18px] py-4"
          >
            <span
              data-f="spin"
              aria-hidden="true"
              className="box-border size-[22px] shrink-0 animate-spin rounded-full border-2 border-line-strong border-t-primary motion-reduce:animate-none motion-reduce:border-t-line-strong motion-reduce:border-r-primary"
            />
            <div>
              <b className="font-semibold">{running.title}</b>
              <div className="text-crumb leading-[1.45] text-meta-foreground">
                {running.detail}
              </div>
              <div className="text-crumb leading-[1.45] text-meta-foreground">
                {copy.running.note}
              </div>
            </div>
            <button
              type="button"
              data-f="rbtn"
              aria-disabled={running.onUndo ? undefined : true}
              onClick={running.onUndo}
              className="ml-auto box-border h-[30px] shrink-0 cursor-pointer rounded-[7px] border border-line-strong bg-popover px-3 text-compact leading-[1.45] font-semibold text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed dark:bg-card"
            >
              {copy.running.undo}
            </button>
          </div>
        ) : (
          <>
            {view.state === "results" ? (
              <div
                data-f="filters"
                role="tablist"
                aria-label={copy.filtersLabel}
                className="box-border flex flex-wrap gap-1 border-b border-border px-3.5 py-2"
              >
                {PALETTE_FILTERS.map((name) => {
                  const count = view.counts[name];
                  const selected = name === filter;
                  return (
                    <button
                      key={name}
                      type="button"
                      role="tab"
                      data-f="fl"
                      aria-selected={selected}
                      aria-controls={listId}
                      tabIndex={-1}
                      onClick={() => {
                        setFilter(name);
                        setSelectedId(null);
                        inputRef.current?.focus();
                      }}
                      className={cn(
                        "inline-flex h-7 cursor-pointer items-center gap-[5px] rounded-full border-0 px-2.5 text-crumb leading-[1.45] font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                        selected
                          ? "bg-foreground font-semibold text-popover dark:text-card"
                          : "bg-transparent text-muted-foreground",
                      )}
                    >
                      {copy.filters[name]}
                      {count ? (
                        <span
                          data-f="flc"
                          className={cn(
                            "text-micro leading-[1.45] opacity-75",
                            selected ? "font-semibold" : "font-medium",
                          )}
                        >
                          {count}
                        </span>
                      ) : null}
                    </button>
                  );
                })}
              </div>
            ) : null}

            {none ? (
              <div
                data-f="empty"
                className="px-[18px] pt-[26px] pb-[18px] text-center text-muted-foreground"
              >
                <b
                  data-f="emptyt"
                  className="mb-1 block text-item-heading font-bold text-foreground"
                >
                  {formatCopy(copy.empty.title, {
                    query: term,
                    workspace: workspace.name,
                  })}
                </b>
                {copy.empty.text}
              </div>
            ) : null}

            <div
              ref={listRef}
              id={listId}
              data-f="res"
              role="listbox"
              aria-label={listLabel}
              className="box-border max-h-[min(534px,calc(100svh-251px))] overflow-auto px-2 pt-1.5 pb-2"
            >
              {view.groups.length === 0 ? (
                <div className="px-2.5 py-6 text-center text-metadata text-meta-foreground">
                  {view.state === "recent"
                    ? copy.recentsEmpty
                    : copy.empty.text}
                </div>
              ) : null}
              {view.groups.map((group) => (
                <React.Fragment key={group.id}>
                  {group.label ? (
                    <div
                      data-f="gh"
                      role="presentation"
                      className="flex items-center gap-2 px-2.5 pt-2.5 pb-1 text-metadata leading-[1.45] font-semibold tracking-normal text-meta-foreground"
                    >
                      {group.label}
                      {group.count !== null ? (
                        <span data-f="ghn" className="font-medium">
                          {group.count}
                        </span>
                      ) : null}
                    </div>
                  ) : null}
                  {group.rows.map((row) => {
                    const selected = row.id === activeId;
                    return (
                      <div
                        key={row.id}
                        id={optionDomId(row.id)}
                        data-f="r"
                        data-row-id={row.id}
                        role="option"
                        aria-selected={selected}
                        aria-disabled={row.unavailable ? true : undefined}
                        title={row.unavailable ?? undefined}
                        onClick={() => choose(row)}
                        className={cn(
                          "box-border flex min-h-[60px] cursor-pointer items-center gap-3 rounded-[9px] px-2.5 py-1.5",
                          selected ? "bg-row-selected" : "hover:bg-state-hover",
                          row.unavailable && "cursor-not-allowed",
                        )}
                      >
                        <RowIcon row={row} />
                        <span data-f="body" className="min-w-0 flex-1">
                          <span
                            data-f="t"
                            className="block truncate text-body leading-[1.45] font-medium tracking-normal"
                          >
                            <Highlighted
                              text={row.title}
                              term={
                                row.id === "command:create-item-named"
                                  ? ""
                                  : term
                              }
                            />
                          </span>
                          <CtxLine pieces={row.ctx} />
                        </span>
                        <RowEnd row={row} selected={selected} copy={copy} />
                      </div>
                    );
                  })}
                </React.Fragment>
              ))}
            </div>
          </>
        )}

        <div
          data-f="foot"
          className="box-border flex flex-wrap items-center gap-3 border-t border-border bg-surface-canvas px-4 py-[9px] text-metadata leading-[1.45] font-normal tracking-normal text-meta-foreground"
        >
          <FootHint keys={["↑", "↓"]}>{copy.foot.select}</FootHint>
          <FootHint keys={["↵"]}>{copy.foot.open}</FootHint>
          <FootHint keys={["⌘↵"]} unavailable={copy.foot.sideUnavailable}>
            {copy.foot.side}
          </FootHint>
          <FootHint keys={["Tab"]}>{copy.foot.filter}</FootHint>
          <span className="flex-1" />
          <FootHint keys={[">"]}>{copy.foot.command}</FootHint>
          <FootHint keys={["@"]}>{copy.foot.people}</FootHint>
          <FootHint keys={["#"]}>{copy.foot.board}</FootHint>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function FootHint({
  keys,
  unavailable,
  children,
}: {
  keys: readonly string[];
  unavailable?: string;
  children: React.ReactNode;
}) {
  return (
    <span
      data-f="fh"
      aria-disabled={unavailable ? true : undefined}
      title={unavailable}
      className={cn(
        "inline-flex items-center gap-[5px]",
        unavailable && "opacity-60",
      )}
    >
      {keys.map((key) => (
        <Key key={key}>{key}</Key>
      ))}
      {children}
    </span>
  );
}
