import { beforeAll, describe, expect, it } from "vitest";
import { act, fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import type { Editor } from "@tiptap/core";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { docToDrafts } from "../block-mapping";
import { FindPanel } from "../find-panel";
import { findStateOf } from "../find-replace";
import { headlessEditor, paragraph, stubLayout } from "./headless-editor";

beforeAll(stubLayout);

const drafts = (): BlockDraft[] => [
  {
    id: "a",
    type: "paragraph",
    content: richText([
      { text: "Cat and ", marks: [] },
      { text: "cat", marks: [{ type: "bold" }] },
      { text: "s", marks: [] },
    ]),
    properties: {},
  },
  paragraph("b", "A CAT sat. Concatenate."),
];

const state = (editor: Editor) => findStateOf(editor.state)!;
const texts = (editor: Editor) =>
  docToDrafts(editor.getJSON()).map((d) =>
    d.content.runs.map((r) => r.text).join(""),
  );

describe("find and replace plugin", () => {
  it("finds across marks, ignores case by default and honours match case", () => {
    const editor = headlessEditor(drafts());
    editor.commands.openFind();
    editor.commands.setFindTerm("cat");
    // "Cat", "cat"(bold)+"s" is one text block: Cat, cat, CAT, Cat in Concatenate.
    expect(state(editor).matches).toHaveLength(4);
    editor.commands.setFindCaseSensitive(true);
    // The bold "cat" and the one inside "Concatenate".
    expect(state(editor).matches).toHaveLength(2);
    editor.commands.setFindCaseSensitive(false);
    expect(state(editor).matches).toHaveLength(4);
  });

  it("steps to the next and previous match and wraps around", () => {
    const editor = headlessEditor(drafts());
    editor.commands.openFind();
    editor.commands.setFindTerm("cat");
    const start = state(editor).index;
    expect(start).toBe(0);
    editor.commands.stepFind(-1);
    expect(state(editor).index).toBe(3);
    editor.commands.stepFind(1);
    expect(state(editor).index).toBe(0);
  });

  it("replaces the current match and moves on without looping on its own text", () => {
    const editor = headlessEditor(drafts());
    editor.commands.openFind(true);
    editor.commands.setFindTerm("cat");
    editor.commands.replaceCurrentMatch("catcat");
    expect(texts(editor)[0]).toBe("catcat and cats");
    // The new text matches too; the current match is the next one after it.
    expect(state(editor).matches[state(editor).index]?.from).toBeGreaterThan(8);
  });

  it("replaces every match as one undo step and keeps marks", () => {
    const editor = headlessEditor(drafts());
    editor.commands.openFind(true);
    editor.commands.setFindTerm("cat");
    editor.commands.replaceAllMatches("dog");
    expect(texts(editor)).toEqual(["dog and dogs", "A dog sat. Condogenate."]);
    expect(docToDrafts(editor.getJSON())[0]?.content.runs[1]).toEqual({
      text: "dog",
      marks: [{ type: "bold" }],
    });
    expect(state(editor).matches).toHaveLength(0);
    editor.commands.undo();
    expect(texts(editor)).toEqual(["Cat and cats", "A CAT sat. Concatenate."]);
  });

  it("does not replace in a read-only editor", () => {
    const editor = headlessEditor(drafts());
    editor.commands.openFind(true);
    editor.commands.setFindTerm("cat");
    editor.setEditable(false);
    expect(editor.commands.replaceAllMatches("x")).toBe(false);
    expect(texts(editor)[0]).toBe("Cat and cats");
  });

  it("keeps matches current while the member types", () => {
    const editor = headlessEditor(drafts());
    editor.commands.openFind();
    editor.commands.setFindTerm("sat");
    expect(state(editor).matches).toHaveLength(1);
    editor.commands.insertContentAt(1, "sat ");
    expect(state(editor).matches).toHaveLength(2);
  });
});

describe("find panel", () => {
  function setup(editable = true) {
    const editor = headlessEditor(drafts());
    editor.setEditable(editable);
    renderPureUi(<FindPanel editor={editor} copy={docsCopy} />);
    return editor;
  }

  it("shows nothing until it is opened with the keyboard shortcut or command", async () => {
    const editor = setup();
    expect(screen.queryByRole("search")).toBeNull();
    await act(async () => {
      editor.commands.openFind();
    });
    expect(
      screen.getByRole("search", { name: docsCopy.find.label }),
    ).toBeTruthy();
    expect(document.activeElement).toBe(
      screen.getByRole("textbox", { name: docsCopy.find.findLabel }),
    );
  });

  it("announces the match count and moves with the buttons", async () => {
    const editor = setup();
    await act(async () => {
      editor.commands.openFind();
    });
    const field = screen.getByRole("textbox", {
      name: docsCopy.find.findLabel,
    });
    await act(async () => {
      fireEvent.change(field, { target: { value: "cat" } });
    });
    const live = screen.getByRole("status");
    expect(live.textContent).toBe("1 / 4");
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: docsCopy.find.next }));
    });
    expect(live.textContent).toBe("2 / 4");
    await act(async () => {
      fireEvent.change(field, { target: { value: "zzz" } });
    });
    expect(live.textContent).toBe(docsCopy.find.none);
  });

  it("replaces all from the panel, says how many, and closes with Escape", async () => {
    const editor = setup();
    await act(async () => {
      editor.commands.openFind(true);
    });
    await act(async () => {
      fireEvent.change(
        screen.getByRole("textbox", { name: docsCopy.find.findLabel }),
        { target: { value: "cat" } },
      );
    });
    await act(async () => {
      fireEvent.change(
        screen.getByRole("textbox", { name: docsCopy.find.replaceLabel }),
        { target: { value: "dog" } },
      );
    });
    await act(async () => {
      fireEvent.click(
        screen.getByRole("button", { name: docsCopy.find.replaceAll }),
      );
    });
    expect(texts(editor)[0]).toBe("dog and dogs");
    expect(screen.getByRole("status").textContent).toBe("Đã thay 4 kết quả");
    await act(async () => {
      fireEvent.keyDown(screen.getByRole("search"), { key: "Escape" });
    });
    expect(screen.queryByRole("search")).toBeNull();
  });

  it("offers no replace field when the page is read-only", async () => {
    const editor = setup(false);
    await act(async () => {
      editor.commands.openFind(true);
    });
    expect(
      screen.queryByRole("textbox", { name: docsCopy.find.replaceLabel }),
    ).toBeNull();
  });
});
