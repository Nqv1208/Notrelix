import { describe, expect, it } from "vitest";
import { en, vi } from "../item-page.copy";
import { collaborationRefusalMessage } from "../collaboration-refusal";

describe("collaborationRefusalMessage", () => {
  it.each([
    ["collaboration.comment_author_only", "commentAuthorOnly"],
    [
      "collaboration.comment_thread_starter_or_editor_only",
      "threadStarterOrEditorOnly",
    ],
    ["collaboration.attachment_editor_only", "attachmentEditorOnly"],
  ] as const)("names who may act for a 403 %s", (code, key) => {
    for (const { toasts } of [en, vi]) {
      expect(collaborationRefusalMessage({ status: 403, code }, toasts)).toBe(
        toasts[key],
      );
    }
  });

  it("says the item is no longer visible on a 404 and falls back otherwise", () => {
    expect(collaborationRefusalMessage({ status: 404 }, en.toasts)).toBe(
      en.toasts.targetGone,
    );
    expect(collaborationRefusalMessage(new Error("x"), vi.toasts)).toBe(
      vi.toasts.commentFailed,
    );
  });
});
