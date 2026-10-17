import { describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import type {
  CardDetail,
  FieldDefinition,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import {
  runCreateItemInLane,
  runItemStatusCommand,
} from "../mutations/use-update-item-status";

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
  options: [
    { id: "o-todo", label: "Cần làm", color: "", category: "todo" },
    { id: "o-doing", label: "Đang làm", color: "", category: "in_progress" },
  ],
};

const baseCard = {
  id: "i-1",
  listId: "g-1",
  boardId: "b-1",
  workspaceId: "ws-1",
  title: "Việc",
  position: 1,
  status: "o-todo",
  statusOptionId: "o-todo",
  statusCategory: "todo" as const,
  isArchived: false,
  isDeleted: false,
  members: [],
  labels: [],
  checklists: [],
  fieldValues: { [FIELD]: "o-todo" },
  _count: { comments: 0, attachments: 0, checklistItems: 0 },
  createdAt: "2026-10-01T00:00:00.000Z",
};

function fullBoard(): FullBoardResponse {
  return {
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
        cards: [baseCard],
      },
    ],
    fieldDefinitions: [statusField],
  };
}

function detail(): CardDetail {
  return {
    ...baseCard,
    boardTitle: "Board",
    watchers: [],
    isWatched: false,
    updates: [],
    files: [],
    activity: [],
  };
}

function seeded() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity } },
  });
  const boardKey = wmQueryKeys.fullBoard("ws-1", "b-1");
  const detailKey = wmQueryKeys.cardDetail("ws-1", "i-1");
  queryClient.setQueryData(boardKey, fullBoard());
  queryClient.setQueryData(detailKey, detail());
  return { queryClient, boardKey, detailKey };
}

function statusOf(queryClient: QueryClient, key: readonly unknown[]) {
  const data = queryClient.getQueryData<FullBoardResponse | CardDetail>(key);
  if (!data) return undefined;
  if ("groups" in data) return data.groups[0]!.cards[0]!.statusOptionId;
  return data.statusOptionId;
}

describe("runItemStatusCommand", () => {
  it("applies the status to the board and the item detail before the server answers", async () => {
    const { queryClient, boardKey, detailKey } = seeded();
    let release!: () => void;
    const updateStatus = vi.fn(
      () => new Promise<void>((resolve) => (release = resolve)),
    );
    const pending = runItemStatusCommand(
      queryClient,
      { updateStatus },
      {
        workspaceId: "ws-1",
        boardId: "b-1",
        cardId: "i-1",
        statusOptionId: "o-doing",
      },
    );
    await vi.waitFor(() => expect(updateStatus).toHaveBeenCalled());
    expect(statusOf(queryClient, boardKey)).toBe("o-doing");
    expect(statusOf(queryClient, detailKey)).toBe("o-doing");
    release();
    await pending;
    const call = updateStatus.mock.calls[0] as unknown as [
      string,
      string,
      { idempotencyKey: string },
    ];
    expect(call[0]).toBe("i-1");
    expect(call[1]).toBe("o-doing");
    expect(call[2].idempotencyKey.length).toBeGreaterThanOrEqual(8);
    expect(call[2].idempotencyKey.length).toBeLessThanOrEqual(128);
  });

  it("rolls the board and the item detail back when the write fails", async () => {
    const { queryClient, boardKey, detailKey } = seeded();
    const updateStatus = vi.fn(async () => {
      throw new Error("forbidden");
    });
    await expect(
      runItemStatusCommand(
        queryClient,
        { updateStatus },
        {
          workspaceId: "ws-1",
          boardId: "b-1",
          cardId: "i-1",
          statusOptionId: "o-doing",
        },
      ),
    ).rejects.toThrow("forbidden");
    expect(statusOf(queryClient, boardKey)).toBe("o-todo");
    expect(statusOf(queryClient, detailKey)).toBe("o-todo");
  });

  it("uses a fresh idempotency key for every command", async () => {
    const { queryClient } = seeded();
    const updateStatus = vi.fn(async () => undefined);
    const variables = {
      workspaceId: "ws-1",
      boardId: "b-1",
      cardId: "i-1",
      statusOptionId: "o-doing",
    };
    await runItemStatusCommand(queryClient, { updateStatus }, variables);
    await runItemStatusCommand(queryClient, { updateStatus }, variables);
    const keys = updateStatus.mock.calls.map(
      (call) =>
        (call as unknown as [string, string, { idempotencyKey: string }])[2]
          .idempotencyKey,
    );
    expect(new Set(keys).size).toBe(2);
  });
});

