import { describe, expect, it } from "vitest";
import type { FullBoardDtoApi } from "../types";
import {
  findPriorityField,
  findStatusField,
  groupCardsByStatus,
  mapBoardAccessDto,
  mapCardDto,
  mapFullBoardDto,
  priorityFromOptionName,
  type GeneratedBoardAccessDto,
  type GeneratedBoardItemDto,
  type GeneratedFullBoardDto,
} from "./board-api-mappers";

const STATUS_FIELD = "f-status";
const PRIORITY_FIELD = "f-priority";
const TODO = "opt-todo";
const DOING = "opt-doing";
const REVIEW = "opt-review";
const DONE = "opt-done";

type GeneratedItem = NonNullable<
  NonNullable<GeneratedFullBoardDto["lists"]>[number]["boardItems"]
>[number];

function item(
  id: string,
  position: string,
  extra: Partial<GeneratedItem> = {},
): GeneratedItem {
  return {
    id,
    name: `Item ${id}`,
    memberCount: 0,
    members: [],
    labels: [],
    checklistProgress: 0,
    checklistTotal: 0,
    commentCount: 0,
    attachmentCount: 0,
    position,
    createdAt: "2026-10-01T02:00:00Z",
    updatedAt: null,
    version: 1,
    itemKey: null,
    parentItemId: null,
    statusOptionId: null,
    priorityOptionId: null,
    dueAt: null,
    startedAt: null,
    completedAt: null,
    childProgress: { done: 0, total: 0 },
    ...extra,
  };
}

/** The generated `/full` shape: options carry position and category. */
function generatedBoard(
  items: GeneratedItem[],
  secondGroupItems: GeneratedItem[] = [],
): GeneratedFullBoardDto {
  return {
    id: "board-1",
    workspaceId: "workspace-from-dto",
    title: "Triển khai ERP Q4",
    description: null,
    background: null,
    visibility: "Workspace",
    version: 7,
    spaceId: "space-1",
    itemKeyPrefix: "ERP",
    members: [
      {
        userId: "user-quan",
        name: "Trần Minh Quân",
        avatar: null,
        role: "Member",
        joinedAt: "2026-09-01T00:00:00Z",
      },
    ],
    columns: [
      {
        id: "board-1",
        boardId: "board-1",
        name: "Task",
        fieldType: "text",
        settings: '{"system":"title"}',
        defaultValue: null,
        position: "a0",
        isSystem: true,
        isDeleted: false,
        version: 0,
      },
      {
        id: STATUS_FIELD,
        boardId: "board-1",
        name: "Trạng thái",
        fieldType: "Status",
        settings: "{}",
        defaultValue: null,
        position: "a1",
        isSystem: true,
        isDeleted: false,
        version: 3,
        // Deliberately out of order: lanes follow `position`, not the array.
        options: [
          {
            id: DONE,
            name: "Hoàn thành",
            color: "#2E8B57",
            position: "a3",
            category: "done",
          },
          {
            id: TODO,
            name: "Cần làm",
            color: "#7A8391",
            position: "a0",
            category: "todo",
          },
          {
            id: REVIEW,
            name: "Chờ duyệt",
            color: "#C18A12",
            position: "a2",
            category: "review",
          },
          {
            id: DOING,
            name: "Đang làm",
            color: "#1E7BE0",
            position: "a1",
            category: "in_progress",
          },
        ],
      },
      {
        id: "f-not-priority",
        boardId: "board-1",
        name: "Priority",
        fieldType: "Text",
        settings: "{}",
        defaultValue: null,
        position: "a2",
        isSystem: false,
        isDeleted: false,
        version: 1,
      },
      {
        id: PRIORITY_FIELD,
        boardId: "board-1",
        name: "Mức ưu tiên",
        fieldType: "Select",
        settings: "{}",
        defaultValue: null,
        position: "a3",
        isSystem: false,
        isDeleted: false,
        version: 1,
        options: [
          { id: "p-urgent", name: "Khẩn", color: "#C8143F", position: "a0" },
          { id: "p-high", name: "Cao", color: "#B4500B", position: "a1" },
          {
            id: "p-medium",
            name: "Trung bình",
            color: "#7A8391",
            position: "a2",
          },
          { id: "p-low", name: "Thấp", color: "#7A8391", position: "a3" },
        ],
      },
      {
        id: "f-deleted",
        boardId: "board-1",
        name: "Old",
        fieldType: "Text",
        settings: null,
        defaultValue: null,
        position: "a4",
        isSystem: false,
        isDeleted: true,
        version: 1,
      },
    ],
    lists: [
      {
        id: "group-2",
        title: "Second",
        color: null,
        position: "a1",
        isArchived: false,
        boardItems: secondGroupItems,
        version: 1,
      },
      {
        id: "group-1",
        title: "First",
        color: "#123456",
        position: "a0",
        isArchived: false,
        boardItems: items,
        version: 1,
      },
      {
        id: "group-archived",
        title: "Archived",
        color: null,
        position: "Z0",
        isArchived: true,
        boardItems: [item("in-archived-group", "a0", { statusOptionId: TODO })],
        version: 1,
      },
    ],
  };
}

