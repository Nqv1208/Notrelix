import { describe, expect, it, vi } from "vitest";
import { createPageApi } from "../api/page.api";
import type { DocsApiClient } from "../api/page.api";
import { testEndpoints } from "./test-endpoints";

describe("Documents.Pages.UpdatePage icon", () => {
  const client = () =>
    ({
      get: vi.fn((url: string) =>
        Promise.resolve(
          url.endsWith("/breadcrumb")
            ? []
            : {
                id: "page-1",
                workspaceId: "ws-1",
                title: "P",
                createdAt: "2026-10-01T00:00:00.000Z",
                updatedAt: "2026-10-02T00:00:00.000Z",
                version: 4,
              },
        ),
      ),
      post: vi.fn(),
      patch: vi.fn().mockResolvedValue(undefined),
      delete: vi.fn(),
    }) as unknown as DocsApiClient & { patch: ReturnType<typeof vi.fn> };

  it("sends the icon alone with the version, and a cleared icon as an empty string", async () => {
    const api = client();
    const pages = createPageApi(api, testEndpoints);

    await pages.update("page-1", { icon: "🚀", expectedVersion: 3 });
    await pages.update("page-1", { icon: null, expectedVersion: 4 });

    expect(api.patch).toHaveBeenNthCalledWith(1, expect.any(String), {
      icon: "🚀",
      expectedVersion: 3,
    });
    expect(api.patch).toHaveBeenNthCalledWith(2, expect.any(String), {
      icon: "",
      expectedVersion: 4,
    });
  });

  it("refuses an update with neither a title nor an icon, or without the version", async () => {
    const pages = createPageApi(client(), testEndpoints);
    await expect(
      pages.update("page-1", { expectedVersion: 3 }),
    ).rejects.toThrow();
    await expect(pages.update("page-1", { icon: "🚀" })).rejects.toThrow();
  });
});
