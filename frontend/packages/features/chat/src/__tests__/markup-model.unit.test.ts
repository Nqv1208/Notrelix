import { describe, expect, it } from "vitest";
import { safeHttpUrl, tokenizeInline } from "../core/model/markup-model";

describe("tokenizeInline (UC-CHT-B08)", () => {
  it("reads bold, italic, code and links, and leaves the rest as text", () => {
    expect(
      tokenizeInline("a **b** _c_ `d` see https://x.test/p?q=1, ok"),
    ).toEqual([
      { type: "text", text: "a " },
      { type: "bold", text: "b" },
      { type: "text", text: " " },
      { type: "italic", text: "c" },
      { type: "text", text: " " },
      { type: "code", text: "d" },
      { type: "text", text: " see " },
      {
        type: "link",
        text: "https://x.test/p?q=1",
        href: "https://x.test/p?q=1",
      },
      { type: "text", text: ", ok" },
    ]);
  });

  it("keeps script, event attributes and other schemes inert as plain text", () => {
    const script = '<script>alert(1)</script><img src=x onerror="alert(1)">';
    expect(tokenizeInline(script)).toEqual([{ type: "text", text: script }]);
    expect(tokenizeInline("javascript:alert(1) data:text/html,x")).toEqual([
      { type: "text", text: "javascript:alert(1) data:text/html,x" },
    ]);
    expect(safeHttpUrl("javascript:alert(1)")).toBeNull();
    expect(safeHttpUrl("ftp://x.test")).toBeNull();
  });

  it("does not italicise snake_case words or an unclosed marker", () => {
    expect(tokenizeInline("a snake_case_name and **open")).toEqual([
      { type: "text", text: "a snake_case_name and **open" },
    ]);
  });

  it("gives back the text it was given when no markup is present", () => {
    expect(tokenizeInline("")).toEqual([]);
    expect(tokenizeInline("plain\nlines")).toEqual([
      { type: "text", text: "plain\nlines" },
    ]);
  });
});
