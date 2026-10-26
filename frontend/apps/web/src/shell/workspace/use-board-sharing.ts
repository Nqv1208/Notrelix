import { useCallback } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  normalizeSharing,
  planChangeLevel,
  planRemove,
  type SharingRequest,
} from "./board-sharing-writes";
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
    queryFn: async () =>
      normalizeSharing(
        await api.api.get<BoardSharingData>(`/boards/${boardId}/sharing`, {
          skipGlobalErrorToast: true,
        }),
      ),
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
  const send = useCallback(
    async (requests: readonly SharingRequest[]) => {
      try {
        for (const request of requests) {
          const options = { idempotencyKey: crypto.randomUUID() };
          if (request.method === "POST")
            await api.api.post(request.path, request.body, options);
          else if (request.method === "PATCH")
            await api.api.patch(request.path, request.body, options);
          else await api.api.delete(request.path, options);
        }
      } finally {
        await refresh();
      }
    },
    [api, refresh],
  );
  const entryOf = useCallback(
    (entryId: string) => {
      const entry = query.data?.entries.find((item) => item.id === entryId);
      if (!entry) throw new Error("The permission is no longer on the Board.");
      return entry;
    },
    [query.data],
  );
  return {
    data: query.data ?? null,
    changeLevel: useCallback(
      (entryId: string, level: SharingLevel) =>
        send(planChangeLevel(boardId, entryOf(entryId), level)),
      [boardId, entryOf, send],
    ),
    remove: useCallback(
      (entryId: string) => send(planRemove(boardId, entryOf(entryId))),
      [boardId, entryOf, send],
    ),
    makePrivate: useCallback(
      async (addUserIds: readonly string[]) => {
        await api.api.post(
          `/boards/${boardId}/sharing/private`,
          { addUserIds },
          { idempotencyKey: crypto.randomUUID() },
        );
        await refresh();
      },
      [api, boardId, refresh],
    ),
  };
}
