import { beforeAll, describe, expect, it, vi } from "vitest";
import { act, fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import type { Editor } from "@tiptap/core";
import { TextSelection } from "@tiptap/pm/state";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { DocSidePanelSurface } from "../../components/doc-side-panel-surface";
import {
  anchorFromSelection,
  blockPlainText,
  commentAnchorStateOf,
} from "../comment-anchors";
import { headlessEditor, paragraph, stubLayout } from "./headless-editor";

beforeAll(stubLayout);

const drafts = (): BlockDraft[] => [
  paragraph("a", "Hello brave new world"),
  paragraph("b", "Second line"),
  {
    id: "c",
    type: "code",
    content: richText([{ text: "const x = 1", marks: [] }]),
    properties: { language: "typescript" },
  },
];

const anchor = { id: "k1", blockId: "a", quote: "brave", start: 6, end: 11 };
const state = (editor: Editor) => commentAnchorStateOf(editor.state)!;
const textOf = (editor: Editor, from: number, to: number) =>
  editor.state.doc.textBetween(from, to);

function select(editor: Editor, from: number, to: number) {
  editor.view.dispatch(
    editor.state.tr.setSelection(
      TextSelection.create(editor.state.doc, from, to),
    ),
  );
}

describe("comment anchors in the editor", () => {
  it("highlights the quoted text and nothing else is written into the page", () => {
    const editor = headlessEditor(drafts());
    editor.commands.setCommentAnchors([anchor], null);
    const [placed] = state(editor).placed;
    expect(textOf(editor, placed!.from, placed!.to)).toBe("brave");
    expect(
      editor.view.dom.querySelectorAll(
        "span.comment-anchor[data-comment-id='k1']",
      ),
    ).toHaveLength(1);
    expect(editor.getHTML()).not.toContain("comment-anchor");
  });

  it("follows the text when it moves and marks the active thread", () => {
    const editor = headlessEditor(drafts());
    editor.commands.setCommentAnchors([anchor], "k1");
    editor.commands.insertContentAt(1, "Oh, ");
    const [placed] = state(editor).placed;
    expect(textOf(editor, placed!.from, placed!.to)).toBe("brave");
    expect(state(editor).detached).toEqual([]);
    expect(
      editor.view.dom.querySelector(".comment-anchor-active"),
    ).not.toBeNull();
  });

  it("detaches an anchor whose text was edited away, and attaches it nowhere else", () => {
    const editor = headlessEditor(drafts());
    editor.commands.setCommentAnchors([anchor], null);
    const [placed] = state(editor).placed;
    editor.commands.deleteRange({ from: placed!.from, to: placed!.to });
    expect(state(editor).detached).toEqual(["k1"]);
    expect(state(editor).placed).toEqual([]);
    expect(editor.view.dom.querySelector(".comment-anchor")).toBeNull();
  });

  it("detaches an anchor of a block that is gone", () => {
    const editor = headlessEditor(drafts());
    editor.commands.setCommentAnchors(
      [{ ...anchor, id: "k2", blockId: "missing" }],
      null,
    );
    expect(state(editor).detached).toEqual(["k2"]);
  });

  it("opens the thread when its highlight is clicked", () => {
    const onOpenComment = vi.fn();
    const editor = headlessEditor(drafts(), { onOpenComment });
    editor.commands.setCommentAnchors([anchor], null);
    const [placed] = state(editor).placed;
    editor.view.someProp("handleClick", (handler) =>
      handler(editor.view, placed!.from + 2, new MouseEvent("click")),
    );
    expect(onOpenComment).toHaveBeenCalledWith("k1");
    onOpenComment.mockClear();
    editor.view.someProp("handleClick", (handler) =>
      handler(editor.view, 1, new MouseEvent("click")),
    );
    expect(onOpenComment).not.toHaveBeenCalled();
  });
});

describe("the anchor of a selection", () => {
  it("is the block, the quote and its offsets in the block's text", () => {
    const editor = headlessEditor(drafts());
    select(editor, 7, 12);
    expect(anchorFromSelection(editor)).toEqual({
      blockId: "a",
      quote: "brave",
      start: 6,
      end: 11,
    });
  });

  it("counts a hard break as one newline, like the stored text", () => {
    const editor = headlessEditor([
      {
        id: "h",
        type: "paragraph",
        content: richText([{ text: "ab\ncd", marks: [] }]),
        properties: {},
      },
    ]);
    expect(blockPlainText(editor.state.doc.child(0))).toBe("ab\ncd");
    select(editor, 4, 5);
    expect(anchorFromSelection(editor)).toMatchObject({
      quote: "c",
      start: 3,
      end: 4,
    });
  });

  it("is clamped to the first block when the selection runs over several", () => {
    const editor = headlessEditor(drafts());
    select(editor, 7, 30);
    expect(anchorFromSelection(editor)).toEqual({
      blockId: "a",
      quote: "brave new world",
      start: 6,
      end: 21,
    });
  });

  it("is none for a caret, a code block or a blank selection", () => {
    const editor = headlessEditor(drafts());
    select(editor, 3, 3);
    expect(anchorFromSelection(editor)).toBeNull();
    const code =
      editor.state.doc.child(0).nodeSize + editor.state.doc.child(1).nodeSize;
    select(editor, code + 2, code + 6);
    expect(anchorFromSelection(editor)).toBeNull();
    const blank = headlessEditor([paragraph("z", "a   b")]);
    select(blank, 2, 5);
    expect(anchorFromSelection(blank)).toBeNull();
  });
});

describe("comments in the side panel", () => {
  const author = {
    id: "u",
    name: "An",
    initials: "A",
    tone: "gold" as const,
  };

  it("shows the quote of an anchored comment, reveals it on click and flags a detached one", async () => {
    const onCommentSelect = vi.fn();
    renderPureUi(
      <DocSidePanelSurface
        copy={docsCopy}
        tab="comments"
        tasks={[]}
        relatedDocs={[]}
        toc={[]}
        canComment
        comments={[
          {
            id: "k1",
            author,
            quote: "brave",
            anchored: true,
            body: "why?",
            when: "Hôm nay",
            active: true,
          },
          {
            id: "k2",
            author,
            quote: "gone text",
            anchored: true,
            detached: true,
            body: "old",
            when: "Hôm nay",
          },
        ]}
        callbacks={{ onCommentSelect }}
      />,
    );
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "brave" }));
    });
    expect(onCommentSelect).toHaveBeenCalledWith("k1");
    // The detached comment keeps its quote but cannot be revealed.
    expect(screen.getByText("gone text").tagName).toBe("DIV");
    expect(screen.getByText(docsCopy.comments.detached)).toBeTruthy();
  });

  it("asks for the words under the selected text and can drop the selection", async () => {
    const onCommentCancel = vi.fn();
    const onCommentCreate = vi.fn();
    renderPureUi(
      <DocSidePanelSurface
        copy={docsCopy}
        tab="comments"
        tasks={[]}
        relatedDocs={[]}
        toc={[]}
        canComment
        comments={[]}
        pendingQuote="brave new"
        callbacks={{ onCommentCancel, onCommentCreate }}
      />,
    );
    expect(screen.getByText("brave new")).toBeTruthy();
    const field = screen.getByRole("textbox", {
      name: docsCopy.comments.reply,
    });
    expect(document.activeElement).toBe(field);
    await act(async () => {
      fireEvent.change(field, { target: { value: "Check this" } });
      fireEvent.keyDown(field, { key: "Enter" });
    });
    expect(onCommentCreate).toHaveBeenCalledWith("Check this");
    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: docsCopy.comments.cancelSelection }),
      );
    });
    expect(onCommentCancel).toHaveBeenCalled();
  });
});
