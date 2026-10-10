/**
 * @notrelix/ui-tokens — Typography tokens.
 *
 * One family (Inter) with tabular numerals for data (WX-DEC-007). Fixed rem
 * scale; adjacent steps keep a 1.125–1.2 ratio. Semantic text roles follow
 * DESIGN.md §8.1 — components choose a role, not a pixel size.
 *
 * Framework-neutral: no React, no DOM.
 */

export const fonts = {
  sans: '"Inter Variable", "Inter", ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
  mono: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

export const weights = {
  regular: 400,
  medium: 500,
  semibold: 600,
  bold: 700,
} as const;

/** Fixed rem scale (1rem = 16px). Ratio between adjacent steps: 1.125–1.2. */
export const fontSizeScale = {
  xs: 0.75,
  sm: 0.875,
  md: 1,
  lg: 1.125,
  xl: 1.3125,
  "2xl": 1.5,
  "3xl": 1.75,
  "4xl": 2,
} as const;

export type FontSizeStep = keyof typeof fontSizeScale;

export interface TextRole {
  /** Step on {@link fontSizeScale}. */
  readonly size: FontSizeStep;
  /** Line height in rem. */
  readonly lineHeight: number;
  readonly weight: number;
  readonly letterSpacing: string;
  readonly family: "sans" | "mono";
  /** Use tabular numerals (aligned digits for data). */
  readonly tabular?: boolean;
}

/** Semantic text roles (DESIGN.md §8.1). */
export const textRoles = {
  display: {
    size: "4xl",
    lineHeight: 2.5,
    weight: weights.semibold,
    letterSpacing: "-0.022em",
    family: "sans",
  },
  "page-title": {
    size: "2xl",
    lineHeight: 2,
    weight: weights.semibold,
    letterSpacing: "-0.017em",
    family: "sans",
  },
  "section-heading": {
    size: "lg",
    lineHeight: 1.75,
    weight: weights.semibold,
    letterSpacing: "-0.011em",
    family: "sans",
  },
  subheading: {
    size: "md",
    lineHeight: 1.5,
    weight: weights.medium,
    letterSpacing: "-0.006em",
    family: "sans",
  },
  body: {
    size: "sm",
    lineHeight: 1.375,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  "dense-body": {
    size: "sm",
    lineHeight: 1.25,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  label: {
    size: "sm",
    lineHeight: 1.25,
    weight: weights.medium,
    letterSpacing: "0em",
    family: "sans",
  },
  metadata: {
    size: "xs",
    lineHeight: 1,
    weight: weights.regular,
    letterSpacing: "0.005em",
    family: "sans",
    tabular: true,
  },
  caption: {
    size: "xs",
    lineHeight: 1,
    weight: weights.regular,
    letterSpacing: "0.005em",
    family: "sans",
  },
  code: {
    size: "sm",
    lineHeight: 1.25,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "mono",
  },
  prose: {
    size: "md",
    lineHeight: 1.75,
    weight: weights.regular,
    letterSpacing: "-0.003em",
    family: "sans",
  },
} as const satisfies Record<string, TextRole>;

export type TextRoleName = keyof typeof textRoles;

/** Inter OpenType features applied to data: tabular, slashed-zero off. */
export const numericFeatures = {
  tabular: "tabular-nums",
} as const;
