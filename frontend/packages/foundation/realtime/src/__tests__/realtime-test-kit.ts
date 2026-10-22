import { vi } from "vitest";
import type {
  RealtimeClient,
  RealtimeConnectionDescriptor,
  WebSocketLike,
} from "../transport/realtime-client";

/** Test seams shared by the realtime foundation suites (not a test file). */

export const TEST_URL = "ws://realtime.test/api/v1/realtime";

export interface MockSocket extends WebSocketLike {
  sentFrames(): Array<Record<string, unknown>>;
}

export function createMockSocket(): MockSocket {
  const send = vi.fn();
  const socket: MockSocket = {
    readyState: 1,
    send,
    close: vi.fn(function (this: WebSocketLike) {
      if (this.onclose) this.onclose({ code: 1000, reason: "Closed" });
    }),
    onopen: null,
    onmessage: null,
    onclose: null,
    onerror: null,
    sentFrames: () =>
      send.mock.calls.map(
        ([data]) => JSON.parse(String(data)) as Record<string, unknown>,
      ),
  };
  return socket;
}

interface FakeTimer {
  callback: () => void;
  delay: number;
  cancelled: boolean;
}

export function createFakeScheduler() {
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
  const active = (list: FakeTimer[]) => list.filter((t) => !t.cancelled);
  return {
    timeouts,
    intervals,
    scheduler,
    activeTimeouts: () => active(timeouts),
    activeIntervals: () => active(intervals),
    lastDelay(): number {
      const timer = active(timeouts).at(-1);
      if (!timer) throw new Error("Expected an active timer");
      return timer.delay;
    },
    fireLastTimeout: () => {
      const timer = active(timeouts).at(-1);
      if (!timer) throw new Error("No active timer to fire");
      timer.cancelled = true;
      timer.callback();
    },
    tickInterval: () => {
      const timer = active(intervals).at(-1);
      if (!timer) throw new Error("No active interval to tick");
      timer.callback();
    },
  };
}

export function readyFrame(overrides: Record<string, unknown> = {}): string {
  return JSON.stringify({
    type: "ready",
    schemaVersion: 1,
    connectionId: "conn-1",
    pingIntervalMs: 25_000,
    idleTimeoutMs: 75_000,
    maxSubscriptions: 20,
    ...overrides,
  });
}

export function subscribedFrame(subscriptionId = "workspace:ws-1"): string {
  const [kind, id] = subscriptionId.split(":");
  return JSON.stringify({
    type: "subscribed",
    subscriptionId,
    scope: { kind, id },
    streamKey: `${kind}:${kind === "board" ? "Board" : "Workspace"}:${id}`,
  });
}

export function subscriptionErrorFrame(
  code: string,
  subscriptionId?: string,
): string {
  return JSON.stringify({ type: "subscription-error", subscriptionId, code });
}

export function eventFrame(overrides: Record<string, unknown> = {}): string {
  return JSON.stringify({
    type: "event",
    subscriptionId: "workspace:ws-1",
    schemaVersion: 1,
    eventId: "evt-1",
    eventType: "workspace.updated",
    changeKind: "updated",
    streamKey: "workspace:Workspace:ws-1",
    streamVersion: 1,
    resourceKind: "Workspace",
    resourceId: "ws-1",
    resourceVersion: 7,
    workspaceId: "ws-1",
    correlationId: "corr-1",
    occurredAt: "2026-01-01T00:00:00.000Z",
    payloadContract: "workspace.updated.v1",
    ...overrides,
  });
}

/** Sockets created by the factory, in creation order. */
export function createSocketFactory() {
  const sockets: MockSocket[] = [];
  const factory = vi.fn((_descriptor: RealtimeConnectionDescriptor) => {
    const socket = createMockSocket();
    sockets.push(socket);
    return socket;
  });
  return {
    factory,
    sockets,
    at(index: number): MockSocket {
      const socket = sockets[index];
      if (!socket) throw new Error(`Expected a socket at index ${index}`);
      return socket;
    },
    last(): MockSocket {
      return this.at(sockets.length - 1);
    },
  };
}

/** Opens the latest socket and delivers `ready` (no subscription acks). */
export async function connectAndReady(
  client: RealtimeClient,
  socketOf: () => MockSocket,
  generation = "gen-1",
): Promise<MockSocket> {
  const connectPromise = client.connect({ sessionGeneration: generation });
  await Promise.resolve();
  const socket = socketOf();
  socket.onopen?.({});
  await connectPromise;
  socket.onmessage?.({ data: readyFrame() });
  return socket;
}

/** Acknowledges every `subscribe` frame sent on the socket so far. */
export function ackSubscriptions(socket: MockSocket): void {
  for (const frame of socket.sentFrames()) {
    if (frame.type === "subscribe") {
      socket.onmessage?.({
        data: subscribedFrame(String(frame.subscriptionId)),
      });
    }
  }
}

export async function flushMicrotasks(): Promise<void> {
  for (let i = 0; i < 5; i++) await Promise.resolve();
}
