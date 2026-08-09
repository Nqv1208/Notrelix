import { describe, it, expect, vi, afterEach } from "vitest";
import { createBrowserWebSocketFactory } from "../browser-websocket-factory";

// Capture constructor arguments of the canonical RealtimeClient when the
// runtime composes its default realtime transport.
const realtimeConstructorCalls: Array<{
  url: string;
  options: Record<string, unknown>;
}> = [];

vi.mock("@notrelix/realtime", () => {
  class RealtimeClient {
    constructor(url: string, options: Record<string, unknown>) {
      realtimeConstructorCalls.push({ url, options });
    }
    getState() {
      return "idle";
    }
    async connect() {}
    disconnect() {}
    subscribe() {
      return () => {};
    }
    subscribeState() {
      return () => {};
    }
    subscribeRecovery() {
      return () => {};
    }
    dispose() {}
  }
  return { RealtimeClient };
});

// Import after vi.mock so the runtime picks up the mocked client.
const { createAppRuntime } = await import("../../runtime/app-runtime");

class FakeBrowserWebSocket {
  static instances: FakeBrowserWebSocket[] = [];
  url: string;
  protocols?: string | string[];
  constructor(url: string, protocols?: string | string[]) {
    this.url = url;
    this.protocols = protocols;
    FakeBrowserWebSocket.instances.push(this);
  }
}

afterEach(() => {
  vi.unstubAllGlobals();
  FakeBrowserWebSocket.instances = [];
  realtimeConstructorCalls.length = 0;
});

describe("browser realtime socket factory", () => {
  it("RT-020 constructs the browser WebSocket with the exact URL", () => {
    vi.stubGlobal("WebSocket", FakeBrowserWebSocket);

    const factory = createBrowserWebSocketFactory();
    const socket = factory({ url: "wss://realtime.example/realtime" });

    expect(FakeBrowserWebSocket.instances).toHaveLength(1);
    expect(socket).toBeInstanceOf(FakeBrowserWebSocket);
    expect((socket as unknown as FakeBrowserWebSocket).url).toBe(
      "wss://realtime.example/realtime",
    );
  });

  it("throws a clear error when the browser runtime lacks WebSocket", () => {
    vi.stubGlobal("WebSocket", undefined);

    const factory = createBrowserWebSocketFactory();

    expect(() => factory({ url: "wss://realtime.example/realtime" })).toThrow(
      /WebSocket is not supported/,
    );
  });
});

describe("app runtime realtime composition", () => {
  it("RT-021 uses the provided fake realtime factory when specified", () => {
    const fakeTransport = { dispose: vi.fn() } as any;
    const createRealtimeClient = vi.fn().mockReturnValue(fakeTransport);

    const runtime = createAppRuntime(
      { apiUrl: "http://api.test", realtimeUrl: "ws://realtime.test" },
      { createRealtimeClient },
    );

    expect(runtime.realtime).toBe(fakeTransport);
    expect(createRealtimeClient).toHaveBeenCalledWith("ws://realtime.test");
  });

  it("RT-022 routes the realtime error callback to the telemetry port", () => {
    const telemetry = {
      track: vi.fn(),
      reportError: vi.fn(),
      withContext: vi.fn(),
      flush: vi.fn(),
    };
    telemetry.withContext.mockReturnValue(telemetry);

    createAppRuntime(
      { apiUrl: "http://api.test", realtimeUrl: "ws://realtime.test" },
      { telemetry: telemetry as any },
    );

    expect(realtimeConstructorCalls).toHaveLength(1);
    const constructorCall = realtimeConstructorCalls[0];
    if (!constructorCall) {
      throw new Error("Expected RealtimeClient to be constructed");
    }
    const { url, options } = constructorCall;
    expect(url).toBe("ws://realtime.test");
    expect(options.socketFactory).toBeTypeOf("function");

    const realtimeTelemetry = options.telemetry as {
      track: (event: string, properties?: Record<string, unknown>) => void;
      reportError: (error: unknown, context?: Record<string, unknown>) => void;
    };
    expect(realtimeTelemetry).toBeDefined();

    realtimeTelemetry.reportError(new Error("socket failed"), {
      context: "socketError",
    });
    expect(telemetry.reportError).toHaveBeenCalledWith(
      expect.objectContaining({ message: "socket failed" }),
      expect.objectContaining({ context: "socketError" }),
    );
  });

  it("RT-023 runtime dispose disposes realtime exactly once", async () => {
    const fakeTransport = { dispose: vi.fn() } as any;

    const runtime = createAppRuntime(
      { apiUrl: "http://api.test", realtimeUrl: "ws://realtime.test" },
      { createRealtimeClient: () => fakeTransport },
    );

    await runtime.dispose();
    await runtime.dispose();

    expect(fakeTransport.dispose).toHaveBeenCalledTimes(1);
  });
});
