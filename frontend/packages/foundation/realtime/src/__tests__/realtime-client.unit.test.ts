import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { RealtimeClient, transitionState } from "../transport/realtime-client";
import {
  TEST_URL,
  ackSubscriptions,
  connectAndReady,
  createFakeScheduler,
  createSocketFactory,
  eventFrame,
  readyFrame,
} from "./realtime-test-kit";

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
  let kit: ReturnType<typeof createSocketFactory>;

  beforeEach(() => {
    kit = createSocketFactory();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  /** Opens, delivers `ready` and acknowledges every subscribe frame. */
  async function connectAndOpen(client: RealtimeClient) {
    const socket = await connectAndReady(client, () => kit.last());
    ackSubscriptions(socket);
    return socket;
  }

  it("RT-001 constructor does not access global WebSocket", async () => {
    vi.stubGlobal("WebSocket", undefined);

    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
    });
    await connectAndOpen(client);

    expect(client.getState()).toBe("connected");
    expect(kit.factory).toHaveBeenCalledTimes(1);
  });

  it("RT-002 connect() creates exactly one socket with the configured URL", async () => {
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
    });

    await connectAndOpen(client);

    expect(kit.factory).toHaveBeenCalledTimes(1);
    expect(kit.factory.mock.calls[0]?.[0]).toMatchObject({ url: TEST_URL });
  });

  it("RT-003 second connect() of the same session while connecting/open does not create another socket", async () => {
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
    });

    const first = client.connect({ sessionGeneration: "gen-1" });
    await client.connect({ sessionGeneration: "gen-1" });
    await Promise.resolve();
    kit.at(0).onopen?.({});
    await first;
    await client.connect({ sessionGeneration: "gen-1" });

    expect(kit.factory).toHaveBeenCalledTimes(1);
  });

  it("RT-004 valid incoming message is parsed and dispatched through the subscription API", async () => {
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
    });
    const listener = vi.fn();
    client.subscribe({ workspaceId: "ws-1" }, listener);

    await connectAndOpen(client);
    kit.at(0).onmessage?.({ data: eventFrame() });

    expect(listener).toHaveBeenCalledTimes(1);
    expect(listener.mock.calls[0]?.[0]).toMatchObject({
      eventId: "evt-1",
      workspaceId: "ws-1",
      timestamp: "2026-01-01T00:00:00.000Z",
      aggregateId: "ws-1",
    });
  });

  it("RT-005 malformed message calls the error port and does not crash the message loop", async () => {
    const telemetry = { track: vi.fn(), reportError: vi.fn() };
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
      telemetry,
    });
    const listener = vi.fn();
    client.subscribe({ workspaceId: "ws-1" }, listener);

    await connectAndOpen(client);
    kit.at(0).onmessage?.({ data: "{not-valid-json" });
    kit.at(0).onmessage?.({ data: eventFrame() });

    expect(telemetry.reportError).toHaveBeenCalledTimes(1);
    expect(listener).toHaveBeenCalledTimes(1);
  });

  it("RT-006 network close schedules exactly one reconnect", async () => {
    const { scheduler, activeTimeouts } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
      scheduler,
    });

    await connectAndOpen(client);
    const timersBefore = activeTimeouts().length;
    kit.at(0).onclose?.({ code: 1006, reason: "network" });

    expect(activeTimeouts().length).toBe(timersBefore + 1);
    expect(client.getState()).toBe("reconnecting");
  });

  it("RT-007 reconnect delay doubles from 1s and is capped at 30s (jitter pinned to its upper bound)", async () => {
    const { scheduler, lastDelay, fireLastTimeout } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
      scheduler,
      random: () => 1,
    });

    await connectAndOpen(client);

    const reconnectDelays: number[] = [];
    for (let attempt = 0; attempt < 7; attempt++) {
      kit.last().onclose?.({ code: 1006, reason: "network" });
      reconnectDelays.push(lastDelay());
      fireLastTimeout();
      await Promise.resolve();
      await Promise.resolve();
    }

    expect(reconnectDelays).toEqual([
      1000, 2000, 4000, 8000, 16000, 30000, 30000,
    ]);
  });

  it("RT-008 gateway ready resets the reconnect attempt counter", async () => {
    const { scheduler, lastDelay, fireLastTimeout } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
      scheduler,
      random: () => 1,
    });

    await connectAndOpen(client);

    kit.at(0).onclose?.({ code: 1006, reason: "network" });
    expect(lastDelay()).toBe(1000);

    fireLastTimeout();
    await Promise.resolve();
    await Promise.resolve();
    kit.at(1).onopen?.({});
    expect(client.getState()).toBe("connected");
    kit.at(1).onmessage?.({ data: readyFrame() });

    kit.at(1).onclose?.({ code: 1006, reason: "network" });
    expect(lastDelay()).toBe(1000);
  });

  it("RT-009 manual disconnect() cancels a pending reconnect", async () => {
    const { scheduler, activeTimeouts } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
      scheduler,
    });

    await connectAndOpen(client);
    kit.at(0).onclose?.({ code: 1006, reason: "network" });
    expect(activeTimeouts().length).toBeGreaterThan(0);

    client.disconnect();

    expect(activeTimeouts().length).toBe(0);
  });

  it("RT-010 manual disconnect() closes the socket exactly once", async () => {
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
    });

    await connectAndOpen(client);
    client.disconnect();
    client.disconnect();

    expect(kit.at(0).close).toHaveBeenCalledTimes(1);
    expect(client.getState()).toBe("closed");
  });

  it("RT-011 dispose() cancels timers, closes the socket and is idempotent", async () => {
    const { scheduler, activeTimeouts } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
      scheduler,
    });

    await connectAndOpen(client);
    client.dispose();
    client.dispose();

    expect(kit.at(0).close).toHaveBeenCalledTimes(1);
    expect(activeTimeouts().length).toBe(0);
    expect(client.getState()).toBe("closed");
  });

  it("RT-012 no reconnect happens after dispose", async () => {
    const { scheduler, activeTimeouts } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
      scheduler,
    });

    await connectAndOpen(client);
    kit.at(0).onclose?.({ code: 1006, reason: "network" });
    client.dispose();

    expect(activeTimeouts().length).toBe(0);
    await client.connect({ sessionGeneration: "gen-2" });
    await Promise.resolve();
    expect(kit.factory).toHaveBeenCalledTimes(1);
  });

  it("RT-013 subscriptions survive network reconnect and are re-established", async () => {
    const { scheduler, fireLastTimeout } = createFakeScheduler();
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
      scheduler,
    });
    const listener = vi.fn();
    client.subscribe({ workspaceId: "ws-1" }, listener);

    await connectAndOpen(client);
    kit.at(0).onclose?.({ code: 1006, reason: "network" });
    fireLastTimeout();
    await Promise.resolve();
    await Promise.resolve();
    kit.at(1).onopen?.({});
    kit.at(1).onmessage?.({ data: readyFrame() });
    ackSubscriptions(kit.at(1));

    kit.at(1).onmessage?.({
      data: eventFrame({ eventId: "evt-after-reconnect" }),
    });
    expect(listener).toHaveBeenCalledTimes(1);
    expect(kit.factory).toHaveBeenCalledTimes(2);
  });

  it("FND-045 unsubscribe detaches the listener without affecting other subscribers", async () => {
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
    });
    const listenerA = vi.fn();
    const listenerB = vi.fn();
    const unsubscribe = client.subscribe({ workspaceId: "ws-1" }, listenerA);
    client.subscribe({ workspaceId: "ws-1" }, listenerB);

    await connectAndOpen(client);

    kit.at(0).onmessage?.({
      data: eventFrame({ eventId: "evt-1", streamVersion: 1 }),
    });
    expect(listenerA).toHaveBeenCalledTimes(1);
    expect(listenerB).toHaveBeenCalledTimes(1);

    unsubscribe();
    kit.at(0).onmessage?.({
      data: eventFrame({ eventId: "evt-2", streamVersion: 2 }),
    });

    expect(listenerA).toHaveBeenCalledTimes(1);
    expect(listenerB).toHaveBeenCalledTimes(2);
  });

  it("FND-045 dispose detaches all subscribers so no listener fires afterwards", async () => {
    const client = new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
    });
    const listener = vi.fn();
    client.subscribe({ workspaceId: "ws-1" }, listener);

    await connectAndOpen(client);
    kit.at(0).onmessage?.({
      data: eventFrame({ eventId: "evt-1", streamVersion: 1 }),
    });
    expect(listener).toHaveBeenCalledTimes(1);

    client.dispose();

    kit.at(0).onmessage?.({
      data: eventFrame({ eventId: "evt-2", streamVersion: 2 }),
    });
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
