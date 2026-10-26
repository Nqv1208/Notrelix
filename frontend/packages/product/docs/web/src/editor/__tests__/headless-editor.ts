import { Editor } from "@tiptap/core";
import { richText, type BlockDraft } from "@notrelix/docs-core";
import { docsCopy } from "../../verification/docs-ui-fixtures";
import { draftsToDoc } from "../block-mapping";
import { createDocExtensions, type ContextHolder } from "../extensions";

/** jsdom has no layout: ProseMirror measures ranges when it scrolls the caret. */
export function stubLayout(): void {
  const rect = {
    x: 0,
    y: 0,
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    width: 0,
    height: 0,
    toJSON: () => ({}),
  };
  Range.prototype.getClientRects = () => [] as unknown as DOMRectList;
  Range.prototype.getBoundingClientRect = () => rect as DOMRect;
  Element.prototype.getClientRects = () => [] as unknown as DOMRectList;
  (
    Text.prototype as unknown as { getClientRects: () => DOMRectList }
  ).getClientRects = () => [] as unknown as DOMRectList;
}

export function paragraph(id: string, text = ""): BlockDraft {
  return {
    id,
    type: "paragraph",
    content: richText(text ? [{ text, marks: [] }] : []),
    properties: {},
  };
}

/** An editor over the page's blocks, without React. */
export function headlessEditor(drafts: readonly BlockDraft[]): Editor {
  const holder: ContextHolder = {
    current: {
      copy: docsCopy,
      readOnly: false,
      resolveTask: (id) => ({ id, restricted: true as const }),
      viewOf: () => undefined,
    },
    version: 0,
    listeners: new Set(),
  };
  return new Editor({
    element: document.createElement("div"),
    extensions: createDocExtensions({
      context: holder,
      placeholder: "",
      emptyPlaceholder: "",
    }),
    content: draftsToDoc(drafts),
  });
}

/** Types text the way the keyboard does, so input rules run. */
export function typeText(editor: Editor, text: string): void {
  for (const char of text) {
    const { from, to } = editor.state.selection;
    const handled = editor.view.someProp("handleTextInput", (handler) =>
      handler(editor.view, from, to, char, () =>
        editor.state.tr.insertText(char, from, to),
      ),
    );
    if (!handled)
      editor.view.dispatch(editor.state.tr.insertText(char, from, to));
  }
}

export function pressEnter(editor: Editor): boolean {
  const event = new KeyboardEvent("keydown", { key: "Enter", bubbles: true });
  return Boolean(
    editor.view.someProp("handleKeyDown", (handler) =>
      handler(editor.view, event),
    ),
  );
}
