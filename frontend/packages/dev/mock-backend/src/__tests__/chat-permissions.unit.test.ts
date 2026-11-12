import { describe, it, expect } from "vitest";
import { SPACES, USERS, actAs, chat, key, storeAs } from "./chat-test-kit";

const GENERAL = SPACES.general;

type Store = ReturnType<typeof storeAs>;

const permissionsPath = `/channels/${GENERAL}/permissions`;

async function read(store: Store) {
  return chat(store, "GET", permissionsPath);
}

async function set(store: Store, entries: unknown[], expectedVersion?: number) {
  const version = expectedVersion ?? store.chat.spaces.get(GENERAL)!.version;
  return chat(store, "PUT", permissionsPath, {
    body: { entries, expectedVersion: version },
  });
}

const deny = (subject: string, action: string) => ({
  subject,
  action,
  effect: "deny",
});

async function send(store: Store, label: string) {
  return chat(store, "POST", `/spaces/${GENERAL}/messages`, {
    body: { content: `hello ${label}`, clientMessageId: `cm-${label}` },
    idempotencyKey: key(label),
  });
}

describe("MFB-CHAT-S3.1: Chat mock handlers (channel permissions, moderation)", () => {
  describe("read and set (UC-CHT-H01)", () => {
    it("reads an empty set with the channel version", async () => {
      const store = storeAs("member");
      const res = await read(store);
      expect(res.status).toBe(200);
      expect(res.body).toEqual({
        channelId: GENERAL,
        entries: [],
        version: store.chat.spaces.get(GENERAL)!.version,
      });
    });

    it("replaces the whole set, in enum order, and bumps the version", async () => {
      const store = storeAs("owner");
      const before = store.chat.spaces.get(GENERAL)!.version;
      const res = await set(store, [
        deny("guest", "post"),
        deny("member", "react"),
        deny("member", "post"),
      ]);
      expect(res.status).toBe(200);
      expect(res.body.version).toBe(before + 1);
      expect(
        res.body.entries.map((e: { subject: string; action: string }) => [
          e.subject,
          e.action,
        ]),
      ).toEqual([
        ["member", "post"],
        ["member", "react"],
        ["guest", "post"],
      ]);

      const fewer = await set(store, [deny("guest", "post")]);
      expect(fewer.body.entries).toHaveLength(1);
    });

    it("a deny beats an allow of the same pair", async () => {
      const store = storeAs("owner");
      const res = await set(store, [
        { subject: "member", action: "post", effect: "allow" },
        deny("member", "post"),
      ]);
      expect(res.status).toBe(200);
      expect(res.body.entries).toEqual([deny("member", "post")]);
    });

    it("a set that changes nothing keeps the version", async () => {
      const store = storeAs("owner");
      const first = await set(store, [deny("member", "pin")]);
      const again = await set(store, [deny("member", "pin")]);
      expect(again.status).toBe(200);
      expect(again.body.version).toBe(first.body.version);
    });

    it("clears the set with DELETE and expectedVersion in the query", async () => {
      const store = storeAs("owner");
      const made = await set(store, [deny("member", "post")]);
      const res = await chat(store, "DELETE", permissionsPath, {
        query: `expectedVersion=${made.body.version}`,
      });
      expect(res.status).toBe(200);
      expect(res.body.entries).toEqual([]);
      expect(res.body.version).toBe(made.body.version + 1);
    });

    it("a stale expectedVersion is 412", async () => {
      const store = storeAs("owner");
      const res = await set(store, [deny("member", "post")], 1);
      expect(res.status).toBe(412);
      const gone = await chat(store, "DELETE", permissionsPath, {
        query: "expectedVersion=1",
      });
      expect(gone.status).toBe(412);
    });

    it("a member who is not a manager gets 403, a Guest 403, a missing channel 404", async () => {
      const store = storeAs("member");
      expect((await set(store, [deny("guest", "post")])).status).toBe(403);
      actAs(store, "viewer");
      expect((await set(store, [deny("guest", "post")])).status).toBe(403);
      actAs(store, "owner");
      const missing = await chat(
        store,
        "PUT",
        `/channels/c1000000-0000-4000-8000-0000000000ff/permissions`,
        { body: { entries: [], expectedVersion: 1 } },
      );
      expect(missing.status).toBe(404);
    });

    it("a channel manager who is a Member can set it; a Workspace Admin who is not a manager can too", async () => {
      const store = storeAs("member");
      store.chat.memberships.get(`${GENERAL}|${USERS.member}`)!.role =
        "manager";
      expect((await set(store, [deny("guest", "post")])).status).toBe(200);
      actAs(store, "admin");
      expect((await set(store, [])).status).toBe(200);
    });

    it("refuses an allow above the role (422), a manager invite deny (422) and a bad value (400)", async () => {
      const store = storeAs("owner");
      const exceeds = await set(store, [
        { subject: "guest", action: "pin", effect: "allow" },
      ]);
      expect(exceeds.status).toBe(422);
      expect(exceeds.body.errorCode).toBe("permission-exceeds-role");
      const lockout = await set(store, [deny("manager", "invite")]);
      expect(lockout.status).toBe(422);
      expect(lockout.body.errorCode).toBe("permission-lockout");
      expect((await set(store, [deny("owner", "post")])).status).toBe(400);
      expect((await set(store, [deny("member", "fly")])).status).toBe(400);
    });

    it("an archived channel is 409", async () => {
      const store = storeAs("owner");
      const space = store.chat.spaces.get(GENERAL)!;
      space.archivedAt = "2026-01-01T00:00:00.000Z";
      const res = await set(store, [deny("member", "post")]);
      expect(res.status).toBe(409);
      expect(res.body.errorCode).toBe("channel-archived");
    });

    it("bumps the channel version once per change", async () => {
      const store = storeAs("owner");
      const before = store.chat.spaces.get(GENERAL)!.version;
      await set(store, [deny("member", "post")]);
      expect(store.chat.spaces.get(GENERAL)!.version).toBe(before + 1);
    });
  });

  describe("overrides are honoured (UC-CHT-H01)", () => {
    it("a read-only channel refuses a Member's post (403) and keeps the manager, the Admin and the Owner posting", async () => {
      const store = storeAs("owner");
      await set(store, [deny("member", "post"), deny("guest", "post")]);

      actAs(store, "member");
      expect((await send(store, "m1")).status).toBe(403);
      actAs(store, "viewer");
      expect((await send(store, "v1")).status).toBe(403);
      actAs(store, "admin");
      expect((await send(store, "a1")).status).toBe(201);
      actAs(store, "owner");
      expect((await send(store, "o1")).status).toBe(201);
    });

    it("the channel view carries the caller's capabilities", async () => {
      const store = storeAs("owner");
      await set(store, [deny("member", "post"), deny("member", "react")]);

      actAs(store, "member");
      const asMember = await chat(store, "GET", `/channels/${GENERAL}`);
      expect(asMember.body.channel.capabilities).toMatchObject({
        canPost: false,
        canReact: false,
        canAttach: true,
        canModerate: false,
        canManagePermissions: false,
      });
      actAs(store, "owner");
      const asOwner = await chat(store, "GET", `/channels/${GENERAL}`);
      expect(asOwner.body.channel.capabilities).toMatchObject({
        canPost: true,
        canModerate: true,
        canManagePermissions: true,
      });
    });

    it("a deny of react and of pin is 403 on the matching action", async () => {
      const store = storeAs("owner");
      const target = store.chat
        .messagesOf(GENERAL)
        .find((m) => m.kind === "user" && m.deletedAt === null)!;
      await set(store, [deny("member", "react"), deny("member", "pin")]);

      actAs(store, "member");
      const react = await chat(
        store,
        "PUT",
        `/messages/${target.id}/reactions/${encodeURIComponent("👍")}`,
      );
      expect(react.status).toBe(403);
      expect(
        (await chat(store, "PUT", `/messages/${target.id}/pin`)).status,
      ).toBe(403);
      actAs(store, "admin");
      expect(
        (await chat(store, "PUT", `/messages/${target.id}/pin`)).status,
      ).toBe(201);
    });
  });

  describe("moderator delete (UC-CHT-H03)", () => {
    function aMemberMessage(store: Store) {
      return store.chat
        .messagesOf(GENERAL)
        .find(
          (m) =>
            m.kind === "user" &&
            m.deletedAt === null &&
            m.authorId === USERS.member,
        )!;
    }

    async function moderate(
      store: Store,
      messageId: string,
      reason: string,
      expectedVersion: number,
    ) {
      return chat(store, "POST", `/messages/${messageId}/moderation`, {
        body: { reason, expectedVersion },
      });
    }

    it("tombstones another person's message and shows it as removed by a moderator", async () => {
      const store = storeAs("admin");
      const target = aMemberMessage(store);
      const res = await moderate(store, target.id, "spam", target.version);
      expect(res.status).toBe(204);

      const page = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: `around=${target.sequence}&limit=5`,
      });
      const view = (page.body.items as { id: string }[]).find(
        (m) => m.id === target.id,
      );
      expect(view).toMatchObject({
        deleted: true,
        content: null,
        removedByModerator: true,
      });
    });

    it("an author's own delete is not a moderator removal", async () => {
      const store = storeAs("member");
      const target = aMemberMessage(store);
      await chat(store, "DELETE", `/messages/${target.id}`, {
        query: `expectedVersion=${target.version}`,
      });
      actAs(store, "owner");
      const page = await chat(store, "GET", `/spaces/${GENERAL}/messages`, {
        query: `around=${target.sequence}&limit=5`,
      });
      const view = (page.body.items as { id: string }[]).find(
        (m) => m.id === target.id,
      );
      expect(view).toMatchObject({ deleted: true, removedByModerator: false });
    });

    it("needs the reason (1 to 500) and the moderate action: a Member and a Guest get 403", async () => {
      const store = storeAs("admin");
      const target = aMemberMessage(store);
      expect(
        (await moderate(store, target.id, "  ", target.version)).status,
      ).toBe(400);
      expect(
        (await moderate(store, target.id, "x".repeat(501), target.version))
          .status,
      ).toBe(400);
      actAs(store, "member");
      expect(
        (await moderate(store, target.id, "no", target.version)).status,
      ).toBe(403);
      actAs(store, "viewer");
      expect(
        (await moderate(store, target.id, "no", target.version)).status,
      ).toBe(403);
    });

    it("a stale version is 412, a repeat is 204, your own message is 400, a system message is 409", async () => {
      const store = storeAs("admin");
      const target = aMemberMessage(store);
      const version = target.version;
      expect((await moderate(store, target.id, "r", 99)).status).toBe(412);
      expect((await moderate(store, target.id, "r", version)).status).toBe(204);
      expect((await moderate(store, target.id, "r", version + 1)).status).toBe(
        204,
      );

      const own = store.chat
        .messagesOf(GENERAL)
        .find(
          (m) =>
            m.kind === "user" &&
            m.deletedAt === null &&
            m.authorId === USERS.admin,
        )!;
      const self = await moderate(store, own.id, "r", own.version);
      expect(self.status).toBe(400);
      expect(self.body.ruleCode).toBe("self-target");

      const system = store.chat
        .messagesOf(GENERAL)
        .find((m) => m.kind === "system")!;
      expect(
        (await moderate(store, system.id, "r", system.version)).status,
      ).toBe(409);
    });

    it("a moderator does not read a private channel by role (404)", async () => {
      const store = storeAs("owner");
      const hidden = store.chat
        .messagesOf(SPACES.leadership)
        .find((m) => m.kind === "user" && m.deletedAt === null)!;
      const res = await moderate(store, hidden.id, "r", hidden.version);
      expect(res.status).toBe(404);
    });
  });
});
