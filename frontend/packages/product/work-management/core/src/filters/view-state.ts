import {
  compareFieldValues,
  groupKeyOf,
  specOf,
  type FieldContext,
} from "../field-types/field-type-registry";
import type { Card } from "../types/item-types";
import type { FieldDefinition, FieldType } from "../types/field-types";
import type { KanbanStatusLane } from "../types/kanban-types";
import type { SavedFilterCriteria } from "../types/saved-filter-types";
import {
  findPriorityField,
  findStatusField,
  priorityFromOptionName,
} from "../utils/status-lanes";
import {
  decodePart,
  emptyCompoundFilter,
  escapePart,
  evaluateFilter,
  parseCompoundFilter,
  serializeCompoundFilter,
  type CompoundFilter,
  type FilterCondition,
} from "./compound-filter";

/* ---------- the model ---------- */

export const DATE_BUCKETS = [
  "day",
  "week",
  "month",
  "quarter",
  "year",
] as const;
export type DateBucket = (typeof DATE_BUCKETS)[number];

/** A field a view groups or splits lanes by; `bucket` only for a date field. */
export interface FieldRef {
  readonly fieldId: string;
  readonly bucket?: DateBucket;
}

export interface SortLevel {
  readonly fieldId: string;
  readonly direction: "asc" | "desc";
}

export const MAX_SORT_LEVELS = 3;

/** What a member sees of a view: the filter, up to three sort levels, the grouping and the swimlane field. */
export interface ViewState {
  readonly filter: CompoundFilter;
  readonly sorts: readonly SortLevel[];
  readonly group?: FieldRef;
  readonly lane?: FieldRef;
}

export const emptyViewState = (): ViewState => ({
  filter: emptyCompoundFilter(),
  sorts: [],
});

export const isViewStateEmpty = (state: ViewState): boolean =>
  state.filter.conditions.length === 0 &&
  state.sorts.length === 0 &&
  !state.group &&
  !state.lane;

/* ---------- the fields a view can use ---------- */

export const TITLE_FIELD_ID = "$title";
export const LABEL_FIELD_ID = "$label";
export const OVERDUE_FIELD_ID = "$overdue";

export interface FilterField {
  readonly id: string;
  readonly name: string;
  readonly field: FieldDefinition;
  /** True for the title, label and overdue pseudo fields (no stored field value). */
  readonly builtin: boolean;
  readonly read: (card: Card) => unknown;
}

export interface FilterFieldInput {
  readonly fieldDefinitions: readonly FieldDefinition[];
  readonly members: readonly { userId: string; name: string }[];
  readonly labels: readonly { id: string; name: string }[];
  readonly names: { title: string; label: string; overdue: string };
  /** Whether the card counts as overdue (the same rule as its due wording). */
  readonly isOverdue: (card: Card) => boolean;
}

const pseudo = (
  id: string,
  name: string,
  fieldType: FieldType,
  options: FieldDefinition["options"] = [],
): FieldDefinition => ({
  id,
  boardId: "",
  name,
  fieldType,
  options,
  position: 0,
  isHidden: false,
  isSystemField: true,
});

/** Fields whose value the registry cannot compare or show in a condition. */
const UNFILTERABLE: ReadonlySet<FieldType> = new Set([
  "timeline",
  "linked_page",
  "progress",
  "relation",
]);

/**
 * The fields a filter, sort, group or swimlane can name: the title, the
 * board's own fields, then Nhãn and Quá hạn. Hidden and unsupported field
 * types are left out.
 */
