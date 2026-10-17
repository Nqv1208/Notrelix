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

export function countMyWorkItems(groups: readonly MyWorkGroup[]): number {
  return groups.reduce((total, group) => total + group.items.length, 0);
}

/**
 * A due moment read in the member's local calendar, for the rows of Home and
 * My work. Numbers only: the surfaces turn them into words with their copy.
 */
export interface MyWorkDue {
  readonly kind: "none" | "overdue" | "today" | "upcoming";
  /** Whole local days past the due day (`overdue` only; otherwise 0). */
  readonly daysOverdue: number;
  /** `HH:mm` when the due moment has a time of day, else `null`. */
  readonly time: string | null;
  /** Local weekday, 0 = Sunday. */
  readonly weekday: number;
  /** Local `dd/MM`. */
  readonly shortDate: string;
}

const TWO_DIGITS = (value: number): string => String(value).padStart(2, "0");

/**
 * Describes `dueAt` against `now` by local calendar day. A due moment at
 * 00:00 local has no time of day (a date-only due); anything else keeps one.
 */
export function describeMyWorkDue(
  dueAt: string | null,
  now: number,
): MyWorkDue {
  const none: MyWorkDue = {
    kind: "none",
    daysOverdue: 0,
    time: null,
    weekday: 0,
    shortDate: "",
  };
  if (!dueAt) return none;
  const due = Date.parse(dueAt);
  if (Number.isNaN(due)) return none;
  const date = new Date(due);
  const time =
    date.getHours() === 0 && date.getMinutes() === 0
      ? null
      : `${TWO_DIGITS(date.getHours())}:${TWO_DIGITS(date.getMinutes())}`;
  const today = startOfLocalDay(now);
  const dueDay = startOfLocalDay(due);
  const kind =
    dueDay < today ? "overdue" : dueDay === today ? "today" : "upcoming";
  const daysOverdue =
    kind === "overdue"
      ? Math.round((today - dueDay) / (24 * 60 * 60 * 1000))
      : 0;
  return {
    kind,
    daysOverdue,
    time,
    weekday: date.getDay(),
    shortDate: `${TWO_DIGITS(date.getDate())}/${TWO_DIGITS(date.getMonth() + 1)}`,
  };
}
