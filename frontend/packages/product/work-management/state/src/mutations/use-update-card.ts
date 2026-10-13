import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import {
  updateCardInFullBoard,
  type Card,
  type UpdateCardInput,
} from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";
import { runCardCommand } from "./card-command";

interface CardUpdater {
  updateCard(cardId: string, patch: UpdateCardInput): Promise<void>;
}

/** A card with the patch applied (title, description, priority, dates). */
export function applyCardPatch<T extends Card>(
  card: T,
  patch: UpdateCardInput,
): T {
  return {
    ...card,
    title: patch.title ?? card.title,
    descriptionMd: patch.descriptionMd ?? card.descriptionMd,
    priority:
      patch.priority === null ? undefined : (patch.priority ?? card.priority),
    dueDate:
      patch.dueDate === null ? undefined : (patch.dueDate ?? card.dueDate),
    // The board read carries the due date as `dueAt`; keep it in step.
    dueAt: patch.dueDate === undefined ? card.dueAt : patch.dueDate,
    startDate:
      patch.startDate === null
        ? undefined
        : (patch.startDate ?? card.startDate),
    fieldValues: {
      ...card.fieldValues,
      [`${card.boardId}-field-title`]:
        patch.title ?? card.fieldValues[`${card.boardId}-field-title`],
      [`${card.boardId}-field-due-date`]:
        patch.dueDate ?? card.fieldValues[`${card.boardId}-field-due-date`],
      [`${card.boardId}-field-priority`]:
        patch.priority ?? card.fieldValues[`${card.boardId}-field-priority`],
    },
    updatedAt: new Date().toISOString(),
  };
}

/**
 * Updates an item (title and other item fields). The board read and the item
 * detail take the change at once; a failure puts only this card back.
 */
export function runUpdateCardCommand(
  queryClient: QueryClient,
  cards: CardUpdater,
  {
    workspaceId,
    boardId,
    cardId,
    patch,
  }: {
    workspaceId: string;
    boardId: string;
    cardId: string;
    patch: UpdateCardInput;
  },
): Promise<void> {
  return runCardCommand({
    queryClient,
    workspaceId,
    boardId,
    cardId,
    kind: "update-item",
    applyBoard: (current) =>
      updateCardInFullBoard(current, cardId, (card) =>
        applyCardPatch(card, patch),
      ) ?? current,
    applyDetail: (detail) => applyCardPatch(detail, patch),
    send: () => cards.updateCard(cardId, patch),
  });
}

export function useUpdateCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: ({
      cardId,
      patch,
    }: {
      cardId: string;
      patch: UpdateCardInput;
    }) =>
      runUpdateCardCommand(queryClient, cards, {
        workspaceId: workspaceId!,
        boardId,
        cardId,
        patch,
      }),
  });
}
