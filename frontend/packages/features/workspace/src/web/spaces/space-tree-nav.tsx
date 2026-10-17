import * as React from "react";
import {
  Archive,
  Briefcase,
  Building2,
  ChevronDown,
  Folder,
  FolderKanban,
  Lock,
  Plus,
  type LucideIcon,
} from "lucide-react";
import {
  Button,
  SidebarGroup,
  SidebarGroupContent,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import {
  buildSpaceIndex,
  childrenOf,
  type SpaceIndex,
  type SpaceNode,
  type SpaceType,
} from "../../core/model/spaces";
import {
  markToneFor,
  type MarkToneId,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import {
  isPlainClick,
  SIDEBAR_SECTION_LABEL_CLASS,
  type ShellNavigation,
} from "../shell/shell-models";
import { IdentityMark } from "../shell/shell-link";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

export type SpaceTreeState =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "ready"; readonly nodes: readonly SpaceNode[] };

/** A Board the member can open, listed under its Space in the sidebar. */
export interface SpaceBoardLink {
  readonly id: string;
  readonly name: string;
  readonly isPrivate: boolean;
}

export interface SpaceTreeNavProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly navigation: ShellNavigation;
  readonly state: SpaceTreeState;
  /** Expanded Space ids (the host persists them per member). */
  readonly expandedIds: ReadonlySet<string>;
  /** The Space of the current page, when it is a Space page. */
  readonly activeSpaceId: string | null;
  /** The Board of the current page, when it is a Board page. */
  readonly activeBoardId?: string | null;
  /**
   * Boards per Space id, from the authorized Board list (never guessed).
   * They render under their Space while it is expanded, before its
   * sub-Spaces; omitted = the tree shows Spaces only.
   */
  readonly boardsBySpace?: ReadonlyMap<string, readonly SpaceBoardLink[]>;
  /** Workspace owners and admins create top-level Spaces (GOV-028). */
  readonly canCreateRoot: boolean;
  readonly onExpandedChange: (spaceId: string, expanded: boolean) => void;
  readonly onCreateSpace: () => void;
  readonly onRetry: () => void;
  /** Called after a plain-click navigation (closes the mobile drawer). */
  readonly onNavigated?: () => void;
  /** Visible rows above this count are windowed (stories and tests lower it). */
  readonly windowThreshold?: number;
}

/**
 * Row boxes of Final-Space `.side` (px): a top-level Space row is 32 high,
 * every nested row (Board, sub-Space) 30; rows sit 2 px apart.
 */
const TOP_ROW_HEIGHT = 32;
const NESTED_ROW_HEIGHT = 30;
const ROW_GAP = 2;
/** Height of the windowed scroll region (`max-h-96`). */
const WINDOW_HEIGHT = 384;
/** Rows rendered beyond the visible ones on each side while windowed. */
const WINDOW_OVERSCAN = 8;
/** Inline start of a nested row's content, and the step per further level (px). */
const NESTED_START = 39;
const INDENT = 20;
export const SPACE_TREE_WINDOW_THRESHOLD = 150;

export const SPACE_TYPE_ICON: Readonly<Record<SpaceType, LucideIcon>> = {
  Department: Building2,
  Folder,
  Portfolio: Briefcase,
  ProjectArea: FolderKanban,
};

const NO_BOARDS: readonly SpaceBoardLink[] = [];

interface TreeEntryBase {
  readonly key: string;
  /** 1-based level (`aria-level`). */
  readonly level: number;
  readonly posInSet: number;
  readonly setSize: number;
  /** Key of the parent Space entry (`null` at the root). */
  readonly parentKey: string | null;
}

interface SpaceEntry extends TreeEntryBase {
  readonly kind: "space";
  readonly node: SpaceNode;
  readonly expandable: boolean;
  readonly expanded: boolean;
  /** The top-level department's tone, shared by its sub-Spaces. */
  readonly tone: MarkToneId;
}