describe("mapFullBoardDto over the generated board read", () => {
  const dto = generatedBoard([
    item("i-2", "a1", {
      itemKey: "ERP-142",
      statusOptionId: DOING,
      priorityOptionId: "p-urgent",
      dueAt: "2026-10-10T10:00:00Z",
      startedAt: "2026-10-01T02:00:00Z",
      childProgress: { done: 3, total: 5 },
      members: [
        {
          userId: "user-quan",
          name: "Trần Minh Quân",
          avatar: null,
          assignedAt: "2026-10-01T02:00:00Z",
        },
      ],
      memberCount: 1,
      labels: [{ labelId: "l-data", name: "Dữ liệu", color: "#1E6FD0" }],
      commentCount: 4,
      checklistTotal: 2,
    }),
    item("i-1", "a0", { itemKey: "ERP-151", statusOptionId: TODO }),
    item("i-15", "a0V", { itemKey: "15", statusOptionId: TODO }),
  ]);

  it("orders groups and items by their position keys and keeps workspace scope", () => {
    const mapped = mapFullBoardDto(dto, { workspaceId: "workspace-1" });
    expect(mapped.board.workspaceId).toBe("workspace-1");
    expect(mapped.board.visibility).toBe("workspace");
    expect(mapped.groups.map((group) => group.id)).toEqual([
      "group-1",
      "group-2",
    ]);
    expect(mapped.groups[0]!.cards.map((card) => card.id)).toEqual([
      "i-1",
      "i-15",
      "i-2",
    ]);
    expect(mapped.groups[0]!.cards.map((card) => card.position)).toEqual([
      1, 2, 3,
    ]);
  });

  it("fills the item key, status, priority, dates and child progress", () => {
    const mapped = mapFullBoardDto(dto, { workspaceId: "workspace-1" });
    const card = mapped.groups[0]!.cards.find((c) => c.id === "i-2")!;
    expect(card).toMatchObject({
      title: "Item i-2",
      listId: "group-1",
      boardId: "board-1",
      workspaceId: "workspace-1",
      itemKey: "ERP-142",
      parentItemId: null,
      statusOptionId: DOING,
      status: DOING,
      statusCategory: "in_progress",
      priority: "urgent",
      dueAt: "2026-10-10T10:00:00Z",
      dueDate: "2026-10-10T10:00:00Z",
      startDate: "2026-10-01T02:00:00Z",
      childProgress: { done: 3, total: 5 },
      labels: [{ id: "l-data", name: "Dữ liệu", color: "#1E6FD0" }],
      _count: { comments: 4, attachments: 0, checklistItems: 2 },
    });
    expect(card.members.map((member) => member.name)).toEqual([
      "Trần Minh Quân",
    ]);
    expect(card.fieldValues[STATUS_FIELD]).toBe(DOING);
    expect(card.fieldValues[PRIORITY_FIELD]).toBe("p-urgent");
  });

  it("leaves priority empty when the item has none", () => {
    const mapped = mapFullBoardDto(dto, { workspaceId: "workspace-1" });
    const card = mapped.groups[0]!.cards.find((c) => c.id === "i-1")!;
    expect(card.priority).toBeUndefined();
    expect(card.dueAt).toBeNull();
    expect(card.dueDate).toBeUndefined();
  });

  it("maps field options with position and category and marks the status and priority fields", () => {
    const mapped = mapFullBoardDto(dto, { workspaceId: "workspace-1" });
    expect(mapped.fieldDefinitions.map((field) => field.id)).toEqual([
      "board-1",
      STATUS_FIELD,
      "f-not-priority",
      PRIORITY_FIELD,
    ]);
    const status = findStatusField(mapped.fieldDefinitions)!;
    expect(status.id).toBe(STATUS_FIELD);
    expect(status.fieldType).toBe("select");
    expect(status.isSystemField).toBe(true);
    expect(status.options.map((option) => option.label)).toEqual([
      "Cần làm",
      "Đang làm",
      "Chờ duyệt",
      "Hoàn thành",
    ]);
    expect(status.options[0]).toEqual({
      id: TODO,
      label: "Cần làm",
      color: "#7A8391",
      position: "a0",
      category: "todo",
    });
    expect(findPriorityField(mapped.fieldDefinitions)?.id).toBe(
      PRIORITY_FIELD,
    );
  });

  it("parses an old bundle's response shape (lists[].cards) unchanged", () => {
    const legacy: FullBoardDtoApi = {
      id: "board-legacy",
      title: "Legacy",
      background: "",
      visibility: "private",
      members: [],
      lists: [
        {
          id: "group-1",
          title: "First",
          position: 1,
          isArchived: false,
          cards: [
            {
              id: "card-1",
              title: "Legacy card",
              priority: "high",
              status: "Done",
              dueDate: "2026-10-18T10:00:00Z",
              memberCount: 0,
              members: [],
              labels: [],
              checklistProgress: 0,
              checklistTotal: 0,
              commentCount: 0,
              attachmentCount: 0,
              position: 1,
            },
          ],
        },
      ],
    };
    const mapped = mapFullBoardDto(legacy, { workspaceId: "workspace-1" });
    const card = mapped.groups[0]!.cards[0]!;
    expect(card).toMatchObject({
      title: "Legacy card",
      status: "status-done",
      statusOptionId: "status-done",
      statusCategory: "done",
      priority: "high",
      dueDate: "2026-10-18T10:00:00Z",
      dueAt: "2026-10-18T10:00:00Z",
      itemKey: null,
      parentItemId: null,
      childProgress: null,
    });
    expect(findStatusField(mapped.fieldDefinitions)?.name).toBe("Status");
  });
});

