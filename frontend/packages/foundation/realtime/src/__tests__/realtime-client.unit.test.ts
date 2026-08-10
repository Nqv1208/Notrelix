import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import {
  RealtimeClient,
  transitionState,
  type WebSocketLike,
} from "../transport/realtime-client";

const TEST_URL = "ws://realtime.test/realtime";

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

interface FakeTimer {
  callback: () => void;
  delay: number;
  cancelled: boolean;
}

function lastDelay(timers: FakeTimer[]): number {
  const timer = timers[timers.length - 1];
  if (!timer) throw new Error("Expected an active timer");
  return timer.delay;
}

function createFakeScheduler() {
  const timeouts: FakeTimer[] = [];
  const intervals: FakeTimer[] = [];
  const scheduler = {
    setTimeout(callback: () => void, delay: number) {
      const timer: FakeTimer = { callback, delay, cancelled: false };
      timeouts.push(timer);
      return timer;
    },
    clearTimeout(handle: unknown) {
      (handle as FakeTimer).cancelled = true;
    },
    setInterval(callback: () => void, delay: number) {
      const timer: FakeTimer = { callback, delay, cancelled: false };
      intervals.push(timer);
      return timer;
    },
    clearInterval(handle: unknown) {
      (handle as FakeTimer).cancelled = true;
    },
  };
  return {
    timeouts,
    intervals,
    scheduler,
    activeTimeouts: () => timeouts.filter((t) => !t.cancelled),
    fireLastTimeout: () => {
      const active = timeouts.filter((t) => !t.cancelled);
      const timer = active[active.length - 1];
      if (!timer) throw new Error("No active timer to fire");
      timer.cancelled = true;
      timer.callback();
    },
  };
}

function validEnvelope(overrides: Record<string, unknown> = {}): string {
  return JSON.stringify({
    schemaVersion: 1,
    eventId: "evt-1",
    eventType: "board.updated",
    workspaceId: "ws-1",
    correlationId: "corr-1",
    timestamp: new Date().toISOString(),
    payload: {},
    ...overrides,
  });
}

describe("RealtimeStateTransition Machine", () => {
  it("transitions idle -> connecting on CONNECT_REQUESTED", () => {
    expect(transitionState("idle", "CONNECT_REQUESTED")).toBe("connecting");
  });

  it("transitions connecting -> connected on SOCKET_OPENED", () => {
    expect(transitionState("connecting", "SOCKET_OPENED")).toBe("connected");
  });

  it("transitions connected -> reconnecting on SOCKET_CLOSED", () => {
    expect(transitionState("connected", "SOCKET_CLOSED")).toBe("reconnecting");
  });

  it("transitions any state to closed on MANUAL_DISCONNECT or DISPOSED", () => {
    expect(transitionState("connected", "MANUAL_DISCONNECT")).toBe("closed");
    expect(transitionState("reconnecting", "DISPOSED")).toBe("closed");
  });
});

