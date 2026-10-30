import { useCallback } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

const retiredKey = (workspaceId: string, state: "archived" | "deleted") =>
  workspaceQueryKey(workspaceId, "work-management", "boards", state);

const impactKey = (workspaceId: string, boardId: string) =>
  workspaceQueryKey(
    workspaceId,
    "work-management",
    "boards",
    "lifecycle-impact",
    boardId,
  );

/** Why a board lifecycle write was refused, as the person is told it. */
export type BoardLifecycleFailure =
  "conflict" | "window" | "denied" | "gone" | "failed";

/** The rule code the server sends when the 30-day restore window has passed. */
export const RESTORE_WINDOW_ELAPSED =
  "WorkManagement_Board_RestoreWindowElapsed";

/**
 * The code the server sends for a write on an archived board: archiving a board that a peer
 * already archived is such a write, so the person is told the board changed, like a stale version.
 */
export const BOARD_ARCHIVED = "WorkManagement_Board_Archived";

export function classifyBoardLifecycleError(
  error: unknown,
): BoardLifecycleFailure {
  const { kind, code } = (error ?? {}) as { kind?: string; code?: unknown };
  if (code === RESTORE_WINDOW_ELAPSED) return "window";
  if (code === BOARD_ARCHIVED) return "conflict";
  if (kind === "conflict") return "conflict";
  if (kind === "forbidden") return "denied";
  if (kind === "not_found") return "gone";
  return "failed";
}

/**
 * What archiving or deleting the board touches (items, members, groups, views), read when the
 * confirmation opens. A person who does not manage the board gets nothing.
 */
export function useBoardLifecycleImpact(
  boardId: string,
  workspaceId: string,
  enabled: boolean,
) {
  const { boardSettings } = useWorkManagementServices();
  return useQuery({
    queryKey: impactKey(workspaceId, boardId),
    queryFn: () => boardSettings.lifecycleImpact(boardId),
    enabled: enabled && Boolean(boardId && workspaceId),
    staleTime: 0,
    retry: false,
  });
}

/**
 * The boards that left the working set: archived ones the person can see (a Manager unarchives
 * them) and deleted ones they can restore inside the 30-day window. Each write sends the board
 * version the list showed; a stale one changes nothing and the lists are read again.
 */
export function useRetiredBoards(workspaceId: string) {
  const { boardSettings } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const archived = useQuery({
    queryKey: retiredKey(workspaceId, "archived"),
    queryFn: () => boardSettings.listArchivedBoards(workspaceId),
    enabled: Boolean(workspaceId),
  });
  const deleted = useQuery({
    queryKey: retiredKey(workspaceId, "deleted"),
    queryFn: () => boardSettings.listDeletedBoards(workspaceId),
    enabled: Boolean(workspaceId),
  });

  const refresh = useCallback(async () => {
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: retiredKey(workspaceId, "archived"),
      }),
      queryClient.invalidateQueries({
        queryKey: retiredKey(workspaceId, "deleted"),
      }),
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.workspaceList(workspaceId),
      }),
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.list(workspaceId),
      }),
    ]);
  }, [queryClient, workspaceId]);

  const run = useCallback(
    async (
      send: () => Promise<unknown>,
    ): Promise<{ ok: true } | { ok: false; reason: BoardLifecycleFailure }> => {
      try {
        await send();
        await refresh();
        return { ok: true };
      } catch (caught) {
        const reason = classifyBoardLifecycleError(caught);
        // A refusal for a stale version or a gone board means the lists are out of date.
        if (reason !== "denied" && reason !== "failed") await refresh();
        return { ok: false, reason };
      }
    },
    [refresh],
  );

  return {
    archived: archived.data ?? [],
    deleted: deleted.data ?? [],
    isLoading: archived.isLoading || deleted.isLoading,
    isError: archived.isError || deleted.isError,
    refetch: refresh,
    unarchive: (boardId: string, version: number) =>
      run(() => boardSettings.unarchiveBoardById(boardId, version)),
    restore: (boardId: string, version: number) =>
      run(() => boardSettings.restoreBoard(boardId, version)),
  };
}

/**
 * Unarchives the board that is open, with the board version the person saw. The board, its lists
 * and the archived list are read again whether the write went through or was refused as stale.
 */
export function useUnarchiveBoard(boardId: string, workspaceId: string) {
  const { boardSettings } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: (expectedVersion: number) =>
      boardSettings.unarchiveBoardById(boardId, expectedVersion),
    onSettled: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
        }),
        queryClient.invalidateQueries({
          queryKey: retiredKey(workspaceId, "archived"),
        }),
        queryClient.invalidateQueries({
          queryKey: wmQueryKeys.workspaceList(workspaceId),
        }),
        queryClient.invalidateQueries({
          queryKey: wmQueryKeys.list(workspaceId),
        }),
      ]);
    },
  });
  return {
    unarchive: mutation.mutateAsync,
    pending: mutation.isPending,
    failure: mutation.error
      ? classifyBoardLifecycleError(mutation.error)
      : null,
    reset: mutation.reset,
  };
}
