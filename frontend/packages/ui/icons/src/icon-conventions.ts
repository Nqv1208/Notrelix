/**
 * @notrelix/ui-icons — Icon sizing/stroke conventions.
 *
 * Stable sizing and stroke conventions for every icon rendered from
 * @notrelix/ui-icons. Consuming components may pass lucide `size`/`strokeWidth`
 * overrides, but the default set below is the product convention:
 *
 *   - 16px: inline/compact controls (table cells, breadcrumbs)
 *   - 20px: controls, list rows, tabular metadata
 *   - 24px: empty states, page-level affordances, navigation rail
 *   - strokeWidth 2: default product weight
 *
 * Business-accessible names (aria-label) belong to the consuming component,
 * not to the icon itself.
 */

export const ICON_SIZE_SM = 16 as const;
export const ICON_SIZE_MD = 20 as const;
export const ICON_SIZE_LG = 24 as const;
export const ICON_STROKE_WIDTH = 2 as const;

export const ICON_SIZES = {
  sm: ICON_SIZE_SM,
  md: ICON_SIZE_MD,
  lg: ICON_SIZE_LG,
} as const;

export type IconSize = keyof typeof ICON_SIZES;
