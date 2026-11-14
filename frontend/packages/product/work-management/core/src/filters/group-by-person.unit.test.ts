import { describe, expect, it } from "vitest";
import type { Card } from "../types/item-types";
import type { FieldDefinition } from "../types/field-types";
import { groupCardsByField, type FilterField } from "./view-state";

const person: FilterField = {
  id: "who",
  name: "Assignee",
  field: {
    id: "who",
    boardId: "b1",
    name: "Assignee",
    fieldType: "person",
    options: [],
    position: 1,
    isHidden: false,
    isSystemField: false,
  } as FieldDefinition,
  builtin: false,
  read: (card) => card.fieldValues?.who,
};
const priority: FilterField = {
  id: "pri",
  name: "Priority",
  field: {
    id: "pri",
    boardId: "b1",
    name: "Priority",
    fieldType: "select",
    options: [
      { id: "high", label: "High", color: "" },
      { id: "low", label: "Low", color: "" },
    ],
    position: 2,
    isHidden: false,
    isSystemField: false,
  } as FieldDefinition,
  builtin: false,
  read: (card) => card.fieldValues?.pri,
};
const card = (id: string, values: Record<string, unknown>) =>
  ({ id, fieldValues: values }) as unknown as Card;
const naming = { person: (id: string) => id, bucket: (key: string) => key };

describe("grouping by a field", () => {
  it("shows an item with two assignees in each person's lane, and the board still holds it once", () => {
    const cards = [
      card("both", { who: ["ada", "bao"] }),
      card("ada-only", { who: ["ada"] }),
      card("nobody", {}),
    ];

    const lanes = groupCardsByField(cards, person, undefined, naming);

    expect(lanes.map((lane) => [lane.optionId, lane.total])).toEqual([
      ["ada", 2],
      ["bao", 1],
      [null, 1],
    ]);
    // Q13: the lanes hold the item twice, the board's own count is the cards it was given.
    expect(lanes.reduce((sum, lane) => sum + lane.total, 0)).toBe(4);
    expect(
      new Set(lanes.flatMap((lane) => lane.cards.map((c) => c.id))).size,
    ).toBe(cards.length);
  });

  it("puts an item whose option was deleted in the No value lane", () => {
    const lanes = groupCardsByField(
      [
        card("kept", { pri: "high" }),
        card("orphan", { pri: "deleted-option" }),
        card("empty", {}),
      ],
      priority,
      undefined,
      naming,
    );

    const none = lanes.at(-1)!;
    expect(none.optionId).toBeNull();
    expect(none.cards.map((c) => c.id).sort()).toEqual(["empty", "orphan"]);
    expect(lanes.find((lane) => lane.optionId === "high")!.total).toBe(1);
  });
});
