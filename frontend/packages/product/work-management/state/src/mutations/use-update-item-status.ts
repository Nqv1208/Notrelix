import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import {
  defineOptimisticUpdate,
  executeOptimisticCommand,
} from "@notrelix/query";
import {
  findStatusField,
  updateCardInFullBoard,
  type CardDetail,
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
 * A command id that is unique across reloads and tabs: it doubles as the
 * idempotency key, and a key reused with another payload is a conflict.
 */
function commandId(kind: string, subject: string): string {
  return `${kind}:${subject}:${crypto.randomUUID()}`;
}

/**
 * Writes an item's Status (`PATCH /board-items/{id}/status`). The board read
 * and the item detail take the option first, so the card is already in its
 * new lane; a failed write puts both back and rethrows, and both reads are
 * refetched either way.
 */
export function runItemStatusCommand(
  queryClient: QueryClient,
  cards: ItemStatusWriter,
  variables: UpdateItemStatusVariables,
): Promise<void> {
  const boardKey = wmQueryKeys.fullBoard(
    variables.workspaceId,
    variables.boardId,
  );
  const detailKey = wmQueryKeys.cardDetail(
    variables.workspaceId,
    variables.cardId,
  );
  const fields =
    queryClient.getQueryData<FullBoardResponse>(boardKey)?.board
      .fieldDefinitions ?? [];
  const id = commandId("item-status", variables.cardId);
  return executeOptimisticCommand({
    queryClient,
    commandId: id,
    updates: [
      defineOptimisticUpdate<
        FullBoardResponse | undefined,
        UpdateItemStatusVariables
      >(boardKey, (current, input) =>
        applyItemStatus(current, input.cardId, input.statusOptionId),
      ),
      defineOptimisticUpdate<CardDetail | undefined, UpdateItemStatusVariables>(
        detailKey,
        (current, input) =>
          applyItemDetailStatus(current, fields, input.statusOptionId),
      ),
    ],
    mutationFn: (input, context) =>
      cards.updateStatus(input.cardId, input.statusOptionId, {
        correlationId: context.correlationId,
        idempotencyKey: context.idempotencyKey,
      }),
    variables,
  });
}

/**
 * "Thêm vào {lane}": creates the item in a board group, then sets the lane's
 * Status (or the lane's value of the Select field the board is grouped by). The board shows the new card in the lane at once; a failure of
 * either write removes it again and rethrows (an item that was created
 * before the status write failed comes back from the refetch without it).
 */
export function runCreateItemInLane(
  queryClient: QueryClient,
  cards: ItemCreator,
  variables: CreateItemInLaneVariables,
): Promise<void> {
  const boardKey = wmQueryKeys.fullBoard(
    variables.workspaceId,
    variables.boardId,
  );
  const id = commandId("create-item", variables.boardId);
  const optimisticId = `optimistic-${id}`;
  return executeOptimisticCommand({
    queryClient,
    commandId: id,
    updates: [
      defineOptimisticUpdate<
        FullBoardResponse | undefined,
        CreateItemInLaneVariables
      >(boardKey, (current, input) => {
        const added = addOptimisticCard(
          current,
          { listId: input.listId, title: input.title },
          optimisticId,
        );
        if (!added) return added;
        const field = findStatusField(added.board.fieldDefinitions);
        const optionId = input.statusOptionId;
        const laneValue = input.fieldValue;
        return updateCardInFullBoard(added, optimisticId, (card) => {
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
        });
      }),
    ],
    mutationFn: async (input, context) => {
      const createdId = await cards.createCard(
        input.boardId,
        { listId: input.listId, title: input.title },
        {
          correlationId: context.correlationId,
          idempotencyKey: context.idempotencyKey,
        },
      );
      if (!createdId) return;
      if (input.statusOptionId) {
        await cards.updateStatus(createdId, input.statusOptionId, {
          correlationId: context.correlationId,
          idempotencyKey: commandId("item-status", createdId),
        });
      }
      if (input.fieldValue && cards.updateFieldValue) {
        await cards.updateFieldValue({
          cardId: createdId,
          fieldDefinitionId: input.fieldValue.fieldDefinitionId,
          value: input.fieldValue.value,
        });
      }
    },
    variables,
  });
}

/** Status write for drag between lanes and the sheet's status pill. */
export function useUpdateItemStatus(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: { cardId: string; statusOptionId: string }) =>
      runItemStatusCommand(queryClient, cards, {
        workspaceId,
        boardId,
        ...input,
      }),
  });
}

/** Creates an item in a lane ("Thêm vào {lane}"). */
export function useCreateItemInLane(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: {
      listId: string;
      title: string;
      statusOptionId: string | null;
      fieldValue?: { fieldDefinitionId: string; value: string } | null;
    }) =>
      runCreateItemInLane(queryClient, cards, {
        workspaceId,
        boardId,
        ...input,
      }),
  });
}
