import { describe, it, expect } from "vitest";
import { MockRealtimeTransport } from "../realtime/mock-realtime-transport";
import type { RealtimeEnvelope } from "@notrelix/realtime";
import { chatGuid } from "../contexts/chat/chat-state";
import { CHAT_LIMITS } from "../contexts/chat/chat-model";
import { SPACES, USERS, actAs, chat, key, storeAs } from "./chat-test-kit";

const GENERAL = SPACES.general;
const LEADERSHIP = SPACES.leadership;

type Store = ReturnType<typeof storeAs>;

let counter = 0;

/** A send with mentions; returns the call. */
async function send(
  store: Store,
  spaceId: string,
  content: string,
  mentions: unknown[] | undefined,
  extra: Record<string, unknown> = {},
) {
  counter += 1;
  return chat(store, "POST", `/spaces/${spaceId}/messages`, {
    idempotencyKey: key(`mention-${counter}`),
    body: {
      content,
      clientMessageId: `cid-mention-${counter}`,
      mentions,
      ...extra,
    },
  });
}

const person = (start: number, length: number, userId: string) => ({
  kind: "user",
  start,
  length,
  userId,
});
const link = (start: number, length: number, spaceId: string) => ({
  kind: "channel-link",
  start,
  length,
  spaceId,
});
const channelToken = (start: number) => ({
  kind: "broadcast-channel",
  start,
  length: 8,
});
const hereToken = (start: number) => ({
  kind: "broadcast-here",
  start,
  length: 5,
});

async function summaryOf(store: Store, spaceId: string) {
  const res = await chat(store, "GET", "/me/unread");
  expect(res.status).toBe(200);
  return {
    row: res.body.spaces.find(
      (s: { spaceId: string }) => s.spaceId === spaceId,
    ),
    totalMentions: res.body.totalMentions as number,
    spaces: res.body.spaces as { spaceId: string }[],
  };
}

