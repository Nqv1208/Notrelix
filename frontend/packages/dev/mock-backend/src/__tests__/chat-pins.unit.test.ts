import { describe, it, expect } from "vitest";
import { MockRealtimeTransport } from "../realtime/mock-realtime-transport";
import type { RealtimeEnvelope } from "@notrelix/realtime";
import { SPACES, USERS, actAs, chat, storeAs } from "./chat-test-kit";

const GENERAL = SPACES.general;
const LEADERSHIP = SPACES.leadership;

type Store = ReturnType<typeof storeAs>;

/** A user message of general that is not deleted, the first `count` of them. */
function generalUserMessages(store: Store, count: number) {
  return store.chat
    .messagesOf(GENERAL)
    .filter((m) => m.kind === "user" && m.deletedAt === null)
    .slice(0, count);
}

async function pin(store: Store, messageId: string) {
  return chat(store, "PUT", `/messages/${messageId}/pin`);
}

async function pinsOf(store: Store, query = "") {
  return chat(store, "GET", `/spaces/${GENERAL}/pins`, { query });
}

/** The realtime frames of the general channel, captured while `run` executes. */
async function captureChannelFrames(
  run: () => Promise<void>,
): Promise<RealtimeEnvelope[]> {
  const transport = new MockRealtimeTransport();
  const frames: RealtimeEnvelope[] = [];
  const stop = transport.subscribeScope(
    { kind: "channel", id: GENERAL },
    (envelope) => {
      frames.push(envelope);
    },
  );
  try {
    await run();
  } finally {
    stop();
    transport.dispose();
  }
  return frames;
}

