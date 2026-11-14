import { describe, it, expect } from "vitest";
import { MockRealtimeTransport } from "../realtime/mock-realtime-transport";
import type { RealtimeEnvelope } from "@notrelix/realtime";
import { mockIds } from "../state/mock-ids";
import { INVOICE_DOC_ID } from "../state/mock-docs-invoice";
import { LINK_KIND } from "../contexts/chat/links.handlers";
import {
  SPACES,
  USERS,
  WORKSPACE_ID,
  actAs,
  chat,
  storeAs,
} from "./chat-test-kit";

type Store = ReturnType<typeof storeAs>;

const GENERAL = SPACES.general;
const linkPath = `/channels/${GENERAL}/link`;
const BOARD = mockIds.boards.roadmap;

const version = (store: Store) => store.chat.spaces.get(GENERAL)!.version;

function link(
  store: Store,
  kind: string,
  resourceId: string,
  expected?: number,
) {
  return chat(store, "PUT", linkPath, {
    body: {
      kind,
      resourceId,
      expectedVersion: expected ?? version(store),
    },
  });
}

const linked = (store: Store, kind: string, id: string) =>
  chat(store, "GET", "/links/linked-channels", {
    query: `kind=${kind}&id=${id}`,
  });

describe("MFB-CHAT-S3.5: Chat mock handlers (link a channel to a Board or Page, UC-CHT-H10)", () => {
  it("links a Board and a Page the manager can view, and the channel view carries the title", async () => {
    const store = storeAs("owner");
    const before = version(store);

    const res = await link(store, LINK_KIND.board, BOARD);
    expect(res.status).toBe(200);
    expect(res.body.version).toBe(before + 1);
    expect(res.body.link).toEqual({
      kind: LINK_KIND.board,
      resourceId: BOARD,
      accessible: true,
      title: store.getBoard(BOARD)!.title,
    });

    // A channel has one link: the second replaces the first.
    const page = await link(store, LINK_KIND.page, INVOICE_DOC_ID);
    expect(page.body.link).toMatchObject({
      kind: LINK_KIND.page,
      title: store.getPage(INVOICE_DOC_ID)!.title,
    });
    const read = await chat(store, "GET", `/channels/${GENERAL}`);
    expect(read.body.channel?.link ?? read.body.link).toMatchObject({
      kind: LINK_KIND.page,
    });
  });

  it("linking the same resource again changes nothing", async () => {
    const store = storeAs("owner");
    const first = await link(store, LINK_KIND.board, BOARD);
    const again = await link(store, LINK_KIND.board, BOARD, first.body.version);
    expect(again.status).toBe(200);
    expect(again.body.version).toBe(first.body.version);
  });

  it("refuses a manager who cannot view the resource (403), a stale version (412), a Team and a bad kind (400)", async () => {
    const store = storeAs("owner");
    const board = store.getBoard(BOARD)!;
    board.visibility = "private";
    for (const member of store.getBoardMembers(BOARD)) {
      expect(member.userId).not.toBe(store.getCurrentUser().id);
    }
    const hidden = await link(store, LINK_KIND.board, BOARD);
    expect(hidden.status).toBe(403);
    const missing = await link(store, LINK_KIND.page, "mock-page-nobody");
    expect(missing.status).toBe(403);
    expect(store.chat.spaces.get(GENERAL)!.link ?? null).toBeNull();

    board.visibility = "workspace";
    const stale = await link(store, LINK_KIND.board, BOARD, version(store) + 5);
    expect(stale.status).toBe(412);

    expect((await link(store, LINK_KIND.team, "mock-team-1")).status).toBe(400);
    expect((await link(store, "chat.message", "mock-x")).status).toBe(400);
  });

  it("is for a manager, an Owner or an Admin: a plain member gets 403 and a Guest gets 403", async () => {
    const store = storeAs("owner");
    actAs(store, "member");
    expect((await link(store, LINK_KIND.board, BOARD)).status).toBe(403);

    actAs(store, "owner");
    const row = store
      .getWorkspaceMembers(WORKSPACE_ID)
      .find((m) => m.userId === USERS.owner)!;
    // A Guest is never a manager and the action is not theirs.
    const original = row.role;
    row.role = "guest";
    expect((await link(store, LINK_KIND.board, BOARD)).status).toBe(403);
    row.role = original;
  });

  it("unlinks, with the version in the query, and the stale version is 412", async () => {
    const store = storeAs("owner");
    const set = await link(store, LINK_KIND.board, BOARD);

    const stale = await chat(store, "DELETE", linkPath, {
      query: `expectedVersion=${set.body.version + 3}`,
    });
    expect(stale.status).toBe(412);

    const res = await chat(store, "DELETE", linkPath, {
      query: `expectedVersion=${set.body.version}`,
    });
    expect(res.status).toBe(200);
    expect(res.body.link).toBeNull();
    expect(res.body.version).toBe(set.body.version + 1);
    expect(store.getBoard(BOARD)).toBeDefined();
  });

  it("degrades to a generic chip for a viewer who cannot see the resource, and when it is gone", async () => {
    const store = storeAs("owner");
    await link(store, LINK_KIND.page, INVOICE_DOC_ID);
    store.getPage(INVOICE_DOC_ID)!.trashedAt = store.getClock().isoNow();

    const res = await chat(store, "GET", `/channels/${GENERAL}`);
    const view = res.body.channel ?? res.body;
    expect(view.link).toEqual({
      kind: LINK_KIND.page,
      resourceId: INVOICE_DOC_ID,
      accessible: false,
      title: null,
    });
  });

  it("the reverse lookup lists only the channels the caller can see, and nothing to someone who cannot view the resource", async () => {
    const store = storeAs("owner");
    await link(store, LINK_KIND.board, BOARD);
    const secret = store.chat.spaces.get(SPACES.leadership)!;
    expect(secret.kind).toBe("channel_private");
    secret.link = { kind: LINK_KIND.board, resourceId: BOARD };

    const owner = await linked(store, LINK_KIND.board, BOARD);
    expect(owner.status).toBe(200);
    const ownerIds = owner.body.map((c: { id: string }) => c.id);
    expect(ownerIds).toContain(GENERAL);

    // A member who is not in the private channel sees the public one only.
    actAs(store, "member");
    const member = await linked(store, LINK_KIND.board, BOARD);
    const memberIds = member.body.map((c: { id: string }) => c.id);
    expect(memberIds).toContain(GENERAL);
    expect(memberIds).not.toContain(
      store.chat.activeMembershipOf(SPACES.leadership, USERS.member)
        ? ""
        : SPACES.leadership,
    );

    // The Board turns private: the member learns nothing about its channels.
    store.getBoard(BOARD)!.visibility = "private";
    const hidden = await linked(store, LINK_KIND.board, BOARD);
    expect(hidden.body).toEqual([]);
  });

  it("announces the change as chat.space.changed with the change link, once per real change", async () => {
    const store = storeAs("owner");
    const transport = new MockRealtimeTransport();
    const frames: RealtimeEnvelope[] = [];
    const stop = transport.subscribeScope(
      { kind: "channel", id: GENERAL },
      (envelope) => {
        frames.push(envelope);
      },
    );
    try {
      const first = await link(store, LINK_KIND.board, BOARD);
      await link(store, LINK_KIND.board, BOARD, first.body.version);
      await chat(store, "DELETE", linkPath, {
        query: `expectedVersion=${first.body.version}`,
      });
      const changes = frames
        .filter((f) => f.eventType === "chat.space.changed")
        .map((f) => (f.payload as { change: string }).change);
      expect(changes).toEqual(["link", "link"]);
    } finally {
      stop();
    }
  });
});
