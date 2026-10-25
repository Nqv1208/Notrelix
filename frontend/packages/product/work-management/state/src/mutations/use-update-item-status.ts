import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import {
  findStatusField,
  updateCardInFullBoard,
  type FullBoardResponse,
} from "@notrelix/work-management-core";
import { addOptimisticCard } from "../cache/optimistic-card";
import {
  applyItemDetailStatus,
  applyItemStatus,
  withItemStatus,
} from "../cache/item-status";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { cardCommandId, runCardCommand } from "./card-command";

export interface UpdateItemStatusVariables {
  readonly workspaceId: string;
  readonly boardId: string;
  readonly cardId: string;
  readonly statusOptionId: string;
}

export interface CreateItemInLaneVariables {
  readonly workspaceId: string;
  readonly boardId: string;
  /** Board group the item is created in. */
  readonly listId: string;
  readonly title: string;
  /** The lane's Status option; `null` creates the item without a status. */
  readonly statusOptionId: string | null;
  /**
   * The lane's value of another Select field, when the board is grouped by
   * one (the item is created without a status then).
   */
  readonly fieldValue?: {
    readonly fieldDefinitionId: string;
    readonly value: string;
  } | null;
}

interface ItemStatusWriter {
  updateStatus(
    cardId: string,
    statusOptionId: string,
    options?: ApiRequestOptions,
  ): Promise<void>;
}

interface ItemCreator extends ItemStatusWriter {
  createCard(
    boardId: string,
    payload: { listId: string; title: string },
    options?: ApiRequestOptions,
  ): Promise<string | undefined>;
  updateFieldValue?(payload: {
    cardId: string;
    fieldDefinitionId: string;
    value: unknown;
  }): Promise<void>;
}

/**
 * Hook-level error reporting: it runs for every write, also when a later
 * write of the same hook has replaced the observer or the view has unmounted.
 */
export interface CardWriteHookOptions<TVariables> {
  readonly onError?: (error: unknown, variables: TVariables) => void;
}

/**
 * Writes an item's Status (`PATCH /board-items/{id}/status`). The board read
 * and the item detail take the option first, so the card is already in its
 * new lane; a failed write puts only this card back and rethrows, and the
 * reads are refetched either way.
 */
export function runItemStatusCommand(
  queryClient: QueryClient,
  cards: ItemStatusWriter,
  variables: UpdateItemStatusVariables,
): Promise<void> {
  const fields =
    queryClient.getQueryData<FullBoardResponse>(
      wmQueryKeys.fullBoard(variables.workspaceId, variables.boardId),
    )?.board.fieldDefinitions ?? [];
  return runCardCommand({
    queryClient,
    workspaceId: variables.workspaceId,
    boardId: variables.boardId,
    cardId: variables.cardId,
    kind: "item-status",
    applyBoard: (board) =>
      applyItemStatus(board, variables.cardId, variables.statusOptionId) ??
      board,
    applyDetail: (detail) =>
      applyItemDetailStatus(detail, fields, variables.statusOptionId) ?? detail,
    send: (context) =>
      cards.updateStatus(variables.cardId, variables.statusOptionId, context),
  });
}

/**
 * "Thêm vào {lane}": creates the item in a board group, then sets the lane's
 * Status (or the lane's value of the Select field the board is grouped by).
 * The board shows the new card in the lane at once; a failure of either
 * write removes only that card again and rethrows (an item that was created
 * before the second write failed comes back from the refetch without it).
 */
export function runCreateItemInLane(
  queryClient: QueryClient,
  cards: ItemCreator,
  variables: CreateItemInLaneVariables,
): Promise<void> {
  const optimisticId = `optimistic-${crypto.randomUUID()}`;
  return runCardCommand({
    queryClient,
    workspaceId: variables.workspaceId,
    boardId: variables.boardId,
    cardId: optimisticId,
    kind: "create-item",
    applyBoard: (board) => {
      const added = addOptimisticCard(
        board,
        { listId: variables.listId, title: variables.title },
        optimisticId,
      );
      if (!added) return board;
      const field = findStatusField(added.board.fieldDefinitions);
      const optionId = variables.statusOptionId;
      const laneValue = variables.fieldValue;
      return (
        updateCardInFullBoard(added, optimisticId, (card) => {
          const withStatus =
            field && optionId ? withItemStatus(card, field, optionId) : card;
          return {
            ...withStatus,
            fieldValues: laneValue
              ? {
                  ...withStatus.fieldValues,
                  [laneValue.fieldDefinitionId]: laneValue.value,
                }
              : withStatus.fieldValues,
            // The legacy optimistic card guesses a priority; the server sets none.
            priority: undefined,
          };
        }) ?? added
      );
    },
    send: async (context) => {
      const createdId = await cards.createCard(
        variables.boardId,
        { listId: variables.listId, title: variables.title },
        context,
      );
      if (!createdId) return;
      if (variables.statusOptionId) {
        const statusKey = cardCommandId("item-status", createdId);
        await cards.updateStatus(createdId, variables.statusOptionId, {
          correlationId: context.correlationId,
          idempotencyKey: statusKey,
        });
      }
      if (variables.fieldValue && cards.updateFieldValue) {
        await cards.updateFieldValue({
          cardId: createdId,
          fieldDefinitionId: variables.fieldValue.fieldDefinitionId,
          value: variables.fieldValue.value,
        });
      }
    },
  });
}

type StatusInput = { cardId: string; statusOptionId: string };

/** Status write for drag between lanes and the sheet's status pill. */
export function useUpdateItemStatus(
  boardId: string,
  workspaceId: string,
  options: CardWriteHookOptions<StatusInput> = {},
) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: StatusInput) =>
      runItemStatusCommand(queryClient, cards, {
        workspaceId,
        boardId,
        ...input,
      }),
    onError: options.onError,
  });
}

type CreateInput = {
  listId: string;
  title: string;
  statusOptionId: string | null;
  fieldValue?: { fieldDefinitionId: string; value: string } | null;
};

/** Creates an item in a lane ("Thêm vào {lane}"). */
export function useCreateItemInLane(
  boardId: string,
  workspaceId: string,
  options: CardWriteHookOptions<CreateInput> = {},
) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: CreateInput) =>
      runCreateItemInLane(queryClient, cards, {
        workspaceId,
        boardId,
        ...input,
      }),
    onError: options.onError,
  });
}