interface BoardEntry extends TreeEntryBase {
  readonly kind: "board";
  readonly spaceId: string;
  readonly board: SpaceBoardLink;
}

type TreeEntry = SpaceEntry | BoardEntry;

function spaceKey(id: string): string {
  return `space:${id}`;
}

function entryName(entry: TreeEntry): string {
  return entry.kind === "space" ? entry.node.name : entry.board.name;
}

/**
 * The rows the tree shows: roots, then — for expanded Spaces only — their
 * Boards followed by their sub-Spaces. Collapsed subtrees are never visited,
 * so a large tree costs only what is open (depth is unlimited, WSP-021).
 */
function visibleTreeEntries(
  index: SpaceIndex,
  expanded: ReadonlySet<string>,
  boardsBySpace: SpaceTreeNavProps["boardsBySpace"],
): TreeEntry[] {
  const entries: TreeEntry[] = [];
  const visit = (
    parent: SpaceNode | null,
    level: number,
    offset: number,
    path: Set<string>,
    tone: MarkToneId | null,
  ) => {
    const siblings = childrenOf(index, parent?.id ?? null);
    siblings.forEach((node, position) => {
      if (path.has(node.id)) return;
      const boards = boardsBySpace?.get(node.id) ?? NO_BOARDS;
      const children = childrenOf(index, node.id);
      const expandable = children.length > 0 || boards.length > 0;
      const open = expandable && expanded.has(node.id);
      const nodeTone = tone ?? markToneFor(node.id);
      entries.push({
        kind: "space",
        key: spaceKey(node.id),
        node,
        level,
        posInSet: offset + position + 1,
        setSize: offset + siblings.length,
        parentKey: parent ? spaceKey(parent.id) : null,
        expandable,
        expanded: open,
        tone: nodeTone,
      });
      if (!open) return;
      const childSetSize = boards.length + children.length;
      boards.forEach((board, boardPosition) => {
        entries.push({
          kind: "board",
          key: `board:${node.id}:${board.id}`,
          spaceId: node.id,
          board,
          level: level + 1,
          posInSet: boardPosition + 1,
          setSize: childSetSize,
          parentKey: spaceKey(node.id),
        });
      });
      path.add(node.id);
      visit(node, level + 1, boards.length, path, nodeTone);
      path.delete(node.id);
    });
  };
  visit(null, 1, 0, new Set(), null);
  return entries;
}

/** Inline start of the content of a row at this level (below the top level). */
function nestedInset(level: number): number {
  return NESTED_START + (level - 2) * INDENT;
}

function rowHeight(entry: TreeEntry): number {
  return entry.level === 1 ? TOP_ROW_HEIGHT : NESTED_ROW_HEIGHT;
}

/** Top offsets and the total height of the rows laid out with {@link ROW_GAP}. */
function rowLayout(rows: readonly TreeEntry[]) {
  const tops: number[] = [];
  let offset = 0;
  for (const row of rows) {
    tops.push(offset);
    offset += rowHeight(row) + ROW_GAP;
  }
  return { tops, total: offset };
}

/** The window of rows around a scroll position (rows differ in height). */
function windowRange(
  rows: readonly TreeEntry[],
  tops: readonly number[],
  scrollTop: number,
): { readonly start: number; readonly end: number } {
  if (rows.length === 0) return { start: 0, end: 0 };
  const bottom = scrollTop + WINDOW_HEIGHT;
  let first = 0;
  while (first < rows.length - 1 && tops[first + 1]! <= scrollTop) first += 1;
  let last = first;
  while (last < rows.length - 1 && tops[last + 1]! < bottom) last += 1;
  return {
    start: Math.max(0, first - WINDOW_OVERSCAN),
    end: Math.min(rows.length, last + 1 + WINDOW_OVERSCAN),
  };
}

const ROW_CLASS =
  "flex w-full min-w-0 items-center rounded-[7px] text-left outline-none hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 focus-visible:ring-focus-ring";
