import * as React from "react";
import {
  ArrowDown,
  ArrowRightLeft,
  ArrowUp,
  ChevronsUpDown,
  Library,
  Pin,
  PinOff,
  Plus,
  Search,
  X,
} from "lucide-react";
import {
  Button,
  Checkbox,
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Spinner,
  ToggleGroup,
  ToggleGroupItem,
  cn,
} from "@notrelix/ui-web";
import {
  DEFAULT_LIBRARY_QUERY,
  LIBRARY_SPACE_ALL,
  LIBRARY_SPACE_NONE,
  LIBRARY_VIRTUALIZATION_THRESHOLD,
  libraryEntryKey,
  libraryWindow,
  nextLibrarySort,
  queryLibrary,
  type LibraryEntry,
  type LibraryQuery,
  type LibrarySortField,
  type LibraryTypeFilter,
} from "../../core/model/library";
import {
  buildSpaceIndex,
  spaceDescendantIds,
  visibleSpaceRows,
  type SpaceNode,
} from "../../core/model/spaces";
import {
  NAVIGATION_RESOURCE_KIND,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import type { SectionState, SurfaceFormatters } from "../home/home-models";
import { ResourceMark } from "../shell/shell-link";
import {
  formatCopy,
  isPlainClick,
  type ShellNavigation,
} from "../shell/shell-models";
import type { WorkspaceLibraryCopy } from "./workspace-library.copy";

export interface WorkspaceLibrarySurfaceProps {
  readonly copy: WorkspaceLibraryCopy;
  readonly navigation: ShellNavigation;
  readonly formatters: SurfaceFormatters;
  /** Reference time for relative labels (epoch ms). */
  readonly now: number;
  readonly workspaceName: string | null;
  readonly state: SectionState<readonly LibraryEntry[]>;
  /** Entries with a pin request in flight. */
  readonly pendingPinKeys: ReadonlySet<string>;
  /** Members and admins create Boards and Docs; guests cannot. */
  readonly canCreate: boolean;
  readonly onCreateBoard: () => void;
  readonly onCreateDoc: () => void;
  /** Pins or unpins one or many entries (bulk actions pass several). */
  readonly onSetPinned: (
    entries: readonly LibraryEntry[],
    pinned: boolean,
  ) => void;
  readonly onRetry: () => void;
  readonly initialQuery?: LibraryQuery;
  /**
   * The visible Space tree (OT-5). When present and not empty, the table can
   * be filtered by Space (a Space includes the Spaces inside it).
   */
  readonly spaces?: readonly SpaceNode[];
  /** Offered on Board rows when the host can move Boards between Spaces. */
  readonly onMoveBoard?: (entry: LibraryEntry) => void;
  /** Offered on root Doc rows (a nested Doc follows its root, WSP §79). */
  readonly onMoveDoc?: (entry: LibraryEntry) => void;
  /** Rows above this count are windowed (stories and tests lower it). */
  readonly virtualizationThreshold?: number;
}

/** Fixed row height of the dense table (`h-10`), used by windowing. */
const ROW_HEIGHT = 40;

const STICKY_CELL = "sticky z-[1] bg-surface-panel";

function pluralCopy(
  forms: { one: string; other: string },
  count: number,
): string {
  return formatCopy(count === 1 ? forms.one : forms.other, { count });
}

function destinationFor(entry: LibraryEntry): WorkspaceDestination {
  return entry.kind === NAVIGATION_RESOURCE_KIND.board
    ? { section: "board", boardId: entry.id }
    : { section: "doc", docId: entry.id };
}

function SortHeader({
  field,
  label,
  query,
  copy,
  onSort,
  className,
}: {
  field: LibrarySortField;
  label: string;
  query: LibraryQuery;
  copy: WorkspaceLibraryCopy;
  onSort: (field: LibrarySortField) => void;
  className?: string;
}) {
  const active = query.sort.field === field;
  const direction = active ? query.sort.direction : null;
  const Icon =
    direction === "asc"
      ? ArrowUp
      : direction === "desc"
        ? ArrowDown
        : ChevronsUpDown;
  return (
    <th
      scope="col"
      aria-sort={
        direction === "asc"
          ? "ascending"
          : direction === "desc"
            ? "descending"
            : "none"
      }
      className={cn(
        "h-9 border-b border-border bg-surface-panel px-2 text-left align-middle font-normal",
        className,
      )}
    >
      <button
        type="button"
        onClick={() => onSort(field)}
        aria-label={formatCopy(copy.sort.by, { column: label })}
        className={cn(
          "-mx-1 inline-flex items-center gap-1 rounded-sm px-1 py-0.5 text-metadata font-medium outline-none",
          "text-muted-foreground hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring active:text-foreground",
          active && "text-foreground",
        )}
      >
        {label}
        <Icon
          aria-hidden="true"
          className={cn("size-3.5", !active && "opacity-60")}
        />
      </button>
    </th>
  );
}

function TableSkeleton({ label }: { label: string }) {
  return (
    <div
      role="status"
      aria-label={label}
      className="overflow-hidden rounded-lg border border-border bg-surface-panel"
    >
      <div className="flex h-9 items-center gap-4 border-b border-border px-3">
        <Skeleton className="h-3 w-24" />
        <Skeleton className="ml-auto h-3 w-16" />
      </div>
      {Array.from({ length: 8 }, (_, index) => (
        <div
          key={index}
          className="flex h-10 items-center gap-3 border-b border-border px-3 last:border-0"
        >
          <Skeleton className="size-4 rounded-sm" />
          <Skeleton className="size-5 rounded-md" />
          <Skeleton className={index % 2 ? "h-3.5 w-1/3" : "h-3.5 w-1/4"} />
          <Skeleton className="ml-auto h-3 w-20" />
        </div>
      ))}
    </div>
  );
}

/**
 * Boards and Docs directory (WX-DIR-001): one dense table with filter by
 * type and text, sortable columns, pin and bulk pin, keyboard row navigation
 * and a sticky name column. Above the threshold, rows are windowed inside
 * the table's own scroll region (DESIGN.md §17.3–17.4).
 */
export function WorkspaceLibrarySurface({
  copy,
  navigation,
  formatters,
  now,
  workspaceName,
  state,
  pendingPinKeys,
  canCreate,
  onCreateBoard,
  onCreateDoc,
  onSetPinned,
  onRetry,
  initialQuery = DEFAULT_LIBRARY_QUERY,
  spaces,
  onMoveBoard,
  onMoveDoc,
  virtualizationThreshold = LIBRARY_VIRTUALIZATION_THRESHOLD,
}: WorkspaceLibrarySurfaceProps) {
  const [query, setQuery] = React.useState<LibraryQuery>(initialQuery);
  const [selected, setSelected] = React.useState<ReadonlySet<string>>(
    () => new Set(),
  );
  const [activeIndex, setActiveIndex] = React.useState(0);
  const [scrollTop, setScrollTop] = React.useState(0);
  const [viewportHeight, setViewportHeight] = React.useState(ROW_HEIGHT * 16);
  const scrollRef = React.useRef<HTMLDivElement>(null);
  const focusPending = React.useRef(false);
  const searchId = React.useId();
  const hintId = React.useId();

  const entries = React.useMemo(
    () => (state.status === "ready" ? state.data : []),
    [state],
  );
  const spaceIndex = React.useMemo(
    () => buildSpaceIndex(spaces ?? []),
    [spaces],
  );
  const spaceOptions = React.useMemo(
    () =>
      visibleSpaceRows(spaceIndex, new Set(spaceIndex.byId.keys())).filter(
        (row) => !row.node.isArchived,
      ),
    [spaceIndex],
  );
  const spaceFilter = query.space ?? LIBRARY_SPACE_ALL;
  const spaceScope = React.useMemo(
    () =>
      spaceFilter === LIBRARY_SPACE_ALL || spaceFilter === LIBRARY_SPACE_NONE
        ? null
        : new Set([
            spaceFilter,
            ...spaceDescendantIds(spaceIndex, spaceFilter),
          ]),
    [spaceFilter, spaceIndex],
  );
  const rows = React.useMemo(
    () => queryLibrary(entries, query, spaceScope),
    [entries, query, spaceScope],
  );
  const rowKeys = React.useMemo(() => rows.map(libraryEntryKey), [rows]);

  // Selection only keeps entries that still exist.
  React.useEffect(() => {
    const existing = new Set(entries.map(libraryEntryKey));
    setSelected((current) => {
      const next = new Set([...current].filter((key) => existing.has(key)));
      return next.size === current.size ? current : next;
    });
  }, [entries]);

  React.useEffect(() => {
    setActiveIndex((index) => Math.min(index, Math.max(0, rows.length - 1)));
  }, [rows.length]);

  const virtualized = rows.length > virtualizationThreshold;

  React.useEffect(() => {
    const node = scrollRef.current;
    if (!node || !virtualized) return;
    const measure = () =>
      setViewportHeight(node.clientHeight || ROW_HEIGHT * 16);
    measure();
    if (typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(measure);
    observer.observe(node);
    return () => observer.disconnect();
  }, [virtualized]);

  const rowWindow = virtualized
    ? libraryWindow({
        rowCount: rows.length,
        rowHeight: ROW_HEIGHT,
        scrollTop,
        viewportHeight,
      })
    : { start: 0, end: rows.length };
  const visibleRows = rows.slice(rowWindow.start, rowWindow.end);

  // Move focus to the active row after a keyboard move (and after the
  // window re-renders to include it).
  React.useEffect(() => {
    if (!focusPending.current) return;
    const node = scrollRef.current?.querySelector<HTMLElement>(
      `[data-row-index="${activeIndex}"] [data-row-link]`,
    );
    if (node) {
      focusPending.current = false;
      node.focus();
    }
  });

  const moveActive = (index: number) => {
    const next = Math.max(0, Math.min(rows.length - 1, index));
    setActiveIndex(next);
    focusPending.current = true;
    const node = scrollRef.current;
    if (node && virtualized) {
      const top = next * ROW_HEIGHT;
      const bottom = top + ROW_HEIGHT;
      // The sticky header covers the first row height of the viewport.
      if (top < node.scrollTop + ROW_HEIGHT) {
        node.scrollTop = Math.max(0, top - ROW_HEIGHT);
      } else if (bottom > node.scrollTop + node.clientHeight) {
        node.scrollTop = bottom - node.clientHeight;
      }
      setScrollTop(node.scrollTop);
    }
  };

  const toggleSelected = (key: string) => {
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  };

  const onRowKeyDown = (event: React.KeyboardEvent, index: number) => {
    const key = rowKeys[index];
    switch (event.key) {
      case "ArrowDown":
        event.preventDefault();
        moveActive(index + 1);
        break;
      case "ArrowUp":
        event.preventDefault();
        moveActive(index - 1);
        break;
      case "Home":
        event.preventDefault();
        moveActive(0);
        break;
      case "End":
        event.preventDefault();
        moveActive(rows.length - 1);
        break;
      case " ":
        if (key) {
          event.preventDefault();
          toggleSelected(key);
        }
        break;
    }
  };

  const selectedEntries = rows.filter((entry) =>
    selected.has(libraryEntryKey(entry)),
  );
  const allShownSelected =
    rows.length > 0 && selectedEntries.length === rows.length;
  const someShownSelected = selectedEntries.length > 0 && !allShownSelected;
  const filtered =
    query.text.trim() !== "" ||
    query.type !== "all" ||
    spaceFilter !== LIBRARY_SPACE_ALL;

  const header = (
    <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div className="flex min-w-0 flex-col gap-1">
        <h1 className="text-page-title text-foreground">{copy.title}</h1>
        <p className="text-body text-muted-foreground">
          {workspaceName
            ? formatCopy(copy.description, { workspace: workspaceName })
            : copy.descriptionLoading}
        </p>
      </div>
      {canCreate ? (
        <div className="flex shrink-0 gap-2">
          <Button size="sm" onClick={onCreateBoard}>
            <Plus aria-hidden="true" />
            {copy.newBoard}
          </Button>
          <Button size="sm" variant="outline" onClick={onCreateDoc}>
            <Plus aria-hidden="true" />
            {copy.newDoc}
          </Button>
        </div>
      ) : null}
    </header>
  );

  let body: React.ReactNode;
  if (state.status === "loading") {
    body = <TableSkeleton label={copy.title} />;
  } else if (state.status === "error") {
    body = (
      <Empty className="border border-border bg-surface-panel">
        <EmptyHeader>
          <EmptyTitle className="text-subheading">
            {copy.error.title}
          </EmptyTitle>
          <EmptyDescription className="text-body">
            {copy.error.description}
          </EmptyDescription>
        </EmptyHeader>
        <EmptyContent>
          <Button variant="outline" size="sm" onClick={onRetry}>
            {copy.error.retry}
          </Button>
        </EmptyContent>
      </Empty>
    );
  } else if (entries.length === 0) {
    body = (
      <Empty className="border border-dashed border-border bg-surface-panel">
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <Library aria-hidden="true" className="text-icon" />
          </EmptyMedia>
          <EmptyTitle className="text-subheading">
            {copy.empty.title}
          </EmptyTitle>
          <EmptyDescription className="text-body">
            {canCreate ? copy.empty.description : copy.empty.readOnly}
          </EmptyDescription>
        </EmptyHeader>
        {canCreate ? (
          <EmptyContent className="flex-row justify-center gap-2">
            <Button size="sm" onClick={onCreateBoard}>
              {copy.newBoard}
            </Button>
            <Button size="sm" variant="outline" onClick={onCreateDoc}>
              {copy.newDoc}
            </Button>
          </EmptyContent>
        ) : null}
      </Empty>
    );
  } else {
    const setType = (value: string) => {
      if (value === "all" || value === "board" || value === "doc") {
        setQuery((current) => ({
          ...current,
          type: value as LibraryTypeFilter,
        }));
      }
    };
    body = (
      <div className="flex flex-col gap-3">
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          <div className="relative sm:w-72">
            <label htmlFor={searchId} className="sr-only">
              {copy.search.label}
            </label>
            <Search
              aria-hidden="true"
              className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground"
            />
            <Input
              id={searchId}
              type="search"
              value={query.text}
              placeholder={copy.search.placeholder}
              onChange={(event) =>
                setQuery((current) => ({
                  ...current,
                  text: event.target.value,
                }))
              }
              className="h-8 pr-8 pl-8"
            />
            {query.text ? (
              <Button
                type="button"
                variant="ghost"
                size="icon-xs"
                aria-label={copy.search.clear}
                onClick={() =>
                  setQuery((current) => ({ ...current, text: "" }))
                }
                className="absolute top-1/2 right-1 -translate-y-1/2"
              >
                <X aria-hidden="true" />
              </Button>
            ) : null}
          </div>
          <ToggleGroup
            type="single"
            variant="outline"
            size="sm"
            value={query.type}
            onValueChange={setType}
            aria-label={copy.types.label}
          >
            <ToggleGroupItem value="all" onClick={() => setType("all")}>
              {copy.types.all}
            </ToggleGroupItem>
            <ToggleGroupItem value="board" onClick={() => setType("board")}>
              {copy.types.board}
            </ToggleGroupItem>
            <ToggleGroupItem value="doc" onClick={() => setType("doc")}>
              {copy.types.doc}
            </ToggleGroupItem>
          </ToggleGroup>
          {spaceOptions.length > 0 ? (
            <Select
              value={spaceFilter}
              onValueChange={(value) =>
                setQuery((current) => ({ ...current, space: value }))
              }
            >
              <SelectTrigger
                size="sm"
                aria-label={copy.spaces.label}
                className="w-full sm:w-56"
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={LIBRARY_SPACE_ALL}>
                  {copy.spaces.all}
                </SelectItem>
                <SelectItem value={LIBRARY_SPACE_NONE}>
                  {copy.spaces.none}
                </SelectItem>
                {spaceOptions.map((row) => (
                  <SelectItem key={row.node.id} value={row.node.id}>
                    <span
                      style={{
                        paddingInlineStart: `${(row.level - 1) * 12}px`,
                      }}
                    >
                      {row.node.name}
                    </span>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          ) : null}
          <p
            className="text-metadata tabular-nums text-muted-foreground sm:ml-auto"
            aria-live="polite"
          >
            {pluralCopy(
              { one: copy.results_one, other: copy.results_other },
              rows.length,
            )}
          </p>
        </div>

        {selectedEntries.length > 0 ? (
          <div
            role="toolbar"
            aria-label={pluralCopy(
              {
                one: copy.selection.count_one,
                other: copy.selection.count_other,
              },
              selectedEntries.length,
            )}
            className="flex flex-wrap items-center gap-2 rounded-md bg-state-selected px-3 py-1.5 text-state-selected-foreground"
          >
            <span className="text-label tabular-nums">
              {pluralCopy(
                {
                  one: copy.selection.count_one,
                  other: copy.selection.count_other,
                },
                selectedEntries.length,
              )}
            </span>
            <Button
              size="xs"
              variant="outline"
              onClick={() => onSetPinned(selectedEntries, true)}
            >
              <Pin aria-hidden="true" />
              {copy.selection.pin}
            </Button>
            <Button
              size="xs"
              variant="outline"
              onClick={() => onSetPinned(selectedEntries, false)}
            >
              <PinOff aria-hidden="true" />
              {copy.selection.unpin}
            </Button>
            <Button
              size="xs"
              variant="ghost"
              className="ml-auto"
              onClick={() => setSelected(new Set())}
            >
              {copy.selection.clear}
            </Button>
          </div>
        ) : null}

        {rows.length === 0 ? (
          <Empty className="border border-dashed border-border bg-surface-panel">
            <EmptyHeader>
              <EmptyTitle className="text-subheading">
                {copy.noResults.title}
              </EmptyTitle>
              <EmptyDescription className="text-body">
                {copy.noResults.description}
              </EmptyDescription>
            </EmptyHeader>
            {filtered ? (
              <EmptyContent>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() =>
                    setQuery((current) => ({
                      ...current,
                      text: "",
                      type: "all",
                      space: LIBRARY_SPACE_ALL,
                    }))
                  }
                >
                  {copy.noResults.clear}
                </Button>
              </EmptyContent>
            ) : null}
          </Empty>
        ) : (
          <>
            <p id={hintId} className="sr-only">
              {copy.keyboardHint}
            </p>
            <div
              ref={scrollRef}
              data-virtualized={virtualized ? "true" : undefined}
              onScroll={
                virtualized
                  ? (event) => setScrollTop(event.currentTarget.scrollTop)
                  : undefined
              }
              className={cn(
                // `isolate`: the sticky header's z-index stays inside the
                // table and never stacks above dialogs (z-index scale).
                "relative isolate overflow-auto rounded-lg border border-border bg-surface-panel",
                virtualized && "max-h-[calc(100svh-15rem)] min-h-80",
              )}
            >
              <table
                aria-rowcount={rows.length + 1}
                aria-describedby={hintId}
                className="w-full min-w-[50rem] table-fixed border-collapse text-left"
              >
                <colgroup>
                  <col className="w-10" />
                  <col />
                  <col className="w-24" />
                  <col className="w-40" />
                  <col className="w-32" />
                  <col className={onMoveBoard || onMoveDoc ? "w-24" : "w-20"} />
                </colgroup>
                <thead className="sticky top-0 z-sticky bg-surface-panel">
                  <tr aria-rowindex={1}>
                    <th
                      scope="col"
                      className={cn(
                        STICKY_CELL,
                        "left-0 h-9 border-b border-border px-3 align-middle",
                      )}
                    >
                      <Checkbox
                        aria-label={copy.selection.selectAll}
                        checked={
                          allShownSelected
                            ? true
                            : someShownSelected
                              ? "indeterminate"
                              : false
                        }
                        onCheckedChange={(checked) =>
                          setSelected(
                            checked === true ? new Set(rowKeys) : new Set(),
                          )
                        }
                      />
                    </th>
                    <SortHeader
                      field="name"
                      label={copy.columns.name}
                      query={query}
                      copy={copy}
                      onSort={(field) =>
                        setQuery((current) => ({
                          ...current,
                          sort: nextLibrarySort(current.sort, field),
                        }))
                      }
                      className={cn(STICKY_CELL, "left-10")}
                    />
                    {(["type", "location", "updated"] as const).map((field) => (
                      <SortHeader
                        key={field}
                        field={field}
                        label={copy.columns[field]}
                        query={query}
                        copy={copy}
                        onSort={(next) =>
                          setQuery((current) => ({
                            ...current,
                            sort: nextLibrarySort(current.sort, next),
                          }))
                        }
                      />
                    ))}
                    <th
                      scope="col"
                      className="h-9 border-b border-border px-2 text-right align-middle text-metadata font-medium text-muted-foreground"
                    >
                      {copy.columns.pinned}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {rowWindow.start > 0 ? (
                    <tr aria-hidden="true">
                      <td
                        colSpan={6}
                        style={{ height: rowWindow.start * ROW_HEIGHT }}
                      />
                    </tr>
                  ) : null}
                  {visibleRows.map((entry, offset) => {
                    const index = rowWindow.start + offset;
                    const key = libraryEntryKey(entry);
                    const isSelected = selected.has(key);
                    const pending = pendingPinKeys.has(key);
                    const isBoard =
                      entry.kind === NAVIGATION_RESOURCE_KIND.board;
                    const onMoveEntry = isBoard
                      ? onMoveBoard
                      : entry.parentId
                        ? undefined
                        : onMoveDoc;
                    const destination = destinationFor(entry);
                    return (
                      <tr
                        key={key}
                        data-row-index={index}
                        aria-rowindex={index + 2}
                        onKeyDown={(event) => onRowKeyDown(event, index)}
                        className={cn(
                          "group/row h-10 border-t border-border",
                          isSelected
                            ? "bg-state-selected"
                            : "hover:bg-state-hover",
                        )}
                      >
                        <td
                          className={cn(
                            STICKY_CELL,
                            "left-0 px-3 align-middle",
                            isSelected
                              ? "bg-state-selected"
                              : "group-hover/row:bg-state-hover",
                          )}
                        >
                          <Checkbox
                            tabIndex={-1}
                            aria-label={formatCopy(copy.row.select, {
                              name: entry.name,
                            })}
                            checked={isSelected}
                            onCheckedChange={() => toggleSelected(key)}
                          />
                        </td>
                        <td
                          className={cn(
                            STICKY_CELL,
                            "left-10 px-2 align-middle",
                            isSelected
                              ? "bg-state-selected"
                              : "group-hover/row:bg-state-hover",
                          )}
                        >
                          <a
                            data-row-link
                            href={navigation.getHref(destination)}
                            tabIndex={index === activeIndex ? 0 : -1}
                            onFocus={() => setActiveIndex(index)}
                            onClick={(event) => {
                              if (!isPlainClick(event)) return;
                              event.preventDefault();
                              navigation.onNavigate(destination);
                            }}
                            className="flex min-w-0 items-center gap-2 rounded-sm py-1 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
                          >
                            <ResourceMark
                              kind={entry.kind}
                              id={entry.id}
                              size="sm"
                            />
                            <span className="truncate text-label text-foreground hover:underline">
                              {entry.name}
                            </span>
                          </a>
                        </td>
                        <td className="px-2 align-middle text-metadata text-muted-foreground">
                          {isBoard ? copy.kind.board : copy.kind.doc}
                        </td>
                        <td className="truncate px-2 align-middle text-metadata text-muted-foreground">
                          {entry.location ?? copy.row.rootLocation}
                        </td>
                        <td className="px-2 align-middle text-metadata tabular-nums text-muted-foreground">
                          {entry.updatedAt ? (
                            <time dateTime={entry.updatedAt}>
                              {formatters.relativeTime(entry.updatedAt, now)}
                            </time>
                          ) : (
                            <>
                              <span aria-hidden="true">—</span>
                              <span className="sr-only">
                                {copy.row.unknownUpdated}
                              </span>
                            </>
                          )}
                        </td>
                        <td className="px-2 text-right align-middle whitespace-nowrap">
                          {onMoveEntry ? (
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon-sm"
                              tabIndex={index === activeIndex ? 0 : -1}
                              aria-label={formatCopy(copy.row.move, {
                                name: entry.name,
                              })}
                              title={formatCopy(copy.row.move, {
                                name: entry.name,
                              })}
                              onClick={() => onMoveEntry(entry)}
                              className="text-muted-foreground opacity-0 group-hover/row:opacity-100 group-focus-within/row:opacity-100 focus-visible:opacity-100 focus-visible:ring-focus-ring"
                            >
                              <ArrowRightLeft aria-hidden="true" />
                            </Button>
                          ) : null}
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon-sm"
                            tabIndex={index === activeIndex ? 0 : -1}
                            disabled={pending}
                            aria-pressed={entry.pinned}
                            aria-label={formatCopy(
                              pending
                                ? copy.row.pinning
                                : entry.pinned
                                  ? copy.row.unpin
                                  : copy.row.pin,
                              { name: entry.name },
                            )}
                            onClick={() => onSetPinned([entry], !entry.pinned)}
                            className={cn(
                              "focus-visible:ring-focus-ring",
                              entry.pinned
                                ? "text-brand-ink"
                                : "text-muted-foreground opacity-0 group-hover/row:opacity-100 group-focus-within/row:opacity-100 focus-visible:opacity-100",
                            )}
                          >
                            {pending ? (
                              <Spinner aria-hidden="true" />
                            ) : entry.pinned ? (
                              <Pin
                                aria-hidden="true"
                                className="fill-current"
                              />
                            ) : (
                              <Pin aria-hidden="true" />
                            )}
                          </Button>
                        </td>
                      </tr>
                    );
                  })}
                  {rowWindow.end < rows.length ? (
                    <tr aria-hidden="true">
                      <td
                        colSpan={6}
                        style={{
                          height: (rows.length - rowWindow.end) * ROW_HEIGHT,
                        }}
                      />
                    </tr>
                  ) : null}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      {header}
      {body}
    </div>
  );
}
