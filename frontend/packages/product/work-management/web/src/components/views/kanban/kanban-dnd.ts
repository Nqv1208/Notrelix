import type { Card, KanbanStatusLane } from "@notrelix/work-management-core";
import { generatePosition } from "@notrelix/work-management-core";

/**
 * What a card drop asks for:
 * - `status`: the card was dropped on another lane and its natural place there
 *   is the place it was dropped at; write that lane's option (the grouping
 *   field's value, Status by default);
 * - `reorder`: the card stays in its lane and moves within its group, the
 *   value stays;
 * - `place`: the card was dropped on another lane between two cards that its
 *   natural place does not give; write the lane's option and the new place.
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
    }
  | {
      readonly kind: "place";
      readonly cardId: string;
      readonly optionId: string;
      readonly fromOptionId: string | null;
      readonly listId: string;
      readonly position: number;
      readonly previousSiblingId: string | null;
      readonly nextSiblingId: string | null;
    };

/** A board group's cards in board order, across every lane. */
export interface KanbanGroupOrder {
  readonly id: string;
  readonly cards: readonly Pick<Card, "id" | "position">[];
}

/** The lane's key: its option id, `none` for the lane without a value. */
export function kanbanLaneKey(optionId: string | null): string {
  return optionId ?? "none";
}

/** Droppable id of a lane (`optionId: null` is the lane without a value). */
export function kanbanLaneDropId(optionId: string | null): string {
  return `kanban-lane:${kanbanLaneKey(optionId)}`;
}

/** Card ids per lane key, in the order the lanes show them while dragging. */
export type KanbanArrangement = ReadonlyMap<string, readonly string[]>;

/** The lanes' cards as they are. */
export function arrangeKanbanLanes(
  lanes: readonly KanbanStatusLane[],
): KanbanArrangement {
  return new Map(
    lanes.map((lane) => [
      kanbanLaneKey(lane.optionId),
      lane.cards.map((card) => card.id),
    ]),
  );
}

/** The lane key whose list holds the card. */
export function laneKeyOfCard(
  arrangement: KanbanArrangement,
  cardId: string,
): string | undefined {
  for (const [key, ids] of arrangement) {
    if (ids.includes(cardId)) return key;
  }
  return undefined;
}

/**
 * The arrangement with the card taken out of its lane and put into `laneKey`
 * at `index` (clamped). The same arrangement comes back when nothing changes,
 * so a drag over the card's own slot does not re-render the board.
 */
export function moveCardInArrangement(
  arrangement: KanbanArrangement,
  cardId: string,
  laneKey: string,
  index: number,
): KanbanArrangement {
  const from = laneKeyOfCard(arrangement, cardId);
  const target = arrangement.get(laneKey);
  if (from === undefined || target === undefined) return arrangement;
  const without = target.filter((id) => id !== cardId);
  const at = Math.max(0, Math.min(index, without.length));
  if (from === laneKey && target.indexOf(cardId) === at) return arrangement;
  const next = new Map(arrangement);
  if (from !== laneKey) {
    next.set(
      from,
      (arrangement.get(from) ?? []).filter((id) => id !== cardId),
    );
  }
  next.set(laneKey, [...without.slice(0, at), cardId, ...without.slice(at)]);
  return next;
}

function sameIds(a: readonly string[], b: readonly string[]): boolean {
  return a.length === b.length && a.every((id, index) => id === b[index]);
}

/**
 * Reads a finished drag. `arrangement` is how the lanes looked when the card
 * was let go: the card sits in the lane and at the place it was dropped at.
 * A drop in the lane without a value never clears the value, a drop that
 * leaves the card's order in its group as it was changes nothing, and the new
 * place is only ever named among cards of the card's own group (the server
 * orders a group, not a lane).
 */
export function getKanbanDrop(input: {
  readonly lanes: readonly KanbanStatusLane[];
  readonly arrangement: KanbanArrangement;
  readonly cardId: string;
  readonly groups?: readonly KanbanGroupOrder[];
}): KanbanLaneDrop | null {
  const { lanes, arrangement, cardId, groups } = input;
  const cards = new Map<string, Card>();
  for (const lane of lanes) {
    for (const card of lane.cards) cards.set(card.id, card);
  }
  const active = cards.get(cardId);
  const source = lanes.find((lane) =>
    lane.cards.some((card) => card.id === cardId),
  );
  const targetKey = laneKeyOfCard(arrangement, cardId);
  const target = lanes.find(
    (lane) => kanbanLaneKey(lane.optionId) === targetKey,
  );
  if (!active || !source || !target) return null;
  const crossLane = target.optionId !== source.optionId;
  if (crossLane && target.optionId === null) return null;

  const ids = arrangement.get(targetKey!) ?? [];
  const index = ids.indexOf(cardId);
  const sameGroup = (id: string) => {
    const other = cards.get(id);
    return (
      other !== undefined && other.listId === active.listId && id !== cardId
    );
  };
  const previousInLane = ids.slice(0, index).reverse().find(sameGroup);
  const nextInLane = ids.slice(index + 1).find(sameGroup);

  if (!crossLane) {
    // The card's order among its group's cards (the card included) decides.
    const inGroup = (id: string) => cards.get(id)?.listId === active.listId;
    const before = source.cards.map((card) => card.id).filter(inGroup);
    const after = ids.filter(inGroup);
    if (sameIds(before, after)) return null;
  }
  if (previousInLane === undefined && nextInLane === undefined) {
    return crossLane
      ? {
          kind: "status",
          cardId,
          optionId: target.optionId!,
          fromOptionId: source.optionId,
        }
      : null;
  }

  // The group's whole order without the card, as far as it is known.
  const groupOrder = (
    groups?.find((group) => group.id === active.listId)?.cards ??
    [...cards.values()].filter((card) => card.listId === active.listId)
  )
    .filter((card) => card.id !== cardId)
    .slice()
    .sort((a, b) => a.position - b.position);
  const indexInGroup = (id: string) =>
    groupOrder.findIndex((card) => card.id === id);
  let previous: (typeof groupOrder)[number] | undefined;
  let next: (typeof groupOrder)[number] | undefined;
  if (previousInLane !== undefined) {
    previous = groupOrder[indexInGroup(previousInLane)];
    next = groupOrder[indexInGroup(previousInLane) + 1];
  } else {
    next = groupOrder[indexInGroup(nextInLane!)];
    previous = groupOrder[indexInGroup(nextInLane!) - 1];
  }
  const position = generatePosition(previous?.position, next?.position);
  const neighbours = {
    previousSiblingId: previous?.id ?? null,
    nextSiblingId: next?.id ?? null,
  };

  if (!crossLane) {
    return {
      kind: "reorder",
      cardId,
      listId: active.listId,
      position,
      ...neighbours,
    };
  }
  // Dropped where the lane would put the card anyway: only the value changes.
  const inPlace =
    (previous === undefined || previous.position < active.position) &&
    (next === undefined || active.position < next.position);
  if (inPlace) {
    return {
      kind: "status",
      cardId,
      optionId: target.optionId!,
      fromOptionId: source.optionId,
    };
  }
  return {
    kind: "place",
    cardId,
    optionId: target.optionId!,
    fromOptionId: source.optionId,
    listId: active.listId,
    position,
    ...neighbours,
  };
}
