import { describe, expect, it } from "vitest";
import { richText, type BlockDraft, type BlockType } from "@notrelix/docs-core";
import {
  docToDrafts,
  draftsToDoc,
  inlineToRuns,
  runsToInline,
} from "../block-mapping";

const rich = richText([
  { text: "plain ", marks: [] },
  { text: "bold", marks: [{ type: "bold" }] },
  { text: " both", marks: [{ type: "bold" }, { type: "italic" }] },
  { text: " u", marks: [{ type: "underline" }] },
  { text: " s", marks: [{ type: "strike" }] },
  { text: " c", marks: [{ type: "code" }] },
  { text: " link", marks: [{ type: "link", href: "https://example.com" }] },
  { text: " red", marks: [{ type: "color", token: "danger" }] },
  { text: " hi", marks: [{ type: "highlight", token: "warning" }] },
  { text: " @Ann", marks: [{ type: "mention", userId: "u-1" }] },
  { text: " page", marks: [{ type: "pageRef", pageId: "p-1" }] },
  { text: "\nnext line", marks: [] },
]);

function draft(
  id: string,
  type: BlockType,
  properties: BlockDraft["properties"] = {},
  content: BlockDraft["content"] = richText([{ text: id, marks: [] }]),
): BlockDraft {
  return { id, type, content, properties };
}

describe("block mapping", () => {
  it("round-trips every mark through inline nodes", () => {
    expect(inlineToRuns(runsToInline(rich.runs))).toEqual(rich.runs);
  });

  it("turns a task token into a chip and back", () => {
    const runs = [{ text: "see [[task:t-9]] now", marks: [] }];
    const inline = runsToInline(runs);
    expect(inline.map((n) => n.type)).toEqual(["text", "taskRef", "text"]);
    expect(inlineToRuns(inline)).toEqual(runs);
  });

  const cases: BlockDraft[] = [
    draft("p", "paragraph", {}, rich),
    draft("p2", "paragraph", { align: "center" }),
    draft("h1", "heading_1"),
    draft("h2", "heading_2", { align: "right" }),
    draft("h3", "heading_3"),
    draft("b", "bulleted_list", {}, rich),
    draft("n", "numbered_list"),
    draft("t", "todo", { checked: true, linkedTaskId: "task-1" }),
    draft("t2", "todo", { checked: false }),
    draft("q", "quote", {}, rich),
    draft("c", "callout", { icon: "💡", tone: "warning" }),
    draft(
      "code",
      "code",
      { language: "ts" },
      richText([{ text: "a\nb", marks: [] }]),
    ),
    draft("tg", "toggle", { collapsed: true }),
    draft("d", "divider", {}, richText()),
    draft(
      "br",
      "board_reference",
      {
        linkedBoardId: "b1",
        title: "Board",
        caption: "Open",
        items: ["i1", "i2"],
      },
      richText(),
    ),
    draft("pr", "page_reference", { linkedPageId: "pg1" }, richText()),
    draft("img", "image", { url: "x", attachmentId: "a1" }, richText()),
    draft("e", "paragraph", {}, richText()),
  ];

  it.each(cases.map((c) => [c.id, c] as const))(
    "round-trips block %s",
    (_id, block) => {
      expect(docToDrafts(draftsToDoc([block]))).toEqual([block]);
    },
  );

  it("keeps block order and ids and never yields an empty document", () => {
    expect(docToDrafts(draftsToDoc(cases)).map((d) => d.id)).toEqual(
      cases.map((c) => c.id),
    );
    const empty = docToDrafts(draftsToDoc([]));
    expect(empty).toHaveLength(1);
    expect(empty[0]?.type).toBe("paragraph");
  });
});
