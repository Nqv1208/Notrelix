import type { Card } from "@notrelix/work-management-core";
import { itemDueState } from "../../shared/item-display";

/**
 * Kanban filter state carried by the board route `filter` search parameter.
 *
 * The toolbar surface emits a whole `KanbanFilterState`; the route stores it
 * as the compact string from `serializeKanbanFilter`, e.g. `p.low~o` for
 * "Ưu tiên Thấp" and "Quá hạn". Facets are joined by `~`, a facet key and its
 * values by `.`; ids are percent-encoded so they can contain either
 * separator. The string uses only characters a URL query keeps as they are.
 * `parseKanbanFilter` reads anything (a stale or hand-edited URL) and keeps
 * only the parts it understands.
 */

export const KANBAN_FILTER_PRIORITIES = [
  "urgent",
  "high",
  "medium",
  "low",
] as const;

export type KanbanFilterPriority = (typeof KANBAN_FILTER_PRIORITIES)[number];

export type KanbanFilterState = {
  statusOptionIds: string[];
  priorities: Array<KanbanFilterPriority>;
  assigneeIds: string[];
  labelIds: string[];
  overdue: boolean;
};

/** A fresh empty filter, safe to mutate (no shared empty constant). */
export function emptyKanbanFilter(): KanbanFilterState {
  return {
    statusOptionIds: [],
    priorities: [],
    assigneeIds: [],
    labelIds: [],
    overdue: false,
  };
}

export function countKanbanFilter(filter: KanbanFilterState): number {
  return (
    filter.statusOptionIds.length +
    filter.priorities.length +
    filter.assigneeIds.length +
    filter.labelIds.length +
    (filter.overdue ? 1 : 0)
  );
}

export function isKanbanFilterActive(filter: KanbanFilterState): boolean {
  return countKanbanFilter(filter) > 0;
}

const FACET_SEPARATOR = "~";
const VALUE_SEPARATOR = ".";
const OVERDUE_KEY = "o";
const ID_FACETS = [
  ["s", "statusOptionIds"],
  ["a", "assigneeIds"],
  ["l", "labelIds"],
] as const;

function escapeId(id: string): string {
  return encodeURIComponent(id).replace(
    /[.~!'()*]/g,
    (char) => `%${char.charCodeAt(0).toString(16).toUpperCase()}`,
  );
}

function unescapeId(value: string): string | null {
  if (value === "") return null;
  try {
    return decodeURIComponent(value);
  } catch {
    return null;
  }
}

function unique<T>(values: readonly T[]): T[] {
  return Array.from(new Set(values));
}

function canonicalPriorities(
  priorities: readonly string[],
): KanbanFilterPriority[] {
  return KANBAN_FILTER_PRIORITIES.filter((priority) =>
    priorities.includes(priority),
  );
}

export function serializeKanbanFilter(filter: KanbanFilterState): string {
  const idFacet = (key: string, ids: readonly string[]) =>
    [key, ...unique(ids).map(escapeId)].join(VALUE_SEPARATOR);
  const facets: string[] = [];
  if (filter.statusOptionIds.length > 0) {
    facets.push(idFacet("s", filter.statusOptionIds));
  }
  const priorities = canonicalPriorities(filter.priorities);
  if (priorities.length > 0) {
    facets.push(["p", ...priorities].join(VALUE_SEPARATOR));
  }
  if (filter.assigneeIds.length > 0) {
    facets.push(idFacet("a", filter.assigneeIds));
  }
  if (filter.labelIds.length > 0) {
    facets.push(idFacet("l", filter.labelIds));
  }
  if (filter.overdue) facets.push(OVERDUE_KEY);
  return facets.join(FACET_SEPARATOR);
}

export function parseKanbanFilter(value: unknown): KanbanFilterState {
  const filter = emptyKanbanFilter();
  if (typeof value !== "string" || value === "") return filter;

  for (const facet of value.split(FACET_SEPARATOR)) {
    const [key, ...rawValues] = facet.split(VALUE_SEPARATOR);
    if (key === OVERDUE_KEY && rawValues.length === 0) {
      filter.overdue = true;
      continue;
    }
    if (key === "p") {
      filter.priorities = canonicalPriorities([
        ...filter.priorities,
        ...rawValues,
      ]);
      continue;
    }
    const idFacet = ID_FACETS.find(([facetKey]) => facetKey === key);
    if (!idFacet) continue;
    const field = idFacet[1];
    const ids = rawValues
      .map(unescapeId)
      .filter((id): id is string => id !== null);
    filter[field] = unique([...filter[field], ...ids]);
  }
  return filter;
}

/**
 * The "Quá hạn" facet: a card that is not done and whose due day is before
 * today, judged by calendar day exactly like the card's due wording.
 */
export function isKanbanCardOverdue(
  card: Card,
  now: Date,
  timeZone?: string,
): boolean {
  if (card.statusCategory === "done" || card.completedAt) return false;
  return (
    itemDueState(card.dueAt ?? card.dueDate, now, timeZone)?.kind === "overdue"
  );
}
