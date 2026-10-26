import { beforeAll, describe, expect, it, vi } from "vitest";
import { act, fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { DocEditorSurface } from "../doc-editor-surface";
import { EditorToolbar } from "../editor-toolbar";
import { matchSlashItems } from "../slash-menu";
import { docToDrafts } from "../block-mapping";
import type { DocPageCallbacks } from "../../components/doc-page-models";
import { useDocRichEditor } from "../use-doc-rich-editor";
import { stubLayout } from "./headless-editor";

const DRAFTS: BlockDraft[] = [
  {
    id: "a",
    type: "paragraph",
    content: richText([{ text: "Hello world", marks: [] }]),
    properties: {},
  },
  {
    id: "b",
    type: "todo",
    content: richText([{ text: "Do it", marks: [] }]),
    properties: { checked: false },
  },
];

const context = {
  copy: docsCopy,
  readOnly: false,
  resolveTask: (id: string) => ({ id, restricted: true as const }),
  viewOf: () => undefined,
};

beforeAll(stubLayout);

let lastDrafts: BlockDraft[] = [];
const onDrafts = vi.fn((d: BlockDraft[]) => {
  lastDrafts = d;
});

function Harness({ callbacks }: { callbacks?: DocPageCallbacks } = {}) {
  const { editor } = useDocRichEditor({
    pageId: "p",
    ready: true,
    initial: DRAFTS,
    editable: true,
    placeholder: "p",
    emptyPlaceholder: "e",
    label: "doc",
    context,
    onDrafts,
  });
  if (!editor) return null;
  return (
    <>
      <EditorToolbar editor={editor} copy={docsCopy} callbacks={callbacks} />
      <DocEditorSurface editor={editor} copy={docsCopy} mentionTasks={[]} />
      <button
        type="button"
        onClick={() =>
          editor.chain().focus().setTextSelection({ from: 1, to: 6 }).run()
        }
      >
        select
      </button>
    </>
  );
}

describe("doc editor", () => {
  it("renders the blocks, reflects marks in the toolbar and saves them as runs", async () => {
    renderPureUi(<Harness />);
    expect(await screen.findByText("Hello world")).toBeTruthy();
    expect(
      screen.getByRole("toolbar", { name: docsCopy.toolbar.label }),
    ).toBeTruthy();
    const bold = screen.getByRole("button", { name: docsCopy.toolbar.bold });
    expect(bold.getAttribute("aria-pressed")).toBe("false");
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "select" }));
    });
    await act(async () => {
      fireEvent.click(bold);
    });
    expect(
      screen
        .getByRole("button", { name: docsCopy.toolbar.bold })
        .getAttribute("aria-pressed"),
    ).toBe("true");
    const saved = lastDrafts.find((d) => d.id === "a");
    expect(saved?.content.runs).toEqual([
      { text: "Hello", marks: [{ type: "bold" }] },
      { text: " world", marks: [] },
    ]);
    expect(lastDrafts.map((d) => d.id)).toEqual(["a", "b"]);
  });

  it("toggles a checklist line from its checkbox", async () => {
    renderPureUi(<Harness />);
    const box = await screen.findByRole("checkbox");
    await act(async () => {
      fireEvent.click(box);
    });
    expect(lastDrafts.find((d) => d.id === "b")?.properties.checked).toBe(true);
    expect(docToDrafts).toBeTruthy();
  });
});

describe("toolbar comment button", () => {
  it("anchors the comment to the selected text, and only opens the panel without a selection", async () => {
    const onCommentStart = vi.fn();
    const onOpenComments = vi.fn();
    renderPureUi(<Harness callbacks={{ onCommentStart, onOpenComments }} />);
    const comment = await screen.findByRole("button", {
      name: docsCopy.toolbar.comment,
    });

    await act(async () => {
      fireEvent.click(comment);
    });
    expect(onOpenComments).toHaveBeenCalledTimes(1);
    expect(onCommentStart).not.toHaveBeenCalled();

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "select" }));
    });
    await act(async () => {
      fireEvent.click(comment);
    });
    expect(onCommentStart).toHaveBeenCalledTimes(1);
  });
});

describe("slash menu matching", () => {
  it("lists every block with its shortcut and filters by name", () => {
    expect(matchSlashItems(docsCopy, "").length).toBeGreaterThan(10);
    const found = matchSlashItems(docsCopy, "check");
    expect(found.map((i) => i.type)).toContain("todo");
    expect(matchSlashItems(docsCopy, "zzzz")).toEqual([]);
  });
});
