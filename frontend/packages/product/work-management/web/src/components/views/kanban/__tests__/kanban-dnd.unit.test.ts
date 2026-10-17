import { describe, expect, it } from "vitest";
import type { Card, KanbanStatusLane } from "@notrelix/work-management-core";
import {
  arrangeKanbanLanes,
  getKanbanDrop,
  kanbanLaneDropId,
  kanbanLaneKey,
  laneKeyOfCard,
  moveCardInArrangement,
} from "../kanban-dnd";

function card(id: string, listId: string, position: number): Card {
  return {
    id,
    listId,
    boardId: "b",
    workspaceId: "w",
    title: id,
    position,
    status: "",
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: {},
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: "2026-10-01T00:00:00.000Z",
  };
}

function lane(optionId: string | null, cards: Card[]): KanbanStatusLane {
  return {
    optionId,
    name: optionId ?? "",
    category: null,
    color: null,
    cards,
    total: cards.length,
  };
}

const a1 = card("a1", "g1", 1);
const a2 = card("a2", "g1", 2);
const a3 = card("a3", "g2", 1);
const b1 = card("b1", "g1", 5);
const b2 = card("b2", "g1", 7);
const n1 = card("n1", "g1", 9);
const lanes = [
  lane("todo", [a1, a2, a3]),
  lane("doing", [b1, b2]),
  lane("empty", []),
  lane(null, [n1]),
];
// Group g1 in board order: a1 a2 b1 b2 n1; g2: a3.
const groups = [
  { id: "g1", cards: [a1, a2, b1, b2, n1] },
  { id: "g2", cards: [a3] },
];

function dropOf(cardId: string, moves: [string, number][] = []) {
  let arrangement = arrangeKanbanLanes(lanes);
  for (const [key, index] of moves) {
    arrangement = moveCardInArrangement(arrangement, cardId, key, index);
  }
  return getKanbanDrop({ lanes, arrangement, cardId, groups });
}

describe("lane keys and the drag arrangement", () => {
  it("names a lane by its option, and the lane without a value `none`", () => {
    expect(kanbanLaneKey("doing")).toBe("doing");
    expect(kanbanLaneKey(null)).toBe("none");
    expect(kanbanLaneDropId(null)).toBe("kanban-lane:none");
  });

  it("starts as the lanes are and finds the lane of a card", () => {
    const arrangement = arrangeKanbanLanes(lanes);
    expect(arrangement.get("todo")).toEqual(["a1", "a2", "a3"]);
    expect(arrangement.get("empty")).toEqual([]);
    expect(laneKeyOfCard(arrangement, "b2")).toBe("doing");
    expect(laneKeyOfCard(arrangement, "zz")).toBeUndefined();
  });

  it("moves a card to another lane at an index and clamps the index", () => {
    const start = arrangeKanbanLanes(lanes);
    const moved = moveCardInArrangement(start, "a1", "doing", 1);
    expect(moved.get("todo")).toEqual(["a2", "a3"]);
    expect(moved.get("doing")).toEqual(["b1", "a1", "b2"]);
    expect(
      moveCardInArrangement(start, "a1", "doing", 99).get("doing"),
    ).toEqual(["b1", "b2", "a1"]);
    expect(moveCardInArrangement(start, "a1", "empty", 0).get("empty")).toEqual(
      ["a1"],
    );
    // The original is untouched.
    expect(start.get("todo")).toEqual(["a1", "a2", "a3"]);
  });

  it("reorders inside a lane, and returns the same arrangement when nothing changes", () => {
    const start = arrangeKanbanLanes(lanes);
    expect(moveCardInArrangement(start, "a1", "todo", 1).get("todo")).toEqual([
      "a2",
      "a1",
      "a3",
    ]);
    expect(moveCardInArrangement(start, "a1", "todo", 0)).toBe(start);
    expect(moveCardInArrangement(start, "a1", "nope", 0)).toBe(start);
    expect(moveCardInArrangement(start, "zz", "todo", 0)).toBe(start);
  });
});

describe("getKanbanDrop", () => {
  it("a card let go where it was changes nothing", () => {
    expect(dropOf("a1")).toBeNull();
    expect(dropOf("a2", [["todo", 1]])).toBeNull();
  });

  it("a drop in an empty lane writes only that lane's option", () => {
    expect(dropOf("a1", [["empty", 0]])).toEqual({
      kind: "status",
      cardId: "a1",
      optionId: "empty",
      fromOptionId: "todo",
    });
  });

  it("a drop on another lane where the card would sit anyway writes only the option", () => {
    // a2 (position 2) between nothing and b1 (position 5): its natural place.
    expect(dropOf("a2", [["doing", 0]])).toEqual({
      kind: "status",
      cardId: "a2",
      optionId: "doing",
      fromOptionId: "todo",
    });
  });

  it("a drop between two cards of another lane writes the option and the place, naming the neighbours in the group's whole order", () => {
    // b1 b2 -> b1 a1 b2: after b1, before b2 (a1 is position 1: not its natural place).
    expect(dropOf("a1", [["doing", 1]])).toMatchObject({
      kind: "place",
      cardId: "a1",
      optionId: "doing",
      fromOptionId: "todo",
      listId: "g1",
      previousSiblingId: "b1",
      nextSiblingId: "b2",
    });
    const drop = dropOf("a1", [["doing", 1]]);
    expect(drop?.kind === "place" && drop.position).toBe(6);
    // After the last card of the lane: before the next card of the group in
    // another lane (n1).
    expect(dropOf("a1", [["doing", 2]])).toMatchObject({
      kind: "place",
      previousSiblingId: "b2",
      nextSiblingId: "n1",
    });
  });

  it("a reorder inside the lane names the new neighbours in the group's whole order", () => {
    // a1 dropped after a2: after a2, before b1 (b1 sits in another lane).
    expect(dropOf("a1", [["todo", 1]])).toMatchObject({
      kind: "reorder",
      cardId: "a1",
      listId: "g1",
      previousSiblingId: "a2",
      nextSiblingId: "b1",
    });
    // a2 dropped before a1: nothing before it, a1 after it.
    expect(dropOf("a2", [["todo", 0]])).toMatchObject({
      kind: "reorder",
      cardId: "a2",
      previousSiblingId: null,
      nextSiblingId: "a1",
    });
    const drop = dropOf("a2", [["todo", 0]]);
    expect(drop?.kind === "reorder" && drop.position).toBeLessThan(a1.position);
  });

  it("only places a card among cards of its own group", () => {
    // a3 (group g2) moved to the front of its lane: no other g2 card, so no change.
    expect(dropOf("a3", [["todo", 0]])).toBeNull();
    // a2 dropped after a3 (another group): its order among g1 cards is unchanged.
    expect(dropOf("a2", [["todo", 2]])).toBeNull();
  });

  it("never clears a value by a drop on the lane without one", () => {
    expect(dropOf("a1", [["none", 1]])).toBeNull();
  });

  it("returns null for a card that is not on the board", () => {
    expect(
      getKanbanDrop({
        lanes,
        arrangement: arrangeKanbanLanes(lanes),
        cardId: "zz",
        groups,
      }),
    ).toBeNull();
  });

  it("works from the lanes alone when the group order is not known", () => {
    let arrangement = arrangeKanbanLanes(lanes);
    arrangement = moveCardInArrangement(arrangement, "a1", "doing", 1);
    expect(getKanbanDrop({ lanes, arrangement, cardId: "a1" })).toMatchObject({
      kind: "place",
      previousSiblingId: "b1",
      nextSiblingId: "b2",
    });
  });
});
