import { describe, expect, it, vi } from "vitest";
import type { DocsApiClient } from "../api/page.api";
import { createTemplateApi } from "../api/template.api";
import { createVersionApi } from "../api/version.api";
import { testEndpoints } from "./test-endpoints";

function client() {
  const api = { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() };
  return api as typeof api & DocsApiClient;
}

const ENTRY = {
  id: "v2",
  pageId: "p1",
  versionNumber: 2,
  kind: "named",
  name: "Before review",
  createdBy: "u1",
  createdAt: "2026-10-26T09:00:00Z",
};

describe("page versions", () => {
  it("lists the history entries with their kind and name", async () => {
    const api = client();
    api.get.mockResolvedValue([
      ENTRY,
      { ...ENTRY, id: "v1", versionNumber: 1, kind: "automatic", name: null },
      { ...ENTRY, id: "v3", kind: "something-new", name: "  " },
    ]);

    const list = await createVersionApi(api, testEndpoints).list("p1");

    expect(api.get).toHaveBeenCalledWith("/pages/p1/versions");
    expect(list.map((v) => [v.id, v.kind, v.name])).toEqual([
      ["v2", "named", "Before review"],
      ["v1", "automatic", null],
      ["v3", "automatic", null],
    ]);
  });

  it("reads the captured blocks as read-only blocks of the page", async () => {
    const api = client();
    api.get.mockResolvedValue({
      version: ENTRY,
      title: "Plan",
      blocks: [
        {
          id: "b1",
          parentId: null,
          type: "Heading1",
          content: { v: 1, runs: [{ text: "Goals", marks: [] }] },
          properties: {},
          position: "a0",
        },
        {
          id: "b2",
          parentId: "b1",
          type: "ToDo",
          content: { v: 1, runs: [{ text: "Ship", marks: [] }] },
          properties: { checked: true },
          position: "a0",
        },
      ],
    });

    const content = await createVersionApi(api, testEndpoints).get("p1", "v2");

    expect(api.get).toHaveBeenCalledWith("/pages/p1/versions/v2");
    expect(content.title).toBe("Plan");
    expect(
      content.blocks.map((b) => [b.id, b.type, b.parentId, b.pageId]),
    ).toEqual([
      ["b1", "heading_1", null, "p1"],
      ["b2", "todo", "b1", "p1"],
    ]);
    expect(content.blocks[1]?.properties.checked).toBe(true);
    expect(content.blocks[0]?.properties.text).toBe("Goals");
  });

  it("saves a named version", async () => {
    const api = client();
    api.post.mockResolvedValue(ENTRY);

    const saved = await createVersionApi(api, testEndpoints).create(
      "p1",
      "Before review",
    );

    expect(api.post).toHaveBeenCalledWith(
      "/pages/p1/versions",
      { name: "Before review" },
      { skipGlobalErrorToast: true },
    );
    expect(saved.kind).toBe("named");
  });

  it("restores against the block versions the caller saw, and returns the new blocks", async () => {
    const api = client();
    api.post.mockResolvedValue({
      version: {
        ...ENTRY,
        id: "v4",
        kind: "restore",
        name: "Restored from version 2",
      },
      blocks: [
        {
          id: "b1",
          pageId: "p1",
          parentId: null,
          type: "Text",
          content: { v: 1, runs: [{ text: "back", marks: [] }] },
          properties: {},
          position: "a0",
          version: 5,
          createdAt: "2026-10-26T09:00:00Z",
        },
      ],
    });

    const restored = await createVersionApi(api, testEndpoints).restore(
      "p1",
      "v2",
      [{ blockId: "b1", version: 4 }],
    );

    expect(api.post).toHaveBeenCalledWith(
      "/pages/p1/versions/v2/restore",
      { currentBlocks: [{ blockId: "b1", version: 4 }] },
      { skipGlobalErrorToast: true },
    );
    expect(restored.version.kind).toBe("restore");
    expect(restored.blocks[0]?.version).toBe(5);
    expect(restored.blocks[0]?.properties.text).toBe("back");
  });
});

describe("page templates", () => {
  it("lists templates and keeps a blank description as none", async () => {
    const api = client();
    api.get.mockResolvedValue([
      {
        id: "t1",
        name: "Weekly",
        description: "",
        title: "Weekly sync",
        icon: "x",
        blockCount: 3,
        createdAt: "2026-10-26T09:00:00Z",
      },
    ]);

    const list = await createTemplateApi(api, testEndpoints).list("w1");

    expect(api.get).toHaveBeenCalledWith("/workspaces/w1/page-templates");
    expect(list[0]).toMatchObject({
      id: "t1",
      description: null,
      blockCount: 3,
      createdBy: null,
    });
  });

  it("saves a page as a template, sending a description only when there is one", async () => {
    const api = client();
    api.post.mockResolvedValue({
      id: "t1",
      name: "Weekly",
      title: "Weekly sync",
      icon: "x",
      blockCount: 1,
      createdAt: "2026-10-26T09:00:00Z",
    });
    const templates = createTemplateApi(api, testEndpoints);

    await templates.create("w1", { pageId: "p1", name: "Weekly" });
    expect(api.post).toHaveBeenLastCalledWith(
      "/workspaces/w1/page-templates",
      { pageId: "p1", name: "Weekly" },
      { skipGlobalErrorToast: true },
    );
    await templates.create("w1", {
      pageId: "p1",
      name: "Weekly",
      description: "Mondays",
    });
    expect(api.post).toHaveBeenLastCalledWith(
      "/workspaces/w1/page-templates",
      { pageId: "p1", name: "Weekly", description: "Mondays" },
      { skipGlobalErrorToast: true },
    );
  });

  it("creates a page from a template with its placement and reports what was left out", async () => {
    const api = client();
    api.post.mockResolvedValue({ pageId: "p9", droppedBlockCount: 2 });

    const made = await createTemplateApi(api, testEndpoints).createPage("w1", {
      templateId: "t1",
      title: "Sprint 12",
      parentId: "p0",
    });

    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/w1/page-templates/t1/pages",
      { title: "Sprint 12", parentId: "p0" },
      { skipGlobalErrorToast: true },
    );
    expect(made).toEqual({ pageId: "p9", droppedBlockCount: 2 });
  });
});
