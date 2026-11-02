import { useCallback, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  serializeSavedFilterCriteria,
  serializeTableColumnPreference,
  type SavedFilterCriteria,
  type TableColumnPreference,
} from "@notrelix/work-management-core";
import type { BoardPreferenceState } from "../api/board-preference.api";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

/**
 * The signed-in member's personal state of one view: filter, sort, grouping
 * and swimlane as saved criteria, and the table's hidden columns, order and
 * widths. Any board viewer may read and write it: it is theirs alone and never
 * changes the shared view. `viewId` is absent while the board has no saved
 * view of the open layout: the state then lives on screen only.
 */
export function useBoardPreference(
  boardId: string,
  workspaceId: string,
  viewId: string | undefined,
) {
  const queryClient = useQueryClient();
  const { boardPreferences } = useWorkManagementServices();
  const enabled = Boolean(boardId && workspaceId && viewId);
  const queryKey = wmQueryKeys.boardPreference(
    workspaceId,
    boardId,
    viewId ?? "none",
  );
  const query = useQuery<BoardPreferenceState>({
    queryKey,
    queryFn: () => boardPreferences.get(boardId, viewId!),
    enabled,
    staleTime: 60_000,
  });

  const lastSent = useRef<SavedFilterCriteria | null>(null);
  const [failed, setFailed] = useState(false);
  const mutation = useMutation({
    mutationFn: (criteria: SavedFilterCriteria) =>
      boardPreferences.saveCriteria(
        boardId,
        viewId!,
        Object.keys(criteria).length === 0
          ? ""
          : serializeSavedFilterCriteria(criteria),
      ),
    onMutate: (criteria) => {
      lastSent.current = criteria;
      setFailed(false);
      queryClient.setQueryData<BoardPreferenceState>(queryKey, (state) => ({
        columns: state?.columns ?? {},
        criteria,
      }));
    },
    // No silent revert: the member keeps what they see and gets "Thử lại".
    onError: () => setFailed(true),
  });
  const { mutate } = mutation;

  const save = useCallback(
    (criteria: SavedFilterCriteria) => {
      if (enabled) mutate(criteria);
    },
    [enabled, mutate],
  );
  const retry = useCallback(() => {
    if (lastSent.current) save(lastSent.current);
  }, [save]);

  // The member's columns: written through their own mutation so a failed
  // column save never shows as a failed filter save.
  const lastColumns = useRef<TableColumnPreference | null>(null);
  const [columnsFailed, setColumnsFailed] = useState(false);
  const columnsMutation = useMutation({
    mutationFn: (columns: TableColumnPreference) =>
      boardPreferences.saveColumns(
        boardId,
        viewId!,
        serializeTableColumnPreference(columns),
      ),
    onMutate: (columns) => {
      lastColumns.current = columns;
      setColumnsFailed(false);
      queryClient.setQueryData<BoardPreferenceState>(queryKey, (state) => ({
        criteria: state?.criteria ?? {},
        columns,
      }));
    },
    onError: () => setColumnsFailed(true),
  });
  const { mutate: mutateColumns } = columnsMutation;
  const saveColumns = useCallback(
    (columns: TableColumnPreference) => {
      if (enabled) mutateColumns(columns);
      // shortcut: a board with no saved view has no server row to hold the member's columns (a preference
      // belongs to a view), so they stay in the query cache while the board is open; upgrade: a view made on demand.
      else
        queryClient.setQueryData<BoardPreferenceState>(queryKey, (state) => ({
          criteria: state?.criteria ?? {},
          columns,
        }));
    },
    [enabled, mutateColumns, queryClient, queryKey],
  );
  const retryColumns = useCallback(() => {
    if (lastColumns.current) saveColumns(lastColumns.current);
  }, [saveColumns]);

  return {
    /** What was saved; `undefined` until read (or when there is no saved view to hold it). */
    criteria: query.data?.criteria,
    isLoading: enabled && query.isLoading,
    persistent: enabled,
    save,
    saving: mutation.isPending,
    saveFailed: failed,
    retry,
    /** The member's own table columns; empty when they have none. */
    columns: query.data?.columns ?? {},
    saveColumns,
    columnsSaving: columnsMutation.isPending,
    columnsFailed,
    retryColumns,
  };
}
