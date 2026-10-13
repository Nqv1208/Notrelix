import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import type {
  FullBoardResponse,
  MoveCardInput,
} from "@notrelix/work-management-core";
import { findBoardCard, placeBoardCard } from "../cache/card-restore";
import { updateCardInFullBoard } from "../cache/full-board-cache";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { runCardCommand } from "./card-command";
import type { CardWriteHookOptions } from "./use-update-item-status";

/** The move as the server takes it: group, version and new neighbours. */
export interface ItemMoveRequest {
  readonly cardId: string;
  readonly listId: string;
  readonly expectedVersion: number;
  readonly previousSiblingId: string | null;
  readonly nextSiblingId: string | null;
}

interface ItemMover {
  moveCard(
    move: ItemMoveRequest,
    options?: ApiRequestOptions,
  ): Promise<{ version?: number } | void>;
}

/** Thrown before sending when the board read carries no item version. */
export class UnknownItemVersionError extends Error {
  constructor(cardId: string) {
    super(
      `The version of item ${cardId} is unknown; reload the board before moving it.`,
    );
    this.name = "UnknownItemVersionError";
  }
}

/**
 * The neighbours of a card placed at `position` in a group, in the group's
 * order and without the card itself.
 */
function neighboursAt(
  board: FullBoardResponse | undefined,
  input: MoveCardInput,
): { previousSiblingId: string | null; nextSiblingId: string | null } {
  if (
    input.previousSiblingId !== undefined ||
    input.nextSiblingId !== undefined
  ) {
    return {
      previousSiblingId: input.previousSiblingId ?? null,
      nextSiblingId: input.nextSiblingId ?? null,
    };
  }
  const others = (
    board?.groups.find((group) => group.id === input.listId)?.cards ?? []
  )
    .filter((card) => card.id !== input.cardId)
    .sort((a, b) => a.position - b.position);
  const nextIndex = others.findIndex((card) => card.position >= input.position);
  const next = nextIndex === -1 ? undefined : others[nextIndex];
  const previous = nextIndex === -1 ? others.at(-1) : others[nextIndex - 1];
  return {
    previousSiblingId: previous?.id ?? null,
    nextSiblingId: next?.id ?? null,
  };
}

/**
 * Moves an item (`POST /board-items/{id}/move`) with its version and new
 * neighbours, so the order the user chose is the order the server keeps.
 * The card moves at once; a failure puts only this card back and rethrows.
 * Every attempt has a fresh idempotency key.
 */
export function runMoveCardCommand(
  queryClient: QueryClient,
  cards: ItemMover,
  {
    workspaceId,
    boardId,
    input,
  }: { workspaceId: string; boardId: string; input: MoveCardInput },
): Promise<void> {
  const boardKey = wmQueryKeys.fullBoard(workspaceId, boardId);
  const board = queryClient.getQueryData<FullBoardResponse>(boardKey);
  const card = findBoardCard(board, input.cardId);
  const expectedVersion = input.expectedVersion ?? card?.version ?? null;
  if (!expectedVersion || expectedVersion <= 0) {
    return Promise.reject(new UnknownItemVersionError(input.cardId));
  }
  const neighbours = neighboursAt(board, input);
  return runCardCommand({
    queryClient,
    workspaceId,
    boardId,
    cardId: input.cardId,
    kind: "move-card",
    applyBoard: (current) =>
      placeBoardCard(current, input.cardId, input.listId, input.position) ??
      current,
    send: (context) =>
      cards.moveCard(
        {
          cardId: input.cardId,
          listId: input.listId,
          expectedVersion,
          ...neighbours,
        },
        context,
      ),
    reconcile: (result) => {
      // The server's new version, so a next move before the refetch is not stale.
      const version =
        result && "version" in result ? result.version : undefined;
      if (typeof version !== "number") return;
      queryClient.setQueryData<FullBoardResponse | undefined>(boardKey, (old) =>
        updateCardInFullBoard(old, input.cardId, (moved) => ({
          ...moved,
          version,
        })),
      );
    },
  }).then(() => undefined);
}

export function useMoveCard(
  boardId: string,
  workspaceId: string,
  options: CardWriteHookOptions<MoveCardInput> = {},
) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: MoveCardInput) =>
      runMoveCardCommand(queryClient, cards, { workspaceId, boardId, input }),
    onError: options.onError,
  });
}
