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
  lane: { fieldDefinitionId: FIELD, optionId: "o-doing" },
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
  it("shows the card in the new lane at its new place at once, then sends one move with the lane's value", async () => {
    const { queryClient, key } = seeded();
    let during: Card | undefined;
    const writer = {
      moveCard: vi.fn(async () => {
        during = cardOf(queryClient, key);
        return { version: 7 };
      }),
    };
    await runDropItemInLane(queryClient, writer, variables);
    expect(writer.moveCard).toHaveBeenCalledTimes(1);
    expect(during).toMatchObject({ statusOptionId: "o-doing", position: 2.5 });
    expect(writer.moveCard).toHaveBeenCalledWith(
      {
        cardId: "a",
        listId: "g-1",
        expectedVersion: 5,
        previousSiblingId: "b",
        nextSiblingId: "c",
        laneFieldId: FIELD,
        laneOptionId: "o-doing",
      },
      expect.objectContaining({ idempotencyKey: expect.any(String) }),
    );
    // The server's new version is kept for the next write before the refetch.
    expect(cardOf(queryClient, key).version).toBe(7);
  });

  it("puts the card back in its lane and place with one rollback when the server refuses", async () => {
    const { queryClient, key } = seeded();
    const writer = {
      moveCard: vi.fn(async () => {
        throw Object.assign(new Error("403"), { status: 403 });
      }),
    };
    await expect(
      runDropItemInLane(queryClient, writer, variables),
    ).rejects.toThrow("403");
    // No second write undoes a first: a refusal changed nothing on the server.
    expect(writer.moveCard).toHaveBeenCalledTimes(1);
    expect(cardOf(queryClient, key)).toMatchObject({
      statusOptionId: "o-todo",
      position: 1,
    });
  });

  it("repeats a drop whose answer never came with the same key and version, and a refused one with a new key", async () => {
    const { queryClient } = seeded();
    const sent: { key?: string; version: number }[] = [];
    const outcomes: unknown[] = [
      new Error("network"),
      Object.assign(new Error("412"), { status: 412 }),
      { version: 7 },
    ];
    const writer = {
      moveCard: vi.fn(
        async (
          move: { expectedVersion: number },
          options?: { idempotencyKey?: string },
        ) => {
          sent.push({
            key: options?.idempotencyKey,
            version: move.expectedVersion,
          });
          const outcome = outcomes.shift();
          if (outcome instanceof Error) throw outcome;
          return outcome as { version: number };
        },
      ),
    };
    await expect(
      runDropItemInLane(queryClient, writer, variables),
    ).rejects.toThrow("network");
    await expect(
      runDropItemInLane(queryClient, writer, variables),
    ).rejects.toThrow("412");
    await runDropItemInLane(queryClient, writer, variables);
    expect(sent[1]).toEqual(sent[0]);
    expect(sent[2]!.key).not.toBe(sent[0]!.key);
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
    const writer = { moveCard: vi.fn() };
    await expect(
      runDropItemInLane(queryClient, writer, variables),
    ).rejects.toThrow(/version/);
    expect(writer.moveCard).not.toHaveBeenCalled();
  });
});
