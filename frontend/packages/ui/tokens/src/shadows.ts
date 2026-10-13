/**
 * @notrelix/ui-tokens — Elevation (shadow) tokens.
 *
 * Elevation is semantic (FE-UI-023): raised content, floating overlays and
 * modals. Dark mode uses deeper shadows plus the lighter surface tier for
 * perceived height; high contrast relies on borders and keeps light shadows.
 *
 * Framework-neutral: no React, no DOM.
 */

import type { ThemeAppearance } from "./color/palette";

export const ELEVATION_ROLES = [
  "raised",
  "overlay",
  "modal",
  "card",
  "card-hover",
  "sheet",
  "popover",
  "dialog",
  "segment",
  "bar",
] as const;
export type ElevationRole = (typeof ELEVATION_ROLES)[number];

const lightShadow = (alpha: number) => `oklch(0.22 0.01 255 / ${alpha})`;
const darkShadow = (alpha: number) => `oklch(0 0 0 / ${alpha})`;

/** Board card, its hover and the item sheet: the mockup's flat shadows. */
const lightBoard = (alpha: number) => `oklch(0.217 0.022 260.5 / ${alpha})`;
const lightSheet = (alpha: number) => `oklch(0.208 0.04 265.8 / ${alpha})`;

const lightElevation: Record<ElevationRole, string> = {
  raised: `0 1px 2px 0 ${lightShadow(0.06)}, 0 1px 1px 0 ${lightShadow(0.04)}`,
  overlay: `0 4px 12px -2px ${lightShadow(0.1)}, 0 2px 4px -2px ${lightShadow(0.06)}`,
  modal: `0 16px 40px -8px ${lightShadow(0.18)}, 0 4px 12px -4px ${lightShadow(0.08)}`,
  card: `0 1px 2px 0 ${lightBoard(0.08)}`,
  "card-hover": `0 3px 10px 0 ${lightBoard(0.12)}`,
  sheet: `-18px 0 40px 0 ${lightSheet(0.14)}`,
  popover: `0 12px 32px 0 ${lightSheet(0.16)}`,
  dialog: `0 24px 60px 0 ${darkShadow(0.28)}`,
  segment: `0 1px 2px 0 ${lightSheet(0.12)}`,
  bar: `0 12px 32px 0 ${lightSheet(0.3)}`,
};

export const elevation: Record<
  ThemeAppearance,
  Record<ElevationRole, string>
> = {
  light: lightElevation,
  dark: {
    raised: `0 1px 2px 0 ${darkShadow(0.4)}`,
    overlay: `0 6px 16px -4px ${darkShadow(0.5)}, 0 2px 4px -2px ${darkShadow(0.4)}`,
    modal: `0 20px 48px -8px ${darkShadow(0.6)}, 0 4px 12px -4px ${darkShadow(0.4)}`,
    card: `0 1px 2px 0 ${darkShadow(0.4)}`,
    "card-hover": `0 3px 10px 0 ${darkShadow(0.5)}`,
    sheet: `-18px 0 40px 0 ${darkShadow(0.5)}`,
    popover: `0 12px 32px 0 ${darkShadow(0.5)}`,
    dialog: `0 24px 60px 0 ${darkShadow(0.28)}`,
    segment: `0 1px 2px 0 ${lightSheet(0.12)}`,
    bar: `0 12px 32px 0 ${darkShadow(0.5)}`,
  },
  "high-contrast": lightElevation,
};
