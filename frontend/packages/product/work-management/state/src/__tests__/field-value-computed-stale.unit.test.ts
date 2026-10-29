import { describe, expect, it } from "vitest";
import type { Card, FieldDefinition } from "@notrelix/work-management-core";
import { applyFieldValue } from "../mutations/use-update-field-value";

const field = (
  id: string,
  fieldType: FieldDefinition["fieldType"],
  patch: Partial<FieldDefinition> = {},
): FieldDefinition => ({
  id,
  boardId: "b-1",
  name: id,
  fieldType,
  options: [],
  position: 1,
  isHidden: false,
  isSystemField: false,
  ...patch,
});

const fields = [
  field("assignee", "person", { isSystemField: true }),
  field("due", "date", { isSystemField: true }),
  field("points", "number"),
  field("reviewer", "person"),
  field("deadline", "date"),
  field("double", "formula"),
];

const card: Card = {
  id: "i-1",
  listId: "g-1",
  boardId: "b-1",
  workspaceId: "ws-1",
  title: "Việc",
  position: 1,
  status: "",
  isArchived: false,
  isDeleted: false,
  members: [],
  labels: [],
  checklists: [],
  dueAt: "2026-10-20T00:00:00.000Z",
  dueDate: "2026-10-20T00:00:00.000Z",
  fieldValues: { points: 13 },
  computedValues: {
    double: { state: "ok", value: 26 },
    broken: { state: "error", errorCode: "divide-by-zero" },
    none: { state: "empty" },
  },
  _count: { comments: 0, attachments: 0, checklistItems: 0 },
  createdAt: "2026-10-01T00:00:00.000Z",
};

const write = (fieldDefinitionId: string, value: unknown) =>
  applyFieldValue(card, { cardId: card.id, fieldDefinitionId, value }, fields, [
    {
      id: "m-1",
      userId: "u-1",
      name: "An",
      initials: "A",
      color: "#000",
    } as never,
  ]);

describe("applyFieldValue and computed values", () => {
  it("marks the calculated results stale until the board is read again, keeping their value", () => {
    const next = write("points", 21);
    expect(next.fieldValues.points).toBe(21);
    expect(next.computedValues).toEqual({
      double: { state: "stale", value: 26 },
      broken: { state: "error", errorCode: "divide-by-zero" },
      none: { state: "empty" },
    });
  });

  it("leaves a card without calculated results as it is", () => {
    const bare = applyFieldValue(
      { ...card, computedValues: undefined },
      { cardId: card.id, fieldDefinitionId: "points", value: 1 },
      fields,
      [],
    );
    expect(bare.computedValues).toBeUndefined();
  });
});

describe("applyFieldValue and the item's own properties", () => {
  it("a custom Date field is a plain value: the due date stays", () => {
    const next = write("deadline", "2026-11-01");
    expect(next.fieldValues.deadline).toBe("2026-11-01");
    expect(next.dueAt).toBe(card.dueAt);
  });

  it("the first Date field is the due date", () => {
    expect(write("due", "2026-11-01T00:00:00.000Z").dueAt).toBe(
      "2026-11-01T00:00:00.000Z",
    );
  });

  it("a custom Person field is a plain value: the assignees stay", () => {
    const next = write("reviewer", ["u-1"]);
    expect(next.fieldValues.reviewer).toEqual(["u-1"]);
    expect(next.members).toEqual([]);
  });

  it("the first Person field sets the assignees", () => {
    expect(write("assignee", ["u-1"]).members.map((m) => m.userId)).toEqual([
      "u-1",
    ]);
  });
});
