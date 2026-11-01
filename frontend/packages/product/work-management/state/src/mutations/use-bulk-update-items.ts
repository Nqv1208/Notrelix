import { useRef } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { BulkItemChange } from "../api/item.api";
import type { FullBoardResponse } from "@notrelix/work-management-core";
import { findBoardCard } from "../cache/card-restore";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { neededVersion } from "./seen-version";

/** What was sent for one submission: a retry of it sends the same key and the same versions. */
interface Submission {
  readonly signature: string;
  readonly idempotencyKey: string;
  readonly expectedVersions: Readonly<Record<string, number>>;
}

/** The answer never arrived (no response, or a gateway failure), so the server may have applied it. */
function outcomeUnknown(error: unknown): boolean {
  const status = (error as { status?: unknown } | null)?.status;
  return typeof status !== "number" || status >= 500;
}

/**
 * One change for several items of a board as one request: every item changes
 * or none does. Not optimistic: the board is read again once the server has
 * answered, so a refusal leaves exactly what the server holds. Every item
 * names the version the board read showed for it. Pressing Apply again after
 * an answer that never came is the same operation: it keeps its idempotency
 * key and versions, so the server replays the first result instead of
 * applying a second time. Any answer, or a different pick or change, starts
 * a new submission.
 */
export function useBulkUpdateItems(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  const submission = useRef<Submission | null>(null);
  return useMutation({
    mutationFn: (input: {
      readonly itemIds: readonly string[];
      readonly change: BulkItemChange;
    }) => {
      const signature = JSON.stringify([
        [...input.itemIds].sort(),
        input.change,
      ]);
      if (submission.current?.signature !== signature) {
        const board = queryClient.getQueryData<FullBoardResponse>(
          wmQueryKeys.fullBoard(workspaceId, boardId),
        );
        const expectedVersions: Record<string, number> = {};
        for (const id of input.itemIds)
          expectedVersions[id] = neededVersion(
            findBoardCard(board, id)?.version,
            id,
          );
        submission.current = {
          signature,
          idempotencyKey: crypto.randomUUID(),
          expectedVersions,
        };
      }
      const sent = submission.current;
      return cards
        .bulkUpdate(
          boardId,
          input.itemIds,
          input.change,
          sent.expectedVersions,
          {
            idempotencyKey: sent.idempotencyKey,
          },
        )
        .then(
          (updated) => {
            submission.current = null;
            return updated;
          },
          (error: unknown) => {
            if (!outcomeUnknown(error)) submission.current = null;
            throw error;
          },
        );
    },
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      }),
  });
}

/** The first item a refused bulk request names (a stale one), when the problem carries it. */
export function bulkFailedItemId(error: unknown): string | undefined {
  const details = (error as { details?: unknown } | null)?.details;
  const id = (details as { itemId?: unknown } | null)?.itemId;
  return typeof id === "string" ? id : undefined;
}

/** The title the board read shows for an item, to name the one a refused bulk request points at. */
export function useBoardCardTitle(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  return (itemId: string): string | undefined =>
    findBoardCard(
      queryClient.getQueryData<FullBoardResponse>(
        wmQueryKeys.fullBoard(workspaceId, boardId),
      ),
      itemId,
    )?.title;
}
