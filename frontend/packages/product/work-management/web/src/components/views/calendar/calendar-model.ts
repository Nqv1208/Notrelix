/**
 * Calendar view model: calendar days, the week grid of a month, the layout of
 * entries in a week and the navigation steps. Pure functions; the clock is
 * always injected. A day is a `DayKey` ("2026-10-12"), a wall-clock day of
 * the view's time zone, so no instant arithmetic crosses a day boundary here.
 */

/** A calendar day as "yyyy-MM-dd". */
export type DayKey = string;

export type CalendarRange = "month" | "week" | "day";

/** How an entry is placed: over its whole start-to-due span, or on its due day. */
export type CalendarDateBasis = "span" | "due";

/** The colour family of an entry: its Status bucket, or overdue. */
export type CalendarTone = "todo" | "doing" | "review" | "done" | "overdue";

/**
 * `item` is a board item. `phase` and `milestone` are the design's range and
 * point markers; the board read carries no such kind yet, so only a producer
 * that has one (stories, a later contract) emits them.
 */
export type CalendarEntryKind = "item" | "phase" | "milestone";

export interface CalendarPerson {
  readonly initials: string;
  /** Literal tone classes of the avatar mark. */
  readonly markClass: string;
  readonly name?: string;
}

export interface CalendarEntry {
  readonly id: string;
  readonly kind: CalendarEntryKind;
  /** Item key such as "ERP-144"; absent for phases and milestones. */
  readonly itemKey?: string | null;
  readonly title: string;
  readonly tone: CalendarTone;
  /** First and last day, inclusive; equal for a single-day entry. */
  readonly startDay: DayKey;
  readonly endDay: DayKey;
  /** The due's wall-clock minutes of the day (17:00 is 1020); `null` = no time. */
  readonly dueMinutes?: number | null;
  readonly assignee?: CalendarPerson | null;
  readonly priority?: "urgent" | "high" | "medium" | "low" | null;
  /** Live, not-completed items that block this one. */
  readonly openBlockerCount?: number;
  /** Text after the title in the accessible name ("hạn 16/10"). */
  readonly note: string;
  /** Only an item with a due day moves to another day. */
  readonly movable: boolean;
}

/** An item with no date at all, listed beside the calendar. */
export interface CalendarUndated {
  readonly id: string;
  readonly itemKey?: string | null;
  readonly title: string;
  /** The assignee's name, or `null` for an unassigned item. */
  readonly assigneeName?: string | null;
}

const DAY_MS = 86_400_000;

function pad2(value: number): string {
  return String(value).padStart(2, "0");
}

export function toDayKey(year: number, month: number, day: number): DayKey {
  return `${String(year).padStart(4, "0")}-${pad2(month)}-${pad2(day)}`;
}

export function parseDayKey(key: DayKey): {
  year: number;
  month: number;
  day: number;
} {
  const [year = 1970, month = 1, day = 1] = key.split("-").map(Number);
  return { year, month, day };
}

export function dayNumber(key: DayKey): number {
  const { year, month, day } = parseDayKey(key);
  return Math.round(Date.UTC(year, month - 1, day) / DAY_MS);
}

export function fromDayNumber(value: number): DayKey {
  const date = new Date(value * DAY_MS);
  return toDayKey(
    date.getUTCFullYear(),
    date.getUTCMonth() + 1,
    date.getUTCDate(),
  );
}

export function addDays(key: DayKey, days: number): DayKey {
  return fromDayNumber(dayNumber(key) + days);
}

/** Whole days from `from` to `to`; negative when `to` is earlier. */
export function diffDays(from: DayKey, to: DayKey): number {
  return dayNumber(to) - dayNumber(from);
}

/** Monday is 0, Sunday is 6 (the design's week starts on Monday). */
export function weekdayIndex(key: DayKey): number {
  return (new Date(dayNumber(key) * DAY_MS).getUTCDay() + 6) % 7;
}

export function startOfWeek(key: DayKey): DayKey {
  return addDays(key, -weekdayIndex(key));
}

export function isWeekend(key: DayKey): boolean {
  return weekdayIndex(key) >= 5;
}

export function sameMonth(a: DayKey, b: DayKey): boolean {
  return a.slice(0, 7) === b.slice(0, 7);
}

/** `key` moved by whole months; the day is kept or clamped to the month's end. */
export function addMonths(key: DayKey, months: number): DayKey {
  const { year, month, day } = parseDayKey(key);
  const target = year * 12 + (month - 1) + months;
  const targetYear = Math.floor(target / 12);
  const targetMonth = (target % 12) + 1;
  const last = new Date(Date.UTC(targetYear, targetMonth, 0)).getUTCDate();
  return toDayKey(targetYear, targetMonth, Math.min(day, last));
}

