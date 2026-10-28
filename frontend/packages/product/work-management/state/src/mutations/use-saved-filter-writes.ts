import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import type {
  SavedFilter,
  SavedFilterCriteria,
} from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/** How a saved-filter write ended; `forbidden` and `missing` tell the page why it did not apply. */
export type SavedFilterWriteResult =
  | { readonly ok: true; readonly id?: string }
  | { readonly ok: false; readonly reason: "forbidden" | "missing" | "failed" };

function failure(error: unknown): SavedFilterWriteResult {
  const status =
    typeof error === "object" && error !== null && "status" in error
      ? (error as { status?: unknown }).status
      : null;
  if (status === 403) return { ok: false, reason: "forbidden" };
  if (status === 404) return { ok: false, reason: "missing" };
  return { ok: false, reason: "failed" };
}

/**
 * Saves, renames, shares, deletes and restores the saved filters of a board.
 * Not optimistic: a filter's version and who sees it are the server's, so the
 * board's list is read again after every answer (also after a refusal, so a
 * filter that vanished or lost its rights leaves the menu). A write never
 * touches the board's shared view.
 */
export function useSavedFilterWrites(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { savedFilters } = useWorkManagementServices();

  const refresh = useCallback(
    () =>
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.boardSavedFilters(workspaceId, boardId),
      }),
    [boardId, queryClient, workspaceId],
  );

  const write = useCallback(
    async (
      send: () => Promise<string | void>,
    ): Promise<SavedFilterWriteResult> => {
      try {
        const id = await send();
        await refresh();
        return typeof id === "string" ? { ok: true, id } : { ok: true };
      } catch (caught) {
        await refresh();
        return failure(caught);
      }
    },
    [refresh],
  );

  return {
    create: (
      name: string,
      criteria: SavedFilterCriteria,
      visibility: "board" | "me",
    ) =>
      write(() => savedFilters.create(boardId, { name, criteria, visibility })),
    rename: (filter: SavedFilter, name: string) =>
      write(() => savedFilters.rename(filter.id, name, filter.version)),
    setVisibility: (filter: SavedFilter, visibility: "board" | "me") =>
      write(() =>
        savedFilters.setVisibility(filter.id, visibility, filter.version),
      ),
    remove: (filter: SavedFilter) =>
      write(() => savedFilters.remove(filter.id, filter.version)),
    restore: (filterId: string) => write(() => savedFilters.restore(filterId)),
  };
}
