import { beforeAll, describe, expect, it } from "vitest";
import { act, renderPureUi, screen } from "@notrelix/testing";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { DocPageBarSurface } from "../../components/doc-page-bar-surface";
import { DocSidePanelSurface } from "../../components/doc-side-panel-surface";
import { revealHeading, summarize } from "../outline";
import { headlessEditor, paragraph, stubLayout } from "./headless-editor";

beforeAll(stubLayout);

const heading = (
  id: string,
  type: "heading_1" | "heading_2" | "heading_3",
  text: string,
): BlockDraft => ({
  id,
  type,
  content: richText([{ text, marks: [] }]),
  properties: {},
});

const drafts = (): BlockDraft[] => [
  heading("h1", "heading_1", "Overview"),
  paragraph("p1", "Hello brave new world"),
  heading("h2", "heading_2", "Details"),
  heading("h3", "heading_3", "Edge cases"),
  heading("hx", "heading_1", "  "),
  paragraph("p2", "Xin chào"),
];

describe("page outline and counts", () => {
  it("lists the non-empty H1 to H3 headings with their ids and levels", () => {
    const editor = headlessEditor(drafts());
    expect(summarize(editor.state.doc).outline).toEqual([
      { id: "h1", level: 1, text: "Overview" },
      { id: "h2", level: 2, text: "Details" },
      { id: "h3", level: 3, text: "Edge cases" },
    ]);
  });

  it("counts words and characters over every text block", () => {
    const editor = headlessEditor(drafts());
    const { stats } = summarize(editor.state.doc);
    // Overview Hello brave new world Details Edge cases Xin chào
    expect(stats.words).toBe(10);
    expect(stats.characters).toBe(
      "Overview".length +
        "Hello brave new world".length +
        "Details".length +
        "Edge cases".length +
        2 +
        "Xin chào".length,
    );
  });

  it("follows edits to a heading", () => {
    const editor = headlessEditor(drafts());
    editor.commands.insertContentAt(2, "Big ");
    expect(summarize(editor.state.doc).outline[0]?.text).toBe("OBig verview");
  });

  it("moves the caret into the heading when it is chosen", () => {
    const editor = headlessEditor(drafts());
    expect(revealHeading(editor, "h2")).toBe(true);
    expect(editor.state.selection.$from.parent.textContent).toBe("Details");
    expect(revealHeading(editor, "missing")).toBe(false);
  });
});

describe("outline and counts surfaces", () => {
  it("indents the contents by heading level and marks the active heading", () => {
    renderPureUi(
      <DocSidePanelSurface
        copy={docsCopy}
        tab="toc"
        tasks={[]}
        relatedDocs={[]}
        toc={[
          { id: "title", level: 1, text: "Page" },
          { id: "h1", level: 2, text: "Overview", active: true },
          { id: "h3", level: 4, text: "Edge cases" },
        ]}
        comments={[]}
        canComment={false}
      />,
    );
    const nav = screen.getByRole("navigation", { name: docsCopy.panel.tocNav });
    expect(nav.querySelectorAll("a")).toHaveLength(3);
    expect(
      screen
        .getByRole("link", { name: "Overview" })
        .getAttribute("aria-current"),
    ).toBe("location");
    expect(
      screen.getByRole("link", { name: "Edge cases" }).className,
    ).toContain("pl-11");
  });

  it("shows the word and character count next to the save state", async () => {
    await act(async () => {
      renderPureUi(
        <DocPageBarSurface
          copy={docsCopy}
          header={{
            title: "T",
            crumbs: ["T"],
            editedBy: null,
            editedAt: null,
            presence: [],
          }}
          saveState="saved"
          stats={{ words: 1234, characters: 5678 }}
        />,
      );
    });
    expect(screen.getByText(/từ · .* ký tự/u).textContent).toMatch(/1.*234/u);
  });
});
