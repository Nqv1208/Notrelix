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

export const ELEVATION_ROLES = ["raised", "overlay", "modal"] as const;
export type ElevationRole = (typeof ELEVATION_ROLES)[number];

const lightShadow = (alpha: number) => `oklch(0.22 0.01 255 / ${alpha})`;
const darkShadow = (alpha: number) => `oklch(0 0 0 / ${alpha})`;

const lightElevation: Record<ElevationRole, string> = {
  raised: `0 1px 2px 0 ${lightShadow(0.06)}, 0 1px 1px 0 ${lightShadow(0.04)}`,
  overlay: `0 4px 12px -2px ${lightShadow(0.1)}, 0 2px 4px -2px ${lightShadow(0.06)}`,
  modal: `0 16px 40px -8px ${lightShadow(0.18)}, 0 4px 12px -4px ${lightShadow(0.08)}`,
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
  },
  "high-contrast": lightElevation,
};
