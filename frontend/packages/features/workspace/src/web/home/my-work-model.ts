import {
  describeMyWorkDue,
  groupMyWorkItems,
  type MyWorkDue,
  type MyWorkWindow,
} from "../../core/model/my-work";
import { normalizeSearchText } from "../../core/model/navigation";
import type {
  MyWorkDepartment,
  MyWorkItemView,
  WorkPriority,
  WorkStatusBucket,
} from "./home-models";

/**
 * The pure model of the My work page: the tabs and their counts, the filters
 * and the search, the two groupings, and what the right rail adds up. No
 * framework, no clock: the caller passes `now`.
 */

export type MyWorkTab = "all" | "mine" | "approve" | "follow" | "done";

export type MyWorkGroupBy = "due" | "board" | "status";

export const MY_WORK_TABS: readonly MyWorkTab[] = [
  "all",
  "mine",
  "approve",
  "follow",
  "done",
];

export interface MyWorkFilters {
  /** Department names to keep; empty keeps every department. */
  readonly departments: readonly string[];
  /** Board ids to keep; empty keeps every Board. */
  readonly boards: readonly string[];
  /** Priorities to keep; empty keeps every Priority (and items without one). */
  readonly priorities: readonly WorkPriority[];
  readonly query: string;
  /** Local `dd/MM` of a day in the week chart; `null` keeps every day. */
  readonly dueOn: string | null;
}

export const NO_MY_WORK_FILTERS: MyWorkFilters = {
  departments: [],
  boards: [],
  priorities: [],
  query: "",
  dueOn: null,
};

/** Whether the item belongs to the tab by its relationship to the member. */
export function inTab(item: MyWorkItemView, tab: MyWorkTab): boolean {
  if (tab === "all") return true;
  if (tab === "done") return false;
  return item.reason === tab;
}

export function tabCounts(
  items: readonly MyWorkItemView[],
): Record<Exclude<MyWorkTab, "done">, number> {
  const counts = { all: items.length, mine: 0, approve: 0, follow: 0 };
  for (const item of items) {
    if (item.reason === "mine") counts.mine += 1;
    else if (item.reason === "approve") counts.approve += 1;
    else if (item.reason === "follow") counts.follow += 1;
  }
  return counts;
}

/** Case- and diacritic-insensitive match on the title, Board and key. */
export function matchesQuery(item: MyWorkItemView, query: string): boolean {
  const needle = normalizeSearchText(query.trim());
  if (!needle) return true;
  const haystack = normalizeSearchText(
    [item.name, item.boardName ?? "", item.key ?? ""].join(" "),
  );
  return haystack.includes(needle);
}

export function applyFilters(
  items: readonly MyWorkItemView[],
  tab: MyWorkTab,
  filters: MyWorkFilters,
  now: number,
): MyWorkItemView[] {
  return items.filter(
    (item) =>
      inTab(item, tab) &&
      (filters.dueOn === null ||
        describeMyWorkDue(item.dueAt, now).shortDate === filters.dueOn) &&
      (filters.departments.length === 0 ||
        (item.department !== null &&
          item.department !== undefined &&
          filters.departments.includes(item.department.name))) &&
      (filters.boards.length === 0 || filters.boards.includes(item.boardId)) &&
      (filters.priorities.length === 0 ||
        (item.priority !== null &&
          item.priority !== undefined &&
          filters.priorities.includes(item.priority))) &&
      matchesQuery(item, filters.query),
  );
}

export interface MyWorkDueGroup {
  readonly id: MyWorkWindow;
  readonly items: readonly MyWorkItemView[];
}

/** The items in due windows, in window order (empty windows are left out). */
export function groupByDue(
  items: readonly MyWorkItemView[],
  now: number,
): MyWorkDueGroup[] {
  return groupMyWorkItems(items, now).map((group) => ({
    id: group.range,
    items: group.items as readonly MyWorkItemView[],
  }));
}

export interface MyWorkStatusGroup {
  readonly id: WorkStatusBucket;
  readonly label: string;
  readonly items: readonly MyWorkItemView[];
}

const STATUS_ORDER: readonly WorkStatusBucket[] = [
  "todo",
  "doing",
  "review",
  "done",
];

/**
 * The items by Status bucket in the design's order (to do, doing, in review,
 * done), labelled with the Status the items carry. Items without a Status are
 * left out; the grouping is offered only when some item has one.
 */
export function groupByStatus(
  items: readonly MyWorkItemView[],
): MyWorkStatusGroup[] {
  const buckets = new Map<WorkStatusBucket, MyWorkItemView[]>();
  for (const item of items) {
    if (!item.status) continue;
    const list = buckets.get(item.status.bucket);
    if (list) list.push(item);
    else buckets.set(item.status.bucket, [item]);
  }
  return STATUS_ORDER.flatMap((id) => {
    const list = buckets.get(id);
    return list && list[0]?.status
      ? [{ id, label: list[0].status.label, items: list }]
      : [];
  });
}

export interface MyWorkBoardGroup {
  readonly id: string;
  readonly boardId: string;
  readonly name: string | null;
  readonly department: MyWorkDepartment | null;
  readonly items: readonly MyWorkItemView[];
}

/** The items by Board, in the order the Boards first appear. */
export function groupByBoard(
  items: readonly MyWorkItemView[],
): MyWorkBoardGroup[] {
  const groups = new Map<string, MyWorkItemView[]>();
  for (const item of items) {
    const list = groups.get(item.boardId);
    if (list) list.push(item);
    else groups.set(item.boardId, [item]);
  }
  return [...groups.entries()].map(([boardId, list]) => ({
    id: `b-${boardId}`,
    boardId,
    name: list[0]?.boardName ?? null,
    department: list[0]?.department ?? null,
    items: list,
  }));
}

