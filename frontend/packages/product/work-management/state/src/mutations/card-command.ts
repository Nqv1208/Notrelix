import type { QueryClient } from "@tanstack/react-query";
import type {
  CardDetail,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import { findBoardCard, restoreBoardCard } from "../cache/card-restore";
import { workspaceQueryKey } from "@notrelix/query";
import { wmQueryKeys } from "../queries/keys";

/**
 * In-flight card commands per board read, per query client. The board read
 * is refetched only when the last of them settles, so a refetch never wipes
 * the optimistic state of a write that is still pending.
 */
const pendingByClient = new WeakMap<QueryClient, Map<string, number>>();

function pendingFor(queryClient: QueryClient): Map<string, number> {
  let pending = pendingByClient.get(queryClient);
  if (!pending) {
    pending = new Map();
    pendingByClient.set(queryClient, pending);
  }
  return pending;
}

export interface CardCommandContext {
  readonly correlationId: string;
  readonly idempotencyKey: string;
}

/**
 * A command id that is unique across reloads and tabs: it doubles as the
 * idempotency key, and a key reused with another payload is a conflict.
 */
export function cardCommandId(kind: string, subject: string): string {
  return `${kind}:${subject}:${crypto.randomUUID()}`;
}

export interface CardCommand<T> {
  readonly queryClient: QueryClient;
  readonly workspaceId: string;
  readonly boardId: string;
  readonly cardId: string;
  /** Names the command in its id (`kind:cardId:uuid`). */
  readonly kind: string;
  readonly applyBoard: (board: FullBoardResponse) => FullBoardResponse;
  readonly applyDetail?: (detail: CardDetail) => CardDetail;
  readonly send: (context: CardCommandContext) => Promise<T>;
  /** Runs after success, before the reads are refetched. */
  readonly reconcile?: (result: T) => void;
}

/**
 * One optimistic write to one card: the board read (and the item detail when
 * `applyDetail` is given) take the change at once. A failure puts only this
 * card back — another card's in-flight change is kept — and rethrows. The
 * item detail and the item's history are read again either way; the board
 * read is refetched when the last in-flight command on the board settles.
 */
export async function runCardCommand<T>({
  queryClient,
  workspaceId,
  boardId,
  cardId,
  kind,
  applyBoard,
  applyDetail,
  send,
  reconcile,
}: CardCommand<T>): Promise<T> {
  const boardKey = wmQueryKeys.fullBoard(workspaceId, boardId);
  const detailKey = wmQueryKeys.cardDetail(workspaceId, cardId);
  const activityKey = wmQueryKeys.cardActivity(workspaceId, cardId);
  await Promise.all([
    queryClient.cancelQueries({ queryKey: boardKey }),
    queryClient.cancelQueries({ queryKey: detailKey }),
  ]);
  const cardBefore = findBoardCard(
    queryClient.getQueryData<FullBoardResponse>(boardKey),
    cardId,
  );
  const detailBefore = queryClient.getQueryData<CardDetail>(detailKey);
  queryClient.setQueryData<FullBoardResponse | undefined>(
    boardKey,
    (current) => (current ? applyBoard(current) : current),
  );
  if (applyDetail && detailBefore) {
    queryClient.setQueryData<CardDetail | undefined>(detailKey, (current) =>
      current ? applyDetail(current) : current,
    );
  }
  const id = cardCommandId(kind, cardId);
  const pending = pendingFor(queryClient);
  const boardHash = JSON.stringify(boardKey);
  pending.set(boardHash, (pending.get(boardHash) ?? 0) + 1);
  try {
    const result = await send({ correlationId: id, idempotencyKey: id });
    reconcile?.(result);
    return result;
  } catch (error) {
    queryClient.setQueryData<FullBoardResponse | undefined>(
      boardKey,
      (current) => restoreBoardCard(current, cardId, cardBefore),
    );
    if (applyDetail && detailBefore) {
      queryClient.setQueryData(detailKey, detailBefore);
    }
    throw error;
  } finally {
    const left = (pending.get(boardHash) ?? 1) - 1;
    if (left > 0) pending.set(boardHash, left);
    else pending.delete(boardHash);
    // Whether the item is archived or completed is read again with it.
    const lifecycleKey = workspaceQueryKey(
      workspaceId,
      "work-management",
      "item-lifecycle",
      cardId,
    );
    const keys =
      left > 0
        ? [detailKey, activityKey, lifecycleKey]
        : [boardKey, detailKey, activityKey, lifecycleKey];
    await Promise.all(
      keys.map((queryKey) => queryClient.invalidateQueries({ queryKey })),
    );
  }
}
