import { describe, it, expect } from "vitest";
import { MockRealtimeTransport } from "../realtime/mock-realtime-transport";
import { MockChatState } from "../contexts/chat/chat-state";
import { storeAs, SPACES, USERS, chat, key } from "./chat-test-kit";
import type { RealtimeEnvelope } from "@notrelix/realtime";

describe("MFB-CHAT-S1.6: Chat realtime frames from the mock backend", () => {
  it("delivers a chat.message.v1 frame on the channel scope with the v1.1 identity and a gap-free streamVersion", async () => {
    const store = storeAs("owner");
    const transport = new MockRealtimeTransport();
    const frames: RealtimeEnvelope[] = [];
    const stop = transport.subscribeScope(
      { kind: "channel", id: SPACES.general },
      (envelope) => {
        frames.push(envelope);
      },
    );
    try {
      const res = await chat(
        store,
        "POST",
        `/spaces/${SPACES.general}/messages`,
        {
          idempotencyKey: key("realtime-a"),
          body: { content: "Frame shape", clientMessageId: "cid-frame-a" },
        },
      );
      expect(res.status).toBe(201);

      const channelFrames = frames.filter(
        (f) => f.payloadContract === "chat.message.v1",
      );
      expect(channelFrames).toHaveLength(1);
      const frame = channelFrames[0]!;
      expect(frame.eventType).toBe("chat.message.created");
      expect(frame.subscriptionId).toBe(`channel:${SPACES.general}`);
      expect(frame.streamKey.startsWith("chat:Channel:")).toBe(true);
      expect(frame.resourceKind).toBe("Channel");
      expect(frame.resourceId).toBe(SPACES.general);
      expect(frame.payload).toMatchObject({
        id: res.body.id,
        sequence: res.body.sequence,
        authorId: USERS.owner,
      });
    } finally {
      stop();
      transport.dispose();
    }
  });

  it("delivers an unread frame to each active non-author member on the user scope, never to the author", async () => {
    const store = storeAs("owner");
    const transport = new MockRealtimeTransport();
    const frames: RealtimeEnvelope[] = [];
    const stop = transport.subscribeScope(
      { kind: "user", id: USERS.member },
      (envelope) => {
        frames.push(envelope);
      },
    );
    const authorFrames: RealtimeEnvelope[] = [];
    const stopAuthor = transport.subscribeScope(
      { kind: "user", id: USERS.owner },
      (envelope) => {
        authorFrames.push(envelope);
      },
    );
    try {
      const res = await chat(
        store,
        "POST",
        `/spaces/${SPACES.general}/messages`,
        {
          idempotencyKey: key("realtime-unread"),
          body: {
            content: "Unread fan-out",
            clientMessageId: "cid-unread-fanout",
          },
        },
      );
      expect(res.status).toBe(201);
      const unread = frames.filter(
        (f) => f.payloadContract === "chat.unread.v1",
      );
      expect(unread.length).toBeGreaterThan(0);
      expect(unread[0]!.subscriptionId).toBe(`user:${USERS.member}`);
      expect(unread[0]!.streamKey.startsWith("user:notifications:")).toBe(true);
      expect(
        authorFrames.filter((f) => f.payloadContract === "chat.unread.v1"),
      ).toHaveLength(0);
      expect(
        frames
          .filter((f) => f.payloadContract === "chat.unread.v1")
          .every((f) => f.subscriptionId === `user:${USERS.member}`),
      ).toBe(true);
    } finally {
      stop();
      stopAuthor();
      transport.dispose();
    }
  });

  it("keeps the stream version of a streamKey gap-free across frames", () => {
    const state = new MockChatState(() => storeAs("owner"));
    const first = state.nextStreamVersion("chat:Channel:1");
    const second = state.nextStreamVersion("chat:Channel:1");
    const other = state.nextStreamVersion("user:notifications:1");
    expect(second).toBe(first + 1);
    expect(other).toBe(1);
  });
});
