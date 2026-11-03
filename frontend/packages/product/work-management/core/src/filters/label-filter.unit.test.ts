import { describe, expect, it } from "vitest";
import type { Card } from "../types/item-types";
import {
  buildFilterFields,
  cardMatchesFilter,
  LABEL_FIELD_ID,
} from "./view-state";

const fields = buildFilterFields({
  fieldDefinitions: [],
  members: [],
  labels: [
    { id: "bug", name: "Bug" },
    { id: "ui", name: "UI" },
  ],
  names: { title: "Title", label: "Labels", overdue: "Overdue" },
  isOverdue: () => false,
});
const names = { person: (id: string) => id, bucket: (key: string) => key };
const card = (...labels: string[]) =>
  ({
    id: labels.join("+") || "none",
    labels: labels.map((id) => ({ id })),
    fieldValues: {},
    members: [],
  }) as unknown as Card;
const matches = (operator: string, values: string[], c: Card) =>
  cardMatchesFilter(
    {
      join: "and",
      conditions: [{ fieldId: LABEL_FIELD_ID, operator, values }],
    } as never,
    fields,
    names.person as never,
    c,
  );

describe("filtering items by label", () => {
  it("has any of: keeps the items holding at least one of the labels", () => {
    expect(matches("has_any", ["bug", "ui"], card("ui"))).toBe(true);
    expect(matches("has_any", ["bug"], card("ui"))).toBe(false);
    expect(matches("has_any", ["bug"], card())).toBe(false);
  });

  it("has all of: keeps the items holding every label", () => {
    expect(matches("has_all", ["bug", "ui"], card("bug", "ui"))).toBe(true);
    expect(matches("has_all", ["bug", "ui"], card("bug"))).toBe(false);
  });

  it("a label of another board matches nothing", () => {
    expect(matches("has_any", ["elsewhere"], card("bug", "ui"))).toBe(false);
  });

  it("is empty and is not empty tell the unlabelled items from the labelled ones", () => {
    expect(matches("is_empty", [], card())).toBe(true);
    expect(matches("is_empty", [], card("bug"))).toBe(false);
    expect(matches("is_not_empty", [], card("bug"))).toBe(true);
  });
});
