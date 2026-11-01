import { describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import type { NotrelixClient } from "@notrelix/contracts";
import type {
  Card,
  FieldDefinition,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import { createCardApi } from "../api/item.api";
import { wmQueryKeys } from "../queries/keys";
import { runItemStatusCommand } from "../mutations/use-update-item-status";
import { runMoveCardCommand } from "../mutations/use-move-card";
import { runArchiveCardCommand } from "../mutations/use-archive-card";

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

function card(id: string, listId: string, position: number): Card {
  return {
    id,
    listId,
    boardId: "b-1",
    workspaceId: "ws-1",
    title: id,
    position,
    status: "o-todo",
    statusOptionId: "o-todo",
    statusCategory: "todo",
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: { [FIELD]: "o-todo" },
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: "2026-10-01T00:00:00.000Z",
    version: 3,
  };
}

function board(): FullBoardResponse {
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
        cards: [card("a", "g-1", 1), card("b", "g-1", 2), card("c", "g-1", 3)],
      },
      {
        id: "g-2",
        title: "Khác",
        position: 2,
        isCollapsed: false,
        cards: [card("d", "g-2", 1)],
      },
    ],
    fieldDefinitions: [statusField],
  };
}

const boardKey = wmQueryKeys.fullBoard("ws-1", "b-1");

function seeded() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity } },
  });
  queryClient.setQueryData(boardKey, board());
  return queryClient;
}

function cardIn(queryClient: QueryClient, id: string) {
  const data = queryClient.getQueryData<FullBoardResponse>(boardKey)!;
  for (const group of data.groups) {
    const found = group.cards.find((candidate) => candidate.id === id);
    if (found) return { group: group.id, card: found };
  }
  return undefined;
}

function order(queryClient: QueryClient, groupId: string): string[] {
  const data = queryClient.getQueryData<FullBoardResponse>(boardKey)!;
  return data.groups
    .find((group) => group.id === groupId)!
    .cards.map((candidate) => candidate.id);
}

function deferred() {
  let resolve!: () => void;
  let reject!: (error: Error) => void;
  const promise = new Promise<{ version?: number }>((res, rej) => {
    resolve = () => res({});
    reject = rej;
  });
  return { promise, resolve, reject };
}

const statusVars = (cardId: string) => ({
  workspaceId: "ws-1",
  boardId: "b-1",
  cardId,
  statusOptionId: "o-doing",
});

describe("a failed card write rolls back only that card", () => {
  it("keeps another card's in-flight status change when the first one fails", async () => {
    const queryClient = seeded();
    const writeA = deferred();
    const writeB = deferred();
    const updateStatus = vi.fn((cardId: string) =>
      cardId === "a" ? writeA.promise : writeB.promise,
    );
    const first = runItemStatusCommand(
      queryClient,
      { updateStatus },
      statusVars("a"),
    );
    const second = runItemStatusCommand(
      queryClient,
      { updateStatus },
      statusVars("b"),
    );
    await vi.waitFor(() => expect(updateStatus).toHaveBeenCalledTimes(2));

    writeA.reject(new Error("forbidden"));
    await expect(first).rejects.toThrow("forbidden");
    expect(cardIn(queryClient, "a")!.card.statusOptionId).toBe("o-todo");
    // B's optimistic status survives A's rollback.
    expect(cardIn(queryClient, "b")!.card.statusOptionId).toBe("o-doing");

    writeB.resolve();
    await second;
  });
});

