import { describe, expect, it, vi } from "vitest";
import { convertBlockProperties, type Block } from "@notrelix/docs-core";
import { createBlockApi } from "../api/block.api";
import { createPageApi } from "../api/page.api";
import { createPageExtrasApi } from "../api/page-extras.api";
import { mapBlock } from "../model/block.mapper";
import { testEndpoints } from "./test-endpoints";

function client() {
  return {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn().mockResolvedValue(undefined),
    delete: vi.fn().mockResolvedValue(undefined),
  };
}

describe("block content conversion (mirrors the producer)", () => {
  it("carries text between text-like types and drops shape-specific members", () => {
    expect(
      convertBlockProperties("todo", "paragraph", {
        text: "Pay",
        checked: true,
        bold: true,
      }),
    ).toEqual({ text: "Pay", bold: true });
    expect(
      convertBlockProperties("paragraph", "todo", { text: "Pay" }),
    ).toEqual({ text: "Pay" });
    expect(
      convertBlockProperties("code", "quote", { text: "x", language: "ts" }),
    ).toEqual({ text: "x" });
    expect(
      convertBlockProperties("paragraph", "callout", { text: "Note" }),
    ).toEqual({ text: "Note" });
  });

  it("starts empty when either side is not text-like", () => {
    expect(
      convertBlockProperties("paragraph", "divider", { text: "x" }),
    ).toEqual({});
    expect(
      convertBlockProperties("page_reference", "paragraph", {
        linkedPageId: "p1",
      }),
    ).toEqual({});
    expect(
      convertBlockProperties("paragraph", "page_reference", { text: "x" }),
    ).toEqual({});
  });
});

describe("Documents.Blocks.UpdateBlock", () => {
  it("retypes in place with the producer's type name and the content", async () => {
    const api = client();
    await createBlockApi(api, testEndpoints).update("pg", "b1", {
      type: "page_reference",
      properties: { linkedPageId: "other" },
    });
    expect(api.patch).toHaveBeenCalledWith("/pages/pg/blocks/b1", {
      type: "PageReference",
      properties: JSON.stringify({ linkedPageId: "other" }),
    });
  });

  it("sends only the content when the type stays", async () => {
    const api = client();
    await createBlockApi(api, testEndpoints).update("pg", "b1", {
      properties: { text: "hi" },
    });
    expect(api.patch.mock.calls[0]?.[1]).toEqual({
      properties: JSON.stringify({ text: "hi" }),
    });
  });

  it("creates a callout and reads the producer's callout and page reference", async () => {
    const api = client();
    api.post.mockResolvedValue("new-id");
    const created = await createBlockApi(api, testEndpoints).create("pg", {
      type: "callout",
      properties: { text: "" },
    });
    expect(api.post.mock.calls[0]?.[1]).toMatchObject({ type: "Callout" });
    expect(created.type).toBe("callout");

    const dto = {
      id: "b",
      pageId: "pg",
      parentId: null,
      type: "PageReference",
      content: JSON.stringify({ linkedPageId: "t" }),
      position: "a0",
      version: 1,
      createdAt: "2026-10-01T00:00:00Z",
    };
    const block = mapBlock(dto, 1000);
    expect(block.type).toBe("page_reference");
    expect(block.properties.linkedPageId).toBe("t");
  });
});

describe("Documents trash", () => {
  it("archives, restores, lists and deletes through the producer routes", async () => {
    const api = client();
    api.post.mockResolvedValue(undefined);
    api.get.mockResolvedValue([
      {
        id: "p1",
        workspaceId: "w",
        title: "Old",
        createdAt: "2026-10-01T00:00:00Z",
      },
    ]);
    const pages = createPageApi(api, testEndpoints);
    await pages.archive("p1");
    await pages.restore("p1");
    const trash = await pages.getTrash("w");
    await pages.delete("p1");

    expect(api.post.mock.calls.map((call) => call[0])).toEqual([
      "/pages/p1/archive",
      "/pages/p1/restore",
    ]);
    expect(api.get).toHaveBeenCalledWith("/workspaces/w/pages/trash");
    expect(trash.map((page) => page.id)).toEqual(["p1"]);
    expect(api.delete).toHaveBeenCalledWith("/pages/p1");
  });
});

describe("Documents.Pages.GetPageBacklinks", () => {
  it("reads the pages that name the page, with the Space id", async () => {
    const api = client();
    api.get.mockResolvedValue({
      docs: [{ id: "d1", title: "Runbook", icon: "📘", spaceId: "s1" }],
    });
    const backlinks = await createPageExtrasApi(
      api,
      testEndpoints,
    ).getBacklinks("pg");
    expect(api.get.mock.calls[0]?.[0]).toBe("/pages/pg/backlinks");
    expect(backlinks.docs).toEqual([
      { id: "d1", title: "Runbook", icon: "📘", spaceId: "s1" },
    ]);
  });
});

describe("convert a checklist line to a task", () => {
  const block: Block = {
    id: "b1",
    pageId: "pg",
    type: "todo",
    properties: { text: " Pay the invoice ", checked: false },
    position: 1000,
    parentId: null,
    createdById: "",
    updatedById: "",
    createdAt: "",
    updatedAt: "",
  };

  it("creates the item in the chosen group, links the page and marks the line", async () => {
    const api = client();
    api.post.mockResolvedValueOnce({ id: "item-1" }).mockResolvedValueOnce({});
    const task = await createPageExtrasApi(api, testEndpoints).convertTodo(
      "pg",
      block,
      { boardId: "bd", groupId: "gr" },
    );

    expect(api.post.mock.calls[0]?.slice(0, 2)).toEqual([
      "/boards/bd/items",
      { groupId: "gr", title: "Pay the invoice" },
    ]);
    expect(api.post.mock.calls[1]?.slice(0, 2)).toEqual([
      "/board-items/item-1/link-page",
      { pageId: "pg" },
    ]);
    expect(api.patch).toHaveBeenCalledWith("/pages/pg/blocks/b1", {
      properties: JSON.stringify({
        text: " Pay the invoice ",
        checked: false,
        linkedTaskId: "item-1",
      }),
    });
    expect(task).toEqual({ id: "item-1", title: "Pay the invoice" });
  });

  it("leaves the line unlinked when linking the page fails", async () => {
    const api = client();
    api.post
      .mockResolvedValueOnce({ id: "item-1" })
      .mockRejectedValueOnce(new Error("forbidden"));
    await expect(
      createPageExtrasApi(api, testEndpoints).convertTodo("pg", block, {
        boardId: "bd",
        groupId: "gr",
      }),
    ).rejects.toThrow("forbidden");
    expect(api.patch).not.toHaveBeenCalled();
  });
});
