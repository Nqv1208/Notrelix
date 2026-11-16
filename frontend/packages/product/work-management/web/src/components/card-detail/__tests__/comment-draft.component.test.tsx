import { describe, expect, it, vi } from "vitest";
import { act, renderHook } from "@notrelix/testing";
import { unsentCommentOf, useCommentDraft } from "../update-composer";

describe("useCommentDraft", () => {
  it("keeps the unsent text for the card after the composer is gone, and drops it once sent", () => {
    const onCreateUpdate = vi.fn((_input, options) => options?.onSuccess?.());
    const first = renderHook(() =>
      useCommentDraft({ cardId: "kept-1", onCreateUpdate }),
    );
    act(() => first.result.current.changeBody("Chưa gửi được"));
    first.unmount();
    expect(unsentCommentOf("kept-1")).toBe("Chưa gửi được");

    const again = renderHook(() =>
      useCommentDraft({ cardId: "kept-1", onCreateUpdate }),
    );
    expect(again.result.current.body).toBe("Chưa gửi được");
    act(() => again.result.current.submit());
    expect(unsentCommentOf("kept-1")).toBe("");
  });

  it("retries an unanswered post with the same key, and a changed text with a new one", () => {
    const keys: Array<string | undefined> = [];
    const onCreateUpdate = vi.fn(
      (input: { idempotencyKey?: string }, options) => {
        keys.push(input.idempotencyKey);
        options?.onSettled?.();
      },
    );
    const draft = renderHook(() =>
      useCommentDraft({ cardId: "key-1", onCreateUpdate }),
    );
    act(() => draft.result.current.changeBody("Xong rồi"));
    act(() => draft.result.current.submit());
    act(() => draft.result.current.submit());
    act(() => draft.result.current.changeBody("Xong rồi nhé"));
    act(() => draft.result.current.submit());
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).toBe(keys[0]);
    expect(keys[2]).not.toBe(keys[0]);
  });

  it("starts a new key once the post was answered", () => {
    const keys: Array<string | undefined> = [];
    const onCreateUpdate = vi.fn(
      (input: { idempotencyKey?: string }, options) => {
        keys.push(input.idempotencyKey);
        options?.onSuccess?.();
        options?.onSettled?.();
      },
    );
    const draft = renderHook(() =>
      useCommentDraft({ cardId: "key-2", onCreateUpdate }),
    );
    for (let i = 0; i < 2; i++) {
      act(() => draft.result.current.changeBody("Giống nhau"));
      act(() => draft.result.current.submit());
    }
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).not.toBe(keys[0]);
  });
});
