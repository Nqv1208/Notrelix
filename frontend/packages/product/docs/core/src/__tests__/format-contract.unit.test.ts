import { describe, expect, it } from "vitest";
import {
  normalizeMarks,
  normalizeRuns,
  propertiesFromWire,
  propertiesToWire,
  tableColumnCount,
  blockFromWire,
  tableFromWire,
  tableToWire,
  exportHtml,
  exportMarkdown,
  richText,
  type Block,
  type TableCell,
} from "../index";

const sup = { type: "superscript" } as const;
const sub = { type: "subscript" } as const;
const bold = { type: "bold" } as const;

describe("superscript and subscript marks", () => {
  it("are part of the closed mark set and sort between strike and underline", () => {
    expect(
      normalizeMarks([{ type: "underline" }, sub, { type: "strike" }]).map(
        (mark) => mark.type,
      ),
    ).toEqual(["strike", "subscript", "underline"]);
  });

  it("exclude each other: raised wins, like the producer", () => {
    expect(normalizeMarks([sub, sup, bold])).toEqual([bold, sup]);
    expect(normalizeMarks([sup, sub])).toEqual([sup]);
  });

  it("merge neighbouring runs with the same marks", () => {
    expect(
      normalizeRuns([
        { text: "x", marks: [sup] },
        { text: "2", marks: [sup] },
      ]),
    ).toEqual([{ text: "x2", marks: [sup] }]);
  });

  it("export as <sup> and <sub> in HTML and Markdown", () => {
    const blocks: Block[] = [
      {
        id: "a",
        pageId: "p",
        type: "paragraph",
        content: richText([
          { text: "H", marks: [] },
          { text: "2", marks: [sub] },
          { text: "O m", marks: [] },
          { text: "2", marks: [sup] },
        ]),
        properties: {},
        position: 1,
        parentId: null,
        createdById: "",
        updatedById: "",
        createdAt: "",
        updatedAt: "",
      },
    ];
    expect(exportHtml("T", blocks)).toContain("H<sub>2</sub>O m<sup>2</sup>");
    expect(exportMarkdown("T", blocks)).toContain(
      "H<sub>2</sub>O m<sup>2</sup>",
    );
  });
});

describe("indent and line spacing", () => {
  it("travel as text-block properties and drop their defaults", () => {
    expect(
      propertiesFromWire("paragraph", { indent: 2, lineSpacing: "relaxed" }),
    ).toEqual({ indent: 2, lineSpacing: "relaxed" });
    expect(propertiesFromWire("paragraph", { lineSpacing: "normal" })).toEqual(
      {},
    );
    expect(
      propertiesToWire("bulleted_list", {
        indent: 3,
        lineSpacing: "tight",
        align: "center",
      }),
    ).toEqual({ indent: 3, lineSpacing: "tight", align: "center" });
    expect(propertiesToWire("paragraph", { indent: 0 })).toEqual({});
  });

  it("are not offered to blocks that cannot take them", () => {
    expect(propertiesToWire("code", { indent: 2 })).toEqual({});
    expect(propertiesFromWire("divider", { indent: 2 })).toEqual({});
  });
});

describe("merged table cells", () => {
  const cell = (extra: Partial<TableCell> = {}): TableCell => ({
    runs: [],
    ...extra,
  });

  it("read and write colSpan and rowSpan, leaving spans of one out", () => {
    const table = tableFromWire({
      rows: [
        [
          { runs: [], colSpan: 2 },
          { runs: [], rowSpan: 2 },
        ],
        [{ runs: [], colSpan: 1, rowSpan: 1 }, { runs: [] }],
      ],
      headerRow: false,
      colWidths: [null, null, null],
    });
    expect(table.rows[0]?.[0]?.colSpan).toBe(2);
    expect(table.rows[0]?.[1]?.rowSpan).toBe(2);
    expect(table.rows[1]?.[0]).toEqual({ runs: [] });
    const wire = tableToWire(table) as { rows: Record<string, unknown>[][] };
    expect(wire.rows[0]?.[0]).toMatchObject({ colSpan: 2 });
    expect(wire.rows[1]?.[0]).toEqual({ runs: [] });
  });

  it("count the grid's columns through merged cells", () => {
    // Row 0: [wide 2][tall]; row 1: [a][b] under the wide cell, tall continues.
    const rows = [
      [cell({ colSpan: 2 }), cell({ rowSpan: 2 })],
      [cell(), cell()],
    ];
    expect(tableColumnCount(rows)).toBe(3);
    expect(
      tableColumnCount([
        [cell(), cell()],
        [cell(), cell()],
      ]),
    ).toBe(2);
    expect(tableColumnCount([])).toBe(0);
  });

  it("keep one width per grid column", () => {
    const wire = tableToWire({
      rows: [[cell({ colSpan: 2 })], [cell(), cell()]],
      headerRow: false,
      colWidths: [100, null],
    }) as { colWidths: unknown[] };
    expect(wire.colWidths).toEqual([100, null]);
  });
});

describe("table of contents block", () => {
  it("has no properties either way and reads from the producer name", () => {
    expect(propertiesToWire("table_of_contents", { align: "center" })).toEqual(
      {},
    );
    expect(propertiesFromWire("table_of_contents", { x: 1 })).toEqual({});
    const block = blockFromWire(
      {
        id: "t",
        pageId: "p",
        type: "TableOfContents",
        content: {},
        properties: {},
      } as never,
      1,
    );
    expect(block.type).toBe("table_of_contents");
  });
});
