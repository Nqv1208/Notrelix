import type {
  ComputedValue,
  FieldDefinition,
  FieldType,
} from "../types/field-types";

/** Operators a filter can use on a field; the filter UI (W2) offers a type's own list. */
export type FilterOperator =
  | "contains"
  | "not_contains"
  | "equals"
  | "not_equals"
  | "is_empty"
  | "is_not_empty"
  | "gt"
  | "gte"
  | "lt"
  | "lte"
  | "before"
  | "after"
  | "is_true"
  | "is_false"
  | "has_any"
  | "has_all";

/** What a type needs from the board to turn ids into words (people, options). */
export interface FieldContext {
  readonly field: FieldDefinition;
  readonly memberName: (userId: string) => string;
}

/** A draft typed in an inline editor, parsed to the value the write carries. */
export type DraftResult =
  | { readonly ok: true; readonly value: unknown }
  | { readonly ok: false; readonly error: DraftError };

export type DraftError =
  "invalid_number" | "invalid_link" | "invalid_date" | "read_only";

/**
 * One semantic contract per field type: what the type can do everywhere it
 * appears (Table cell, side sheet, filter, sort, group). Renderers and editors
 * bind to this in the web package; nothing here knows React.
 */
export interface FieldTypeSpec {
  readonly type: FieldType;
  /** A Formula or Rollup: evaluated by the backend, shown read-only with a ƒx glyph. */
  readonly computed: boolean;
  /** Whether a viewer with edit rights can write it inline. */
  readonly editable: boolean;
  readonly operators: readonly FilterOperator[];
  /** Whether "group by" can bucket by this field. */
  readonly groupable: boolean;
  /** The canonical empty test: null, "", [] and undefined are all empty. */
  readonly isEmpty: (value: unknown) => boolean;
  /** Orders two non-empty values; the null rule is applied by `compareFieldValues`. */
  readonly compare: (a: unknown, b: unknown, context: FieldContext) => number;
  /** The plain-text form of a value (search, aria-label, tooltip, sort fallback). */
  readonly text: (value: unknown, context: FieldContext) => string;
  /** Parses an inline editor's draft; empty clears the value (`null`). */
  readonly parseDraft: (draft: string) => DraftResult;
}

const collator = new Intl.Collator("vi", {
  sensitivity: "base",
  numeric: true,
});

const isEmptyValue = (value: unknown): boolean =>
  value === null ||
  value === undefined ||
  value === "" ||
  (Array.isArray(value) && value.length === 0);

const asString = (value: unknown): string =>
  typeof value === "string" ? value : value == null ? "" : String(value);

const compareText = (a: unknown, b: unknown): number =>
  collator.compare(asString(a), asString(b));

const compareNumber = (a: unknown, b: unknown): number => Number(a) - Number(b);

const compareDate = (a: unknown, b: unknown): number =>
  Date.parse(asString(a)) - Date.parse(asString(b));

const ok = (value: unknown): DraftResult => ({ ok: true, value });
const fail = (error: DraftError): DraftResult => ({ ok: false, error });

const textDraft = (draft: string): DraftResult =>
  draft.trim() === "" ? ok(null) : ok(draft);

const numberDraft = (draft: string): DraftResult => {
  const trimmed = draft.trim().replace(",", ".");
  if (trimmed === "") return ok(null);
  const value = Number(trimmed);
  return Number.isFinite(value) ? ok(value) : fail("invalid_number");
};

const linkDraft = (draft: string): DraftResult => {
  const trimmed = draft.trim();
  if (trimmed === "") return ok(null);
  try {
    const url = new URL(
      /^[a-z][a-z0-9+.-]*:/i.test(trimmed) ? trimmed : `https://${trimmed}`,
    );
    return url.protocol === "http:" || url.protocol === "https:"
      ? ok(url.toString())
      : fail("invalid_link");
  } catch {
    return fail("invalid_link");
  }
};

