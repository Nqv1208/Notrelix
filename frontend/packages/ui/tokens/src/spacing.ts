/**
 * @notrelix/ui-tokens — Spacing and layout tokens.
 *
 * 4px base grid expressed in rem. Semantic roles name repeated rhythm; one-off
 * gaps use the Tailwind scale directly (FE-UI-018).
 *
 * Framework-neutral: no React, no DOM.
 */

export const baseUnitPx = 4;

/** Spacing scale in rem (step × 4px). */
export const spacing = {
  0: "0rem",
  1: "0.25rem",
  2: "0.5rem",
  3: "0.75rem",
  4: "1rem",
  5: "1.25rem",
  6: "1.5rem",
  8: "2rem",
  10: "2.5rem",
  12: "3rem",
  16: "4rem",
} as const;

/** Semantic spacing roles. */
export const space = {
  "inline-xs": spacing[1],
  "inline-sm": spacing[2],
  "inline-md": spacing[3],
  "stack-xs": spacing[1],
  "stack-sm": spacing[2],
  "stack-md": spacing[4],
  "stack-lg": spacing[6],
  section: spacing[12],
  "page-gutter": spacing[6],
  "panel-padding": spacing[4],
} as const;

/** Control and row heights (density contract inputs). */
export const sizes = {
  "control-sm": "2rem",
  "control-md": "2.25rem",
  "control-lg": "2.5rem",
  "row-dense": "2rem",
  row: "2.5rem",
  "touch-target": "2.75rem",
} as const;

export const layout = {
  "sidebar-width": "15rem",
  "sidebar-collapsed-width": "3.5rem",
  "topbar-height": "3.5rem",
  "content-max-width": "45rem",
  "page-max-width": "80rem",
  /** Fixed width of a Kanban column (and its skeleton / add-column slot). */
  "kanban-column": "16.5rem",
} as const;
