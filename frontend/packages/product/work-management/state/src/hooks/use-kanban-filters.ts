import { useMemo } from "react";
import {
  findStatusField,
  type Board,
  type BoardGroup,
  type Card,
  type KanbanStatusLane,
} from "@notrelix/work-management-core";

/**
 * The Kanban view filter: values within a facet match any (OR), facets
 * combine (AND). It mirrors the web `KanbanFilterState` carried by the route
 * `filter` parameter.
 */
export interface KanbanLaneFilter {
  readonly statusOptionIds: readonly string[];
  readonly priorities: readonly string[];
  readonly assigneeIds: readonly string[];
  readonly labelIds: readonly string[];
  readonly overdue: boolean;
}

export interface KanbanLaneFilterOptions {
  /** Free-text search; matched without case or diacritics. */
  readonly query: string;
  /**
   * Whether a card counts as overdue. Injected so the filter judges calendar
   * days exactly as the card shows its due wording.
   */
  readonly isOverdue: (card: Card) => boolean;
}

export interface FilteredKanbanLanes {
  /** The lanes with only the matching cards; `total` stays the lane total. */
  readonly lanes: KanbanStatusLane[];
  /** Matching cards across the lanes. */
  readonly visibleCount: number;
  /** Every card of the lanes before the filter. */
  readonly totalCount: number;
  /** True when a facet or the search narrows the board. */
  readonly active: boolean;
}

function normalise(text: string): string {
  return text
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[đĐ]/g, "d")
    .toLowerCase()
    .replace(/\s+/g, " ")
    .trim();
}

function searchText(card: Card): string {
  return normalise(
    [
      card.title,
      card.itemKey ?? "",
      card.descriptionMd ?? "",
      ...card.members.map((member) => member.name),
      ...card.labels.map((label) => label.name),
    ].join(" "),
  );
}

function matches(
  card: Card,
  filter: KanbanLaneFilter,
  needle: string,
  isOverdue: (card: Card) => boolean,
): boolean {
  if (
    filter.statusOptionIds.length > 0 &&
    !filter.statusOptionIds.includes(card.statusOptionId ?? "")
  ) {
    return false;
  }
  if (
    filter.priorities.length > 0 &&
    !(card.priority && filter.priorities.includes(card.priority))
  ) {
    return false;
  }
  if (
    filter.assigneeIds.length > 0 &&
    !card.members.some((member) => filter.assigneeIds.includes(member.userId))
  ) {
    return false;
  }
  if (
    filter.labelIds.length > 0 &&
    !card.labels.some((label) => filter.labelIds.includes(label.id))
  ) {
    return false;
  }
  if (filter.overdue && !isOverdue(card)) return false;
  if (needle && !searchText(card).includes(needle)) return false;
  return true;
}

function isFilterActive(filter: KanbanLaneFilter): boolean {
  return (
    filter.statusOptionIds.length > 0 ||
    filter.priorities.length > 0 ||
    filter.assigneeIds.length > 0 ||
    filter.labelIds.length > 0 ||
    filter.overdue
  );
}

/** Applies the view filter and the search to the status lanes. */
export function filterKanbanLanes(
  lanes: readonly KanbanStatusLane[],
  filter: KanbanLaneFilter,
  { query, isOverdue }: KanbanLaneFilterOptions,
): FilteredKanbanLanes {
  const needle = normalise(query);
  const active = isFilterActive(filter) || needle.length > 0;
  const totalCount = lanes.reduce((sum, lane) => sum + lane.cards.length, 0);
  if (!active) {
    return { lanes: [...lanes], visibleCount: totalCount, totalCount, active };
  }
  const filtered = lanes.map((lane) => ({
    ...lane,
    cards: lane.cards.filter((card) =>
      matches(card, filter, needle, isOverdue),
    ),
  }));
  const visibleCount = filtered.reduce(
    (sum, lane) => sum + lane.cards.length,
    0,
  );
  return { lanes: filtered, visibleCount, totalCount, active };
}

/** Memoised {@link filterKanbanLanes} for a view. */
export function useKanbanFilters(
  lanes: readonly KanbanStatusLane[],
  filter: KanbanLaneFilter,
  options: KanbanLaneFilterOptions,
): FilteredKanbanLanes {
  const { query, isOverdue } = options;
  return useMemo(
    () => filterKanbanLanes(lanes, filter, { query, isOverdue }),
    [lanes, filter, query, isOverdue],
  );
}

/** A value the filter menu offers. */
export interface KanbanFilterChoice {
  readonly id: string;
  readonly name: string;
}

/** What the filter menu offers for a board. */
export interface KanbanFilterChoices {
  readonly statuses: readonly KanbanFilterChoice[];
  readonly assignees: readonly KanbanFilterChoice[];
  readonly labels: readonly KanbanFilterChoice[];
}

/**
 * The Status options, the people (board members, then other assignees) and
 * the labels used on the board's cards, each once, in board order.
 */
export function kanbanFilterChoices(
  board: Pick<Board, "fieldDefinitions" | "members">,
  groups: readonly BoardGroup[],
): KanbanFilterChoices {
  const statusField = findStatusField(board.fieldDefinitions);
  const people = new Map<string, string>();
  for (const member of board.members) people.set(member.userId, member.name);
  const labels = new Map<string, string>();
  for (const group of groups) {
    for (const card of group.cards) {
      for (const member of card.members) {
        if (!people.has(member.userId)) people.set(member.userId, member.name);
      }
      for (const label of card.labels) {
        if (!labels.has(label.id)) labels.set(label.id, label.name);
      }
    }
  }
  const entries = (map: Map<string, string>) =>
    Array.from(map, ([id, name]) => ({ id, name }));
  return {
    statuses: (statusField?.options ?? []).map((option) => ({
      id: option.id,
      name: option.label,
    })),
    assignees: entries(people),
    labels: entries(labels),
  };
}

/**
 * The filter without ids the board no longer has (a stale or hand-edited
 * URL). Returns the same object when nothing was dropped.
 */
export function dropUnknownFilterIds<T extends KanbanLaneFilter>(
  filter: T,
  choices: KanbanFilterChoices,
): T {
  const known = (options: readonly KanbanFilterChoice[]) =>
    new Set(options.map((option) => option.id));
  const statuses = known(choices.statuses);
  const people = known(choices.assignees);
  const labels = known(choices.labels);
  const statusOptionIds = filter.statusOptionIds.filter((id) =>
    statuses.has(id),
  );
  const assigneeIds = filter.assigneeIds.filter((id) => people.has(id));
  const labelIds = filter.labelIds.filter((id) => labels.has(id));
  if (
    statusOptionIds.length === filter.statusOptionIds.length &&
    assigneeIds.length === filter.assigneeIds.length &&
    labelIds.length === filter.labelIds.length
  ) {
    return filter;
  }
  return { ...filter, statusOptionIds, assigneeIds, labelIds };
}
