import { describe, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import { RealtimeClient, type WebSocketLike } from "@notrelix/realtime";
import { handleWorkspaceRecovery } from "../../realtime/workspace-recovery-policy";

function createMockSocket() {
  const socket: WebSocketLike = {
    readyState: 1,
    send: vi.fn(),
    close: vi.fn(function (this: WebSocketLike) {
      if (this.onclose) this.onclose({ code: 1000, reason: "Closed" });
    }),
    onopen: null,
    onmessage: null,
    onclose: null,
    onerror: null,
  };
  return socket;
}

function subscribed(workspaceId: string): string {
  return JSON.stringify({
    type: "subscribed",
    subscriptionId: `workspace:${workspaceId}`,
    scope: { kind: "workspace", id: workspaceId },
    streamKey: `workspace:Workspace:${workspaceId}`,
  });
}

function eventFrame(
  eventId: string,
  streamVersion: number,
  workspaceId = "ws-1",
): string {
  return JSON.stringify({
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
    correlationId: "corr-1",
    occurredAt: new Date().toISOString(),
  });
}

async function connectReady(client: RealtimeClient, socket: WebSocketLike) {
  const connectPromise = client.connect({ sessionGeneration: "gen-1" });
  await Promise.resolve();
  socket.onopen?.({});
  await connectPromise;
  socket.onmessage?.({
    data: JSON.stringify({
      type: "ready",
      schemaVersion: 1,
      connectionId: "c-1",
    }),
  });
}

describe("integration: realtime recovery reaches the query cache", () => {
  it("a stream gap invalidates the canonical workspace-scoped keys and the next event continues", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const wsKey = ["workspace", "ws-1", "workspace", "detail"];
    const otherWsKey = ["workspace", "ws-2", "workspace", "detail"];
    queryClient.setQueryData(wsKey, { name: "Old" });
    queryClient.setQueryData(otherWsKey, { name: "Other" });

    const socket = createMockSocket();
    const client = new RealtimeClient("wss://ws.test/api/v1/realtime", {
      socketFactory: vi.fn(() => socket),
    });
    const events = vi.fn();
    client.subscribe({ workspaceId: "ws-1" }, events);
    client.subscribeRecovery((signal) => {
      void handleWorkspaceRecovery({
        workspaceId: signal.workspaceId,
        invalidateQueries: async (keys) => {
          await Promise.all(
            keys.map((queryKey) => queryClient.invalidateQueries({ queryKey })),
          );
        },
      });
    });

    await connectReady(client, socket);
    socket.onmessage?.({ data: subscribed("ws-1") });
    // Subscription ack re-establishes authoritative state.
    expect(queryClient.getQueryState(wsKey)?.isInvalidated).toBe(true);
    queryClient.setQueryData(wsKey, { name: "Fresh" });
    expect(queryClient.getQueryState(wsKey)?.isInvalidated).toBe(false);

    socket.onmessage?.({ data: eventFrame("e1", 1) });
    socket.onmessage?.({ data: eventFrame("e3", 3) }); // gap

    expect(queryClient.getQueryState(wsKey)?.isInvalidated).toBe(true);
    expect(queryClient.getQueryState(otherWsKey)?.isInvalidated).toBe(false);
    expect(events).toHaveBeenCalledTimes(1);

    // Recovery rebased the checkpoint: the next +1 event is consumed once.
    socket.onmessage?.({ data: eventFrame("e4", 4) });
    expect(events).toHaveBeenCalledTimes(2);
  });

  it("after a workspace switch a late event of the previous workspace does not touch the new one", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const keyB = ["workspace", "ws-b", "workspace", "detail"];
    queryClient.setQueryData(keyB, { name: "B" });

    const socket = createMockSocket();
    const client = new RealtimeClient("wss://ws.test/api/v1/realtime", {
      socketFactory: vi.fn(() => socket),
    });
    const invalidateB = vi.fn(() =>
      queryClient.invalidateQueries({ queryKey: keyB }),
    );

    const offA = client.subscribe({ workspaceId: "ws-a" }, invalidateB);
    await connectReady(client, socket);
    socket.onmessage?.({ data: subscribed("ws-a") });

    offA();
    client.subscribe({ workspaceId: "ws-b" }, invalidateB);
    socket.onmessage?.({ data: subscribed("ws-b") });

    socket.onmessage?.({ data: eventFrame("late-a", 1, "ws-a") });

    expect(invalidateB).not.toHaveBeenCalled();
    expect(queryClient.getQueryState(keyB)?.isInvalidated).toBe(false);
  });
});
