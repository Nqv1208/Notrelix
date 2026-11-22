import { describe, expect, it } from "vitest";
import { isWebUrl } from "../types/attachment-rules";

describe("isWebUrl: the links a page may register", () => {
  it("accepts an absolute http or https address with a host", () => {
    expect(isWebUrl("https://example.com/a?b=1")).toBe(true);
    expect(isWebUrl(" http://localhost:5173/x ")).toBe(true);
  });

  it("refuses every other scheme and anything that is not an absolute address", () => {
    for (const value of [
      "javascript:alert(1)",
      "data:text/html,<b>x</b>",
      "vbscript:x",
      "file:///etc/passwd",
      "mailto:a@b.c",
      "ftp://example.com",
      "example.com/path",
      "/relative",
      "https://",
      "",
    ]) {
      expect(isWebUrl(value), value).toBe(false);
    }
  });
});
