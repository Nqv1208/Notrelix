import {
  cellValueOf,
  compareFieldValues,
  type FieldContext,
} from "../field-types/field-type-registry";
import type { Card } from "../types";
import type { KanbanStatusLane } from "../types/kanban-types";

/**
 * Columns the Board table can sort by. The key is also the `fieldId` of the
 * `SortConfig` the view saves, so the table sort survives a reload.
 */
export const TABLE_SORT_KEYS = [
  "key",
  "title",
  "status",
  "assignee",
  "due",
  "priority",
  "label",
  "progress",
  "checklist",
  "comments",
  "files",
  "updated",
  "start",
  "created",
  "completed",
] as const;

export type TableSortKey = (typeof TABLE_SORT_KEYS)[number];

export interface TableSort {
  /** A built-in column, or the id of a custom field the table shows as a column. */
  readonly key: TableSortKey | (string & {});
  readonly direction: "asc" | "desc";
}

/** The sort of a view that has none saved: the due date, soonest first. */
export const DEFAULT_TABLE_SORT: TableSort = { key: "due", direction: "asc" };

/**
 * The table sort of a saved view. The first entry that names a table column
 * (a built-in one, or a custom field in `customFieldIds`) wins; anything else
 * (a field id of an older table, a hand-edited config) is ignored, and a view
 * without a usable entry sorts by due date.
 */
export function tableSortFromConfig(
  sortBy: readonly { fieldId: string; direction: "asc" | "desc" }[],
  customFieldIds: ReadonlySet<string> = new Set(),
): TableSort {
  for (const entry of sortBy) {
    const key =
      TABLE_SORT_KEYS.find((candidate) => candidate === entry.fieldId) ??
      (customFieldIds.has(entry.fieldId) ? entry.fieldId : undefined);
    if (key) return { key, direction: entry.direction };
  }
  return DEFAULT_TABLE_SORT;
}

const PRIORITY_RANK: Readonly<Record<NonNullable<Card["priority"]>, number>> = {
  urgent: 0,
  high: 1,
  medium: 2,
  low: 3,
};

/** Whether an item counts as finished for "hide finished items". */
export function isTableCardDone(card: Card): boolean {
  return card.statusCategory === "done" || Boolean(card.completedAt);
}

function timeOf(value: string | null | undefined): number | null {
  if (!value) return null;
  const time = Date.parse(value);
  return Number.isNaN(time) ? null : time;
}

/**
 * The comparable value of a card for a sort key: a number, a string, or
 * `null` for "no value" (always sorted last, whatever the direction).
 */
function sortValue(
  card: Card,
  key: TableSortKey | (string & {}),
  statusRank: (card: Card) => number,
): number | string | null {
  switch (key) {
    case "key": {
      const itemKey = card.itemKey?.trim();
      return itemKey ? itemKey : null;
    }
    case "title":
      return card.title.trim().toLowerCase() || null;
    case "status": {
      const rank = statusRank(card);
      return rank < 0 ? null : rank;
    }
    case "assignee":
      return card.members?.[0]?.name.trim().toLowerCase() || null;
    case "due":
      return timeOf(card.dueAt ?? card.dueDate);
    case "priority":
      return card.priority ? PRIORITY_RANK[card.priority] : null;
    case "label":
      return card.labels?.[0]?.name?.trim().toLowerCase() || null;
    case "progress": {
      const progress = card.childProgress;
      return progress && progress.total > 0
        ? progress.done / progress.total
        : null;
    }
    case "checklist": {
      const checklist = card.checklistProgress;
      return checklist && checklist.total > 0
        ? checklist.done / checklist.total
        : null;
    }
    case "comments":
      return card._count?.comments ? card._count.comments : null;
    case "files":
      return card._count?.attachments ? card._count.attachments : null;
    case "updated":
      return timeOf(card.updatedAt);
    case "start":
      return timeOf(card.startDate);
    case "created":
      return timeOf(card.createdAt);
    case "completed":
      return timeOf(card.completedAt);
    default:
      return null;
  }
}

function compareValues(left: number | string, right: number | string): number {
  if (typeof left === "number" && typeof right === "number")
    return left - right;
  return String(left).localeCompare(String(right), "vi", {
    numeric: true,
    sensitivity: "base",
  });
}

/**
 * Sorts cards by one column. Cards without a value come last in both
 * directions; equal cards keep their board order (stable). A custom field
 * (`customFields` by field id) sorts with its type's comparator and the
 * registry's null rule: empty values last ascending, first descending.
 */
