import { useCallback, useMemo } from "react";
import {
  tableSortFromConfig,
  type TableDensity,
  type TableSort,
} from "@notrelix/work-management-core";
import { useBoardKanban, type UseBoardKanbanOptions } from "./use-board-kanban";
import { useBoardView } from "./use-board-view";

/**
 * The table of a board. It reads the board the same way the Kanban does
 * (status or Select lanes with the route filter and search applied, and the
 * writes a lane needs) and adds what is saved with the table view: the sort,
 * the hidden columns, the column widths and the row density.
 *
 * The saved view config is the view's own settings (`GET/PUT /boards/{id}/views`);
 * selection, collapsed groups and "hide finished" are local to the open table.
 */
export function useBoardTable(
  boardId: string,
  workspaceId: string,
  options: UseBoardKanbanOptions,
) {
  const lanes = useBoardKanban(boardId, workspaceId, options);
  const {
    viewConfig,
    updateViewConfig,
    isLoading: viewLoading,
  } = useBoardView(boardId, workspaceId);

  const sort = useMemo<TableSort>(
    () => tableSortFromConfig(viewConfig.sortBy),
    [viewConfig.sortBy],
  );
  const setSort = useCallback(
    (next: TableSort) =>
      updateViewConfig({
        sortBy: [{ fieldId: next.key, direction: next.direction }],
      }),
    [updateViewConfig],
  );

  const hiddenColumnIds = viewConfig.hiddenFields;
  const setColumnVisible = useCallback(
    (columnId: string, visible: boolean) => {
      const without = viewConfig.hiddenFields.filter((id) => id !== columnId);
      updateViewConfig({
        hiddenFields: visible ? without : [...without, columnId],
      });
    },
    [updateViewConfig, viewConfig.hiddenFields],
  );

  const density: TableDensity = viewConfig.density;
  const setDensity = useCallback(
    (next: TableDensity) => updateViewConfig({ density: next }),
    [updateViewConfig],
  );

  const columnWidths = viewConfig.columnWidths;
  const setColumnWidth = useCallback(
    (columnId: string, width: number) =>
      updateViewConfig({
        columnWidths: {
          ...viewConfig.columnWidths,
          [columnId]: Math.round(width),
        },
      }),
    [updateViewConfig, viewConfig.columnWidths],
  );
  const resetColumnWidth = useCallback(
    (columnId: string) => {
      updateViewConfig({
        columnWidths: Object.fromEntries(
          Object.entries(viewConfig.columnWidths).filter(
            ([id]) => id !== columnId,
          ),
        ),
      });
    },
    [updateViewConfig, viewConfig.columnWidths],
  );

  return {
    ...lanes,
    isLoading: lanes.isLoading || viewLoading,
    sort,
    setSort,
    hiddenColumnIds,
    setColumnVisible,
    density,
    setDensity,
    columnWidths,
    setColumnWidth,
    resetColumnWidth,
  };
}

export type BoardTable = ReturnType<typeof useBoardTable>;