const dateDraft = (draft: string): DraftResult => {
  const trimmed = draft.trim();
  if (trimmed === "") return ok(null);
  return /^\d{4}-\d{2}-\d{2}$/.test(trimmed) &&
    !Number.isNaN(Date.parse(trimmed))
    ? ok(trimmed)
    : fail("invalid_date");
};

const readOnlyDraft = (): DraftResult => fail("read_only");

const optionLabel = (id: unknown, { field }: FieldContext): string =>
  field.options.find((option) => option.id === id)?.label ?? "";

const optionPosition = (id: unknown, { field }: FieldContext): number => {
  const index = field.options.findIndex((option) => option.id === id);
  return index < 0 ? Number.MAX_SAFE_INTEGER : index;
};

const base = {
  computed: false,
  editable: true,
  isEmpty: isEmptyValue,
  groupable: false,
} as const;

const TEXT_OPERATORS: readonly FilterOperator[] = [
  "contains",
  "not_contains",
  "equals",
  "is_empty",
  "is_not_empty",
];

const text: FieldTypeSpec = {
  ...base,
  type: "text",
  operators: TEXT_OPERATORS,
  compare: compareText,
  text: (value) => asString(value),
  parseDraft: textDraft,
};

const NUMERIC_OPERATORS: readonly FilterOperator[] = [
  "equals",
  "not_equals",
  "gt",
  "gte",
  "lt",
  "lte",
  "is_empty",
  "is_not_empty",
];

const computed = (
  type: "formula" | "rollup",
  operators: readonly FilterOperator[],
): FieldTypeSpec => ({
  ...base,
  type,
  computed: true,
  editable: false,
  operators,
  // A computed value compares by what it holds: numbers numerically, the rest as text.
  compare: (a, b) =>
    typeof a === "number" && typeof b === "number" ? a - b : compareText(a, b),
  text: (value) => asString(value),
  parseDraft: readOnlyDraft,
});

/** The count of a Vote value (`{ count, voted }`); anything else counts as no votes. */
function voteCount(value: unknown): number {
  const count = (value as { count?: unknown } | null | undefined)?.count;
  return typeof count === "number" && Number.isFinite(count) ? count : 0;
}