describe("priorityFromOptionName", () => {
  it.each([
    ["Khẩn", "urgent"],
    ["Cao", "high"],
    ["Trung bình", "medium"],
    ["Thấp", "low"],
    ["Urgent", "urgent"],
    ["High", "high"],
    ["Medium", "medium"],
    ["low", "low"],
    [" trung  BÌNH ", "medium"],
  ] as const)("maps %s to %s", (name, expected) => {
    expect(priorityFromOptionName(name)).toBe(expected);
  });

  it("returns undefined for an unknown or missing name", () => {
    expect(priorityFromOptionName("Rất gấp")).toBeUndefined();
    expect(priorityFromOptionName(null)).toBeUndefined();
    expect(priorityFromOptionName(undefined)).toBeUndefined();
  });
});

describe("groupCardsByStatus", () => {
  it("builds one lane per status option in option position order", () => {
    const mapped = mapFullBoardDto(
      generatedBoard(
        [
          item("done-1", "a0", { statusOptionId: DONE }),
          item("todo-1", "a1", { statusOptionId: TODO }),
          item("doing-1", "a2", { statusOptionId: DOING }),
        ],
        [item("todo-2", "a0", { statusOptionId: TODO })],
      ),
      { workspaceId: "workspace-1" },
    );
    const lanes = groupCardsByStatus(mapped);
    expect(lanes.map((lane) => lane.name)).toEqual([
      "Cần làm",
      "Đang làm",
      "Chờ duyệt",
      "Hoàn thành",
    ]);
    expect(lanes.map((lane) => lane.optionId)).toEqual([
      TODO,
      DOING,
      REVIEW,
      DONE,
    ]);
    expect(lanes.map((lane) => lane.category)).toEqual([
      "todo",
      "in_progress",
      "review",
      "done",
    ]);
    expect(lanes[0]!.color).toBe("#7A8391");
    // Group order first, then item order inside the group.
    expect(lanes[0]!.cards.map((card) => card.id)).toEqual([
      "todo-1",
      "todo-2",
    ]);
    expect(lanes.map((lane) => lane.total)).toEqual([2, 1, 0, 1]);
  });

  it("never shows child items or archived groups as top-level cards", () => {
    const mapped = mapFullBoardDto(
      generatedBoard([
        item("parent", "a0", {
          statusOptionId: DOING,
          childProgress: { done: 1, total: 2 },
        }),
        item("child", "a1", { statusOptionId: DOING, parentItemId: "parent" }),
      ]),
      { workspaceId: "workspace-1" },
    );
    const lanes = groupCardsByStatus(mapped);
    const ids = lanes.flatMap((lane) => lane.cards.map((card) => card.id));
    expect(ids).toEqual(["parent"]);
    expect(lanes.find((lane) => lane.optionId === DOING)!.total).toBe(1);
  });

  it("adds a no-status lane last, and only when an item has no known status", () => {
    const withoutStatus = groupCardsByStatus(
      mapFullBoardDto(
        generatedBoard([
          item("a", "a0", { statusOptionId: TODO }),
          item("b", "a1"),
          item("c", "a2", { statusOptionId: "opt-removed" }),
        ]),
        { workspaceId: "workspace-1" },
      ),
    );
    expect(withoutStatus).toHaveLength(5);
    const last = withoutStatus[4]!;
    expect(last).toMatchObject({
      optionId: null,
      name: "",
      category: null,
      color: null,
      total: 2,
    });
    expect(last.cards.map((card) => card.id)).toEqual(["b", "c"]);

    const allKnown = groupCardsByStatus(
      mapFullBoardDto(generatedBoard([item("a", "a0", { statusOptionId: TODO })]), {
        workspaceId: "workspace-1",
      }),
    );
    expect(allKnown.map((lane) => lane.optionId)).not.toContain(null);
    expect(allKnown).toHaveLength(4);
  });

  it("returns no lanes for a board without a status field and without items", () => {
    expect(groupCardsByStatus({ fieldDefinitions: [], groups: [] })).toEqual(
      [],
    );
  });
});

