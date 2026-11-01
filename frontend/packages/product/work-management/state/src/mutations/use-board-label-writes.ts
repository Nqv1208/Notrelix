import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type {
  BoardSettingsWriteResult,
  CardLabel,
} from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { neededVersion } from "./seen-version";

/** How a failed label write is told to the page. */
function failure(error: unknown): BoardSettingsWriteResult {
  const kind =
    typeof error === "object" && error !== null && "kind" in error
      ? (error as { kind?: unknown }).kind
      : null;
  // A stale label version answers 412: the page reloads the labels and says someone else changed it.
  const status =
    typeof error === "object" && error !== null && "status" in error
      ? (error as { status?: unknown }).status
      : null;
  if (status === 412) return { ok: false, reason: "conflict" };
  return {
    ok: false,
    reason: kind === "validation" ? "invalid" : "failed",
    ...(kind === "validation" ? { field: "name" } : {}),
  };
}

/**
 * Creates, renames, recolours and deletes the labels of a board. Not
 * optimistic: the board's label list, the board and the open items are read
 * again once the server has answered (a delete takes the label off every
 * item, a rename shows on every card).
 */
export function useBoardLabelWrites(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { labels } = useWorkManagementServices();

  const refresh = useCallback(async () => {
    await queryClient.invalidateQueries({
      queryKey: wmQueryKeys.boardLabels(workspaceId, boardId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
    });
    void queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(
        workspaceId,
        "work-management",
        "cards",
        "detail",
      ),
    });
  }, [boardId, queryClient, workspaceId]);

  /** Creates a label; resolves with it, or throws so the caller can say why. */
  const createLabel = useCallback(
    async (name: string, color: string): Promise<CardLabel> => {
      const label = await labels.createLabel({ boardId, name, color });
      await refresh();
      return label;
    },
    [boardId, labels, refresh],
  );

  /** The label version the panel rendered; a label it does not hold is refused. */
  const seenVersion = useCallback(
    (labelId: string) =>
      neededVersion(
        queryClient
          .getQueryData<readonly CardLabel[]>(
            wmQueryKeys.boardLabels(workspaceId, boardId),
          )
          ?.find((label) => label.id === labelId)?.version,
        labelId,
      ),
    [boardId, queryClient, workspaceId],
  );

  const write = useCallback(
    async (send: () => Promise<unknown>): Promise<BoardSettingsWriteResult> => {
      try {
        await send();
        await refresh();
        return { ok: true };
      } catch (caught) {
        return failure(caught);
      }
    },
    [refresh],
  );

  return {
    createLabel,
    create: (name: string, color: string) =>
      write(() => labels.createLabel({ boardId, name, color })),
    update: (labelId: string, changes: { name?: string; color?: string }) =>
      write(async () =>
        labels.updateLabel({
          boardId,
          labelId,
          ...changes,
          expectedVersion: seenVersion(labelId),
        }),
      ),
    remove: (labelId: string) =>
      write(async () => labels.deleteLabel(labelId, seenVersion(labelId))),
  };
}