/** One entry per field type; the single place that knows what a type may do. */
export const fieldTypeRegistry: Readonly<Record<FieldType, FieldTypeSpec>> = {
  text,
  long_text: { ...text, type: "long_text" },
  link: {
    ...text,
    type: "link",
    operators: ["contains", "is_empty", "is_not_empty"],
    parseDraft: linkDraft,
  },
  number: {
    ...base,
    type: "number",
    operators: NUMERIC_OPERATORS,
    compare: compareNumber,
    text: (value) => asString(value),
    parseDraft: numberDraft,
  },
  checkbox: {
    ...base,
    type: "checkbox",
    // false is a value, not empty: an unticked box is "no", never "unset".
    isEmpty: (value) => value === null || value === undefined,
    operators: ["is_true", "is_false"],
    groupable: true,
    compare: (a, b) => Number(Boolean(a)) - Number(Boolean(b)),
    text: (value) => (value ? "true" : "false"),
    parseDraft: (draft) => ok(draft === "true"),
  },
  select: {
    ...base,
    type: "select",
    operators: ["equals", "not_equals", "has_any", "is_empty", "is_not_empty"],
    groupable: true,
    // Options sort by their board order, not alphabetically.
    compare: (a, b, context) =>
      optionPosition(a, context) - optionPosition(b, context),
    text: (value, context) => optionLabel(value, context),
    parseDraft: textDraft,
  },
  multi_select: {
    ...base,
    type: "multi_select",
    operators: ["has_any", "has_all", "is_empty", "is_not_empty"],
    groupable: false,
    compare: (a, b, context) => {
      const first = (value: unknown) =>
        Array.isArray(value)
          ? Math.min(...value.map((id) => optionPosition(id, context)))
          : Number.MAX_SAFE_INTEGER;
      return first(a) - first(b);
    },
    text: (value, context) =>
      Array.isArray(value)
        ? value
            .map((id) => optionLabel(id, context))
            .filter(Boolean)
            .join(", ")
        : "",
    parseDraft: readOnlyDraft,
  },
  date: {
    ...base,
    type: "date",
    operators: ["equals", "before", "after", "is_empty", "is_not_empty"],
    groupable: true,
    compare: compareDate,
    text: (value) => asString(value).slice(0, 10),
    parseDraft: dateDraft,
  },
  person: {
    ...base,
    type: "person",
    operators: ["has_any", "is_empty", "is_not_empty"],
    groupable: true,
    // People sort by display name; an id the board does not know sorts as its id.
    compare: (a, b, { memberName }) => {
      const first = (value: unknown) =>
        Array.isArray(value)
          ? memberName(asString(value[0]))
          : memberName(asString(value));
      return collator.compare(first(a), first(b));
    },
    text: (value, { memberName }) =>
      (Array.isArray(value) ? value : [value])
        .filter((id) => !isEmptyValue(id))
        .map((id) => memberName(asString(id)))
        .join(", "),
    parseDraft: readOnlyDraft,
  },
  formula: computed("formula", [...NUMERIC_OPERATORS, "contains"]),
  rollup: computed("rollup", NUMERIC_OPERATORS),
  // A vote count: cast from the item's vote chip, never typed; sorts and filters by the count.
  vote: {
    ...base,
    type: "vote",
    editable: false,
    operators: NUMERIC_OPERATORS,
    isEmpty: (value) => voteCount(value) === 0,
    compare: (a, b) => voteCount(a) - voteCount(b),
    text: (value) => String(voteCount(value)),
    parseDraft: readOnlyDraft,
  },
  // Types the board still carries but this contract does not edit inline.
  timeline: {
    ...text,
    type: "timeline",
    editable: false,
    parseDraft: readOnlyDraft,
  },
  linked_page: {
    ...text,
    type: "linked_page",
    editable: false,
    parseDraft: readOnlyDraft,
  },
  progress: {
    ...text,
    type: "progress",
    editable: false,
    parseDraft: readOnlyDraft,
  },
  relation: {
    ...text,
    type: "relation",
    editable: false,
    parseDraft: readOnlyDraft,
  },
};

/** The spec of a field's type. */
export function specOf(
  field: Pick<FieldDefinition, "fieldType">,
): FieldTypeSpec {
  return fieldTypeRegistry[field.fieldType] ?? fieldTypeRegistry.text;
}

/**
 * Orders two values of a field. The null rule (WM §151): empty values sort
 * last when ascending and first when descending; the direction flips only
 * the order of the values that exist.
 */
export function compareFieldValues(
  context: FieldContext,
  a: unknown,
  b: unknown,
  direction: "asc" | "desc",
): number {
  const spec = specOf(context.field);
  const emptyA = spec.isEmpty(a);
  const emptyB = spec.isEmpty(b);
  if (emptyA && emptyB) return 0;
  if (emptyA) return direction === "asc" ? 1 : -1;
  if (emptyB) return direction === "asc" ? -1 : 1;
  const order = spec.compare(a, b, context);
  return direction === "asc" ? order : -order;
}

/** The value a cell shows for a field: the computed result for Formula and Rollup, else the stored one. */
export function cellValueOf(
  field: Pick<FieldDefinition, "id" | "fieldType">,
  card: {
    readonly fieldValues?: Record<string, unknown>;
    readonly computedValues?: Record<string, ComputedValue>;
  },
): { readonly value: unknown; readonly computed?: ComputedValue } {
  if (!specOf(field).computed) return { value: card.fieldValues?.[field.id] };
  const computedValue = card.computedValues?.[field.id];
  const value =
    computedValue &&
    (computedValue.state === "ok" || computedValue.state === "stale")
      ? computedValue.value
      : undefined;
  return computedValue ? { value, computed: computedValue } : { value };
}

/** The group key of a value ("" for empty) when the type can be grouped. */
export function groupKeyOf(
  field: FieldDefinition,
  value: unknown,
): string | null {
  const spec = specOf(field);
  if (!spec.groupable) return null;
  if (spec.isEmpty(value)) return "";
  return Array.isArray(value) ? asString(value[0]) : asString(value);
}
