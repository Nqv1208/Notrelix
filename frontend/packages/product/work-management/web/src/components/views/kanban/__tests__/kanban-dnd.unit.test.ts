import { describe, expect, it } from "vitest";
import type { Card, KanbanStatusLane } from "@notrelix/work-management-core";
import { getKanbanLaneDrop, kanbanLaneDropId } from "../kanban-dnd";

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
const b1 = card("b1", "g1", 3);
const lanes = [
  lane("todo", [a1, a2, a3]),
  lane("doing", [b1]),
  lane(null, [card("n1", "g1", 9)]),
];

type DragEnd = Parameters<typeof getKanbanLaneDrop>[0];

function overCard(active: Card, over: Card): DragEnd {
  return {
    active: {
      id: active.id,
      data: { current: { type: "kanban-card", card: active } },
    },
    over: {
      id: over.id,
      data: { current: { type: "kanban-card", card: over } },
    },
  } as unknown as DragEnd;
}

function overLane(active: Card, optionId: string | null): DragEnd {
  const target = lanes.find((candidate) => candidate.optionId === optionId)!;
  return {
    active: {
      id: active.id,
      data: { current: { type: "kanban-card", card: active } },
    },
    over: {
      id: kanbanLaneDropId(optionId),
      data: { current: { type: "kanban-lane", lane: target } },
    },
  } as unknown as DragEnd;
}

describe("getKanbanLaneDrop", () => {
  it("a drop on another lane writes that lane's option", () => {
    expect(getKanbanLaneDrop(overLane(a1, "doing"), lanes)).toEqual({
      kind: "status",
      cardId: "a1",
      optionId: "doing",
      fromOptionId: "todo",
    });
    expect(getKanbanLaneDrop(overCard(a2, b1), lanes)).toEqual({
      kind: "status",
      cardId: "a2",
      optionId: "doing",
      fromOptionId: "todo",
    });
  });

  it("a drop in the same lane only reorders within the card's group", () => {
    const drop = getKanbanLaneDrop(overCard(a2, a1), lanes);
    expect(drop).toMatchObject({ kind: "reorder", cardId: "a2", listId: "g1" });
    expect(drop?.kind === "reorder" && drop.position < a1.position).toBe(true);
  });

  it("does nothing for a same-lane drop on a card of another group, on itself, or on the lane", () => {
    expect(getKanbanLaneDrop(overCard(a1, a3), lanes)).toBeNull();
    expect(getKanbanLaneDrop(overCard(a1, a1), lanes)).toBeNull();
    expect(getKanbanLaneDrop(overLane(a1, "todo"), lanes)).toBeNull();
  });

  it("never clears a status by a drop on the lane without status", () => {
    expect(getKanbanLaneDrop(overLane(a1, null), lanes)).toBeNull();
  });

  it("ignores drops outside any lane and non-card drags", () => {
    expect(
      getKanbanLaneDrop(
        {
          active: {
            id: "a1",
            data: { current: { type: "kanban-card", card: a1 } },
          },
          over: null,
        } as unknown as DragEnd,
        lanes,
      ),
    ).toBeNull();
    expect(
      getKanbanLaneDrop(
        {
          active: { id: "x", data: { current: { type: "other" } } },
          over: {
            id: "b1",
            data: { current: { type: "kanban-card", card: b1 } },
          },
        } as unknown as DragEnd,
        lanes,
      ),
    ).toBeNull();
  });
});
