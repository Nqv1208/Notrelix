import { describe, expect, it } from "vitest";
import type { Card } from "../types";
import type { KanbanStatusLane } from "../types/kanban-types";
import {
  DEFAULT_TABLE_SORT,
  projectTableLanes,
  sortTableCards,
  tableSortFromConfig,
} from "./table-projection";

function card(id: string, patch: Partial<Card> = {}): Card {
  return {
    id,
    listId: "group-1",
    boardId: "board-1",
    workspaceId: "workspace-1",
    title: id,
    position: 0,
    status: "",
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: {},
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: "2026-10-01T00:00:00.000Z",
    ...patch,
  };
}

const rankNone = () => 0;

function lane(
  optionId: string | null,
  name: string,
  cards: Card[],
  total = cards.length,
): KanbanStatusLane {
  return { optionId, name, category: null, color: null, cards, total };
}

describe("tableSortFromConfig", () => {
  it("uses the first saved entry that names a table column", () => {
    expect(
      tableSortFromConfig([
        { fieldId: "field-from-an-older-table", direction: "asc" },
        { fieldId: "priority", direction: "desc" },
      ]),
    ).toEqual({ key: "priority", direction: "desc" });
  });

  it("sorts a view without a usable entry by due date, soonest first", () => {
    expect(tableSortFromConfig([])).toEqual(DEFAULT_TABLE_SORT);
    expect(
      tableSortFromConfig([{ fieldId: "unknown", direction: "desc" }]),
    ).toEqual(DEFAULT_TABLE_SORT);
  });
});

describe("sortTableCards", () => {
  const cards = [
    card("late", { dueAt: "2026-10-30T10:00:00.000Z" }),
    card("none"),
    card("soon", { dueAt: "2026-10-18T10:00:00.000Z" }),
  ];

  it("puts items without a date last in both directions", () => {
    expect(
      sortTableCards(cards, { key: "due", direction: "asc" }, rankNone).map(
        (c) => c.id,
      ),
    ).toEqual(["soon", "late", "none"]);
    expect(
      sortTableCards(cards, { key: "due", direction: "desc" }, rankNone).map(
        (c) => c.id,
      ),
    ).toEqual(["late", "soon", "none"]);
  });

  it("keeps board order for equal values", () => {
    const equal = [card("a"), card("b"), card("c")];
    expect(
      sortTableCards(
        equal,
        { key: "priority", direction: "asc" },
        rankNone,
      ).map((c) => c.id),
    ).toEqual(["a", "b", "c"]);
  });

  it("ranks priority urgent first", () => {
    const byPriority = [
      card("low", { priority: "low" }),
      card("urgent", { priority: "urgent" }),
      card("high", { priority: "high" }),
    ];
    expect(
      sortTableCards(
        byPriority,
        { key: "priority", direction: "asc" },
        rankNone,
      ).map((c) => c.id),
    ).toEqual(["urgent", "high", "low"]);
  });
});

describe("projectTableLanes", () => {
  const base = {
    sort: DEFAULT_TABLE_SORT,
    statusRank: rankNone,
    isOverdue: (c: Card) => c.id === "overdue",
  } as const;

  const lanes = [
    lane("todo", "Cần làm", [card("a")]),
    lane("doing", "Đang làm", [card("overdue"), card("b")]),
    lane("done", "Hoàn thành", [
      card("done-1", { statusCategory: "done" }),
      card("done-2", { completedAt: "2026-10-05T00:00:00.000Z" }),
    ]),
  ];

  it("leaves out finished items and the lane they empty, and counts them", () => {
    const result = projectTableLanes({
      ...base,
      lanes,
      hideDone: true,
      narrowed: false,
    });
    expect(result.groups.map((g) => g.name)).toEqual(["Cần làm", "Đang làm"]);
    expect(result.visibleCount).toBe(3);
    expect(result.totalCount).toBe(5);
    expect(result.hiddenCount).toBe(2);
    expect(result.doneHiddenCount).toBe(2);
  });

  it("reports the overdue items of a group", () => {
    const result = projectTableLanes({
      ...base,
      lanes,
      hideDone: false,
      narrowed: false,
    });
    expect(result.groups.map((g) => g.overdueCount)).toEqual([0, 1, 0]);
    expect(result.hiddenCount).toBe(0);
  });

  it("keeps a lane that is empty on the board, unless a filter narrows it", () => {
    const withEmpty = [...lanes, lane("review", "Chờ duyệt", [])];
    expect(
      projectTableLanes({
        ...base,
        lanes: withEmpty,
        hideDone: false,
        narrowed: false,
      }).groups.map((g) => g.name),
    ).toContain("Chờ duyệt");
    expect(
      projectTableLanes({
        ...base,
        lanes: withEmpty,
        hideDone: false,
        narrowed: true,
      }).groups.map((g) => g.name),
    ).not.toContain("Chờ duyệt");
  });

  it("drops a lane that a filter emptied but the board still holds", () => {
    const filtered = [lane("todo", "Cần làm", [], 4)];
    const result = projectTableLanes({
      ...base,
      lanes: filtered,
      hideDone: false,
      narrowed: true,
    });
    expect(result.groups).toEqual([]);
    expect(result.hiddenCount).toBe(4);
  });
});
