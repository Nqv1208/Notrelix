import type { FullBoardResponse } from "../types"

export function updateCardInFullBoard(
  old: FullBoardResponse | undefined,
  cardId: string,
  updater: (card: FullBoardResponse["groups"][number]["cards"][number]) => FullBoardResponse["groups"][number]["cards"][number]
) {
  if (!old) return old
  return {
    ...old,
    groups: old.groups.map((group) => ({
      ...group,
      cards: group.cards.map((card) => (card.id === cardId ? updater(card) : card)),
    })),
  }
}
