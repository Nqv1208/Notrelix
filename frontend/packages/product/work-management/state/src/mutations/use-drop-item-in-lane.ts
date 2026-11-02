import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import {
  updateCardInFullBoard,
  type FullBoardResponse,
  type MoveCardInput,
  type UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import { findBoardCard, placeBoardCard } from "../cache/card-restore";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { outcomeUnknown, runCardCommand } from "./card-command";
import { UnknownItemVersionError, type ItemMoveRequest } from "./use-move-card";
import { applyFieldValue } from "./use-update-field-value";
import type { CardWriteHookOptions } from "./use-update-item-status";

/** The lane a card is dropped in: an option of the field the board is grouped by (Status or another Select). */
export interface LaneValue {
  readonly fieldDefinitionId: string;
  readonly optionId: string;
}

export interface DropItemInLaneVariables {
  readonly workspaceId: string;
  readonly boardId: string;
  readonly cardId: string;
  readonly lane: LaneValue;
  /** Where in the group the card goes, named by its new neighbours. */
  readonly placement: Pick<
    MoveCardInput,
    "listId" | "position" | "previousSiblingId" | "nextSiblingId"
  >;
}

interface LaneDropWriter {
  moveCard(
    move: ItemMoveRequest,
    options?: ApiRequestOptions,
  ): Promise<{ version?: number } | void>;
}

/** A drop sent without an answer: a repeat of it must send the same key and version. */
interface UnknownOutcome {
  readonly signature: string;
  readonly idempotencyKey: string;
  readonly expectedVersion: number;
}

const unknownByClient = new WeakMap<QueryClient, Map<string, UnknownOutcome>>();

function unknownOutcomes(
  queryClient: QueryClient,
): Map<string, UnknownOutcome> {
  let unknown = unknownByClient.get(queryClient);
  if (!unknown) {
    unknown = new Map();
    unknownByClient.set(queryClient, unknown);
  }
  return unknown;
}

/**
 * A card dropped between two cards of another lane: it takes the lane's value
 * and the new place in its group in one optimistic step, so it sits exactly
 * where the drag showed it. The server gets one command (the move with the
 * lane's field and option) in one transaction, so it keeps both or neither.
 * A failure puts only this card back locally, rethrows, and the board read is
 * refetched, which shows what the server really kept. A repeat of a drop whose
 * answer never came keeps its idempotency key and version, so the server
 * replays the first result instead of refusing the moved version as stale.
 */
export function runDropItemInLane(
  queryClient: QueryClient,
  cards: LaneDropWriter,
  variables: DropItemInLaneVariables,
): Promise<void> {
  const boardKey = wmQueryKeys.fullBoard(
    variables.workspaceId,
    variables.boardId,
  );
  const board = queryClient.getQueryData<FullBoardResponse>(boardKey);
  const card = findBoardCard(board, variables.cardId);
  const { placement, lane } = variables;
  const signature = JSON.stringify({ lane, placement });
  const unknown = unknownOutcomes(queryClient);
  const kept = unknown.get(variables.cardId);
  const expectedVersion =
    kept?.signature === signature ? kept.expectedVersion : card?.version;
  if (!expectedVersion || expectedVersion <= 0) {
    return Promise.reject(new UnknownItemVersionError(variables.cardId));
  }
  const fields = board?.board.fieldDefinitions ?? [];
  const members = board?.board.members ?? [];
  const payload: UpdateFieldValueInput = {
    cardId: variables.cardId,
    fieldDefinitionId: lane.fieldDefinitionId,
    value: lane.optionId,
  };
  return runCardCommand({
    queryClient,
    workspaceId: variables.workspaceId,
    boardId: variables.boardId,
    cardId: variables.cardId,
    kind: "drop-item-in-lane",
    applyBoard: (current) => {
      const valued =
        updateCardInFullBoard(current, variables.cardId, (candidate) =>
          applyFieldValue(candidate, payload, fields, members),
        ) ?? current;
      return (
        placeBoardCard(
          valued,
          variables.cardId,
          placement.listId,
          placement.position,
        ) ?? valued
      );
    },
    send: (context) => {
      const idempotencyKey =
        kept?.signature === signature
          ? kept.idempotencyKey
          : context.idempotencyKey;
      return cards
        .moveCard(
          {
            cardId: variables.cardId,
            listId: placement.listId,
            expectedVersion,
            previousSiblingId: placement.previousSiblingId ?? null,
            nextSiblingId: placement.nextSiblingId ?? null,
            laneFieldId: lane.fieldDefinitionId,
            laneOptionId: lane.optionId,
          },
          { ...context, idempotencyKey },
        )
        .then(
          (moved) => {
            unknown.delete(variables.cardId);
            return moved;
          },
          (error: unknown) => {
            if (outcomeUnknown(error))
              unknown.set(variables.cardId, {
                signature,
                idempotencyKey,
                expectedVersion,
              });
            else unknown.delete(variables.cardId);
            throw error;
          },
        );
    },
    reconcile: (moved) => {
      // The server's new version, so a next write before the refetch is not stale.
      const version = moved && "version" in moved ? moved.version : undefined;
      if (typeof version !== "number") return;
      queryClient.setQueryData<FullBoardResponse | undefined>(boardKey, (old) =>
        updateCardInFullBoard(old, variables.cardId, (done) => ({
          ...done,
          version,
        })),
      );
    },
  }).then(() => undefined);
}

type DropInput = Pick<DropItemInLaneVariables, "cardId" | "lane" | "placement">;

/** Drag between lanes onto a chosen place: the lane's value and the order, in one command. */
export function useDropItemInLane(
  boardId: string,
  workspaceId: string,
  options: CardWriteHookOptions<DropInput> = {},
) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: DropInput) =>
      runDropItemInLane(queryClient, cards, { workspaceId, boardId, ...input }),
    onError: options.onError,
  });
}
