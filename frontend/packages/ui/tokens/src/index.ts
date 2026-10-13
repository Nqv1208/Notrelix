// @notrelix/ui-tokens — Design tokens barrel export (framework-neutral).

// Color: OKLCH model, palette, semantic roles per appearance × accent.
export * from "./colors";

export {
  fonts,
  weights,
  fontSizeScale,
  fontSizeDense,
  textRoles,
  numericFeatures,
} from "./typography";
export type {
  DenseFontSizeStep,
  FontSizeStep,
  TextRole,
  TextRoleName,
} from "./typography";

export { baseUnitPx, spacing, space, sizes, layout } from "./spacing";

export { radius, radiusBase, radiusRoles } from "./radius";

export { elevation, ELEVATION_ROLES } from "./shadows";
export type { ElevationRole } from "./shadows";

export { duration, durationMs, easing, reducedMotionDuration } from "./motion";

export { zIndex, Z_INDEX_ORDER } from "./z-index";
export type { ZIndexRole } from "./z-index";

export { SURFACE_TIERS, focusRing } from "./semantic";
export type { SurfaceTier } from "./semantic";

export { lightTheme } from "./themes/light";
export { darkTheme } from "./themes/dark";
export { highContrastTheme } from "./themes/high-contrast";
export { resolveThemeVariables } from "./themes/resolve";

export { buildTokensCss } from "./css/build-css";

/**
 * Combines class names with Tailwind merge support.
 */
import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