describe("mapBoardAccessDto", () => {
  const dto: GeneratedBoardAccessDto = {
    boardId: "board-1",
    version: 7,
    myLevel: "commenter",
    mySource: {
      kind: "explicit",
      grantedBy: { userId: "user-phuc", name: "Lê Hoàng Phúc" },
    },
    peopleCount: 32,
    guestCount: 1,
    peopleCountComplete: false,
    sources: [
      { kind: "space", id: "space-1", name: "Phòng Công nghệ", count: 24 },
      { kind: "team", id: null, name: null, count: 6 },
      { kind: "user", id: "user-linh", name: "Đỗ Khánh Linh", count: 1 },
      { kind: "guest", id: null, name: null, count: 1 },
    ],
  };

  it("keeps the caller's level, source, counts and named sources", () => {
    expect(mapBoardAccessDto(dto)).toEqual({
      boardId: "board-1",
      version: 7,
      myLevel: "commenter",
      mySource: {
        kind: "explicit",
        grantedBy: { userId: "user-phuc", name: "Lê Hoàng Phúc" },
      },
      peopleCount: 32,
      guestCount: 1,
      peopleCountComplete: false,
      sources: [
        { kind: "space", id: "space-1", name: "Phòng Công nghệ", count: 24 },
        { kind: "team", id: null, name: null, count: 6 },
        { kind: "user", id: "user-linh", name: "Đỗ Khánh Linh", count: 1 },
        { kind: "guest", id: null, name: null, count: 1 },
      ],
    });
  });

  it("accepts the workspace default source and fails closed on an unknown level", () => {
    const mapped = mapBoardAccessDto({
      boardId: "board-2",
      myLevel: "superuser",
      mySource: { kind: "workspace" },
      sources: [{ kind: "unknown-kind", count: 3 }],
    });
    expect(mapped.myLevel).toBe("viewer");
    expect(mapped.mySource).toEqual({ kind: "workspace", grantedBy: null });
    expect(mapped.peopleCount).toBe(0);
    expect(mapped.peopleCountComplete).toBe(false);
    expect(mapped.sources).toEqual([]);
  });
});

describe("mapCardDto over the generated item read", () => {
  it("reads the name, key and dates of the generated item", () => {
    const dto: GeneratedBoardItemDto = {
      id: "i-142",
      boardId: "board-1",
      workspaceId: "workspace-1",
      groupId: "group-1",
      name: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      members: [],
      labels: [],
      checklists: [],
      commentCount: 2,
      attachmentCount: 1,
      position: "a1",
      createdAt: "2026-10-01T02:00:00Z",
      updatedAt: null,
      version: 4,
      itemKey: "ERP-142",
      parentItemId: null,
      statusOptionId: DOING,
      priorityOptionId: "p-urgent",
      dueAt: "2026-10-10T10:00:00Z",
      startedAt: "2026-10-01T02:00:00Z",
      completedAt: null,
      childProgress: { done: 3, total: 5 },
    };
    expect(mapCardDto(dto)).toMatchObject({
      id: "i-142",
      listId: "group-1",
      title: "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP",
      itemKey: "ERP-142",
      statusOptionId: DOING,
      status: DOING,
      dueAt: "2026-10-10T10:00:00Z",
      dueDate: "2026-10-10T10:00:00Z",
      childProgress: { done: 3, total: 5 },
      _count: { comments: 2, attachments: 1, checklistItems: 0 },
    });
  });
});
