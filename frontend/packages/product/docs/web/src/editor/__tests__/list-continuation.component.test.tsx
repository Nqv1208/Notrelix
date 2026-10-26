import { beforeAll, describe, expect, it } from "vitest";
import {
  headlessEditor,
  paragraph,
  pressEnter,
  stubLayout,
  typeText,
} from "./headless-editor";

beforeAll(stubLayout);

function fresh() {
  const editor = headlessEditor([paragraph("a")]);
  editor.commands.focus("end");
  return editor;
}

const kinds = (editor: ReturnType<typeof fresh>) =>
  (editor.getJSON().content ?? []).map((node) => node.type);

describe("list items", () => {
  it.each([
    ["- ", "bulletedItem"],
    ["1. ", "numberedItem"],
    ["[ ] ", "todoItem"],
  ])(
    "start the next item of the same kind on Enter at the end of %s",
    (marker, kind) => {
      const editor = fresh();
      typeText(editor, `${marker}first`);

      expect(pressEnter(editor)).toBe(true);
      typeText(editor, "second");

      expect(kinds(editor)).toEqual([kind, kind]);
      expect(editor.getText()).toContain("second");
    },
  );

  it("leaves the list when Enter is pressed on an empty item", () => {
    const editor = fresh();
    typeText(editor, "- first");
    pressEnter(editor);

    pressEnter(editor);

    expect(kinds(editor)).toEqual(["bulletedItem", "paragraph"]);
  });

  it("splits an item in the middle into two items of the same kind", () => {
    const editor = fresh();
    typeText(editor, "- abcd");
    editor.commands.setTextSelection(editor.state.selection.from - 2);

    pressEnter(editor);

    expect(kinds(editor)).toEqual(["bulletedItem", "bulletedItem"]);
  });
});
