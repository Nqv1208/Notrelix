import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
} from "@notrelix/dev-mock-backend";
import { bulkEach, createAssignedItem } from "../work-feed-item-writes";

const BOARD = mockIds.boards.erp;

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const requests: string[] = [];
  const mockFetch = createMockFetch(store);
  const { api } = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => {
      requests.push(
        `${init?.method ?? "GET"} ${new URL(String(input instanceof Request ? input.url : input)).pathname}`,
      );
      return mockFetch(input, init);
    },
  });
  return { store, api, requests };
}

describe("createAssignedItem", () => {
  it("creates in the Board's first group, assigns the member and sets To do", async () => {
    const { store, api, requests } = setup();
    const userId = store.getCurrentUser().id;

    const made = await createAssignedItem(api, {
      boardId: BOARD,
      name: "Chuẩn bị báo cáo",
      userId,
    });

    const card = store.getCard(made.id);
    expect(card?.title).toBe("Chuẩn bị báo cáo");
    expect(card?.assigneeUserIds).toContain(userId);
    expect(card?.listId).toBe(store.getLists(BOARD)[0]?.id);
    expect(requests.filter((entry) => !entry.endsWith("/auth/csrf"))).toEqual([
      `GET /api/v1/boards/${BOARD}/full`,
      `POST /api/v1/boards/${BOARD}/items`,
      `POST /api/v1/board-items/${made.id}/assignees`,
      `PATCH /api/v1/board-items/${made.id}/status`,
    ]);
    // Nothing is written to a route only the mock knows.
    expect(requests.join("\n")).not.toContain("my-work");
  });

  it("fails without creating anything when the Board is unknown", async () => {
    const { api, requests } = setup();
    await expect(
      createAssignedItem(api, { boardId: "missing", name: "x", userId: "u" }),
    ).rejects.toBeDefined();
    expect(requests.some((entry) => entry.startsWith("POST"))).toBe(false);
  });
});

describe("bulkEach", () => {
  const firstItem = (store: MockStore) => {
    const list = store.getLists(BOARD)[0]!;
    return store.createCard(BOARD, list.id, { title: "bulk" });
  };

  it("sets a due date with one due-date write per item", async () => {
    const { store, api, requests } = setup();
    const a = firstItem(store);
    const b = firstItem(store);
    const dueAt = "2026-11-02T00:00:00.000Z";

    const result = await bulkEach(api, {
      itemIds: [a.id, b.id],
      boardIdOf: () => BOARD,
      action: { type: "set-due", dueAt },
    });

    expect(result.results.map((entry) => entry.ok)).toEqual([true, true]);
    expect(store.getCard(a.id)?.dueAt).toBe(dueAt);
    expect(
      requests.filter((entry) => entry.includes("/due-date")),
    ).toHaveLength(2);
  });

  it("changes status to the Board's option for the bucket (one Board read)", async () => {
    const { store, api, requests } = setup();
    const a = firstItem(store);
    const b = firstItem(store);

    const result = await bulkEach(api, {
      itemIds: [a.id, b.id],
      boardIdOf: () => BOARD,
      action: { type: "set-status", bucket: "review" },
    });

    expect(result.results.every((entry) => entry.ok)).toBe(true);
    expect(requests.filter((entry) => entry.endsWith("/full"))).toHaveLength(1);
    expect(requests.filter((entry) => entry.endsWith("/status"))).toHaveLength(
      2,
    );
    expect(requests.join("\n")).not.toContain("status-bucket");
  });

  it("reports a failing item without stopping the others", async () => {
    const { store, api } = setup();
    const a = firstItem(store);

    const result = await bulkEach(api, {
      itemIds: [a.id, "missing-item"],
      boardIdOf: () => BOARD,
      action: { type: "assign", assigneeId: store.getCurrentUser().id },
    });

    expect(result.results).toEqual([
      { itemId: a.id, ok: true, error: null },
      { itemId: "missing-item", ok: false, error: "invalid" },
    ]);
    expect(store.getCard(a.id)?.assigneeUserIds).toContain(
      store.getCurrentUser().id,
    );
  });

  it("stops following with one delete per item", async () => {
    const { store, api, requests } = setup();
    const a = firstItem(store);
    const result = await bulkEach(api, {
      itemIds: [a.id],
      boardIdOf: () => BOARD,
      action: { type: "unfollow" },
    });
    expect(result.results[0]?.ok).toBe(true);
    expect(requests).toContain(`DELETE /api/v1/board-items/${a.id}/following`);
  });
});
