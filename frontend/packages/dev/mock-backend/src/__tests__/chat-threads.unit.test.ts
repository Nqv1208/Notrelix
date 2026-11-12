import { describe, it, expect } from "vitest";
import { MockRealtimeTransport } from "../realtime/mock-realtime-transport";
import type { RealtimeEnvelope } from "@notrelix/realtime";
import { SPACES, USERS, actAs, chat, key, storeAs } from "./chat-test-kit";

const GENERAL = SPACES.general;
const ENGINEERING = SPACES.engineering;
const LEADERSHIP = SPACES.leadership;

type Store = ReturnType<typeof storeAs>;

/** A root message sent to general by the store's current person. */
async function sendRoot(store: Store, clientMessageId: string) {
  const res = await chat(store, "POST", `/spaces/${GENERAL}/messages`, {
    idempotencyKey: key(`root-${clientMessageId}`),
    body: { content: `Root ${clientMessageId}`, clientMessageId },
  });
  expect(res.status).toBe(201);
  return res.body;
}

/** A reply by the store's current person, with the Idempotency-Key of its clientMessageId. */
async function reply(
  store: Store,
  rootId: string,
  clientMessageId: string,
  extra: Record<string, unknown> = {},
) {
  return chat(store, "POST", `/messages/${rootId}/replies`, {
    idempotencyKey: key(`reply-${clientMessageId}`),
    body: { content: `Reply ${clientMessageId}`, clientMessageId, ...extra },
  });
}

async function repliesOf(store: Store, rootId: string, query = "") {
  return chat(store, "GET", `/messages/${rootId}/replies`, { query });
}

