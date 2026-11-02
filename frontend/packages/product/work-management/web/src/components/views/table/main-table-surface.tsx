import {
  useEffect,
  useRef,
  Fragment,
  useState,
  type CSSProperties,
  type KeyboardEvent,
} from "react";
import { ArrowUp } from "lucide-react";
import { BULK_ITEM_LIMIT } from "@notrelix/work-management-core";
import { Button } from "@notrelix/ui-web";
import { fillTableCopy } from "./main-table.copy";
import { TableAddGroup, type TableAddGroupProps } from "./table-add-group";
import {
  TableArchivedNotice,
  TableBulkBar,
  TableBulkFailure,
  type TableBulkBarProps,
} from "./table-bulk-bar";
import { groupAccent } from "./table-chrome";
import {
  clampColumnWidth,
  tableGridTemplate,
  visibleTableColumns,
  type TableColumnWidths,
} from "./table-columns";
import {
  TABLE_GROUP_WINDOW,
  TableAddRow,
  TableGroupHeader,
  TableLoadMoreError,
  TableShowMoreRow,
} from "./table-group-section";
import type { TableGroupMenuModel } from "./table-group-menu";
import { TableHeaderRow } from "./table-header-row";
import {
  formatTableCount,
  type TableGroupModel,
  type TableStatusOption,
} from "./table-model";
import type { TableEditors } from "./table-cell-editors";
import type { TableFieldEditing } from "./table-field-cell";
import { TableRow } from "./table-row";
import { TableEmpty, TableFooter, TableNoResults } from "./table-states";
import { MainTableToolbar, type MainTableToolbarProps } from "./table-toolbar";

/** Row heights of the two densities (the mockup `--row-h`). */
const ROW_HEIGHT = { comfortable: "40px", compact: "32px" } as const;

