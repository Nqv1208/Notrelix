import { describe, it, expect } from "vitest";
import { MockRealtimeTransport } from "../realtime/mock-realtime-transport";
import type { RealtimeEnvelope } from "@notrelix/realtime";
import { SPACES, USERS, actAs, chat, storeAs } from "./chat-test-kit";

const GENERAL = SPACES.general;
const LEADERSHIP = SPACES.leadership;
const THUMBS = "👍";
const ROCKET = "🚀";

type Store = ReturnType<typeof storeAs>;

/** A user message of general that is not deleted, the first `count` of them. */
function generalUserMessages(store: Store, count: number) {
  return store.chat
    .messagesOf(GENERAL)
    .filter((m) => m.kind === "user" && m.deletedAt === null)
    .slice(0, count);
}

function kindPath(kind: string): string {
  return encodeURIComponent(kind);
}

async function react(store: Store, messageId: string, kind: string) {
  return chat(
    store,
    "PUT",
    `/messages/${messageId}/reactions/${kindPath(kind)}`,
  );
}

async function unreact(store: Store, messageId: string, kind: string) {
  return chat(
    store,
    "DELETE",
    `/messages/${messageId}/reactions/${kindPath(kind)}`,
  );
}

async function reactorsOf(store: Store, messageId: string, query = "") {
  return chat(store, "GET", `/messages/${messageId}/reactions`, { query });
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

describe("MFB-CHAT-S2.5: Chat mock handlers (reactions)", () => {
  describe("react (UC-CHT-G06)", () => {
    it("adds the caller's reaction (201) and answers with the message's summary", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);

      const res = await react(store, target!.id, THUMBS);

      expect(res.status).toBe(201);
      expect(res.body).toEqual({
        messageId: target!.id,
        reactions: [{ emoji: THUMBS, count: 1, reactedByMe: true }],
      });
      expect(
        store.chat.reactions.get(
          store.chat.reactionKey(target!.id, USERS.owner, THUMBS),
        ),
      ).toBeDefined();
    });

    it("answers a repeat with the summary (200) and changes nothing", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      const first = await react(store, target!.id, THUMBS);

      const again = await react(store, target!.id, THUMBS);

      expect(again.status).toBe(200);
      expect(again.body).toEqual(first.body);
      expect(store.chat.reactionsOfMessage(target!.id)).toHaveLength(1);
    });

    it("counts two people with one kind and marks only the caller", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      await react(store, target!.id, THUMBS);

      actAs(store, "admin");
      const res = await react(store, target!.id, THUMBS);

      expect(res.status).toBe(201);
      expect(res.body.reactions).toEqual([
        { emoji: THUMBS, count: 2, reactedByMe: true },
      ]);
    });

    it("refuses a kind off the palette with 400", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);

      const res = await react(store, target!.id, "🔥");

      expect(res.status).toBe(400);
      expect(store.chat.reactionsOfMessage(target!.id)).toHaveLength(0);
    });

    it("refuses a system message with 409 message-not-reactable", async () => {
      const store = storeAs("owner");
      const system = store.chat
        .messagesOf(GENERAL)
        .find((m) => m.kind === "system");
      expect(system).toBeDefined();

      const res = await react(store, system!.id, THUMBS);

      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("message-not-reactable");
    });

    it("refuses a deleted message with 409 message-deleted", async () => {
      const store = storeAs("owner");
      const deleted = store.chat
        .messagesOf(GENERAL)
        .find((m) => m.kind === "user" && m.deletedAt !== null);
      expect(deleted).toBeDefined();

      const res = await react(store, deleted!.id, THUMBS);

      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("message-deleted");
    });

    it("refuses a reaction in an archived channel with 409 channel-archived", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      store.chat.spaces.get(GENERAL)!.archivedAt = "2026-01-01T00:00:00.000Z";

      const res = await react(store, target!.id, THUMBS);

      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("channel-archived");
    });

    it("answers a message in a private channel the caller is not in with NotFound", async () => {
      const store = storeAs("owner");
      const leadership = store.chat
        .messagesOf(LEADERSHIP)
        .find((m) => m.kind === "user" && m.deletedAt === null);
      expect(leadership).toBeDefined();

      const res = await react(store, leadership!.id, THUMBS);

      expect(res.status).toBe(404);
      expect(res.body.errorCode).toBe("resource.not_found");
    });

    it("a repeat of a kind the caller has does not count against the limits", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      await react(store, target!.id, THUMBS);
      await react(store, target!.id, ROCKET);

      // The palette holds six kinds and the limits are six, so the limits are not reachable with the palette alone;
      // what the test pins is that a repeat is a no-op and never counted twice.
      const again = await react(store, target!.id, ROCKET);
      expect(again.status).toBe(200);
      expect(store.chat.reactionsOfMessage(target!.id)).toHaveLength(2);
    });
  });

  describe("remove and who reacted (UC-CHT-G07)", () => {
    it("removes the caller's reaction (200) and drops the count", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      await react(store, target!.id, THUMBS);

      const res = await unreact(store, target!.id, THUMBS);

      expect(res.status).toBe(200);
      expect(res.body).toEqual({ messageId: target!.id, reactions: [] });
    });

    it("answers the removal of a reaction the caller does not have with 200 and changes nothing", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);

      const res = await unreact(store, target!.id, THUMBS);

      expect(res.status).toBe(200);
      expect(res.body.reactions).toEqual([]);
    });

    it("lists the reactors earliest first with their emoji and person, narrowed by kind when asked", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      await react(store, target!.id, THUMBS);
      actAs(store, "admin");
      await react(store, target!.id, ROCKET);

      const all = await reactorsOf(store, target!.id);
      expect(all.status).toBe(200);
      expect(all.body.items).toEqual([
        { emoji: THUMBS, userId: USERS.owner, reactedAt: expect.any(String) },
        { emoji: ROCKET, userId: USERS.admin, reactedAt: expect.any(String) },
      ]);
      expect(all.body.nextCursor).toBeNull();

      const narrowed = await reactorsOf(
        store,
        target!.id,
        `kind=${kindPath(ROCKET)}`,
      );
      expect(
        narrowed.body.items.map((i: { emoji: string }) => i.emoji),
      ).toEqual([ROCKET]);
    });

    it("pages the reactors with a cursor without repeats", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      await react(store, target!.id, THUMBS);
      actAs(store, "admin");
      await react(store, target!.id, ROCKET);

      const first = await reactorsOf(store, target!.id, "limit=1");
      const second = await reactorsOf(
        store,
        target!.id,
        `limit=1&cursor=${encodeURIComponent(first.body.nextCursor)}`,
      );

      expect(first.body.items).toHaveLength(1);
      expect(first.body.nextCursor).not.toBeNull();
      expect(second.body.items).toHaveLength(1);
      expect(second.body.items[0].emoji).toBe(ROCKET);
      expect(second.body.nextCursor).toBeNull();
    });

    it("answers the reactors of a private channel the caller is not in with NotFound", async () => {
      const store = storeAs("owner");
      const leadership = store.chat
        .messagesOf(LEADERSHIP)
        .find((m) => m.kind === "user" && m.deletedAt === null);

      const res = await reactorsOf(store, leadership!.id);

      expect(res.status).toBe(404);
    });
  });

  describe("the summary in message views (history and thread reads)", () => {
    it("carries the summary of each message in the history read, marked for the viewer", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      await react(store, target!.id, THUMBS);

      const { sequence } = store.chat.messages.get(target!.id)!;
      const history = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: `around=${sequence}&limit=10`,
      });
      const row = history.body.items.find(
        (m: { id: string }) => m.id === target!.id,
      );

      expect(row.reactions).toEqual([
        { emoji: THUMBS, count: 1, reactedByMe: true },
      ]);
      actAs(store, "admin");
      const asOther = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: `around=${sequence}&limit=10`,
      });
      const otherRow = asOther.body.items.find(
        (m: { id: string }) => m.id === target!.id,
      );
      expect(otherRow.reactions).toEqual([
        { emoji: THUMBS, count: 1, reactedByMe: false },
      ]);
    });
  });

  describe("realtime and deletion", () => {
    it("sends one chat.reaction.changed frame per change and none for a repeat", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);

      const frames = await captureChannelFrames(async () => {
        await react(store, target!.id, THUMBS);
        await react(store, target!.id, THUMBS);
        await unreact(store, target!.id, THUMBS);
      });

      const reactions = frames.filter(
        (f) => f.eventType === "chat.reaction.changed",
      );
      expect(reactions).toHaveLength(2);
      expect(reactions[0]!.payloadContract).toBe("chat.reaction.v1");
      expect(reactions[0]!.payload).toEqual({
        messageId: target!.id,
        kind: THUMBS,
        change: "added",
        actorId: USERS.owner,
      });
      expect(reactions[1]!.payload).toMatchObject({ change: "removed" });
    });

    it("clears the reactions of a message when it is deleted", async () => {
      const store = storeAs("owner");
      const [target] = generalUserMessages(store, 1);
      await react(store, target!.id, THUMBS);

      const res = await chat(store, "DELETE", `/messages/${target!.id}`, {
        query: `expectedVersion=${store.chat.messages.get(target!.id)!.version}`,
      });

      expect(res.status).toBe(204);
      expect(store.chat.reactionsOfMessage(target!.id)).toHaveLength(0);
    });
  });
});