describe("RealtimeClient foundation contract", () => {
  let sockets: WebSocketLike[];
  let factory: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    sockets = [];
    factory = vi.fn(() => {
      const socket = createMockSocket();
      sockets.push(socket);
      return socket;
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  function socketAt(index: number): WebSocketLike {
    const socket = sockets[index];
    if (!socket) throw new Error(`Expected a socket at index ${index}`);
    return socket;
  }

  function lastSocket(): WebSocketLike {
    return socketAt(sockets.length - 1);
  }

  async function connectAndOpen(
    client: RealtimeClient,
  ): Promise<WebSocketLike> {
    const connectPromise = client.connect({ sessionGeneration: "gen-1" });
    await Promise.resolve();
    const socket = lastSocket();
    socket.onopen?.({});
    await connectPromise;
    return socket;
  }

  it("RT-001 constructor does not access global WebSocket", async () => {
    vi.stubGlobal("WebSocket", undefined);

    const client = new RealtimeClient(TEST_URL, { socketFactory: factory });
    await connectAndOpen(client);

    expect(client.getState()).toBe("connected");
    expect(factory).toHaveBeenCalledTimes(1);
  });

  it("RT-002 connect() creates exactly one socket with the configured URL", async () => {
    const client = new RealtimeClient(TEST_URL, { socketFactory: factory });

    await connectAndOpen(client);

    expect(factory).toHaveBeenCalledTimes(1);
    expect(factory.mock.calls[0]?.[0]).toMatchObject({ url: TEST_URL });
  });

  it("RT-003 second connect() while connecting/open does not create another socket", async () => {
    const client = new RealtimeClient(TEST_URL, { socketFactory: factory });

    const first = client.connect({ sessionGeneration: "gen-1" });
    await client.connect({ sessionGeneration: "gen-2" });
    await Promise.resolve();
    socketAt(0).onopen?.({});
    await first;
    await client.connect({ sessionGeneration: "gen-3" });

    expect(factory).toHaveBeenCalledTimes(1);
  });

  it("RT-004 valid incoming message is parsed and dispatched through the subscription API", async () => {
    const client = new RealtimeClient(TEST_URL, { socketFactory: factory });
    const listener = vi.fn();
    client.subscribe({ workspaceId: "ws-1" }, listener);

    await connectAndOpen(client);
    socketAt(0).onmessage?.({ data: validEnvelope() });

    expect(listener).toHaveBeenCalledTimes(1);
    expect(listener.mock.calls[0]?.[0]).toMatchObject({
      eventId: "evt-1",
      workspaceId: "ws-1",
    });
  });

  it("RT-005 malformed message calls the error port and does not crash the message loop", async () => {
    const telemetry = { track: vi.fn(), reportError: vi.fn() };
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: factory,
      telemetry,
    });
    const listener = vi.fn();
    client.subscribe({ workspaceId: "ws-1" }, listener);

    await connectAndOpen(client);
    socketAt(0).onmessage?.({ data: "{not-valid-json" });
    socketAt(0).onmessage?.({ data: validEnvelope() });

    expect(telemetry.reportError).toHaveBeenCalledTimes(1);
    expect(listener).toHaveBeenCalledTimes(1);
  });

  it("RT-006 network close schedules exactly one reconnect", async () => {
    const { scheduler, activeTimeouts } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: factory,
      scheduler,
    });

    await connectAndOpen(client);
    const timersBefore = activeTimeouts().length;
    socketAt(0).onclose?.({ code: 1006, reason: "network" });

    expect(activeTimeouts().length).toBe(timersBefore + 1);
    expect(client.getState()).toBe("reconnecting");
  });

  it("RT-007 reconnect delay follows deterministic 1s,2s,4s,... capped at 30s", async () => {
    const { scheduler, activeTimeouts, fireLastTimeout } =
      createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: factory,
      scheduler,
    });

    await connectAndOpen(client);

    const reconnectDelays: number[] = [];
    for (let attempt = 0; attempt < 7; attempt++) {
      const current = lastSocket();
      current.onclose?.({ code: 1006, reason: "network" });
      reconnectDelays.push(lastDelay(activeTimeouts()));
      fireLastTimeout();
      await Promise.resolve();
      await Promise.resolve();
    }

    expect(reconnectDelays).toEqual([
      1000, 2000, 4000, 8000, 16000, 30000, 30000,
    ]);
  });

  it("RT-008 successful open resets the reconnect attempt counter", async () => {
    const { scheduler, activeTimeouts, fireLastTimeout } =
      createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: factory,
      scheduler,
    });

    await connectAndOpen(client);

    socketAt(0).onclose?.({ code: 1006, reason: "network" });
    expect(lastDelay(activeTimeouts())).toBe(1000);

    fireLastTimeout();
    await Promise.resolve();
    await Promise.resolve();
    socketAt(1).onopen?.({});
    expect(client.getState()).toBe("connected");

    socketAt(1).onclose?.({ code: 1006, reason: "network" });
    expect(lastDelay(activeTimeouts())).toBe(1000);
  });

  it("RT-009 manual disconnect() cancels a pending reconnect", async () => {
    const { scheduler, activeTimeouts } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: factory,
      scheduler,
    });

    await connectAndOpen(client);
    socketAt(0).onclose?.({ code: 1006, reason: "network" });
    expect(activeTimeouts().length).toBeGreaterThan(0);

    client.disconnect();

    expect(activeTimeouts().length).toBe(0);
  });

  it("RT-010 manual disconnect() closes the socket exactly once", async () => {
    const client = new RealtimeClient(TEST_URL, { socketFactory: factory });

    await connectAndOpen(client);
    client.disconnect();
    client.disconnect();

    expect(socketAt(0).close).toHaveBeenCalledTimes(1);
    expect(client.getState()).toBe("closed");
  });

  it("RT-011 dispose() cancels timers, closes the socket and is idempotent", async () => {
    const { scheduler, activeTimeouts } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: factory,
      scheduler,
    });

    await connectAndOpen(client);
    client.dispose();
    client.dispose();

    expect(socketAt(0).close).toHaveBeenCalledTimes(1);
    expect(activeTimeouts().length).toBe(0);
    expect(client.getState()).toBe("closed");
  });

  it("RT-012 no reconnect happens after dispose", async () => {
    const {
      scheduler,
      activeTimeouts,
      fireLastTimeout: _fireLastTimeout,
    } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: factory,
      scheduler,
    });

    await connectAndOpen(client);
    socketAt(0).onclose?.({ code: 1006, reason: "network" });
    client.dispose();

    expect(activeTimeouts().length).toBe(0);
    await client.connect({ sessionGeneration: "gen-2" });
    await Promise.resolve();
    expect(factory).toHaveBeenCalledTimes(1);
  });

  it("RT-013 subscriptions survive network reconnect", async () => {
    const { scheduler, fireLastTimeout } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: factory,
      scheduler,
    });
    const listener = vi.fn();
    client.subscribe({ workspaceId: "ws-1" }, listener);

    await connectAndOpen(client);
    socketAt(0).onclose?.({ code: 1006, reason: "network" });
    fireLastTimeout();
    await Promise.resolve();
    await Promise.resolve();
    socketAt(1).onopen?.({});

    socketAt(1).onmessage?.({
      data: validEnvelope({ eventId: "evt-after-reconnect" }),
    });
    expect(listener).toHaveBeenCalledTimes(1);
    expect(factory).toHaveBeenCalledTimes(2);
  });

  it("FND-045 unsubscribe detaches the listener without affecting other subscribers", async () => {
    const client = new RealtimeClient(TEST_URL, { socketFactory: factory });
    const listenerA = vi.fn();
    const listenerB = vi.fn();
    const unsubscribe = client.subscribe({ workspaceId: "ws-1" }, listenerA);
    client.subscribe({ workspaceId: "ws-1" }, listenerB);

    await connectAndOpen(client);

    socketAt(0).onmessage?.({ data: validEnvelope({ eventId: "evt-1" }) });
    expect(listenerA).toHaveBeenCalledTimes(1);
    expect(listenerB).toHaveBeenCalledTimes(1);

    unsubscribe();
    socketAt(0).onmessage?.({ data: validEnvelope({ eventId: "evt-2" }) });

    expect(listenerA).toHaveBeenCalledTimes(1);
    expect(listenerB).toHaveBeenCalledTimes(2);
  });

  it("FND-045 dispose detaches all subscribers so no listener fires afterwards", async () => {
    const client = new RealtimeClient(TEST_URL, { socketFactory: factory });
    const listener = vi.fn();
    client.subscribe({ workspaceId: "ws-1" }, listener);

    await connectAndOpen(client);
    socketAt(0).onmessage?.({ data: validEnvelope({ eventId: "evt-1" }) });
    expect(listener).toHaveBeenCalledTimes(1);

    client.dispose();

    socketAt(0).onmessage?.({ data: validEnvelope({ eventId: "evt-2" }) });
    expect(listener).toHaveBeenCalledTimes(1);
  });

  it("RT-014 no hard-coded realtime URL exists in production realtime source", () => {
    const sources = collectProductionSources();
    for (const file of sources) {
      const content = readFileSync(file, "utf8");
      expect(content, file).not.toMatch(/['"`](wss?:\/\/)/);
    }
  });

  it("RT-015 no console.* exists in canonical realtime source", () => {
    const sources = collectProductionSources();
    for (const file of sources) {
      const content = readFileSync(file, "utf8");
      expect(content, file).not.toMatch(/console\.(log|warn|error|info|debug)/);
    }
  });

  it("no exported global realtime singleton exists in production source", () => {
    const sources = collectProductionSources();
    for (const file of sources) {
      const content = readFileSync(file, "utf8");
      expect(content, file).not.toMatch(
        /export\s+(const|let|var)\s+realtimeClient\b/,
      );
      expect(content, file).not.toMatch(
        /export\s+(const|let|var)\s+\w*[Rr]ealtime\w*\s*=\s*new\s+RealtimeClient/,
      );
    }
  });
});

function collectProductionSources(): string[] {
  const srcDir = join(dirname(fileURLToPath(import.meta.url)), "..");
  const files: string[] = [];

  function walk(dir: string): void {
    for (const entry of readdirSync(dir)) {
      if (entry === "__tests__") continue;
      const full = join(dir, entry);
      const stat = statSync(full);
      if (stat.isDirectory()) {
        walk(full);
      } else if (full.endsWith(".ts") || full.endsWith(".tsx")) {
        files.push(full);
      }
    }
  }

  walk(srcDir);
  return files;
}
