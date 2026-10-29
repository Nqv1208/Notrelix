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
import { resolveOperand } from "./compound-filter";

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
  it("stores date presets as relative operands, not today's dates", () => {
    expect(presetCondition("d", "week").values).toEqual([
      "@week-start",
      "@week-end",
    ]);
    expect(presetCondition("d", "month").values).toEqual([
      "@month-start",
      "@month-end",
    ]);
    expect(presetCondition("d", "next7").values).toEqual([
      "@today",
      "@today+6",
    ]);
    expect(presetCondition("d", "today")).toMatchObject({
      operator: "equals",
      values: ["@today"],
    });
    expect(presetCondition("d", "overdue")).toMatchObject({
      operator: "before",
      values: ["@today"],
    });
    expect(conditionIssue(presetCondition("d", "week"))).toBeNull();
  });
  it("resolves the presets on any now", () => {
    const env = { now: new Date("2026-10-18T05:00:00Z") };
    expect(resolveOperand("@week-start", env)).toBe("2026-10-12");
    expect(resolveOperand("@week-end", env)).toBe("2026-10-18");
    expect(resolveOperand("@month-end", env)).toBe("2026-10-31");
    expect(resolveOperand("@today+6", env)).toBe("2026-10-24");
    expect(resolveOperand("2026-01-01", env)).toBe("2026-01-01");
  });
});
