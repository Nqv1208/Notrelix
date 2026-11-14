import { beforeEach, describe, expect, it, vi } from "vitest";
import { parseRealtimeMessage } from "../protocol";
import { RealtimeClient } from "../transport/realtime-client";
import {
  TEST_URL,
  connectAndReady,
  createFakeScheduler,
  createSocketFactory,
} from "./realtime-test-kit";

const channelAck = JSON.stringify({
  type: "subscribed",
  subscriptionId: "channel:space-1",
  scope: { kind: "channel", id: "space-1" },
  streamKey: "chat:Channel:space-1",
});

describe("typing and presence frames (protocol v1.1, ephemeral)", () => {
  let kit: ReturnType<typeof createSocketFactory>;

  beforeEach(() => {
    kit = createSocketFactory();
  });

  function makeClient() {
    return new RealtimeClient(TEST_URL, {
      socketFactory: kit.factory,
      scheduler: createFakeScheduler().scheduler,
      telemetry: { track: vi.fn(), reportError: vi.fn() },
      random: () => 1,
    });
  }

  it("parses a typing frame and a presence frame", () => {
    expect(
      parseRealtimeMessage({
        type: "typing",
        subscriptionId: "channel:s",
        userId: "u",
        expiresInMs: 5000,
      }),
    ).toEqual({
      ok: true,
      value: {
        kind: "ephemeral",
        frame: {
          type: "typing",
          subscriptionId: "channel:s",
          userId: "u",
          expiresInMs: 5000,
        },
      },
    });
    expect(
      parseRealtimeMessage({
        type: "presence",
        subscriptionId: "channel:s",
        userId: "u",
        state: "idle",
        ttlMs: 75000,
      }).ok,
    ).toBe(true);
  });

  it("rejects malformed ephemeral frames", () => {
    for (const frame of [
      { type: "typing", subscriptionId: "c", userId: "u" },
      { type: "typing", userId: "u", expiresInMs: 1 },
      {
        type: "presence",
        subscriptionId: "c",
        userId: "u",
        state: "busy",
        ttlMs: 1,
      },
      { type: "presence", subscriptionId: "c", userId: "u", state: "online" },
    ]) {
      expect(parseRealtimeMessage(frame).ok).toBe(false);
    }
  });

  it("delivers the other people's signals to the listeners of the channel only", async () => {
    const client = makeClient();
    const heard = vi.fn();
    const other = vi.fn();
    client.subscribeEphemeral(
      { workspaceId: "ws-1", channelId: "space-1" },
      heard,
    );
    client.subscribeEphemeral(
      { workspaceId: "ws-1", channelId: "space-2" },
      other,
    );
    const socket = await connectAndReady(client, () => kit.last());
    socket.onmessage?.({ data: channelAck });

    socket.onmessage?.({
      data: JSON.stringify({
        type: "typing",
        subscriptionId: "channel:space-1",
        userId: "bob",
        expiresInMs: 5000,
      }),
    });
    socket.onmessage?.({
      data: JSON.stringify({
        type: "presence",
        subscriptionId: "channel:space-1",
        userId: "bob",
        state: "online",
        ttlMs: 75000,
      }),
    });

    expect(heard.mock.calls.map(([event]) => event)).toEqual([
      {
        kind: "typing",
        channelId: "space-1",
        userId: "bob",
        expiresInMs: 5000,
      },
      {
        kind: "presence",
        channelId: "space-1",
        userId: "bob",
        state: "online",
        ttlMs: 75000,
      },
    ]);
    expect(other).not.toHaveBeenCalled();
  });

  it("ignores a signal of a subscription that is not active, and stops after unsubscribe", async () => {
    const client = makeClient();
    const heard = vi.fn();
    const stop = client.subscribeEphemeral(
      { workspaceId: "ws-1", channelId: "space-1" },
      heard,
    );
    const socket = await connectAndReady(client, () => kit.last());
    const typing = JSON.stringify({
      type: "typing",
      subscriptionId: "channel:space-1",
      userId: "bob",
      expiresInMs: 5000,
    });

    socket.onmessage?.({ data: typing }); // not acknowledged yet
    expect(heard).not.toHaveBeenCalled();

    socket.onmessage?.({ data: channelAck });
    stop();
    socket.onmessage?.({ data: typing });
    expect(heard).not.toHaveBeenCalled();
  });

  it("sends typing for an active channel subscription and presence once connected", async () => {
    const client = makeClient();
    client.subscribeEphemeral(
      { workspaceId: "ws-1", channelId: "space-1" },
      vi.fn(),
    );
    client.sendPresence("online"); // not connected yet: nothing sent
    const socket = await connectAndReady(client, () => kit.last());

    client.sendTyping("space-1"); // subscribed but not acknowledged: nothing sent
    socket.onmessage?.({ data: channelAck });
    client.sendTyping("space-1");
    client.sendTyping("unknown-space");
    client.sendPresence("idle");

    const sent = socket
      .sentFrames()
      .filter((f) => f.type === "typing" || f.type === "presence");
    expect(sent).toEqual([
      { type: "typing", subscriptionId: "channel:space-1" },
      { type: "presence", state: "idle" },
    ]);
  });
});
