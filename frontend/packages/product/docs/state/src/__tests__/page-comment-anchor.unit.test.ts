import { describe, expect, it, vi } from "vitest";
import { createCommentApi } from "../api/comment.api";
import type { DocsApiClient } from "../api/page.api";
import { testEndpoints } from "./test-endpoints";

function client() {
  const api = { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() };
  return api as typeof api & DocsApiClient;
}

const ANCHOR = { blockId: "b1", quote: "brave", start: 6, end: 11 };

describe("page comments with a text anchor", () => {
  it("sends the anchor with the comment and only then", async () => {
    const api = client();
    api.post.mockResolvedValue("c1");
    const comments = createCommentApi(api, testEndpoints);

    const created = await comments.create("p1", {
      pageId: "p1",
      body: "why?",
      anchor: ANCHOR,
    });
    expect(api.post).toHaveBeenLastCalledWith("/pages/p1/comments", {
      contentMd: "why?",
      anchor: ANCHOR,
    });
    expect(created.anchor).toEqual(ANCHOR);

    await comments.create("p1", { pageId: "p1", body: "plain" });
    expect(api.post).toHaveBeenLastCalledWith("/pages/p1/comments", {
      contentMd: "plain",
    });
  });

  it("reads the anchor of a comment, and none for a comment without one", async () => {
    const api = client();
    api.get.mockResolvedValue([
      {
        id: "c1",
        userId: "u1",
        contentMd: "why?",
        createdAt: "2026-10-26T10:00:00Z",
        isEdited: false,
        anchor: ANCHOR,
      },
      {
        id: "c2",
        userId: "u1",
        contentMd: "hi",
        createdAt: "2026-10-26T11:00:00Z",
        isEdited: false,
      },
    ]);
    const list = await createCommentApi(api, testEndpoints).getList("p1");
    expect(list.map((comment) => comment.anchor)).toEqual([ANCHOR, null]);
  });
});
