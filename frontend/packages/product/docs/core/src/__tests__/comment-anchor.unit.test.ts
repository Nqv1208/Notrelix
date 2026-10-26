import { describe, expect, it } from "vitest";
import { resolveCommentAnchor } from "../types/comment";

const anchor = { quote: "brave", start: 6, end: 11 };

describe("resolveCommentAnchor", () => {
  it("keeps the stored range while it still holds the quote", () => {
    expect(resolveCommentAnchor("Hello brave new world", anchor)).toEqual({
      start: 6,
      end: 11,
    });
  });

  it("follows the quote when the text before it changed", () => {
    expect(resolveCommentAnchor("Oh, hello brave new world", anchor)).toEqual({
      start: 10,
      end: 15,
    });
  });

  it("picks the occurrence nearest to where the comment was made", () => {
    const text = "brave 12345 brave 12345 brave";
    expect(
      resolveCommentAnchor(text, { quote: "brave", start: 11, end: 16 }),
    ).toEqual({
      start: 12,
      end: 17,
    });
  });

  it("is detached when the quoted text is gone, never attached to other text", () => {
    expect(resolveCommentAnchor("Hello bold new world", anchor)).toBeNull();
    expect(resolveCommentAnchor("", anchor)).toBeNull();
    expect(
      resolveCommentAnchor("abc", { quote: "", start: 0, end: 0 }),
    ).toBeNull();
  });

  it("counts UTF-16 code units like the producer", () => {
    expect(
      resolveCommentAnchor("Xin chào 😀 bạn", {
        quote: "bạn",
        start: 12,
        end: 15,
      }),
    ).toEqual({ start: 12, end: 15 });
  });
});
