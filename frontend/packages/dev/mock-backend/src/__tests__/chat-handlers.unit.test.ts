import { describe, it, expect } from "vitest";
import {
  GROUPS,
  SECONDARY_WORKSPACE_ID,
  SPACES,
  USERS,
  chat,
  key,
  storeAs,
} from "./chat-test-kit";
import { GENERAL_MESSAGE_COUNT } from "../contexts/chat/chat-world";

const GENERAL = SPACES.general;

/** The unread count the owner has in general at the seed: the messages after 300 by others. */
const OWNER_GENERAL_UNREAD = 15;

describe("MFB-CHAT-S1.6: Chat mock handlers (channels, messages, read state)", () => {
  describe("seed world and privacy", () => {
    it("lists the channels the owner is in, without the private channel they are not in and without deleted ones", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "GET", "/channels");
      expect(res.status).toBe(200);
      const names = res.body.map((c: { name: string }) => c.name);
      // Sidebar order: position, then name (general sits first at position 1).
      expect(names).toEqual([
        "general",
        "engineering",
        "erp-rollout",
        "random",
        "launch-2026",
      ]);
      const general = res.body.find((c: { id: string }) => c.id === GENERAL);
      expect(general).toMatchObject({
        isMember: true,
        role: "manager",
        kind: "channel_public",
        version: 3,
      });
    });

    it("gives a Guest only the spaces they were added to, and browses nothing", async () => {
      const store = storeAs("viewer");
      const mine = await chat(store, "GET", "/channels");
      expect(mine.body.map((c: { name: string }) => c.name)).toEqual([
        "general",
        "erp-rollout",
      ]);
      const browse = await chat(store, "GET", "/channels", {
        query: "browse=true",
      });
      expect(browse.body).toEqual([]);
    });

    it("answers a private channel the caller is not in with NotFound, Owner included (CHT-004)", async () => {
      const owner = storeAs("owner");
      const denied = await chat(owner, "GET", `/channels/${SPACES.leadership}`);
      expect(denied.status).toBe(404);
      expect(denied.body.errorCode).toBe("resource.not_found");
      const messages = await chat(
        owner,
        "GET",
        `/spaces/${SPACES.leadership}/messages`,
      );
      expect(messages.status).toBe(404);

      const admin = storeAs("admin");
      const allowed = await chat(
        admin,
        "GET",
        `/channels/${SPACES.leadership}`,
      );
      expect(allowed.status).toBe(200);
      expect(
        allowed.body.members.map((m: { userId: string }) => m.userId).sort(),
      ).toEqual([USERS.admin, USERS.member].sort());
    });

    it("answers a channel of another Workspace with the same NotFound as a missing one", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "GET", `/channels/${GENERAL}`, {
        workspaceId: SECONDARY_WORKSPACE_ID,
      });
      expect(res.status).toBe(404);
      expect(res.body.errorCode).toBe("resource.not_found");
    });

    it("answers a person who is not in the Workspace with NotFound", async () => {
      // The commenter is a member of the primary Workspace only; the secondary one is not hers.
      const store = storeAs("commenter");
      const res = await chat(store, "GET", "/channels", {
        workspaceId: SECONDARY_WORKSPACE_ID,
      });
      expect(res.status).toBe(404);
    });
  });

  describe("messages: paging, sequence, tombstones and system messages", () => {
    it("serves the newest page of 50 with the flags of a paged list", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "GET", `/spaces/${GENERAL}/messages`);
      expect(res.status).toBe(200);
      const sequences = res.body.items.map(
        (m: { sequence: number }) => m.sequence,
      );
      expect(sequences).toHaveLength(50);
      expect(sequences[0]).toBe(GENERAL_MESSAGE_COUNT - 49);
      expect(sequences.at(-1)).toBe(GENERAL_MESSAGE_COUNT);
      expect(res.body.hasOlder).toBe(true);
      expect(res.body.hasNewer).toBe(false);
    });

    it("pages older with before and newer with after, and cuts around a sequence", async () => {
      const store = storeAs("owner");
      const older = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: "before=271",
      });
      expect(older.body.items[0].sequence).toBe(221);
      expect(older.body.items.at(-1).sequence).toBe(270);
      expect(older.body.hasNewer).toBe(true);

      const newer = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: "after=300&limit=5",
      });
      expect(
        newer.body.items.map((m: { sequence: number }) => m.sequence),
      ).toEqual([301, 302, 303, 304, 305]);
      expect(newer.body.hasOlder).toBe(true);
      expect(newer.body.hasNewer).toBe(true);

      const around = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: "around=160&limit=10",
      });
      expect(
        around.body.items.map((m: { sequence: number }) => m.sequence),
      ).toEqual([156, 157, 158, 159, 160, 161, 162, 163, 164, 165]);
    });

    it("refuses two cursors and a limit out of 1 to 100", async () => {
      const store = storeAs("owner");
      expect(
        (
          await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
            query: "before=10&after=2",
          })
        ).status,
      ).toBe(400);
      expect(
        (
          await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
            query: "limit=0",
          })
        ).status,
      ).toBe(400);
      expect(
        (
          await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
            query: "limit=101",
          })
        ).status,
      ).toBe(400);
    });

    it("keeps the sequence gap-free and never reuses one", () => {
      const store = storeAs("owner");
      const sequences = store.chat.messagesOf(GENERAL).map((m) => m.sequence);
      expect(sequences).toEqual(
        Array.from({ length: GENERAL_MESSAGE_COUNT }, (_, i) => i + 1),
      );
    });

    it("keeps a tombstone in place with its author and no content", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: "before=41&limit=2",
      });
      const tombstone = res.body.items.find(
        (m: { sequence: number }) => m.sequence === 40,
      );
      expect(tombstone).toMatchObject({
        sequence: 40,
        deleted: true,
        content: null,
        kind: "user",
      });
      expect(tombstone.authorId).toBe(USERS.viewer);
    });

    it("carries the system message of a join as kind system", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: "around=100&limit=2",
      });
      const system = res.body.items.find(
        (m: { sequence: number }) => m.sequence === 100,
      );
      expect(system).toMatchObject({
        kind: "system",
        content: "joined the channel",
        clientMessageId: null,
      });
    });
  });

  describe("send", () => {
    it("stores the message at the next sequence and returns 201", async () => {
      const store = storeAs("owner");
      const before = store.chat.spaces.get(GENERAL)!.lastSequence;
      const res = await chat(store, "POST", `/spaces/${GENERAL}/messages`, {
        idempotencyKey: key("send-a"),
        body: { content: "  Xin chào  ", clientMessageId: "cid-send-a" },
      });
      expect(res.status).toBe(201);
      expect(res.body).toMatchObject({
        sequence: before + 1,
        kind: "user",
        authorId: USERS.owner,
        content: "Xin chào",
        clientMessageId: "cid-send-a",
        deleted: false,
        edited: false,
        version: 1,
      });
      expect(store.chat.spaces.get(GENERAL)!.lastSequence).toBe(before + 1);
    });

    it("answers a retry with the same clientMessageId with the stored message and stores nothing", async () => {
      const store = storeAs("owner");
      const first = await chat(store, "POST", `/spaces/${GENERAL}/messages`, {
        idempotencyKey: key("send-b"),
        body: { content: "Một lần", clientMessageId: "cid-idem" },
      });
      const sequenceAfterFirst = store.chat.spaces.get(GENERAL)!.lastSequence;
      const retry = await chat(store, "POST", `/spaces/${GENERAL}/messages`, {
        idempotencyKey: key("send-b-retry"),
        body: { content: "Một lần", clientMessageId: "cid-idem" },
      });
      expect(retry.status).toBe(200);
      expect(retry.body.id).toBe(first.body.id);
      expect(store.chat.spaces.get(GENERAL)!.lastSequence).toBe(
        sequenceAfterFirst,
      );
    });

    it("keeps the sequence gap-free over several sends", async () => {
      const store = storeAs("owner");
      const start = store.chat.spaces.get(GENERAL)!.lastSequence;
      const sequences: number[] = [];
      for (const n of [1, 2, 3]) {
        const res = await chat(store, "POST", `/spaces/${GENERAL}/messages`, {
          idempotencyKey: key(`seq-${n}`),
          body: { content: `Dòng ${n}`, clientMessageId: `cid-seq-${n}` },
        });
        sequences.push(res.body.sequence);
      }
      expect(sequences).toEqual([start + 1, start + 2, start + 3]);
    });

    it("refuses empty text, text over 4000 characters, a long clientMessageId and a missing Idempotency-Key", async () => {
      const store = storeAs("owner");
      const path = `/spaces/${GENERAL}/messages`;
      expect(
        (
          await chat(store, "POST", path, {
            idempotencyKey: key("v-1"),
            body: { content: "   ", clientMessageId: "x1" },
          })
        ).status,
      ).toBe(400);
      expect(
        (
          await chat(store, "POST", path, {
            idempotencyKey: key("v-2"),
            body: { content: "a".repeat(4001), clientMessageId: "x2" },
          })
        ).status,
      ).toBe(400);
      expect(
        (
          await chat(store, "POST", path, {
            idempotencyKey: key("v-3"),
            body: { content: "ok", clientMessageId: "c".repeat(129) },
          })
        ).status,
      ).toBe(400);
      expect(
        (
          await chat(store, "POST", path, {
            body: { content: "ok", clientMessageId: "no-key-1" },
          })
        ).status,
      ).toBe(400);
    });

    it("refuses a send to an archived channel with 409 channel-archived", async () => {
      const store = storeAs("admin");
      const res = await chat(
        store,
        "POST",
        `/spaces/${SPACES.launch2026}/messages`,
        {
          idempotencyKey: key("archived"),
          body: { content: "Muộn rồi", clientMessageId: "cid-archived" },
        },
      );
      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("channel-archived");
    });

    it("refuses a send by a person who is not a member of the space with NotFound", async () => {
      const store = storeAs("viewer");
      const res = await chat(
        store,
        "POST",
        `/spaces/${SPACES.random}/messages`,
        {
          idempotencyKey: key("not-member"),
          body: {
            content: "Mình không ở đây",
            clientMessageId: "cid-not-member",
          },
        },
      );
      expect(res.status).toBe(404);
    });

    it("refuses a reply to a message of another space", async () => {
      const store = storeAs("owner");
      const other = store.chat.messagesOf(SPACES.random)[0]!;
      const res = await chat(store, "POST", `/spaces/${GENERAL}/messages`, {
        idempotencyKey: key("reply-other"),
        body: {
          content: "Trả lời",
          clientMessageId: "cid-reply-other",
          replyToId: other.id,
        },
      });
      expect(res.status).toBe(404);
    });
  });

  describe("edit and delete", () => {
    it("edits the author's message, keeps the sequence and moves the version", async () => {
      const store = storeAs("owner");
      const first = store.chat.messagesOf(GENERAL)[0]!;
      const res = await chat(store, "PATCH", `/messages/${first.id}`, {
        body: { content: "Đã sửa lời chào", expectedVersion: 1 },
      });
      expect(res.status).toBe(200);
      expect(res.body).toMatchObject({
        sequence: 1,
        content: "Đã sửa lời chào",
        edited: true,
        version: 2,
      });
    });

    it("answers a stale expectedVersion with 412 and changes nothing", async () => {
      const store = storeAs("owner");
      const first = store.chat.messagesOf(GENERAL)[0]!;
      // The first edit moves the version to 2 in the store used below; a fresh store is at 1.
      const edited = await chat(store, "PATCH", `/messages/${first.id}`, {
        body: { content: "Đã sửa lời chào", expectedVersion: 1 },
      });
      expect(edited.body.version).toBe(2);
      const res = await chat(store, "PATCH", `/messages/${first.id}`, {
        body: { content: "Bản cũ", expectedVersion: 1 },
      });
      expect(res.status).toBe(412);
      expect(res.body.errorCode).toBe("common.precondition-failed");
      expect(store.chat.messages.get(first.id)!.content).toBe(
        "Đã sửa lời chào",
      );
    });

    it("refuses an edit by someone other than the author with 403", async () => {
      const store = storeAs("admin");
      const first = store.chat.messagesOf(GENERAL)[0]!;
      const res = await chat(store, "PATCH", `/messages/${first.id}`, {
        body: { content: "Không phải của tôi", expectedVersion: 1 },
      });
      expect(res.status).toBe(403);
    });

    it("refuses to edit a system message (409) and a tombstone (409 message-deleted)", async () => {
      const admin = storeAs("admin");
      const system = admin.chat
        .messagesOf(GENERAL)
        .find((m) => m.sequence === 100)!;
      const sys = await chat(admin, "PATCH", `/messages/${system.id}`, {
        body: { content: "x", expectedVersion: 1 },
      });
      expect(sys.status).toBe(409);
      expect(sys.body.errorCode).toBe("system-message-immutable");

      const viewer = storeAs("viewer");
      const tomb = viewer.chat
        .messagesOf(GENERAL)
        .find((m) => m.sequence === 40)!;
      const dead = await chat(viewer, "PATCH", `/messages/${tomb.id}`, {
        body: { content: "x", expectedVersion: 1 },
      });
      expect(dead.status).toBe(409);
      expect(dead.body.errorCode).toBe("message-deleted");
    });

    it("deletes the author's message into a tombstone that keeps the sequence, and a repeat changes nothing", async () => {
      const store = storeAs("owner");
      const message = store.chat
        .messagesOf(GENERAL)
        .find((m) => m.sequence === 5)!;
      const res = await chat(store, "DELETE", `/messages/${message.id}`, {
        query: "expectedVersion=1",
      });
      expect(res.status).toBe(204);
      const after = store.chat.messages.get(message.id)!;
      expect(after).toMatchObject({ sequence: 5, content: null, version: 2 });
      expect(after.deletedAt).not.toBeNull();

      const again = await chat(store, "DELETE", `/messages/${message.id}`, {
        query: "expectedVersion=2",
      });
      expect(again.status).toBe(204);
      expect(store.chat.messages.get(message.id)!.version).toBe(2);
    });

    it("refuses a delete of a system message with 409 and a stale delete with 412", async () => {
      const admin = storeAs("admin");
      const system = admin.chat
        .messagesOf(GENERAL)
        .find((m) => m.sequence === 100)!;
      const res = await chat(admin, "DELETE", `/messages/${system.id}`, {
        query: "expectedVersion=1",
      });
      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("system-message-immutable");

      const owner = storeAs("owner");
      const mine = owner.chat
        .messagesOf(GENERAL)
        .find((m) => m.sequence === 9)!;
      const stale = await chat(owner, "DELETE", `/messages/${mine.id}`, {
        query: "expectedVersion=7",
      });
      expect(stale.status).toBe(412);
    });
  });

  describe("unread and read state", () => {
    it("counts other people's messages after the boundary, excluding own, system and deleted ones", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "GET", "/me/unread");
      expect(res.status).toBe(200);
      const general = res.body.spaces.find(
        (s: { spaceId: string }) => s.spaceId === GENERAL,
      );
      expect(general).toMatchObject({
        readSequence: 300,
        lastSequence: GENERAL_MESSAGE_COUNT,
        unreadCount: OWNER_GENERAL_UNREAD,
        muted: false,
      });
      expect(general.firstUnreadSequence).toBe(302);
    });

    it("keeps a muted space out of the totals, and lists the spaces by name", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "GET", "/me/unread");
      const names = res.body.spaces.map((s: { name: string }) => s.name);
      expect(names).toEqual([
        "engineering",
        "erp-rollout",
        "general",
        "launch-2026",
        "random",
      ]);
      const engineering = res.body.spaces.find(
        (s: { name: string }) => s.name === "engineering",
      );
      expect(engineering).toMatchObject({
        muted: true,
        notificationLevel: "none",
        unreadCount: 2,
      });
      // erp-rollout: two unread (the Guest's and the member's); general: 15; engineering is muted.
      expect(res.body.totalUnread).toBe(OWNER_GENERAL_UNREAD + 2);
      expect(res.body.totalMentions).toBe(0);
    });

    it("moves the boundary forward only, and never past the last sequence", async () => {
      const store = storeAs("owner");
      const up = await chat(store, "PUT", `/spaces/${GENERAL}/read`, {
        body: { upToSequence: 9999 },
      });
      expect(up.status).toBe(200);
      expect(up.body).toEqual({
        spaceId: GENERAL,
        readSequence: GENERAL_MESSAGE_COUNT,
        lastSequence: GENERAL_MESSAGE_COUNT,
      });

      const back = await chat(store, "PUT", `/spaces/${GENERAL}/read`, {
        body: { upToSequence: 5 },
      });
      expect(back.body.readSequence).toBe(GENERAL_MESSAGE_COUNT);
    });

    it("marks every space read with read-all", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "POST", "/me/read-all");
      expect(res.status).toBe(200);
      expect(res.body.totalUnread).toBe(0);
      expect(
        res.body.spaces.every(
          (s: { unreadCount: number }) => s.unreadCount === 0,
        ),
      ).toBe(true);
    });

    it("sets the notification level, and a timed mute that has ended changes nothing", async () => {
      const store = storeAs("owner");
      const level = await chat(
        store,
        "PUT",
        `/spaces/${SPACES.random}/notification-level`,
        {
          body: { level: "mentions" },
        },
      );
      expect(level.body).toEqual({
        spaceId: SPACES.random,
        notificationLevel: "mentions",
        muted: false,
        mutedUntil: null,
      });

      const pastMute = await chat(
        store,
        "PUT",
        `/spaces/${SPACES.random}/notification-level`,
        {
          body: { level: "none", mutedUntil: "2000-01-01T00:00:00.000Z" },
        },
      );
      expect(pastMute.body.muted).toBe(false);
      expect(pastMute.body.notificationLevel).toBe("mentions");

      const timed = await chat(
        store,
        "PUT",
        `/spaces/${SPACES.random}/notification-level`,
        {
          body: { level: "none", mutedUntil: "2999-01-01T00:00:00.000Z" },
        },
      );
      expect(timed.body).toMatchObject({
        notificationLevel: "none",
        muted: true,
        mutedUntil: "2999-01-01T00:00:00.000Z",
      });
    });

    it("refuses an until time with a level other than none", async () => {
      const store = storeAs("owner");
      const res = await chat(
        store,
        "PUT",
        `/spaces/${SPACES.random}/notification-level`,
        {
          body: { level: "all", mutedUntil: "2999-01-01T00:00:00.000Z" },
        },
      );
      expect(res.status).toBe(400);
    });
  });

  describe("channels: create, update, archive, delete, order", () => {
    it("creates a channel as its first manager, replays the same key, and refuses a changed payload on that key", async () => {
      const store = storeAs("owner");
      const body = { name: "design", topic: "Thiết kế", isPrivate: false };
      const first = await chat(store, "POST", "/channels", {
        idempotencyKey: key("create-design"),
        body,
      });
      expect(first.status).toBe(201);
      expect(first.body).toMatchObject({
        name: "design",
        isMember: true,
        role: "manager",
        version: 1,
        isArchived: false,
      });
      const replay = await chat(store, "POST", "/channels", {
        idempotencyKey: key("create-design"),
        body,
      });
      expect(replay.status).toBe(201);
      expect(replay.body.id).toBe(first.body.id);
      const changed = await chat(store, "POST", "/channels", {
        idempotencyKey: key("create-design"),
        body: { ...body, name: "design-2" },
      });
      expect(changed.status).toBe(409);
      expect(changed.body.errorCode).toBe("idempotency_payload_mismatch");
    });

    it("refuses a channel name already used by a live channel (case-insensitive) with 409 name-conflict", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "POST", "/channels", {
        idempotencyKey: key("create-dup"),
        body: { name: "General" },
      });
      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("name-conflict");
    });

    it("refuses channel creation by a Guest with 403", async () => {
      const store = storeAs("viewer");
      const res = await chat(store, "POST", "/channels", {
        idempotencyKey: key("guest-create"),
        body: { name: "guest-room" },
      });
      expect(res.status).toBe(403);
    });

    it("renames with the expected version, announces the rename, and refuses a stale version", async () => {
      const store = storeAs("owner");
      const stale = await chat(store, "PATCH", `/channels/${SPACES.random}`, {
        body: { name: "random", expectedVersion: 99 },
      });
      expect(stale.status).toBe(412);

      const before = store.chat.spaces.get(SPACES.random)!.lastSequence;
      const res = await chat(store, "PATCH", `/channels/${SPACES.random}`, {
        body: { name: "random-talk", topic: null, expectedVersion: 1 },
      });
      expect(res.status).toBe(200);
      expect(res.body).toMatchObject({ name: "random-talk", version: 2 });
      expect(store.chat.spaces.get(SPACES.random)!.lastSequence).toBe(
        before + 1,
      );
      const announcement = store.chat.messagesOf(SPACES.random).at(-1)!;
      expect(announcement).toMatchObject({
        kind: "system",
        content: "renamed the channel to random-talk",
      });
    });

    it("refuses a rename by a member who is not a manager (403), and by a Guest (403)", async () => {
      const member = await chat(
        storeAs("member"),
        "PATCH",
        `/channels/${SPACES.general}`,
        {
          body: { name: "general", expectedVersion: 3 },
        },
      );
      expect(member.status).toBe(403);
      const guest = await chat(
        storeAs("viewer"),
        "PATCH",
        `/channels/${SPACES.general}`,
        {
          body: { name: "general", expectedVersion: 3 },
        },
      );
      expect(guest.status).toBe(403);
    });

    it("archives a channel (read-only, announced) and unarchives it", async () => {
      const store = storeAs("owner");
      const archived = await chat(
        store,
        "POST",
        `/channels/${SPACES.engineering}/archive`,
        {
          body: { archived: true, expectedVersion: 2 },
        },
      );
      expect(archived.body).toMatchObject({ isArchived: true, version: 3 });
      const send = await chat(
        store,
        "POST",
        `/spaces/${SPACES.engineering}/messages`,
        {
          idempotencyKey: key("after-archive"),
          body: { content: "Muộn", clientMessageId: "cid-after-archive" },
        },
      );
      expect(send.status).toBe(409);
      expect(send.body.errorCode).toBe("channel-archived");

      const back = await chat(
        store,
        "POST",
        `/channels/${SPACES.engineering}/archive`,
        {
          body: { archived: false, expectedVersion: 3 },
        },
      );
      expect(back.body).toMatchObject({ isArchived: false, version: 4 });
    });

    it("deletes a channel (204), releases its name and drops it from the list", async () => {
      const store = storeAs("owner");
      const res = await chat(store, "DELETE", `/channels/${SPACES.random}`, {
        query: "expectedVersion=1",
      });
      expect(res.status).toBe(204);
      const list = await chat(store, "GET", "/channels");
      expect(list.body.map((c: { id: string }) => c.id)).not.toContain(
        SPACES.random,
      );
      const created = await chat(store, "POST", "/channels", {
        idempotencyKey: key("reuse-random"),
        body: { name: "random" },
      });
      expect(created.status).toBe(201);
    });

    it("orders the sidebar and refuses the whole batch on one stale version", async () => {
      const store = storeAs("owner");
      const stale = await chat(store, "PATCH", "/channels/order", {
        body: {
          placements: [
            {
              channelId: SPACES.general,
              groupId: null,
              position: 10,
              expectedVersion: 3,
            },
            {
              channelId: SPACES.random,
              groupId: null,
              position: 11,
              expectedVersion: 42,
            },
          ],
        },
      });
      expect(stale.status).toBe(412);
      expect(store.chat.spaces.get(SPACES.general)!.position).toBe(1);

      const ok = await chat(store, "PATCH", "/channels/order", {
        body: {
          placements: [
            {
              channelId: SPACES.general,
              groupId: null,
              position: 10,
              expectedVersion: 3,
            },
            {
              channelId: SPACES.random,
              groupId: GROUPS.team,
              position: 11,
              expectedVersion: 1,
            },
          ],
        },
      });
      expect(ok.status).toBe(200);
      expect(store.chat.spaces.get(SPACES.general)).toMatchObject({
        position: 10,
        version: 4,
      });
      expect(store.chat.spaces.get(SPACES.random)).toMatchObject({
        position: 11,
        groupId: GROUPS.team,
        version: 2,
      });
    });
  });

  describe("members", () => {
    it("joins a public channel once, announces it, and refuses a Guest with 403", async () => {
      const member = storeAs("member");
      const join = await chat(
        member,
        "POST",
        `/channels/${SPACES.random}/members/me`,
      );
      expect(join.status).toBe(200);
      expect(join.body).toMatchObject({ isMember: true, role: "member" });
      expect(member.chat.messagesOf(SPACES.random).at(-1)).toMatchObject({
        kind: "system",
        content: "joined the channel",
      });
      const again = await chat(
        member,
        "POST",
        `/channels/${SPACES.random}/members/me`,
      );
      expect(again.status).toBe(200);
      expect(
        member.chat
          .messagesOf(SPACES.random)
          .filter((m) => m.kind === "system"),
      ).toHaveLength(2);

      const guest = await chat(
        storeAs("viewer"),
        "POST",
        `/channels/${SPACES.random}/members/me`,
      );
      expect(guest.status).toBe(403);
    });

    it("answers a join of a private channel with NotFound", async () => {
      const res = await chat(
        storeAs("member"),
        "POST",
        `/channels/${SPACES.leadership}/members/me`,
      );
      expect(res.status).toBe(404);
    });

    it("adds Workspace members and skips a person who is not in the Workspace", async () => {
      const store = storeAs("owner");
      const res = await chat(
        store,
        "POST",
        `/channels/${SPACES.random}/members`,
        {
          body: { userIds: [USERS.member, "mock-user-commenter"] },
        },
      );
      expect(res.status).toBe(200);
      expect(res.body.added).toEqual([USERS.member]);
      expect(res.body.skipped).toEqual(["mock-user-commenter"]);
    });

    it("refuses to remove the last manager (422) and lets a member leave", async () => {
      const owner = storeAs("owner");
      const last = await chat(
        owner,
        "DELETE",
        `/channels/${SPACES.erpRollout}/members/${USERS.owner}`,
      );
      expect(last.status).toBe(422);
      expect(last.body.errorCode).toBe("last-manager");

      // The member leaves in their own store; their view of the channel then lists the rest.
      const member = storeAs("member");
      const leave = await chat(
        member,
        "DELETE",
        `/channels/${SPACES.erpRollout}/members/${USERS.member}`,
      );
      expect(leave.status).toBe(204);
      const detail = await chat(
        member,
        "GET",
        `/channels/${SPACES.erpRollout}`,
      );
      expect(
        detail.body.members.map((m: { userId: string }) => m.userId),
      ).not.toContain(USERS.member);
    });

    it("lets an Admin remove a member and writes the removal as a system message", async () => {
      const admin = storeAs("admin");
      const res = await chat(
        admin,
        "DELETE",
        `/channels/${SPACES.engineering}/members/${USERS.member}`,
      );
      expect(res.status).toBe(204);
      expect(admin.chat.messagesOf(SPACES.engineering).at(-1)).toMatchObject({
        kind: "system",
        content: "was removed from the channel",
        authorId: USERS.admin,
      });
    });
  });

  describe("channel groups", () => {
    it("creates a group at the next position, refuses a duplicate name and a Member", async () => {
      const owner = storeAs("owner");
      const created = await chat(owner, "POST", "/channel-groups", {
        idempotencyKey: key("group-ops"),
        body: { name: "Khách hàng" },
      });
      expect(created.status).toBe(201);
      expect(created.body).toMatchObject({
        name: "Khách hàng",
        position: 3,
        version: 1,
      });

      const dup = await chat(owner, "POST", "/channel-groups", {
        idempotencyKey: key("group-dup"),
        body: { name: "team" },
      });
      expect(dup.status).toBe(409);

      const member = await chat(storeAs("member"), "POST", "/channel-groups", {
        idempotencyKey: key("group-member"),
        body: { name: "Nope" },
      });
      expect(member.status).toBe(403);
    });

    it("renames a group with the expected version, and deleting it keeps its channels in place", async () => {
      const owner = storeAs("owner");
      const stale = await chat(
        owner,
        "PATCH",
        `/channel-groups/${GROUPS.team}`,
        {
          body: { name: "Team", position: 1, expectedVersion: 9 },
        },
      );
      expect(stale.status).toBe(412);

      const renamed = await chat(
        owner,
        "PATCH",
        `/channel-groups/${GROUPS.team}`,
        {
          body: { name: "Team A", position: 1, expectedVersion: 1 },
        },
      );
      expect(renamed.body).toMatchObject({ name: "Team A", version: 2 });

      const deleted = await chat(
        owner,
        "DELETE",
        `/channel-groups/${GROUPS.team}`,
        { query: "expectedVersion=2" },
      );
      expect(deleted.status).toBe(204);
      expect(owner.chat.spaces.get(SPACES.engineering)).toMatchObject({
        groupId: null,
        position: 2,
      });
    });
  });
});
