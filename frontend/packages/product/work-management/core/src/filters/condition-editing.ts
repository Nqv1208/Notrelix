import { specOf } from "../field-types/field-type-registry";
import type { FieldDefinition } from "../types/field-types";
import {
  NONE_VALUE,
  resolveOperand,
  type CompoundFilter,
  type ConditionOperator,
  type FilterCondition,
} from "./compound-filter";

/** Bounds the builder keeps, so a view stays under the server's criteria cap. */
export const MAX_CONDITIONS = 20;
export const MAX_VALUE_LENGTH = 200;

/** Operators offered for a field, in the order the builder lists them (the registry's, plus `between` for dates). */
export function operatorsFor(field: FieldDefinition): ConditionOperator[] {
  const own = specOf(field).operators as readonly ConditionOperator[];
  if (field.fieldType === "date")
    return ["equals", "before", "after", "between", "is_empty", "is_not_empty"];
  if (field.fieldType === "select")
    return ["has_any", "equals", "not_equals", "is_empty", "is_not_empty"];
  return [...own];
}

/** Whether the operator reads operands (is_empty, is_true, ... do not). */
export function operatorTakesValue(operator: ConditionOperator): boolean {
  return !(
    operator === "is_empty" ||
    operator === "is_not_empty" ||
    operator === "is_true" ||
    operator === "is_false"
  );
}

/** The condition a new row starts with: the field's first operator, no value yet. */
export function newCondition(field: FieldDefinition): FilterCondition {
  const operator = operatorsFor(field)[0] ?? "is_empty";
  return { fieldId: field.id, operator, values: [] };
}

/** Switching the field resets the operator unless the new field offers it too; values carry over only for the same operator. */
export function withOperator(
  condition: FilterCondition,
  operator: ConditionOperator,
): FilterCondition {
  const takes = operatorTakesValue(operator);
  const multi = operator === "has_any" || operator === "has_all";
  const range = operator === "between";
  const keep = takes ? condition.values : [];
  return {
    ...condition,
    operator,
    values: multi || range ? keep : keep.slice(0, 1),
  };
}

/** A relative operand (`@...`) the resolver does not understand, or whose date is out of range. */
const unresolved = (operand: string): boolean =>
  operand.startsWith("@") && resolveOperand(operand) === operand;

export type ConditionIssue = "incomplete" | "range" | null;

/** Why a condition is not applied yet: no value, or a date range whose start is after its end. */
export function conditionIssue(condition: FilterCondition): ConditionIssue {
  if (!operatorTakesValue(condition.operator)) return null;
  if (condition.values.some(unresolved)) return "incomplete";
  if (condition.operator === "between") {
    const [from, to] = condition.values;
    if (!from || !to) return "incomplete";
    return resolveOperand(from) > resolveOperand(to) ? "range" : null;
  }
  const first = condition.values[0];
  if (first === undefined || first === "") return "incomplete";
  return null;
}

/** The filter as it applies: conditions that are incomplete or invalid are left out (not treated as a mismatch). */
export function effectiveFilter(filter: CompoundFilter): CompoundFilter {
  const conditions = filter.conditions.filter(
    (condition) => conditionIssue(condition) === null,
  );
  return conditions.length === filter.conditions.length
    ? filter
    : { ...filter, conditions };
}

export type DatePreset = "today" | "week" | "month" | "next7" | "overdue";

/** A preset as a condition on relative operands (`@today`, `@week-start`, ...): they resolve when the filter runs, so a saved "Tuần này" keeps meaning this week. */
export function presetCondition(
  fieldId: string,
  preset: DatePreset,
): FilterCondition {
  switch (preset) {
    case "today":
      return { fieldId, operator: "equals", values: ["@today"] };
    case "week":
      return {
        fieldId,
        operator: "between",
        values: ["@week-start", "@week-end"],
      };
    case "month":
      return {
        fieldId,
        operator: "between",
        values: ["@month-start", "@month-end"],
      };
    case "next7":
      return { fieldId, operator: "between", values: ["@today", "@today+6"] };
    case "overdue":
      return { fieldId, operator: "before", values: ["@today"] };
  }
}

export { NONE_VALUE };
