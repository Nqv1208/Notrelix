import type { DragEndEvent } from "@dnd-kit/core";
import type { BoardGroup, Card } from "@notrelix/work-management-core";
import { generatePosition } from "@notrelix/work-management-core";

export interface KanbanCardMove {
  readonly cardId: string;
  readonly listId: string;
  readonly position: number;
}

export function getKanbanCardMove(
  event: DragEndEvent,
  columns: readonly BoardGroup[],
): KanbanCardMove | null {
  if (!event.over) return null;

  const activeType = event.active.data.current?.type;
  const overType = event.over.data.current?.type;

  if (activeType !== "kanban-card") return null;

  const activeCard = event.active.data.current?.card as Card | undefined;
  const targetGroupId =
    overType === "kanban-card"
      ? (event.over.data.current?.card as Card | undefined)?.listId
      : overType === "kanban-column"
        ? String(event.over.id)
        : undefined;

  if (!activeCard || !targetGroupId) return null;

  const targetGroup = columns.find((group) => group.id === targetGroupId);
  if (!targetGroup) return null;

  const overCard =
    overType === "kanban-card"
      ? (event.over.data.current?.card as Card | undefined)
      : undefined;
  const orderedCards = targetGroup.cards
    .filter((card) => card.id !== activeCard.id)
    .sort((a, b) => a.position - b.position);
  const overIndex = overCard
    ? orderedCards.findIndex((card) => card.id === overCard.id)
    : orderedCards.length;
  const before = orderedCards[overIndex - 1]?.position;
  const after = orderedCards[overIndex]?.position;

  return {
    cardId: activeCard.id,
    listId: targetGroupId,
    position: generatePosition(before, after),
  };
}
