import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RealtimeClient, type WebSocketLike } from "@notrelix/realtime";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const state = vi.hoisted(() => ({
  pathname: "/workspaces/ws-a/boards",
  isAuthenticated: true,
  sessionGeneration: "user:1" as string | null,
  runtime: null as unknown,
}));

vi.mock("@tanstack/react-router", () => ({
  useLocation: () => ({ pathname: state.pathname }),
}));
vi.mock("@notrelix/runtime-web", () => ({
  useAppRuntime: () => state.runtime,
}));
vi.mock("@notrelix/features-auth", () => ({
  useAuth: () => ({
    isAuthenticated: state.isAuthenticated,
    sessionGeneration: state.sessionGeneration,
    user: state.isAuthenticated ? { id: "1" } : null,
  }),
}));
vi.mock("@notrelix/features-workspace", () => ({
  workspaceRealtimeAdapter: {
    id: "workspace-adapter",
    supports: (envelope: { eventType: string }) =>
      envelope.eventType.startsWith("workspace."),
    validateAndHandle: async (
      envelope: { workspaceId: string },
      context: {
        workspaceId: string;
        invalidateQueries: (keys: unknown[][]) => Promise<void> | void;
      },
    ) => {
      if (envelope.workspaceId === context.workspaceId) {
        await context.invalidateQueries([["workspace", context.workspaceId]]);
      }
    },
  },
}));

import { RealtimeLifecycle } from "../realtime-lifecycle";

function createSocket() {
  const sent: Array<Record<string, unknown>> = [];
  const socket: WebSocketLike & { sent: typeof sent } = {
    sent,
    readyState: 1,
    send: (data: string) => {
      sent.push(JSON.parse(data) as Record<string, unknown>);
    },
    close: vi.fn(),
    onopen: null,
    onmessage: null,
    onclose: null,
    onerror: null,
  };
  return socket;
}

function frame(body: Record<string, unknown>) {
  return { data: JSON.stringify(body) };
}

function subscribed(workspaceId: string) {
  return frame({
    type: "subscribed",
    subscriptionId: `workspace:${workspaceId}`,
    scope: { kind: "workspace", id: workspaceId },
    streamKey: `workspace:Workspace:${workspaceId}`,
  });
}

function event(workspaceId: string, eventId: string, streamVersion: number) {
  return frame({
    type: "event",
    subscriptionId: `workspace:${workspaceId}`,
    schemaVersion: 1,
    eventId,
    eventType: "workspace.updated",
    changeKind: "updated",
    streamKey: `workspace:Workspace:${workspaceId}`,
    streamVersion,
    resourceKind: "Workspace",
    resourceId: workspaceId,
    workspaceId,
    correlationId: "corr",
    occurredAt: new Date().toISOString(),
  });
}

function boardSubscribed(boardId: string) {
  return frame({
    type: "subscribed",
    subscriptionId: `board:${boardId}`,
    scope: { kind: "board", id: boardId },
    streamKey: `board:Board:${boardId}`,
  });
}

function boardEvent(
  boardId: string,
  eventId: string,
  streamVersion: number,
  workspaceId = "ws-a",
) {
  return frame({
    type: "event",
    subscriptionId: `board:${boardId}`,
    schemaVersion: 1,
    eventId,
    eventType: "board.board",
    changeKind: "UpdateBoardItemStatus",
    streamKey: `board:Board:${boardId}`,
    streamVersion,
    resourceKind: "Board",
    resourceId: boardId,
    workspaceId,
    correlationId: "corr",
    occurredAt: new Date().toISOString(),
  });
}

let root: Root | null = null;
let host: HTMLDivElement | null = null;
let sockets: ReturnType<typeof createSocket>[] = [];
let queryClient: QueryClient;
let realtime: RealtimeClient;
const telemetry = { track: vi.fn(), reportError: vi.fn() };

async function render() {
  await act(async () => {
    root?.render(
      <QueryClientProvider client={queryClient}>
        <RealtimeLifecycle>
          <span>child</span>
        </RealtimeLifecycle>
      </QueryClientProvider>,
    );
  });
}

async function flush() {
  await act(async () => {
    for (let i = 0; i < 6; i++) await Promise.resolve();
  });
}

