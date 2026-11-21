/** The shortest and longest glide of a scripted scroll, in milliseconds. */
export const SCROLL_MIN_MS = 220;
export const SCROLL_MAX_MS = 450;

/** Longer trips take longer, but never beyond the cap: about 1 ms per 7 px on top of the minimum. */
export function scrollDuration(distance: number): number {
  return Math.min(
    SCROLL_MAX_MS,
    Math.max(SCROLL_MIN_MS, SCROLL_MIN_MS + Math.abs(distance) / 7),
  );
}

function easeOutCubic(t: number, power = 3): number {
  return 1 - Math.pow(1 - t, power);
}

export function prefersReducedMotion(): boolean {
  return (
    typeof window !== "undefined" &&
    window.matchMedia?.("(prefers-reduced-motion: reduce)").matches === true
  );
}

/**
 * Glides a scroller to `top` (an ease-out of 220-450 ms by distance); under reduced motion, or for a trip of a
 * pixel, it jumps. `top` may be a function so a target that moves while rows measure is followed. Returns a cancel.
 */
export function animateScrollTo(
  el: HTMLElement,
  top: number | (() => number),
  options: {
    readonly reducedMotion?: boolean;
    readonly durationMs?: number;
    /** A gentler ease-out (quadratic) for a short follow, so a dropped frame never carries most of the trip. */
    readonly gentle?: boolean;
  } = {},
): () => void {
  const target = () => (typeof top === "function" ? top() : top);
  const from = el.scrollTop;
  const distance = target() - from;
  if (
    (options.reducedMotion ?? prefersReducedMotion()) ||
    (Math.abs(distance) <= 1 && options.durationMs === undefined)
  ) {
    el.scrollTop = target();
    return () => undefined;
  }
  const duration = options.durationMs ?? scrollDuration(distance);
  const start = performance.now();
  let frame = 0;
  const step = (now: number) => {
    const t = Math.min(1, (now - start) / duration);
    const next =
      from + (target() - from) * easeOutCubic(t, options.gentle ? 2 : 3);
    // A glide down only ever goes forward: the end can dip for a frame while rows measure, and the list is not pulled back.
    el.scrollTop = distance > 0 ? Math.max(el.scrollTop, next) : next;
    if (t < 1) frame = requestAnimationFrame(step);
  };
  frame = requestAnimationFrame(step);
  return () => cancelAnimationFrame(frame);
}
