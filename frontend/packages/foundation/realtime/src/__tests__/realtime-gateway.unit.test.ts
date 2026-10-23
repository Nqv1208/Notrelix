import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  RealtimeClient,
  resolveRealtimeEndpointUrl,
  type RealtimeClientOptions,
  type RealtimeRecoverySignal,
} from "../transport/realtime-client";
import {
  TEST_URL,
  ackSubscriptions,
  connectAndReady,
  createFakeScheduler,
  createSocketFactory,
  eventFrame,
  flushMicrotasks,
  readyFrame,
  subscribedFrame,
  subscriptionErrorFrame,
  type MockSocket,
} from "./realtime-test-kit";

describe("RealtimeClient gateway protocol v1", () => {
  let kit: ReturnType<typeof createSocketFactory>;
  let sched: ReturnType<typeof createFakeScheduler>;
  let telemetry: {
    track: ReturnType<typeof vi.fn>;
    reportError: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    kit = createSocketFactory();
    sched = createFakeScheduler();
    telemetry = { track: vi.fn(), reportError: vi.fn() };
  });

  function makeClient(extra: Partial<RealtimeClientOptions> = {}) {
    return new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
      scheduler: sched.scheduler,
      telemetry,
      random: () => 1,
      ...extra,
    });
  }

  async function connect(client: RealtimeClient, generation = "gen-1") {
    return connectAndReady(client, () => kit.last(), generation);
  }

  function collectRecovery(client: RealtimeClient): RealtimeRecoverySignal[] {
    const signals: RealtimeRecoverySignal[] = [];
    client.subscribeRecovery((signal) => signals.push(signal));
    return signals;
  }

  async function reconnect(): Promise<MockSocket> {
    sched.fireLastTimeout();
    await flushMicrotasks();
    const socket = kit.last();
    socket.onopen?.({});
    socket.onmessage?.({ data: readyFrame() });
    return socket;
  }

  describe("subscribe handshake", () => {
    it("sends subscribe for each scope only after ready, and events are ignored before the ack", async () => {
      const client = makeClient();
      const listener = vi.fn();
      client.subscribe({ workspaceId: "ws-1" }, listener);

      const connectPromise = client.connect({ sessionGeneration: "gen-1" });
      await Promise.resolve();
      kit.at(0).onopen?.({});
      await connectPromise;
      expect(kit.at(0).sentFrames()).toEqual([]);

      kit.at(0).onmessage?.({ data: readyFrame() });
      expect(kit.at(0).sentFrames()).toEqual([
        {
          type: "subscribe",
          subscriptionId: "workspace:ws-1",
          scope: { kind: "workspace", id: "ws-1" },
        },
      ]);

      kit.at(0).onmessage?.({ data: eventFrame() });
      expect(listener).not.toHaveBeenCalled();

      kit.at(0).onmessage?.({ data: subscribedFrame("workspace:ws-1") });
      kit.at(0).onmessage?.({ data: eventFrame() });
      expect(listener).toHaveBeenCalledTimes(1);
    });

    it("subscribes immediately when already ready and shares one wire subscription per scope", async () => {
      const client = makeClient();
      await connect(client);

      client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      client.subscribe({ workspaceId: "ws-1", boardId: "board-1" }, vi.fn());

      const subscribes = kit
        .at(0)
        .sentFrames()
        .filter((frame) => frame.type === "subscribe");
      expect(subscribes).toEqual([
        {
          type: "subscribe",
          subscriptionId: "workspace:ws-1",
          scope: { kind: "workspace", id: "ws-1" },
        },
        {
          type: "subscribe",
          subscriptionId: "board:board-1",
          scope: { kind: "board", id: "board-1" },
        },
      ]);
    });

    it("sends unsubscribe only when the last listener of a scope leaves", async () => {
      const client = makeClient();
      await connect(client);
      const offA = client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      const offB = client.subscribe({ workspaceId: "ws-1" }, vi.fn());

      offA();
      expect(
        kit
          .at(0)
          .sentFrames()
          .filter((f) => f.type === "unsubscribe"),
      ).toEqual([]);

      offB();
      offB();
      expect(
        kit
          .at(0)
          .sentFrames()
          .filter((f) => f.type === "unsubscribe"),
      ).toEqual([{ type: "unsubscribe", subscriptionId: "workspace:ws-1" }]);
    });

    it("rejects scope ids the gateway would refuse without sending a frame", async () => {
      const client = makeClient();
      await connect(client);

      const off = client.subscribe(
        { workspaceId: "bad id/with space" },
        vi.fn(),
      );

      off();
      expect(kit.at(0).sentFrames()).toEqual([]);
      expect(telemetry.reportError).toHaveBeenCalledTimes(1);
    });

    it("emits a subscribed recovery signal on every ack so the host fetches authoritative state", async () => {
      const client = makeClient();
      const signals = collectRecovery(client);
      client.subscribe({ workspaceId: "ws-1" }, vi.fn());

      const first = await connect(client);
      ackSubscriptions(first);
      expect(signals).toHaveLength(1);
      expect(signals[0]).toMatchObject({
        reason: "subscribed",
        workspaceId: "ws-1",
        subscriptionId: "workspace:ws-1",
        scope: { kind: "workspace", id: "ws-1" },
        streamKey: "workspace:Workspace:ws-1",
      });

      first.onclose?.({ code: 1006, reason: "" });
      const second = await reconnect();
      ackSubscriptions(second);
      expect(signals.map((s) => s.reason)).toEqual([
        "subscribed",
        "subscribed",
      ]);
    });
  });

  describe("per-stream checkpoint", () => {
    async function ready(client: RealtimeClient) {
      const listener = vi.fn();
      const signals = collectRecovery(client);
      client.subscribe({ workspaceId: "ws-1" }, listener);
      const socket = await connect(client);
      ackSubscriptions(socket);
      signals.length = 0; // drop the subscribed signal
      return { socket, listener, signals };
    }

    it("first event after subscribe sets the checkpoint and is dispatched", async () => {
      const client = makeClient();
      const { socket, listener, signals } = await ready(client);

      socket.onmessage?.({
        data: eventFrame({ eventId: "e1", streamVersion: 41 }),
      });

      expect(listener).toHaveBeenCalledTimes(1);
      expect(signals).toEqual([]);
    });

    it("accepts consecutive +1 versions without recovery", async () => {
      const client = makeClient();
      const { socket, listener, signals } = await ready(client);

      for (const version of [1, 2, 3]) {
        socket.onmessage?.({
          data: eventFrame({ eventId: `e${version}`, streamVersion: version }),
        });
      }

      expect(listener).toHaveBeenCalledTimes(3);
      expect(signals).toEqual([]);
    });

    it("a gap triggers recovery, withholds the frame, and rebases the checkpoint", async () => {
      const client = makeClient();
      const { socket, listener, signals } = await ready(client);

      socket.onmessage?.({
        data: eventFrame({ eventId: "e1", streamVersion: 1 }),
      });
      socket.onmessage?.({
        data: eventFrame({ eventId: "e4", streamVersion: 4 }),
      });

      expect(listener).toHaveBeenCalledTimes(1);
      expect(signals).toHaveLength(1);
      expect(signals[0]).toMatchObject({
        reason: "gap",
        expected: 2,
        received: 4,
        streamKey: "workspace:Workspace:ws-1",
        workspaceId: "ws-1",
      });

      // Continuation: the next +1 event is consumed exactly once.
      socket.onmessage?.({
        data: eventFrame({ eventId: "e5", streamVersion: 5 }),
      });
      expect(listener).toHaveBeenCalledTimes(2);
      expect(signals).toHaveLength(1);
    });

    it("ignores a repeated frame with the same eventId without recovery", async () => {
      const client = makeClient();
      const { socket, listener, signals } = await ready(client);

      socket.onmessage?.({
        data: eventFrame({ eventId: "e1", streamVersion: 1 }),
      });
      socket.onmessage?.({
        data: eventFrame({ eventId: "e1", streamVersion: 1 }),
      });

      expect(listener).toHaveBeenCalledTimes(1);
      expect(signals).toEqual([]);
    });

    it("a different event re-using the checkpoint version recovers as duplicate", async () => {
      const client = makeClient();
      const { socket, listener, signals } = await ready(client);

      socket.onmessage?.({
        data: eventFrame({ eventId: "e1", streamVersion: 5 }),
      });
      socket.onmessage?.({
        data: eventFrame({ eventId: "e2", streamVersion: 5 }),
      });

      expect(listener).toHaveBeenCalledTimes(1);
      expect(signals.map((s) => s.reason)).toEqual(["duplicate"]);
    });

    it("a version that goes backwards recovers as reset and rebases", async () => {
      const client = makeClient();
      const { socket, listener, signals } = await ready(client);

      socket.onmessage?.({
        data: eventFrame({ eventId: "e1", streamVersion: 9 }),
      });
      socket.onmessage?.({
        data: eventFrame({ eventId: "e2", streamVersion: 2 }),
      });
      socket.onmessage?.({
        data: eventFrame({ eventId: "e3", streamVersion: 3 }),
      });

      expect(signals.map((s) => s.reason)).toEqual(["reset"]);
      expect(listener).toHaveBeenCalledTimes(2);
    });

    it("tracks streams independently per subscription and streamKey", async () => {
      const client = makeClient();
      const wsListener = vi.fn();
      const boardListener = vi.fn();
      const signals = collectRecovery(client);
      client.subscribe({ workspaceId: "ws-1" }, wsListener);
      client.subscribe(
        { workspaceId: "ws-1", boardId: "board-1" },
        boardListener,
      );
      const socket = await connect(client);
      ackSubscriptions(socket);
      signals.length = 0;

      socket.onmessage?.({
        data: eventFrame({ eventId: "w1", streamVersion: 1 }),
      });
      socket.onmessage?.({
        data: eventFrame({
          eventId: "b7",
          subscriptionId: "board:board-1",
          streamKey: "board:Board:board-1",
          streamVersion: 7,
        }),
      });
      socket.onmessage?.({
        data: eventFrame({ eventId: "w2", streamVersion: 2 }),
      });
      socket.onmessage?.({
        data: eventFrame({
          eventId: "b8",
          subscriptionId: "board:board-1",
          streamKey: "board:Board:board-1",
          streamVersion: 8,
        }),
      });

      expect(signals).toEqual([]);
      expect(wsListener).toHaveBeenCalledTimes(2);
      expect(boardListener).toHaveBeenCalledTimes(2);
    });

    it("resets checkpoints on resubscribe so the first event sets a new one", async () => {
      const client = makeClient();
      const { socket, listener, signals } = await ready(client);
      socket.onmessage?.({
        data: eventFrame({ eventId: "e1", streamVersion: 50 }),
      });

      socket.onclose?.({ code: 1006, reason: "" });
      const next = await reconnect();
      ackSubscriptions(next);
      signals.length = 0;

      next.onmessage?.({
        data: eventFrame({ eventId: "e2", streamVersion: 3 }),
      });

      expect(signals).toEqual([]);
      expect(listener).toHaveBeenCalledTimes(2);
    });

    it("never uses resourceVersion for ordering", async () => {
      const client = makeClient();
      const { socket, listener, signals } = await ready(client);

      socket.onmessage?.({
        data: eventFrame({
          eventId: "e1",
          streamVersion: 1,
          resourceVersion: 100,
        }),
      });
      socket.onmessage?.({
        data: eventFrame({
          eventId: "e2",
          streamVersion: 2,
          resourceVersion: 3,
        }),
      });

      expect(listener).toHaveBeenCalledTimes(2);
      expect(signals).toEqual([]);
    });

    it("a listener failure does not stop other listeners", async () => {
      const client = makeClient();
      const bad = vi.fn(() => {
        throw new Error("boom");
      });
      const good = vi.fn();
      client.subscribe({ workspaceId: "ws-1" }, bad);
      client.subscribe({ workspaceId: "ws-1" }, good);
      const socket = await connect(client);
      ackSubscriptions(socket);

      socket.onmessage?.({ data: eventFrame() });

      expect(good).toHaveBeenCalledTimes(1);
      expect(telemetry.reportError).toHaveBeenCalledTimes(1);
    });
  });

  describe("scope transitions (NRX-014)", () => {
    it("a late event of the previous workspace is ignored after unsubscribe", async () => {
      const client = makeClient();
      const signals = collectRecovery(client);
      const listenerA = vi.fn();
      const offA = client.subscribe({ workspaceId: "ws-a" }, listenerA);
      const socket = await connect(client);
      ackSubscriptions(socket);

      offA();
      const listenerB = vi.fn();
      client.subscribe({ workspaceId: "ws-b" }, listenerB);
      ackSubscriptions(socket);
      signals.length = 0;

      socket.onmessage?.({
        data: eventFrame({
          eventId: "late-a",
          subscriptionId: "workspace:ws-a",
          workspaceId: "ws-a",
          streamKey: "workspace:Workspace:ws-a",
        }),
      });

      expect(listenerA).not.toHaveBeenCalled();
      expect(listenerB).not.toHaveBeenCalled();
      expect(signals).toEqual([]);
    });

    it("never delivers a frame whose workspaceId differs from the subscribed scope", async () => {
      const client = makeClient();
      const listener = vi.fn();
      client.subscribe({ workspaceId: "ws-1" }, listener);
      const socket = await connect(client);
      ackSubscriptions(socket);

      socket.onmessage?.({
        data: eventFrame({ eventId: "foreign", workspaceId: "ws-other" }),
      });

      expect(listener).not.toHaveBeenCalled();
    });

    it("a new session generation replaces the socket and ignores late frames of the old one", async () => {
      const client = makeClient();
      const listener = vi.fn();
      client.subscribe({ workspaceId: "ws-1" }, listener);
      const first = await connect(client, "user:1");
      ackSubscriptions(first);

      await connect(client, "user:2");
      expect(first.close).toHaveBeenCalledTimes(1);
      expect(kit.factory).toHaveBeenCalledTimes(2);

      first.onmessage?.({ data: eventFrame({ eventId: "old-principal" }) });
      first.onclose?.({ code: 1000, reason: "late" });

      expect(listener).not.toHaveBeenCalled();
      expect(client.getState()).toBe("connected");
    });

    it("disconnect keeps listener subscriptions and re-subscribes on the next connect", async () => {
      const client = makeClient();
      client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      const first = await connect(client);
      ackSubscriptions(first);

      client.disconnect("workspace-switch");
      const second = await connect(client);

      expect(
        second.sentFrames().filter((f) => f.type === "subscribe"),
      ).toHaveLength(1);
    });
  });

  describe("subscription errors", () => {
    it("subscription_revoked is permission loss: signal once, no resubscribe on reconnect", async () => {
      const client = makeClient();
      const signals = collectRecovery(client);
      client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      const socket = await connect(client);
      ackSubscriptions(socket);
      signals.length = 0;

      socket.onmessage?.({
        data: subscriptionErrorFrame("subscription_revoked", "workspace:ws-1"),
      });
      expect(signals).toHaveLength(1);
      expect(signals[0]).toMatchObject({
        reason: "permission-lost",
        code: "subscription_revoked",
        workspaceId: "ws-1",
      });

      socket.onclose?.({ code: 1006, reason: "" });
      const next = await reconnect();
      expect(next.sentFrames().filter((f) => f.type === "subscribe")).toEqual(
        [],
      );
      expect(signals).toHaveLength(1);
    });

    it("subscription_denied at subscribe time is permission loss and never retried", async () => {
      const client = makeClient();
      const signals = collectRecovery(client);
      client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      const socket = await connect(client);

      socket.onmessage?.({
        data: subscriptionErrorFrame("subscription_denied", "workspace:ws-1"),
      });

      expect(signals.map((s) => s.reason)).toEqual(["permission-lost"]);
      expect(sched.activeTimeouts()).toEqual([]);
    });

    it("a permission-lost scope is re-subscribed for a new principal", async () => {
      const client = makeClient();
      client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      const first = await connect(client, "user:1");
      first.onmessage?.({
        data: subscriptionErrorFrame("subscription_denied", "workspace:ws-1"),
      });

      const second = await connect(client, "user:2");

      expect(
        second.sentFrames().filter((f) => f.type === "subscribe"),
      ).toHaveLength(1);
    });

    it("authorization_unavailable retries with backoff and then gives up without a loop", async () => {
      const client = makeClient();
      const signals = collectRecovery(client);
      client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      const socket = await connect(client);
      const subscribeCount = () =>
        socket.sentFrames().filter((f) => f.type === "subscribe").length;
      expect(subscribeCount()).toBe(1);

      for (let attempt = 1; attempt <= 5; attempt++) {
        socket.onmessage?.({
          data: subscriptionErrorFrame(
            "authorization_unavailable",
            "workspace:ws-1",
          ),
        });
        expect(sched.lastDelay()).toBe(1000 * 2 ** (attempt - 1));
        sched.fireLastTimeout();
        expect(subscribeCount()).toBe(1 + attempt);
      }

      socket.onmessage?.({
        data: subscriptionErrorFrame(
          "authorization_unavailable",
          "workspace:ws-1",
        ),
      });
      expect(sched.activeTimeouts()).toEqual([]);
      expect(signals.map((s) => s.reason)).toEqual(["subscription-failed"]);
    });

    it("non-retryable codes mark the subscription failed and surface a failure signal", async () => {
      const client = makeClient();
      const signals = collectRecovery(client);
      client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      const socket = await connect(client);

      socket.onmessage?.({
        data: subscriptionErrorFrame("subscription_limit", "workspace:ws-1"),
      });

      expect(signals).toHaveLength(1);
      expect(signals[0]).toMatchObject({
        reason: "subscription-failed",
        code: "subscription_limit",
      });
      expect(sched.activeTimeouts()).toEqual([]);
    });

    it("unknown_subscription and errors without a subscription are only observed", async () => {
      const client = makeClient();
      const signals = collectRecovery(client);
      client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      const socket = await connect(client);

      socket.onmessage?.({
        data: subscriptionErrorFrame("unknown_subscription", "workspace:ws-1"),
      });
      socket.onmessage?.({ data: subscriptionErrorFrame("invalid_message") });

      expect(signals).toEqual([]);
      expect(telemetry.track).toHaveBeenCalledWith(
        "realtime.subscription_error",
        expect.objectContaining({ code: "invalid_message" }),
      );
    });
  });

  describe("heartbeat", () => {
    it("pings on the configured interval and answers server pings with pong", async () => {
      const client = makeClient({ heartbeat: { intervalMs: 20_000 } });
      const socket = await connect(client);

      expect(sched.activeIntervals()).toHaveLength(1);
      expect(sched.activeIntervals()[0]?.delay).toBe(20_000);

      sched.tickInterval();
      const ping = socket.sentFrames().find((f) => f.type === "ping");
      expect(ping).toMatchObject({ type: "ping" });
      expect(typeof ping?.sentAt).toBe("string");

      socket.onmessage?.({
        data: JSON.stringify({ type: "ping", sentAt: "2026-01-01T00:00:00Z" }),
      });
      expect(socket.sentFrames().at(-1)).toEqual({ type: "pong" });
    });

    it("keeps the default ping period below the 75s idle timeout", async () => {
      const client = makeClient();
      await connect(client);

      const delay = sched.activeIntervals()[0]?.delay ?? Infinity;
      expect(delay).toBeLessThan(75_000);
    });

    it("tightens the ping period to a third of the advertised idle timeout", async () => {
      const client = makeClient({ heartbeat: { intervalMs: 60_000 } });
      const connectPromise = client.connect({ sessionGeneration: "gen-1" });
      await Promise.resolve();
      kit.at(0).onopen?.({});
      await connectPromise;

      kit.at(0).onmessage?.({ data: readyFrame({ idleTimeoutMs: 30_000 }) });

      expect(sched.activeIntervals()).toHaveLength(1);
      expect(sched.activeIntervals()[0]?.delay).toBe(10_000);
    });

    it("closes with 4000 after the maximum number of missed pongs and then reconnects", async () => {
      const client = makeClient({
        heartbeat: {
          intervalMs: 10_000,
          pongTimeoutMs: 2_000,
          maximumMissedPongs: 2,
        },
      });
      const socket = await connect(client);

      sched.tickInterval();
      sched.fireLastTimeout(); // pong timeout 1
      expect(socket.close).not.toHaveBeenCalled();

      sched.tickInterval();
      sched.fireLastTimeout(); // pong timeout 2
      expect(socket.close).toHaveBeenCalledWith(4000, "Heartbeat timeout");
    });

    it("a pong clears the pending timeout and the miss counter", async () => {
      const client = makeClient({
        heartbeat: {
          intervalMs: 10_000,
          pongTimeoutMs: 2_000,
          maximumMissedPongs: 2,
        },
      });
      const socket = await connect(client);

      sched.tickInterval();
      sched.fireLastTimeout(); // miss 1
      sched.tickInterval();
      socket.onmessage?.({
        data: JSON.stringify({ type: "pong", sentAt: "2026-01-01T00:00:00Z" }),
      });
      sched.tickInterval();
      sched.fireLastTimeout(); // miss 1 again, not 2

      expect(socket.close).not.toHaveBeenCalled();
    });

    it("stops the heartbeat when the socket closes", async () => {
      const client = makeClient();
      const socket = await connect(client);

      socket.onclose?.({ code: 1006, reason: "" });

      expect(sched.activeIntervals()).toEqual([]);
    });
  });

  describe("close codes", () => {
    it.each([1000, 1001, 1006, 1013, 4408, 4000])(
      "code %i reconnects with jittered backoff and re-subscribes",
      async (code) => {
        const client = makeClient();
        client.subscribe({ workspaceId: "ws-1" }, vi.fn());
        const first = await connect(client);
        ackSubscriptions(first);

        first.onclose?.({ code, reason: "" });
        expect(client.getState()).toBe("reconnecting");
        expect(sched.lastDelay()).toBe(1000);

        const next = await reconnect();
        expect(client.getState()).toBe("connected");
        expect(
          next.sentFrames().filter((f) => f.type === "subscribe"),
        ).toHaveLength(1);
      },
    );

    it("applies equal jitter between half and the full backoff delay", async () => {
      const lower = makeClient({ random: () => 0 });
      const first = await connect(lower);
      first.onclose?.({ code: 1006, reason: "" });
      expect(sched.lastDelay()).toBe(500);
    });

    it.each([1003, 1008, 1009])(
      "code %i is a client protocol fault: failed, no reconnect",
      async (code) => {
        const client = makeClient();
        const socket = await connect(client);

        socket.onclose?.({ code, reason: "" });

        expect(client.getState()).toBe("failed");
        expect(sched.activeTimeouts()).toEqual([]);
        expect(telemetry.reportError).toHaveBeenCalledWith(
          expect.any(Error),
          expect.objectContaining({ context: "nonRetryableClose", code }),
        );
      },
    );

    it("4401 refreshes the session and then reconnects immediately", async () => {
      const refreshSession = vi.fn().mockResolvedValue(undefined);
      const client = makeClient({ refreshSession });
      client.subscribe({ workspaceId: "ws-1" }, vi.fn());
      const first = await connect(client);
      ackSubscriptions(first);

      first.onclose?.({ code: 4401, reason: "expired" });
      expect(refreshSession).toHaveBeenCalledTimes(1);
      expect(kit.factory).toHaveBeenCalledTimes(1);

      await flushMicrotasks();
      expect(kit.factory).toHaveBeenCalledTimes(2);
      const next = kit.at(1);
      next.onopen?.({});
      next.onmessage?.({ data: readyFrame() });
      expect(
        next.sentFrames().filter((f) => f.type === "subscribe"),
      ).toHaveLength(1);
      expect(client.getState()).toBe("connected");
    });

    it("4401 with a rejected refresh ends in failed without reconnecting", async () => {
      const refreshSession = vi
        .fn()
        .mockRejectedValue(new Error("refresh rejected"));
      const client = makeClient({ refreshSession });
      const socket = await connect(client);

      socket.onclose?.({ code: 4401, reason: "expired" });
      await flushMicrotasks();

      expect(client.getState()).toBe("failed");
      expect(kit.factory).toHaveBeenCalledTimes(1);
      expect(sched.activeTimeouts()).toEqual([]);
      expect(telemetry.reportError).toHaveBeenCalledWith(
        expect.any(Error),
        expect.objectContaining({ context: "sessionRefresh" }),
      );
    });

    it("4401 without a refresh capability ends in failed", async () => {
      const client = makeClient();
      const socket = await connect(client);

      socket.onclose?.({ code: 4401, reason: "expired" });

      expect(client.getState()).toBe("failed");
      expect(sched.activeTimeouts()).toEqual([]);
    });

    it("repeated 4401 without reaching ready stops after the bounded attempts", async () => {
      const refreshSession = vi.fn().mockResolvedValue(undefined);
      const client = makeClient({ refreshSession });
      const first = await connect(client);

      first.onclose?.({ code: 4401, reason: "" });
      await flushMicrotasks();
      kit.at(1).onopen?.({});
      kit.at(1).onclose?.({ code: 4401, reason: "" });
      await flushMicrotasks();
      kit.at(2).onopen?.({});
      kit.at(2).onclose?.({ code: 4401, reason: "" });
      await flushMicrotasks();

      expect(refreshSession).toHaveBeenCalledTimes(2);
      expect(client.getState()).toBe("failed");
      expect(kit.factory).toHaveBeenCalledTimes(3);
    });

    it("a manual disconnect while the refresh is in flight does not reconnect", async () => {
      let resolveRefresh: () => void = () => {};
      const refreshSession = vi.fn(
        () => new Promise<void>((resolve) => (resolveRefresh = resolve)),
      );
      const client = makeClient({ refreshSession });
      const socket = await connect(client);

      socket.onclose?.({ code: 4401, reason: "" });
      client.disconnect("logout");
      resolveRefresh();
      await flushMicrotasks();

      expect(kit.factory).toHaveBeenCalledTimes(1);
      expect(client.getState()).toBe("closed");
    });

    it("a close of a replaced socket does not disturb the current connection", async () => {
      const client = makeClient();
      const first = await connect(client);
      client.disconnect();
      const second = await connect(client);

      first.onclose?.({ code: 1006, reason: "late" });

      expect(client.getState()).toBe("connected");
      expect(second.close).not.toHaveBeenCalled();
      expect(sched.activeTimeouts()).toEqual([]);
    });
  });
});

describe("resolveRealtimeEndpointUrl", () => {
  it("appends the gateway path to a bare origin", () => {
    expect(resolveRealtimeEndpointUrl("wss://api.example.test")).toBe(
      "wss://api.example.test/api/v1/realtime",
    );
    expect(resolveRealtimeEndpointUrl("ws://localhost:5000/")).toBe(
      "ws://localhost:5000/api/v1/realtime",
    );
  });

  it("keeps a URL that already carries an explicit path", () => {
    expect(resolveRealtimeEndpointUrl("wss://api.example.test/realtime")).toBe(
      "wss://api.example.test/realtime",
    );
    expect(
      resolveRealtimeEndpointUrl("wss://api.example.test/api/v1/realtime"),
    ).toBe("wss://api.example.test/api/v1/realtime");
  });

  it("returns non-URL values unchanged for the socket factory to reject", () => {
    expect(resolveRealtimeEndpointUrl("not a url")).toBe("not a url");
  });
});