describe("MFB-CHAT-S2.3: Chat mock handlers (pins)", () => {
  describe("pin (UC-CHT-F06)", () => {
    it("pins a message (201), stores a system message that announces it, and lists the pin", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      const before = store.chat.spaces.get(GENERAL)!.lastSequence;

      const res = await pin(store, target!.id);

      expect(res.status).toBe(201);
      expect(res.body).toMatchObject({
        message: { id: target!.id, content: target!.content },
        pinnedBy: USERS.owner,
      });
      expect(typeof res.body.pinnedAt).toBe("string");
      // The announcement is the next message of the channel, a system message by the pinner.
      expect(store.chat.spaces.get(GENERAL)!.lastSequence).toBe(before + 1);
      const announcement = store.chat.messagesOf(GENERAL).at(-1)!;
      expect(announcement).toMatchObject({
        kind: "system",
        content: "pinned a message",
        authorId: USERS.owner,
      });

      const list = await pinsOf(store);
      expect(list.status).toBe(200);
      expect(
        list.body.items.map((i: { message: { id: string } }) => i.message.id),
      ).toEqual([target!.id]);
    });

    it("answers a repeat pin with the existing pin (200), changes nothing and writes no announcement", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      const first = await pin(store, target!.id);
      const sequence = store.chat.spaces.get(GENERAL)!.lastSequence;

      actAs(store, "admin");
      const again = await pin(store, target!.id);

      expect(again.status).toBe(200);
      expect(again.body).toEqual(first.body);
      expect(store.chat.spaces.get(GENERAL)!.lastSequence).toBe(sequence);
    });

    it("refuses a system message with 409 message-not-pinnable and a deleted message with 409 message-deleted", async () => {
      const store = storeAs("owner");
      const system = store.chat
        .messagesOf(GENERAL)
        .find((m) => m.kind === "system");
      expect(system).toBeDefined();
      const notPinnable = await pin(store, system!.id);
      expect(notPinnable.status).toBe(409);
      expect(notPinnable.body.errorCode).toBe("message-not-pinnable");

      const [target] = generalUserMessages(store, 1);
      await chat(store, "DELETE", `/messages/${target!.id}`, {
        query: `expectedVersion=${target!.version}`,
      });
      const deleted = await pin(store, target!.id);
      expect(deleted.status).toBe(409);
      expect(deleted.body.errorCode).toBe("message-deleted");
    });

    it("refuses the pin past 50 with 422 limit-reached and changes nothing", async () => {
      const store = storeAs("owner");
      const targets = generalUserMessages(store, 51);
      expect(targets).toHaveLength(51);
      for (const target of targets.slice(0, 50)) {
        expect((await pin(store, target.id)).status).toBe(201);
      }
      const sequence = store.chat.spaces.get(GENERAL)!.lastSequence;

      const refused = await pin(store, targets[50]!.id);

      expect(refused.status).toBe(422);
      expect(refused.body.errorCode).toBe("limit-reached");
      expect(store.chat.spaces.get(GENERAL)!.lastSequence).toBe(sequence);
      expect(store.chat.pins.has(targets[50]!.id)).toBe(false);
    });

    it("refuses a Guest with 403 before the membership check", async () => {
      const store = storeAs("viewer");
      const [target] = generalUserMessages(store, 1);
      const res = await pin(store, target!.id);
      expect(res.status).toBe(403);
      expect(res.body.errorCode).toBe("auth.forbidden");
    });

    it("answers a pin of a message in a private channel the caller is not in with NotFound", async () => {
      const store = storeAs("owner");
      const leadership = store.chat
        .messagesOf(LEADERSHIP)
        .find((m) => m.kind === "user" && m.deletedAt === null);
      expect(leadership).toBeDefined();
      const res = await pin(store, leadership!.id);
      expect(res.status).toBe(404);
      expect(res.body.errorCode).toBe("resource.not_found");
    });

    it("refuses a pin in an archived channel with 409 channel-archived", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      store.chat.spaces.get(GENERAL)!.archivedAt = "2026-01-01T00:00:00.000Z";
      const res = await pin(store, target!.id);
      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("channel-archived");
    });

    it("announces the pin on the channel: a system message frame, then a chat.space.changed pinned frame", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      const frames = await captureChannelFrames(async () => {
        const res = await pin(store, target!.id);
        expect(res.status).toBe(201);
      });
      const spaceFrame = frames.find(
        (f) => f.eventType === "chat.space.changed",
      );
      expect(spaceFrame).toMatchObject({
        payloadContract: "chat.space.v1",
        subscriptionId: `channel:${GENERAL}`,
        resourceKind: "Channel",
        resourceId: GENERAL,
        payload: { change: "pinned", messageId: target!.id },
      });
      expect(
        frames.some(
          (f) =>
            f.eventType === "chat.message.created" &&
            (f.payload as { kind: string }).kind === "system",
        ),
      ).toBe(true);
    });
  });

  describe("unpin (UC-CHT-F07)", () => {
    it("removes the pin (204), announces the unpin, and repeats as a success that changes nothing", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      await pin(store, target!.id);

      const frames = await captureChannelFrames(async () => {
        const res = await chat(store, "DELETE", `/messages/${target!.id}/pin`);
        expect(res.status).toBe(204);
      });
      expect(store.chat.pins.has(target!.id)).toBe(false);
      expect(frames).toContainEqual(
        expect.objectContaining({
          eventType: "chat.space.changed",
          payload: { change: "unpinned", messageId: target!.id },
        }),
      );

      const again = await chat(store, "DELETE", `/messages/${target!.id}/pin`);
      expect(again.status).toBe(204);
      expect((await pinsOf(store)).body.items).toEqual([]);
    });

    it("refuses an unpin in an archived channel with 409", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      await pin(store, target!.id);
      store.chat.spaces.get(GENERAL)!.archivedAt = "2026-01-01T00:00:00.000Z";
      const res = await chat(store, "DELETE", `/messages/${target!.id}/pin`);
      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("channel-archived");
    });

    it("ends the pin when its message is deleted, and announces it", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      await pin(store, target!.id);

      const frames = await captureChannelFrames(async () => {
        const res = await chat(store, "DELETE", `/messages/${target!.id}`, {
          query: `expectedVersion=${target!.version}`,
        });
        expect(res.status).toBe(204);
      });

      expect(store.chat.pins.has(target!.id)).toBe(false);
      expect((await pinsOf(store)).body.items).toEqual([]);
      expect(frames).toContainEqual(
        expect.objectContaining({
          eventType: "chat.space.changed",
          payload: { change: "unpinned", messageId: target!.id },
        }),
      );
    });
  });

  describe("list pins (UC-CHT-F07)", () => {
    it("lists the pins newest first and pages them with the cursor", async () => {
      const store = storeAs("owner");
      const [a, b, c] = generalUserMessages(store, 3);
      await pin(store, a!.id);
      await pin(store, b!.id);
      await pin(store, c!.id);

      const first = await pinsOf(store, "limit=2");
      expect(first.status).toBe(200);
      expect(
        first.body.items.map((i: { message: { id: string } }) => i.message.id),
      ).toEqual([c!.id, b!.id]);
      expect(first.body.nextCursor).not.toBeNull();

      const second = await pinsOf(
        store,
        `limit=2&cursor=${encodeURIComponent(first.body.nextCursor)}`,
      );
      expect(
        second.body.items.map((i: { message: { id: string } }) => i.message.id),
      ).toEqual([a!.id]);
      expect(second.body.nextCursor).toBeNull();
    });

    it("refuses a limit out of 1 to 100 and a malformed cursor with 400", async () => {
      const store = storeAs("owner");
      expect((await pinsOf(store, "limit=0")).status).toBe(400);
      expect((await pinsOf(store, "limit=101")).status).toBe(400);
      expect((await pinsOf(store, "cursor=nope")).status).toBe(400);
    });

    it("answers the pins of a private channel the caller is not in with NotFound", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "GET", `/spaces/${LEADERSHIP}/pins`);
      expect(res.status).toBe(404);
      expect(res.body.errorCode).toBe("resource.not_found");
    });
  });
});
