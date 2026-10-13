import type { DragEndEvent } from "@dnd-kit/core";
import type { Card, KanbanStatusLane } from "@notrelix/work-management-core";
import { generatePosition } from "@notrelix/work-management-core";

/**
 * What a card drop asks for:
 * - `status`: the card was dropped on another lane; write that lane's option
 *   (the grouping field's value, Status by default);
 * - `reorder`: the card was dropped on a card of its own lane and group; move
 *   it within the group, the value stays.
 */
export type KanbanLaneDrop =
  | {
      readonly kind: "status";
      readonly cardId: string;
      readonly optionId: string;
      /** The lane the card left; `null` for the lane without a value. */
      readonly fromOptionId: string | null;
    }
  | {
      readonly kind: "reorder";
      readonly cardId: string;
      readonly listId: string;
      readonly position: number;
      /** The new neighbours in the group's whole order (`null` = none). */
      readonly previousSiblingId: string | null;
      readonly nextSiblingId: string | null;
    };

/** A board group's cards in board order, across every lane. */
export interface KanbanGroupOrder {
  readonly id: string;
  readonly cards: readonly Pick<Card, "id" | "position">[];
}

/** Droppable id of a lane (`optionId: null` is the lane without a value). */
export function kanbanLaneDropId(optionId: string | null): string {
  return `kanban-lane:${optionId ?? "none"}`;
}

function laneOf(
  lanes: readonly KanbanStatusLane[],
  cardId: string,
): KanbanStatusLane | undefined {
  return lanes.find((lane) => lane.cards.some((card) => card.id === cardId));
}

/**
 * Reads a drag end over the lanes. A drop on the lane without a value never
 * clears the value, and a same-lane drop on a card of another group, on the
 * card itself or on the lane background changes nothing.
 */
export function getKanbanLaneDrop(
  event: DragEndEvent,
  lanes: readonly KanbanStatusLane[],
  groups?: readonly KanbanGroupOrder[],
): KanbanLaneDrop | null {
  if (!event.over) return null;
  if (event.active.data.current?.type !== "kanban-card") return null;
  const active = event.active.data.current?.card as Card | undefined;
  if (!active) return null;

  const overData = event.over.data.current;
  const overCard =
    overData?.type === "kanban-card"
      ? (overData.card as Card | undefined)
      : undefined;
  const target =
    overData?.type === "kanban-lane"
      ? (overData.lane as KanbanStatusLane | undefined)
      : overCard
        ? laneOf(lanes, overCard.id)
        : undefined;
  const source = laneOf(lanes, active.id);
  if (!target || !source) return null;

  if (target.optionId !== source.optionId) {
    if (target.optionId === null) return null;
    return {
      kind: "status",
      cardId: active.id,
      optionId: target.optionId,
      fromOptionId: source.optionId,
    };
  }

  if (!overCard || overCard.id === active.id) return null;
  if (overCard.listId !== active.listId) return null;
  const groupCards = source.cards
    .filter((card) => card.listId === active.listId && card.id !== active.id)
    .sort((a, b) => a.position - b.position);
  const overIndex = groupCards.findIndex((card) => card.id === overCard.id);
  const activeIndex = source.cards.findIndex((card) => card.id === active.id);
  const overLaneIndex = source.cards.findIndex(
    (card) => card.id === overCard.id,
  );
  // Moving down places the card after the one it is dropped on, moving up
  // places it before.
  const movingDown = activeIndex < overLaneIndex;
  const insertAt = movingDown ? overIndex + 1 : overIndex;
  // The server orders the whole group, cards of other lanes included: the
  // neighbours are taken from the group's full order when it is known.
  const groupOrder = groups
    ?.find((group) => group.id === active.listId)
    ?.cards.filter((card) => card.id !== active.id)
    .slice()
    .sort((a, b) => a.position - b.position);
  const fullOver = groupOrder?.findIndex((card) => card.id === overCard.id);
  const before = groupOrder
    ? groupOrder[movingDown ? fullOver! : fullOver! - 1]
    : groupCards[insertAt - 1];
  const after = groupOrder
    ? groupOrder[movingDown ? fullOver! + 1 : fullOver!]
    : groupCards[insertAt];
  return {
    kind: "reorder",
    cardId: active.id,
    listId: active.listId,
    position: generatePosition(
      groupCards[insertAt - 1]?.position,
      groupCards[insertAt]?.position,
    ),
    previousSiblingId: before?.id ?? null,
    nextSiblingId: after?.id ?? null,
  };
}