const ACTIVE_ROW_CLASS =
  "bg-state-selected font-semibold text-state-selected-foreground hover:bg-state-selected hover:text-state-selected-foreground";

interface RowProps<T extends TreeEntry> {
  entry: T;
  copy: WorkspaceSpacesCopy;
  navigation: ShellNavigation;
  active: boolean;
  focusable: boolean;
  onToggle: (entry: SpaceEntry) => void;
  onKeyDown: (event: React.KeyboardEvent, entry: TreeEntry) => void;
  onFocusEntry: (entry: TreeEntry) => void;
  onNavigated?: () => void;
}

function navigateOnPlainClick(
  event: React.MouseEvent,
  destination: WorkspaceDestination,
  navigation: ShellNavigation,
  onNavigated?: () => void,
) {
  if (!isPlainClick(event)) return;
  event.preventDefault();
  navigation.onNavigate(destination);
  onNavigated?.();
}

function PrivateMarker({ label }: { label: string }) {
  return (
    <span className="flex shrink-0 items-center text-meta-foreground">
      <Lock aria-hidden="true" className="size-3.25 stroke-[1.8]" />
      <span className="sr-only">, {label}</span>
    </span>
  );
}

function SpaceRow({
  entry,
  copy,
  navigation,
  active,
  focusable,
  onToggle,
  onKeyDown,
  onFocusEntry,
  onNavigated,
}: RowProps<SpaceEntry>) {
  const { node } = entry;
  const destination: WorkspaceDestination = {
    section: "space",
    spaceId: node.id,
  };
  const top = entry.level === 1;
  return (
    <div
      role="none"
      className={cn(
        "flex w-full items-center gap-0.5 group-data-[collapsible=icon]:justify-center",
        !top && "group-data-[collapsible=icon]:hidden",
      )}
    >
      <a
        role="treeitem"
        href={navigation.getHref(destination)}
        data-tree-key={entry.key}
        data-space-id={node.id}
        aria-level={entry.level}
        aria-posinset={entry.posInSet}
        aria-setsize={entry.setSize}
        aria-expanded={entry.expandable ? entry.expanded : undefined}
        aria-selected={active}
        aria-current={active ? "page" : undefined}
        tabIndex={focusable ? 0 : -1}
        onFocus={() => onFocusEntry(entry)}
        onKeyDown={(event) => onKeyDown(event, entry)}
        onClick={(event) =>
          navigateOnPlainClick(event, destination, navigation, onNavigated)
        }
        style={
          top
            ? undefined
            : { paddingInlineStart: `${nestedInset(entry.level)}px` }
        }
        className={cn(
          ROW_CLASS,
          "min-w-0 flex-1",
          top
            ? "h-8 gap-2.25 px-2.5 text-nav text-sidebar-foreground group-data-[tablet=true]:h-11 group-data-[tablet=true]:gap-2.5 group-data-[tablet=true]:rounded-[10px] group-data-[tablet=true]:px-3 group-data-[tablet=true]:text-label group-data-[collapsible=icon]:size-11! group-data-[collapsible=icon]:flex-none group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:p-0!"
            : "h-7.5 gap-2 pr-2.5 text-compact text-muted-foreground group-data-[tablet=true]:h-11 group-data-[tablet=true]:gap-2.5 group-data-[tablet=true]:rounded-[10px] group-data-[tablet=true]:text-label",
          active && ACTIVE_ROW_CLASS,
          node.isArchived && "text-muted-foreground",
        )}
      >
        <IdentityMark
          name={node.name}
          markTone={entry.tone}
          size="sm"
          className={cn(
            "leading-none font-bold",
            top
              ? "size-5 rounded-[6px] text-mark-xs group-data-[tablet=true]:size-5.5 group-data-[collapsible=icon]:size-7! group-data-[collapsible=icon]:rounded-lg"
              : "size-4 rounded-[5px] text-mark-2xs group-data-[tablet=true]:size-4.5",
          )}
        />
        <span
          className="min-w-0 flex-1 truncate group-data-[collapsible=icon]:sr-only"
          title={node.name}
        >
          {node.name}
        </span>
        {node.visibility === "Private" ? (
          <span className="flex group-data-[collapsible=icon]:hidden">
            <PrivateMarker label={copy.tree.private} />
          </span>
        ) : null}
        {node.isArchived ? (
          <span className="flex shrink-0 items-center text-muted-foreground">
            <Archive aria-hidden="true" className="size-3.5" />
            <span className="sr-only">, {copy.tree.archived}</span>
          </span>
        ) : null}
      </a>
      {entry.expandable ? (
        <span
          data-tree-toggle=""
          aria-hidden="true"
          onClick={() => onToggle(entry)}
          className="flex size-6 shrink-0 cursor-pointer items-center justify-center rounded-[6px] text-meta-foreground hover:bg-sidebar-accent hover:text-foreground group-data-[tablet=true]:size-11 group-data-[collapsible=icon]:hidden"
        >
          <ChevronDown
            className={cn(
              "size-3.5 stroke-[1.8] transition-transform duration-fast",
              !entry.expanded && "-rotate-90",
            )}
          />
        </span>
      ) : null}
    </div>
  );
}