describe("runCreateItemInLane", () => {
  it("creates the item in the group, then sets the lane's status", async () => {
    const { queryClient, boardKey } = seeded();
    const calls: string[] = [];
    const services = {
      createCard: vi.fn(async () => {
        calls.push("create");
        return "i-new";
      }),
      updateStatus: vi.fn(async () => {
        calls.push("status");
      }),
    };
    let optimisticLane: string | null | undefined;
    services.createCard.mockImplementationOnce(async () => {
      const board = queryClient.getQueryData<FullBoardResponse>(boardKey)!;
      optimisticLane = board.groups[0]!.cards.find((card) =>
        card.id.startsWith("optimistic-"),
      )?.statusOptionId;
      calls.push("create");
      return "i-new";
    });
    await runCreateItemInLane(queryClient, services, {
      workspaceId: "ws-1",
      boardId: "b-1",
      listId: "g-1",
      title: "Việc mới",
      statusOptionId: "o-doing",
    });
    expect(calls).toEqual(["create", "status"]);
    expect(optimisticLane).toBe("o-doing");
    expect(services.updateStatus).toHaveBeenCalledWith(
      "i-new",
      "o-doing",
      expect.objectContaining({ idempotencyKey: expect.any(String) }),
    );
  });

  it("skips the status write when no status is asked for", async () => {
    const { queryClient } = seeded();
    const services = {
      createCard: vi.fn(async () => "i-new"),
      updateStatus: vi.fn(async () => undefined),
    };
    await runCreateItemInLane(queryClient, services, {
      workspaceId: "ws-1",
      boardId: "b-1",
      listId: "g-1",
      title: "Việc mới",
      statusOptionId: null,
    });
    expect(services.updateStatus).not.toHaveBeenCalled();
  });

  it("sets another Select field's value when the board is grouped by it", async () => {
    const { queryClient, boardKey } = seeded();
    let optimisticValue: unknown;
    const services = {
      createCard: vi.fn(async () => {
        const board = queryClient.getQueryData<FullBoardResponse>(boardKey)!;
        optimisticValue = board.groups[0]!.cards.find((card) =>
          card.id.startsWith("optimistic-"),
        )?.fieldValues["field-priority"];
        return "i-new";
      }),
      updateStatus: vi.fn(async () => undefined),
      updateFieldValue: vi.fn(async () => undefined),
    };
    await runCreateItemInLane(queryClient, services, {
      workspaceId: "ws-1",
      boardId: "b-1",
      listId: "g-1",
      title: "Việc mới",
      statusOptionId: null,
      fieldValue: { fieldDefinitionId: "field-priority", value: "p-high" },
    });
    expect(optimisticValue).toBe("p-high");
    expect(services.updateStatus).not.toHaveBeenCalled();
    expect(services.updateFieldValue).toHaveBeenCalledWith({
      cardId: "i-new",
      fieldDefinitionId: "field-priority",
      value: "p-high",
    });
  });

  it("removes the optimistic card when the create fails", async () => {
    const { queryClient, boardKey } = seeded();
    const services = {
      createCard: vi.fn(async () => {
        throw new Error("offline");
      }),
      updateStatus: vi.fn(async () => undefined),
    };
    await expect(
      runCreateItemInLane(queryClient, services, {
        workspaceId: "ws-1",
        boardId: "b-1",
        listId: "g-1",
        title: "Việc mới",
        statusOptionId: "o-doing",
      }),
    ).rejects.toThrow("offline");
    const board = queryClient.getQueryData<FullBoardResponse>(boardKey)!;
    expect(board.groups[0]!.cards.map((card) => card.id)).toEqual(["i-1"]);
  });
});
