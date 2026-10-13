import type {
  BoardGroup,
  Card,
  FieldDefinition,
  FieldOption,
  WorkStatusCategory,
} from "../types";
import type { KanbanStatusLane } from "../types/kanban-types";

// ─── Status and priority fields ──────────────────────────────────────────────

const PRIORITY_FIELD_NAMES: ReadonlySet<string> = new Set([
  "priority",
  "uu_tien",
  "do_uu_tien",
  "muc_uu_tien",
]);

const PRIORITY_BY_OPTION_NAME: Readonly<
  Record<string, NonNullable<Card["priority"]>>
> = {
  khan: "urgent",
  urgent: "urgent",
  cao: "high",
  high: "high",
  trung_binh: "medium",
  medium: "medium",
  thap: "low",
  low: "low",
};

/** Whether a field name (any language, any case) names a priority field. */
export function isPriorityFieldName(name: string): boolean {
  return PRIORITY_FIELD_NAMES.has(normalizeName(name));
}

/** The board's Status field: the mapped one, else a field named "Status". */
export function findStatusField(
  fields: readonly FieldDefinition[],
): FieldDefinition | undefined {
  return (
    fields.find((field) => field.semanticRole === "status") ??
    fields.find(
      (field) =>
        field.id.endsWith("field-status") ||
        normalizeName(field.name) === "status" ||
        normalizeName(field.name) === "trang_thai",
    )
  );
}

/** The board's priority field (first Select named like a priority). */
export function findPriorityField(
  fields: readonly FieldDefinition[],
): FieldDefinition | undefined {
  return (
    fields.find((field) => field.semanticRole === "priority") ??
    fields.find(
      (field) =>
        field.fieldType === "select" && isPriorityFieldName(field.name),
    )
  );
}

/**
 * Maps a priority option's name to the card's priority. Vietnamese and English
 * names, case- and diacritics-insensitive; anything else has no priority.
 */
export function priorityFromOptionName(
  name: string | null | undefined,
): Card["priority"] {
  if (!name) return undefined;
  return PRIORITY_BY_OPTION_NAME[normalizeName(name)];
}

/**
 * Groups the board's top-level cards into one lane per Status option, in the
 * options' order. Child items and archived items are never lanes' cards; a
 * lane for items without a known status is added last when there are any.
 */
export function groupCardsByStatus(board: {
  readonly fieldDefinitions: readonly FieldDefinition[];
  readonly groups: readonly BoardGroup[];
}): KanbanStatusLane[] {
  const statusField = findStatusField(board.fieldDefinitions);
  const options = statusField ? orderOptions(statusField.options) : [];
  const cardsByOption = new Map<string, Card[]>(
    options.map((option) => [option.id, []]),
  );
  const withoutStatus: Card[] = [];

  for (const group of board.groups) {
    for (const card of group.cards) {
      if (card.parentItemId || card.isArchived || card.isDeleted) continue;
      const optionId = card.statusOptionId ?? undefined;
      const lane =
        optionId === undefined ? undefined : cardsByOption.get(optionId);
      if (lane) lane.push(card);
      else withoutStatus.push(card);
    }
  }

  const lanes: KanbanStatusLane[] = options.map((option, index) => {
    const cards = cardsByOption.get(option.id) ?? [];
    return {
      optionId: option.id,
      name: option.label,
      category: option.category ?? inferStatusCategory(option.label, index),
      color: option.color || null,
      cards,
      total: cards.length,
    };
  });
  if (withoutStatus.length > 0) {
    lanes.push({
      optionId: null,
      name: "",
      category: null,
      color: null,
      cards: withoutStatus,
      total: withoutStatus.length,
    });
  }
  return lanes;
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

/**
 * Orders by a position key. Generated reads carry fractional-index strings
 * (compared by code unit, as the backend orders them); older reads carry
 * numbers. Ties keep the incoming order.
 */
export function orderByKey<T>(
  values: readonly T[],
  keyOf: (value: T) => string | number | null | undefined,
): T[] {
  return values
    .map((value, index) => ({ value, index, key: keyOf(value) }))
    .sort((a, b) => compareKeys(a.key, b.key) || a.index - b.index)
    .map((entry) => entry.value);
}

function compareKeys(
  a: string | number | null | undefined,
  b: string | number | null | undefined,
): number {
  if (a == null || b == null) return a == null ? (b == null ? 0 : 1) : -1;
  if (typeof a === "number" && typeof b === "number") return a - b;
  const left = String(a);
  const right = String(b);
  return left < right ? -1 : left > right ? 1 : 0;
}

function orderOptions(options: readonly FieldOption[]): FieldOption[] {
  return orderByKey(options, (option) => option.position);
}

export function normalizeName(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

/** Category of a status option without one (older reads), from its name. */
export function inferStatusCategory(
  label: string,
  index: number,
): WorkStatusCategory {
  const name = normalizeName(label);
  if (/(^|_)(done|completed?|hoan_thanh|xong)($|_)/.test(name)) return "done";
  if (/(^|_)(review|cho_duyet|dang_duyet)($|_)/.test(name)) return "review";
  if (index === 0 || /(not_started|to_do|todo|can_lam)/.test(name))
    return "todo";
  return "in_progress";
}
