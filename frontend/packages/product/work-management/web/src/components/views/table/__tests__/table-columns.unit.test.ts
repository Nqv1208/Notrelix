import { describe, expect, it } from "vitest";
import {
  clampColumnWidth,
  columnWidth,
  tableGridTemplate,
  TABLE_COLUMNS,
  TABLE_MAX_COLUMN_WIDTH,
  TABLE_MIN_COLUMN_WIDTH,
  visibleTableColumns,
} from "../table-columns";

describe("table column widths", () => {
  it("clamps and rounds a width to what a column can be", () => {
    expect(clampColumnWidth(10)).toBe(TABLE_MIN_COLUMN_WIDTH);
    expect(clampColumnWidth(5000)).toBe(TABLE_MAX_COLUMN_WIDTH);
    expect(clampColumnWidth(200.6)).toBe(201);
  });

  it("uses the saved width, else the design width, ignoring junk", () => {
    const title = TABLE_COLUMNS.find((c) => c.id === "title")!;
    expect(columnWidth(title, {})).toBe(340);
    expect(columnWidth(title, { title: 420 })).toBe(420);
    expect(columnWidth(title, { title: 4 })).toBe(TABLE_MIN_COLUMN_WIDTH);
    expect(columnWidth(title, { title: Number.NaN })).toBe(340);
  });

  it("builds one grid template for the header and the rows", () => {
    const columns = visibleTableColumns([
      "start",
      "created",
      "completed",
      "checklist",
      "comments",
      "files",
    ]);
    expect(tableGridTemplate(columns)).toBe(
      "40px 84px 340px 140px 184px 132px 96px 120px 136px 120px",
    );
    expect(tableGridTemplate(columns, { key: 100, status: 200 })).toBe(
      "40px 100px 340px 200px 184px 132px 96px 120px 136px 120px",
    );
  });
});
