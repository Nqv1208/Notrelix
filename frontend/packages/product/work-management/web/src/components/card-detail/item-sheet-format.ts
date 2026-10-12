import {
  fillSheetCopy,
  pluralSheetCopy,
  type ItemSheetCopy,
} from "./item-sheet.copy";

/**
 * Date wording for the item side sheet. Every function takes the injected
 * `now` (never reads the clock) and an optional IANA `timeZone`; without one
 * the runtime time zone is used, like the Kanban card. Calendar days are
 * compared, not 24-hour spans: an item due earlier today is "today".
 */

interface CalendarParts {
  readonly year: number;
  readonly month: number;
  readonly day: number;
  readonly hour: number;
  readonly minute: number;
}

const DAY_MS = 86_400_000;

function toDate(value: string | Date): Date | null {
  const date = typeof value === "string" ? new Date(value) : value;
  return Number.isNaN(date.getTime()) ? null : date;
}

function calendarParts(date: Date, timeZone?: string): CalendarParts {
  const parts = new Intl.DateTimeFormat("en-GB", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).formatToParts(date);
  const value = (type: Intl.DateTimeFormatPartTypes) =>
    Number(parts.find((part) => part.type === type)?.value ?? 0);
  return {
    year: value("year"),
    month: value("month"),
    day: value("day"),
    hour: value("hour"),
    minute: value("minute"),
  };
}

function dayNumber(parts: CalendarParts): number {
  return Math.round(Date.UTC(parts.year, parts.month - 1, parts.day) / DAY_MS);
}

function pad(value: number): string {
  return String(value).padStart(2, "0");
}

/** "10/10", or "18/01/2027" when the year differs from `now`. */
export function formatSheetDate(
  value: string | Date,
  now: Date,
  timeZone?: string,
): string | null {
  const date = toDate(value);
  if (!date) return null;
  const parts = calendarParts(date, timeZone);
  const today = calendarParts(now, timeZone);
  const dayMonth = `${pad(parts.day)}/${pad(parts.month)}`;
  return parts.year === today.year ? dayMonth : `${dayMonth}/${parts.year}`;
}

/** Calendar days from today to `value`: negative in the past, 0 today. */
export function calendarDaysFromToday(
  value: string | Date,
  now: Date,
  timeZone?: string,
): number | null {
  const date = toDate(value);
  if (!date) return null;
  return (
    dayNumber(calendarParts(date, timeZone)) -
    dayNumber(calendarParts(now, timeZone))
  );
}

export type SheetDueTone = "overdue" | "today" | "upcoming" | "done";

export interface SheetDue {
  readonly text: string;
  readonly tone: SheetDueTone;
}

/**
 * Due pill wording: "10/10 · quá hạn 2 ngày", "12/10 · hôm nay" or "18/10".
 * A done item shows only the date and is never overdue.
 */
export function describeSheetDue(
  dueAt: string | null | undefined,
  now: Date,
  isDone: boolean,
  copy: ItemSheetCopy,
  timeZone?: string,
): SheetDue | null {
  if (!dueAt) return null;
  const date = formatSheetDate(dueAt, now, timeZone);
  const days = calendarDaysFromToday(dueAt, now, timeZone);
  if (date === null || days === null) return null;
  if (isDone) return { text: date, tone: "done" };
  if (days < 0) {
    return {
      text: pluralSheetCopy(
        copy.properties.dueOverdue_one,
        copy.properties.dueOverdue_other,
        -days,
        { date },
      ),
      tone: "overdue",
    };
  }
  if (days === 0) {
    return {
      text: fillSheetCopy(copy.properties.dueToday, { date }),
      tone: "today",
    };
  }
  return { text: date, tone: "upcoming" };
}

/**
 * Comment and history time: "Vừa xong", "5 phút trước", "2 giờ trước",
 * "Hôm qua, 16:40", "5 ngày trước", then the date.
 */
export function describeSheetTime(
  value: string,
  now: Date,
  copy: ItemSheetCopy,
  timeZone?: string,
): string {
  const date = toDate(value);
  if (!date) return "";
  const elapsedMinutes = Math.floor((now.getTime() - date.getTime()) / 60_000);
  const days = -(calendarDaysFromToday(date, now, timeZone) ?? 0);
  const { time } = copy;
  if (elapsedMinutes < 1) return time.justNow;
  if (elapsedMinutes < 60) {
    return pluralSheetCopy(
      time.minutesAgo_one,
      time.minutesAgo_other,
      elapsedMinutes,
    );
  }
  if (days <= 0) {
    return pluralSheetCopy(
      time.hoursAgo_one,
      time.hoursAgo_other,
      Math.floor(elapsedMinutes / 60),
    );
  }
  if (days === 1) {
    const parts = calendarParts(date, timeZone);
    return fillSheetCopy(time.yesterday, {
      time: `${pad(parts.hour)}:${pad(parts.minute)}`,
    });
  }
  if (days < 7) {
    return pluralSheetCopy(time.daysAgo_one, time.daysAgo_other, days);
  }
  return formatSheetDate(date, now, timeZone) ?? "";
}