describe("runMoveCardCommand", () => {
  it("sends the item version and the new neighbours, and a fresh key per attempt", async () => {
    const queryClient = seeded();
    const moveCard = vi.fn(async () => undefined);
    await runMoveCardCommand(
      queryClient,
      { moveCard },
      {
        workspaceId: "ws-1",
        boardId: "b-1",
        input: { cardId: "c", listId: "g-1", position: 0.5 },
      },
    );
    await runMoveCardCommand(
      queryClient,
      { moveCard },
      {
        workspaceId: "ws-1",
        boardId: "b-1",
        input: { cardId: "c", listId: "g-1", position: 0.5 },
      },
    );
    const calls = moveCard.mock.calls as unknown as Array<
      [
        {
          cardId: string;
          listId: string;
          expectedVersion?: number;
          previousSiblingId?: string | null;
          nextSiblingId?: string | null;
        },
        { idempotencyKey: string },
      ]
    >;
    expect(calls[0]![0]).toMatchObject({
      cardId: "c",
      listId: "g-1",
      expectedVersion: 3,
      previousSiblingId: null,
      nextSiblingId: "a",
    });
    expect(calls[0]![1].idempotencyKey).not.toBe(calls[1]![1].idempotencyKey);
    expect(calls[0]![1].idempotencyKey).toMatch(/^move-card:c:/);
  });

  it("keeps explicit neighbours and moves the card at once", async () => {
    const queryClient = seeded();
    const write = deferred();
    const moveCard = vi.fn(() => write.promise);
    const pending = runMoveCardCommand(
      queryClient,
      { moveCard },
      {
        workspaceId: "ws-1",
        boardId: "b-1",
        input: {
          cardId: "a",
          listId: "g-2",
          position: 2,
          previousSiblingId: "d",
          nextSiblingId: null,
        },
      },
    );
    await vi.waitFor(() => expect(moveCard).toHaveBeenCalled());
    expect(order(queryClient, "g-2")).toEqual(["d", "a"]);
    expect(order(queryClient, "g-1")).toEqual(["b", "c"]);
    expect(
      (moveCard.mock.calls[0] as unknown as [{ previousSiblingId: string }])[0]
        .previousSiblingId,
    ).toBe("d");
    write.resolve();
    await pending;
  });

  it("puts only the moved card back when the move fails", async () => {
    const queryClient = seeded();
    const writeA = deferred();
    const moveCard = vi.fn(() => writeA.promise);
    const updateStatus = vi.fn(
      () => new Promise<{ version?: number }>(() => undefined),
    );
    const move = runMoveCardCommand(
      queryClient,
      { moveCard },
      {
        workspaceId: "ws-1",
        boardId: "b-1",
        input: { cardId: "a", listId: "g-2", position: 2 },
      },
    );
    void runItemStatusCommand(queryClient, { updateStatus }, statusVars("d"));
    await vi.waitFor(() => expect(updateStatus).toHaveBeenCalled());
    writeA.reject(new Error("conflict"));
    await expect(move).rejects.toThrow("conflict");
    expect(order(queryClient, "g-1")).toEqual(["a", "b", "c"]);
    expect(cardIn(queryClient, "d")!.card.statusOptionId).toBe("o-doing");
  });

  it("refuses to send a move without a known item version", async () => {
    const queryClient = seeded();
    queryClient.setQueryData<FullBoardResponse>(boardKey, (old) => ({
      ...old!,
      groups: old!.groups.map((group) => ({
        ...group,
        cards: group.cards.map((item) => ({ ...item, version: null })),
      })),
    }));
    const moveCard = vi.fn(async () => undefined);
    await expect(
      runMoveCardCommand(
        queryClient,
        { moveCard },
        {
          workspaceId: "ws-1",
          boardId: "b-1",
          input: { cardId: "a", listId: "g-2", position: 2 },
        },
      ),
    ).rejects.toThrow(/version/);
    expect(moveCard).not.toHaveBeenCalled();
    expect(order(queryClient, "g-1")).toEqual(["a", "b", "c"]);
  });
});

