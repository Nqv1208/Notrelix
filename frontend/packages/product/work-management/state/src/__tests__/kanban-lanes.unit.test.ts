import { describe, expect, it } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import {
  groupCardsByStatus,
  type Card,
  type FieldDefinition,
  type FullBoardResponse,
} from "@notrelix/work-management-core";
import { createCardApi } from "../api/item.api";
import {
  groupCardsBySelectField,
  kanbanGroupByFields,
} from "../hooks/use-board-kanban";
import { applyItemStatus } from "../cache/item-status";
import {
  dropUnknownFilterIds,
  filterKanbanLanes,
  kanbanFilterChoices,
  type KanbanLaneFilter,
} from "../hooks/use-kanban-filters";

const STATUS_FIELD = "field-status-x";
const OPTION = {
  todo: "opt-todo",
  doing: "opt-doing",
  review: "opt-review",
  done: "opt-done",
} as const;

const statusField: FieldDefinition = {
  id: STATUS_FIELD,
  boardId: "board-1",
  name: "Trạng thái",
  fieldType: "select",
  semanticRole: "status",
  position: 1,
  isHidden: false,
  isSystemField: true,
  options: [
    {
      id: OPTION.todo,
      label: "Cần làm",
      color: "",
      position: "a",
      category: "todo",
    },
    {
      id: OPTION.doing,
      label: "Đang làm",
      color: "",
      position: "b",
      category: "in_progress",
    },
    {
      id: OPTION.review,
      label: "Chờ duyệt",
      color: "",
      position: "c",
      category: "review",
    },
    {
      id: OPTION.done,
      label: "Hoàn thành",
      color: "",
      position: "d",
      category: "done",
    },
  ],
};

function card(
  key: string,
  option: keyof typeof OPTION,
  extra: Partial<Card> = {},
): Card {
  return {
    id: `item-${key}`,
    listId: "group-1",
    boardId: "board-1",
    workspaceId: "ws-1",
    title: `Việc ${key}`,
    position: Number(key),
    status: OPTION[option],
    statusOptionId: OPTION[option],
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: { [STATUS_FIELD]: OPTION[option] },
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: "2026-10-01T00:00:00.000Z",
    itemKey: `ERP-${key}`,
    ...extra,
  };
}

const person = (userId: string, name: string) => ({
  id: `cm-${userId}`,
  userId,
  name,
  initials: name.slice(0, 2),
  color: "",
});

function board(): FullBoardResponse {
  const cards = [
    card("1", "todo", { priority: "high", title: "Cấu hình quyền truy cập" }),
    card("2", "todo", {
      priority: "low",
      members: [person("u-linh", "Đỗ Khánh Linh")],
    }),
    card("3", "doing", {
      priority: "urgent",
      title: "Chuyển dữ liệu khách hàng",
      labels: [{ id: "label-data", name: "Dữ liệu", color: "#1E6FD0" }],
    }),
    card("4", "doing", { priority: "high" }),
    card("5", "review", { priority: "high" }),
    card("6", "done", { priority: "medium" }),
  ];
  return {
    board: {
      id: "board-1",
      workspaceId: "ws-1",
      title: "Board",
      background: { type: "color", value: "#fff" },
      visibility: "workspace",
      isArchived: false,
      fieldDefinitions: [statusField],
      members: [],
      createdAt: "2026-10-01T00:00:00.000Z",
    },
    groups: [
      {
        id: "group-1",
        title: "Kế hoạch",
        position: 1,
        isCollapsed: false,
        cards,
      },
    ],
    fieldDefinitions: [statusField],
  };
}

function lanes() {
  const full = board();
  return groupCardsByStatus({
    fieldDefinitions: full.board.fieldDefinitions,
    groups: full.groups,
  });
}

const NO_FILTER: KanbanLaneFilter = {
  statusOptionIds: [],
  priorities: [],
  assigneeIds: [],
  labelIds: [],
  overdue: false,
};

const never = () => false;

function keys(result: ReturnType<typeof filterKanbanLanes>): string[][] {
  return result.lanes.map((lane) =>
    lane.cards.map((item) => item.itemKey ?? item.id),
  );
}

