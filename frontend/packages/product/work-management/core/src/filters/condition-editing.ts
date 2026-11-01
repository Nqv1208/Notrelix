import { specOf } from "../field-types/field-type-registry";
import type { FieldDefinition } from "../types/field-types";
import {
  NONE_VALUE,
  type CompoundFilter,
  type ConditionOperator,
  type FilterCondition,
} from "./compound-filter";

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

export type ConditionIssue = "incomplete" | "range" | null;

/** Why a condition is not applied yet: no value, or a date range whose start is after its end. */
export function conditionIssue(condition: FilterCondition): ConditionIssue {
  if (!operatorTakesValue(condition.operator)) return null;
  if (condition.operator === "between") {
    const [from, to] = condition.values;
    if (!from || !to) return "incomplete";
    return from > to ? "range" : null;
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

const pad = (n: number) => String(n).padStart(2, "0");
const iso = (date: Date) =>
  `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;

/** A preset as a condition: `between` for the week, month and next seven days, `before` today for overdue. */
export function presetCondition(
  fieldId: string,
  preset: DatePreset,
  now: Date,
): FilterCondition {
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const shift = (days: number) =>
    new Date(today.getFullYear(), today.getMonth(), today.getDate() + days);
  switch (preset) {
    case "today":
      return { fieldId, operator: "equals", values: [iso(today)] };
    case "week": {
      const monday = shift(-((today.getDay() + 6) % 7));
      return {
        fieldId,
        operator: "between",
        values: [
          iso(monday),
          iso(
            new Date(
              monday.getFullYear(),
              monday.getMonth(),
              monday.getDate() + 6,
            ),
          ),
        ],
      };
    }
    case "month":
      return {
        fieldId,
        operator: "between",
        values: [
          iso(new Date(today.getFullYear(), today.getMonth(), 1)),
          iso(new Date(today.getFullYear(), today.getMonth() + 1, 0)),
        ],
      };
    case "next7":
      return {
        fieldId,
        operator: "between",
        values: [iso(today), iso(shift(6))],
      };
    case "overdue":
      return { fieldId, operator: "before", values: [iso(today)] };
  }
}

export { NONE_VALUE };
