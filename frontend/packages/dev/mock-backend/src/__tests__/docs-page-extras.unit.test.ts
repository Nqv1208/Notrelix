import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createPageExtrasApi } from "../../../../product/docs/state/src/index";
import { defaultConfig } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { INVOICE_DOC_ID } from "../state/mock-docs-invoice";
import { mockIds } from "../state/mock-ids";
import { readBlockBody } from "../contexts/documents/block-body";
import { createMockFetch } from "../transport/create-mock-fetch";

const endpoints = {
  pages: {
    detail: (id: string) => `/pages/${id}`,
    backlinks: (id: string) => `/pages/${id}/backlinks`,
  },
  resourcePermissions: {
    list: (kind: string, id: string) => `/resources/${kind}/${id}/permissions`,
  },
  blocks: {
    detail: (pageId: string, blockId: string) =>
      `/pages/${pageId}/blocks/${blockId}`,
  },
  boardItems: {
    create: (boardId: string) => `/boards/${boardId}/items`,
    linkPage: (itemId: string) => `/board-items/${itemId}/link-page`,
  },
} as never;

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return {
    store,
    client,
    extras: createPageExtrasApi(client.api as never, endpoints),
  };
}

describe("Documents page extras in the mock world (CTR-GAP-DOCS-PAGE-EXT)", () => {
  it("serves the invoice page in review, with who is on it and their names", async () => {
    const { client } = setup();
    const page = await client.api.get<{
      status: string;
      activeUserIds: string[];
      people: { id: string; name: string }[];
    }>(`/pages/${INVOICE_DOC_ID}`);
    expect(page.status).toBe("review");
    expect(page.activeUserIds).toHaveLength(2);
    expect(page.people.map((person) => person.name)).toEqual(
      expect.arrayContaining(["Đỗ Khánh Linh", "Lý Thu Trang"]),
    );
  });

  it("lists the pages that name the page, found from page references", async () => {
    const { store, extras } = setup();
    const links = await extras.getBacklinks(INVOICE_DOC_ID);
    expect(links.docs.map((doc) => doc.title)).toEqual([
      "Đặc tả triển khai ERP",
    ]);
    const other = store
      .getPages(store.getPage(INVOICE_DOC_ID)!.workspaceId)
      .find((page) => page.id !== INVOICE_DOC_ID)!;
    store.createBlock(other.id, {
      type: "page_reference",
      properties: JSON.stringify({ linkedPageId: INVOICE_DOC_ID }),
    });
    const after = await extras.getBacklinks(INVOICE_DOC_ID);
    expect(after.docs.map((doc) => doc.id)).toContain(other.id);
  });

  it("serves the permissions given on the page, creator first", async () => {
    const { extras } = setup();
    const { members } = await extras.getPermissions(INVOICE_DOC_ID);
    expect(members.map((member) => [member.name, member.level])).toEqual([
      ["Đỗ Khánh Linh", "manage"],
      ["Nguyễn Thu Hà", "edit"],
      ["Nhóm Kỹ sư ERP", "comment"],
      ["Lâm Gia Bảo", "view"],
    ]);
    expect(members[3]?.outside).toBe(true);
    expect(
      (await extras.getPermissions("mock-doc-product-spec")).members,
    ).toEqual([]);
  });

  it("turns a checklist line into a work item in the chosen group and links the line to it", async () => {
    const { store, extras } = setup();
    const line = store
      .getPageBlocks(INVOICE_DOC_ID)
      .find((block) => block.properties.includes("Đào tạo nhóm Công nợ"))!;
    const list = store.getLists(mockIds.boards.erp)[0]!;
    const task = await extras.convertTodo(
      INVOICE_DOC_ID,
      {
        id: line.id,
        pageId: INVOICE_DOC_ID,
        type: "todo",
        properties: JSON.parse(line.properties),
        version: line.version,
        position: 0,
        parentId: null,
        createdById: "",
        updatedById: "",
        createdAt: "",
        updatedAt: "",
      },
      { boardId: mockIds.boards.erp, groupId: list.id },
      "attempt-1",
    );
    expect(task.title).toBe("Đào tạo nhóm Công nợ về đối chiếu tự động");
    expect(store.getCard(task.id)?.title).toBe(task.title);
    expect(
      readBlockBody(
        store.getPageBlocks(INVOICE_DOC_ID).find((b) => b.id === line.id)!,
      ).text,
    ).toBe(`[[task:${task.id}]]`);
  });

  it("retypes a block in place, converting its content, and keeps trashed pages out of reads", async () => {
    const { store, client } = setup();
    const line = store
      .getPageBlocks(INVOICE_DOC_ID)
      .find((block) => block.type === "todo")!;
    const readVersion = line.version;
    await client.api.patch(`/pages/${INVOICE_DOC_ID}/blocks/${line.id}`, {
      type: "callout",
      expectedVersion: readVersion,
    });
    const after = store
      .getPageBlocks(INVOICE_DOC_ID)
      .find((b) => b.id === line.id)!;
    expect(after.type).toBe("callout");
    expect(readBlockBody(after).properties.checked).toBeUndefined();
    expect(readBlockBody(after).text).toBeTruthy();
    expect(after.version).toBe(readVersion + 1);

    await client.api.post(`/pages/${INVOICE_DOC_ID}/archive`, {});
    await expect(
      client.api.get(`/pages/${INVOICE_DOC_ID}`),
    ).rejects.toMatchObject({
      kind: "not_found",
    });
    await expect(
      client.api.delete(`/pages/mock-doc-product-spec`),
    ).rejects.toBeTruthy();
    await client.api.post(`/pages/${INVOICE_DOC_ID}/restore`, {});
    await expect(
      client.api.get(`/pages/${INVOICE_DOC_ID}`),
    ).resolves.toBeTruthy();
  });
});
