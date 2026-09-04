export const FIXED_NOW = "2026-01-15T12:00:00.000Z";

export function fixedClock(): Date {
  return new Date(FIXED_NOW);
}

export function fixedIso(offsetDays = 0): string {
  const date = fixedClock();
  date.setUTCDate(date.getUTCDate() + offsetDays);
  return date.toISOString();
}
