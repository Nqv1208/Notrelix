import { describe, it, expect } from "vitest";
import { USERS, actAs, chat, key, storeAs } from "./chat-test-kit";

type Store = ReturnType<typeof storeAs>;

async function startDirect(store: Store, userId: string, label = userId) {
  return chat(store, "POST", "/conversations", {
    body: { kind: "direct", userId },
    idempotencyKey: key(`direct-${label}-${store.getCurrentUser().id}`),
  });
}

async function startGroup(
  store: Store,
  userIds: string[],
  label: string,
  name?: string,
) {
  return chat(store, "POST", "/conversations", {
    body: { kind: "group", userIds, ...(name ? { name } : {}) },
    idempotencyKey: key(`group-${label}`),
  });
}

const listConversations = (store: Store) =>
  chat(store, "GET", "/conversations");

describe("MFB-CHAT-S2.1: Chat mock handlers (conversations)", () => {
  describe("start a direct conversation (UC-CHT-E01)", () => {
    it("creates the pair's conversation (201) with both people as participants", async () => {
      const store = storeAs("owner");

      const res = await startDirect(store, USERS.member);

      expect(res.status).toBe(201);
      expect(res.body.created).toBe(true);
      expect(res.body.skipped).toEqual([]);
      expect(res.body.conversation).toMatchObject({
        kind: "direct",
        name: null,
        isHidden: false,
        unreadCount: 0,
        lastMessage: null,
      });
      expect(
        res.body.conversation.participants.map((p: any) => p.userId).sort(),
      ).toEqual([USERS.member, USERS.owner].sort());
    });

    it("returns the same conversation (200) when either person starts it again", async () => {
      const store = storeAs("owner");
      const first = await startDirect(store, USERS.member);

      const again = await chat(store, "POST", "/conversations", {
        body: { kind: "direct", userId: USERS.member },
        idempotencyKey: key("direct-again"),
      });
      actAs(store, "member");
      const reverse = await startDirect(store, USERS.owner);

      expect(again.status).toBe(200);
      expect(again.body.created).toBe(false);
      expect(again.body.conversation.id).toBe(first.body.conversation.id);
      expect(reverse.status).toBe(200);
      expect(reverse.body.conversation.id).toBe(first.body.conversation.id);
      expect(
        Array.from(store.chat.spaces.values()).filter(
          (s) => s.kind === "direct",
        ),
      ).toHaveLength(1);
    });

    it("replays the stored answer for the same Idempotency-Key and refuses another payload with 409", async () => {
      const store = storeAs("owner");
      const k = key("replay");
      const first = await chat(store, "POST", "/conversations", {
        body: { kind: "direct", userId: USERS.member },
        idempotencyKey: k,
      });

      const replay = await chat(store, "POST", "/conversations", {
        body: { kind: "direct", userId: USERS.member },
        idempotencyKey: k,
      });
      const other = await chat(store, "POST", "/conversations", {
        body: { kind: "direct", userId: USERS.admin },
        idempotencyKey: k,
      });

      expect(replay.status).toBe(first.status);
      expect(replay.body).toEqual(first.body);
      expect(other.status).toBe(409);
    });

    it("refuses yourself with 400 self-target, a missing key and a bad kind with 400", async () => {
      const store = storeAs("owner");

      const self = await startDirect(store, USERS.owner);
      const noKey = await chat(store, "POST", "/conversations", {
        body: { kind: "direct", userId: USERS.member },
      });
      const badKind = await chat(store, "POST", "/conversations", {
        body: { kind: "channel", userId: USERS.member },
        idempotencyKey: key("bad-kind"),
      });
      const noUser = await chat(store, "POST", "/conversations", {
        body: { kind: "direct" },
        idempotencyKey: key("no-user"),
      });

      expect(self.status).toBe(400);
      expect(self.body.ruleCode).toBe("self-target");
      expect(noKey.status).toBe(400);
      expect(badKind.status).toBe(400);
      expect(noUser.status).toBe(400);
    });

    it("answers 404 for a person who is not in the Workspace", async () => {
      const store = storeAs("owner");

      const res = await startDirect(store, "mock-user-nobody");

      expect(res.status).toBe(404);
    });

    it("answers 404 when either person blocked the other", async () => {
      const store = storeAs("member");
      await chat(store, "PUT", "/me/blocks", {
        body: { userId: USERS.owner, blocked: true },
      });

      const fromBlocker = await startDirect(store, USERS.owner);
      actAs(store, "owner");
      const fromBlocked = await startDirect(store, USERS.member);

      expect(fromBlocker.status).toBe(404);
      expect(fromBlocked.status).toBe(404);
    });

    it("starts again after an unblock", async () => {
      const store = storeAs("member");
      await chat(store, "PUT", "/me/blocks", {
        body: { userId: USERS.owner, blocked: true },
      });
      await chat(store, "PUT", "/me/blocks", {
        body: { userId: USERS.owner, blocked: false },
      });
      actAs(store, "owner");

      const res = await startDirect(store, USERS.member);

      expect(res.status).toBe(201);
    });
  });

  describe("start a group conversation (UC-CHT-E02)", () => {
    it("creates a group with the caller and the named people and an optional name", async () => {
      const store = storeAs("owner");

      const res = await startGroup(
        store,
        [USERS.member, USERS.admin],
        "named",
        "Launch crew",
      );

      expect(res.status).toBe(201);
      expect(res.body.conversation).toMatchObject({
        kind: "group",
        name: "Launch crew",
      });
      expect(res.body.conversation.participants).toHaveLength(3);
    });

    it("lists each of the three people the group and no other Admin", async () => {
      const store = storeAs("owner");
      await startGroup(store, [USERS.member, USERS.viewer], "privacy");

      const mine = await listConversations(store);
      actAs(store, "member");
      const theirs = await listConversations(store);
      actAs(store, "admin");
      const admin = await listConversations(store);

      expect(mine.body).toHaveLength(1);
      expect(theirs.body).toHaveLength(1);
      expect(admin.body).toEqual([]);
    });

    it("reports a person outside the Workspace as skipped and refuses a group left under three", async () => {
      const store = storeAs("owner");

      const enough = await startGroup(
        store,
        [USERS.member, USERS.admin, "mock-user-nobody"],
        "skip",
      );
      const tooSmall = await startGroup(
        store,
        [USERS.member, "mock-user-nobody"],
        "small",
      );

      expect(enough.status).toBe(201);
      expect(enough.body.skipped).toEqual(["mock-user-nobody"]);
      expect(tooSmall.status).toBe(400);
      expect(tooSmall.body.ruleCode).toBe("group-too-small");
    });

    it("refuses a group over the cap of eight with 422 limit-reached", async () => {
      const store = storeAs("owner");
      const eight = Array.from({ length: 8 }, (_, i) => `mock-user-x${i}`);

      const res = await startGroup(store, eight, "cap");

      expect(res.status).toBe(422);
      expect(res.body.errorCode).toBe("limit-reached");
    });

    it("needs at least two other people", async () => {
      const store = storeAs("owner");

      const res = await startGroup(store, [USERS.member], "one");

      expect(res.status).toBe(400);
    });
  });

  describe("add people (UC-CHT-E03)", () => {
    it("adds a person, announces it, and leaves a person already in as no change", async () => {
      const store = storeAs("owner");
      const group = await startGroup(
        store,
        [USERS.member, USERS.admin],
        "add-one",
      );
      const id = group.body.conversation.id;

      const res = await chat(
        store,
        "POST",
        `/conversations/${id}/participants`,
        {
          body: { userIds: [USERS.viewer, USERS.member, "mock-user-nobody"] },
        },
      );

      expect(res.status).toBe(200);
      expect(res.body).toEqual({
        added: [USERS.viewer],
        skipped: ["mock-user-nobody"],
      });
      expect(store.chat.messagesOf(id).at(-1)).toMatchObject({
        kind: "system",
      });
    });

    it("refuses a direct conversation (400 kind-not-supported) and a non-participant (404)", async () => {
      const store = storeAs("owner");
      const direct = await startDirect(store, USERS.member);
      const group = await startGroup(store, [USERS.member, USERS.admin], "np");

      const onDirect = await chat(
        store,
        "POST",
        `/conversations/${direct.body.conversation.id}/participants`,
        { body: { userIds: [USERS.admin] } },
      );
      actAs(store, "commenter");
      const outsider = await chat(
        store,
        "POST",
        `/conversations/${group.body.conversation.id}/participants`,
        { body: { userIds: [USERS.owner] } },
      );

      expect(onDirect.status).toBe(400);
      expect(onDirect.body.ruleCode).toBe("kind-not-supported");
      expect(outsider.status).toBe(404);
    });

    it("refuses the ninth person with 422 limit-reached", async () => {
      const store = storeAs("owner");
      const group = await startGroup(
        store,
        [USERS.member, USERS.admin],
        "full",
      );
      const id = group.body.conversation.id;
      const workspaceId = store.chat.spaces.get(id)!.workspaceId;
      // Fill the group to the cap with other people.
      for (let i = 3; i < 8; i += 1) {
        store.chat.memberships.set(`${id}|filler-${i}`, {
          spaceId: id,
          workspaceId,
          userId: `filler-${i}`,
          role: "member",
          notificationLevel: "all",
          mutedUntil: null,
          readSequence: 0,
          mentionCount: 0,
          joinedAt: "2026-01-01T00:00:00.000Z",
          leftAt: null,
          hiddenAt: null,
        });
      }

      const res = await chat(
        store,
        "POST",
        `/conversations/${id}/participants`,
        {
          body: { userIds: [USERS.viewer] },
        },
      );

      expect(res.status).toBe(422);
      expect(res.body.errorCode).toBe("limit-reached");
    });
  });

  describe("leave (UC-CHT-E04)", () => {
    it("leaves a group with a system message for the others and drops it from the list", async () => {
      const store = storeAs("owner");
      const group = await startGroup(
        store,
        [USERS.member, USERS.admin],
        "leave",
      );
      const id = group.body.conversation.id;
      actAs(store, "member");

      const res = await chat(
        store,
        "DELETE",
        `/conversations/${id}/participants/me`,
      );
      const list = await listConversations(store);

      expect(res.status).toBe(204);
      expect(list.body).toEqual([]);
      expect(store.chat.messagesOf(id).at(-1)).toMatchObject({
        kind: "system",
      });
    });

    it("answers 404 when the caller left already (a second leave)", async () => {
      const store = storeAs("owner");
      const group = await startGroup(
        store,
        [USERS.member, USERS.admin],
        "twice",
      );
      const id = group.body.conversation.id;
      actAs(store, "member");
      await chat(store, "DELETE", `/conversations/${id}/participants/me`);

      const again = await chat(
        store,
        "DELETE",
        `/conversations/${id}/participants/me`,
      );

      expect(again.status).toBe(404);
    });

    it("refuses leaving a direct conversation with 400 kind-not-supported", async () => {
      const store = storeAs("owner");
      const direct = await startDirect(store, USERS.member);

      const res = await chat(
        store,
        "DELETE",
        `/conversations/${direct.body.conversation.id}/participants/me`,
      );

      expect(res.status).toBe(400);
      expect(res.body.ruleCode).toBe("kind-not-supported");
    });

    it("closes the group when the last participant leaves", async () => {
      const store = storeAs("owner");
      const group = await startGroup(
        store,
        [USERS.member, USERS.admin],
        "close",
      );
      const id = group.body.conversation.id;
      for (const persona of ["owner", "member", "admin"] as const) {
        actAs(store, persona);
        await chat(store, "DELETE", `/conversations/${id}/participants/me`);
      }

      expect(store.chat.spaces.get(id)!.deletedAt).not.toBeNull();
    });
  });

  describe("list (UC-CHT-E05)", () => {
    it("orders by last activity, with the preview and the caller's unread count", async () => {
      const store = storeAs("owner");
      const a = await startDirect(store, USERS.member);
      const b = await startDirect(store, USERS.admin);
      actAs(store, "member");
      await chat(store, "POST", `/spaces/${a.body.conversation.id}/messages`, {
        body: { content: "hello there", clientMessageId: "cm-1" },
        idempotencyKey: key("msg-1"),
      });
      actAs(store, "owner");

      const list = await listConversations(store);

      expect(list.status).toBe(200);
      expect(list.body.map((c: any) => c.id)).toEqual([
        a.body.conversation.id,
        b.body.conversation.id,
      ]);
      expect(list.body[0]).toMatchObject({
        unreadCount: 1,
        lastMessage: { text: "hello there", authorId: USERS.member },
      });
    });

    it("keeps conversations out of the channel list and refuses channel commands on them", async () => {
      const store = storeAs("owner");
      const direct = await startDirect(store, USERS.member);
      const id = direct.body.conversation.id;

      const channels = await chat(store, "GET", "/channels");
      const get = await chat(store, "GET", `/channels/${id}`);
      const join = await chat(store, "POST", `/channels/${id}/members/me`);

      expect(channels.body.map((c: any) => c.id)).not.toContain(id);
      expect(get.status).toBe(404);
      expect(join.status).toBe(404);
    });

    it("refuses a limit outside 1 to 100", async () => {
      const store = storeAs("owner");

      const res = await chat(store, "GET", "/conversations", {
        query: "limit=0",
      });

      expect(res.status).toBe(400);
    });
  });

  describe("hide and restore (UC-CHT-E07)", () => {
    it("hides for the caller only and a new message restores it for everyone", async () => {
      const store = storeAs("owner");
      const direct = await startDirect(store, USERS.member);
      const id = direct.body.conversation.id;

      const hide = await chat(store, "PUT", `/conversations/${id}/visibility`, {
        body: { hidden: true },
      });
      const hiddenList = await listConversations(store);
      actAs(store, "member");
      const otherList = await listConversations(store);
      await chat(store, "POST", `/spaces/${id}/messages`, {
        body: { content: "ping", clientMessageId: "cm-restore" },
        idempotencyKey: key("msg-restore"),
      });
      actAs(store, "owner");
      const restored = await listConversations(store);

      expect(hide.status).toBe(200);
      expect(hide.body).toEqual({ conversationId: id, hidden: true });
      expect(hiddenList.body).toEqual([]);
      expect(otherList.body).toHaveLength(1);
      expect(restored.body).toHaveLength(1);
      expect(restored.body[0].isHidden).toBe(false);
    });

    it("shows it again on request, is idempotent, and starting it again restores it", async () => {
      const store = storeAs("owner");
      const direct = await startDirect(store, USERS.member);
      const id = direct.body.conversation.id;
      await chat(store, "PUT", `/conversations/${id}/visibility`, {
        body: { hidden: true },
      });
      const twice = await chat(
        store,
        "PUT",
        `/conversations/${id}/visibility`,
        {
          body: { hidden: true },
        },
      );
      const restart = await chat(store, "POST", "/conversations", {
        body: { kind: "direct", userId: USERS.member },
        idempotencyKey: key("restart"),
      });
      const show = await chat(store, "PUT", `/conversations/${id}/visibility`, {
        body: { hidden: false },
      });

      expect(twice.body.hidden).toBe(true);
      expect(restart.status).toBe(200);
      expect(restart.body.conversation.isHidden).toBe(false);
      expect(show.body.hidden).toBe(false);
    });

    it("answers 404 to a non-participant, even the Workspace owner, and 400 without hidden", async () => {
      const store = storeAs("member");
      const direct = await startDirect(store, USERS.admin);
      const id = direct.body.conversation.id;
      actAs(store, "owner");

      const owner = await chat(
        store,
        "PUT",
        `/conversations/${id}/visibility`,
        {
          body: { hidden: true },
        },
      );
      const messages = await chat(store, "GET", `/spaces/${id}/messages`);
      actAs(store, "admin");
      const missing = await chat(
        store,
        "PUT",
        `/conversations/${id}/visibility`,
        {
          body: {},
        },
      );

      expect(owner.status).toBe(404);
      expect(messages.status).toBe(404);
      expect(missing.status).toBe(400);
    });
  });

  describe("block (UC-CHT-E06)", () => {
    it("is idempotent in both directions and answers the person and the state", async () => {
      const store = storeAs("owner");

      const block = await chat(store, "PUT", "/me/blocks", {
        body: { userId: USERS.member, blocked: true },
      });
      const again = await chat(store, "PUT", "/me/blocks", {
        body: { userId: USERS.member, blocked: true },
      });
      const unblock = await chat(store, "PUT", "/me/blocks", {
        body: { userId: USERS.member, blocked: false },
      });
      const unblockAgain = await chat(store, "PUT", "/me/blocks", {
        body: { userId: USERS.member, blocked: false },
      });

      expect(block.body).toEqual({ userId: USERS.member, blocked: true });
      expect(again.status).toBe(200);
      expect(unblock.body).toEqual({ userId: USERS.member, blocked: false });
      expect(unblockAgain.status).toBe(200);
    });

    it("refuses yourself with 400 self-target, a stranger with 404 and a missing field with 400", async () => {
      const store = storeAs("owner");

      const self = await chat(store, "PUT", "/me/blocks", {
        body: { userId: USERS.owner, blocked: true },
      });
      const stranger = await chat(store, "PUT", "/me/blocks", {
        body: { userId: "mock-user-nobody", blocked: true },
      });
      const noFlag = await chat(store, "PUT", "/me/blocks", {
        body: { userId: USERS.member },
      });

      expect(self.status).toBe(400);
      expect(self.body.ruleCode).toBe("self-target");
      expect(stranger.status).toBe(404);
      expect(noFlag.status).toBe(400);
    });

    it("does not hide channel messages of the blocked person", async () => {
      const store = storeAs("owner");
      await chat(store, "PUT", "/me/blocks", {
        body: { userId: USERS.member, blocked: true },
      });

      const channels = await chat(store, "GET", "/channels");

      expect(channels.status).toBe(200);
      expect(channels.body.length).toBeGreaterThan(0);
    });
  });
});