function BoardRow({
  entry,
  copy,
  navigation,
  active,
  focusable,
  onKeyDown,
  onFocusEntry,
  onNavigated,
}: RowProps<BoardEntry>) {
  const { board } = entry;
  const destination: WorkspaceDestination = {
    section: "board",
    boardId: board.id,
  };
  return (
    <a
      role="treeitem"
      href={navigation.getHref(destination)}
      data-tree-key={entry.key}
      data-board-id={board.id}
      aria-level={entry.level}
      aria-posinset={entry.posInSet}
      aria-setsize={entry.setSize}
      aria-selected={active}
      aria-current={active ? "page" : undefined}
      tabIndex={focusable ? 0 : -1}
      onFocus={() => onFocusEntry(entry)}
      onKeyDown={(event) => onKeyDown(event, entry)}
      onClick={(event) =>
        navigateOnPlainClick(event, destination, navigation, onNavigated)
      }
      style={{ paddingInlineStart: `${nestedInset(entry.level)}px` }}
      className={cn(
        ROW_CLASS,
        "h-7.5 gap-2 pr-2.5 text-compact text-muted-foreground group-data-[tablet=true]:h-11 group-data-[tablet=true]:gap-2.5 group-data-[tablet=true]:rounded-[10px] group-data-[tablet=true]:text-label group-data-[collapsible=icon]:hidden",
        active && ACTIVE_ROW_CLASS,
      )}
    >
      <span className="min-w-0 flex-1 truncate" title={board.name}>
        {board.name}
      </span>
      {board.isPrivate ? <PrivateMarker label={copy.tree.private} /> : null}
    </a>
  );
}

/**
 * The department (Space) tree in the workspace sidebar (OT-5): a WAI-ARIA
 * navigation tree (roving focus; ↑ ↓ move, → opens or enters, ← closes or
 * goes to the parent, Home/End, type-ahead). An expanded Space lists its
 * Boards, then its sub-Spaces. Only expanded subtrees are rendered, and
 * above the threshold the visible rows are windowed inside their own scroll
 * region, so depth and size are unlimited (WSP-021). Private Spaces and
 * Boards carry a lock marker; which actions show is governed by the caller's
 * access.
 */
