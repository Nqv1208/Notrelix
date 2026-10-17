import {
  findStatusField,
  updateCardInFullBoard,
  type Card,
  type CardDetail,
  type FieldDefinition,
  type FullBoardResponse,
} from "@notrelix/work-management-core";

/** The card with its Status option set to `optionId` (option of `field`). */
export function withItemStatus<T extends Card>(
  card: T,
  field: FieldDefinition,
  optionId: string,
): T {
  const option = field.options.find((candidate) => candidate.id === optionId);
  if (!option) return card;
  return {
    ...card,
    status: optionId,
    statusOptionId: optionId,
    statusCategory: option.category ?? card.statusCategory ?? null,
    fieldValues: { ...card.fieldValues, [field.id]: optionId },
  };
}

/**
 * Optimistic Status change in the board read: the card takes the option, so
 * the status lanes (`groupCardsByStatus`) show it in its new lane. An unknown
 * card or option leaves the board as it is.
 */
export function applyItemStatus(
  fullBoard: FullBoardResponse | undefined,
  cardId: string,
  optionId: string,
): FullBoardResponse | undefined {
  if (!fullBoard) return fullBoard;
  const field = findStatusField(fullBoard.board.fieldDefinitions);
  if (!field?.options.some((option) => option.id === optionId)) {
    return fullBoard;
  }
  const exists = fullBoard.groups.some((group) =>
    group.cards.some((card) => card.id === cardId),
  );
  if (!exists) return fullBoard;
  return updateCardInFullBoard(fullBoard, cardId, (card) =>
    withItemStatus(card, field, optionId),
  );
}

/** Optimistic Status change in the item detail read. */
export function applyItemDetailStatus(
  detail: CardDetail | undefined,
  fields: readonly FieldDefinition[],
  optionId: string,
): CardDetail | undefined {
  if (!detail) return detail;
  const field = findStatusField(fields);
  return field ? withItemStatus(detail, field, optionId) : detail;
}
