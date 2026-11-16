import { describe, expect, it, vi } from "vitest";
import { createPageApi, type DocsApiClient } from "../api/page.api";
import { testEndpoints } from "./test-endpoints";

function client() {
  const api = { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() };
  return api as typeof api & DocsApiClient;
}

describe("duplicate a page", () => {
  it("makes one server call with a key and the title, and reads the dropped count", async () => {
    const api = client();
    api.post.mockResolvedValue({ pageId: "p2", droppedBlockCount: 3 });

    const copy = await createPageApi(api, testEndpoints).duplicate(
      "w1",
      "p1",
      "Plan (bản sao)",
    );

    expect(copy).toEqual({ pageId: "p2", droppedBlockCount: 3 });
    expect(api.post).toHaveBeenCalledTimes(1);
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/w1/pages/p1/duplicate",
      { title: "Plan (bản sao)" },
      { skipGlobalErrorToast: true, idempotencyKey: expect.any(String) },
    );
  });

  it("sends no title when none is given, and a new key for each call", async () => {
    const api = client();
    api.post.mockResolvedValue({ pageId: "p2", droppedBlockCount: 0 });
    const pages = createPageApi(api, testEndpoints);

    await pages.duplicate("w1", "p1");
    await pages.duplicate("w1", "p1");

    expect(api.post.mock.calls[0]![1]).toEqual({});
    expect(api.post.mock.calls[0]![2].idempotencyKey).not.toBe(
      api.post.mock.calls[1]![2].idempotencyKey,
    );
  });
});
