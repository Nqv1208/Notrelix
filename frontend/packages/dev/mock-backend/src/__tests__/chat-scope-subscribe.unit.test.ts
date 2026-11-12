import { describe, it, expect } from "vitest";
import { MockRealtimeTransport } from "../realtime/mock-realtime-transport";
import {
  storeAs,
  SPACES,
  USERS,
  WORKSPACE_ID,
  chat,
  key,
} from "./chat-test-kit";
import type { RealtimeEnvelope } from "@notrelix/realtime";

describe("MFB-CHAT-S1.8: the mock transport serves Chat scope subscriptions like the gateway", () => {
  it("a channel subscription receives the frames of that channel only, and no workspace frames", async () => {
    const store = storeAs("owner");
    const transport = new MockRealtimeTransport();
    const active: RealtimeEnvelope[] = [];
    const other: RealtimeEnvelope[] = [];
    const stopActive = transport.subscribe(
      { workspaceId: WORKSPACE_ID, channelId: SPACES.general },
      (envelope) => active.push(envelope),
    );
    const stopOther = transport.subscribe(
      { workspaceId: WORKSPACE_ID, channelId: SPACES.engineering },
      (envelope) => other.push(envelope),
    );
    try {
      const res = await chat(
        store,
        "POST",
        `/spaces/${SPACES.general}/messages`,
        {
          idempotencyKey: key("scope-a"),
          body: { content: "Scoped frame", clientMessageId: "cid-scope-a" },
        },
      );
      expect(res.status).toBe(201);
      expect(
        active.filter((f) => f.eventType === "chat.message.created"),
      ).toHaveLength(1);
      expect(other).toHaveLength(0);
    } finally {
      stopActive();
      stopOther();
      transport.dispose();
    }
  });

  it("the unsubscribe of a channel stops its frames (switching channels)", async () => {
    const store = storeAs("owner");
    const transport = new MockRealtimeTransport();
    const frames: RealtimeEnvelope[] = [];
    const stop = transport.subscribe(
      { workspaceId: WORKSPACE_ID, channelId: SPACES.general },
      (envelope) => frames.push(envelope),
    );
    stop();
    try {
      await chat(store, "POST", `/spaces/${SPACES.general}/messages`, {
        idempotencyKey: key("scope-b"),
        body: { content: "After unsubscribe", clientMessageId: "cid-scope-b" },
      });
      expect(frames).toHaveLength(0);
    } finally {
      transport.dispose();
    }
  });

  it("a user subscription receives the unread counters of its own person, in its workspace", async () => {
    const store = storeAs("owner");
    const transport = new MockRealtimeTransport();
    const member: RealtimeEnvelope[] = [];
    const stop = transport.subscribe(
      { workspaceId: WORKSPACE_ID, userId: USERS.member },
      (envelope) => member.push(envelope),
    );
    try {
      await chat(store, "POST", `/spaces/${SPACES.general}/messages`, {
        idempotencyKey: key("scope-c"),
        body: {
          content: "Badge for the member",
          clientMessageId: "cid-scope-c",
        },
      });
      const unread = member.filter(
        (f) => f.eventType === "chat.unread.changed",
      );
      expect(unread.length).toBeGreaterThan(0);
      expect(
        unread.every((f) => f.subscriptionId === `user:${USERS.member}`),
      ).toBe(true);
      expect(member.some((f) => f.eventType === "chat.message.created")).toBe(
        false,
      );
    } finally {
      stop();
      transport.dispose();
    }
  });
});