describe("MFB-CHAT-S2.2: Chat mock handlers (threads)", () => {
  describe("reply (UC-CHT-F01)", () => {
    it("stores the reply at the next space and thread sequence and answers 201 with its thread fields", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-a");
      const before = store.chat.spaces.get(GENERAL)!.lastSequence;

      const res = await reply(store, root.id, "thr-reply-a");

      expect(res.status).toBe(201);
      expect(res.body).toMatchObject({
        spaceId: GENERAL,
        sequence: before + 1,
        kind: "user",
        authorId: USERS.owner,
        content: "Reply thr-reply-a",
        threadRootId: root.id,
        threadSequence: 1,
        alsoSentToChannel: false,
        replyToId: null,
      });
      expect(store.chat.spaces.get(GENERAL)!.lastSequence).toBe(before + 1);
    });

    it("keeps the thread sequence gap-free and interleaves it with the space sequence", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-seq");
      const first = await reply(store, root.id, "thr-seq-1");
      const channel = await chat(store, "POST", `/spaces/${GENERAL}/messages`, {
        idempotencyKey: key("thr-seq-channel"),
        body: { content: "Between", clientMessageId: "thr-seq-channel" },
      });
      const second = await reply(store, root.id, "thr-seq-2");

      expect([first.body.threadSequence, second.body.threadSequence]).toEqual([
        1, 2,
      ]);
      expect(channel.body.sequence).toBe(first.body.sequence + 1);
      expect(second.body.sequence).toBe(channel.body.sequence + 1);
      expect(store.chat.spaces.get(GENERAL)!.lastSequence).toBe(
        second.body.sequence,
      );
    });

    it("answers a retry with the same clientMessageId with the stored reply (200) and stores nothing", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-cid");
      const first = await reply(store, root.id, "thr-cid-1");
      const sequence = store.chat.spaces.get(GENERAL)!.lastSequence;

      const retry = await chat(store, "POST", `/messages/${root.id}/replies`, {
        idempotencyKey: key("thr-cid-retry"),
        body: { content: "Reply thr-cid-1", clientMessageId: "thr-cid-1" },
      });

      expect(retry.status).toBe(200);
      expect(retry.body.id).toBe(first.body.id);
      expect(store.chat.spaces.get(GENERAL)!.lastSequence).toBe(sequence);
      const page = await repliesOf(store, root.id);
      expect(page.body.items).toHaveLength(1);
    });

    it("replays a create with the same Idempotency-Key and payload, and refuses a changed payload on that key with 409", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-idem");
      const body = { content: "Once", clientMessageId: "thr-idem-1" };
      const first = await chat(store, "POST", `/messages/${root.id}/replies`, {
        idempotencyKey: key("thr-idem-key"),
        body,
      });
      const sequence = store.chat.spaces.get(GENERAL)!.lastSequence;

      const replay = await chat(store, "POST", `/messages/${root.id}/replies`, {
        idempotencyKey: key("thr-idem-key"),
        body,
      });
      expect(replay.status).toBe(201);
      expect(replay.body).toEqual(first.body);
      expect(store.chat.spaces.get(GENERAL)!.lastSequence).toBe(sequence);

      const changed = await chat(
        store,
        "POST",
        `/messages/${root.id}/replies`,
        {
          idempotencyKey: key("thr-idem-key"),
          body: { content: "Other", clientMessageId: "thr-idem-2" },
        },
      );
      expect(changed.status).toBe(409);
      expect(changed.body.errorCode).toBe("idempotency_payload_mismatch");
    });

    it("refuses a reply without an Idempotency-Key, empty text, text over 4000 characters and a long clientMessageId", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-val");
      const noKey = await chat(store, "POST", `/messages/${root.id}/replies`, {
        body: { content: "x", clientMessageId: "thr-val-nokey" },
      });
      expect(noKey.status).toBe(400);
      const empty = await reply(store, root.id, "thr-val-empty", {
        content: "   ",
      });
      expect(empty.status).toBe(400);
      const long = await reply(store, root.id, "thr-val-long", {
        content: "x".repeat(4001),
      });
      expect(long.status).toBe(400);
      const longId = await chat(store, "POST", `/messages/${root.id}/replies`, {
        idempotencyKey: key("thr-val-longid"),
        body: { content: "x", clientMessageId: "x".repeat(129) },
      });
      expect(longId.status).toBe(400);
    });

    it("refuses a reply to a reply and to a system message with 409 thread-root-invalid", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-invalid");
      const first = await reply(store, root.id, "thr-invalid-1");
      const toReply = await reply(store, first.body.id, "thr-invalid-2");
      expect(toReply.status).toBe(409);
      expect(toReply.body.errorCode).toBe("thread-root-invalid");

      // The seed's join message at general sequence 100 is a system message.
      const system = store.chat
        .messagesOf(GENERAL)
        .find((m) => m.kind === "system");
      expect(system).toBeDefined();
      const toSystem = await reply(store, system!.id, "thr-invalid-3");
      expect(toSystem.status).toBe(409);
      expect(toSystem.body.errorCode).toBe("thread-root-invalid");
    });

    it("refuses a reply to a deleted root with no replies (409 message-deleted), and allows one when the root already has replies", async () => {
      const store = storeAs("owner");
      const empty = await sendRoot(store, "thr-root-gone");
      await chat(store, "DELETE", `/messages/${empty.id}`, {
        query: `expectedVersion=${empty.version}`,
      });
      const refused = await reply(store, empty.id, "thr-gone-1");
      expect(refused.status).toBe(409);
      expect(refused.body.errorCode).toBe("message-deleted");

      const withReply = await sendRoot(store, "thr-root-kept");
      const kept = await reply(store, withReply.id, "thr-kept-1");
      expect(kept.status).toBe(201);
      await chat(store, "DELETE", `/messages/${withReply.id}`, {
        query: `expectedVersion=${withReply.version}`,
      });
      const after = await reply(store, withReply.id, "thr-kept-2");
      expect(after.status).toBe(201);
      expect(after.body.threadSequence).toBe(2);
    });

    it("refuses a reply in an archived channel with 409 channel-archived", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-arch");
      store.chat.spaces.get(GENERAL)!.archivedAt = "2026-01-01T00:00:00.000Z";
      const res = await reply(store, root.id, "thr-arch-1");
      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("channel-archived");
    });

    it("answers a reply by a person who is not a member of the space with NotFound", async () => {
      const store = storeAs("owner");
      const engineering = store.chat
        .messagesOf(ENGINEERING)
        .find((m) => m.kind === "user" && m.deletedAt === null);
      expect(engineering).toBeDefined();
      // The viewer is a Guest of general only, not of engineering.
      actAs(store, "viewer");
      const res = await reply(store, engineering!.id, "thr-nonmember-1");
      expect(res.status).toBe(404);
      expect(res.body.errorCode).toBe("resource.not_found");
    });

    it("answers a reply to a private channel the caller is not in with NotFound, Owner included", async () => {
      const store = storeAs("owner");
      const leadership = store.chat
        .messagesOf(LEADERSHIP)
        .find((m) => m.kind === "user" && m.deletedAt === null);
      expect(leadership).toBeDefined();
      const res = await reply(store, leadership!.id, "thr-private-1");
      expect(res.status).toBe(404);
      expect(res.body.errorCode).toBe("resource.not_found");
    });
  });

  describe("read replies (UC-CHT-F03)", () => {
    it("pages replies by thread sequence with after and limit, and reports hasNewer", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-page");
      for (const n of [1, 2, 3]) await reply(store, root.id, `thr-page-${n}`);

      const first = await repliesOf(store, root.id, "limit=2");
      expect(first.status).toBe(200);
      expect(first.body.root).toMatchObject({ id: root.id });
      expect(
        first.body.items.map(
          (m: { threadSequence: number }) => m.threadSequence,
        ),
      ).toEqual([1, 2]);
      expect(first.body.hasNewer).toBe(true);
      expect(first.body.thread).toMatchObject({
        rootMessageId: root.id,
        replyCount: 3,
        following: true,
        muted: false,
        unreadReplies: 0,
      });

      const rest = await repliesOf(store, root.id, "after=2&limit=2");
      expect(
        rest.body.items.map(
          (m: { threadSequence: number }) => m.threadSequence,
        ),
      ).toEqual([3]);
      expect(rest.body.hasNewer).toBe(false);
    });

    it("refuses a limit out of 1 to 100 and a negative after with 400", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-bounds");
      expect((await repliesOf(store, root.id, "limit=0")).status).toBe(400);
      expect((await repliesOf(store, root.id, "limit=101")).status).toBe(400);
      expect((await repliesOf(store, root.id, "after=-1")).status).toBe(400);
    });

    it("answers a reply as the root of its own thread with NotFound, and a missing root the same", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-notfound");
      const first = await reply(store, root.id, "thr-nf-1");
      expect((await repliesOf(store, first.body.id)).status).toBe(404);
      const missing = await repliesOf(
        store,
        "c3000000-0000-4000-8000-999999999999",
      );
      expect(missing.status).toBe(404);
    });

    it("keeps replies out of the channel history unless they were also sent to the channel", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-history");
      const hidden = await reply(store, root.id, "thr-hidden-1");
      const shown = await reply(store, root.id, "thr-shown-1", {
        alsoSendToChannel: true,
      });
      const history = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: `after=${root.sequence}`,
      });
      const ids = history.body.items.map((m: { id: string }) => m.id);
      expect(ids).not.toContain(hidden.body.id);
      expect(ids).toContain(shown.body.id);
      const shownInHistory = history.body.items.find(
        (m: { id: string }) => m.id === shown.body.id,
      );
      expect(shownInHistory).toMatchObject({
        threadRootId: root.id,
        threadSequence: 2,
        alsoSentToChannel: true,
      });
    });
  });

  describe("follow, mute and read (UC-CHT-F04, F08)", () => {
    it("makes the root author and the replier followers, and counts the others' replies as unread for the author", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-follow");
      actAs(store, "admin");
      const res = await reply(store, root.id, "thr-follow-1");
      expect(res.status).toBe(201);

      const asAdmin = await repliesOf(store, root.id);
      expect(asAdmin.body.thread).toMatchObject({
        following: true,
        unreadReplies: 0,
      });
      actAs(store, "owner");
      const asOwner = await repliesOf(store, root.id);
      expect(asOwner.body.thread).toMatchObject({
        following: true,
        muted: false,
        unreadReplies: 1,
      });
    });

    it("follows a thread with the replies already posted read, and mutes it on request", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-newfollow");
      actAs(store, "admin");
      await reply(store, root.id, "thr-nf-a");

      actAs(store, "member");
      const follow = await chat(store, "PUT", `/threads/${root.id}/follow`, {
        body: { following: true, muted: true },
      });
      expect(follow.status).toBe(200);
      expect(follow.body).toEqual({
        rootMessageId: root.id,
        following: true,
        muted: true,
      });
      const view = await repliesOf(store, root.id);
      expect(view.body.thread).toMatchObject({
        following: true,
        muted: true,
        unreadReplies: 0,
      });
    });

    it("unfollows with following false, and refuses a body without following", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-unfollow");
      await reply(store, root.id, "thr-uf-1");
      const missing = await chat(store, "PUT", `/threads/${root.id}/follow`, {
        body: { muted: true },
      });
      expect(missing.status).toBe(400);

      const unfollow = await chat(store, "PUT", `/threads/${root.id}/follow`, {
        body: { following: false },
      });
      expect(unfollow.status).toBe(200);
      expect(unfollow.body).toEqual({
        rootMessageId: root.id,
        following: false,
        muted: false,
      });
      const mine = await chat(store, "GET", "/me/threads");
      expect(
        mine.body.items.map((i: { root: { id: string } }) => i.root.id),
      ).not.toContain(root.id);
    });

    it("answers a follow of a thread that does not exist with 404", async () => {
      const store = storeAs("owner");
      const res = await chat(
        store,
        "PUT",
        "/threads/c3000000-0000-4000-8000-999999999998/follow",
        { body: { following: true } },
      );
      expect(res.status).toBe(404);
    });

    it("moves the read pointer forward only, clamps it to the last reply, and returns the unread left", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-read");
      actAs(store, "admin");
      for (const n of [1, 2, 3]) {
        await reply(store, root.id, `thr-read-${n}`);
      }
      actAs(store, "owner");
      const up = await chat(store, "PUT", `/threads/${root.id}/read`, {
        body: { upToReplySequence: 2 },
      });
      expect(up.status).toBe(200);
      expect(up.body).toEqual({ rootMessageId: root.id, unreadReplies: 1 });

      const lower = await chat(store, "PUT", `/threads/${root.id}/read`, {
        body: { upToReplySequence: 1 },
      });
      expect(lower.body.unreadReplies).toBe(1);

      const past = await chat(store, "PUT", `/threads/${root.id}/read`, {
        body: { upToReplySequence: 99 },
      });
      expect(past.body.unreadReplies).toBe(0);

      const invalid = await chat(store, "PUT", `/threads/${root.id}/read`, {
        body: {},
      });
      expect(invalid.status).toBe(400);
    });

    it("keeps no read pointer for a thread the caller does not follow, and answers 0 unread", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-nofollow");
      actAs(store, "admin");
      await reply(store, root.id, "thr-nofollow-a");
      actAs(store, "member");
      const res = await chat(store, "PUT", `/threads/${root.id}/read`, {
        body: { upToReplySequence: 1 },
      });
      expect(res.status).toBe(200);
      expect(res.body.unreadReplies).toBe(0);
      expect(
        store.chat.threadFollowerOf(root.id, USERS.member),
      ).toBeUndefined();
    });

    it("lists the threads I follow, most recent reply first, with unread counts, and pages with a cursor", async () => {
      const store = storeAs("owner");
      const older = await sendRoot(store, "thr-mine-older");
      const newer = await sendRoot(store, "thr-mine-newer");
      await reply(store, older.id, "thr-mine-o1");
      await reply(store, newer.id, "thr-mine-n1");
      // The admin replies after the owner in the newer thread: the owner has one unread there.
      actAs(store, "admin");
      await reply(store, newer.id, "thr-mine-admin");
      actAs(store, "owner");

      const all = await chat(store, "GET", "/me/threads");
      expect(all.status).toBe(200);
      const roots = all.body.items.map(
        (i: { root: { id: string } }) => i.root.id,
      );
      expect(roots.indexOf(newer.id)).toBeGreaterThanOrEqual(0);
      expect(roots.indexOf(newer.id)).toBeLessThan(roots.indexOf(older.id));
      const newerRow = all.body.items.find(
        (i: { root: { id: string } }) => i.root.id === newer.id,
      );
      expect(newerRow.thread.unreadReplies).toBe(1);

      const first = await chat(store, "GET", "/me/threads", {
        query: "limit=1",
      });
      expect(first.body.items).toHaveLength(1);
      expect(first.body.nextCursor).not.toBeNull();
      const second = await chat(store, "GET", "/me/threads", {
        query: `limit=1&cursor=${first.body.nextCursor}`,
      });
      expect(second.body.items).toHaveLength(1);
      expect(second.body.items[0].root.id).not.toBe(
        first.body.items[0].root.id,
      );
    });

    it("refuses a list limit out of 1 to 100 and a cursor that is not a number", async () => {
      const store = storeAs("owner");
      expect(
        (await chat(store, "GET", "/me/threads", { query: "limit=0" })).status,
      ).toBe(400);
      expect(
        (await chat(store, "GET", "/me/threads", { query: "cursor=abc" }))
          .status,
      ).toBe(400);
    });
  });

  describe("realtime frames of a reply", () => {
    it("sends the channel frame of a reply with its threadRootId and threadSequence", async () => {
      const store = storeAs("owner");
      const root = await sendRoot(store, "thr-root-rt");
      const transport = new MockRealtimeTransport();
      const frames: RealtimeEnvelope[] = [];
      const stop = transport.subscribeScope(
        { kind: "channel", id: GENERAL },
        (envelope) => {
          frames.push(envelope);
        },
      );
      try {
        const res = await reply(store, root.id, "thr-rt-1");
        expect(res.status).toBe(201);
        const created = frames.filter(
          (f) =>
            f.eventType === "chat.message.created" &&
            (f.payload as { id: string }).id === res.body.id,
        );
        expect(created).toHaveLength(1);
        expect(created[0]!.payload).toMatchObject({
          id: res.body.id,
          threadRootId: root.id,
          threadSequence: 1,
        });
      } finally {
        stop();
        transport.dispose();
      }
    });
  });
});
