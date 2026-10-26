import {
  useRef,
  Fragment,
  useState,
  type CSSProperties,
  type KeyboardEvent,
} from "react";
import { fillTableCopy } from "./main-table.copy";
import { TableAddGroup, type TableAddGroupProps } from "./table-add-group";
import {
  TableArchivedNotice,
  TableBulkBar,
  type TableBulkBarProps,
} from "./table-bulk-bar";
import { groupAccent } from "./table-chrome";
import {
  clampColumnWidth,
  tableGridTemplate,
  visibleTableColumns,
  type TableColumnWidths,
} from "./table-columns";
import { TableAddRow, TableGroupHeader } from "./table-group-section";
import { TableHeaderRow } from "./table-header-row";
import type { TableGroupModel, TableStatusOption } from "./table-model";
import type { TableEditors } from "./table-cell-editors";
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
  /** The viewer may add items. */
  canCreate: boolean;
  /** Opens the full "Add item" form; without it "Add item" starts the first group's inline row. */
  onOpenCreateDialog?: () => void;
  /** The viewer may archive items. */
  canArchive: boolean;
  /** Saved column widths by column id (the view config); absent ids use the design width. */
  columnWidths?: TableColumnWidths;
  /**
   * "Thêm nhóm" / "Thêm trạng thái" under the last group: creates a new
   * option of the grouped field. Absent when the viewer may not add one.
   */
  addGroup?: Omit<TableAddGroupProps, "copy">;
  /** A width settled by a drag release or a key press. */
  onColumnWidthChange?: (columnId: string, width: number) => void;
  /** Back to the design width of a column. */
  onColumnWidthReset?: (columnId: string) => void;
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
  onBulkStatus: TableBulkBarProps["onStatus"];
  onBulkDue: TableBulkBarProps["onDue"];
  onBulkArchive: TableBulkBarProps["onArchive"];
  /** The row menu: duplicate, copy link, archive; absent = no menu. */
  rowActions?: {
    readonly onDuplicate?: (id: string) => void;
    readonly onCopyLink: (id: string) => void;
    readonly onArchive?: (id: string) => void;
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
  canCreate,
  onOpenCreateDialog,
  canArchive,
  addGroup,
  columnWidths,
  onColumnWidthChange,
  onColumnWidthReset,
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
  onBulkStatus,
  onBulkDue,
  onBulkArchive,
  bulkAssign,
  bulkMove,
  rowActions,
  initialEditing,
}: MainTableSurfaceProps) {
  const { copy } = toolbar;
  const columns = visibleTableColumns(toolbar.hiddenColumnIds);
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

  const visibleRowIds = groups.flatMap((group) =>
    collapsedGroupIds.has(group.id) ? [] : group.rows.map((row) => row.id),
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
      case "ArrowDown":
        event.preventDefault();
        focusRow(visibleRowIds[Math.min(index + 1, visibleRowIds.length - 1)]);
        break;
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
            if (!first) return;
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
        className="mx-4 min-h-0 flex-1 overflow-auto rounded-[12px] border border-border bg-popover sm:mx-6 dark:bg-card"
      >
        <div
          role="table"
          aria-label={fillTableCopy(copy.tableLabel, { board: boardTitle })}
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
            onToggleAll={onSelectAllChange}
            onWidthPreview={(id, width) =>
              setPreview(width === null ? null : { id, width })
            }
            onWidthCommit={(id, width) => onColumnWidthChange?.(id, width)}
            onWidthReset={(id) => onColumnWidthReset?.(id)}
          />
          {groups.map((group) => {
            const collapsed = collapsedGroupIds.has(group.id);
            return (
              <div role="rowgroup" aria-label={group.name} key={group.id}>
                <TableGroupHeader
                  copy={copy}
                  group={group}
                  collapsed={collapsed}
                  onToggle={() => onGroupToggle(group.id)}
                />
                {collapsed
                  ? null
                  : group.rows.map((row) => (
                      <Fragment key={row.id}>
                        <TableRow
                          row={row}
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
                            style={{ gridTemplateColumns: "var(--table-grid)" }}
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

      {selectedCount > 0 ? (
        <TableBulkBar
          copy={copy}
          count={selectedCount}
          statusOptions={statusOptions}
          canEdit={canEdit}
          canArchive={canArchive}
          onStatus={onBulkStatus}
          onDue={onBulkDue}
          onArchive={onBulkArchive}
          onClear={onClearSelection}
          assign={bulkAssign}
          move={bulkMove}
        />
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
