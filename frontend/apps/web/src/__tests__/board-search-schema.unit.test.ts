import { describe, expect, it } from "vitest";
import { boardSearchSchema } from "../router";
import { parseBoardSearch } from "../router/schemas/board-search-schema";

describe("boardSearchSchema", () => {
  it("defaults view and preserves route URL state fields", () => {
    expect(boardSearchSchema.parse({})).toEqual({ view: "kanban" });

    expect(
      boardSearchSchema.parse({
        view: "table",
        filter: "status=done",
        sort: "priority:asc",
        groupBy: "assignee",
        item: "item-100",
      }),
    ).toEqual({
      view: "table",
      filter: "status=done",
      sort: "priority:asc",
      groupBy: "assignee",
      item: "item-100",
    });
  });
});

describe("parseBoardSearch (UC-WM-H13)", () => {
  it("keeps the name of a layout the address gave that does not exist", () => {
    expect(parseBoardSearch({ view: "gantt" })).toEqual({
      view: "kanban",
      unknownView: "gantt",
    });
  });

  it("names nothing for a known layout or no layout, and ignores an unknownView in the address", () => {
    expect(parseBoardSearch({ view: "table" })).toEqual({ view: "table" });
    expect(parseBoardSearch({})).toEqual({ view: "kanban" });
    expect(parseBoardSearch({ view: "calendar", unknownView: "x" })).toEqual({
      view: "calendar",
    });
  });
});
