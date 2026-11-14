/**
 * The wait of a 429 (UC-CHT-H08): how the retry countdown reads and which options a manager can pick for a channel's slow mode.
 * Pure: the countdown itself is driven by the component, so nothing here reads a clock.
 */

import { CHAT_LIMITS } from "../types/chat";

/** The slow mode choices of the channel settings, in seconds; 0 is off. */
export const SLOW_MODE_OPTIONS: readonly number[] = [
  0, 5, 10, 30, 60, 300, 900, 3600,
];

/** The options to offer: the standard ones, plus the channel's current value when it is not one of them. */
export function slowModeChoices(current: number): readonly number[] {
  const value = Math.min(
    Math.max(0, Math.floor(current)),
    CHAT_LIMITS.slowModeMaxSeconds,
  );
  return SLOW_MODE_OPTIONS.includes(value)
    ? SLOW_MODE_OPTIONS
    : [...SLOW_MODE_OPTIONS, value].sort((a, b) => a - b);
}

export type WaitUnit = "seconds" | "minutes" | "hours";

/** A wait split into the unit that reads best: seconds under a minute, whole minutes, whole hours, else seconds. */
export function splitWait(seconds: number): {
  readonly unit: WaitUnit;
  readonly count: number;
} {
  const whole = Math.max(0, Math.ceil(seconds));
  if (whole >= 3600 && whole % 3600 === 0)
    return { unit: "hours", count: whole / 3600 };
  if (whole >= 60 && whole % 60 === 0)
    return { unit: "minutes", count: whole / 60 };
  return { unit: "seconds", count: whole };
}

/** The seconds still to wait after `elapsed` seconds of the countdown; never below zero. */
export function remainingSeconds(total: number, elapsed: number): number {
  return Math.max(0, Math.ceil(total) - Math.max(0, Math.floor(elapsed)));
}