export function SpaceTreeNav({
  copy,
  navigation,
  state,
  expandedIds,
  activeSpaceId,
  activeBoardId = null,
  boardsBySpace,
  canCreateRoot,
  onExpandedChange,
  onCreateSpace,
  onRetry,
  onNavigated,
  windowThreshold = SPACE_TREE_WINDOW_THRESHOLD,
}: SpaceTreeNavProps) {
  const hintId = React.useId();
  const scrollRef = React.useRef<HTMLDivElement>(null);
  const treeRef = React.useRef<HTMLDivElement>(null);
  const focusPending = React.useRef<string | null>(null);
  const typeAhead = React.useRef({ text: "", at: 0 });
  const [focusedKey, setFocusedKey] = React.useState<string | null>(null);
  const [scrollTop, setScrollTop] = React.useState(0);

  const nodes = state.status === "ready" ? state.nodes : null;
  const index = React.useMemo(() => buildSpaceIndex(nodes ?? []), [nodes]);
  const rows = React.useMemo(
    () => visibleTreeEntries(index, expandedIds, boardsBySpace),
    [index, expandedIds, boardsBySpace],
  );
  const rowIndexByKey = React.useMemo(
    () => new Map(rows.map((row, position) => [row.key, position])),
    [rows],
  );

  const isActive = (entry: TreeEntry) =>
    entry.kind === "space"
      ? entry.node.id === activeSpaceId
      : entry.board.id === activeBoardId;

  // The focusable row: the focused one if still visible, else the current
  // Space or Board, else the first row.
  const activeKey = rows.find(isActive)?.key ?? null;
  const tabStopKey =
    (focusedKey && rowIndexByKey.has(focusedKey) ? focusedKey : null) ??
    activeKey ??
    rows[0]?.key ??
    null;

  const windowed = rows.length > windowThreshold;
  const layout = React.useMemo(() => rowLayout(rows), [rows]);
  const range = windowed
    ? windowRange(rows, layout.tops, scrollTop)
    : { start: 0, end: rows.length };

  React.useEffect(() => {
    const key = focusPending.current;
    if (!key) return;
    const element = Array.from(
      treeRef.current?.querySelectorAll<HTMLElement>("[data-tree-key]") ?? [],
    ).find((candidate) => candidate.dataset.treeKey === key);
    if (element) {
      focusPending.current = null;
      element.focus();
    }
  });

  const focusRow = (position: number) => {
    const target = rows[Math.max(0, Math.min(rows.length - 1, position))];
    if (!target) return;
    setFocusedKey(target.key);
    focusPending.current = target.key;
    const region = scrollRef.current;
    if (region && windowed) {
      const top = layout.tops[position] ?? 0;
      const bottom = top + rowHeight(target);
      if (top < region.scrollTop) region.scrollTop = top;
      else if (bottom > region.scrollTop + region.clientHeight) {
        region.scrollTop = bottom - region.clientHeight;
      }
      setScrollTop(region.scrollTop);
    }
  };

  const toggle = (entry: SpaceEntry) => {
    if (entry.expandable) onExpandedChange(entry.node.id, !entry.expanded);
  };

  const onKeyDown = (event: React.KeyboardEvent, entry: TreeEntry) => {
    const position = rowIndexByKey.get(entry.key) ?? 0;
    switch (event.key) {
      case "ArrowDown":
        event.preventDefault();
        focusRow(position + 1);
        return;
      case "ArrowUp":
        event.preventDefault();
        focusRow(position - 1);
        return;
      case "Home":
        event.preventDefault();
        focusRow(0);
        return;
      case "End":
        event.preventDefault();
        focusRow(rows.length - 1);
        return;
      case "ArrowRight":
        event.preventDefault();
        if (entry.kind !== "space" || !entry.expandable) return;
        if (!entry.expanded) onExpandedChange(entry.node.id, true);
        else focusRow(position + 1);
        return;
      case "ArrowLeft": {
        event.preventDefault();
        if (entry.kind === "space" && entry.expandable && entry.expanded) {
          onExpandedChange(entry.node.id, false);
          return;
        }
        const parentPosition =
          entry.parentKey !== null
            ? rowIndexByKey.get(entry.parentKey)
            : undefined;
        if (parentPosition !== undefined) focusRow(parentPosition);
        return;
      }
      default:
        break;
    }
    if (
      event.key.length === 1 &&
      !event.ctrlKey &&
      !event.metaKey &&
      !event.altKey &&
      event.key.trim() !== ""
    ) {
      const now = event.timeStamp;
      const buffer = typeAhead.current;
      buffer.text = now - buffer.at < 700 ? buffer.text + event.key : event.key;
      buffer.at = now;
      const needle = buffer.text.toLocaleLowerCase();
      for (let step = 1; step <= rows.length; step += 1) {
        const candidate = (position + step) % rows.length;
        const name = entryName(rows[candidate]!).toLocaleLowerCase();
        if (name.startsWith(needle)) {
          focusRow(candidate);
          break;
        }
      }
    }
  };

  let body: React.ReactNode;
  if (state.status === "loading") {
    body = (
      <div
        role="status"
        aria-label={copy.tree.loading}
        className="flex flex-col gap-1 px-2 py-1"
      >
        {["w-2/3", "w-1/2", "w-3/4"].map((width) => (
          <div key={width} className="flex h-8 items-center gap-2">
            <Skeleton className="size-4 rounded-sm" />
            <Skeleton className={`h-3.5 ${width}`} />
          </div>
        ))}
      </div>
    );
  } else if (state.status === "error") {
    body = (
      <div className="flex items-center justify-between gap-2 px-2 py-1.5">
        <p className="text-metadata text-muted-foreground">{copy.tree.error}</p>
        <Button variant="ghost" size="xs" onClick={onRetry}>
          {copy.tree.retry}
        </Button>
      </div>
    );
  } else if (rows.length === 0) {
    body = (
      <p className="px-2 py-1.5 text-metadata text-muted-foreground">
        {canCreateRoot ? copy.tree.emptyManager : copy.tree.empty}
      </p>
    );
  } else {
    const shown = rows.slice(range.start, range.end);
    const tree = (
      <div
        ref={treeRef}
        role="tree"
        aria-label={copy.tree.label}
        aria-describedby={hintId}
        className="flex flex-col gap-0.5"
      >
        {windowed && range.start > 0 ? (
          <div
            role="none"
            aria-hidden="true"
            style={{ height: (layout.tops[range.start] ?? 0) - ROW_GAP }}
          />
        ) : null}
        {shown.map((entry) => {
          const rowProps = {
            copy,
            navigation,
            active: isActive(entry),
            focusable: entry.key === tabStopKey,
            onToggle: toggle,
            onKeyDown,
            onFocusEntry: (focused: TreeEntry) => setFocusedKey(focused.key),
            onNavigated,
          };
          return entry.kind === "space" ? (
            <SpaceRow key={entry.key} entry={entry} {...rowProps} />
          ) : (
            <BoardRow key={entry.key} entry={entry} {...rowProps} />
          );
        })}
        {windowed && range.end < rows.length ? (
          <div
            role="none"
            aria-hidden="true"
            style={{
              height: layout.total - (layout.tops[range.end] ?? 0) - ROW_GAP,
            }}
          />
        ) : null}
      </div>
    );
    body = (
      <>
        <p id={hintId} className="sr-only">
          {copy.tree.keyboardHint}
        </p>
        {windowed ? (
          <div
            ref={scrollRef}
            data-testid="space-tree-window"
            onScroll={(event) => setScrollTop(event.currentTarget.scrollTop)}
            className="max-h-96 overflow-y-auto overscroll-contain"
          >
            {tree}
          </div>
        ) : (
          tree
        )}
      </>
    );
  }

  return (
    <SidebarGroup className="p-0">
      <div className={SIDEBAR_SECTION_LABEL_CLASS}>
        <span>{copy.tree.title}</span>
        {canCreateRoot ? (
          <button
            type="button"
            onClick={onCreateSpace}
            aria-label={copy.tree.create}
            title={copy.tree.create}
            className="flex size-6 items-center justify-center rounded-[6px] text-meta-foreground outline-none group-data-[tablet=true]:size-11 hover:bg-sidebar-accent hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <Plus aria-hidden="true" className="size-4 stroke-[1.8]" />
          </button>
        ) : null}
      </div>
      <SidebarGroupContent>{body}</SidebarGroupContent>
    </SidebarGroup>
  );
}
