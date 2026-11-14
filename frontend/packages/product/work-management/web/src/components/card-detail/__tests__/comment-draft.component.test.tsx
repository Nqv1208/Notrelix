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
});
