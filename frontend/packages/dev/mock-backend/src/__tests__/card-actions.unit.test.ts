import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { erpItemId } from "../state/mock-erp-board";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, client };
}

const item = erpItemId("ERP-142");

describe("Card actions in the mock world", () => {
  it("links a page, lists it, and unlinks only that page", async () => {
    const { store, client } = setup();
    const pages = store.getPages(mockIds.workspaces.primary);
    expect(pages.length).toBeGreaterThan(1);
    const [a, b] = pages;
    for (const page of [a!, b!])
      await client.api.post(`/board-items/${item}/link-page`, {
        pageId: page.id,
      });
    const listed = await client.api.get<{
      pages: { pageId: string }[];
      hiddenCount: number;
    }>(`/board-items/${item}/linked-pages`);
    expect(listed.pages.map((p) => p.pageId).sort()).toEqual(
      [a!.id, b!.id].sort(),
    );
    await client.api.delete(`/board-items/${item}/link-page/${a!.id}`);
    const after = await client.api.get<{ pages: { pageId: string }[] }>(
      `/board-items/${item}/linked-pages`,
    );
    expect(after.pages.map((p) => p.pageId)).toEqual([b!.id]);
  });

  it("answers a second reminder inside 24 hours with 409", async () => {
    const { store, client } = setup();
    const card = store.getCard(item)!;
    store.updateCard(item, {
      assigneeUserIds: [
        ...new Set([...(card.assigneeUserIds ?? []), "someone-else"]),
      ],
    });
    const first = await client.api.post<{ recipients: number }>(
      `/board-items/${item}/remind`,
      {},
    );
    expect(first.recipients).toBeGreaterThan(0);
    await expect(
      client.api.post(`/board-items/${item}/remind`, {}),
    ).rejects.toMatchObject({ status: 409 });
  });
});
