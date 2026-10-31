import { useCallback, useMemo, useState } from "react";
import {
  findStatusField,
  hasTableColumnPreference,
  serializeSorts,
  tableCustomFields,
  tableSortFromConfig,
  type FieldDefinition,
  type TableDensity,
  type TableSort,
} from "@notrelix/work-management-core";
import { useBoardKanban, type UseBoardKanbanOptions } from "./use-board-kanban";
import { useBoardPreference } from "../queries/use-board-preference";
import { useBoardView } from "./use-board-view";
import { useBoardReadNeed } from "../queries/board-read";

/**
 * The sort the server can order a page of rows by for a table sort: the built-in columns that are a field's
 * value (title, status, due date) and a custom field. Anything else (the item key, the people, the counts)
 * cannot be, and the table reads the whole board to sort it.
 */
function serverSortOf(
  sort: TableSort,
  fields: readonly FieldDefinition[],
  customFieldIds: ReadonlySet<string>,
): string | null {
  const direction = sort.direction;
  const level = (fieldId: string) => `${encodeURIComponent(fieldId)}.${direction}`;
  if (customFieldIds.has(sort.key)) return level(sort.key);
  switch (sort.key) {
    case "title":
      return level("$title");
    case "status": {
      const status = findStatusField(fields);
      return status ? level(status.id) : null;
    }
    case "due": {
      const due = fields.find((field) => field.fieldType === "date");
      return due ? level(due.id) : null;
    }
    default:
      return null;
  }
}

/**
 * The table of a board. It reads the board the same way the Kanban does
 * (status or Select lanes with the route filter and search applied, and the
 * writes a lane needs) and adds what is saved with the table view: the sort,
 * the hidden columns, the column widths and the row density.
 *
 * The saved view config is the view's own settings (`GET/PUT /boards/{id}/views`);
 * selection, collapsed groups and "hide finished" are local to the open table.
 *
 * Hidden columns, column order and widths are the member's own: they are kept
 * on the board preference and replace the shared view's for that member alone.
 * `saveColumnsToView` makes them the shared default (an editor's change) and
 * `resetColumns` drops them.
 */
export function useBoardTable(
  boardId: string,
  workspaceId: string,
  options: UseBoardKanbanOptions,
) {
  const lanes = useBoardKanban(boardId, workspaceId, options);
  const {
    viewId,
    viewConfig,
    updateViewConfig,
    isLoading: viewLoading,
  } = useBoardView(boardId, workspaceId);
  const preference = useBoardPreference(boardId, workspaceId, viewId);
  const personal = preference.columns;
  const { saveColumns } = preference;

  // Custom fields shown as columns; the view config's hide, order, width and sort apply to them by field id.
  const customFields = useMemo(
    () => tableCustomFields(lanes.board?.fieldDefinitions ?? []),
    [lanes.board],
  );
  const sort = useMemo<TableSort>(
    () =>
      tableSortFromConfig(
        viewConfig.sortBy,
        new Set(customFields.map((field) => field.id)),
      ),
    [viewConfig.sortBy, customFields],
  );
  // The rows the server pages are ordered by what the table orders them by: the member's own sort levels, else
  // the view's sort. Without a way to say it to the server the table reads the whole board.
  const personalSorts = options.view?.state.sorts;
  const readSort = useMemo(
    () =>
      !lanes.board || viewLoading
        ? "" // the fields or the saved sort are not read yet: nothing to say about the sort
        : personalSorts && personalSorts.length > 0
        ? serializeSorts(personalSorts)
        : serverSortOf(
            sort,
            lanes.board.fieldDefinitions,
            new Set(customFields.map((field) => field.id)),
          ),
    [personalSorts, sort, lanes.board, viewLoading, customFields],
  );
  useBoardReadNeed(boardId, workspaceId, {
    full: readSort === null,
    filter: "",
    sort: readSort ?? "",
  });
  const setSort = useCallback(
    (next: TableSort) =>
      updateViewConfig({
        sortBy: [{ fieldId: next.key, direction: next.direction }],
      }),
    [updateViewConfig],
  );

  const hiddenColumnIds = personal.hidden ?? viewConfig.hiddenFields;
  const setColumnVisible = useCallback(
    (columnId: string, visible: boolean) => {
      const without = hiddenColumnIds.filter((id) => id !== columnId);
      saveColumns({
        ...personal,
        hidden: visible ? without : [...without, columnId],
      });
    },
    [saveColumns, personal, hiddenColumnIds],
  );

  const columnOrder = personal.order ?? viewConfig.columnOrder;
  const setColumnOrder = useCallback(
    (next: string[]) => saveColumns({ ...personal, order: next }),
    [saveColumns, personal],
  );

  const density: TableDensity = viewConfig.density;
  const setDensity = useCallback(
    (next: TableDensity) => updateViewConfig({ density: next }),
    [updateViewConfig],
  );

  // The member's widths replace the shared ones as a whole, so a reset to the
  // design width stays a reset even when the shared view names a width.
  const columnWidths = personal.widths ?? viewConfig.columnWidths;
  const setColumnWidth = useCallback(
    (columnId: string, width: number) =>
      saveColumns({
        ...personal,
        widths: { ...columnWidths, [columnId]: Math.round(width) },
      }),
    [saveColumns, personal, columnWidths],
  );
  const resetColumnWidth = useCallback(
    (columnId: string) =>
      saveColumns({
        ...personal,
        widths: Object.fromEntries(
          Object.entries(columnWidths).filter(([id]) => id !== columnId),
        ),
      }),
    [saveColumns, personal, columnWidths],
  );

  const columnsPersonal = hasTableColumnPreference(personal);
  const resetColumns = useCallback(() => saveColumns({}), [saveColumns]);
  const [savingToView, setSavingToView] = useState(false);
  const [saveToViewFailed, setSaveToViewFailed] = useState(false);
  // The shared view takes the member's columns first; theirs are dropped only
  // once that write went through, so a refused write loses nothing.
  const saveColumnsToView = useCallback(() => {
    setSavingToView(true);
    setSaveToViewFailed(false);
    updateViewConfig(
      {
        hiddenFields: [...hiddenColumnIds],
        columnOrder: [...columnOrder],
        columnWidths,
      },
      {
        onSuccess: () => {
          setSavingToView(false);
          saveColumns({});
        },
        onError: () => {
          setSavingToView(false);
          setSaveToViewFailed(true);
        },
      },
    );
  }, [
    updateViewConfig,
    saveColumns,
    hiddenColumnIds,
    columnOrder,
    columnWidths,
  ]);

  return {
    ...lanes,
    isLoading: lanes.isLoading || viewLoading,
    customFields,
    sort,
    setSort,
    columnOrder,
    setColumnOrder,
    hiddenColumnIds,
    setColumnVisible,
    density,
    setDensity,
    columnWidths,
    setColumnWidth,
    resetColumnWidth,
    /** The member has columns of their own (hidden, order or widths). */
    columnsPersonal,
    resetColumns,
    saveColumnsToView,
    savingColumnsToView: savingToView,
    saveColumnsToViewFailed: saveToViewFailed,
    columnsSaveFailed: preference.columnsFailed,
    retryColumnsSave: preference.retryColumns,
  };
}

export type BoardTable = ReturnType<typeof useBoardTable>;
