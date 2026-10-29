import {
  specOf,
  type FieldContext,
  type FilterOperator,
} from "../field-types/field-type-registry";

/** `between` is a date range (from and to days, both included); the registry lists the single-sided date operators. */
export type ConditionOperator = FilterOperator | "between";

export interface FilterCondition {
  /** A board field id, or a built-in pseudo field (`$overdue`) the caller resolves. */
  readonly fieldId: string;
  readonly operator: ConditionOperator;
  /** Operands as strings so a condition survives the URL; none for is_empty, is_true, ... */
  readonly values: readonly string[];
}

/** One level only (WM W2): every condition is joined by the same AND or OR. */
export interface CompoundFilter {
  readonly join: "and" | "or";
  readonly conditions: readonly FilterCondition[];
}

export const emptyCompoundFilter = (): CompoundFilter => ({
  join: "and",
  conditions: [],
});

const normalise = (text: string): string =>
  text
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[đĐ]/g, "d")
    .toLowerCase()
    .trim();

const asList = (value: unknown): string[] =>
  (Array.isArray(value) ? value : [value])
    .filter((item) => item !== null && item !== undefined && item !== "")
    .map(String);

const day = (value: unknown): number => Date.parse(String(value).slice(0, 10));

function equalsValue(
  context: FieldContext,
  value: unknown,
  operand: string,
): boolean {
  switch (context.field.fieldType) {
    case "number":
    case "formula":
    case "rollup":
      return typeof value === "number"
        ? value === Number(operand)
        : normalise(String(value)) === normalise(operand);
    case "date":
      return day(value) === day(operand);
    case "select":
      return String(value) === operand;
    default:
      return (
        normalise(specOf(context.field).text(value, context)) ===
        normalise(operand)
      );
  }
}

/**
 * Whether one stored value satisfies a condition. Emptiness is decided by the
 * field type's own `isEmpty`; the negations (`not_contains`, `not_equals`) are
 * true for an empty value: nothing contains "x".
 */
export function matchesCondition(
  context: FieldContext,
  { operator, values }: Pick<FilterCondition, "operator" | "values">,
  value: unknown,
): boolean {
  const spec = specOf(context.field);
  const empty = spec.isEmpty(value);
  const [first = "", second = ""] = values;
  switch (operator) {
    case "is_empty":
      return empty;
    case "is_not_empty":
      return !empty;
    case "is_true":
      return value === true;
    case "is_false":
      return value !== true;
    case "contains":
      return (
        !empty &&
        normalise(spec.text(value, context)).includes(normalise(first))
      );
    case "not_contains":
      return (
        empty ||
        !normalise(spec.text(value, context)).includes(normalise(first))
      );
    case "equals":
      return !empty && equalsValue(context, value, first);
    case "not_equals":
      return empty || !equalsValue(context, value, first);
    case "gt":
    case "gte":
    case "lt":
    case "lte": {
      const order = Number(value) - Number(first);
      if (empty || first === "" || Number.isNaN(order)) return false;
      if (operator === "gt") return order > 0;
      if (operator === "gte") return order >= 0;
      return operator === "lt" ? order < 0 : order <= 0;
    }
    case "before":
    case "after": {
      if (empty || Number.isNaN(day(first))) return false;
      return operator === "before"
        ? day(value) < day(first)
        : day(value) > day(first);
    }
    case "between":
      return (
        !empty &&
        !Number.isNaN(day(first)) &&
        !Number.isNaN(day(second)) &&
        day(value) >= day(first) &&
        day(value) <= day(second)
      );
    case "has_any":
      return asList(value).some((id) => values.includes(id));
    case "has_all":
      return (
        values.length > 0 && values.every((id) => asList(value).includes(id))
      );
  }
}

export interface FilterSubject {
  /** The field's context and this item's value, or `undefined` when the board no longer has the field. */
  readonly resolve: (
    fieldId: string,
  ) => { context: FieldContext; value: unknown } | undefined;
}

export interface FilterOutcome {
  readonly matches: boolean;
  /** Fields a condition named that the board no longer has: ignored, and shown as a hint. */
  readonly staleFieldIds: readonly string[];
}

/** Evaluates the filter on one item; a condition on a missing field is ignored, never a mismatch. */
export function evaluateFilter(
  filter: CompoundFilter,
  subject: FilterSubject,
): FilterOutcome {
  const stale = new Set<string>();
  const results: boolean[] = [];
  for (const condition of filter.conditions) {
    const hit = subject.resolve(condition.fieldId);
    if (!hit) {
      stale.add(condition.fieldId);
      continue;
    }
    results.push(matchesCondition(hit.context, condition, hit.value));
  }
  const matches =
    results.length === 0 ||
    (filter.join === "and" ? results.every(Boolean) : results.some(Boolean));
  return { matches, staleFieldIds: [...stale] };
}

/* URL form: `c2:and~<field>.<op>.<value>.<value>~...`; parts are percent-encoded so `.` and `~` stay separators. */
const PREFIX = "c2:";

export const escapePart = (part: string): string =>
  encodeURIComponent(part).replace(
    /[.~!'()*]/g,
    (char) => `%${char.charCodeAt(0).toString(16).toUpperCase()}`,
  );

export function serializeCompoundFilter(filter: CompoundFilter): string {
  if (filter.conditions.length === 0) return "";
  const parts = filter.conditions.map((condition) =>
    [condition.fieldId, condition.operator, ...condition.values]
      .map(escapePart)
      .join("."),
  );
  return `${PREFIX}${[filter.join, ...parts].join("~")}`;
}

const OPERATORS: readonly string[] = [
  "contains",
  "not_contains",
  "equals",
  "not_equals",
  "is_empty",
  "is_not_empty",
  "gt",
  "gte",
  "lt",
  "lte",
  "before",
  "after",
  "is_true",
  "is_false",
  "has_any",
  "has_all",
  "between",
] satisfies ConditionOperator[];

export const decodePart = (part: string): string | null => {
  try {
    return decodeURIComponent(part);
  } catch {
    return null;
  }
};

/**
 * Reads a `c2:` string; a stale or hand-edited URL keeps only the conditions
 * it understands. Anything else (the legacy facet form) returns `undefined`.
 */
export function parseCompoundFilter(
  value: unknown,
): CompoundFilter | undefined {
  if (typeof value !== "string" || !value.startsWith(PREFIX)) return undefined;
  const [join, ...rest] = value.slice(PREFIX.length).split("~");
  const conditions: FilterCondition[] = [];
  for (const raw of rest) {
    const [fieldId, operator, ...values] = raw.split(".").map(decodePart);
    if (!fieldId || !operator || values.includes(null)) continue;
    if (!OPERATORS.includes(operator)) continue;
    conditions.push({
      fieldId,
      operator: operator as ConditionOperator,
      values: values as string[],
    });
  }
  return { join: join === "or" ? "or" : "and", conditions };
}
