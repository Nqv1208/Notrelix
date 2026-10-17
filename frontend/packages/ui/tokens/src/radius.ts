/**
 * @notrelix/ui-tokens — Radius tokens.
 *
 * One shape language (FE-UI-022). `base` drives the shadcn-derived scale in
 * the web host; semantic roles name where each step is used.
 *
 * Framework-neutral: no React, no DOM.
 */

export const radius = {
  none: "0rem",
  sm: "0.375rem",
  md: "0.5rem",
  lg: "0.625rem",
  xl: "0.875rem",
  full: "9999px",
} as const;

/** Base radius for the shadcn calc() scale (equals `lg`). */
export const radiusBase = radius.lg;

export const radiusRoles = {
  control: radius.md,
  surface: radius.lg,
  overlay: radius.xl,
  badge: radius.full,
  pill: radius.full,
} as const;
