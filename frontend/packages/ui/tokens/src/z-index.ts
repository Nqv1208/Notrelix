/**
 * @notrelix/ui-tokens — Semantic stacking order.
 *
 * dropdown → sticky → backdrop → modal → toast → tooltip. Components use the
 * role, never a raw number.
 *
 * Framework-neutral: no React, no DOM.
 */

export const zIndex = {
  dropdown: 1000,
  sticky: 1100,
  backdrop: 1200,
  modal: 1300,
  toast: 1400,
  tooltip: 1500,
} as const;

export type ZIndexRole = keyof typeof zIndex;

export const Z_INDEX_ORDER: readonly ZIndexRole[] = [
  "dropdown",
  "sticky",
  "backdrop",
  "modal",
  "toast",
  "tooltip",
];