export interface MainTableSurfaceProps {
  /**
   * The toolbar's props: copy, search, filter, sort, columns, density, and
   * whether items can be added. "Thêm công việc" opens the first group's add
   * field, so the surface supplies `onAddItem`.
   */
  toolbar: Omit<MainTableToolbarProps, "onAddItem">;
  boardTitle: string;
  groups: readonly TableGroupModel[];
  statusOptions: readonly TableStatusOption[];
  selectedIds: ReadonlySet<string>;
  collapsedGroupIds: ReadonlySet<string>;
  /** The item whose side sheet is open. */
  openItemId?: string | null;
  /** The viewer may change status and due date of items. */
  canEdit: boolean;
  /** In-place editors of assignee, due date and priority; absent = read-only. */
  editors?: TableEditors;
  /** Editing of the custom field columns; absent = they show values only. */
  fieldEditing?: TableFieldEditing;
  /** Saves a row's title typed in place; absent = titles are read-only here. */
  onRename?: (rowId: string, title: string) => void;
  /** The viewer may add items. */
  canCreate: boolean;
  /** Opens the full "Add item" form; without it "Add item" starts the first group's inline row. */
  onOpenCreateDialog?: () => void;
  /** Said when "Add item" has no group to start in (a board needs one to hold an item). */
  onNoGroup?: () => void;
  /** The viewer may archive items. */
  canArchive: boolean;
  /** Saved column widths by column id (the view config); absent ids use the design width. */
  columnWidths?: TableColumnWidths;
  /**
   * "Thêm nhóm" / "Thêm trạng thái" under the last group: creates a new
   * option of the grouped field. Absent when the viewer may not add one.
   */
  addGroup?: Omit<TableAddGroupProps, "copy">;
  /** The "…" menu of a group header (move, delete); `undefined` for a group without one. */
  groupMenu?: (group: TableGroupModel) => TableGroupMenuModel | undefined;
  /** A width settled by a drag release or a key press. */
  onColumnWidthChange?: (columnId: string, width: number) => void;
  /** Back to the design width of a column. */
  onColumnWidthReset?: (columnId: string) => void;
  /**
   * A board read in pages: what the first group says of the whole board, and the next page of a group (the
   * group's id is its lane). Absent: every row is loaded.
   */
  paging?: {
    readonly boardTotal: number;
    /** Items that pass the filter; null when none is applied. */
    readonly boardFiltered: number | null;
    readonly loadMore: (groupId: string) => void;
    readonly loading: ReadonlySet<string>;
    readonly failed: ReadonlySet<string>;
  };
  /** Cards shown and hidden by search, filter and "hide finished". */
  shown: number;
  hidden: number;
  /** Finished items are hidden and can be shown again. */
  canShowDone: boolean;
  /** Search or filter leave nothing to show. */
  noResults: boolean;
  /** The one person the filter narrows to, for the empty note. */
  noResultsAssignee?: string;
  /** The notice after archiving; "Hoàn tác" shows when `onUndo` is given. */
  archivedNotice?: {
    count: number;
    onUndo?: () => void;
    onClose: () => void;
  } | null;
  onRowSelectedChange: (id: string, selected: boolean) => void;
  onSelectAllChange: (selectAll: boolean) => void;
  onClearSelection: () => void;
  onGroupToggle: (groupId: string) => void;
  onOpenItem: (id: string) => void;
  onStatusChange: (id: string, optionId: string) => void;
  onCreateItem: (groupId: string, title: string) => void;
  onClearFilter: () => void;
  onShowDone: () => void;
  /** A refused or lost bulk change: the banner under the table with "Thử lại"; absent = none. */
  bulkFailure?: { detail: string; onRetry: () => void } | null;
  onBulkStatus: TableBulkBarProps["onStatus"];
  onBulkDue: TableBulkBarProps["onDue"];
  onBulkArchive: TableBulkBarProps["onArchive"];
  /** "Xoá" of the bulk bar; absent = no button. */
  onBulkDelete?: TableBulkBarProps["onDelete"];
  /** "Ưu tiên" of the bulk bar; absent = no button. */
  bulkPriority?: TableBulkBarProps["priority"];
  /** The row menu: duplicate, copy link, archive; absent = no menu. */
  rowActions?: {
    readonly onDuplicate?: (id: string) => void;
    readonly onCopyLink: (id: string) => void;
    readonly onArchive?: (id: string) => void;
    readonly onDelete?: (id: string) => void;
    readonly onMoveToBoard?: (id: string) => void;
  };
  /** "Giao cho" of the bulk bar; absent = disabled with the reason. */
  bulkAssign?: TableBulkBarProps["assign"];
  /** "Chuyển board" of the bulk bar; absent = disabled with the reason. */
  bulkMove?: TableBulkBarProps["move"];
  /**
   * What shows under a row whose sub-items are expanded (the container loads
   * them); absent = rows have no expander.
   */
  renderSubItems?: (rowId: string) => React.ReactNode;
  /** Opens the status list of a row when the table first renders (stories, tests). */
  initialEditing?: { rowId: string; highlightStatusId?: string | null };
}

/**
 * The Board table (pure surface): toolbar, the grid with its sticky header,
 * the groups of rows, the footer and the bulk bar. Everything it shows and
 * every write it asks for comes through props; the container owns the data,
 * the selection and the writes.
 *
 * Keyboard: arrow keys, Home and End move between rows, Enter or Space opens
 * the focused row's item, X selects it and Escape clears the selection.
 */
