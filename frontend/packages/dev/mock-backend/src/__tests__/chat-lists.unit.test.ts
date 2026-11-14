import { describe, it, expect } from "vitest";
import {
  actAs,
  chat,
  GROUPS,
  storeAs,
  USERS,
  WORKSPACE_ID,
} from "./chat-test-kit";

type Row = { userId: string; blockedAt: string };

describe("Chat mock: the category names and the block list", () => {
  describe("GET /channel-groups (Chat.ChannelGroups.List)", () => {
    it("gives every member every category with its name, in sidebar order", async () => {
      const store = storeAs("member");

      const res = await chat(store, "GET", "/channel-groups");

      expect(res.status).toBe(200);
      const names = (res.body as { id: string; name: string }[]).map(
        (g) => g.name,
      );
      expect(names.length).toBeGreaterThanOrEqual(2);
      expect((res.body as { id: string }[]).map((g) => g.id)).toEqual(
        expect.arrayContaining([GROUPS.team, GROUPS.projects]),
      );
      expect(res.body[0]).toEqual(
        expect.objectContaining({
          name: expect.any(String),
          position: expect.any(Number),
          version: expect.any(Number),
        }),
      );
    });

    it("shows a category created a moment ago with its real name", async () => {
      const store = storeAs("owner");
      const created = await chat(store, "POST", "/channel-groups", {
        idempotencyKey: "00000000-0000-4000-8000-000000000a01",
        body: { name: "Customers" },
      });
      expect(created.status).toBe(201);

      const res = await chat(store, "GET", "/channel-groups");

      expect((res.body as { name: string }[]).map((g) => g.name)).toContain(
        "Customers",
      );
    });

    it("gives a Guest only the categories that hold a channel they are in", async () => {
      const store = storeAs("member");
      const row = store
        .getWorkspaceMembers(WORKSPACE_ID)
        .find((m) => m.userId === USERS.member)!;
      row.role = "guest";
      const all = (await chat(storeAs("owner"), "GET", "/channel-groups"))
        .body as { id: string }[];

      const res = await chat(store, "GET", "/channel-groups");

      expect(res.status).toBe(200);
      const guestIds = (res.body as { id: string }[]).map((g) => g.id);
      const inSpace = store.chat
        .spacesOfWorkspace(WORKSPACE_ID)
        .filter(
          (s) =>
            s.groupId !== null &&
            store.chat.activeMembershipOf(s.id, USERS.member) !== undefined,
        )
        .map((s) => s.groupId);
      expect(new Set(guestIds)).toEqual(new Set(inSpace));
      expect(guestIds.length).toBeLessThanOrEqual(all.length);
    });
  });

  describe("GET /me/blocks (Chat.Blocks.List)", () => {
    async function block(
      store: ReturnType<typeof storeAs>,
      userId: string,
      blocked = true,
    ) {
      const res = await chat(store, "PUT", "/me/blocks", {
        body: { userId, blocked },
      });
      expect(res.status).toBe(200);
    }

    it("is empty until the caller blocks someone, then lists them newest first", async () => {
      const store = storeAs("member");
      expect((await chat(store, "GET", "/me/blocks")).body).toEqual([]);

      await block(store, USERS.owner);
      await block(store, USERS.admin);

      const res = await chat(store, "GET", "/me/blocks");
      expect(res.status).toBe(200);
      expect((res.body as Row[]).map((r) => r.userId)).toEqual([
        USERS.admin,
        USERS.owner,
      ]);
      expect(Date.parse((res.body as Row[])[0]!.blockedAt)).toBeGreaterThan(0);
    });

    it("drops a person after an unblock and keeps the first time of a repeated block", async () => {
      const store = storeAs("member");
      await block(store, USERS.owner);
      const first = ((await chat(store, "GET", "/me/blocks")).body as Row[])[0]!
        .blockedAt;
      await block(store, USERS.owner);
      expect(
        ((await chat(store, "GET", "/me/blocks")).body as Row[])[0]!.blockedAt,
      ).toBe(first);

      await block(store, USERS.owner, false);

      expect((await chat(store, "GET", "/me/blocks")).body).toEqual([]);
    });

    it("never lists another person's blocks, and the blocked person is not told", async () => {
      const store = storeAs("member");
      await block(store, USERS.owner);

      actAs(store, "owner");
      const asBlocked = await chat(store, "GET", "/me/blocks");

      expect(asBlocked.body).toEqual([]);
    });

    it("gives a Guest only the blocks of people they share a space with", async () => {
      const store = storeAs("member");
      await block(store, USERS.owner);
      const row = store
        .getWorkspaceMembers(WORKSPACE_ID)
        .find((m) => m.userId === USERS.member)!;
      row.role = "guest";
      const shared = store.chat
        .spacesOfWorkspace(WORKSPACE_ID)
        .some(
          (s) =>
            store.chat.activeMembershipOf(s.id, USERS.member) !== undefined &&
            store.chat.activeMembershipOf(s.id, USERS.owner) !== undefined,
        );

      const res = await chat(store, "GET", "/me/blocks");

      expect((res.body as Row[]).length).toBe(shared ? 1 : 0);
    });
  });
});