export function buildFilterFields(input: FilterFieldInput): FilterField[] {
  const status = findStatusField(input.fieldDefinitions);
  const priority = findPriorityField(input.fieldDefinitions);
  const person = input.fieldDefinitions.find((f) => f.fieldType === "person");
  const due = input.fieldDefinitions.find((f) => f.fieldType === "date");
  const own = input.fieldDefinitions
    .filter((f) => !f.isHidden && !UNFILTERABLE.has(f.fieldType))
    .map<FilterField>((field) => ({
      id: field.id,
      name: field.name,
      field,
      builtin: false,
      read: (card) => {
        if (field.id === status?.id) return card.statusOptionId ?? undefined;
        if (field.id === priority?.id)
          return card.priorityOptionId ?? card.fieldValues[field.id];
        if (field.id === person?.id) {
          const stored = card.fieldValues[field.id];
          return Array.isArray(stored) && stored.length > 0
            ? stored
            : card.members.map((member) => member.userId);
        }
        if (field.id === due?.id)
          return card.fieldValues[field.id] ?? card.dueAt ?? card.dueDate;
        const { value } = cellValueFor(field, card);
        return value;
      },
    }));
  const title: FilterField = {
    id: TITLE_FIELD_ID,
    name: input.names.title,
    field: pseudo(TITLE_FIELD_ID, input.names.title, "text"),
    builtin: true,
    read: (card) => card.title,
  };
  const label: FilterField = {
    id: LABEL_FIELD_ID,
    name: input.names.label,
    field: pseudo(
      LABEL_FIELD_ID,
      input.names.label,
      "multi_select",
      input.labels.map((l) => ({ id: l.id, label: l.name, color: "" })),
    ),
    builtin: true,
    read: (card) => card.labels.map((l) => l.id),
  };
  const overdue: FilterField = {
    id: OVERDUE_FIELD_ID,
    name: input.names.overdue,
    field: pseudo(OVERDUE_FIELD_ID, input.names.overdue, "checkbox"),
    builtin: true,
    read: (card) => input.isOverdue(card),
  };
  return [title, ...own, label, overdue];
}

function cellValueFor(field: FieldDefinition, card: Card) {
  const computed = specOf(field).computed;
  if (!computed) return { value: card.fieldValues[field.id] };
  const result = card.computedValues?.[field.id];
  return {
    value:
      result && (result.state === "ok" || result.state === "stale")
        ? result.value
        : undefined,
  };
}

export type MemberNames = (userId: string) => string;

export function fieldContext(
  field: FilterField,
  memberName: MemberNames,
): FieldContext {
  return { field: field.field, memberName };
}

/* ---------- filter ---------- */

export interface FilterOutput {
  readonly cards: readonly Card[];
  /** Field ids named by the filter that the board no longer has. */
  readonly staleFieldIds: readonly string[];
}

export function staleFieldIdsOf(
  filter: CompoundFilter,
  fields: readonly FilterField[],
): string[] {
  const known = new Set(fields.map((f) => f.id));
  return [
    ...new Set(
      filter.conditions.map((c) => c.fieldId).filter((id) => !known.has(id)),
    ),
  ];
}

/** Whether a card passes the compound filter; conditions on missing fields are ignored. */
export function cardMatchesFilter(
  filter: CompoundFilter,
  fields: readonly FilterField[],
  memberName: MemberNames,
  card: Card,
): boolean {
  if (filter.conditions.length === 0) return true;
  const byId = new Map(fields.map((f) => [f.id, f]));
  return evaluateFilter(filter, {
    resolve: (fieldId) => {
      const found = byId.get(fieldId);
      return found
        ? {
            context: fieldContext(found, memberName),
            value: found.read(card),
          }
        : undefined;
    },
  }).matches;
}

/** Applies the filter to lanes, keeping lane `total` (the lane before the filter). */
export function filterLanesByCompound(
  lanes: readonly KanbanStatusLane[],
  filter: CompoundFilter,
  fields: readonly FilterField[],
  memberName: MemberNames,
): KanbanStatusLane[] {
  if (filter.conditions.length === 0) return [...lanes];
  return lanes.map((lane) => ({
    ...lane,
    cards: lane.cards.filter((card) =>
      cardMatchesFilter(filter, fields, memberName, card),
    ),
  }));
}

/* ---------- sort ---------- */