beforeEach(async () => {
  state.pathname = "/workspaces/ws-a/boards";
  state.isAuthenticated = true;
  state.sessionGeneration = "user:1";
  sockets = [];
  telemetry.track.mockClear();
  telemetry.reportError.mockClear();
  queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  realtime = new RealtimeClient("wss://ws.test/api/v1/realtime", {
    socketFactory: () => {
      const socket = createSocket();
      sockets.push(socket);
      return socket;
    },
    telemetry,
  });
  state.runtime = { realtime, telemetry };
  host = document.createElement("div");
  document.body.appendChild(host);
  root = createRoot(host);
  await render();
  await flush();
});

afterEach(() => {
  act(() => root?.unmount());
  realtime.dispose();
  host?.remove();
  root = null;
  host = null;
});

async function openSocket(index: number) {
  const socket = sockets[index];
  if (!socket) throw new Error(`no socket ${index}`);
  await act(async () => {
    socket.onopen?.({});
    socket.onmessage?.(
      frame({ type: "ready", schemaVersion: 1, connectionId: "c" }),
    );
  });
  return socket;
}

describe("RealtimeLifecycle", () => {
  it("connects, subscribes the active workspace and refetches state on the ack", async () => {
    const key = ["workspace", "ws-a", "workspace", "detail"];
    queryClient.setQueryData(key, { name: "stale" });
    const socket = await openSocket(0);

    expect(socket.sent).toEqual([
      {
        type: "subscribe",
        subscriptionId: "workspace:ws-a",
        scope: { kind: "workspace", id: "ws-a" },
      },
    ]);

    await act(async () => {
      socket.onmessage?.(subscribed("ws-a"));
    });
    await flush();

    expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true);
  });

  it("a stream gap refetches the workspace scope; permission loss does too without a retry loop", async () => {
    const key = ["workspace", "ws-a", "workspace", "detail"];
    const socket = await openSocket(0);
    await act(async () => {
      socket.onmessage?.(subscribed("ws-a"));
      socket.onmessage?.(event("ws-a", "e1", 1));
    });
    queryClient.setQueryData(key, { name: "fresh" });
    expect(queryClient.getQueryState(key)?.isInvalidated).toBe(false);

    await act(async () => {
      socket.onmessage?.(event("ws-a", "e9", 9));
    });
    await flush();
    expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true);

    queryClient.setQueryData(key, { name: "fresh-2" });
    await act(async () => {
      socket.onmessage?.(
        frame({
          type: "subscription-error",
          subscriptionId: "workspace:ws-a",
          code: "subscription_revoked",
        }),
      );
    });
    await flush();

    expect(queryClient.getQueryState(key)?.isInvalidated).toBe(true);
    expect(telemetry.track).toHaveBeenCalledWith(
      "realtime.permission_lost",
      expect.objectContaining({ workspaceId: "ws-a" }),
    );
    expect(sockets).toHaveLength(1);
  });

  it("after a member is revoked the app's own socket stops delivering that workspace and never subscribes it again (UC-WSP-I04)", async () => {
    const key = ["workspace", "ws-a", "workspace", "detail"];
    const socket = await openSocket(0);
    await act(async () => {
      socket.onmessage?.(subscribed("ws-a"));
      socket.onmessage?.(event("ws-a", "e1", 1));
    });
    await act(async () => {
      socket.onmessage?.(
        frame({
          type: "subscription-error",
          subscriptionId: "workspace:ws-a",
          code: "subscription_revoked",
        }),
      );
    });
    await flush();

    // The revoke refetched over REST once; what the server still sends for the scope is dropped, not applied.
    queryClient.setQueryData(key, { name: "after-revoke" });
    expect(queryClient.getQueryState(key)?.isInvalidated).toBe(false);
    await act(async () => {
      socket.onmessage?.(event("ws-a", "e2", 2));
    });
    await flush();

    expect(queryClient.getQueryState(key)?.isInvalidated).toBe(false);
    expect(telemetry.track).toHaveBeenCalledWith(
      "realtime.event_for_inactive_subscription",
      expect.objectContaining({ eventId: "e2" }),
    );
    expect(
      socket.sent.filter((frame) => frame.type === "subscribe"),
    ).toHaveLength(1);
  });

  it("a late event of the previous workspace is ignored after a workspace switch", async () => {
    const keyA = ["workspace", "ws-a", "workspace", "detail"];
    const keyB = ["workspace", "ws-b", "workspace", "detail"];
    const socket = await openSocket(0);
    await act(async () => {
      socket.onmessage?.(subscribed("ws-a"));
    });

    state.pathname = "/workspaces/ws-b/boards";
    await render();
    await flush();
    await act(async () => {
      socket.onmessage?.(subscribed("ws-b"));
    });
    await flush();

    queryClient.setQueryData(keyA, { name: "A" });
    queryClient.setQueryData(keyB, { name: "B" });

    await act(async () => {
      socket.onmessage?.(event("ws-a", "late-a", 1));
      // Even a gap on the old scope must not refetch anything.
      socket.onmessage?.(event("ws-a", "late-a2", 7));
    });
    await flush();

    expect(queryClient.getQueryState(keyA)?.isInvalidated).toBe(false);
    expect(queryClient.getQueryState(keyB)?.isInvalidated).toBe(false);
    expect(socket.sent.filter((f) => f.type === "unsubscribe")).toEqual([
      { type: "unsubscribe", subscriptionId: "workspace:ws-a" },
    ]);
  });

  it("does not connect or subscribe while signed out", async () => {
    await act(async () => root?.unmount());
    realtime.dispose();
    sockets = [];
    state.isAuthenticated = false;
    state.sessionGeneration = null;
    realtime = new RealtimeClient("wss://ws.test/api/v1/realtime", {
      socketFactory: () => {
        const socket = createSocket();
        sockets.push(socket);
        return socket;
      },
    });
    state.runtime = { realtime, telemetry };
    root = createRoot(host as HTMLDivElement);
    await render();
    await flush();

    expect(sockets).toHaveLength(0);
    expect(realtime.getState()).toBe("closed");
  });

  describe("open board", () => {
    const itemKey = [
      "workspace",
      "ws-a",
      "work-management",
      "cards",
      "detail",
      "i1",
    ];

    async function openBoard(boardId: string) {
      state.pathname = `/workspaces/ws-a/boards/${boardId}`;
      await render();
      await flush();
    }

    it("subscribes the board stream and a board frame refetches the work-management scope", async () => {
      const socket = await openSocket(0);
      await openBoard("b1");
      await act(async () => {
        socket.onmessage?.(subscribed("ws-a"));
        socket.onmessage?.(boardSubscribed("b1"));
      });
      await flush();

      expect(socket.sent).toContainEqual({
        type: "subscribe",
        subscriptionId: "board:b1",
        scope: { kind: "board", id: "b1" },
      });

      queryClient.setQueryData(itemKey, { name: "fresh" });
      await act(async () => {
        socket.onmessage?.(boardEvent("b1", "e1", 1));
      });
      await flush();

      expect(queryClient.getQueryState(itemKey)?.isInvalidated).toBe(true);
    });

    it("a duplicate frame is applied once and a stream gap refetches the workspace scope", async () => {
      const socket = await openSocket(0);
      await openBoard("b1");
      await act(async () => {
        socket.onmessage?.(boardSubscribed("b1"));
        socket.onmessage?.(boardEvent("b1", "e1", 1));
      });
      await flush();
      queryClient.setQueryData(itemKey, { name: "fresh" });

      await act(async () => {
        socket.onmessage?.(boardEvent("b1", "e1", 1));
      });
      await flush();
      expect(queryClient.getQueryState(itemKey)?.isInvalidated).toBe(false);

      await act(async () => {
        socket.onmessage?.(boardEvent("b1", "e9", 9));
      });
      await flush();
      expect(queryClient.getQueryState(itemKey)?.isInvalidated).toBe(true);
    });

    it("a late frame of the previous board is ignored after opening another board", async () => {
      const socket = await openSocket(0);
      await openBoard("b1");
      await act(async () => {
        socket.onmessage?.(boardSubscribed("b1"));
      });
      await openBoard("b2");
      await act(async () => {
        socket.onmessage?.(boardSubscribed("b2"));
      });
      await flush();

      queryClient.setQueryData(itemKey, { name: "B2" });
      await act(async () => {
        socket.onmessage?.(boardEvent("b1", "late", 1));
        socket.onmessage?.(boardEvent("b1", "late2", 5));
      });
      await flush();

      expect(queryClient.getQueryState(itemKey)?.isInvalidated).toBe(false);
      expect(
        socket.sent.filter((f) => f.type === "unsubscribe"),
      ).toContainEqual({ type: "unsubscribe", subscriptionId: "board:b1" });
    });
  });
});
