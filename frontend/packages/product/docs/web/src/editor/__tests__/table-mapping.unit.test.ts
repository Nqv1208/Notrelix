import { describe, expect, it } from "vitest";
import { richText, type BlockDraft, type TableData } from "@notrelix/docs-core";
import { docToDrafts, draftsToDoc } from "../block-mapping";

function tableDraft(table: TableData): BlockDraft {
  return {
    id: "tbl",
    type: "table",
    content: richText(),
    properties: { table },
  };
}

const TABLE: TableData = {
  headerRow: true,
  colWidths: [120, null, 300],
  rows: [
    [
      { runs: [{ text: "Name", marks: [{ type: "bold" }] }] },
      { runs: [{ text: "Owner", marks: [] }], bg: "accent" },
      { runs: [] },
    ],
    [
      {
        runs: [
          { text: "Ship ", marks: [] },
          { text: "docs", marks: [{ type: "link", href: "https://x.test" }] },
        ],
      },
      { runs: [{ text: "line one\nline two", marks: [] }] },
      {
        runs: [
          { text: "ok", marks: [{ type: "highlight", token: "warning" }] },
        ],
        bg: "danger",
      },
    ],
  ],
};

describe("table block mapping", () => {
  it("round-trips cells with marks, backgrounds, line breaks, header and widths", () => {
    const back = docToDrafts(draftsToDoc([tableDraft(TABLE)]));
    expect(back).toHaveLength(1);
    expect(back[0]?.type).toBe("table");
    expect(back[0]?.properties.table).toEqual(TABLE);
  });

  it("maps the header flag to header cells of the first row only", () => {
    const doc = draftsToDoc([tableDraft(TABLE)]);
    const rows = doc.content?.[0]?.content ?? [];
    expect(rows[0]?.content?.map((cell) => cell.type)).toEqual([
      "tableHeader",
      "tableHeader",
      "tableHeader",
    ]);
    expect(rows[1]?.content?.map((cell) => cell.type)).toEqual([
      "tableCell",
      "tableCell",
      "tableCell",
    ]);
    const plain = draftsToDoc([tableDraft({ ...TABLE, headerRow: false })]);
    expect(plain.content?.[0]?.content?.[0]?.content?.[0]?.type).toBe(
      "tableCell",
    );
    const reread = docToDrafts(plain)[0]?.properties.table;
    expect(reread?.headerRow).toBe(false);
  });

  it("pads short rows and keeps the block id", () => {
    const ragged: TableData = {
      headerRow: false,
      colWidths: [],
      rows: [[{ runs: [] }, { runs: [] }], [{ runs: [] }]],
    };
    const back = docToDrafts(draftsToDoc([tableDraft(ragged)]))[0];
    expect(back?.id).toBe("tbl");
    expect(back?.properties.table?.rows.map((row) => row.length)).toEqual([
      2, 2,
    ]);
  });

  it("clamps column widths to the producer's range", () => {
    const doc = draftsToDoc([
      tableDraft({ ...TABLE, colWidths: [10, 5000, null] }),
    ]);
    const wide = docToDrafts(doc)[0]?.properties.table?.colWidths;
    expect(wide).toEqual([40, 2000, null]);
  });
});
