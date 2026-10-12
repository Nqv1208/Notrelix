import { useQuery } from "@tanstack/react-query";
import type { BoardAccess } from "@notrelix/work-management-core";
import type { createBoardAccessApi } from "../api/board-access.api";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";

type BoardAccessApi = ReturnType<typeof createBoardAccessApi>;

/**
 * Query options for a board's access summary. The key is workspace-scoped, so
 * a workspace switch never shows another workspace's answer; the query waits
 * until both ids are known.
 */
export function boardAccessQueryOptions(
  api: BoardAccessApi,
  boardId: string | undefined,
  workspaceId: string | undefined,
) {
  return {
    queryKey: wmQueryKeys.boardAccess(
      workspaceId ?? "pending",
      boardId ?? "pending",
    ),
    queryFn: (): Promise<BoardAccess> => {
      if (!boardId || !workspaceId) {
        throw new Error(
          "boardId and workspaceId are required to load board access",
        );
      }
      return api.getBoardAccess(boardId);
    },
    enabled: Boolean(boardId && workspaceId),
    staleTime: 30_000,
  };
}

/** The caller's level and the board's audience; `null` while loading. */
export function useBoardAccess(boardId?: string, workspaceId?: string) {
  const { access: api } = useWorkManagementServices();
  const query = useQuery(boardAccessQueryOptions(api, boardId, workspaceId));
  return {
    access: query.data ?? null,
    isLoading: query.isLoading,
    error: query.error,
  };
}
