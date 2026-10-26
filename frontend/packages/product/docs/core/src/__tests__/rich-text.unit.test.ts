import { describe, expect, it } from "vitest";
import {
  blockPlainText,
  diffBlockDrafts,
  draftOfBlock,
  isEmptyDiff,
  isSafeHref,
  normalizeMarks,
  normalizeRuns,
  parseRichText,
  plainTextOf,
  richText,
  upgradeLegacyProperties,
  type Block,
  type BlockDraft,
} from "../index";

const bold = { type: "bold" } as const;
const italic = { type: "italic" } as const;

function draft(
  id: string,
  text: string,
  type: BlockDraft["type"] = "paragraph",
): BlockDraft {
  return {
    id,
    type,
    content: richText(text ? [{ text, marks: [] }] : []),
    properties: {},
  };
}

describe("rich text v1", () => {
  it("merges neighbours with equal marks, drops empty runs and sorts marks", () => {
    const runs = normalizeRuns([
      { text: "a", marks: [italic, bold] },
      { text: "b", marks: [bold, italic] },
      { text: "", marks: [bold] },
      { text: "c", marks: [] },
    ]);
    expect(runs).toEqual([
      { text: "ab", marks: [bold, italic] },
      { text: "c", marks: [] },
    ]);
  });

  it("keeps one mark per type (last wins) and code only beside a link", () => {
    expect(
      normalizeMarks([
        { type: "color", token: "danger" },
        { type: "color", token: "success" },
      ]),
    ).toEqual([{ type: "color", token: "success" }]);
    expect(
      normalizeMarks([
        bold,
        { type: "code" },
        { type: "link", href: "https://x.test" },
      ]),
    ).toEqual([{ type: "code" }, { type: "link", href: "https://x.test" }]);
  });

  it("drops unsafe links and unknown tokens", () => {
    expect(isSafeHref("javascript:alert(1)")).toBe(false);
    expect(isSafeHref("data:text/html,x")).toBe(false);
    expect(isSafeHref("mailto:a@b.co")).toBe(true);
    expect(isSafeHref("https://example.com/a?b=1")).toBe(true);
    expect(
      normalizeMarks([
        { type: "link", href: "javascript:x" },
        { type: "color", token: "pink" as never },
      ]),
    ).toEqual([]);
  });

  it("parses stored rich text and rejects other shapes", () => {
    expect(
      parseRichText({ v: 1, runs: [{ text: "hi", marks: [bold] }] }),
    ).toEqual({ v: 1, runs: [{ text: "hi", marks: [bold] }] });
    expect(parseRichText({ text: "hi" })).toBeNull();
    expect(parseRichText("x")).toBeNull();
  });
});

describe("legacy upgrade", () => {
  it("turns block-level flags into marks over the whole text", () => {
    const upgraded = upgradeLegacyProperties("paragraph", {
      text: "Hello",
      bold: true,
      underline: true,
      textColor: "destructive",
      highlight: "accent",
      align: "center",
      fontFamily: "serif",
    });
    expect(upgraded.content.runs).toEqual([
      {
        text: "Hello",
        marks: [
          bold,
          { type: "color", token: "danger" },
          { type: "highlight", token: "accent" },
          { type: "underline" },
        ],
      },
    ]);
    expect(upgraded.properties).toEqual({ align: "center" });
  });

  it("keeps type properties and gives code and empty text no marks", () => {
    expect(
      upgradeLegacyProperties("todo", { text: "", checked: true }),
    ).toEqual({ content: { v: 1, runs: [] }, properties: { checked: true } });
    expect(
      upgradeLegacyProperties("code", {
        text: "x",
        bold: true,
        language: "ts",
      }),
    ).toEqual({
      content: { v: 1, runs: [{ text: "x", marks: [] }] },
      properties: { language: "ts" },
    });
  });

  it("reads a block of either shape", () => {
    const base = {
      pageId: "p",
      position: 1,
      parentId: null,
      createdById: "",
      updatedById: "",
      createdAt: "",
      updatedAt: "",
    };
    const legacy: Block = {
      ...base,
      id: "a",
      type: "paragraph",
      properties: { text: "Hi", italic: true },
    };
    const v1: Block = {
      ...base,
      id: "b",
      type: "todo",
      content: richText([{ text: "Do", marks: [bold] }]),
      properties: { checked: false, text: "Do" },
    };
    expect(draftOfBlock(legacy).content.runs).toEqual([
      { text: "Hi", marks: [italic] },
    ]);
    expect(draftOfBlock(v1).properties).toEqual({ checked: false });
    expect(blockPlainText(legacy)).toBe("Hi");
    expect(blockPlainText(v1)).toBe("Do");
    expect(plainTextOf(undefined)).toBe("");
  });
});

describe("diffBlockDrafts", () => {
  it("is empty when nothing changed", () => {
    const list = [draft("a", "x"), draft("b", "y")];
    expect(
      isEmptyDiff(
        diffBlockDrafts(
          list,
          list.map((d) => ({ ...d })),
        ),
      ),
    ).toBe(true);
  });

  it("reports text edits, retypes, creates in order and deletes", () => {
    const prev = [draft("a", "x"), draft("b", "y"), draft("c", "z")];
    const next = [
      draft("a", "x2"),
      draft("n", "new"),
      { ...draft("b", "y"), type: "heading_1" as const },
    ];
    const diff = diffBlockDrafts(prev, next);
    expect(diff.deletes).toEqual(["c"]);
    expect(diff.updates.map((u) => [u.id, Object.keys(u.patch)])).toEqual([
      ["a", ["content"]],
      ["b", ["type"]],
    ]);
    expect(diff.creates.map((c) => [c.draft.id, c.afterId])).toEqual([
      ["n", "a"],
    ]);
    expect(diff.moves).toEqual([]);
  });

  it("moves only the blocks that left their place", () => {
    const prev = [
      draft("a", ""),
      draft("b", ""),
      draft("c", ""),
      draft("d", ""),
    ];
    const next = [
      draft("a", ""),
      draft("c", ""),
      draft("d", ""),
      draft("b", ""),
    ];
    expect(diffBlockDrafts(prev, next).moves).toEqual([
      { id: "b", afterId: "d" },
    ]);
  });
});
