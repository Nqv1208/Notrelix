import { describe, expect, it, vi } from "vitest";
import type { DocsApiClient } from "../api/page.api";
import { docsQueryKeys } from "../query/keys";
import { createUsePageActivity } from "../query/hooks/use-page-activity";
import { testEndpoints } from "./test-endpoints";

// `useQuery` is replaced by its options, so the key, fetch and gate can be read without a React tree.
vi.mock("@tanstack/react-query", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@tanstack/react-query")>();
  return { ...actual, useQuery: (options: unknown) => options };
});

interface QueryOptions {
  queryKey: readonly unknown[];
  queryFn: () => Promise<unknown>;
  enabled: boolean;
}

function setup() {
  const api = { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() };
  const use = createUsePageActivity(
    api as typeof api & DocsApiClient,
    testEndpoints,
  );
  return { api, use };
}

describe("usePageActivity", () => {
  it("reads the requested page of the feed from the page activity route", async () => {
    const { api, use } = setup();
    const feed = { data: [], total: 0, page: 2, pageSize: 20 };
    api.get.mockResolvedValue(feed);

    const options = use("ws-1", "p1", 2) as unknown as QueryOptions;

    await expect(options.queryFn()).resolves.toBe(feed);
    expect(api.get).toHaveBeenCalledWith("/pages/p1/activity?page=2");
    expect(options.queryKey).toEqual(docsQueryKeys.activity("ws-1", "p1", 2));
  });

  it("keys each page apart and under the workspace", () => {
    expect(docsQueryKeys.activity("ws-1", "p1", 1)).not.toEqual(
      docsQueryKeys.activity("ws-1", "p1", 2),
    );
    expect(docsQueryKeys.activity("ws-1", "p1", 1).slice(0, 3)).toEqual([
      "workspace",
      "ws-1",
      "documents",
    ]);
  });

  it("waits for a workspace and a page", () => {
    const { use } = setup();
    expect((use("", "p1") as unknown as QueryOptions).enabled).toBe(false);
    expect((use("ws-1", "") as unknown as QueryOptions).enabled).toBe(false);
    expect(
      (use("ws-1", "p1", 1, false) as unknown as QueryOptions).enabled,
    ).toBe(false);
    expect((use("ws-1", "p1") as unknown as QueryOptions).enabled).toBe(true);
  });
});
