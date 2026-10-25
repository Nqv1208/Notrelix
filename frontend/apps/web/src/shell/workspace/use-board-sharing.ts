import { useCallback, useMemo } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { endpoints } from "@notrelix/contracts";
import type {
  OperationRequestBody,
  OperationResponse,
} from "@notrelix/contracts";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  planLevelChange,
  planRemoval,
  toBoardSharingData,
  type BoardSharingData,
  type SharingLevel,
  type SharingWrite,
} from "@notrelix/work-management-web";

/** The resource kind Governance names a Board by. */
const BOARD_KIND = "work-management.board";

type Entry = BoardSharingData["entries"][number];

/**
 * The Board's own permissions, the history and who would lose the Board when it
 * goes Private (`WorkManagement.Boards.GetSharing`), with the writes the dialog
 * offers: change a permission, remove one, make the Board Private (adding the
 * chosen people first, `WorkManagement.Boards.MakePrivate`).
 *
 * An entry's id is the person's user id or the Team's id. A permission is
 * changed or removed with the operation that holds it: an explicit grant by
 * `Governance.ResourcePermissions.Grant` / `Revoke`, a Board membership by
 * `WorkManagement.Boards.UpdateMemberRole` / `RemoveMember` (see
 * `planLevelChange` and `planRemoval`).
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
      toBoardSharingData(
        await api.api.get<
          OperationResponse<"WorkManagement.Boards.GetSharing", 200>
        >(endpoints.boards.sharing(boardId), { skipGlobalErrorToast: true }),
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

  const run = useCallback(
    async (write: SharingWrite) => {
      switch (write.kind) {
        case "grant": {
          const body: OperationRequestBody<"Governance.ResourcePermissions.Grant"> =
            {
              subjectType: write.subjectType,
              subjectId: write.subjectId,
              level: write.level,
            };
          await api.api.post(
            endpoints.resourcePermissions.list(BOARD_KIND, boardId),
            body,
          );
          return;
        }
        case "revoke":
          await api.api.delete(
            endpoints.resourcePermissions.detail(
              BOARD_KIND,
              boardId,
              write.permissionId,
            ),
          );
          return;
        case "member-role": {
          const body: OperationRequestBody<"WorkManagement.Boards.UpdateMemberRole"> =
            { role: write.role };
          await api.api.patch(
            endpoints.boards.memberRole(boardId, write.userId),
            body,
            { headers: { "Idempotency-Key": crypto.randomUUID() } },
          );
          return;
        }
        case "member-remove":
          await api.api.delete(endpoints.boards.member(boardId, write.userId), {
            headers: { "Idempotency-Key": crypto.randomUUID() },
          });
          return;
      }
    },
    [api, boardId],
  );

  const runAll = useCallback(
    async (writes: readonly SharingWrite[]) => {
      try {
        for (const write of writes) await run(write);
      } finally {
        // A later write may have failed after an earlier one took effect.
        await refresh();
      }
    },
    [refresh, run],
  );

  const entryOf = useCallback(
    (entryId: string): Entry | undefined =>
      query.data?.entries.find((entry) => entry.id === entryId),
    [query.data],
  );

  return useMemo(
    () => ({
      data: query.data ?? null,
      changeLevel: async (entryId: string, level: SharingLevel) => {
        const entry = entryOf(entryId);
        if (entry) await runAll(planLevelChange(entry, level));
      },
      remove: async (entryId: string) => {
        const entry = entryOf(entryId);
        if (entry) await runAll(planRemoval(entry));
      },
      makePrivate: async (addUserIds: readonly string[]) => {
        const body: OperationRequestBody<"WorkManagement.Boards.MakePrivate"> =
          { addUserIds: [...addUserIds] };
        try {
          await api.api.post(endpoints.boards.sharingPrivate(boardId), body, {
            headers: { "Idempotency-Key": crypto.randomUUID() },
          });
        } finally {
          await refresh();
        }
      },
    }),
    [api, boardId, entryOf, query.data, refresh, runAll],
  );
}
