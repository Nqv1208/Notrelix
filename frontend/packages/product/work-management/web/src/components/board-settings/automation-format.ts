/** Dates of the automation log, written in the board's time zone. */

const ZONE_PARTS = (iso: string, timeZone: string) => {
  const parts = new Intl.DateTimeFormat("en-GB", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).formatToParts(new Date(iso));
  const pick = (type: string) =>
    parts.find((part) => part.type === type)?.value ?? "";
  return {
    year: pick("year"),
    month: pick("month"),
    day: pick("day"),
    hour: pick("hour"),
    minute: pick("minute"),
  };
};

/** "10:28". */
export function timeOf(iso: string, timeZone: string): string {
  const p = ZONE_PARTS(iso, timeZone);
  return `${p.hour}:${p.minute}`;
}

/** "10/10". */
export function dayMonthOf(iso: string, timeZone: string): string {
  const p = ZONE_PARTS(iso, timeZone);
  return `${p.day}/${p.month}`;
}

/** "2026-10-10": the calendar day in the zone. */
export function dateKeyOf(iso: string, timeZone: string): string {
  const p = ZONE_PARTS(iso, timeZone);
  return `${p.year}-${p.month}-${p.day}`;
}

/** 0 = Sunday .. 6 = Saturday, in the zone. */
export function weekdayOf(iso: string, timeZone: string): number {
  const key = dateKeyOf(iso, timeZone);
  return new Date(`${key}T12:00:00Z`).getUTCDay();
}

export function isSameDay(a: string, b: string, timeZone: string): boolean {
  return dateKeyOf(a, timeZone) === dateKeyOf(b, timeZone);
}
