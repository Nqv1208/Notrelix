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

/** Fixed base epoch — 2025-01-15T09:00:00.000Z */
const BASE_EPOCH_MS = 1736934000000;

export function createMockClock(seed: number = 1001): MockClock {
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

export const defaultClock: MockClock = createMockClock(1001);
