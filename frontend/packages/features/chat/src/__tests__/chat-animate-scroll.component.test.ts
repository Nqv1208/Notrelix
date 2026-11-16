import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  SCROLL_MAX_MS,
  SCROLL_MIN_MS,
  animateScrollTo,
  scrollDuration,
} from "../web/components/animate-scroll";

describe("scrollDuration", () => {
  it("stays between 220 and 450 ms and grows with the distance", () => {
    expect(scrollDuration(0)).toBe(SCROLL_MIN_MS);
    expect(scrollDuration(700)).toBeGreaterThan(SCROLL_MIN_MS);
    expect(scrollDuration(700)).toBeLessThan(scrollDuration(1400));
    expect(scrollDuration(100000)).toBe(SCROLL_MAX_MS);
  });
});

describe("animateScrollTo", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.spyOn(performance, "now").mockImplementation(() => Date.now());
    vi.stubGlobal("requestAnimationFrame", (cb: FrameRequestCallback) =>
      setTimeout(() => cb(performance.now()), 16),
    );
    vi.stubGlobal("cancelAnimationFrame", (id: number) => clearTimeout(id));
  });
  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("glides: part way at half time, there after the duration", () => {
    const el = document.createElement("div");
    animateScrollTo(el, 1000, { reducedMotion: false });
    expect(el.scrollTop).toBe(0);
    vi.advanceTimersByTime(scrollDuration(1000) / 2);
    expect(el.scrollTop).toBeGreaterThan(0);
    expect(el.scrollTop).toBeLessThan(1000);
    vi.advanceTimersByTime(scrollDuration(1000));
    expect(el.scrollTop).toBe(1000);
  });

  it("jumps at once under reduced motion", () => {
    const el = document.createElement("div");
    animateScrollTo(el, 1000, { reducedMotion: true });
    expect(el.scrollTop).toBe(1000);
  });

  it("stops when cancelled", () => {
    const el = document.createElement("div");
    const cancel = animateScrollTo(el, 1000, { reducedMotion: false });
    vi.advanceTimersByTime(100);
    const at = el.scrollTop;
    cancel();
    vi.advanceTimersByTime(1000);
    expect(el.scrollTop).toBe(at);
  });
});
