import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createCardApi } from "../api/item.api";

function apiWith(post: ReturnType<typeof vi.fn>) {
  return createCardApi({ api: { post } } as unknown as NotrelixClient);
}

describe("createCardApi.bulkUpdate", () => {
  it("sends one request naming the board, the items and the change", async () => {
    const post = vi.fn(async (..._args: unknown[]) => ({ updated: 2 }));
    const updated = await apiWith(post).bulkUpdate("b-1", ["i-1", "i-2"], {
      type: "set-status",
      statusOptionId: "opt-done",
    });
    expect(updated).toBe(2);
    expect(post).toHaveBeenCalledTimes(1);
    const [path, body, options] = post.mock.calls[0] as unknown as [
      string,
      Record<string, unknown>,
      { idempotencyKey: string },
    ];
    expect(path).toBe("/boards/b-1/items/bulk");
    expect(body).toEqual({
      operation: "set-status",
      itemIds: ["i-1", "i-2"],
      status: "opt-done",
    });
    expect(options.idempotencyKey).toBeTruthy();
  });

  it.each([
    [
      { type: "set-due-date", dueDate: "2026-11-03T10:00:00.000Z" } as const,
      { operation: "set-due-date", dueDate: "2026-11-03T10:00:00.000Z" },
    ],
    [{ type: "clear-due-date" } as const, { operation: "clear-due-date" }],
    [
      { type: "assign", userId: "u-1" } as const,
      { operation: "assign", assigneeUserId: "u-1" },
    ],
    [{ type: "unassign-all" } as const, { operation: "unassign-all" }],
    [{ type: "archive" } as const, { operation: "archive" }],
    [
      { type: "set-priority", priorityOptionId: "opt-high" } as const,
      { operation: "set-priority", priority: "opt-high" },
    ],
    [{ type: "delete" } as const, { operation: "delete" }],
  ])("maps %j to the request body", async (change, expected) => {
    const post = vi.fn(async (..._args: unknown[]) => ({ updated: 1 }));
    await apiWith(post).bulkUpdate("b-1", ["i-1"], change);
    expect(post.mock.calls[0]?.[1]).toEqual({ itemIds: ["i-1"], ...expected });
  });
});
