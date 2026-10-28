import { describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import {
  boardRealtimeQueryKeys,
  workManagementRealtimeAdapter as adapter,
} from "../realtime";

function frame(
  overrides: Partial<Parameters<typeof adapter.supports>[0]> = {},
) {
  return {
    eventType: "board.board",
    resourceKind: "Board",
    resourceId: "b1",
    workspaceId: "ws1",
    ...overrides,
  };
}

function setup() {
  const invalidateQueries = vi.fn();
  return {
    invalidateQueries,
    context: { workspaceId: "ws1", boardId: "b1", invalidateQueries },
  };
}

describe("workManagementRealtimeAdapter", () => {
  it("supports board frames only", () => {
    expect(adapter.supports(frame())).toBe(true);
    expect(adapter.supports(frame({ eventType: "workspace.workspace" }))).toBe(
      false,
    );
  });

  it("invalidates the workspace's work-management scope for the open board's frame, never patches", async () => {
    const { context, invalidateQueries } = setup();

    await adapter.validateAndHandle(frame(), context);

    expect(invalidateQueries).toHaveBeenCalledTimes(1);
    expect(invalidateQueries).toHaveBeenCalledWith(
      boardRealtimeQueryKeys("ws1", "b1"),
    );
  });

  it("the invalidation reaches board, item detail, comments, checklists, labels and saved filters of the workspace only", async () => {
    const client = new QueryClient();
    const keys = {
      board: wmQueryKeys.fullBoard("ws1", "b1"),
      detail: wmQueryKeys.cardDetail("ws1", "i1"),
      comments: wmQueryKeys.cardComments("ws1", "i1"),
      checklists: wmQueryKeys.cardChecklists("ws1", "i1"),
      labels: wmQueryKeys.boardLabels("ws1", "b1"),
      filters: wmQueryKeys.boardSavedFilters("ws1", "b1"),
      otherWorkspace: wmQueryKeys.fullBoard("ws2", "b1"),
      otherBoard: wmQueryKeys.fullBoard("ws1", "b2"),
      myWork: ["workspace", "ws1", "work-management", "my-work"],
    };
    Object.values(keys).forEach((key) => client.setQueryData(key, { v: 1 }));

    await adapter.validateAndHandle(frame(), {
      workspaceId: "ws1",
      boardId: "b1",
      invalidateQueries: (list) =>
        Promise.all(
          list.map((queryKey) => client.invalidateQueries({ queryKey })),
        ).then(() => undefined),
    });

    for (const [name, key] of Object.entries(keys)) {
      expect(client.getQueryState(key)?.isInvalidated, name).toBe(
        !["otherWorkspace", "otherBoard", "myWork"].includes(name),
      );
    }
  });

  it("ignores a frame of another board (previous scope) and of another workspace", async () => {
    const { context, invalidateQueries } = setup();

    await adapter.validateAndHandle(frame({ resourceId: "b-old" }), context);
    await adapter.validateAndHandle(frame({ workspaceId: "ws-old" }), context);

    expect(invalidateQueries).not.toHaveBeenCalled();
  });

  it("ignores a board frame when no board is open", async () => {
    const { invalidateQueries } = setup();

    await adapter.validateAndHandle(frame(), {
      workspaceId: "ws1",
      invalidateQueries,
    });

    expect(invalidateQueries).not.toHaveBeenCalled();
  });

  it("duplicate and out-of-order frames are harmless: each only marks stale, state stays server-owned", async () => {
    const client = new QueryClient();
    const key = wmQueryKeys.fullBoard("ws1", "b1");
    client.setQueryData(key, { name: "server" });
    const context = {
      workspaceId: "ws1",
      boardId: "b1",
      invalidateQueries: async (list: readonly unknown[][]) => {
        await Promise.all(
          list.map((queryKey) => client.invalidateQueries({ queryKey })),
        );
      },
    };

    await adapter.validateAndHandle(frame(), context);
    await adapter.validateAndHandle(frame(), context);

    expect(client.getQueryData(key)).toEqual({ name: "server" });
    expect(client.getQueryState(key)?.isInvalidated).toBe(true);
  });
});
