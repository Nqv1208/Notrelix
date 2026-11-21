import { describe, expect, it } from "vitest";
import {
  safeHttpUrl,
  tokenizeBlocks,
  tokenizeInline,
} from "../core/model/markup-model";

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

describe("tokenizeBlocks (UC-CHT-B08)", () => {
  it("is one paragraph when no line is a block", () => {
    expect(tokenizeBlocks("a\nb - c\n-no space")).toEqual([
      { type: "paragraph", text: "a\nb - c\n-no space" },
    ]);
  });

  it("groups quotes and bullet or numbered lists between paragraphs", () => {
    expect(
      tokenizeBlocks("intro\n> q1\n> q2\n- a\n* b\n1. one\n2. two\nend"),
    ).toEqual([
      { type: "paragraph", text: "intro" },
      { type: "quote", text: "q1\nq2" },
      { type: "list", ordered: false, items: ["a", "b"] },
      { type: "list", ordered: true, items: ["one", "two"] },
      { type: "paragraph", text: "end" },
    ]);
  });

  it("leaves hostile text in a block as text for the inline pass", () => {
    const hostile = "- <img src=x onerror=alert(1)>";
    expect(tokenizeBlocks(hostile)).toEqual([
      { type: "list", ordered: false, items: ["<img src=x onerror=alert(1)>"] },
    ]);
  });
});

describe("link targets (UC-CHT-B08)", () => {
  const hostile = [
    "javascript:alert(1)",
    "JaVaScRiPt:alert(1)",
    "java\tscript:alert(1)",
    "data:text/html,<script>alert(1)</script>",
    "vbscript:x",
    "file:///etc/passwd",
    "//evil.test/x",
    "mailto:a@b.test",
  ];
  it("never makes a link of an address that is not http or https", () => {
    for (const address of hostile) {
      const tokens = tokenizeInline(`see ${address} and [x](${address})`);
      expect(tokens.filter((token) => token.type === "link")).toEqual([]);
      expect(safeHttpUrl(address)).toBeNull();
    }
  });
  it("only links lowercase http and https addresses", () => {
    const links = tokenizeInline(
      "HTTP://a.test/x http://a.test/x https://b.test/y",
    )
      .filter((token) => token.type === "link")
      .map((token) => (token.type === "link" ? token.href : ""));
    expect(links).toEqual(["http://a.test/x", "https://b.test/y"]);
  });
});
