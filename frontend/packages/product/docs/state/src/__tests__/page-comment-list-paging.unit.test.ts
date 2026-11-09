import { describe, expect, it, vi } from "vitest";
import { createCommentApi } from "../api/comment.api";
import type { DocsApiClient } from "../api/page.api";
import { testEndpoints } from "./test-endpoints";

function client() {
  const api = { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() };
  return api as typeof api & DocsApiClient;
}

const dto = (id: string, extra: Record<string, unknown> = {}) => ({
  id,
  userId: "u-1",
  contentMd: `text ${id}`,
  createdAt: "2026-11-01T09:00:00.000Z",
  isEdited: false,
  version: 2,
  ...extra,
});

describe("page comment list", () => {
  it("reads every page, following nextCursor until it runs out", async () => {
    const api = client();
    api.get
      .mockResolvedValueOnce({ items: [dto("c-1")], nextCursor: "next-1" })
      .mockResolvedValueOnce({ items: [dto("c-2")], nextCursor: null });
    const comments = createCommentApi(api, testEndpoints);

    const list = await comments.getList("p-1");

    expect(api.get).toHaveBeenNthCalledWith(1, "/pages/p-1/comments/page");
    expect(api.get).toHaveBeenNthCalledWith(
      2,
      "/pages/p-1/comments/page?cursor=next-1",
    );
    expect(list.map((c) => c.id)).toEqual(["c-1", "c-2"]);
    expect(list[0]?.version).toBe(2);
  });

  it("leaves out a deleted comment that only kept its replies, since this view does not thread them", async () => {
    const api = client();
    api.get.mockResolvedValueOnce({
      items: [dto("c-1", { isDeleted: true, contentMd: "" }), dto("c-2")],
      nextCursor: null,
    });
    const comments = createCommentApi(api, testEndpoints);

    const list = await comments.getList("p-1");

    expect(list.map((c) => c.id)).toEqual(["c-2"]);
  });
});