export function MainTableSurface({
  renderSubItems,
  toolbar,
  boardTitle,
  groups,
  statusOptions,
  selectedIds,
  collapsedGroupIds,
  openItemId,
  canEdit,
  editors,
  fieldEditing,
  onRename,
  canCreate,
  onOpenCreateDialog,
  onNoGroup,
  canArchive,
  addGroup,
  groupMenu,
  columnWidths,
  onColumnWidthChange,
  onColumnWidthReset,
  paging,
  shown,
  hidden,
  canShowDone,
  noResults,
  noResultsAssignee,
  archivedNotice,
  onRowSelectedChange,
  onSelectAllChange,
  onClearSelection,
  onGroupToggle,
  onOpenItem,
  onStatusChange,
  onCreateItem,
  onClearFilter,
  onShowDone,
  bulkFailure,
  onBulkStatus,
  onBulkDue,
  onBulkArchive,
  onBulkDelete,
  bulkPriority,
  bulkAssign,
  bulkMove,
  rowActions,
  initialEditing,
}: MainTableSurfaceProps) {
  const { copy } = toolbar;
  const columns = visibleTableColumns(
    toolbar.hiddenColumnIds,
    toolbar.customFields,
    toolbar.columnOrder,
  );
  const [editingId, setEditingId] = useState<string | null>(
    initialEditing?.rowId ?? null,
  );
  const [tabStopId, setTabStopId] = useState<string | null>(null);
  const [expandedIds, setExpandedIds] = useState<ReadonlySet<string>>(
    () => new Set(),
  );
  const toggleExpanded = (id: string) =>
    setExpandedIds((previous) => {
      const next = new Set(previous);
      if (!next.delete(id)) next.add(id);
      return next;
    });
  const [addRequest, setAddRequest] = useState({ groupId: "", count: 0 });
  const gridRef = useRef<HTMLDivElement>(null);
  // The width the pointer is dragging a column to; the saved width applies
  // once the drag is released.
  const [preview, setPreview] = useState<{
    id: string;
    width: number;
  } | null>(null);
  const widths: TableColumnWidths = preview
    ? { ...columnWidths, [preview.id]: clampColumnWidth(preview.width) }
    : (columnWidths ?? {});

  // A group draws its first rows and a "Show more" row; the row that is open, being edited or holding the
  // keyboard stays drawn wherever it sits.
  const [shownByGroup, setShownByGroup] = useState<Record<string, number>>({});
  const focusAfterReveal = useRef<string | null>(null);
  const shownRows = (group: TableGroupModel) => {
    const pinned = [openItemId, editingId, tabStopId].reduce(
      (last, id) =>
        id
          ? Math.max(
              last,
              group.rows.findIndex((row) => row.id === id),
            )
          : last,
      -1,
    );
    return Math.min(
      group.rows.length,
      Math.max(shownByGroup[group.id] ?? TABLE_GROUP_WINDOW, pinned + 1),
    );
  };
  // After a page of the group is read from the server, the focus goes to the first row it brought.
  const focusAfterLoad = useRef<{ groupId: string; index: number } | null>(
    null,
  );
  const showMore = (group: TableGroupModel) => {
    const shown = shownRows(group);
    focusAfterReveal.current = group.rows[shown]?.id ?? null;
    // Rows read from the server beyond the loaded ones fall inside the window once they arrive.
    if (shown >= group.rows.length && group.hasMore && paging) {
      focusAfterLoad.current = { groupId: group.id, index: group.rows.length };
      paging.loadMore(group.id);
    }
    setShownByGroup((current) => ({
      ...current,
      [group.id]: shown + TABLE_GROUP_WINDOW,
    }));
  };
  useEffect(() => {
    const pending = focusAfterLoad.current;
    if (!pending) return;
    const id = groups.find((group) => group.id === pending.groupId)?.rows[
      pending.index
    ]?.id;
    if (!id) return;
    focusAfterLoad.current = null;
    gridRef.current
      ?.querySelector<HTMLElement>(`[data-row-id="${CSS.escape(id)}"]`)
      ?.focus();
  }, [groups]);
  useEffect(() => {
    const id = focusAfterReveal.current;
    if (!id) return;
    focusAfterReveal.current = null;
    gridRef.current
      ?.querySelector<HTMLElement>(`[data-row-id="${CSS.escape(id)}"]`)
      ?.focus();
  }, [shownByGroup]);

  const visibleRowIds = groups.flatMap((group) =>
    collapsedGroupIds.has(group.id)
      ? []
      : group.rows.slice(0, shownRows(group)).map((row) => row.id),
  );
  const allRowIds = groups.flatMap((group) => group.rows.map((row) => row.id));
  const selectedCount = allRowIds.filter((id) => selectedIds.has(id)).length;
  const allChecked: boolean | "mixed" =
    selectedCount === 0
      ? false
      : selectedCount === allRowIds.length
        ? true
        : "mixed";
  const tabStop =
    tabStopId && visibleRowIds.includes(tabStopId)
      ? tabStopId
      : (visibleRowIds[0] ?? null);

  const focusRow = (id: string | undefined) => {
    if (!id) return;
    const row = gridRef.current?.querySelector<HTMLElement>(
      `[data-row-id="${CSS.escape(id)}"]`,
    );
    row?.focus();
  };

  const onRowKeyDown = (
    event: KeyboardEvent<HTMLDivElement>,
    rowId: string,
  ) => {
    // Keys typed in a control of the row (the status list, a checkbox) are
    // theirs; the row takes only the keys pressed on the row itself.
    if (event.target !== event.currentTarget) return;
    const index = visibleRowIds.indexOf(rowId);
    switch (event.key) {
      case "ArrowDown": {
        event.preventDefault();
        // On the last drawn row of a group with more rows, the arrow draws the next page and moves into it.
        const group = groups.find(
          (candidate) => candidate.rows[shownRows(candidate) - 1]?.id === rowId,
        );
        if (
          group &&
          (shownRows(group) < group.rows.length ||
            (group.hasMore && paging && !paging.loading.has(group.id)))
        ) {
          showMore(group);
          break;
        }
        focusRow(visibleRowIds[Math.min(index + 1, visibleRowIds.length - 1)]);
        break;
      }
      case "ArrowUp":
        event.preventDefault();
        focusRow(visibleRowIds[Math.max(index - 1, 0)]);
        break;
      case "Home":
        event.preventDefault();
        focusRow(visibleRowIds[0]);
        break;
      case "End":
        event.preventDefault();
        focusRow(visibleRowIds.at(-1));
        break;
      case "Enter":
      case " ":
        event.preventDefault();
        onOpenItem(rowId);
        break;
      case "x":
      case "X":
        event.preventDefault();
        onRowSelectedChange(rowId, !selectedIds.has(rowId));
        break;
    }
  };

  const style = {
    "--table-row-height": ROW_HEIGHT[toolbar.density],
  } as CSSProperties;
  const gridStyle = {
    "--table-grid": tableGridTemplate(columns, widths),
  } as CSSProperties;
  // Every row of every group, loaded or not, is counted: a screen reader says "row 51 of 400" at the edge of
  // the window, not "row 1 of 50".
  const rowsOf = (group: TableGroupModel) =>
    group.filteredTotal ?? group.rows.length;
  const rowOffsets = new Map<string, number>();
  let rowCount = 0;
  for (const group of groups) {
    rowOffsets.set(group.id, rowCount);
    rowCount += Math.max(rowsOf(group), group.rows.length);
  }
  // Deep in the table the way back to the top is one control away.
  const [deep, setDeep] = useState(false);
  const jumpToTop = () => {
    gridRef.current?.scrollTo({ top: 0 });
    focusRow(tabStop ?? undefined);
  };

  return (
    <section
      aria-label={fillTableCopy(copy.tableLabel, { board: boardTitle })}
      data-slot="main-table-surface"
      data-density={toolbar.density}
      style={style}
      onKeyDown={(event) => {
        if (
          event.key === "Escape" &&
          selectedCount > 0 &&
          editingId === null &&
          event.currentTarget.contains(event.target as Node)
        ) {
          onClearSelection();
        }
      }}
      className="relative flex h-full min-h-0 flex-col overflow-hidden bg-background font-body"
    >
      <div className="px-4 sm:px-6">
        <MainTableToolbar
          {...toolbar}
          canAdd={toolbar.canAdd && canCreate}
          onAddItem={() => {
            if (onOpenCreateDialog) {
              onOpenCreateDialog();
              return;
            }
            const first = groups[0];
            if (!first) {
              onNoGroup?.();
              return;
            }
            if (collapsedGroupIds.has(first.id)) onGroupToggle(first.id);
            setAddRequest((current) => ({
              groupId: first.id,
              count: current.count + 1,
            }));
          }}
        />
      </div>

      <div
        ref={gridRef}
        data-table-scroll=""
        onScroll={(event) => {
          const el = event.currentTarget;
          setDeep(el.scrollTop > el.clientHeight * 2);
        }}
        className="mx-4 min-h-0 flex-1 overflow-auto rounded-[12px] border border-border bg-popover sm:mx-6 dark:bg-card"
      >
        <div
          role="table"
          aria-label={fillTableCopy(copy.tableLabel, { board: boardTitle })}
          aria-rowcount={paging ? rowCount : undefined}
          style={gridStyle}
          className="w-max min-w-full text-nav font-normal text-foreground"
        >
          <TableHeaderRow
            copy={copy}
            columns={columns}
            widths={widths}
            sort={toolbar.sort}
            allChecked={allChecked}
            canSelect={allRowIds.length > 0}
            canEditFields={canEdit && Boolean(fieldEditing?.canEdit)}
            onToggleAll={onSelectAllChange}
            onWidthPreview={(id, width) =>
              setPreview(width === null ? null : { id, width })
            }
            onWidthCommit={(id, width) => onColumnWidthChange?.(id, width)}
            onWidthReset={(id) => onColumnWidthReset?.(id)}
          />
          {groups.map((group, groupIndex) => {
            const collapsed = collapsedGroupIds.has(group.id);
            return (
              <div role="rowgroup" aria-label={group.name} key={group.id}>
                <TableGroupHeader
                  copy={copy}
                  group={group}
                  collapsed={collapsed}
                  onToggle={() => onGroupToggle(group.id)}
                  menu={groupMenu?.(group)}
                  boardNote={
                    paging && groupIndex === 0
                      ? paging.boardFiltered === null
                        ? fillTableCopy(copy.group.boardTotal, {
                            total: formatTableCount(paging.boardTotal),
                          })
                        : fillTableCopy(copy.group.boardFiltered, {
                            shown: formatTableCount(paging.boardFiltered),
                            total: formatTableCount(paging.boardTotal),
                          })
                      : undefined
                  }
                />
                {collapsed
                  ? null
                  : group.rows
                      .slice(0, shownRows(group))
                      .map((row, rowIndex) => (
                        <Fragment key={row.id}>
                          <TableRow
                            row={row}
                            rowIndex={
                              paging
                                ? (rowOffsets.get(group.id) ?? 0) + rowIndex + 1
                                : undefined
                            }
                            expand={
                              renderSubItems && row.progress
                                ? {
                                    expanded: expandedIds.has(row.id),
                                    count: row.progress.total,
                                    onToggle: () => toggleExpanded(row.id),
                                  }
                                : undefined
                            }
                            copy={copy}
                            columns={columns}
                            accent={groupAccent(group)}
                            statusOptions={statusOptions}
                            selected={selectedIds.has(row.id)}
                            active={openItemId === row.id}
                            tabStop={tabStop === row.id}
                            canEdit={canEdit}
                            editors={editors}
                            fieldEditing={fieldEditing}
                            onRename={
                              onRename
                                ? (title) => onRename(row.id, title)
                                : undefined
                            }
                            actions={
                              rowActions
                                ? {
                                    onDuplicate: rowActions.onDuplicate
                                      ? () => rowActions.onDuplicate?.(row.id)
                                      : undefined,
                                    onCopyLink: () =>
                                      rowActions.onCopyLink(row.id),
                                    onArchive: rowActions.onArchive
                                      ? () => rowActions.onArchive?.(row.id)
                                      : undefined,
                                    onDelete: rowActions.onDelete
                                      ? () => rowActions.onDelete?.(row.id)
                                      : undefined,
                                    onMoveToBoard: rowActions.onMoveToBoard
                                      ? () => rowActions.onMoveToBoard?.(row.id)
                                      : undefined,
                                  }
                                : undefined
                            }
                            editing={editingId === row.id}
                            highlightStatusId={
                              initialEditing?.rowId === row.id
                                ? initialEditing.highlightStatusId
                                : undefined
                            }
                            onSelectedChange={(selected) =>
                              onRowSelectedChange(row.id, selected)
                            }
                            onOpen={() => onOpenItem(row.id)}
                            onEditingChange={(editing) =>
                              setEditingId(editing ? row.id : null)
                            }
                            onStatusChange={(optionId) =>
                              onStatusChange(row.id, optionId)
                            }
                            onKeyDown={(event) => {
                              setTabStopId(row.id);
                              onRowKeyDown(event, row.id);
                            }}
                          />
                          {renderSubItems && expandedIds.has(row.id) ? (
                            <div
                              role="row"
                              data-sub-items-of={row.id}
                              className="grid min-w-full"
                              style={{
                                gridTemplateColumns: "var(--table-grid)",
                              }}
                            >
                              <div
                                role="cell"
                                className="box-border min-w-0 border-b border-border bg-state-hover/40 py-1 pr-3 pl-16"
                                style={{ gridColumn: "1 / -1" }}
                              >
                                {renderSubItems(row.id)}
                              </div>
                            </div>
                          ) : null}
                        </Fragment>
                      ))}
                {!collapsed && paging?.failed.has(group.id) && group.hasMore ? (
                  <TableLoadMoreError
                    copy={copy}
                    next={Math.min(
                      TABLE_GROUP_WINDOW,
                      rowsOf(group) - group.rows.length,
                    )}
                    onRetry={() => paging.loadMore(group.id)}
                  />
                ) : !collapsed &&
                  (shownRows(group) < group.rows.length || group.hasMore) ? (
                  <TableShowMoreRow
                    copy={copy}
                    shown={shownRows(group)}
                    total={Math.max(rowsOf(group), group.rows.length)}
                    loading={paging?.loading.has(group.id) ?? false}
                    onShowMore={() => showMore(group)}
                  />
                ) : null}
                {collapsed || !canCreate ? null : (
                  <TableAddRow
                    copy={copy}
                    group={group}
                    openRequest={
                      addRequest.groupId === group.id ? addRequest.count : 0
                    }
                    onCreate={(title) => onCreateItem(group.id, title)}
                  />
                )}
              </div>
            );
          })}
          {addGroup && !noResults ? (
            <TableAddGroup copy={copy} {...addGroup} />
          ) : null}
          {noResults ? (
            <TableNoResults
              copy={copy}
              assigneeName={noResultsAssignee}
              hidesDone={toolbar.hideDone}
              onClear={onClearFilter}
            />
          ) : groups.length === 0 ? (
            <TableEmpty copy={copy} />
          ) : null}
        </div>
      </div>

      <div className="px-4 sm:px-6">
        <TableFooter
          copy={copy}
          shown={shown}
          hidden={hidden}
          archived={archivedNotice?.count ?? 0}
          canShowDone={canShowDone}
          onShowDone={onShowDone}
        />
      </div>

      {deep ? (
        <div className="absolute right-6 bottom-5 z-10">
          <Button
            type="button"
            className="h-11 gap-1.5 rounded-[7px] px-3.5 text-[13px] font-semibold shadow-popover"
            onClick={jumpToTop}
          >
            <ArrowUp aria-hidden="true" className="size-4" />
            {copy.group.jumpTop}
          </Button>
        </div>
      ) : null}

      {selectedCount > 0 ? (
        // The banner and the bar share one bottom anchor, so the bar never covers the banner (BC-Bulk bk-err).
        <div
          data-table-bulk-stack=""
          className="pointer-events-none absolute inset-x-3 bottom-5.5 z-20 flex flex-col items-center gap-6 max-md:bottom-3"
        >
          {bulkFailure ? (
            <div className="w-full px-1 sm:px-3">
              <div className="pointer-events-auto rounded-[10px] bg-background shadow-bar">
                <TableBulkFailure
                  copy={copy}
                  count={selectedCount}
                  detail={bulkFailure.detail}
                  onRetry={bulkFailure.onRetry}
                />
              </div>
            </div>
          ) : null}
          <TableBulkBar
            copy={copy}
            count={selectedCount}
            limit={{ max: BULK_ITEM_LIMIT, available: allRowIds.length }}
            stacked
            statusOptions={statusOptions}
            canEdit={canEdit}
            canArchive={canArchive}
            onStatus={onBulkStatus}
            onDue={onBulkDue}
            onArchive={onBulkArchive}
            onDelete={onBulkDelete}
            priority={bulkPriority}
            onClear={onClearSelection}
            assign={bulkAssign}
            move={bulkMove}
          />
        </div>
      ) : archivedNotice ? (
        <TableArchivedNotice
          copy={copy}
          count={archivedNotice.count}
          onUndo={archivedNotice.onUndo}
          onClose={archivedNotice.onClose}
        />
      ) : null}
    </section>
  );
}
