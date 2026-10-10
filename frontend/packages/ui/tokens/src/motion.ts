/**
 * @notrelix/ui-tokens — Motion tokens.
 *
 * Product motion is short and decelerating: 150–200 ms with ease-out curves
 * (FE-UI-015/016). Under `prefers-reduced-motion: reduce` every duration
 * collapses to ~0 so state still changes without movement (FE-UI-017).
 *
 * Framework-neutral: no React, no DOM.
 */

/** Durations in milliseconds. */
export const durationMs = {
  fast: 150,
  base: 180,
  slow: 200,
} as const;

export const duration = {
  fast: `${durationMs.fast}ms`,
  base: `${durationMs.base}ms`,
  slow: `${durationMs.slow}ms`,
} as const;

/** Reduced-motion duration (not 0, so transitionend still fires). */
export const reducedMotionDuration = "0.01ms";

export const easing = {
  /** Standard deceleration for enter/state changes. */
  out: "cubic-bezier(0.2, 0, 0, 1)",
  /** Emphasized deceleration for overlays entering. */
  "out-emphasized": "cubic-bezier(0.16, 1, 0.3, 1)",
} as const;
