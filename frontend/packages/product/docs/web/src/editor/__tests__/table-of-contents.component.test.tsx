import { beforeAll, describe, expect, it } from "vitest";
import { act, fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import { EditorContent } from "@tiptap/react";
import { docToDrafts } from "../block-mapping";
import { insertBlockType } from "../block-insert";
import { headlessEditor, paragraph, stubLayout } from "./headless-editor";

beforeAll(stubLayout);

const heading = (id: string, level: 1 | 2 | 3, text: string): BlockDraft => ({
  id,
  type: `heading_${level}`,
  content: richText([{ text, marks: [] }]),
  properties: {},
});

const toc = (id: string): BlockDraft => ({
  id,
  type: "table_of_contents",
  content: richText(),
  properties: {},
});

describe("table of contents block", () => {
  it("is stored without properties or text and keeps its id and place", () => {
    const editor = headlessEditor([
      heading("h1", 1, "Intro"),
      toc("t"),
      paragraph("p", "body"),
    ]);
    const saved = docToDrafts(editor.getJSON());
    expect(saved.map((d) => [d.id, d.type])).toEqual([
      ["h1", "heading_1"],
      ["t", "table_of_contents"],
      ["p", "paragraph"],
    ]);
    expect(saved[1]!.properties).toEqual({});
    expect(saved[1]!.content.runs).toEqual([]);
  });

  it("lists the page's headings by level as links and follows edits", async () => {
    const editor = headlessEditor([
      heading("h1", 1, "Intro"),
      toc("t"),
      heading("h2", 2, "Details"),
      paragraph("p", "body"),
    ]);
    renderPureUi(<EditorContent editor={editor} />);
    const nav = await screen.findByRole("navigation");
    expect(nav.textContent).toContain("Intro");
    expect(nav.textContent).toContain("Details");
    expect(screen.getAllByRole("button", { name: "Details" })).toHaveLength(1);
    await act(async () => {
      editor.commands.insertContentAt(editor.state.doc.content.size, {
        type: "heading",
        attrs: { level: 3, blockId: "h3" },
        content: [{ type: "text", text: "Later" }],
      });
    });
    expect(screen.getByRole("navigation").textContent).toContain("Later");
    fireEvent.click(screen.getByRole("button", { name: "Intro" }));
  });

  it("shows a hint when the page has no headings", async () => {
    const editor = headlessEditor([toc("t"), paragraph("p", "body")]);
    renderPureUi(<EditorContent editor={editor} />);
    const nav = await screen.findByRole("navigation");
    expect(nav.querySelectorAll("button")).toHaveLength(0);
    expect(nav.textContent).not.toBe("");
  });

  it("replaces the empty block under the caret and leaves a paragraph after it", () => {
    const editor = headlessEditor([paragraph("a", "text"), paragraph("b")]);
    editor.commands.setTextSelection(editor.state.doc.content.size - 1);
    expect(insertBlockType(editor, "table_of_contents")).toBe(true);
    const saved = docToDrafts(editor.getJSON());
    expect(saved.map((d) => d.type)).toEqual([
      "paragraph",
      "table_of_contents",
      "paragraph",
    ]);
    expect(saved[1]!.id).toBe("b");
  });
});