export interface MyWorkWeekDay {
  /** 0 = Sunday. */
  readonly weekday: number;
  readonly dayOfMonth: number;
  readonly shortDate: string;
  readonly count: number;
  readonly today: boolean;
}

function startOfLocalDay(time: number): number {
  const date = new Date(time);
  date.setHours(0, 0, 0, 0);
  return date.getTime();
}

/**
 * The seven local days from today (the "this week" window of the grouping)
 * with the number of items due on each.
 */
export function weekDays(
  items: readonly MyWorkItemView[],
  now: number,
): MyWorkWeekDay[] {
  const today = startOfLocalDay(now);
  const counts = new Map<number, number>();
  for (const item of items) {
    if (!item.dueAt) continue;
    const due = Date.parse(item.dueAt);
    if (Number.isNaN(due)) continue;
    const key = startOfLocalDay(due);
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  return Array.from({ length: 7 }, (_, index) => {
    const date = new Date(today);
    date.setDate(date.getDate() + index);
    return {
      weekday: date.getDay(),
      dayOfMonth: date.getDate(),
      shortDate: `${String(date.getDate()).padStart(2, "0")}/${String(
        date.getMonth() + 1,
      ).padStart(2, "0")}`,
      count: counts.get(date.getTime()) ?? 0,
      today: index === 0,
    };
  });
}

/** `12–18/10` for the window, or `28/10–03/11` across a month. */
export function weekRange(days: readonly MyWorkWeekDay[]): string {
  const first = days[0];
  const last = days[days.length - 1];
  if (!first || !last) return "";
  const [lastDay, lastMonth] = last.shortDate.split("/");
  const [firstDay, firstMonth] = first.shortDate.split("/");
  return firstMonth === lastMonth
    ? `${firstDay}–${lastDay}/${lastMonth}`
    : `${first.shortDate}–${last.shortDate}`;
}

export interface MyWorkDepartmentCount {
  readonly department: MyWorkDepartment;
  readonly count: number;
}

/** Open items per department, the most first. Items without one are left out. */
export function departmentCounts(
  items: readonly MyWorkItemView[],
): MyWorkDepartmentCount[] {
  const counts = new Map<string, MyWorkDepartmentCount>();
  for (const item of items) {
    const department = item.department;
    if (!department) continue;
    const entry = counts.get(department.name);
    counts.set(department.name, {
      department,
      count: (entry?.count ?? 0) + 1,
    });
  }
  return [...counts.values()].sort((a, b) => b.count - a.count);
}

export interface MyWorkOverdueSummary {
  readonly count: number;
  /** The earliest overdue due day; `null` without an overdue item. */
  readonly since: MyWorkDue | null;
}

export function overdueSummary(
  items: readonly MyWorkItemView[],
  now: number,
): MyWorkOverdueSummary {
  let count = 0;
  let earliest: { at: number; item: MyWorkItemView } | null = null;
  for (const item of items) {
    const due = describeMyWorkDue(item.dueAt, now);
    if (due.kind !== "overdue" || !item.dueAt) continue;
    count += 1;
    const at = Date.parse(item.dueAt);
    if (!earliest || at < earliest.at) earliest = { at, item };
  }
  return {
    count,
    since: earliest ? describeMyWorkDue(earliest.item.dueAt, now) : null,
  };
}

/** The last day of the "this week" window (today plus six local days). */
export function weekWindowEnd(now: number): {
  weekday: number;
  shortDate: string;
} {
  const date = new Date(startOfLocalDay(now));
  date.setDate(date.getDate() + 6);
  return {
    weekday: date.getDay(),
    shortDate: `${String(date.getDate()).padStart(2, "0")}/${String(
      date.getMonth() + 1,
    ).padStart(2, "0")}`,
  };
}

export type MyWorkDoneWindow = "today" | "week" | "last-week" | "earlier";

export interface MyWorkDoneGroup {
  readonly id: MyWorkDoneWindow;
  readonly items: readonly MyWorkItemView[];
}

/** Monday of the local calendar week of `time`. */
function startOfLocalWeek(time: number): number {
  const date = new Date(startOfLocalDay(time));
  date.setDate(date.getDate() - ((date.getDay() + 6) % 7));
  return date.getTime();
}

/**
 * Completed items by when they were completed: today, earlier this calendar
 * week, last week, before that. Newest first inside a window.
 */
export function groupByDone(
  items: readonly MyWorkItemView[],
  now: number,
): MyWorkDoneGroup[] {
  const today = startOfLocalDay(now);
  const thisWeek = startOfLocalWeek(now);
  const lastWeek = new Date(thisWeek);
  lastWeek.setDate(lastWeek.getDate() - 7);
  const buckets: Record<MyWorkDoneWindow, MyWorkItemView[]> = {
    today: [],
    week: [],
    "last-week": [],
    earlier: [],
  };
  const stamped = [...items].sort(
    (a, b) => Date.parse(b.completedAt ?? "") - Date.parse(a.completedAt ?? ""),
  );
  for (const item of stamped) {
    const at = Date.parse(item.completedAt ?? "");
    const day = Number.isNaN(at) ? 0 : startOfLocalDay(at);
    const bucket: MyWorkDoneWindow =
      day >= today
        ? "today"
        : day >= thisWeek
          ? "week"
          : day >= lastWeek.getTime()
            ? "last-week"
            : "earlier";
    buckets[bucket].push(item);
  }
  return (["today", "week", "last-week", "earlier"] as const)
    .filter((id) => buckets[id].length > 0)
    .map((id) => ({ id, items: buckets[id] }));
}
