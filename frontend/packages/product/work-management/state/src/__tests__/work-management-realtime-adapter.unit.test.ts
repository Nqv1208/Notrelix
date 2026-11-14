import { describe, expect, it, vi } from "vitest";
import {
  boardRealtimeQueryKeys,
  workManagementRealtimeAdapter,
} from "../realtime/work-management-realtime-adapter";
import {
  holdRealtimeRefetch,
  isRealtimeRefetchHeld,
  onRealtimeRefetchRelease,
} from "../realtime/refetch-hold";

const frame = (
  over: Partial<{ resourceId: string; workspaceId: string }> = {},
) => ({
  eventType: "board.board",
  resourceKind: "board",
  resourceId: "board-1",
  workspaceId: "ws-1",
  ...over,
});

describe("workManagementRealtimeAdapter", () => {
  it("handles only the board topic", () => {
    expect(workManagementRealtimeAdapter.supports(frame())).toBe(true);
    expect(
      workManagementRealtimeAdapter.supports({
        ...frame(),
        eventType: "docs.page",
      }),
    ).toBe(false);
  });

  it("a frame of the open board refetches the board's reads and the item families", async () => {
    const invalidateQueries = vi.fn();
    await workManagementRealtimeAdapter.validateAndHandle(frame(), {
      workspaceId: "ws-1",
      boardId: "board-1",
      invalidateQueries,
    });
    expect(invalidateQueries).toHaveBeenCalledTimes(1);
    const keys = invalidateQueries.mock.calls[0]![0] as unknown[][];
    expect(keys).toEqual(boardRealtimeQueryKeys("ws-1", "board-1"));
    // Every key is scoped to the workspace: nothing of another scope is marked stale.
    expect(keys.every((key) => key.includes("ws-1"))).toBe(true);
    expect(keys.length).toBeGreaterThan(10);
  });

  it("ignores a frame of another board, of another workspace, or with no open board", async () => {
    const invalidateQueries = vi.fn();
    const adapter = workManagementRealtimeAdapter;
    await adapter.validateAndHandle(frame({ resourceId: "board-2" }), {
      workspaceId: "ws-1",
      boardId: "board-1",
      invalidateQueries,
    });
    await adapter.validateAndHandle(frame({ workspaceId: "ws-0" }), {
      workspaceId: "ws-1",
      boardId: "board-1",
      invalidateQueries,
    });
    await adapter.validateAndHandle(frame(), {
      workspaceId: "ws-1",
      invalidateQueries,
    });
    expect(invalidateQueries).not.toHaveBeenCalled();
  });

  it("a duplicate or out-of-order frame only asks for the same refetch again and patches nothing", async () => {
    const invalidateQueries = vi.fn();
    const context = {
      workspaceId: "ws-1",
      boardId: "board-1",
      invalidateQueries,
    };
    await workManagementRealtimeAdapter.validateAndHandle(frame(), context);
    await workManagementRealtimeAdapter.validateAndHandle(frame(), context);
    expect(invalidateQueries.mock.calls[1]).toEqual(
      invalidateQueries.mock.calls[0],
    );
  });
});

describe("refetch hold", () => {
  it("holds while a drag is in flight and tells the listener once when the last hold is released", () => {
    const listener = vi.fn();
    const off = onRealtimeRefetchRelease(listener);
    const first = holdRealtimeRefetch();
    const second = holdRealtimeRefetch();
    expect(isRealtimeRefetchHeld()).toBe(true);
    first();
    first();
    expect(listener).not.toHaveBeenCalled();
    second();
    expect(isRealtimeRefetchHeld()).toBe(false);
    expect(listener).toHaveBeenCalledTimes(1);
    off();
  });
});
