import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import { updateCardInFullBoard } from "@notrelix/work-management-core";
import { addOptimisticCard } from "../cache/optimistic-card";
import { useWorkManagementServices } from "../services";
import { runCardCommand } from "./card-command";
import { applyCardPatch } from "./use-update-card";

export interface CreateDatedItemVariables {
  readonly workspaceId: string;
  readonly boardId: string;
  /** Board group the item is created in. */
  readonly listId: string;
  readonly title: string;
  /** The due date and time as an ISO 8601 string. */
  readonly dueDate: string;
}

interface DatedItemWriter {
  createCard(
    boardId: string,
    payload: { listId: string; title: string; dueDate: string },
    options?: ApiRequestOptions,
  ): Promise<{ id: string; version: number } | undefined>;
}

/**
 * Creates an item that already has a due date (the Calendar's quick create)
 * in one write: the item is born with its date. The board shows the new card
 * on its day at once; a failure removes only that card again and rethrows.
 */
export function runCreateDatedItem(
  queryClient: QueryClient,
  cards: DatedItemWriter,
  variables: CreateDatedItemVariables,
): Promise<void> {
  const optimisticId = `optimistic-${crypto.randomUUID()}`;
  return runCardCommand({
    queryClient,
    workspaceId: variables.workspaceId,
    boardId: variables.boardId,
    cardId: optimisticId,
    kind: "create-dated-item",
    applyBoard: (board) => {
      const added = addOptimisticCard(
        board,
        { listId: variables.listId, title: variables.title },
        optimisticId,
      );
      if (!added) return board;
      return (
        updateCardInFullBoard(added, optimisticId, (card) => ({
          ...applyCardPatch(card, { dueDate: variables.dueDate }),
          // The legacy optimistic card guesses a priority; the server sets none.
          priority: undefined,
        })) ?? added
      );
    },
    send: async (context) => {
      await cards.createCard(
        variables.boardId,
        {
          listId: variables.listId,
          title: variables.title,
          dueDate: variables.dueDate,
        },
        context,
      );
    },
  });
}

export function useCreateDatedItem(
  boardId: string,
  workspaceId: string,
  options: {
    readonly onError?: (
      error: unknown,
      variables: Pick<CreateDatedItemVariables, "title">,
    ) => void;
  } = {},
) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (
      input: Omit<CreateDatedItemVariables, "boardId" | "workspaceId">,
    ) =>
      runCreateDatedItem(queryClient, cards, {
        workspaceId,
        boardId,
        ...input,
      }),
    onError: options.onError,
  });
}
