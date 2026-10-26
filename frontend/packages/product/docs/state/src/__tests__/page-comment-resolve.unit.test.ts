import { describe, expect, it, vi } from "vitest";
import { createCommentApi } from "../api/comment.api";
import type { DocsApiClient } from "../api/page.api";
import { testEndpoints } from "./test-endpoints";

function client() {
  const api = { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() };
  return api as typeof api & DocsApiClient;
}

describe("page comment resolve and delete", () => {
  it("resolves and deletes through the Collaboration comment routes", async () => {
    const api = client();
    const comments = createCommentApi(api, testEndpoints);
    await comments.resolve("c1");
    expect(api.post).toHaveBeenCalledWith("/comments/c1/resolve", {});
    await comments.reopen("c1");
    expect(api.post).toHaveBeenCalledWith("/comments/c1/reopen", {});
    await comments.delete("c1");
    expect(api.delete).toHaveBeenCalledWith("/comments/c1");
  });

  it("reads a resolved thread from resolvedAt", async () => {
    const api = client();
    api.get.mockResolvedValue([
      {
        id: "c1",
        userId: "u1",
        contentMd: "done",
        createdAt: "2026-10-26T10:00:00Z",
        isEdited: false,
        resolvedAt: "2026-10-26T11:00:00Z",
      },
      {
        id: "c2",
        userId: "u1",
        contentMd: "open",
        createdAt: "2026-10-26T10:05:00Z",
        isEdited: false,
        resolvedAt: null,
      },
    ]);
    const list = await createCommentApi(api, testEndpoints).getList("p1");
    expect(list.map((c) => c.resolved)).toEqual([true, false]);
  });
});
