import { describe, expect, it } from "vitest";
import { boardSettingsSearchSchema } from "../router/schemas/board-settings-search-schema";

describe("board settings search schema", () => {
  it("opens the fields section by default and keeps no panel open", () => {
    expect(boardSettingsSearchSchema.parse({})).toEqual({ section: "fields" });
  });

  it("keeps the section, the open field or view and the view it came from", () => {
    expect(
      boardSettingsSearchSchema.parse({
        section: "views",
        view: "sv-kanban",
        from: "table",
      }),
    ).toEqual({ section: "views", view: "sv-kanban", from: "table" });
    expect(
      boardSettingsSearchSchema.parse({ section: "fields", field: "sf-due" }),
    ).toEqual({ section: "fields", field: "sf-due" });
  });

  it("opens the default section for an old access link instead of failing", () => {
    expect(boardSettingsSearchSchema.parse({ section: "access" })).toEqual({
      section: "fields",
    });
  });

  it("rejects a section or a board view it does not know", () => {
    expect(
      boardSettingsSearchSchema.safeParse({ section: "billing" }).success,
    ).toBe(false);
    expect(boardSettingsSearchSchema.safeParse({ from: "gantt" }).success).toBe(
      false,
    );
  });
});
