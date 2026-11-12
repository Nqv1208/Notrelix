import { afterEach, beforeEach, describe, expect, it } from "vitest";
import type { RealtimeEphemeralEvent } from "@notrelix/realtime";
import {
  MockRealtimeTransport,
  resetMockEphemeral,
  setMockEphemeralClock,
  simulateMockPresence,
  simulateMockTyping,
} from "../realtime/mock-realtime-transport";

const WORKSPACE = "ws-1";
const CHANNEL = "space-1";

describe("MockRealtimeTransport typing and presence", () => {
  let now = 1_000_000;
  const transports: MockRealtimeTransport[] = [];

  function create(viewerId: string): MockRealtimeTransport {
    const transport = new MockRealtimeTransport({ viewerId });
    transports.push(transport);
    return transport;
  }

  beforeEach(() => {
    now = 1_000_000;
    resetMockEphemeral();
    setMockEphemeralClock(() => now);
  });

  afterEach(() => {
    for (const transport of transports.splice(0)) transport.dispose();
    setMockEphemeralClock(null);
    resetMockEphemeral();
  });

  function listen(transport: MockRealtimeTransport): RealtimeEphemeralEvent[] {
    const seen: RealtimeEphemeralEvent[] = [];
    transport.subscribeEphemeral(
      { workspaceId: WORKSPACE, channelId: CHANNEL },
      (event) => seen.push(event),
    );
    return seen;
  }

  it("delivers a typing frame to the other personas and never to the sender", () => {
    const alice = create("alice");
    const bob = create("bob");
    const aliceSeen = listen(alice);
    const bobSeen = listen(bob);

    alice.sendTyping(CHANNEL);

    expect(bobSeen).toEqual([
      {
        kind: "typing",
        channelId: CHANNEL,
        userId: "alice",
        expiresInMs: 5000,
      },
    ]);
    expect(aliceSeen).toEqual([]);
  });

  it("drops typing frames faster than the gateway rate limit and accepts the next after it", () => {
    const alice = create("alice");
    const bob = create("bob");
    const bobSeen = listen(bob);

    alice.sendTyping(CHANNEL);
    now += 500;
    alice.sendTyping(CHANNEL);
    expect(bobSeen).toHaveLength(1);

    now += 2_000;
    alice.sendTyping(CHANNEL);
    expect(bobSeen).toHaveLength(2);
  });

  it("emits persona typing to every live transport, with the same rate limit", () => {
    const bob = create("bob");
    const bobSeen = listen(bob);

    expect(simulateMockTyping(CHANNEL, "carol")).toBe(true);
    expect(simulateMockTyping(CHANNEL, "carol")).toBe(false);

    expect(bobSeen.map((e) => e.userId)).toEqual(["carol"]);
  });

  it("tells a new subscriber who is here, and forgets personas after the presence ttl", () => {
    simulateMockPresence([], "carol", "online");

    const bob = create("bob");
    expect(listen(bob)).toEqual([
      {
        kind: "presence",
        channelId: CHANNEL,
        userId: "carol",
        state: "online",
        ttlMs: 75_000,
      },
    ]);

    now += 76_000;
    const late = create("dave");
    expect(listen(late)).toEqual([]);
  });

  it("drops an unchanged presence state sooner than 5 s and accepts a changed one", () => {
    const bob = create("bob");
    const bobSeen = listen(bob);

    expect(simulateMockPresence([CHANNEL], "carol", "online")).toBe(true);
    now += 1_000;
    expect(simulateMockPresence([CHANNEL], "carol", "online")).toBe(false);
    expect(simulateMockPresence([CHANNEL], "carol", "idle")).toBe(true);

    expect(bobSeen.map((e) => (e.kind === "presence" ? e.state : "x"))).toEqual(
      ["online", "idle"],
    );
  });

  it("stops delivering after unsubscribe and dispose", () => {
    const bob = create("bob");
    const seen: RealtimeEphemeralEvent[] = [];
    const stop = bob.subscribeEphemeral(
      { workspaceId: WORKSPACE, channelId: CHANNEL },
      (event) => seen.push(event),
    );
    stop();
    simulateMockTyping(CHANNEL, "carol");
    expect(seen).toEqual([]);
  });
});
