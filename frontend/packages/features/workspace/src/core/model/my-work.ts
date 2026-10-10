/**
 * My work grouping (WX-HOME-001, read from WX-BE-005).
 *
 * The producer returns open Items assigned to the member ordered by due date
 * (earliest first, undated last). The client groups them into due windows in
 * the member's local calendar: overdue, today, this week (until the end of
 * the 7th day from today), later, and no due date.
 */

/** One open Item assigned to the member, as the My Work read returns it. */
export interface MyWorkItem {
  readonly id: string;
  readonly boardId: string;
  readonly groupId: string;
  readonly name: string;
  readonly dueAt: string | null;
  readonly startedAt: string | null;
  readonly updatedAt: string | null;
  readonly version: number;
}

export type MyWorkWindow =
  "overdue" | "today" | "this-week" | "later" | "no-date";

export const MY_WORK_WINDOWS: readonly MyWorkWindow[] = [
  "overdue",
  "today",
  "this-week",
  "later",
  "no-date",
];

export interface MyWorkGroup {
  readonly range: MyWorkWindow;
  readonly items: readonly MyWorkItem[];
}

function startOfLocalDay(time: number): number {
  const date = new Date(time);
  date.setHours(0, 0, 0, 0);
  return date.getTime();
}

function addLocalDays(dayStart: number, days: number): number {
  const date = new Date(dayStart);
  date.setDate(date.getDate() + days);
  return date.getTime();
}

/** The due window of one Item at `now` (epoch ms). */
export function myWorkWindow(dueAt: string | null, now: number): MyWorkWindow {
  if (!dueAt) return "no-date";
  const due = Date.parse(dueAt);
  if (Number.isNaN(due)) return "no-date";
  const today = startOfLocalDay(now);
  // Windows are calendar days: an Item due earlier today is "today", not
  // overdue; only Items due on a past day are overdue.
  if (due < today) return "overdue";
  if (due < addLocalDays(today, 1)) return "today";
  if (due < addLocalDays(today, 7)) return "this-week";
  return "later";
}

/**
 * Groups Items into non-empty due windows, in window order. Items keep the
 * producer's order inside a window; duplicates (same id) are dropped.
 */
export function groupMyWorkItems(
  items: readonly MyWorkItem[],
  now: number,
): MyWorkGroup[] {
  const buckets = new Map<MyWorkWindow, MyWorkItem[]>();
  const seen = new Set<string>();
  for (const item of items) {
    if (seen.has(item.id)) continue;
    seen.add(item.id);
    const range = myWorkWindow(item.dueAt, now);
    const bucket = buckets.get(range);
    if (bucket) bucket.push(item);
    else buckets.set(range, [item]);
  }
  return MY_WORK_WINDOWS.flatMap((range) => {
    const bucket = buckets.get(range);
    return bucket ? [{ range, items: bucket }] : [];
  });
}

/** The first `limit` Items across groups, keeping group order (Home preview). */
export function limitMyWorkGroups(
  groups: readonly MyWorkGroup[],
  limit: number,
): MyWorkGroup[] {
  const out: MyWorkGroup[] = [];
  let remaining = limit;
  for (const group of groups) {
    if (remaining <= 0) break;
    const items = group.items.slice(0, remaining);
    remaining -= items.length;
    out.push({ range: group.range, items });
  }
  return out;
}

export function countMyWorkItems(groups: readonly MyWorkGroup[]): number {
  return groups.reduce((total, group) => total + group.items.length, 0);
}
