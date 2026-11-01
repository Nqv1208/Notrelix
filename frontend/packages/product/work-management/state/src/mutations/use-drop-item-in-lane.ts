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
import { cardCommandId, runCardCommand } from "./card-command";
import { UnknownItemVersionError, type ItemMoveRequest } from "./use-move-card";
import { applyFieldValue } from "./use-update-field-value";
import type { CardWriteHookOptions } from "./use-update-item-status";

/** The lane a card is dropped in: an option of the field the board is grouped by. */
export interface LaneValue {
  readonly fieldDefinitionId: string;
  readonly optionId: string;
  /** Status goes through the status endpoint, any other Select through its field value. */
  readonly viaStatusEndpoint: boolean;
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
  updateStatus(
    cardId: string,
    statusOptionId: string,
    expectedVersion: number,
    options?: ApiRequestOptions,
  ): Promise<unknown>;
  updateFieldValue(
    payload: UpdateFieldValueInput,
    options: ApiRequestOptions & { readonly expectedVersion: number },
  ): Promise<unknown>;
}

/**
 * A card dropped between two cards of another lane: it takes the lane's value
 * and the new place in its group in one optimistic step, so it sits exactly
 * where the drag showed it. The server gets the move first (it names the version
 * the person saw) and the lane's value second (it names the version the move
 * returned). A failure of the move changes nothing on the server.
 * A failure of the value write after the move puts the card back in its old
 * place on the server too (best effort, with the version the move returned), so
 * the card is not left reordered in the lane it never left. Either failure
 * puts only this card back locally, rethrows, and the board read is refetched,
 * which shows what the server really kept.
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
  const expectedVersion = card?.version ?? null;
  if (!expectedVersion || expectedVersion <= 0) {
    return Promise.reject(new UnknownItemVersionError(variables.cardId));
  }
  const fields = board?.board.fieldDefinitions ?? [];
  const members = board?.board.members ?? [];
  const payload: UpdateFieldValueInput = {
    cardId: variables.cardId,
    fieldDefinitionId: variables.lane.fieldDefinitionId,
    value: variables.lane.optionId,
  };
  const { placement } = variables;
  // Where the card was in its group, for undoing the move if the value write fails.
  const origin = (() => {
    const others = (
      board?.groups.find((group) => group.id === card!.listId)?.cards ?? []
    )
      .filter((candidate) => candidate.id !== variables.cardId)
      .sort((a, b) => a.position - b.position);
    return {
      listId: card!.listId,
      previousSiblingId:
        others.filter((other) => other.position < card!.position).at(-1)?.id ??
        null,
      nextSiblingId:
        others.find((other) => other.position > card!.position)?.id ?? null,
    };
  })();
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
    send: async (context) => {
      const moved = await cards.moveCard(
        {
          cardId: variables.cardId,
          listId: placement.listId,
          expectedVersion,
          previousSiblingId: placement.previousSiblingId ?? null,
          nextSiblingId: placement.nextSiblingId ?? null,
        },
        context,
      );
      const version = moved && "version" in moved ? moved.version : undefined;
      try {
        if (typeof version !== "number") {
          throw new UnknownItemVersionError(variables.cardId);
        }
        if (variables.lane.viaStatusEndpoint) {
          await cards.updateStatus(
            variables.cardId,
            variables.lane.optionId,
            version,
            {
              correlationId: context.correlationId,
              idempotencyKey: cardCommandId("item-status", variables.cardId),
            },
          );
        } else {
          await cards.updateFieldValue(payload, {
            expectedVersion: version,
          });
        }
      } catch (error) {
        if (typeof version === "number") {
          await cards
            .moveCard(
              { cardId: variables.cardId, expectedVersion: version, ...origin },
              {
                correlationId: context.correlationId,
                idempotencyKey: cardCommandId("undo-move", variables.cardId),
              },
            )
            .catch(() => undefined);
        }
        throw error;
      }
    },
  });
}

type DropInput = Pick<DropItemInLaneVariables, "cardId" | "lane" | "placement">;

/** Drag between lanes onto a chosen place (status or Select value, then order). */
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
