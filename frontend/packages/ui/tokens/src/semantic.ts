/**
 * @notrelix/ui-tokens — Semantic non-color roles.
 *
 * Surface tiers and the focus-ring geometry. Colors for these roles are
 * resolved per appearance × accent in `./color/theme`.
 *
 * Framework-neutral: no React, no DOM.
 */

/** Surface tiers, lowest to highest (DESIGN.md §10.1). */
export const SURFACE_TIERS = ["canvas", "sidebar", "panel", "raised"] as const;
export type SurfaceTier = (typeof SURFACE_TIERS)[number];

/** Focus ring geometry; color is the `focus-ring` role (≥3:1 on every surface). */
export const focusRing = {
  width: "2px",
  offset: "2px",
} as const;