/** Stable multi-level sort; the null rule (empty last ascending, first descending) is the registry's. */
export function sortCardsByLevels<T extends Card>(
  cards: readonly T[],
  levels: readonly SortLevel[],
  fields: readonly FilterField[],
  memberName: MemberNames,
): T[] {
  const byId = new Map(fields.map((f) => [f.id, f]));
  const usable = levels
    .map((level) => ({ level, field: byId.get(level.fieldId) }))
    .filter(
      (entry): entry is { level: SortLevel; field: FilterField } =>
        entry.field !== undefined,
    );
  if (usable.length === 0) return [...cards];
  return cards
    .map((card, index) => ({ card, index }))
    .sort((a, b) => {
      for (const { level, field } of usable) {
        const order = compareFieldValues(
          fieldContext(field, memberName),
          field.read(a.card),
          field.read(b.card),
          level.direction,
        );
        if (order !== 0) return order;
      }
      return a.index - b.index;
    })
    .map((entry) => entry.card);
}

/* ---------- group and date buckets ---------- */

export function dateBucketFields(
  fields: readonly FilterField[],
): FilterField[] {
  return fields.filter((f) => f.field.fieldType === "date");
}

/** Fields a view can group by or split lanes by (registry `groupable`: select, person, date, checkbox). */
export function groupableFields(fields: readonly FilterField[]): FilterField[] {
  return fields.filter(
    (f) => !f.builtin && specOf(f.field).groupable && !specOf(f.field).computed,
  );
}

const pad = (n: number) => String(n).padStart(2, "0");

/** The day (YYYY-MM-DD) of a date value, in `timeZone` when the value is an instant. */
export function dayOf(value: unknown, timeZone?: string): string | null {
  if (typeof value !== "string" || value === "") return null;
  if (/^\d{4}-\d{2}-\d{2}$/.test(value)) return value;
  const time = Date.parse(value);
  if (Number.isNaN(time)) return null;
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: timeZone ?? "UTC",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(time);
  return parts;
}

/** The bucket a day falls in: its day, the Monday of its week, `YYYY-MM`, `YYYY-Qn` or `YYYY`. */
export function bucketKeyOf(day: string, bucket: DateBucket): string {
  const [y, m, d] = day.split("-").map(Number) as [number, number, number];
  switch (bucket) {
    case "day":
      return day;
    case "week": {
      const date = new Date(Date.UTC(y, m - 1, d));
      const back = (date.getUTCDay() + 6) % 7;
      date.setUTCDate(date.getUTCDate() - back);
      return `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}`;
    }
    case "month":
      return `${y}-${pad(m)}`;
    case "quarter":
      return `${y}-Q${Math.floor((m - 1) / 3) + 1}`;
    case "year":
      return String(y);
  }
}

export interface LaneNaming {
  readonly person: MemberNames;
  /** The label of a date bucket key ("Tuần 12/10 – 18/10/2026"). */
  readonly bucket: (key: string, bucket: DateBucket) => string;
  readonly timeZone?: string;
}

/**
 * Splits cards into lanes by a field: one per Select option (option order),
 * per person (by name) or per date bucket (chronological); cards with no value
 * are the last lane (`optionId: null`, empty name). Same lane model as the
 * Status lanes, so the Table and Kanban render them alike.
 */
export function groupCardsByField(
  cards: readonly Card[],
  field: FilterField,
  bucket: DateBucket | undefined,
  naming: LaneNaming,
): KanbanStatusLane[] {
  const type = field.field.fieldType;
  const keyOf = (card: Card): string => {
    const value = field.read(card);
    if (type === "date") {
      const day = dayOf(value, naming.timeZone);
      return day ? bucketKeyOf(day, bucket ?? "day") : "";
    }
    return groupKeyOf(field.field, value) ?? "";
  };
  const byKey = new Map<string, Card[]>();
  for (const card of cards) {
    const key = keyOf(card);
    byKey.set(key, [...(byKey.get(key) ?? []), card]);
  }
  let keys = [...byKey.keys()].filter((key) => key !== "");
  if (type === "select" || type === "checkbox") {
    const order = field.field.options.map((o) => o.id);
    keys = type === "select" ? order : keys.sort();
  } else if (type === "person") {
    keys = keys.sort((a, b) =>
      naming.person(a).localeCompare(naming.person(b), "vi"),
    );
  } else {
    keys = keys.sort();
  }
  const name = (key: string): string => {
    if (type === "select")
      return field.field.options.find((o) => o.id === key)?.label ?? key;
    if (type === "person") return naming.person(key);
    if (type === "checkbox") return key === "true" ? "✓" : "–";
    return naming.bucket(key, bucket ?? "day");
  };
  const lanes: KanbanStatusLane[] = keys.map((key) => {
    const laneCards = byKey.get(key) ?? [];
    return {
      optionId: key,
      name: name(key),
      category: null,
      color:
        type === "select"
          ? field.field.options.find((o) => o.id === key)?.color || null
          : null,
      cards: laneCards,
      total: laneCards.length,
    };
  });
  const none = byKey.get("") ?? [];
  if (none.length > 0) {
    lanes.push({
      optionId: null,
      name: "",
      category: null,
      color: null,
      cards: none,
      total: none.length,
    });
  }
  return lanes;
}

