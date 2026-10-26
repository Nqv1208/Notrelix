import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { act, fireEvent, renderPureUi, screen } from "@notrelix/testing";
import type { Editor } from "@tiptap/core";
import { TextSelection } from "@tiptap/pm/state";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import type { DocMarkTargets } from "../../components/doc-page-models";
import { MarkChips } from "../mark-chips";
import { headlessEditor, paragraph, stubLayout } from "./headless-editor";

beforeAll(stubLayout);

const editors: Editor[] = [];
afterEach(() => {
  for (const editor of editors.splice(0)) {
    (editor.options.element as HTMLElement).remove();
    editor.destroy();
  }
});

function chipEditor(mark: "mentionUser" | "pageRef", attrs: object) {
  const editor = headlessEditor([paragraph("a", "Ping Lan Pham today")]);
  editors.push(editor);
  document.body.append(editor.options.element as HTMLElement);
  editor.view.dispatch(
    editor.state.tr.addMark(6, 14, editor.schema.marks[mark]!.create(attrs)),
  );
  return editor;
}

const TARGETS = (overrides: Partial<DocMarkTargets> = {}): DocMarkTargets => ({
  person: (id) => ({
    id,
    name: id === "u1" ? "Linh Tran" : "—",
    initials: id === "u1" ? "LT" : "?",
    tone: "blue",
  }),
  page: (id) => (id === "p1" ? { title: "Runbook" } : null),
  ...overrides,
});

/** The editor announces itself on a timer once mounted; chips listen from then. */
const settle = () =>
  act(async () => {
    await new Promise((resolve) => setTimeout(resolve, 0));
  });

const chip = (editor: Editor, selector: string) =>
  editor.view.dom.querySelector(selector)!;

describe("mention chips and page references in the text", () => {
  it("opens the member card when a mention chip is clicked and closes on Escape", async () => {
    const editor = chipEditor("mentionUser", { userId: "u1" });
    renderPureUi(
      <MarkChips editor={editor} copy={docsCopy} targets={TARGETS()} />,
    );
    await settle();
    await act(async () => {
      fireEvent.click(chip(editor, "[data-user-id]"));
    });
    expect(await screen.findByText("Linh Tran")).toBeTruthy();
    expect(await screen.findByText(docsCopy.chips.member)).toBeTruthy();
    await act(async () => {
      fireEvent.keyDown(document.activeElement ?? document.body, {
        key: "Escape",
      });
    });
    expect(screen.queryByText("Linh Tran")).toBeNull();
  });

  it("opens the card from the keyboard with Alt+Enter on the chip under the caret", async () => {
    const editor = chipEditor("mentionUser", { userId: "u1" });
    editor.view.dispatch(
      editor.state.tr.setSelection(TextSelection.create(editor.state.doc, 9)),
    );
    renderPureUi(
      <MarkChips editor={editor} copy={docsCopy} targets={TARGETS()} />,
    );
    await settle();
    await act(async () => {
      fireEvent.keyDown(editor.view.dom, { key: "Enter", altKey: true });
    });
    expect(await screen.findByText("Linh Tran")).toBeTruthy();
  });

  it("names an unknown member without guessing", async () => {
    const editor = chipEditor("mentionUser", { userId: "gone" });
    renderPureUi(
      <MarkChips editor={editor} copy={docsCopy} targets={TARGETS()} />,
    );
    await settle();
    await act(async () => {
      fireEvent.click(chip(editor, "[data-user-id]"));
    });
    expect(await screen.findByText(docsCopy.chips.unknownMember)).toBeTruthy();
  });

  it("opens a readable page on Cmd-click while editing and says unavailable for any other", async () => {
    const openPage = vi.fn();
    const readable = chipEditor("pageRef", { pageId: "p1" });
    const view = renderPureUi(
      <MarkChips
        editor={readable}
        copy={docsCopy}
        targets={TARGETS({ openPage })}
      />,
    );
    await settle();
    await act(async () => {
      fireEvent.click(chip(readable, "[data-page-id]"));
    });
    expect(openPage).not.toHaveBeenCalled();
    await act(async () => {
      fireEvent.click(chip(readable, "[data-page-id]"), { metaKey: true });
    });
    expect(openPage).toHaveBeenCalledWith("p1");
    view.unmount();

    const hidden = chipEditor("pageRef", { pageId: "p9" });
    renderPureUi(
      <MarkChips editor={hidden} copy={docsCopy} targets={TARGETS()} />,
    );
    await settle();
    await act(async () => {
      fireEvent.click(chip(hidden, "[data-page-id]"), { ctrlKey: true });
    });
    expect(
      await screen.findByText(docsCopy.blocks.pageReference.unavailable),
    ).toBeTruthy();
  });
});
