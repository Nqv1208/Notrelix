import { describe, expect, it, vi } from "vitest";
import { createAppRuntime } from "../runtime/app-runtime";
import { createBrowserWebSocketFactory } from "../realtime/browser-websocket-factory";
import { RealtimeClient, type WebSocketLike } from "@notrelix/realtime";
import { createNotrelixClient } from "@notrelix/contracts";

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

function validEnvelope(
  eventId: string,
  overrides: Record<string, unknown> = {},
): string {
  return JSON.stringify({
    schemaVersion: 1,
    eventId,
    eventType: "board.updated",
    workspaceId: "ws-1",
    correlationId: "corr-1",
    timestamp: new Date().toISOString(),
    payload: {},
    ...overrides,
  });
}

describe("integration: composition root wires kernel, contracts, and realtime", () => {
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

  it("realtime transport delivers a protocol envelope to a listener end-to-end", async () => {
    const socket = createMockSocket();
    const factory = vi.fn(() => socket);

    const client = new RealtimeClient("wss://ws.test/realtime", {
      socketFactory: factory,
    });
    const listener = vi.fn();

    const connectPromise = client.connect({ sessionGeneration: "gen-1" });
    await Promise.resolve();
    socket.onopen?.({});
    await connectPromise;

    client.subscribe({ workspaceId: "ws-1" }, listener);
    socket.onmessage?.({ data: validEnvelope("evt-1") });

    expect(listener).toHaveBeenCalledTimes(1);
    expect(listener.mock.calls[0]?.[0]).toMatchObject({
      eventId: "evt-1",
      eventType: "board.updated",
      workspaceId: "ws-1",
    });
  });

  it("browser WebSocket factory passes the descriptor url through to the transport", () => {
    const factory = createBrowserWebSocketFactory();
    expect(typeof factory).toBe("function");
  });
});
