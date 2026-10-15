import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createPageExtrasApi } from "../../../../product/docs/state/src/index";
import { defaultConfig } from "../config/mock-config";
import { MockStore } from "../state/mock-store";
import { INVOICE_DOC_ID } from "../state/mock-docs-invoice";
import { createMockFetch } from "../transport/create-mock-fetch";

const endpoints = {
  pages: { detail: (id: string) => `/pages/${id}` },
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

  it("lists what mentions the page: a document and the item with it attached", async () => {
    const { extras } = setup();
    const links = await extras.getBacklinks(INVOICE_DOC_ID);
    expect(links.docs.map((doc) => [doc.title, doc.spaceName])).toEqual([
      ["Đặc tả triển khai ERP", "Công nghệ"],
    ]);
    expect(links.tasks.map((task) => task.key)).toEqual(["ERP-149"]);
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

  it("serves the blocks as they stood after each version", async () => {
    const { store, extras } = setup();
    const current = store.getPageBlocks(INVOICE_DOC_ID);
    const entries = [...store.getPageHistory(INVOICE_DOC_ID)].sort((a, b) =>
      b.createdAt.localeCompare(a.createdAt),
    );
    expect(entries[4]?.versionName ?? entries[3]?.versionName).toBe(
      "Bản trình Ban giám đốc",
    );
    // The newest version is the page itself.
    expect(
      (await extras.getVersionSnapshot(INVOICE_DOC_ID, entries[0]!.id)).length,
    ).toBe(current.length);
    // 10/10 15:42: step 3 as it read, and without the two lines added later.
    const older = await extras.getVersionSnapshot(
      INVOICE_DOC_ID,
      entries[2]!.id,
    );
    expect(older.length).toBe(current.length - 2);
    expect(
      older.find((block) =>
        String(block.properties.text).startsWith("Hoá đơn sai lệch"),
      )?.properties.text,
    ).toBe("Hoá đơn sai lệch quá 1% chuyển cho kế toán trưởng duyệt.");
    await expect(
      extras.getVersionSnapshot(INVOICE_DOC_ID, "nope"),
    ).rejects.toMatchObject({
      kind: "not_found",
    });
  });

  it("turns a checklist line into a work item and links the line to it", async () => {
    const { store, extras } = setup();
    const line = store
      .getPageBlocks(INVOICE_DOC_ID)
      .find((block) => block.properties.includes("Đào tạo nhóm Công nợ"))!;
    const task = await extras.convertTodo(INVOICE_DOC_ID, line.id);
    expect(task).toMatchObject({
      title: "Đào tạo nhóm Công nợ về đối chiếu tự động",
    });
    expect(store.getCard(task.id)?.itemKey).toBe(task.key);
    expect(
      JSON.parse(
        store.getPageBlocks(INVOICE_DOC_ID).find((b) => b.id === line.id)!
          .properties,
      ).linkedTaskId,
    ).toBe(task.id);
    // A line that is already a task cannot be converted twice.
    await expect(
      extras.convertTodo(INVOICE_DOC_ID, line.id),
    ).rejects.toMatchObject({
      kind: "validation",
    });
  });
});
