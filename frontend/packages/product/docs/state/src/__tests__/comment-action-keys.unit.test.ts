import { describe, expect, it, vi } from "vitest";
import { createCommentApi } from "../api/comment.api";
import {
  COMMENT_REPEAT_WINDOW_MS,
  createCommentActionKeys,
} from "../query/hooks/comment-action-keys";

const payload = { pageId: "p1", body: "Đã xem", mentionIds: ["u2", "u1"] };

function keys() {
  let now = 1_000;
  let n = 0;
  const keeper = createCommentActionKeys(
    () => now,
    () => `k${++n}`,
  );
  return { keeper, advance: (ms: number) => (now += ms) };
}

describe("comment action keys (UC-COL-H01)", () => {
  it("a second send of the same comment before an answer reuses the key, so it is stored once", () => {
    const { keeper } = keys();
    const first = keeper.keyFor(payload);
    expect(keeper.keyFor({ ...payload, mentionIds: ["u1", "u2"] })).toBe(first);
    expect(keeper.keyFor({ ...payload, body: " Đã xem " })).toBe(first);
  });

  it("a success forgets the key: the same words posted again later are a new comment", () => {
    const { keeper } = keys();
    const first = keeper.keyFor(payload);
    keeper.succeeded(payload);
    expect(keeper.keyFor(payload)).not.toBe(first);
  });

  it("another text, reply target or anchor is another action", () => {
    const { keeper } = keys();
    const first = keeper.keyFor(payload);
    expect(keeper.keyFor({ ...payload, body: "Khác" })).not.toBe(first);
    expect(keeper.keyFor({ ...payload, parentId: "c1" })).not.toBe(first);
    expect(
      keeper.keyFor({
        ...payload,
        anchor: { blockId: "b1", quote: "x", start: 0, end: 1 },
      }),
    ).not.toBe(first);
  });

  it("after the repeat window a failed send is retried as a new action", () => {
    const { keeper, advance } = keys();
    const first = keeper.keyFor(payload);
    advance(COMMENT_REPEAT_WINDOW_MS);
    expect(keeper.keyFor(payload)).not.toBe(first);
  });

  it("the page comment create sends the key it is given as the Idempotency-Key option", async () => {
    const post = vi.fn(async () => "c-1");
    const api = { get: vi.fn(), post, patch: vi.fn(), delete: vi.fn() };
    const endpoints = {
      pages: { comments: (p: string) => `/pages/${p}/comments` },
    } as never;
    await createCommentApi(api as never, endpoints).create("p1", payload, {
      idempotencyKey: "k-42",
    });
    expect(post).toHaveBeenCalledWith(
      "/pages/p1/comments",
      expect.objectContaining({ contentMd: "Đã xem" }),
      { idempotencyKey: "k-42" },
    );
  });
});
