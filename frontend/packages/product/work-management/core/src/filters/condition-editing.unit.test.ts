import { describe, expect, it } from "vitest";
import type { FieldDefinition } from "../types/field-types";
import {
  conditionIssue,
  effectiveFilter,
  newCondition,
  operatorsFor,
  presetCondition,
  withOperator,
} from "./condition-editing";

const field = (fieldType: FieldDefinition["fieldType"]): FieldDefinition => ({
  id: "f",
  boardId: "b",
  name: "F",
  fieldType,
  options: [],
  position: 1,
  isHidden: false,
  isSystemField: false,
});

describe("condition editing", () => {
  it("lists a date's range operator and a select's 'is one of' first", () => {
    expect(operatorsFor(field("date"))).toContain("between");
    expect(operatorsFor(field("select"))[0]).toBe("has_any");
    expect(operatorsFor(field("person"))).toEqual([
      "has_any",
      "is_empty",
      "is_not_empty",
    ]);
    expect(newCondition(field("text"))).toEqual({
      fieldId: "f",
      operator: "contains",
      values: [],
    });
  });
  it("keeps one value when a multi operator becomes single and drops values for empty tests", () => {
    const c = {
      fieldId: "f",
      operator: "has_any" as const,
      values: ["a", "b"],
    };
    expect(withOperator(c, "equals").values).toEqual(["a"]);
    expect(withOperator(c, "is_empty").values).toEqual([]);
  });
  it("leaves out conditions with no value or a reversed range", () => {
    const ok = { fieldId: "a", operator: "contains" as const, values: ["x"] };
    const blank = { fieldId: "b", operator: "contains" as const, values: [""] };
    const reversed = {
      fieldId: "d",
      operator: "between" as const,
      values: ["2026-10-20", "2026-10-12"],
    };
    expect(conditionIssue(blank)).toBe("incomplete");
    expect(conditionIssue(reversed)).toBe("range");
    expect(
      conditionIssue({ fieldId: "p", operator: "is_empty", values: [] }),
    ).toBeNull();
    expect(
      effectiveFilter({ join: "and", conditions: [ok, blank, reversed] })
        .conditions,
    ).toEqual([ok]);
  });
  it("builds date presets for Monday 12/10/2026", () => {
    const now = new Date(2026, 9, 12, 10, 40);
    expect(presetCondition("d", "week", now).values).toEqual([
      "2026-10-12",
      "2026-10-18",
    ]);
    expect(presetCondition("d", "month", now).values).toEqual([
      "2026-10-01",
      "2026-10-31",
    ]);
    expect(presetCondition("d", "next7", now).values).toEqual([
      "2026-10-12",
      "2026-10-18",
    ]);
    expect(presetCondition("d", "today", now)).toMatchObject({
      operator: "equals",
      values: ["2026-10-12"],
    });
    expect(presetCondition("d", "overdue", now)).toMatchObject({
      operator: "before",
      values: ["2026-10-12"],
    });
    expect(presetCondition("d", "week", new Date(2026, 9, 18)).values[0]).toBe(
      "2026-10-12",
    );
  });
});
