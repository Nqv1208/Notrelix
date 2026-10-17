import type { Card, FullBoardResponse } from "@notrelix/work-management-core";

/** The card with this id in the board read, or undefined. */
export function findBoardCard(
  board: FullBoardResponse | undefined,
  cardId: string,
): Card | undefined {
  for (const group of board?.groups ?? []) {
    const card = group.cards.find((candidate) => candidate.id === cardId);
    if (card) return card;
  }
  return undefined;
}

/**
 * Puts one card back as it was (`snapshot`), or removes it when it did not
 * exist before (`undefined`, e.g. an optimistic new card). Every other card
 * stays as it is now, so another card's in-flight change is not undone.
 */
export function restoreBoardCard(
  board: FullBoardResponse | undefined,
  cardId: string,
  snapshot: Card | undefined,
): FullBoardResponse | undefined {
  if (!board) return board;
  return {
    ...board,
    groups: board.groups.map((group) => {
      const others = group.cards.filter((card) => card.id !== cardId);
      if (!snapshot || group.id !== snapshot.listId) {
        return others.length === group.cards.length
          ? group
          : { ...group, cards: others };
      }
      return {
        ...group,
        cards: [...others, snapshot].sort((a, b) => a.position - b.position),
      };
    }),
  };
}

/** Moves one card to a group at a position (optimistic move). */
export function placeBoardCard(
  board: FullBoardResponse | undefined,
  cardId: string,
  listId: string,
  position: number,
): FullBoardResponse | undefined {
  const card = findBoardCard(board, cardId);
  if (!board || !card) return board;
  return restoreBoardCard(board, cardId, { ...card, listId, position });
}