/** The weeks of the month of `anchor`, Monday first, padded to whole weeks. */
export function monthWeeks(anchor: DayKey): DayKey[][] {
  const { year, month } = parseDayKey(anchor);
  const first = toDayKey(year, month, 1);
  const last = toDayKey(
    year,
    month,
    new Date(Date.UTC(year, month, 0)).getUTCDate(),
  );
  const weeks: DayKey[][] = [];
  for (
    let start = startOfWeek(first);
    diffDays(start, last) >= 0;
    start = addDays(start, 7)
  ) {
    weeks.push(Array.from({ length: 7 }, (_, index) => addDays(start, index)));
  }
  return weeks;
}

export function weekDays(anchor: DayKey): DayKey[] {
  const start = startOfWeek(anchor);
  return Array.from({ length: 7 }, (_, index) => addDays(start, index));
}

/** The previous or next month, week or day (`direction` -1 or 1). */
export function stepAnchor(
  range: CalendarRange,
  anchor: DayKey,
  direction: -1 | 1,
): DayKey {
  if (range === "month") return addMonths(anchor, direction);
  return addDays(anchor, direction * (range === "week" ? 7 : 1));
}

/** "dd/MM" of a day. */
export function formatDayMonth(key: DayKey): string {
  const { month, day } = parseDayKey(key);
  return `${pad2(day)}/${pad2(month)}`;
}

/** "dd/MM/yyyy" of a day. */
export function formatFullDay(key: DayKey): string {
  const { year } = parseDayKey(key);
  return `${formatDayMonth(key)}/${year}`;
}

/** "17:00" of minutes of the day. */
export function formatMinutes(minutes: number): string {
  return `${pad2(Math.floor(minutes / 60))}:${pad2(minutes % 60)}`;
}

// ─── Week layout ────────────────────────────────────────────────────────────

export interface CalendarPlacement {
  readonly entry: CalendarEntry;
  /** Grid column 1–7 and the number of columns, clipped to the week. */
  readonly col: number;
  readonly span: number;
  /** 0-based event row under the day numbers. */
  readonly row: number;
}

export interface CalendarMore {
  readonly col: number;
  readonly count: number;
  readonly day: DayKey;
}

export interface CalendarWeekLayout {
  readonly days: readonly DayKey[];
  readonly placements: readonly CalendarPlacement[];
  readonly more: readonly CalendarMore[];
}

/** Whether the entry overlaps the days `from`..`to`. */
function overlaps(entry: CalendarEntry, from: DayKey, to: DayKey): boolean {
  return entry.startDay <= to && entry.endDay >= from;
}

/**
 * Places the entries that touch the week `days` on the grid: start column
 * first, longer spans first, then input order (so equal entries keep the
 * order of the board). Each placement takes the first row whose columns are
 * free, like a dense grid. With `maxRows` set, rows that do not fit collapse
 * into one "+N" per column: the last visible row of such a column is given
 * up for the counter, so the number always names the hidden entries.
 */
export function layoutWeek(
  days: readonly DayKey[],
  entries: readonly CalendarEntry[],
  maxRows: number | null,
): CalendarWeekLayout {
  const first = days[0];
  const last = days[days.length - 1];
  if (!first || !last) return { days, placements: [], more: [] };

  const touching = entries
    .map((entry, order) => ({ entry, order }))
    .filter(({ entry }) => overlaps(entry, first, last))
    .map(({ entry, order }) => {
      const from = entry.startDay < first ? first : entry.startDay;
      const to = entry.endDay > last ? last : entry.endDay;
      return {
        entry,
        order,
        col: diffDays(first, from) + 1,
        span: diffDays(from, to) + 1,
      };
    })
    .sort((a, b) => a.col - b.col || b.span - a.span || a.order - b.order);

  const taken: boolean[][] = [];
  const placed: CalendarPlacement[] = touching.map(({ entry, col, span }) => {
    let row = 0;
    for (; ; row += 1) {
      const used = taken[row] ?? (taken[row] = []);
      let free = true;
      for (let c = col; c < col + span; c += 1) {
        if (used[c]) {
          free = false;
          break;
        }
      }
      if (free) {
        for (let c = col; c < col + span; c += 1) used[c] = true;
        break;
      }
    }
    return { entry, col, span, row };
  });

  if (maxRows === null || taken.length <= maxRows) {
    return { days, placements: placed, more: [] };
  }

  const overflowColumns = new Set<number>();
  for (const placement of placed) {
    if (placement.row < maxRows) continue;
    for (let c = placement.col; c < placement.col + placement.span; c += 1) {
      overflowColumns.add(c);
    }
  }
  const hiddenPerColumn = new Map<number, number>();
  const visible = placed.filter((placement) => {
    const touchesOverflow = Array.from(
      { length: placement.span },
      (_, index) => placement.col + index,
    ).some((c) => overflowColumns.has(c));
    const hidden =
      placement.row >= maxRows ||
      (placement.row === maxRows - 1 && touchesOverflow);
    if (hidden) {
      for (let c = placement.col; c < placement.col + placement.span; c += 1) {
        if (overflowColumns.has(c)) {
          hiddenPerColumn.set(c, (hiddenPerColumn.get(c) ?? 0) + 1);
        }
      }
    }
    return !hidden;
  });
  const more = [...overflowColumns]
    .sort((a, b) => a - b)
    .map((col) => ({
      col,
      count: hiddenPerColumn.get(col) ?? 0,
      day: days[col - 1] ?? first,
    }));
  return { days, placements: visible, more };
}