/* ---------- swimlane drop ---------- */

export type LaneDropRefusal = "lane_readonly" | "lane_derived" | "wip";

export interface LaneDropInput {
  readonly laneField: FilterField;
  /** Column (Status or other Select) option id of the target cell, and the card's own. */
  readonly fromColumn: string | null;
  readonly toColumn: string | null;
  /** The lane group key the card is in and the target lane's (`null`: the "none" lane). */
  readonly fromLane: string | null;
  readonly toLane: string | null;
  /** The target column's WIP limit and how many cards the cell column holds now. */
  readonly wip?: { readonly limit: number; readonly count: number } | null;
}

export type LaneDropPlan =
  | {
      readonly ok: true;
      readonly column: string | null | undefined;
      /** The new value of the lane field, or `undefined` when the lane did not change. */
      readonly lane: unknown;
      readonly laneChanged: boolean;
      readonly columnChanged: boolean;
    }
  | { readonly ok: false; readonly reason: LaneDropRefusal };

/**
 * What a drop on a swimlane cell writes: the column field when the column
 * changed and the lane field when the lane changed (both or one), or why it
 * is refused (a read-only or derived lane field, a full WIP column).
 */
export function planLaneDrop(input: LaneDropInput): LaneDropPlan {
  const columnChanged = input.fromColumn !== input.toColumn;
  const laneChanged = input.fromLane !== input.toLane;
  const spec = specOf(input.laneField.field);
  if (laneChanged) {
    if (spec.computed || !spec.editable || input.laneField.builtin)
      return { ok: false, reason: "lane_readonly" };
    if (input.laneField.field.fieldType === "date")
      return { ok: false, reason: "lane_derived" };
  }
  if (
    columnChanged &&
    input.wip &&
    input.wip.count >= input.wip.limit &&
    input.toColumn !== null
  )
    return { ok: false, reason: "wip" };
  let lane: unknown;
  if (laneChanged) {
    const type = input.laneField.field.fieldType;
    lane =
      input.toLane === null
        ? type === "person"
          ? []
          : null
        : type === "person"
          ? [input.toLane]
          : type === "checkbox"
            ? input.toLane === "true"
            : input.toLane;
  }
  return {
    ok: true,
    column: columnChanged ? input.toColumn : undefined,
    lane,
    laneChanged,
    columnChanged,
  };
}

/* ---------- legacy facet filter -> compound ---------- */

export interface LegacyFacets {
  readonly statusOptionIds: readonly string[];
  readonly priorities: readonly string[];
  readonly assigneeIds: readonly string[];
  readonly labelIds: readonly string[];
  readonly overdue: boolean;
}

/**
 * The route filter of an older bar (`p.low~o`) as a compound AND filter on the
 * board's own fields: status and person ids as they are, priorities mapped to
 * the board's priority options by name, Nhãn and Quá hạn on their pseudo fields.
 */
