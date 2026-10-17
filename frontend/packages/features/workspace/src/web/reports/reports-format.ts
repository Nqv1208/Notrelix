import type { SurfaceFormatters } from "../home/home-models";

/** The locale formatters the Reports surfaces use (`useFormatters()`). */
export interface ReportFormatters extends SurfaceFormatters {
  readonly number: (
    value: number,
    options?: Intl.NumberFormatOptions,
  ) => string;
  readonly list: (
    items: readonly string[],
    options?: Intl.ListFormatOptions,
  ) => string;
}

const asDay = (day: string) => Date.parse(`${day}T00:00:00Z`);

/** "14/10" of a "YYYY-MM-DD" day (day first in every locale). */
export function dayLabel(_formatters: ReportFormatters, day: string): string {
  return `${day.slice(8, 10)}/${day.slice(5, 7)}`;
}

/** "13–19/10", or "26/10–01/11" when the months differ. */
export function rangeLabel(
  formatters: ReportFormatters,
  from: string,
  to: string,
): string {
  const sameMonth = from.slice(0, 7) === to.slice(0, 7);
  const start = sameMonth ? from.slice(8, 10) : dayLabel(formatters, from);
  return `${start}–${dayLabel(formatters, to)}`;
}

/** "Thứ Tư" of a day. */
export function weekdayLabel(
  formatters: ReportFormatters,
  day: string,
): string {
  return formatters.date(asDay(day), { weekday: "long", timeZone: "UTC" });
}

/** "10:38" of a moment in the report's time zone. */
export function timeLabel(
  formatters: ReportFormatters,
  moment: string,
  timeZone: string,
): string {
  return formatters.date(Date.parse(moment), {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
    timeZone,
  });
}

/** The "YYYY-MM-DD" day of a moment in the report's time zone. */
export function dayOfMoment(moment: string, timeZone: string): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone }).format(
    Date.parse(moment),
  );
}

/** True when two moments fall on the same day in the report's time zone. */
export function sameDay(a: string, b: string, timeZone: string): boolean {
  return dayOfMoment(a, timeZone) === dayOfMoment(b, timeZone);
}

/** "1,7": at most one decimal. */
export function decimalLabel(
  formatters: ReportFormatters,
  value: number,
): string {
  return formatters.number(value, {
    maximumFractionDigits: 1,
    minimumFractionDigits: 0,
  });
}

export function percentOf(done: number, total: number): number {
  return total === 0 ? 0 : Math.round((done / total) * 100);
}
