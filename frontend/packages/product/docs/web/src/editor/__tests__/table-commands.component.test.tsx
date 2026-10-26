import { beforeAll, describe, expect, it } from "vitest";
import { docToDrafts } from "../block-mapping";
import {
  TABLE_COMMANDS,
  insertTableBlock,
  selectTableState,
  setCellBackground,
} from "../table-commands";
import { headlessEditor, paragraph, stubLayout } from "./headless-editor";

beforeAll(stubLayout);

function withTable() {
  const editor = headlessEditor([paragraph("a")]);
  editor.commands.focus("end");
  insertTableBlock(editor);
  return editor;
}

function table(editor: ReturnType<typeof headlessEditor>) {
  const drafts = docToDrafts(editor.getJSON());
  return drafts.find((draft) => draft.type === "table")?.properties.table;
}

describe("table commands", () => {
  it("inserts a 3x3 table with a header row and a paragraph below", () => {
    const editor = withTable();
    const drafts = docToDrafts(editor.getJSON());
    expect(drafts.map((draft) => draft.type)).toEqual(["table", "paragraph"]);
    expect(drafts[0]?.id).toBeTruthy();
    expect(table(editor)?.rows).toHaveLength(3);
    expect(table(editor)?.rows[0]).toHaveLength(3);
    expect(table(editor)?.headerRow).toBe(true);
    expect(selectTableState(editor).active).toBe(true);
  });

  it("adds and removes rows and columns", () => {
    const editor = withTable();
    TABLE_COMMANDS.rowAfter(editor);
    TABLE_COMMANDS.columnAfter(editor);
    expect(table(editor)?.rows).toHaveLength(4);
    expect(table(editor)?.rows[0]).toHaveLength(4);
    TABLE_COMMANDS.deleteRow(editor);
    TABLE_COMMANDS.deleteColumn(editor);
    expect(table(editor)?.rows).toHaveLength(3);
    expect(table(editor)?.rows[0]).toHaveLength(3);
  });

  it("toggles the header row", () => {
    const editor = withTable();
    TABLE_COMMANDS.toggleHeaderRow(editor);
    expect(table(editor)?.headerRow).toBe(false);
    TABLE_COMMANDS.toggleHeaderRow(editor);
    expect(table(editor)?.headerRow).toBe(true);
  });

  it("sets and clears the background of the cell under the caret", () => {
    const editor = withTable();
    setCellBackground(editor, "success");
    expect(selectTableState(editor).bg).toBe("success");
    expect(table(editor)?.rows[0]?.[0]?.bg).toBe("success");
    setCellBackground(editor, "none");
    expect(table(editor)?.rows[0]?.[0]?.bg).toBeUndefined();
  });

  it("keeps typed cell text as runs and Enter as a line break", () => {
    const editor = withTable();
    editor.commands.insertContent("one");
    editor.commands.setHardBreak();
    editor.commands.insertContent("two");
    expect(table(editor)?.rows[0]?.[0]?.runs).toEqual([
      { text: "one\ntwo", marks: [] },
    ]);
  });

  it("deletes the table", () => {
    const editor = withTable();
    TABLE_COMMANDS.deleteTable(editor);
    expect(docToDrafts(editor.getJSON()).map((d) => d.type)).not.toContain(
      "table",
    );
  });
});
