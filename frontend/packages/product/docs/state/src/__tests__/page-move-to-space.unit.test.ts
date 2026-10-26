import { describe, expect, it, vi } from "vitest";
import { createPageApi } from "../api/page.api";
import { testEndpoints } from "./test-endpoints";
import type { PageDtoApi } from "../dto";
import { mapPage } from "../model/page.mapper";

const endpoints = testEndpoints;

const dto: PageDtoApi = {
  id: "page-1",
  workspaceId: "ws-1",
  title: "Playbook",
  createdAt: "2026-10-01T00:00:00.000Z",
  updatedAt: "2026-10-02T00:00:00.000Z",
  version: 3,
  spaceId: "sp-1",
};

describe("Page version and Documents.Pages.MoveToSpace", () => {
  it("keeps the producer's aggregate version on the page model", () => {
    expect(mapPage(dto).version).toBe(3);
    expect(mapPage(dto).spaceId).toBe("sp-1");
  });

  it("reports no version (so no versioned write is offered) when the producer does not report a positive one", () => {
    expect(mapPage({ ...dto, version: undefined }).version).toBeNull();
    expect(mapPage({ ...dto, version: 0 }).version).toBeNull();
  });

  it("posts the target Space and the expected version to the move operation, showing failures itself", async () => {
    const post = vi.fn().mockResolvedValue(undefined);
    const api = { get: vi.fn(), post, patch: vi.fn(), delete: vi.fn() };

    await createPageApi(api, endpoints).moveToSpace({
      pageId: "page-1",
      spaceId: "sp-2",
      expectedVersion: 3,
    });

    expect(post).toHaveBeenCalledWith(
      "/pages/page-1/move-to-space",
      { spaceId: "sp-2", expectedVersion: 3 },
      { skipGlobalErrorToast: true },
    );
  });

  it("sends a null Space to move the page back to the workspace level", async () => {
    const post = vi.fn().mockResolvedValue(undefined);
    const api = { get: vi.fn(), post, patch: vi.fn(), delete: vi.fn() };

    await createPageApi(api, endpoints).moveToSpace({
      pageId: "page-1",
      spaceId: null,
      expectedVersion: 4,
    });

    expect(post.mock.calls[0]?.[1]).toEqual({
      spaceId: null,
      expectedVersion: 4,
    });
  });
});
