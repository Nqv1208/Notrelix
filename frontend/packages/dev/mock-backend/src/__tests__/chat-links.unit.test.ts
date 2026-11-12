import { describe, it, expect } from "vitest";
import { mockIds } from "../state/mock-ids";
import { INVOICE_DOC_ID } from "../state/mock-docs-invoice";
import {
  LINK_KIND,
  LINK_URL_MAX,
  targetOfUrl,
} from "../contexts/chat/links.handlers";
import { WORKSPACE_ID, actAs, chat, storeAs } from "./chat-test-kit";

type Store = ReturnType<typeof storeAs>;

const resolve = (store: Store, query: string) =>
  chat(store, "GET", "/links/resolve", { query });

/** A card of the roadmap board, the board the world seeds with items. */
function roadmapCard(store: Store) {
  const list = store.getLists(mockIds.boards.roadmap)[0]!;
  return store.getCards(list.id)[0]!;
}

describe("MFB-CHAT-S2.8: Chat mock handler (link preview, UC-CHT-G05)", () => {
  it("gives a Page the reader can view its title, by reference and by product URL", async () => {
    const store = storeAs("owner");
    const page = store.getPage(INVOICE_DOC_ID)!;

    const byRef = await resolve(
      store,
      `kind=${LINK_KIND.page}&resourceId=${page.id}`,
    );
    const byUrl = await resolve(
      store,
      `url=${encodeURIComponent(`/workspaces/${WORKSPACE_ID}/docs/${page.id}`)}`,
    );

    expect(byRef.status).toBe(200);
    expect(byRef.body).toEqual({
      kind: LINK_KIND.page,
      resourceId: page.id,
      accessible: true,
      title: page.title,
    });
    expect(byUrl.body).toEqual(byRef.body);
  });

  it("gives a Board item on a Board the reader can see its title, and a private Board's item to its members only", async () => {
    const store = storeAs("owner");
    const card = roadmapCard(store);
    const ref = `kind=${LINK_KIND.boardItem}&resourceId=${card.id}`;

    expect((await resolve(store, ref)).body).toMatchObject({
      accessible: true,
      title: card.title,
    });

    // The Board becomes private and the reader is not one of its members.
    const board = store.getBoard(card.boardId)!;
    board.visibility = "private";
    for (const member of store.getBoardMembers(board.id)) {
      expect(member.userId).not.toBe(store.getCurrentUser().id);
    }
    const hidden = await resolve(store, ref);
    expect(hidden.status).toBe(200);
    expect(hidden.body).toEqual({
      kind: LINK_KIND.boardItem,
      resourceId: card.id,
      accessible: false,
      title: null,
    });
  });

  it("answers a missing item, a missing page and a trashed page exactly like a hidden one", async () => {
    const store = storeAs("owner");
    const missing = "00000000-0000-4000-8000-00000000ffff";
    const page = store.getPage(INVOICE_DOC_ID)!;
    page.trashedAt = store.getClock().isoNow();

    const item = await resolve(
      store,
      `kind=${LINK_KIND.boardItem}&resourceId=${missing}`,
    );
    const gone = await resolve(
      store,
      `kind=${LINK_KIND.page}&resourceId=${missing}`,
    );
    const trashed = await resolve(
      store,
      `kind=${LINK_KIND.page}&resourceId=${page.id}`,
    );

    for (const answer of [item, gone, trashed]) {
      expect(answer.status).toBe(200);
      expect(answer.body.accessible).toBe(false);
      expect(answer.body.title).toBeNull();
    }
  });

  it("never changes the resource", async () => {
    const store = storeAs("owner");
    const card = roadmapCard(store);
    const before = JSON.stringify([card, store.getPage(INVOICE_DOC_ID)]);

    await resolve(store, `kind=${LINK_KIND.boardItem}&resourceId=${card.id}`);
    await resolve(store, `kind=${LINK_KIND.page}&resourceId=${INVOICE_DOC_ID}`);

    expect(JSON.stringify([card, store.getPage(INVOICE_DOC_ID)])).toBe(before);
  });

  it("refuses a URL that is not a Board item or Page of this Workspace, a bad kind, both forms and neither (400)", async () => {
    const store = storeAs("owner");
    const card = roadmapCard(store);
    const encoded = (url: string) => `url=${encodeURIComponent(url)}`;

    const cases = [
      encoded("https://elsewhere.test/page"),
      encoded(`/workspaces/${WORKSPACE_ID}/boards/${card.boardId}`),
      encoded(`/workspaces/${mockIds.workspaces.secondary}/docs/${card.id}`),
      encoded(`//evil.test/workspaces/${WORKSPACE_ID}/docs/${card.id}`),
      encoded("javascript:alert(1)"),
      encoded("x".repeat(LINK_URL_MAX + 1)),
      "kind=chat.message&resourceId=00000000-0000-4000-8000-000000000001",
      `kind=${LINK_KIND.page}`,
      `${encoded("/x")}&kind=${LINK_KIND.page}&resourceId=${card.id}`,
      "",
    ];
    for (const query of cases) {
      const res = await resolve(store, query);
      expect(res.status, query).toBe(400);
    }
  });

  it("is a read for any member and is not found outside the Workspace", async () => {
    const store = storeAs("member");
    expect(
      (
        await resolve(
          store,
          `kind=${LINK_KIND.page}&resourceId=${INVOICE_DOC_ID}`,
        )
      ).status,
    ).toBe(200);
    actAs(store, "commenter");
    const outside = await chat(store, "GET", "/links/resolve", {
      workspaceId: mockIds.workspaces.secondary,
      query: `kind=${LINK_KIND.page}&resourceId=${INVOICE_DOC_ID}`,
    });
    expect([403, 404]).toContain(outside.status);
  });

  describe("targetOfUrl", () => {
    const ws = "ws-1";

    it("reads the path and the item query of the product routes on any host", () => {
      expect(
        targetOfUrl(`https://app.test/workspaces/${ws}/docs/page-1`, ws),
      ).toEqual({ kind: LINK_KIND.page, id: "page-1" });
      expect(
        targetOfUrl(
          `/workspaces/${ws}/boards/b-1?view=table&item=card-9#x`,
          ws,
        ),
      ).toEqual({ kind: LINK_KIND.boardItem, id: "card-9" });
      expect(targetOfUrl(`/workspaces/${ws}/boards/b-1`, ws)).toBeNull();
      expect(targetOfUrl(`/workspaces/${ws}/spaces/s-1`, ws)).toBeNull();
      expect(targetOfUrl(`/workspaces/other/docs/page-1`, ws)).toBeNull();
    });
  });
});
