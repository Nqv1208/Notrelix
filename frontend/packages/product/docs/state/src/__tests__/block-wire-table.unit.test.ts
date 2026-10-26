import { describe, expect, it } from "vitest";
import {
  decodeBlockBody,
  encodeProperties,
  tableFromWire,
  tableToWire,
} from "../model/block-wire";

const WIRE = {
  rows: [
    [
      { runs: [{ text: "A", marks: [{ type: "bold" }] }] },
      { runs: [], bg: "primary" },
    ],
    [{ runs: [{ text: "b", marks: [] }], bg: "none" }, { runs: [] }],
  ],
  headerRow: true,
  colWidths: [200, null],
};

describe("table wire shape", () => {
  it("reads cells, background, header flag and widths", () => {
    const table = tableFromWire(WIRE);
    expect(table.headerRow).toBe(true);
    expect(table.colWidths).toEqual([200, null]);
    expect(table.rows[0]?.[0]?.runs).toEqual([
      { text: "A", marks: [{ type: "bold" }] },
    ]);
    expect(table.rows[0]?.[1]?.bg).toBe("primary");
    expect(table.rows[1]?.[0]?.bg).toBeUndefined();
  });

  it("writes what it reads (the none background is dropped)", () => {
    const wire = tableToWire(tableFromWire(WIRE));
    expect(wire).toEqual({
      ...WIRE,
      rows: [
        WIRE.rows[0],
        [{ runs: [{ text: "b", marks: [] }] }, { runs: [] }],
      ],
    });
  });

  it("sends one width per column", () => {
    const wire = tableToWire({
      headerRow: false,
      colWidths: [90],
      rows: [[{ runs: [] }, { runs: [] }, { runs: [] }]],
    });
    expect(wire.colWidths).toEqual([90, null, null]);
  });

  it("decodes a table block and encodes its properties", () => {
    const body = decodeBlockBody("table", { content: {}, properties: WIRE });
    expect(body.properties.table?.rows).toHaveLength(2);
    expect(
      encodeProperties({ type: "table", properties: body.properties }),
    ).toMatchObject({ headerRow: true, colWidths: [200, null] });
  });

  it("treats a table without properties as empty", () => {
    expect(tableFromWire({}).rows).toEqual([]);
  });
});