describe("filterKanbanLanes", () => {
  it("keeps every card and lane total without a filter or search", () => {
    const result = filterKanbanLanes(lanes(), NO_FILTER, {
      query: "",
      isOverdue: never,
    });
    expect(result.lanes.map((lane) => lane.cards.length)).toEqual([2, 2, 1, 1]);
    expect(result.lanes.map((lane) => lane.total)).toEqual([2, 2, 1, 1]);
    expect(result).toMatchObject({
      visibleCount: 6,
      totalCount: 6,
      active: false,
    });
  });

  it("filters by priority and keeps the unfiltered lane totals", () => {
    const result = filterKanbanLanes(
      lanes(),
      { ...NO_FILTER, priorities: ["urgent"] },
      { query: "", isOverdue: never },
    );
    expect(keys(result)).toEqual([[], ["ERP-3"], [], []]);
    expect(result.lanes.map((lane) => lane.total)).toEqual([2, 2, 1, 1]);
    expect(result).toMatchObject({
      visibleCount: 1,
      totalCount: 6,
      active: true,
    });
  });

  it("combines facets with AND and values within a facet with OR", () => {
    const result = filterKanbanLanes(
      lanes(),
      {
        ...NO_FILTER,
        statusOptionIds: [OPTION.doing, OPTION.review],
        priorities: ["urgent", "high"],
      },
      { query: "", isOverdue: never },
    );
    expect(keys(result).flat()).toEqual(["ERP-3", "ERP-4", "ERP-5"]);
  });

  it("filters by assignee and label ids", () => {
    expect(
      keys(
        filterKanbanLanes(
          lanes(),
          { ...NO_FILTER, assigneeIds: ["u-linh"] },
          { query: "", isOverdue: never },
        ),
      ).flat(),
    ).toEqual(["ERP-2"]);
    expect(
      keys(
        filterKanbanLanes(
          lanes(),
          { ...NO_FILTER, labelIds: ["label-data"] },
          { query: "", isOverdue: never },
        ),
      ).flat(),
    ).toEqual(["ERP-3"]);
  });

  it("applies the overdue facet through the injected predicate", () => {
    const result = filterKanbanLanes(
      lanes(),
      { ...NO_FILTER, overdue: true },
      { query: "", isOverdue: (item) => item.itemKey === "ERP-3" },
    );
    expect(keys(result).flat()).toEqual(["ERP-3"]);
  });

  it("reports no visible card when the facets exclude each other", () => {
    const result = filterKanbanLanes(
      lanes(),
      { ...NO_FILTER, overdue: true, priorities: ["low"] },
      { query: "", isOverdue: (item) => item.itemKey === "ERP-3" },
    );
    expect(result).toMatchObject({ visibleCount: 0, totalCount: 6 });
  });

  it("searches titles, keys, people and labels without diacritics or case", () => {
    const search = (query: string) =>
      keys(
        filterKanbanLanes(lanes(), NO_FILTER, { query, isOverdue: never }),
      ).flat();
    expect(search("du lieu khach hang")).toEqual(["ERP-3"]);
    expect(search("erp-5")).toEqual(["ERP-5"]);
    expect(search("khanh linh")).toEqual(["ERP-2"]);
    expect(search("  ")).toHaveLength(6);
    expect(
      filterKanbanLanes(lanes(), NO_FILTER, { query: "x", isOverdue: never })
        .active,
    ).toBe(true);
  });
});

describe("applyItemStatus", () => {
  it("moves the card to the lane of the new status option", () => {
    const next = applyItemStatus(board(), "item-2", OPTION.doing);
    const nextLanes = groupCardsByStatus({
      fieldDefinitions: next!.board.fieldDefinitions,
      groups: next!.groups,
    });
    expect(nextLanes.map((lane) => lane.total)).toEqual([1, 3, 1, 1]);
    const moved = nextLanes[1]!.cards.find((item) => item.id === "item-2")!;
    expect(moved).toMatchObject({
      statusOptionId: OPTION.doing,
      status: OPTION.doing,
      statusCategory: "in_progress",
    });
    expect(moved.fieldValues[STATUS_FIELD]).toBe(OPTION.doing);
  });

  it("leaves the board unchanged for an unknown card or option", () => {
    const full = board();
    expect(applyItemStatus(full, "missing", OPTION.doing)).toBe(full);
    expect(applyItemStatus(full, "item-2", "nope")).toBe(full);
    expect(applyItemStatus(undefined, "x", "y")).toBeUndefined();
  });
});

function recordingClient(response: unknown = undefined) {
  const calls: Array<{
    method: string;
    url: string;
    body: unknown;
    options: unknown;
  }> = [];
  const record =
    (method: string) =>
    async (url: string, body?: unknown, options?: unknown) => {
      calls.push({ method, url, body, options });
      return response;
    };
  const client = {
    api: {
      get: record("GET"),
      post: record("POST"),
      patch: record("PATCH"),
      put: record("PUT"),
      delete: record("DELETE"),
    },
  } as unknown as NotrelixClient;
  return { client, calls };
}

