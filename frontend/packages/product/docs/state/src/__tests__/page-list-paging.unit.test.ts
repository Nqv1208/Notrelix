import { describe, expect, it, vi } from "vitest";
import type { DocsApiClient } from "../api/page.api";
import { createPageApi } from "../api/page.api";
import { createVersionApi } from "../api/version.api";
import { testEndpoints } from "./test-endpoints";

function client() {
  const api = { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() };
  return api as typeof api & DocsApiClient;
}

const page = (id: string, title: string) => ({
  id,
  workspaceId: "w-1",
  parentId: null,
  title,
  icon: null,
  spaceId: null,
  version: 1,
  createdAt: "2026-11-01T09:00:00.000Z",
  updatedAt: "2026-11-01T09:00:00.000Z",
});

const entry = (id: string, versionNumber: number) => ({
  id,
  pageId: "p-1",
  versionNumber,
  kind: "automatic",
  name: null,
  createdBy: "u-1",
  createdAt: "2026-11-01T09:00:00.000Z",
});

describe("page list paging", () => {
  it("follows nextCursor to the end and lists the pages by title", async () => {
    const api = client();
    api.get
      .mockResolvedValueOnce({
        items: [page("p-2", "Zeta"), page("p-3", "beta")],
        nextCursor: "next-1",
      })
      .mockResolvedValueOnce({
        items: [page("p-1", "Alpha")],
        nextCursor: null,
      });

    const pages = await createPageApi(api, testEndpoints).getList("w-1");

    expect(api.get).toHaveBeenNthCalledWith(1, "/workspaces/w-1/pages");
    expect(api.get).toHaveBeenNthCalledWith(
      2,
      "/workspaces/w-1/pages?cursor=next-1",
    );
    expect(pages.map((p) => p.title)).toEqual(["Alpha", "beta", "Zeta"]);
  });

  it("reads every version of the history, following nextCursor", async () => {
    const api = client();
    api.get
      .mockResolvedValueOnce({
        items: [entry("v-3", 3), entry("v-2", 2)],
        nextCursor: "older",
      })
      .mockResolvedValueOnce({ items: [entry("v-1", 1)], nextCursor: null });

    const versions = await createVersionApi(api, testEndpoints).list("p-1");

    expect(api.get).toHaveBeenNthCalledWith(
      2,
      "/pages/p-1/versions?cursor=older",
    );
    expect(versions.map((v) => v.id)).toEqual(["v-3", "v-2", "v-1"]);
  });
});
