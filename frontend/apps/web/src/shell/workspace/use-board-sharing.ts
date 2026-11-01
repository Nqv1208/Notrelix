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

  // The board version the list showed: a stale one changes nothing, and the list is read again.
  const seenVersion = query.data?.version ?? 0;
  const run = useCallback(
    async (write: SharingWrite) => {
      const version = () => {
        if (seenVersion <= 0) throw new Error("The board version is unknown.");
        return seenVersion;
      };
      switch (write.kind) {
        case "grant": {
          const body: OperationRequestBody<"Governance.ResourcePermissions.Grant"> =
            {
              subjectType: write.subjectType,
              subjectId: write.subjectId,
              level: write.level,
              expectedVersion: write.expectedVersion ?? undefined,
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
            { role: write.role, expectedVersion: version() };
          await api.api.patch(
            endpoints.boards.memberRole(boardId, write.userId),
            body,
            { headers: { "Idempotency-Key": crypto.randomUUID() } },
          );
          return;
        }
        case "member-remove":
          await api.api.delete(
            `${endpoints.boards.member(boardId, write.userId)}?expectedVersion=${version()}`,
            { headers: { "Idempotency-Key": crypto.randomUUID() } },
          );
          return;
      }
    },
    [api, boardId, seenVersion],
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
      /** Gives a workspace member a permission on the Board (`Governance.ResourcePermissions.Grant`). */
      invite: (userId: string, level: SharingLevel) =>
        runAll([
          { kind: "grant", subjectType: "User", subjectId: userId, level },
        ]),
      /** A workspace Owner takes a board that has no owner (`WorkManagement.Boards.AddMember` as Owner). */
      claimOwner: async (userId: string) => {
        try {
          await api.api.post(
            `/boards/${boardId}/members`,
            { userId, role: "Owner" },
            { headers: { "Idempotency-Key": crypto.randomUUID() } },
          );
        } finally {
          await refresh();
        }
      },
      /** Hands ownership to a board member (`WorkManagement.Boards.UpdateMemberRole` to Owner). */
      makeOwner: async (entryId: string) =>
        runAll([{ kind: "member-role", userId: entryId, role: "Owner" }]),
      remove: async (entryId: string) => {
        const entry = entryOf(entryId);
        if (entry) await runAll(planRemoval(entry));
      },
      makePrivate: async (
        addUserIds: readonly string[],
        addMeAsOwner: boolean,
      ) => {
        const body: OperationRequestBody<"WorkManagement.Boards.MakePrivate"> =
          { addUserIds: [...addUserIds], addMeAsOwner };
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
