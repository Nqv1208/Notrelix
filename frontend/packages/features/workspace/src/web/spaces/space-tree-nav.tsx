import * as React from "react";
import {
  Archive,
  Briefcase,
  Building2,
  ChevronRight,
  Folder,
  FolderKanban,
  Lock,
  Plus,
  type LucideIcon,
} from "lucide-react";
import {
  Button,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  SidebarGroup,
  SidebarGroupAction,
  SidebarGroupContent,
  SidebarGroupLabel,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import { libraryWindow } from "../../core/model/library";
import {
  buildSpaceIndex,
  visibleSpaceRows,
  type SpaceNode,
  type SpaceTreeRow,
  type SpaceType,
} from "../../core/model/spaces";
import type { WorkspaceDestination } from "../../core/model/navigation";
import { isPlainClick, type ShellNavigation } from "../shell/shell-models";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

export type SpaceTreeState =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "ready"; readonly nodes: readonly SpaceNode[] };

export interface SpaceTreeNavProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly navigation: ShellNavigation;
  readonly state: SpaceTreeState;
  /** Expanded Space ids (the host persists them per member). */
  readonly expandedIds: ReadonlySet<string>;
  /** The Space of the current page, when it is a Space page. */
  readonly activeSpaceId: string | null;
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

/** Fixed row height (`h-8`), used by windowing. */
const ROW_HEIGHT = 32;
/** Height of the windowed scroll region (`max-h-96`). */
const WINDOW_HEIGHT = 384;
const INDENT = 12;
export const SPACE_TREE_WINDOW_THRESHOLD = 150;

export const SPACE_TYPE_ICON: Readonly<Record<SpaceType, LucideIcon>> = {
  Department: Building2,
  Folder,
  Portfolio: Briefcase,
  ProjectArea: FolderKanban,
};

function destinationFor(spaceId: string): WorkspaceDestination {
  return { section: "space", spaceId };
}

function TreeRow({
  row,
  copy,
  navigation,
  active,
  focusable,
  onToggle,
  onKeyDown,
  onFocusRow,
  onNavigated,
}: {
  row: SpaceTreeRow;
  copy: WorkspaceSpacesCopy;
  navigation: ShellNavigation;
  active: boolean;
  focusable: boolean;
  onToggle: (row: SpaceTreeRow) => void;
  onKeyDown: (event: React.KeyboardEvent, row: SpaceTreeRow) => void;
  onFocusRow: (row: SpaceTreeRow) => void;
  onNavigated?: () => void;
}) {
  const { node } = row;
  const Icon = SPACE_TYPE_ICON[node.type];
  const destination = destinationFor(node.id);
  return (
    <a
      role="treeitem"
      href={navigation.getHref(destination)}
      data-space-id={node.id}
      aria-level={row.level}
      aria-posinset={row.posInSet}
      aria-setsize={row.setSize}
      aria-expanded={row.hasChildren ? row.expanded : undefined}
      aria-selected={active}
      aria-current={active ? "page" : undefined}
      tabIndex={focusable ? 0 : -1}
      onFocus={() => onFocusRow(row)}
      onKeyDown={(event) => onKeyDown(event, row)}
      onClick={(event) => {
        const toggle = (event.target as Element).closest("[data-tree-toggle]");
        if (toggle) {
          event.preventDefault();
          onToggle(row);
          return;
        }
        if (!isPlainClick(event)) return;
        event.preventDefault();
        navigation.onNavigate(destination);
        onNavigated?.();
      }}
      style={{ paddingInlineStart: `${(row.level - 1) * INDENT + 4}px` }}
      className={cn(
        "group/space flex h-8 w-full min-w-0 items-center gap-1.5 rounded-md pr-2 text-left text-sm text-sidebar-foreground outline-none",
        "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 focus-visible:ring-focus-ring",
        active &&
          "bg-state-selected font-medium text-state-selected-foreground hover:bg-state-selected hover:text-state-selected-foreground",
        node.isArchived && "text-muted-foreground",
      )}
    >
      <span
        data-tree-toggle={row.hasChildren ? "" : undefined}
        aria-hidden="true"
        className={cn(
          "flex size-5 shrink-0 items-center justify-center rounded-sm text-muted-foreground",
          row.hasChildren && "hover:bg-state-hover hover:text-foreground",
        )}
      >
        {row.hasChildren ? (
          <ChevronRight
            className={cn(
              "size-3.5 transition-transform duration-fast",
              row.expanded && "rotate-90",
            )}
          />
        ) : null}
      </span>
      <Icon aria-hidden="true" className="size-4 shrink-0 text-icon" />
      <span className="min-w-0 flex-1 truncate" title={node.name}>
        {node.name}
      </span>
      {node.visibility === "Private" ? (
        <span className="flex shrink-0 items-center text-muted-foreground">
          <Lock aria-hidden="true" className="size-3.5" />
          <span className="sr-only">, {copy.tree.private}</span>
        </span>
      ) : null}
      {node.isArchived ? (
        <span className="flex shrink-0 items-center text-muted-foreground">
          <Archive aria-hidden="true" className="size-3.5" />
          <span className="sr-only">, {copy.tree.archived}</span>
        </span>
      ) : null}
    </a>
  );
}