describe("item status write", () => {
  it("sends PATCH /board-items/{id}/status with the option id and the command keys", async () => {
    const { client, calls } = recordingClient();
    await createCardApi(client).updateStatus("item-1", "option-doing", 4, {
      correlationId: "corr-1",
      idempotencyKey: "idem-key-1",
    });
    expect(calls).toEqual([
      {
        method: "PATCH",
        url: "/board-items/item-1/status",
        body: { status: "option-doing", expectedVersion: 4 },
        options: { correlationId: "corr-1", idempotencyKey: "idem-key-1" },
      },
    ]);
  });

  it("returns the created item id so a lane can set its status next", async () => {
    const { client } = recordingClient({ id: "item-new", groupId: "g-1" });
    const id = await createCardApi(client).createCard("board-1", {
      listId: "g-1",
      title: "Việc mới",
    });
    expect(id).toBe("item-new");
  });
});

const priorityField: FieldDefinition = {
  id: "field-priority-x",
  boardId: "board-1",
  name: "Ưu tiên",
  fieldType: "select",
  semanticRole: "priority",
  position: 2,
  isHidden: false,
  isSystemField: false,
  options: [
    { id: "p-high", label: "Cao", color: "" },
    { id: "p-low", label: "Thấp", color: "" },
  ],
};

describe("grouping by another Select field", () => {
  it("lists the Status field first, then Select fields with options", () => {
    const text: FieldDefinition = {
      ...priorityField,
      id: "field-text",
      fieldType: "text",
      options: [],
    };
    expect(kanbanGroupByFields([text, priorityField, statusField])).toEqual([
      { id: STATUS_FIELD, name: "Trạng thái", kind: "status" },
      { id: "field-priority-x", name: "Ưu tiên", kind: "select" },
    ]);
  });

  it("makes one lane per option and a last lane for cards without a value", () => {
    const full = board();
    const groups = full.groups.map((group) => ({
      ...group,
      cards: group.cards.map((item, index) => ({
        ...item,
        fieldValues: {
          ...item.fieldValues,
          ...(index < 2 ? { [priorityField.id]: "p-low" } : {}),
          ...(index === 2 ? { [priorityField.id]: ["p-high"] } : {}),
        },
      })),
    }));
    const result = groupCardsBySelectField(priorityField, groups);
    expect(
      result.map((lane) => [lane.optionId, lane.name, lane.total]),
    ).toEqual([
      ["p-high", "Cao", 1],
      ["p-low", "Thấp", 2],
      [null, "", 3],
    ]);
    expect(result.every((lane) => lane.category === null)).toBe(true);
  });
});

describe("filter choices and stale ids", () => {
  it("offers the status options, the people and the labels of the board", () => {
    const full = board();
    full.board.members = [
      {
        id: "m-1",
        userId: "u-ha",
        name: "Nguyễn Thu Hà",
        initials: "HA",
        role: "owner",
        color: "",
      },
    ];
    const choices = kanbanFilterChoices(full.board, full.groups);
    expect(choices.statuses.map((option) => option.name)).toEqual([
      "Cần làm",
      "Đang làm",
      "Chờ duyệt",
      "Hoàn thành",
    ]);
    expect(choices.assignees).toEqual([
      { id: "u-ha", name: "Nguyễn Thu Hà" },
      { id: "u-linh", name: "Đỗ Khánh Linh" },
    ]);
    expect(choices.labels).toEqual([{ id: "label-data", name: "Dữ liệu" }]);
  });

  it("drops ids the board does not have and keeps the rest", () => {
    const full = board();
    const choices = kanbanFilterChoices(full.board, full.groups);
    const filter = {
      ...NO_FILTER,
      statusOptionIds: [OPTION.doing, "gone-status"],
      assigneeIds: ["gone-user"],
      labelIds: ["label-data"],
      priorities: ["low"],
      overdue: true,
    };
    expect(dropUnknownFilterIds(filter, choices)).toEqual({
      ...filter,
      statusOptionIds: [OPTION.doing],
      assigneeIds: [],
    });
    const clean = { ...NO_FILTER, labelIds: ["label-data"] };
    expect(dropUnknownFilterIds(clean, choices)).toBe(clean);
  });
});
