import {
  findPriorityField,
  priorityFromOptionName,
  type Board,
  type CardDetail,
} from "@notrelix/work-management-core";

/**
 * The item with its priority read from the board's priority field. The item
 * read carries the selected option id, not a priority word; an item without
 * a selected option keeps no priority rather than a default.
 */
export function withBoardPriority(card: CardDetail, board: Board): CardDetail {
  if (card.priority || !card.priorityOptionId) return card;
  const option = findPriorityField(board.fieldDefinitions)?.options.find(
    (entry) => entry.id === card.priorityOptionId,
  );
  const priority = priorityFromOptionName(option?.label);
  return priority ? { ...card, priority } : card;
}
