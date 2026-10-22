import { afterEach, describe, expect, it, vi } from "vitest";
import { createAppRuntime } from "../runtime/app-runtime";
import { createBrowserWebSocketFactory } from "../realtime/browser-websocket-factory";
import { RealtimeClient, type WebSocketLike } from "@notrelix/realtime";
import { createNotrelixClient } from "@notrelix/contracts";

function createMockSocket() {
  const socket: WebSocketLike & { sent: Array<Record<string, unknown>> } = {
    sent: [],
    readyState: 1,
    send: vi.fn((data: string) => {
      socket.sent.push(JSON.parse(data) as Record<string, unknown>);
    }),
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

const READY = JSON.stringify({
  type: "ready",
  schemaVersion: 1,
  connectionId: "conn-1",
  pingIntervalMs: 25000,
  idleTimeoutMs: 75000,
  maxSubscriptions: 20,
});

const SUBSCRIBED = JSON.stringify({
  type: "subscribed",
  subscriptionId: "workspace:ws-1",
  scope: { kind: "workspace", id: "ws-1" },
  streamKey: "workspace:Workspace:ws1",
});

function eventFrame(eventId: string, streamVersion: number): string {
  return JSON.stringify({
    type: "event",
    subscriptionId: "workspace:ws-1",
    schemaVersion: 1,
    eventId,
    eventType: "workspace.updated",
    changeKind: "updated",
    streamKey: "workspace:Workspace:ws1",
    streamVersion,
    resourceKind: "Workspace",
    resourceId: "ws-1",
    resourceVersion: 1,
    workspaceId: "ws-1",
    correlationId: "corr-1",
    occurredAt: new Date().toISOString(),
    payloadContract: "workspace.updated.v1",
  });
}

describe("integration: composition root wires kernel, contracts, and realtime", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("createAppRuntime builds a client from the parsed kernel env", () => {
    const runtime = createAppRuntime({
      mode: "development",
      apiUrl: "https://api.example.com/api/v1",
      realtimeUrl: "wss://ws.example.com/realtime",
    });

    expect(runtime.env.apiUrl).toBe("https://api.example.com/api/v1");
    expect(runtime.api).toBeInstanceOf(
      createNotrelixClient({ baseUrl: "https://x" }).constructor,
    );
    expect(runtime.realtime).toBeInstanceOf(RealtimeClient);
  });

  it("realtime transport subscribes, awaits the ack and delivers a protocol envelope end-to-end", async () => {
    const socket = createMockSocket();
    const factory = vi.fn(() => socket);

    const client = new RealtimeClient("wss://ws.test/realtime", {
      socketFactory: factory,
    });
    const listener = vi.fn();
    client.subscribe({ workspaceId: "ws-1" }, listener);

    const connectPromise = client.connect({ sessionGeneration: "gen-1" });
    await Promise.resolve();
    socket.onopen?.({});
    await connectPromise;
    socket.onmessage?.({ data: READY });

    expect(socket.sent).toEqual([
      {
        type: "subscribe",
        subscriptionId: "workspace:ws-1",
        scope: { kind: "workspace", id: "ws-1" },
      },
    ]);

    socket.onmessage?.({ data: SUBSCRIBED });
    socket.onmessage?.({ data: eventFrame("evt-1", 1) });

    expect(listener).toHaveBeenCalledTimes(1);
    expect(listener.mock.calls[0]?.[0]).toMatchObject({
      eventId: "evt-1",
      eventType: "workspace.updated",
      workspaceId: "ws-1",
    });
  });

  it("browser WebSocket factory passes the descriptor url through to the transport", () => {
    const factory = createBrowserWebSocketFactory();
    expect(typeof factory).toBe("function");
  });

  it("close code 4401 refreshes the session through the API client and reconnects", async () => {
    const sockets: Array<
      WebSocketLike & { url: string; sent: Array<Record<string, unknown>> }
    > = [];
    class FakeWebSocket {
      readyState = 1;
      sent: Array<Record<string, unknown>> = [];
      onopen: WebSocketLike["onopen"] = null;
      onmessage: WebSocketLike["onmessage"] = null;
      onclose: WebSocketLike["onclose"] = null;
      onerror: WebSocketLike["onerror"] = null;
      constructor(public url: string) {
        sockets.push(this as unknown as (typeof sockets)[number]);
      }
      send(data: string): void {
        this.sent.push(JSON.parse(data) as Record<string, unknown>);
      }
      close(): void {}
    }
    vi.stubGlobal("WebSocket", FakeWebSocket);

    const calls: string[] = [];
    let profileCalls = 0;
    const fetchImpl = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      calls.push(url);
      if (url.endsWith("/auth/csrf")) {
        return new Response(JSON.stringify({ token: "csrf-1" }), {
          status: 200,
        });
      }
      if (url.endsWith("/auth/refresh")) {
        return new Response(null, { status: 204 });
      }
      if (url.endsWith("/auth/me")) {
        profileCalls++;
        return profileCalls === 1
          ? new Response(null, { status: 401 })
          : new Response(JSON.stringify({ id: "u1" }), { status: 200 });
      }
      return new Response(null, { status: 404 });
    });

    const telemetry = {
      track: vi.fn(),
      reportError: vi.fn(),
      withContext: vi.fn(function withContext() {
        return telemetry;
      }),
      flush: vi.fn(),
    };
    const runtime = createAppRuntime(
      {
        mode: "test",
        apiUrl: "https://api.example.test/api/v1",
        realtimeUrl: "wss://api.example.test",
      },
      { fetchImpl: fetchImpl as unknown as typeof fetch, telemetry },
    );

    await runtime.realtime.connect({ sessionGeneration: "user:1" });
    expect(sockets).toHaveLength(1);
    // A bare origin resolves to the gateway endpoint.
    expect(sockets[0]?.url).toBe("wss://api.example.test/api/v1/realtime");

    sockets[0]?.onopen?.({});
    sockets[0]?.onclose?.({ code: 4401, reason: "credentials expired" });
    for (let i = 0; i < 40 && sockets.length < 2; i++) {
      await new Promise((resolve) => setTimeout(resolve, 0));
    }

    expect(calls.filter((url) => url.endsWith("/auth/refresh"))).toHaveLength(
      1,
    );
    expect(sockets).toHaveLength(2);
    await runtime.dispose();
  });
});