/** The most event rows that fit a week of `weekHeight` px (month grid). */
export function weekRowCapacity(weekHeight: number): number {
  // 4 px top padding + 26 px day number row + 6 px bottom padding; each event
  // row is 24 px plus the 3 px row gap.
  return Math.max(1, Math.floor((weekHeight - 36) / 27));
}

// ─── Day agenda ─────────────────────────────────────────────────────────────

export interface CalendarAgenda {
  /** Overdue entries, shown while the day is today. */
  readonly overdue: readonly CalendarEntry[];
  /** Entries that cover the day without a time. */
  readonly allDay: readonly CalendarEntry[];
  /** Entries with a due time, by hour of the slot they fall in. */
  readonly byHour: ReadonlyMap<number, readonly CalendarEntry[]>;
}

export const AGENDA_FIRST_HOUR = 8;
export const AGENDA_LAST_HOUR = 18;

export function agendaHours(): number[] {
  return Array.from(
    { length: AGENDA_LAST_HOUR - AGENDA_FIRST_HOUR + 1 },
    (_, index) => AGENDA_FIRST_HOUR + index,
  );
}

/**
 * The agenda of `day`: overdue entries first (only on today), entries that
 * span the day (and do not end on it) or have no time as all-day, the rest in the slot of their due
 * hour (hours outside the slots go to the first or last slot).
 */
export function buildAgenda(
  entries: readonly CalendarEntry[],
  day: DayKey,
  today: DayKey,
): CalendarAgenda {
  const covering = entries.filter((entry) => overlaps(entry, day, day));
  const overdue =
    day === today
      ? entries.filter(
          (entry) => entry.tone === "overdue" && entry.endDay < today,
        )
      : [];
  const overdueIds = new Set(overdue.map((entry) => entry.id));
  const allDay: CalendarEntry[] = [];
  const byHour = new Map<number, CalendarEntry[]>();
  for (const entry of covering) {
    if (overdueIds.has(entry.id)) continue;
    const timed =
      (entry.startDay === entry.endDay || entry.endDay === day) &&
      entry.kind === "item" &&
      entry.dueMinutes != null;
    if (!timed) {
      allDay.push(entry);
      continue;
    }
    const hour = Math.min(
      AGENDA_LAST_HOUR,
      Math.max(AGENDA_FIRST_HOUR, Math.floor((entry.dueMinutes ?? 0) / 60)),
    );
    byHour.set(hour, [...(byHour.get(hour) ?? []), entry]);
  }
  return { overdue, allDay, byHour };
}

/** Height of one agenda slot in px (the mockup's `.slot` minimum). */
export const AGENDA_SLOT_HEIGHT = 44;

/** Top offset of the "now" line from the first slot, in px. */
export function nowLineOffset(minutesOfDay: number): number | null {
  const first = AGENDA_FIRST_HOUR * 60;
  const end = (AGENDA_LAST_HOUR + 1) * 60;
  if (minutesOfDay < first || minutesOfDay >= end) return null;
  return ((minutesOfDay - first) / 60) * AGENDA_SLOT_HEIGHT;
}

// ─── Rescheduling ───────────────────────────────────────────────────────────

/**
 * The days an entry occupies after it is dropped on `toDay`. The due day
 * moves to `toDay` and the start day moves by the same number of days.
 */
export function rescheduledDays(
  entry: Pick<CalendarEntry, "startDay" | "endDay">,
  toDay: DayKey,
): { startDay: DayKey; endDay: DayKey; shift: number } {
  const shift = diffDays(entry.endDay, toDay);
  return {
    startDay: addDays(entry.startDay, shift),
    endDay: toDay,
    shift,
  };
}