export function legacyFacetsToCompound(
  facets: LegacyFacets,
  fieldDefinitions: readonly FieldDefinition[],
): CompoundFilter {
  const status = findStatusField(fieldDefinitions);
  const priority = findPriorityField(fieldDefinitions);
  const person = fieldDefinitions.find((f) => f.fieldType === "person");
  const conditions: FilterCondition[] = [];
  if (status && facets.statusOptionIds.length > 0)
    conditions.push({
      fieldId: status.id,
      operator: "has_any",
      values: facets.statusOptionIds,
    });
  if (priority && facets.priorities.length > 0) {
    const ids = priority.options
      .filter((o) => {
        const mapped = priorityFromOptionName(o.label);
        return mapped !== undefined && facets.priorities.includes(mapped);
      })
      .map((o) => o.id);
    if (ids.length > 0)
      conditions.push({
        fieldId: priority.id,
        operator: "has_any",
        values: ids,
      });
  }
  if (person && facets.assigneeIds.length > 0)
    conditions.push({
      fieldId: person.id,
      operator: "has_any",
      values: facets.assigneeIds,
    });
  if (facets.labelIds.length > 0)
    conditions.push({
      fieldId: LABEL_FIELD_ID,
      operator: "has_any",
      values: facets.labelIds,
    });
  if (facets.overdue)
    conditions.push({
      fieldId: OVERDUE_FIELD_ID,
      operator: "is_true",
      values: [],
    });
  return { join: "and", conditions };
}

/* ---------- personal criteria JSON (BoardPreference) and the route form ---------- */

const encodeRef = (ref: FieldRef | undefined): string | undefined =>
  ref ? (ref.bucket ? `${ref.fieldId}|${ref.bucket}` : ref.fieldId) : undefined;

const decodeRef = (value: string | undefined): FieldRef | undefined => {
  if (!value) return undefined;
  const cut = value.lastIndexOf("|");
  if (cut > 0) {
    const bucket = value.slice(cut + 1);
    if ((DATE_BUCKETS as readonly string[]).includes(bucket))
      return { fieldId: value.slice(0, cut), bucket: bucket as DateBucket };
  }
  return { fieldId: value };
};

export function serializeSorts(sorts: readonly SortLevel[]): string {
  return sorts
    .slice(0, MAX_SORT_LEVELS)
    .map((s) => `${escapePart(s.fieldId)}.${s.direction}`)
    .join("~");
}

export function parseSorts(value: string | undefined): SortLevel[] {
  if (!value) return [];
  const levels: SortLevel[] = [];
  for (const part of value.split("~")) {
    const [rawId, direction] = part.split(".");
    const fieldId = rawId ? decodePart(rawId) : null;
    if (!fieldId || (direction !== "asc" && direction !== "desc")) continue;
    if (levels.some((l) => l.fieldId === fieldId)) continue;
    levels.push({ fieldId, direction });
  }
  return levels.slice(0, MAX_SORT_LEVELS);
}

/** A saved filter keeps the filter, grouping and swimlane; the personal state adds the sort (decision 68a: a saved filter has no sort). */
export function viewStateToCriteria(
  state: ViewState,
  options: { readonly withSort: boolean },
): SavedFilterCriteria {
  const filter = serializeCompoundFilter(state.filter);
  const sort = options.withSort ? serializeSorts(state.sorts) : "";
  return {
    ...(filter ? { filter } : {}),
    ...(sort ? { sort } : {}),
    ...(state.group ? { groupBy: encodeRef(state.group) } : {}),
    ...(state.lane ? { swimlaneBy: encodeRef(state.lane) } : {}),
  };
}

/** Reads criteria (version 1 or 2). A legacy facet `filter` string has no compound form here: the caller converts it. */
export function viewStateFromCriteria(
  criteria: SavedFilterCriteria,
): ViewState {
  const group = decodeRef(criteria.groupBy);
  const lane = decodeRef(criteria.swimlaneBy);
  return {
    filter: parseCompoundFilter(criteria.filter) ?? emptyCompoundFilter(),
    sorts: parseSorts(criteria.sort),
    ...(group ? { group } : {}),
    ...(lane ? { lane } : {}),
  };
}

export { type FieldContext };
