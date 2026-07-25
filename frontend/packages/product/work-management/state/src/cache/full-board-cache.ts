import type { FullBoardResponse, Card } from "@notrelix/work-management-core";
import { updateCardInFullBoard } from "@notrelix/work-management-core";

export { updateCardInFullBoard };

export function updateFullBoardCache(
  old: FullBoardResponse | undefined,
  cardId: string,
  updater: (card: Card) => Partial<Card>
): FullBoardResponse | undefined {
  if (!old) return old;
  return updateCardInFullBoard(old, cardId, (card) => ({
    ...card,
    ...updater(card) as Card,
  }));
}
