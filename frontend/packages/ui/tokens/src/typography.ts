/**
 * @notrelix/ui-tokens — Typography tokens.
 *
 * One UI family (Be Vietnam Pro, 400/500/600/700) with tabular numerals for
 * data, and IBM Plex Mono (400/500) for keys, code and IDs (WX-DEC-013,
 * superseding WX-DEC-007). Fixed rem scale; adjacent steps keep a 1.125–1.2
 * ratio. Semantic text roles follow
 * DESIGN.md §8.1 — components choose a role, not a pixel size.
 *
 * Framework-neutral: no React, no DOM.
 */

export const fonts = {
  sans: '"Be Vietnam Pro", ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
  mono: '"IBM Plex Mono", ui-monospace, SFMono-Regular, Menlo, Consolas, monospace',
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

/**
 * Off-grid sizes of the October 2026 mockups' dense chrome (rail, sidebar,
 * marks, keys). They sit between modular steps on purpose and are only
 * reachable through the roles below; the modular-ratio contract applies to
 * {@link fontSizeScale} alone. Keys are the size in px, `p` for the point.
 */
export const fontSizeDense = {
  "7": 0.4375,
  "9": 0.5625,
  "10": 0.625,
  "10p5": 0.65625,
  "11": 0.6875,
  "13": 0.8125,
  "13p5": 0.84375,
} as const;

export type FontSizeStep = keyof typeof fontSizeScale;
export type DenseFontSizeStep = keyof typeof fontSizeDense;

export interface TextRole {
  /** Step on {@link fontSizeScale} or {@link fontSizeDense}. */
  readonly size: FontSizeStep | DenseFontSizeStep;
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
  /** Navigation rows, department rows and tabs (mockup 13.5px / 500). */
  nav: {
    size: "13p5",
    lineHeight: 1.25,
    weight: weights.medium,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Compact controls and nested rows (mockup 13px). */
  compact: {
    size: "13",
    lineHeight: 1.25,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Label under a system rail entry (mockup 10.5px / 500). */
  "rail-label": {
    size: "10p5",
    lineHeight: 0.9375,
    weight: weights.medium,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Keyboard key chip (mockup 11px mono). */
  key: {
    size: "11",
    lineHeight: 1.125,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "mono",
  },
  /** Initials in a 16px mark (sub-department chip). */
  "mark-2xs": {
    size: "7",
    lineHeight: 1,
    weight: weights.bold,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Initials in a 20px mark (department chip). */
  "mark-xs": {
    size: "9",
    lineHeight: 1,
    weight: weights.bold,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Initials in a 26px avatar; unread count badges. */
  "mark-sm": {
    size: "10",
    lineHeight: 1,
    weight: weights.bold,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Initials in a 28px mark (workspace chip). */
  "mark-md": {
    size: "11",
    lineHeight: 1,
    weight: weights.bold,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Initials in a 40px mark (rail workspace tile). */
  "mark-lg": {
    size: "13",
    lineHeight: 1,
    weight: weights.bold,
    letterSpacing: "0em",
    family: "sans",
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

/** Be Vietnam Pro OpenType features applied to data: tabular numerals (WX-DEC-013). */
export const numericFeatures = {
  tabular: "tabular-nums",
} as const;
