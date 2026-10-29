import { describe, expect, it } from "vitest";
import type { FieldDefinition } from "../types/field-types";
import { isReadOnlyFieldType, tableCustomFields } from "./table-fields";

const field = (
  id: string,
  fieldType: FieldDefinition["fieldType"],
  patch: Partial<FieldDefinition> = {},
): FieldDefinition => ({
  id,
  boardId: "board-1",
  name: id,
  fieldType,
  options: [],
  position: 1,
  isHidden: false,
  isSystemField: false,
  ...patch,
});

describe("tableCustomFields", () => {
  const fields = [
    field("title", "text", { isSystemField: true, position: 0 }),
    field("status", "select", {
      isSystemField: true,
      semanticRole: "status",
      position: 1,
    }),
    field("assignee", "person", { isSystemField: true, position: 2 }),
    field("due", "date", { isSystemField: true, position: 3 }),
    field("note", "long_text", { position: 9 }),
    field("points", "number", { position: 5 }),
    field("hidden", "text", { isHidden: true, position: 6 }),
    field("timeline", "timeline", { position: 7 }),
    field("reviewer", "person", { position: 8 }),
    field("deadline", "date", { position: 10 }),
    field("double", "formula", { position: 11 }),
  ];

  it("lists the visible custom fields the table can show, in column order", () => {
    expect(tableCustomFields(fields).map((entry) => entry.id)).toEqual([
      "points",
      "reviewer",
      "note",
      "deadline",
      "double",
    ]);
  });

  it("leaves out the item's own Assignee and Due even when they are not system fields", () => {
    const ids = tableCustomFields([
      field("owner", "person", { position: 1 }),
      field("when", "date", { position: 2 }),
      field("second-person", "person", { position: 3 }),
    ]).map((entry) => entry.id);
    expect(ids).toEqual(["second-person"]);
  });

  it("knows which types are read-only", () => {
    expect(isReadOnlyFieldType(field("f", "formula"))).toBe(true);
    expect(isReadOnlyFieldType(field("r", "rollup"))).toBe(true);
    expect(isReadOnlyFieldType(field("t", "timeline"))).toBe(true);
    expect(isReadOnlyFieldType(field("n", "number"))).toBe(false);
  });
});
