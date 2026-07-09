/**
 * @notrelix/ui-tokens — Motion and animation tokens.
 *
 * Framework-neutral: no React, no DOM.
 */

// ─── Duration ───

export const duration = {
  instant: '80ms',
  fast: '150ms',
  base: '250ms',
  slow: '400ms',
  deliberate: '600ms',
} as const;

// ─── Easing ───

export const easing = {
  out: 'cubic-bezier(0.0, 0.0, 0.2, 1)',
  in: 'cubic-bezier(0.4, 0.0, 1, 1)',
  spring: 'cubic-bezier(0.34, 1.56, 0.64, 1)',
} as const;