export function sortTableCards(
  cards: readonly Card[],
  sort: TableSort,
  statusRank: (card: Card) => number,
  customFields?: ReadonlyMap<string, FieldContext>,
): Card[] {
  const custom = customFields?.get(sort.key);
  if (custom) {
    return cards
      .map((card, index) => ({ card, index }))
      .sort(
        (a, b) =>
          compareFieldValues(
            custom,
            cellValueOf(custom.field, a.card).value,
            cellValueOf(custom.field, b.card).value,
            sort.direction,
          ) || a.index - b.index,
      )
      .map((entry) => entry.card);
  }
  const sign = sort.direction === "desc" ? -1 : 1;
  return cards
    .map((card, index) => ({
      card,
      index,
      value: sortValue(card, sort.key, statusRank),
    }))
    .sort((a, b) => {
      if (a.value === null || b.value === null) {
        if (a.value === b.value) return a.index - b.index;
        return a.value === null ? 1 : -1;
      }
      return sign * compareValues(a.value, b.value) || a.index - b.index;
    })
    .map((entry) => entry.card);
}

/** One group of the table: a lane of the board with its sorted cards. */
export interface TableLane {
  readonly optionId: string | null;
  readonly name: string;
  readonly category: KanbanStatusLane["category"];
  readonly color: string | null;
  readonly cards: Card[];
  /** Cards of the lane before filter, search and "hide finished". */
  readonly total: number;
  /** Visible cards that are overdue (the group header says so). */
  readonly overdueCount: number;
  /** A board read in pages: items of the lane that pass the filter, loaded or not. Absent: the lane holds them all. */
  readonly filteredTotal?: number;
  /** The server holds more items of the lane that pass the filter than are loaded. */
  readonly hasMore: boolean;
}

export interface TableProjectionInput {
  /** Lanes with the route filter and search applied. */
  readonly lanes: readonly KanbanStatusLane[];
  readonly sort: TableSort;
  /** "Trạng thái: chưa xong": finished items are left out. */
  readonly hideDone: boolean;
  /** Rank of a card's status option; negative when it has none. */
  readonly statusRank: (card: Card) => number;
  readonly isOverdue: (card: Card) => boolean;
  /** Search or a route filter narrows the board. */
  readonly narrowed: boolean;
  /** The member's own multi-level sort; when given it replaces `sort` for the order inside a group. */
  readonly orderCards?: (cards: Card[]) => Card[];
  /** Custom field columns that can be sorted, by field id. */
  readonly customFields?: ReadonlyMap<string, FieldContext>;
}

export interface TableProjection {
  readonly groups: TableLane[];
  /** Cards shown. */
  readonly visibleCount: number;
  /** Cards of the board (children and archived items are never cards). */
  readonly totalCount: number;
  /** Cards hidden by search, filter and "hide finished". */
  readonly hiddenCount: number;
  /** Finished items that "hide finished" leaves out (after the other filters). */
  readonly doneHiddenCount: number;
}

/**
 * The table's groups: each lane keeps its cards in the chosen sort order.
 * A lane that has cards on the board but none to show is left out; a lane
 * that is empty on the board stays (so "Thêm vào" still works) unless a
 * search or filter is active.
 */
export function projectTableLanes({
  lanes,
  sort,
  hideDone,
  statusRank,
  isOverdue,
  narrowed,
  orderCards,
  customFields,
}: TableProjectionInput): TableProjection {
  let visibleCount = 0;
  let totalCount = 0;
  let doneHiddenCount = 0;
  const groups: TableLane[] = [];

  for (const lane of lanes) {
    totalCount += lane.total;
    const kept = hideDone
      ? lane.cards.filter((card) => !isTableCardDone(card))
      : lane.cards;
    doneHiddenCount += lane.cards.length - kept.length;
    // A paged lane shows how many items pass the filter, not how many are loaded so far.
    const laneShown =
      lane.filteredTotal === undefined
        ? kept.length
        : Math.max(
            kept.length,
            lane.filteredTotal - (lane.cards.length - kept.length),
          );
    visibleCount += laneShown;
    if (kept.length === 0 && (lane.total > 0 || narrowed)) continue;
    const cards = orderCards
      ? orderCards([...kept])
      : sortTableCards(kept, sort, statusRank, customFields);
    groups.push({
      optionId: lane.optionId,
      name: lane.name,
      category: lane.category,
      color: lane.color,
      cards,
      total: lane.total,
      overdueCount: cards.filter(isOverdue).length,
      ...(lane.filteredTotal === undefined ? {} : { filteredTotal: laneShown }),
      hasMore: lane.hasMore === true,
    });
  }

  return {
    groups,
    visibleCount,
    totalCount,
    hiddenCount: totalCount - visibleCount,
    doneHiddenCount,
  };
}
