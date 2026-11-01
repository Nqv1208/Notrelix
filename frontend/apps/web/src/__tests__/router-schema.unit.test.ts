import { describe, it, expect } from "vitest";
import { boardSearchSchema } from "../router";

describe("boardSearchSchema", () => {
  it("parses valid search parameters with defaults", () => {
    const result = boardSearchSchema.parse({});
    expect(result).toEqual({ view: "kanban" });
  });

  it("validates allowed view enum values", () => {
    expect(boardSearchSchema.parse({ view: "table" }).view).toBe("table");
    expect(boardSearchSchema.parse({ view: "calendar" }).view).toBe("calendar");
    expect(boardSearchSchema.parse({ view: "timeline" }).view).toBe("timeline");

    // An unknown layout in the address opens the default one instead of failing the page.
    expect(boardSearchSchema.parse({ view: "invalid-view" }).view).toBe(
      "kanban",
    );
  });

  it("preserves optional filter, sort, groupBy, and item params", () => {
    const parsed = boardSearchSchema.parse({
      view: "table",
      filter: "status:in_progress",
      sort: "dueDate:asc",
      groupBy: "status",
      item: "item-123",
    });

    expect(parsed).toEqual({
      view: "table",
      filter: "status:in_progress",
      sort: "dueDate:asc",
      groupBy: "status",
      item: "item-123",
    });
  });
});