/**
 * The Space org tree in the workspace sidebar (OT-5): a WAI-ARIA navigation
 * tree (roving focus; ↑ ↓ move, → opens or enters, ← closes or goes to the
 * parent, Home/End, type-ahead). Only expanded subtrees are rendered, and
 * above the threshold the visible rows are windowed inside their own scroll
 * region, so depth and size are unlimited (WSP-021). Private Spaces carry a
 * lock marker; which actions show is governed by the caller's access.
 */
export function SpaceTreeNav({
  copy,
  navigation,
  state,
  expandedIds,
  activeSpaceId,
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
  const [focusedId, setFocusedId] = React.useState<string | null>(null);
  const [scrollTop, setScrollTop] = React.useState(0);

  const nodes = state.status === "ready" ? state.nodes : null;
  const index = React.useMemo(() => buildSpaceIndex(nodes ?? []), [nodes]);
  const rows = React.useMemo(
    () => visibleSpaceRows(index, expandedIds),
    [index, expandedIds],
  );
  const rowIndexById = React.useMemo(
    () => new Map(rows.map((row, position) => [row.node.id, position])),
    [rows],
  );

  // The focusable row: the focused one if still visible, else the current
  // Space, else the first row.
  const tabStopId =
    (focusedId && rowIndexById.has(focusedId) ? focusedId : null) ??
    (activeSpaceId && rowIndexById.has(activeSpaceId) ? activeSpaceId : null) ??
    rows[0]?.node.id ??
    null;

  const windowed = rows.length > windowThreshold;
  const range = windowed
    ? libraryWindow({
        rowCount: rows.length,
        rowHeight: ROW_HEIGHT,
        scrollTop,
        viewportHeight: WINDOW_HEIGHT,
      })
    : { start: 0, end: rows.length };

  React.useEffect(() => {
    const id = focusPending.current;
    if (!id) return;
    const node = Array.from(
      treeRef.current?.querySelectorAll<HTMLElement>("[data-space-id]") ?? [],
    ).find((element) => element.dataset.spaceId === id);
    if (node) {
      focusPending.current = null;
      node.focus();
    }
  });

  const focusRow = (position: number) => {
    const target = rows[Math.max(0, Math.min(rows.length - 1, position))];
    if (!target) return;
    setFocusedId(target.node.id);
    focusPending.current = target.node.id;
    const region = scrollRef.current;
    if (region && windowed) {
      const top = position * ROW_HEIGHT;
      if (top < region.scrollTop) region.scrollTop = top;
      else if (top + ROW_HEIGHT > region.scrollTop + region.clientHeight) {
        region.scrollTop = top + ROW_HEIGHT - region.clientHeight;
      }
      setScrollTop(region.scrollTop);
    }
  };

  const toggle = (row: SpaceTreeRow) => {
    if (row.hasChildren) onExpandedChange(row.node.id, !row.expanded);
  };

  const onKeyDown = (event: React.KeyboardEvent, row: SpaceTreeRow) => {
    const position = rowIndexById.get(row.node.id) ?? 0;
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
        if (!row.hasChildren) return;
        if (!row.expanded) onExpandedChange(row.node.id, true);
        else focusRow(position + 1);
        return;
      case "ArrowLeft": {
        event.preventDefault();
        if (row.hasChildren && row.expanded) {
          onExpandedChange(row.node.id, false);
          return;
        }
        const parentId = row.node.parentId;
        const parentPosition =
          parentId !== null ? rowIndexById.get(parentId) : undefined;
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
        const name = rows[candidate]!.node.name.toLocaleLowerCase();
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
        className="flex flex-col gap-px"
      >
        {windowed && range.start > 0 ? (
          <div
            role="none"
            aria-hidden="true"
            style={{ height: range.start * ROW_HEIGHT }}
          />
        ) : null}
        {shown.map((row) => (
          <TreeRow
            key={row.node.id}
            row={row}
            copy={copy}
            navigation={navigation}
            active={row.node.id === activeSpaceId}
            focusable={row.node.id === tabStopId}
            onToggle={toggle}
            onKeyDown={onKeyDown}
            onFocusRow={(focused) => setFocusedId(focused.node.id)}
            onNavigated={onNavigated}
          />
        ))}
        {windowed && range.end < rows.length ? (
          <div
            role="none"
            aria-hidden="true"
            style={{ height: (rows.length - range.end) * ROW_HEIGHT }}
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
    <Collapsible defaultOpen className="group/spaces">
      <SidebarGroup className="py-1">
        <SidebarGroupLabel asChild>
          <CollapsibleTrigger
            aria-label={copy.tree.toggle}
            className="w-full gap-1 text-metadata font-medium text-muted-foreground hover:text-foreground focus-visible:ring-focus-ring"
          >
            <ChevronRight
              aria-hidden="true"
              className="transition-transform duration-fast group-data-[state=open]/spaces:rotate-90"
            />
            <span>{copy.tree.title}</span>
          </CollapsibleTrigger>
        </SidebarGroupLabel>
        {canCreateRoot ? (
          <SidebarGroupAction
            type="button"
            onClick={onCreateSpace}
            aria-label={copy.tree.create}
            title={copy.tree.create}
            className="focus-visible:ring-focus-ring"
          >
            <Plus aria-hidden="true" />
          </SidebarGroupAction>
        ) : null}
        <CollapsibleContent>
          <SidebarGroupContent>{body}</SidebarGroupContent>
        </CollapsibleContent>
      </SidebarGroup>
    </Collapsible>
  );
}