describe("runArchiveCardCommand", () => {
  it("archives (not deletes) the item, takes it off the board at once and sends a fresh key", async () => {
    const queryClient = seeded();
    const write = deferred();
    const archiveCard = vi.fn(() => write.promise);
    const pending = runArchiveCardCommand(
      queryClient,
      { archiveCard },
      {
        workspaceId: "ws-1",
        boardId: "b-1",
        cardId: "b",
      },
    );
    await vi.waitFor(() => expect(archiveCard).toHaveBeenCalled());
    expect(order(queryClient, "g-1")).toEqual(["a", "c"]);
    const [cardId, expectedVersion, options] = archiveCard.mock
      .calls[0] as unknown as [string, number, { idempotencyKey: string }];
    expect(cardId).toBe("b");
    expect(expectedVersion).toBe(3);
    expect(options.idempotencyKey).toMatch(/^archive-item:b:/);
    write.resolve();
    await pending;
  });

  it("refuses to send an archive for an item whose version is not known", async () => {
    const queryClient = seeded();
    queryClient.setQueryData<FullBoardResponse>(boardKey, (current) => ({
      ...current!,
      groups: current!.groups.map((group) => ({
        ...group,
        cards: group.cards.map((item) => ({ ...item, version: null })),
      })),
    }));
    const archiveCard = vi.fn(async () => undefined);
    await expect(
      runArchiveCardCommand(
        queryClient,
        { archiveCard },
        { workspaceId: "ws-1", boardId: "b-1", cardId: "b" },
      ),
    ).rejects.toThrow(/version/);
    expect(archiveCard).not.toHaveBeenCalled();
    expect(order(queryClient, "g-1")).toEqual(["a", "b", "c"]);
  });

  it("sends the version the dialog opened with, not a newer one the cache holds now", async () => {
    const queryClient = seeded();
    const archiveCard = vi.fn(async () => undefined);
    await runArchiveCardCommand(
      queryClient,
      { archiveCard },
      { workspaceId: "ws-1", boardId: "b-1", cardId: "b", expectedVersion: 2 },
    );
    expect(archiveCard).toHaveBeenCalledWith("b", 2, expect.anything());
  });

  it("takes a sub-item's version from the item read, as it is not in the board read", async () => {
    const queryClient = seeded();
    queryClient.setQueryData(wmQueryKeys.cardDetail("ws-1", "sub"), {
      ...card("sub", "g-1", 9),
      version: 7,
    });
    const archiveCard = vi.fn(async () => undefined);
    await runArchiveCardCommand(
      queryClient,
      { archiveCard },
      { workspaceId: "ws-1", boardId: "b-1", cardId: "sub" },
    );
    expect(archiveCard).toHaveBeenCalledWith("sub", 7, expect.anything());
  });

  it("puts a stale version on the wire unchanged and never reads the item", async () => {
    const queryClient = seeded();
    // The server has moved on (v5); the person saw v3 and must be refused, not silently re-based.
    const get = vi.fn(async () => ({ id: "b", version: 5 }));
    const post = vi.fn(async (..._args: unknown[]) => undefined);
    const cards = createCardApi({
      api: { get, post },
    } as unknown as NotrelixClient);
    await runArchiveCardCommand(queryClient, cards, {
      workspaceId: "ws-1",
      boardId: "b-1",
      cardId: "b",
    });
    expect(get).not.toHaveBeenCalled();
    expect(post).toHaveBeenCalledWith(
      "/board-items/b/archive",
      { expectedVersion: 3 },
      expect.objectContaining({ idempotencyKey: expect.any(String) }),
    );
  });

  it("puts the card back when archiving fails", async () => {
    const queryClient = seeded();
    await expect(
      runArchiveCardCommand(
        queryClient,
        {
          archiveCard: async () => {
            throw new Error("forbidden");
          },
        },
        { workspaceId: "ws-1", boardId: "b-1", cardId: "b" },
      ),
    ).rejects.toThrow("forbidden");
    expect(order(queryClient, "g-1")).toEqual(["a", "b", "c"]);
  });
});

describe("board refetch while writes are in flight", () => {
  it("refetches the board read only when the last pending card write settles", async () => {
    const queryClient = seeded();
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    const writeA = deferred();
    const writeB = deferred();
    const updateStatus = vi.fn((cardId: string) =>
      cardId === "a" ? writeA.promise : writeB.promise,
    );
    const first = runItemStatusCommand(
      queryClient,
      { updateStatus },
      statusVars("a"),
    );
    const second = runItemStatusCommand(
      queryClient,
      { updateStatus },
      statusVars("b"),
    );
    await vi.waitFor(() => expect(updateStatus).toHaveBeenCalledTimes(2));
    const boardRefetches = () =>
      invalidate.mock.calls.filter(
        (call) =>
          JSON.stringify((call[0] as { queryKey: unknown }).queryKey) ===
          JSON.stringify(boardKey),
      ).length;

    writeB.resolve();
    await second;
    expect(boardRefetches()).toBe(0);
    expect(cardIn(queryClient, "a")!.card.statusOptionId).toBe("o-doing");

    writeA.resolve();
    await first;
    expect(boardRefetches()).toBe(1);
  });
});
