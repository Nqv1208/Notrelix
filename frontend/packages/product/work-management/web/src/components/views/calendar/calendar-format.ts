import type { CalendarCopy } from "./calendar.copy";
import {
  dayNumber,
  formatDayMonth,
  formatFullDay,
  parseDayKey,
  weekDays,
  type CalendarRange,
  type DayKey,
} from "./calendar-model";

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

function capitalize(text: string): string {
  return text.charAt(0).toLocaleUpperCase() + text.slice(1);
}

function utcDate(day: DayKey): Date {
  return new Date(dayNumber(day) * 86_400_000);
}

/** Month name of the locale: "Tháng 10" or "October". */
export function monthName(day: DayKey, locale: string): string {
  return capitalize(
    new Intl.DateTimeFormat(locale, { month: "long", timeZone: "UTC" }).format(
      utcDate(day),
    ),
  );
}

/** Weekday name of the locale: "Thứ Hai" or "Monday". */
export function weekdayName(day: DayKey, locale: string): string {
  return capitalize(
    new Intl.DateTimeFormat(locale, {
      weekday: "long",
      timeZone: "UTC",
    }).format(utcDate(day)),
  );
}

/** The range heading: "Tháng 10, 2026", "12/10 – 18/10/2026", "Thứ Hai, 12/10/2026". */
export function calendarHeading(
  range: CalendarRange,
  anchor: DayKey,
  copy: CalendarCopy,
  locale: string,
): string {
  if (range === "month") {
    return fill(copy.heading.month, {
      month: monthName(anchor, locale),
      year: parseDayKey(anchor).year,
    });
  }
  if (range === "week") {
    const days = weekDays(anchor);
    const first = days[0] ?? anchor;
    const last = days[6] ?? anchor;
    return fill(copy.heading.week, {
      from: formatDayMonth(first),
      to: formatDayMonth(last),
      year: parseDayKey(last).year,
    });
  }
  return fill(copy.heading.day, {
    weekday: weekdayName(anchor, locale),
    date: formatFullDay(anchor),
  });
}

/** The accessible name of the calendar grid. */
export function calendarGridLabel(
  range: CalendarRange,
  anchor: DayKey,
  copy: CalendarCopy,
  locale: string,
  zoneName: string,
): string {
  if (range === "month") {
    return fill(copy.gridLabel.month, {
      month: monthName(anchor, locale),
      monthLower: monthName(anchor, locale).toLocaleLowerCase(locale),
      year: parseDayKey(anchor).year,
    });
  }
  if (range === "week") {
    const days = weekDays(anchor);
    const last = days[6] ?? anchor;
    return fill(copy.gridLabel.week, {
      from: formatDayMonth(days[0] ?? anchor),
      to: formatDayMonth(last),
      year: parseDayKey(last).year,
    });
  }
  return fill(copy.gridLabel.day, {
    date: formatFullDay(anchor),
    zone: zoneName,
  });
}