describe("MFB-CHAT-S2.4: Chat mock handlers (mentions and broadcast)", () => {
  describe("mention a person (UC-CHT-G01)", () => {
    it("stores a mention of a member by id and answers with the validated list", async () => {
      const store = storeAs("owner");

      const res = await send(store, GENERAL, "hi @Member", [
        person(3, 7, USERS.member),
      ]);

      expect(res.status).toBe(201);
      expect(res.body.mentions).toEqual([
        {
          kind: "user",
          start: 3,
          length: 7,
          userId: USERS.member,
          spaceId: null,
        },
      ]);
      expect(res.body.mentionNotices).toEqual([]);
    });

    it("moves the spans onto the trimmed text", async () => {
      const store = storeAs("owner");

      const res = await send(store, GENERAL, "   hi @Member  ", [
        person(6, 7, USERS.member),
      ]);

      expect(res.body.content).toBe("hi @Member");
      expect(res.body.mentions[0].start).toBe(3);
    });

    it("answers every message view with the stored mentions", async () => {
      const store = storeAs("owner");
      const sent = await send(store, GENERAL, "hi @Member", [
        person(3, 7, USERS.member),
      ]);

      const page = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: "limit=1",
      });

      expect(page.body.items[0].id).toBe(sent.body.id);
      expect(page.body.items[0].mentions).toHaveLength(1);
      expect(page.body.items[0].mentionNotices).toEqual([]);
    });

    it("refuses a mention that does not fit the text (400)", async () => {
      const store = storeAs("owner");
      const cases: unknown[][] = [
        [person(3, 40, USERS.member)],
        [person(-1, 4, USERS.member)],
        [person(0, 0, USERS.member)],
        [person(2, 5, USERS.member)],
        [{ kind: "team", start: 3, length: 4, userId: USERS.member }],
        [person(3, 7, USERS.member), person(4, 5, USERS.admin)],
        [{ kind: "user", start: 3, length: 7 }],
      ];
      for (const mentions of cases) {
        const res = await send(store, GENERAL, "hi @Member", mentions);
        expect(res.status).toBe(400);
      }
      expect(
        store.chat.messagesOf(GENERAL).some((m) => m.content === "hi @Member"),
      ).toBe(false);
    });

    it("refuses more mentions than the limit (400)", async () => {
      const store = storeAs("owner");
      const text = Array.from(
        { length: CHAT_LIMITS.mentionsPerMessageMax + 1 },
        () => "@b",
      ).join(" ");
      const mentions = Array.from(
        { length: CHAT_LIMITS.mentionsPerMessageMax + 1 },
        (_, i) => person(i * 3, 2, USERS.member),
      );

      const res = await send(store, GENERAL, text, mentions);

      expect(res.status).toBe(400);
    });
  });

  describe("a mention that cannot be seen is a warning, not a leak (UC-CHT-G02, COL-030)", () => {
    it("stores a mention of a Workspace member outside a private channel and warns the sender", async () => {
      // The admin manages the private leadership channel; the owner is a Workspace member who is not in it.
      const store = storeAs("admin");

      const res = await send(store, LEADERSHIP, "ping @Owner", [
        person(5, 6, USERS.owner),
      ]);

      expect(res.status).toBe(201);
      expect(res.body.mentions).toHaveLength(1);
      expect(res.body.mentionNotices).toEqual([
        { code: "cannot-view", start: 5, length: 6, userId: USERS.owner },
      ]);
    });

    it("never counts the mention for the person who cannot see the channel", async () => {
      const store = storeAs("admin");
      await send(store, LEADERSHIP, "ping @Owner", [person(5, 6, USERS.owner)]);

      actAs(store, "owner");
      const { spaces, totalMentions } = await summaryOf(store, LEADERSHIP);

      expect(spaces.some((s) => s.spaceId === LEADERSHIP)).toBe(false);
      expect(totalMentions).toBe(0);
    });

    it("shows a person outside the Workspace as plain text and stores nothing", async () => {
      const store = storeAs("owner");

      const res = await send(store, GENERAL, "hi @Nobody", [
        person(3, 7, "mock-user-nobody"),
      ]);

      expect(res.body.mentions).toEqual([]);
      expect(res.body.mentionNotices).toEqual([
        { code: "unknown", start: 3, length: 7, userId: null },
      ]);
    });

    it("tells a Guest nothing about the Workspace: a colleague outside their spaces looks like a stranger", async () => {
      const store = storeAs("viewer");
      // The admin left the ERP channel, so for the Guest the admin is a Workspace member outside the space.
      const erp = store.chat.membershipOf(SPACES.erpRollout, USERS.admin);
      if (erp) erp.leftAt = "2026-01-01T00:00:00.000Z";

      const colleague = await send(store, SPACES.erpRollout, "hi @Admin", [
        person(3, 6, USERS.admin),
      ]);
      const stranger = await send(store, SPACES.erpRollout, "hi @Nobody", [
        person(3, 7, "mock-user-nobody"),
      ]);

      expect(colleague.body.mentionNotices[0].code).toBe("unknown");
      expect(stranger.body.mentionNotices[0].code).toBe("unknown");
      expect(colleague.body.mentions).toEqual([]);
    });

    it("answers a retry with the stored message, stores nothing twice and repeats the warning", async () => {
      const store = storeAs("admin");
      const body = {
        content: "ping @Owner",
        clientMessageId: "cid-retry-mention",
        mentions: [person(5, 6, USERS.owner)],
      };
      const first = await chat(
        store,
        "POST",
        `/spaces/${LEADERSHIP}/messages`,
        {
          idempotencyKey: key("retry-a"),
          body,
        },
      );
      const retry = await chat(
        store,
        "POST",
        `/spaces/${LEADERSHIP}/messages`,
        {
          idempotencyKey: key("retry-b"),
          body,
        },
      );

      expect(first.status).toBe(201);
      expect(retry.status).toBe(200);
      expect(retry.body.id).toBe(first.body.id);
      expect(retry.body.mentions).toHaveLength(1);
      expect(retry.body.mentionNotices).toEqual([
        { code: "cannot-view", start: 5, length: 6, userId: USERS.owner },
      ]);
      expect(
        store.chat
          .messagesOf(LEADERSHIP)
          .filter((m) => m.clientMessageId === "cid-retry-mention"),
      ).toHaveLength(1);
    });
  });

  describe("broadcast with @channel or @here (UC-CHT-G03)", () => {
    it("stores a broadcast sent by someone who holds the action", async () => {
      const store = storeAs("owner");

      const res = await send(store, GENERAL, "hey @channel", [channelToken(4)]);

      expect(res.status).toBe(201);
      expect(res.body.mentions).toEqual([
        {
          kind: "broadcast-channel",
          start: 4,
          length: 8,
          userId: null,
          spaceId: null,
        },
      ]);
      expect(res.body.mentionNotices).toEqual([]);
    });

    it("stores @here as its own kind", async () => {
      const store = storeAs("owner");

      const res = await send(store, GENERAL, "hey @here", [hereToken(4)]);

      expect(res.body.mentions[0].kind).toBe("broadcast-here");
    });

    it("shows a broadcast of a Member as plain text with a notice (no action)", async () => {
      const store = storeAs("member");

      const res = await send(store, GENERAL, "hey @channel", [channelToken(4)]);

      expect(res.status).toBe(201);
      expect(res.body.mentions).toEqual([]);
      expect(res.body.mentionNotices).toEqual([
        { code: "broadcast-not-allowed", start: 4, length: 8, userId: null },
      ]);
    });

    it("never lets a Guest broadcast", async () => {
      const store = storeAs("viewer");

      const res = await send(store, GENERAL, "hey @channel", [channelToken(4)]);

      expect(res.body.mentions).toEqual([]);
      expect(res.body.mentionNotices[0].code).toBe("broadcast-not-allowed");
    });

    it("shows a broadcast in a channel with no one else to reach as plain text", async () => {
      const store = storeAs("owner");
      // The admin leaves random, so the owner is its only active member.
      const admin = store.chat.membershipOf(SPACES.random, USERS.admin)!;
      admin.leftAt = "2026-01-01T00:00:00.000Z";

      const res = await send(store, SPACES.random, "hey @channel", [
        channelToken(4),
      ]);

      expect(res.status).toBe(201);
      expect(res.body.mentions).toEqual([]);
      expect(res.body.mentionNotices).toEqual([
        { code: "no-audience", start: 4, length: 8, userId: null },
      ]);
    });

    it("counts a broadcast as a mention for every member but the author", async () => {
      const store = storeAs("owner");
      const before = (await summaryOf(store, GENERAL)).row.mentionCount;
      await send(store, GENERAL, "hey @channel", [channelToken(4)]);

      const author = await summaryOf(store, GENERAL);
      actAs(store, "admin");
      const admin = await summaryOf(store, GENERAL);
      actAs(store, "member");
      const member = await summaryOf(store, GENERAL);

      expect(author.row.mentionCount).toBe(before);
      expect(admin.row.mentionCount).toBe(1);
      expect(member.row.mentionCount).toBe(1);
    });

    it("needs the confirmation above the member threshold", async () => {
      const store = storeAs("owner");
      const space = store.chat.spaces.get(GENERAL)!;
      for (let i = 0; i < CHAT_LIMITS.broadcastConfirmThreshold; i += 1) {
        store.chat.memberships.set(`${GENERAL}|extra-${i}`, {
          spaceId: GENERAL,
          workspaceId: space.workspaceId,
          userId: `extra-${i}`,
          role: "member",
          notificationLevel: "all",
          mutedUntil: null,
          readSequence: 0,
          mentionCount: 0,
          joinedAt: "2026-01-01T00:00:00.000Z",
          leftAt: null,
        });
      }

      const unconfirmed = await send(store, GENERAL, "hey @channel", [
        channelToken(4),
      ]);
      const confirmed = await send(
        store,
        GENERAL,
        "hey @channel",
        [channelToken(4)],
        { confirmBroadcast: true },
      );

      expect(unconfirmed.status).toBe(409);
      expect(unconfirmed.body.errorCode).toBe(
        "broadcast-confirmation-required",
      );
      expect(confirmed.status).toBe(201);
      expect(confirmed.body.mentions).toHaveLength(1);
    });
  });

  describe("link to another channel with # (UC-CHT-G04)", () => {
    it("stores a link to a public channel by id", async () => {
      const store = storeAs("owner");

      const res = await send(store, GENERAL, "see #engineering", [
        link(4, 12, SPACES.engineering),
      ]);

      expect(res.body.mentions).toEqual([
        {
          kind: "channel-link",
          start: 4,
          length: 12,
          userId: null,
          spaceId: SPACES.engineering,
        },
      ]);
    });

    it("shows a link to a private channel the sender is not in as plain text, like a missing one", async () => {
      const store = storeAs("owner");

      const toPrivate = await send(store, GENERAL, "see #leadership", [
        link(4, 11, LEADERSHIP),
      ]);
      const toMissing = await send(store, GENERAL, "see #nothing", [
        link(4, 8, chatGuid("space", 999)),
      ]);

      expect(toPrivate.body.mentions).toEqual([]);
      expect(toPrivate.body.mentionNotices[0].code).toBe("link-not-visible");
      expect(toMissing.body.mentionNotices[0].code).toBe("link-not-visible");
    });

    it("leaves a link to a private channel out of the list of a reader who cannot see it", async () => {
      const store = storeAs("admin");
      const sent = await send(store, GENERAL, "see #leadership", [
        link(4, 11, LEADERSHIP),
      ]);
      expect(sent.body.mentions).toHaveLength(1);

      const forAdmin = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: "limit=1",
      });
      actAs(store, "owner");
      const forOwner = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: "limit=1",
      });

      expect(forAdmin.body.items[0].mentions).toHaveLength(1);
      expect(forOwner.body.items[0].mentions).toEqual([]);
    });
  });

  describe("edit, delete and thread reply", () => {
    it("replaces the mentions when the text changes and leaves them when it does not", async () => {
      const store = storeAs("owner");
      const sent = await send(store, GENERAL, "hi @Member", [
        person(3, 7, USERS.member),
      ]);

      const same = await chat(store, "PATCH", `/messages/${sent.body.id}`, {
        body: {
          content: "hi @Member",
          expectedVersion: sent.body.version,
          mentions: [],
        },
      });
      expect(same.body.mentions).toHaveLength(1);

      const edited = await chat(store, "PATCH", `/messages/${sent.body.id}`, {
        body: {
          content: "hi @Admin",
          expectedVersion: same.body.version,
          mentions: [person(3, 6, USERS.admin)],
        },
      });

      expect(edited.status).toBe(200);
      expect(edited.body.mentions).toEqual([
        {
          kind: "user",
          start: 3,
          length: 6,
          userId: USERS.admin,
          spaceId: null,
        },
      ]);
    });

    it("removes the mentions of a deleted message", async () => {
      const store = storeAs("owner");
      const sent = await send(store, GENERAL, "hi @Member", [
        person(3, 7, USERS.member),
      ]);

      const deleted = await chat(store, "DELETE", `/messages/${sent.body.id}`, {
        query: `expectedVersion=${sent.body.version}`,
      });

      expect(deleted.status).toBe(204);
      expect(store.chat.mentions.has(sent.body.id)).toBe(false);
    });

    it("stores the mentions of a thread reply like a send", async () => {
      const store = storeAs("owner");
      const root = await send(store, GENERAL, "root", undefined);

      const reply = await chat(
        store,
        "POST",
        `/messages/${root.body.id}/replies`,
        {
          idempotencyKey: key("mention-reply"),
          body: {
            content: "cc @Member",
            clientMessageId: "cid-mention-reply",
            mentions: [person(3, 7, USERS.member)],
          },
        },
      );

      expect(reply.status).toBe(201);
      expect(reply.body.mentions).toHaveLength(1);
    });
  });

  describe("mention counters (UC-CHT-D04)", () => {
    it("counts a mention for the mentioned person only and badges the total", async () => {
      const store = storeAs("owner");
      await send(store, GENERAL, "hi @Member", [person(3, 7, USERS.member)]);

      actAs(store, "member");
      const member = await summaryOf(store, GENERAL);
      actAs(store, "admin");
      const admin = await summaryOf(store, GENERAL);

      expect(member.row.mentionCount).toBe(1);
      expect(member.totalMentions).toBe(1);
      expect(admin.row.mentionCount).toBe(0);
    });

    it("counts two mentions of one person in one message once", async () => {
      const store = storeAs("owner");
      await send(store, GENERAL, "@Member and @Member", [
        person(0, 7, USERS.member),
        person(12, 7, USERS.member),
      ]);

      actAs(store, "member");

      expect((await summaryOf(store, GENERAL)).row.mentionCount).toBe(1);
    });

    it("clears the count when the person reads past the mention", async () => {
      const store = storeAs("owner");
      const sent = await send(store, GENERAL, "hi @Member", [
        person(3, 7, USERS.member),
      ]);
      actAs(store, "member");
      expect((await summaryOf(store, GENERAL)).row.mentionCount).toBe(1);

      const read = await chat(store, "PUT", `/spaces/${GENERAL}/read`, {
        body: { upToSequence: sent.body.sequence },
      });

      expect(read.status).toBe(200);
      expect((await summaryOf(store, GENERAL)).row.mentionCount).toBe(0);
    });

    it("counts a mention in a muted space but leaves it out of the badge total", async () => {
      const store = storeAs("owner");
      await send(store, GENERAL, "hi @Member", [person(3, 7, USERS.member)]);
      actAs(store, "member");
      await chat(store, "PUT", `/spaces/${GENERAL}/notification-level`, {
        body: { level: "none" },
      });

      const { row, totalMentions } = await summaryOf(store, GENERAL);

      expect(row.muted).toBe(true);
      expect(row.mentionCount).toBe(1);
      expect(totalMentions).toBe(0);
    });

    it("no longer counts a deleted message", async () => {
      const store = storeAs("owner");
      const sent = await send(store, GENERAL, "hi @Member", [
        person(3, 7, USERS.member),
      ]);
      await chat(store, "DELETE", `/messages/${sent.body.id}`, {
        query: `expectedVersion=${sent.body.version}`,
      });

      actAs(store, "member");

      expect((await summaryOf(store, GENERAL)).row.mentionCount).toBe(0);
    });

    it("carries the mention count on the mentioned person's unread frame", async () => {
      const store = storeAs("owner");
      const transport = new MockRealtimeTransport();
      const frames: RealtimeEnvelope[] = [];
      const stop = transport.subscribeScope(
        { kind: "user", id: USERS.member },
        (envelope) => {
          frames.push(envelope);
        },
      );
      try {
        await send(store, GENERAL, "hi @Member", [person(3, 7, USERS.member)]);

        const unread = frames.filter(
          (f) => f.payloadContract === "chat.unread.v1",
        );
        expect(unread).toHaveLength(1);
        expect(unread[0]!.payload).toMatchObject({
          spaceId: GENERAL,
          mentions: 1,
        });
      } finally {
        stop();
        transport.dispose();
      }
    });
  });

  describe("the mention picker (UC-CHT-G01 to G03)", () => {
    it("lists the members of the space first, then the rest of the Workspace marked not in the space", async () => {
      const store = storeAs("admin");

      const res = await chat(
        store,
        "GET",
        `/spaces/${LEADERSHIP}/mentionables`,
      );

      expect(res.status).toBe(200);
      const inSpace = res.body.people
        .filter((p: { inSpace: boolean }) => p.inSpace)
        .map((p: { userId: string }) => p.userId);
      expect(inSpace.sort()).toEqual([USERS.admin, USERS.member].sort());
      const others = res.body.people.filter(
        (p: { inSpace: boolean }) => !p.inSpace,
      );
      expect(others.map((p: { userId: string }) => p.userId)).toContain(
        USERS.owner,
      );
      expect(
        others.some((p: { userId: string }) => p.userId === USERS.viewer),
      ).toBe(false);
      expect(
        res.body.people
          .slice(0, 2)
          .every((p: { inSpace: boolean }) => p.inSpace),
      ).toBe(true);
      expect(res.body.memberCount).toBe(2);
      expect(res.body.broadcastConfirmAbove).toBe(
        CHAT_LIMITS.broadcastConfirmThreshold,
      );
    });

    it("tells who may broadcast: the owner yes, a Member and a Guest no", async () => {
      const owner = await chat(
        storeAs("owner"),
        "GET",
        `/spaces/${GENERAL}/mentionables`,
      );
      const member = await chat(
        storeAs("member"),
        "GET",
        `/spaces/${GENERAL}/mentionables`,
      );
      const guest = await chat(
        storeAs("viewer"),
        "GET",
        `/spaces/${GENERAL}/mentionables`,
      );

      expect(owner.body.canBroadcast).toBe(true);
      expect(member.body.canBroadcast).toBe(false);
      expect(guest.body.canBroadcast).toBe(false);
    });

    it("lists the people of the space only for a Guest", async () => {
      const res = await chat(
        storeAs("viewer"),
        "GET",
        `/spaces/${GENERAL}/mentionables`,
      );

      expect(res.status).toBe(200);
      expect(
        res.body.people.every((p: { inSpace: boolean }) => p.inSpace),
      ).toBe(true);
    });

    it("answers a space the caller is not in with the NotFound of a missing space", async () => {
      const privateSpace = await chat(
        storeAs("owner"),
        "GET",
        `/spaces/${LEADERSHIP}/mentionables`,
      );
      const missing = await chat(
        storeAs("owner"),
        "GET",
        `/spaces/${chatGuid("space", 999)}/mentionables`,
      );

      expect(privateSpace.status).toBe(404);
      expect(missing.status).toBe(404);
      expect(privateSpace.body.errorCode).toBe(missing.body.errorCode);
    });
  });
  describe("mentions in conversations", () => {
    async function group(store: Store) {
      const res = await chat(store, "POST", "/conversations", {
        body: { kind: "group", userIds: [USERS.member, USERS.admin] },
        idempotencyKey: key("mention-group"),
      });
      return res.body.conversation.id as string;
    }
    async function direct(store: Store) {
      const res = await chat(store, "POST", "/conversations", {
        body: { kind: "direct", userId: USERS.member },
        idempotencyKey: key("mention-direct"),
      });
      return res.body.conversation.id as string;
    }

    it("stores a mention of a participant and counts it for them", async () => {
      const store = storeAs("owner");
      const id = await group(store);

      const res = await send(store, id, "hi @Member", [
        person(3, 7, USERS.member),
      ]);

      expect(res.status).toBe(201);
      expect(res.body.mentions).toHaveLength(1);
      expect(res.body.mentionNotices).toEqual([]);
      actAs(store, "member");
      expect((await summaryOf(store, id)).row.mentionCount).toBe(1);
    });

    it("warns about a Workspace member who is not a participant and never counts it for them", async () => {
      const store = storeAs("owner");
      const id = await direct(store);

      const res = await send(store, id, "psst @Admin", [
        person(5, 6, USERS.admin),
      ]);

      expect(res.body.mentionNotices).toEqual([
        { code: "cannot-view", start: 5, length: 6, userId: USERS.admin },
      ]);
      actAs(store, "admin");
      const unread = await chat(store, "GET", "/me/unread");
      expect(unread.body.spaces.some((x: any) => x.spaceId === id)).toBe(false);
    });

    it("has no audience for @channel in a direct conversation, but broadcasts in a group", async () => {
      const store = storeAs("owner");
      const dm = await direct(store);
      const grp = await group(store);

      const inDirect = await send(store, dm, "hey @channel", [channelToken(4)]);
      const inGroup = await send(store, grp, "hey @channel", [channelToken(4)]);

      expect(inDirect.body.mentions).toEqual([]);
      expect(inDirect.body.mentionNotices[0].code).toBe("no-audience");
      expect(inGroup.body.mentions).toHaveLength(1);
    });

    it("lists the participants only in the picker of a conversation", async () => {
      const store = storeAs("owner");
      const id = await group(store);

      const res = await chat(store, "GET", `/spaces/${id}/mentionables`);

      expect(res.status).toBe(200);
      expect(res.body.people.every((p: any) => p.inSpace)).toBe(true);
      expect(res.body.people).toHaveLength(3);
      expect(res.body.canBroadcast).toBe(true);
    });
  });
});
