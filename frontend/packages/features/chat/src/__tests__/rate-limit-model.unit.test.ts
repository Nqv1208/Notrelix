import { describe, expect, it } from "vitest";
import {
  SLOW_MODE_OPTIONS,
  remainingSeconds,
  slowModeChoices,
  splitWait,
} from "../core/model/rate-limit-model";

describe("rate limit model", () => {
  it("offers the standard slow modes and adds a channel's own value in order", () => {
    expect(slowModeChoices(30)).toEqual(SLOW_MODE_OPTIONS);
    expect(slowModeChoices(45)).toEqual([0, 5, 10, 30, 45, 60, 300, 900, 3600]);
    expect(slowModeChoices(99999).at(-1)).toBe(3600);
  });

  it("reads a wait in the unit that fits", () => {
    expect(splitWait(12)).toEqual({ unit: "seconds", count: 12 });
    expect(splitWait(90)).toEqual({ unit: "seconds", count: 90 });
    expect(splitWait(120)).toEqual({ unit: "minutes", count: 2 });
    expect(splitWait(3600)).toEqual({ unit: "hours", count: 1 });
    expect(splitWait(0.2)).toEqual({ unit: "seconds", count: 1 });
  });

  it("counts down without going below zero", () => {
    expect(remainingSeconds(5, 0)).toBe(5);
    expect(remainingSeconds(5, 3)).toBe(2);
    expect(remainingSeconds(5, 9)).toBe(0);
  });
});
