import { describe, expect, it } from "vitest";
import type { Card } from "../types/item-types";
import type { FieldDefinition } from "../types/field-types";
import { groupCardsByField, type FilterField } from "./view-state";

const definition: FieldDefinition = {
  id: "accepted",
  boardId: "b1",
  name: "Accepted",
  fieldType: "checkbox",
  options: [],
  position: 1,
  isHidden: false,
  isSystemField: false,
};
const field: FilterField = {
  id: "accepted",
  name: "Accepted",
  field: definition,
  builtin: false,
  read: (card) => card.fieldValues?.accepted,
};
const card = (id: string, accepted?: boolean) =>
  ({
    id,
    fieldValues: accepted === undefined ? {} : { accepted },
  }) as unknown as Card;
const naming = { person: (id: string) => id, bucket: (key: string) => key };

describe("grouping by a Checkbox field", () => {
  it("gives two groups, ticked and unticked, with the cards in each", () => {
    const lanes = groupCardsByField(
      [card("a", true), card("b", false), card("c", true)],
      field,
      undefined,
      naming,
    );

    expect(lanes.map((lane) => [lane.optionId, lane.total])).toEqual([
      ["false", 1],
      ["true", 2],
    ]);
  });
});
