/**
 * Deterministic mock clock.
 *
 * All factories and handlers use this instead of uncontrolled `new Date()` or
 * `Math.random()` calls, satisfying the determinism invariant.
 *
 * Plan: 03-MOCK-DATA-MODEL.md §Clock, 01-ARCHITECTURE-SPEC.md §9
 */

export interface MockClock {
  now(): Date;
  isoNow(): string;
  offsetSeconds(seconds: number): string;
  offsetMinutes(minutes: number): string;
  offsetDays(days: number): string;
}

/**
 * Fixed base epoch: Monday 2026-10-12 10:40 in Hanoi (03:40Z), the "today"
 * of the 2026-10 design mockups and of the e2e/fidelity page clock, less the
 * default seed's offset below so the default world's "now" is exactly 10:40.
 */
const DEFAULT_SEED = 1001;
const BASE_EPOCH_MS = Date.UTC(2026, 9, 12, 3, 40, 0) - DEFAULT_SEED * 1000;

export function createMockClock(seed: number = DEFAULT_SEED): MockClock {
  // Deterministic offset derived from seed — no wall-clock reference
  const baseMs = BASE_EPOCH_MS + (seed % 10000) * 1000;

  return {
    now(): Date {
      return new Date(baseMs);
    },
    isoNow(): string {
      return new Date(baseMs).toISOString();
    },
    offsetSeconds(seconds: number): string {
      return new Date(baseMs + seconds * 1000).toISOString();
    },
    offsetMinutes(minutes: number): string {
      return new Date(baseMs + minutes * 60_000).toISOString();
    },
    offsetDays(days: number): string {
      return new Date(baseMs + days * 86_400_000).toISOString();
    },
  };
}

export const defaultClock: MockClock = createMockClock(DEFAULT_SEED);

/** A UTC instant from calendar parts (the month is 0-based). */
export function utcInstant(
  year: number,
  monthIndex: number,
  day: number,
  hour: number,
  minute = 0,
): string {
  return new Date(
    Date.UTC(year, monthIndex, day, hour, minute, 0),
  ).toISOString();
}

/**
 * The mock clock's Hanoi day (UTC+7) at `hour:minute` Hanoi time, shifted by
 * `dayOffset` days, as a UTC instant.
 */
export function hanoiDayAt(
  clock: MockClock,
  hour: number,
  minute = 0,
  dayOffset = 0,
): string {
  const hanoi = new Date(clock.now().getTime() + 7 * 60 * 60 * 1000);
  return utcInstant(
    hanoi.getUTCFullYear(),
    hanoi.getUTCMonth(),
    hanoi.getUTCDate() + dayOffset,
    hour - 7,
    minute,
  );
}

/** "dd/MM" of an instant in Hanoi (UTC+7). */
export function hanoiShortDay(iso: string): string {
  const hanoi = new Date(Date.parse(iso) + 7 * 60 * 60 * 1000);
  return `${String(hanoi.getUTCDate()).padStart(2, "0")}/${String(
    hanoi.getUTCMonth() + 1,
  ).padStart(2, "0")}`;
}
