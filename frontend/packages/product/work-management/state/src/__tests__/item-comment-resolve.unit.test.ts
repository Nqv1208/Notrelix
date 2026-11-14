import { describe, expect, it, vi } from "vitest";
import { endpoints, type NotrelixClient } from "@notrelix/contracts";
import { createCommentApi } from "../api/item-comments.api";
import { mapCommentDtoToCardUpdate } from "../api/board-api-mappers";

function clientWith(post = vi.fn(async () => undefined), get = vi.fn()) {
  const client = {
    api: { post, get, patch: vi.fn(), delete: vi.fn() },
    endpoints,
  } as unknown as NotrelixClient;
  return { api: createCommentApi(client), post, get };
}

const starterDto = {
  id: "c-1",
  userId: "u-1",
  userName: "Ada",
  contentMd: "Thử nghiệm",
  parentCommentId: null,
  isEdited: false,
  resolvedAt: null,
  createdAt: "2026-11-01T09:00:00Z",
  reactions: [],
  version: 1,
  isDeleted: false,
};

describe("item comment resolve and reopen (UC-COL-A07, A08)", () => {
  it("resolves a thread with POST /comments/{id}/resolve", async () => {
    const { api, post } = clientWith();

    await api.setCardUpdateResolved("c-1", true);

    expect(post).toHaveBeenCalledWith("/comments/c-1/resolve", {});
  });

  it("reopens a thread with POST /comments/{id}/reopen", async () => {
    const { api, post } = clientWith();

    await api.setCardUpdateResolved("c-1", false);

    expect(post).toHaveBeenCalledWith("/comments/c-1/reopen", {});
  });

  it("maps resolvedAt onto the comment's resolved flag", () => {
    expect(
      mapCommentDtoToCardUpdate(starterDto as never, "item-1").resolved,
    ).toBe(false);
    expect(
      mapCommentDtoToCardUpdate(
        { ...starterDto, resolvedAt: "2026-11-02T09:00:00Z" } as never,
        "item-1",
      ).resolved,
    ).toBe(true);
  });
});
