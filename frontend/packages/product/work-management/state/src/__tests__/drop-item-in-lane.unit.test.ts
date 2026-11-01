import { describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import type {
  Card,
  FieldDefinition,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import { runDropItemInLane } from "../mutations/use-drop-item-in-lane";

const FIELD = "field-status";
const statusField: FieldDefinition = {
  id: FIELD,
  boardId: "b-1",
  name: "Trạng thái",
  fieldType: "select",
  semanticRole: "status",
  position: 1,
  isHidden: false,
  isSystemField: true,
  version: 2,
  options: [
    { id: "o-todo", label: "Cần làm", color: "#7A8391", category: "todo" },
    {
      id: "o-doing",
      label: "Đang làm",
      color: "#1E7BE0",
      category: "in_progress",
    },
  ],
};

function card(id: string, optionId: string, position: number): Card {
  return {
    id,
    listId: "g-1",
    boardId: "b-1",
    workspaceId: "ws-1",
    title: id,
    position,
    status: optionId,
    statusOptionId: optionId,
    statusCategory: optionId === "o-todo" ? "todo" : "in_progress",
    version: 5,
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: { [FIELD]: optionId },
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: "2026-10-01T00:00:00.000Z",
  };
}

function seeded() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity } },
  });
  const key = wmQueryKeys.fullBoard("ws-1", "b-1");
  const full: FullBoardResponse = {
    board: {
      id: "b-1",
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
        id: "g-1",
        title: "Kế hoạch",
        position: 1,
        isCollapsed: false,
        cards: [
          card("a", "o-todo", 1),
          card("b", "o-doing", 2),
          card("c", "o-doing", 3),
        ],
      },
    ],
    fieldDefinitions: [statusField],
  };
  queryClient.setQueryData(key, full);
  return { queryClient, key };
}

const variables = {
  workspaceId: "ws-1",
  boardId: "b-1",
  cardId: "a",
  lane: {
    fieldDefinitionId: FIELD,
    optionId: "o-doing",
    viaStatusEndpoint: true,
  },
  placement: {
    listId: "g-1",
    position: 2.5,
    previousSiblingId: "b",
    nextSiblingId: "c",
  },
};

function cardOf(queryClient: QueryClient, key: readonly unknown[]) {
  return queryClient
    .getQueryData<FullBoardResponse>(key)!
    .groups[0]!.cards.find((candidate) => candidate.id === "a")!;
}

describe("runDropItemInLane", () => {
  it("shows the card in the new lane at its new place at once, then sends the move before the status", async () => {
    const { queryClient, key } = seeded();
    const calls: string[] = [];
    let during: Card | undefined;
    const writer = {
      moveCard: vi.fn(async () => {
        calls.push("move");
        during = cardOf(queryClient, key);
        return { version: 6 };
      }),
      updateStatus: vi.fn(async () => {
        calls.push("status");
      }),
      updateFieldValue: vi.fn(),
    };
    await runDropItemInLane(queryClient, writer, variables);
    expect(calls).toEqual(["move", "status"]);
    expect(during).toMatchObject({ statusOptionId: "o-doing", position: 2.5 });
    expect(writer.moveCard).toHaveBeenCalledWith(
      {
        cardId: "a",
        listId: "g-1",
        expectedVersion: 5,
        previousSiblingId: "b",
        nextSiblingId: "c",
      },
      expect.objectContaining({ idempotencyKey: expect.any(String) }),
    );
    expect(writer.updateStatus).toHaveBeenCalledWith(
      "a",
      "o-doing",
      6,
      expect.objectContaining({
        idempotencyKey: expect.stringMatching(/^item-status:a:/),
      }),
    );
  });

  it("writes a Select lane through its field value", async () => {
    const { queryClient } = seeded();
    const writer = {
      moveCard: vi.fn(async () => ({ version: 6 })),
      updateStatus: vi.fn(),
      updateFieldValue: vi.fn(async () => undefined),
    };
    await runDropItemInLane(queryClient, writer, {
      ...variables,
      lane: { ...variables.lane, viaStatusEndpoint: false },
    });
    expect(writer.updateStatus).not.toHaveBeenCalled();
    expect(writer.updateFieldValue).toHaveBeenCalledWith(
      {
        cardId: "a",
        fieldDefinitionId: FIELD,
        value: "o-doing",
      },
      { expectedVersion: 6 },
    );
  });

  it("moves the card back to its old place on the server when the status write fails after the move", async () => {
    const { queryClient } = seeded();
    const writer = {
      moveCard: vi.fn(async () => ({ version: 6 })),
      updateStatus: vi.fn(async () => {
        throw new Error("403");
      }),
      updateFieldValue: vi.fn(),
    };
    await expect(
      runDropItemInLane(queryClient, writer, variables),
    ).rejects.toThrow("403");
    expect(writer.moveCard).toHaveBeenCalledTimes(2);
    // a is the first card of its group: nothing before it, b after it.
    expect(writer.moveCard).toHaveBeenLastCalledWith(
      {
        cardId: "a",
        listId: "g-1",
        expectedVersion: 6,
        previousSiblingId: null,
        nextSiblingId: "b",
      },
      expect.objectContaining({
        idempotencyKey: expect.stringMatching(/^undo-move:a:/),
      }),
    );
  });

  it("puts the card back in its lane and place when the status write fails", async () => {
    const { queryClient, key } = seeded();
    const writer = {
      moveCard: vi.fn(async () => ({ version: 6 })),
      updateStatus: vi.fn(async () => {
        throw new Error("403");
      }),
      updateFieldValue: vi.fn(),
    };
    await expect(
      runDropItemInLane(queryClient, writer, variables),
    ).rejects.toThrow("403");
    expect(cardOf(queryClient, key)).toMatchObject({
      statusOptionId: "o-todo",
      position: 1,
    });
  });

  it("sends nothing when the item's version is unknown", async () => {
    const { queryClient, key } = seeded();
    queryClient.setQueryData<FullBoardResponse>(key, (old) => ({
      ...old!,
      groups: old!.groups.map((group) => ({
        ...group,
        cards: group.cards.map(
          ({ version: _version, ...rest }) => rest as Card,
        ),
      })),
    }));
    const writer = {
      moveCard: vi.fn(),
      updateStatus: vi.fn(),
      updateFieldValue: vi.fn(),
    };
    await expect(
      runDropItemInLane(queryClient, writer, variables),
    ).rejects.toThrow(/version/);
    expect(writer.moveCard).not.toHaveBeenCalled();
  });
});
