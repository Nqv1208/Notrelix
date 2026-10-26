import { describe, expect, it, vi } from "vitest";
import { richText, type ImportedBlock } from "@notrelix/docs-core";
import { createImportApi } from "../api/import.api";
import type { DocsApiClient } from "../api/page.api";
import { testEndpoints } from "./test-endpoints";

function client() {
  const api = { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() };
  return api as typeof api & DocsApiClient;
}

const PAGE = {
  id: "new-page",
  title: "Imported",
  workspaceId: "w1",
};

function paragraph(text: string): ImportedBlock {
  return {
    type: "paragraph",
    content: richText([{ text, marks: [] }]),
    properties: {},
  };
}

function setup() {
  const api = client();
  let n = 0;
  api.post.mockImplementation(async (url: string) => {
    if (url === "/workspaces/w1/pages") return PAGE.id;
    return [];
  });
  api.get.mockImplementation(async (url: string) =>
    String(url).endsWith("/breadcrumb") ? [] : { ...PAGE, metadata: {} },
  );
  return {
    api,
    importApi: createImportApi(api, testEndpoints, () => `b${(n += 1)}`),
  };
}

describe("importing a file as a page", () => {
  it("creates the page, then writes the blocks in order, each after the one before", async () => {
    const { api, importApi } = setup();
    const id = await importApi.importPage("w1", {
      title: "Imported",
      blocks: [paragraph("one"), paragraph("two"), paragraph("three")],
    });
    expect(id).toBe("new-page");
    const batches = api.post.mock.calls.filter((call) =>
      String(call[0]).endsWith("/blocks/batch"),
    );
    expect(batches).toHaveLength(1);
    const items = (batches[0] as unknown[])[1] as {
      blocks: { id: string; afterBlockId?: string }[];
    };
    expect(items.blocks.map((entry) => entry.id)).toEqual(["b1", "b2", "b3"]);
    expect(items.blocks[1]?.afterBlockId).toBe("b1");
    expect(items.blocks[2]?.afterBlockId).toBe("b2");
  });

  it("sends at most 500 operations per request", async () => {
    const { api, importApi } = setup();
    await importApi.importPage("w1", {
      title: "Big",
      blocks: Array.from({ length: 1100 }, (_, i) => paragraph(`p${i}`)),
    });
    const sizes = api.post.mock.calls
      .filter((call) => String(call[0]).endsWith("/blocks/batch"))
      .map((call) => (call[1] as { blocks: unknown[] }).blocks.length);
    expect(sizes).toEqual([500, 500, 100]);
  });

  it("moves the new page to the trash when a write fails, so no half page stays", async () => {
    const { api, importApi } = setup();
    api.post.mockImplementation(async (url: string) => {
      if (url === "/workspaces/w1/pages") return PAGE.id;
      if (String(url).endsWith("/blocks/batch")) throw new Error("boom");
      return undefined;
    });
    await expect(
      importApi.importPage("w1", { title: "x", blocks: [paragraph("a")] }),
    ).rejects.toThrow("boom");
    expect(api.post).toHaveBeenCalledWith("/pages/new-page/archive", {});
  });
});
