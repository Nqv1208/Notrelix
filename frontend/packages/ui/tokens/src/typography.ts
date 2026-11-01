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
  "10p4": 0.6510417,
  "10p5": 0.65625,
  "11": 0.6875,
  "13": 0.8125,
  "13p5": 0.84375,
  "8": 0.5,
  "8p5": 0.53125,
  "7p5": 0.46875,
  "17": 1.0625,
  "9p5": 0.59375,
  "11p5": 0.71875,
  "12p5": 0.78125,
  "14p5": 0.90625,
  "15": 0.9375,
  "15p5": 0.96875,
  "19": 1.1875,
  "20": 1.25,
  "21": 1.3125,
  "22": 1.375,
  "26": 1.625,
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
  /** Board and item title in the page header (mockup 21px / 650). */
  "board-title": {
    size: "xl",
    lineHeight: 1.70625,
    weight: 650,
    letterSpacing: "-0.01em",
    family: "sans",
  },
  /** Title of the item side sheet (mockup 20px / 650). */
  "sheet-title": {
    size: "20",
    lineHeight: 1.6875,
    weight: 650,
    letterSpacing: "-0.01em",
    family: "sans",
  },
  /** Breadcrumb and secondary lines (mockup 12.5px). */
  crumb: {
    size: "12p5",
    lineHeight: 1.1328,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Board card tag and progress count (mockup 11.5px). */
  micro: {
    size: "11p5",
    lineHeight: 1.0422,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Initials in an 18px mark (item sheet breadcrumb). */
  "mark-3xs": {
    size: "8",
    lineHeight: 1,
    weight: weights.bold,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Initials in a 20px person avatar (item sheet properties). */
  "avatar-sm": {
    size: "8p5",
    lineHeight: 1,
    weight: weights.bold,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Initials in a 26px person avatar (item sheet comments). */
  "avatar-md": {
    size: "9p5",
    lineHeight: 1,
    weight: weights.bold,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Initials in an 18px person avatar (Calendar events, mockup 7.5px / 700). */
  "avatar-xs": {
    size: "7p5",
    lineHeight: 1,
    weight: weights.bold,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Hint line under an input (mockup 11px). */
  hint: {
    size: "11",
    lineHeight: 1,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Range heading of the Calendar bar (mockup 17px / 650). */
  "calendar-title": {
    size: "17",
    lineHeight: 1.540625,
    weight: 650,
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
  /** Item page title (Final-Detail h1, 28px / 650, line 1.28). */
  "item-title": {
    size: "3xl",
    lineHeight: 2.24,
    weight: 650,
    letterSpacing: "-0.015em",
    family: "sans",
  },
  /** Item page section heading (Final-Detail h2, 15px / 650). */
  "item-heading": {
    size: "15",
    lineHeight: 1.359375,
    weight: 650,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Item page description (Final-Detail `.desc`, 14.5px / 1.7). */
  "item-prose": {
    size: "14p5",
    lineHeight: 1.540625,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Item page rows and fields (Final-Detail body, 14px / 1.45). */
  "item-text": {
    size: "sm",
    lineHeight: 1.26875,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Item page comment text (Final-Detail `.cm p`, 14px / 1.6). */
  "item-comment": {
    size: "sm",
    lineHeight: 1.4,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Item page property values, tabs and links (Final-Detail, 13.5px / 1.45). */
  "item-ui": {
    size: "13p5",
    lineHeight: 1.2234375,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Item page small controls and links (Final-Detail, 13px / 1.45). */
  "item-small": {
    size: "13",
    lineHeight: 1.178125,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Item page notes, times and group labels (Final-Detail, 12px / 1.45). */
  "item-note": {
    size: "xs",
    lineHeight: 1.0875,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Document title (Final-Docs h1, 32px). */
  "doc-title": {
    size: "4xl",
    lineHeight: 2.5,
    weight: 650,
    letterSpacing: "-0.015em",
    family: "sans",
  },
  /** Document section heading (Final-Docs h2, 20px). */
  "doc-heading": {
    size: "20",
    lineHeight: 1.6875,
    weight: 650,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Document paragraph (Final-Docs `.prose`, 15.5px / 1.7). */
  "doc-prose": {
    size: "15p5",
    lineHeight: 1.646875,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Document ordered list item (15.5px / 1.75). */
  "doc-list": {
    size: "15p5",
    lineHeight: 1.6953125,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Document checklist item (15px). */
  "doc-check": {
    size: "15",
    lineHeight: 1.359375,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Document callout (14.5px / 1.6). */
  "doc-callout": {
    size: "14p5",
    lineHeight: 1.45,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Dialog title (Final-Docs share dialog, 18px). */
  "dialog-title": {
    size: "lg",
    lineHeight: 1.51875,
    weight: 650,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Command palette query (Final-Command `.q`, 17px, line 1.45). */
  "palette-query": {
    size: "17",
    lineHeight: 1.540625,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** The `small` under a value of the department access card (Final-Space `.acc dd small`, 12.5px reduced one step to 10.4px, line 1.45). */
  "note-small": {
    size: "10p4",
    lineHeight: 0.94375,
    weight: weights.regular,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Greeting on Home (Final-Home h1, 26px / 650, line 1.25). */
  greeting: {
    size: "26",
    lineHeight: 2.03125,
    weight: 650,
    letterSpacing: "-0.015em",
    family: "sans",
  },
  /** Title of an empty page (Final-MyWork `.emp` h2, 19px / 650, line 1.45). */
  "empty-title": {
    size: "19",
    lineHeight: 1.721875,
    weight: 650,
    letterSpacing: "0em",
    family: "sans",
  },
  /** Title of the My work page (Final-MyWork h1, 24px / 650, line 1.25). */
  "list-title": {
    size: "2xl",
    lineHeight: 1.875,
    weight: 650,
    letterSpacing: "-0.015em",
    family: "sans",
  },
  /** Title of the account pages (Final-Account h1, 22px / 650, line 1.25). */
  "account-title": {
    size: "22",
    lineHeight: 1.71875,
    weight: 650,
    letterSpacing: "-0.015em",
    family: "sans",
  },
  /** Product name beside the logo on the sign-in brand panel (Final-Auth `.mark b`, 21px / 700, line 1.5). */
  "brand-name": {
    size: "21",
    lineHeight: 1.96875,
    weight: weights.bold,
    letterSpacing: "-0.01em",
    family: "sans",
  },
  /** Promise on the sign-in brand panel (Final-Auth `.promise`, 32px / 650, line 1.28). */
  "brand-promise": {
    size: "4xl",
    lineHeight: 2.56,
    weight: 650,
    letterSpacing: "-0.02em",
    family: "sans",
  },
  /** Initials in a 30px person avatar (share dialog people). */
  "avatar-lg": {
    size: "10p5",
    lineHeight: 1,
    weight: weights.bold,
    letterSpacing: "0em",
    family: "sans",
  },
} as const satisfies Record<string, TextRole>;

export type TextRoleName = keyof typeof textRoles;

/** Be Vietnam Pro OpenType features applied to data: tabular numerals (WX-DEC-013). */
export const numericFeatures = {
  tabular: "tabular-nums",
} as const;
