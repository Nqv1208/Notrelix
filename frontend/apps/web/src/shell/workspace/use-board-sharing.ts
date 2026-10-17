import { useCallback } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import type {
  BoardSharingData,
  SharingLevel,
} from "@notrelix/work-management-web";

/**
 * The Board's own permissions, the history and who would lose the Board when it
 * goes Private (gap `CTR-GAP-WM-BOARD-SHARING`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE: Share"),
 * with the three writes the dialog offers: change a permission, remove one,
 * make the Board Private (adding the chosen people first).
 */
export function useBoardSharing(
  workspaceId: string,
  boardId: string,
  enabled: boolean,
) {
  const { api } = useAppRuntime();
  const client = useQueryClient();
  const key = workspaceQueryKey(workspaceId, "board-sharing", boardId);
  const query = useQuery({
    queryKey: key,
    queryFn: () =>
      api.api.get<BoardSharingData>(`/boards/${boardId}/sharing`, {
        skipGlobalErrorToast: true,
      }),
    enabled,
    staleTime: 10_000,
    retry: false,
  });
  // The Board's access, record and lists read the same facts: read them again.
  const refresh = useCallback(async () => {
    await client.invalidateQueries({
      predicate: (candidate) =>
        JSON.stringify(candidate.queryKey).includes(boardId),
    });
    await client.invalidateQueries({
      queryKey: workspaceQueryKey(workspaceId, "boards"),
    });
  }, [boardId, client, workspaceId]);
  return {
    data: query.data ?? null,
    changeLevel: useCallback(
      async (entryId: string, level: SharingLevel) => {
        await api.api.patch(`/boards/${boardId}/sharing/${entryId}`, { level });
        await refresh();
      },
      [api, boardId, refresh],
    ),
    remove: useCallback(
      async (entryId: string) => {
        await api.api.delete(`/boards/${boardId}/sharing/${entryId}`);
        await refresh();
      },
      [api, boardId, refresh],
    ),
    makePrivate: useCallback(
      async (addUserIds: readonly string[]) => {
        await api.api.post(`/boards/${boardId}/sharing/private`, {
          addUserIds,
        });
        await refresh();
      },
      [api, boardId, refresh],
    ),
  };
}
