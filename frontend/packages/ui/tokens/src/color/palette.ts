/**
 * @notrelix/ui-tokens — Primitive color palette (OKLCH).
 *
 * Strategy: Restrained. Neutrals carry the interface; one accent marks
 * interactive intent; status colors keep stable meaning; the logo's warm
 * colors are identity colors (Board/avatar marks, chart series) and are
 * never status colors (DESIGN.md §9, WX-DEC-003..006); workspace and
 * department (Space) chips use the mark tones (WX-DEC-014).
 *
 * Framework-neutral: no React, no DOM.
 */

import { fromHex, oklch, toCss, tone, type Oklch } from "./oklch";

/** Appearances that have a complete, independently tuned palette. */
export const THEME_APPEARANCES = ["light", "dark", "high-contrast"] as const;
export type ThemeAppearance = (typeof THEME_APPEARANCES)[number];

/** Neutrals are tinted very slightly toward the brand hue. */
export const NEUTRAL_HUE = 255;

// ─── Logo colors (source hex) ────────────────────────────────────────────

/** Exact logo colors from `packages/ui/web/src/assets/logo.svg`. */
export const logoHex = {
  blue: "#1E90FF",
  coral: "#FC744C",
  gold: "#F9C942",
  sage: "#8CADA1",
  ember: "#FE3154",
  red: "#FF1E56",
} as const;

/**
 * Brand gradient: the logo's red → coral → blue. Fixed on purpose (it carries
 * the Notrelix identity on sign-in, independent of the user's accent). Emitted
 * as `--brand-gradient`; use `bg-[image:var(--brand-gradient)]`.
 */
export const brandGradient = `linear-gradient(135deg, ${toCss(fromHex(logoHex.red))} 0%, ${toCss(fromHex(logoHex.coral))} 35%, ${toCss(fromHex(logoHex.blue))} 100%)`;

// ─── Neutrals ────────────────────────────────────────────────────────────

export interface NeutralRoles {
  /** Page background. */
  readonly canvas: Oklch;
  /** Second neutral layer (navigation sidebar). */
  readonly sidebar: Oklch;
  /** Inline content surface (cards, panels, tables). */
  readonly panel: Oklch;
  /** Floating surface (menus, popovers, dialogs). */
  readonly raised: Oklch;
  /** Subtle fill (secondary buttons, tracks, header bands). */
  readonly subtle: Oklch;
  /** Pointer hover / keyboard-highlight fill on canvas, panel and menus. */
  readonly hover: Oklch;
  /** Hover on the sidebar tier (one step stronger than `hover`). */
  readonly hoverStrong: Oklch;
  readonly border: Oklch;
  /** Form control boundary. */
  readonly input: Oklch;
  readonly text: Oklch;
  readonly textMuted: Oklch;
  /** Quieter than `textMuted`: meta lines, group labels, counts, hints. */
  readonly textMeta: Oklch;
  /** Hairline on a sidebar or panel (search field, chip, key). */
  readonly line: Oklch;
  /** Control outline one step stronger than `line` (keys, buttons). */
  readonly lineStrong: Oklch;
  /** Keyboard key chip fill. */
  readonly key: Oklch;
  readonly disabled: Oklch;
  readonly textDisabled: Oklch;
  /** Text placed on saturated fills (white-ish). */
  readonly onFill: Oklch;
  /** Dark text placed on light fills. */
  readonly onLightFill: Oklch;
}

const n = (l: number, c: number) => tone(l, c, NEUTRAL_HUE);

/*
 * Mockup neutrals (docs/design/mockups/2026-10, `.nx` light / `.nx.mode-dem`
 * dark) are authored on the CSS emit grid (L, C to 3 decimals) at the brand
 * hue so the emitted value renders the mockup hex exactly. Light muted text
 * (#4F5866) has no hue-255 fit, so it keeps its own hue. Subtle and hover
 * fills are the mockup `--hover`; the sidebar hover is the mockup `--hover2`
 * (light #EFF1F4 → #E6E9EE is ΔL 0.024; dark #13181E → #232A34 is ΔL 0.076).
 */
export const neutrals: Record<ThemeAppearance, NeutralRoles> = {
  light: {
    canvas: n(0.979, 0.003), // #F7F8FA
    sidebar: n(0.957, 0.005), // #EFF1F4
    panel: n(0.997, 0.002),
    raised: n(1, 0),
    subtle: n(0.964, 0.005), // #F1F3F6
    hover: n(0.964, 0.005), // #F1F3F6
    hoverStrong: n(0.933, 0.007), // #E6E9EE
    border: n(0.933, 0.007), // #E6E9EE
    input: n(0.87, 0.008),
    text: oklch(0.217, 0.015, 261.6), // #161A21
    textMuted: oklch(0.458, 0.026, 259.2), // #4F5866
    textMeta: oklch(0.514, 0.023, 255), // #5F6875
    line: oklch(0.919, 0.008, 255), // #E1E5EA
    lineStrong: oklch(0.877, 0.011, 255), // #D2D7DE
    key: oklch(0.954, 0.005, 255), // #EEF0F3
    disabled: n(0.94, 0.004),
    textDisabled: n(0.66, 0.006),
    onFill: n(0.99, 0.002),
    onLightFill: n(0.2, 0.01),
  },
  dark: {
    canvas: n(0.177, 0.011), // #0E1116
    sidebar: n(0.207, 0.014), // #13181E
    panel: n(0.216, 0.016), // #151A21
    raised: n(0.235, 0.009),
    subtle: n(0.253, 0.02), // #1C232C
    hover: n(0.253, 0.02), // #1C232C
    hoverStrong: n(0.283, 0.021), // #232A34
    border: n(0.286, 0.022), // #232B35
    input: n(0.36, 0.01),
    text: n(0.933, 0.007), // #E6E9EE
    textMuted: n(0.764, 0.021), // #AAB3C0
    textMeta: oklch(0.68, 0.023, 255), // #8F99A7
    line: oklch(0.284, 0.022, 255), // #232B35
    lineStrong: oklch(0.333, 0.024, 255), // #2E3743
    key: oklch(0.284, 0.022, 255), // #232B35
    disabled: n(0.24, 0.006),
    textDisabled: n(0.5, 0.008),
    onFill: n(0.99, 0.002),
    onLightFill: n(0.18, 0.01),
  },
  "high-contrast": {
    canvas: n(1, 0),
    sidebar: n(0.97, 0.004),
    panel: n(1, 0),
    raised: n(1, 0),
    subtle: n(0.95, 0.004),
    hover: n(0.92, 0.006),
    hoverStrong: n(0.92, 0.006),
    border: n(0.45, 0.01),
    input: n(0.4, 0.01),
    text: n(0.13, 0.01),
    textMuted: n(0.33, 0.01),
    textMeta: n(0.33, 0.01),
    line: n(0.45, 0.01),
    lineStrong: n(0.4, 0.01),
    key: n(0.92, 0.006),
    disabled: n(0.93, 0.004),
    textDisabled: n(0.5, 0.008),
    onFill: n(1, 0),
    onLightFill: n(0.13, 0.01),
  },
};

// ─── Accents ─────────────────────────────────────────────────────────────

export const ACCENT_IDS = ["blue", "coral", "ember", "sage", "amber"] as const;
export type AccentId = (typeof ACCENT_IDS)[number];
export const DEFAULT_ACCENT: AccentId = "blue";

/** One accent resolved for one appearance. */
export interface AccentTones {
  /** Marks only (logo-like glyphs, identity dots). ≥3:1 on surfaces. */
  readonly mark: Oklch;
  /** Filled interactive surface (primary button). */
  readonly solid: Oklch;
  /** Text/icon on `solid`. ≥4.5:1 (≥7:1 in high contrast). */
  readonly onSolid: Oklch;
  /** Links and accent-colored text. ≥4.5:1 on surfaces. */
  readonly ink: Oklch;
  /** Selected-state fill. */
  readonly subtle: Oklch;
  /** Text on `subtle`. */
  readonly onSubtle: Oklch;
  /** Focus ring. ≥3:1 on every surface. */
  readonly ring: Oklch;
}

export interface AccentDefinition {
  readonly id: AccentId;
  /** Logo/brief seed color the accent derives from. */
  readonly seedHex: string;
  /**
   * True when the light/high-contrast fill is a light color carrying dark
   * text (Amber). Such an accent's `primary` is fill-only: accent-colored
   * text must use `link` / `brand-ink`.
   */
  readonly lightFill: boolean;
  readonly tones: Record<ThemeAppearance, AccentTones>;
}

interface AccentRecipe {
  readonly seedHex: string;
  readonly hue: number;
  /** Target chroma for saturated tones; capped to sRGB per tone. */
  readonly chroma: number;
  /** Amber is light by nature: its fills carry dark text. */
  readonly lightFill?: boolean;
  /** Hue used for ink/marks when it should differ from the fill hue. */
  readonly inkHue?: number;
  /** Light-appearance mark lightness when the default fails 3:1 on a surface. */
  readonly lightMarkL?: number;
  /** Light-appearance tones pinned to exact mockup values. */
  readonly lightTones?: Partial<AccentTones>;
}

function buildAccent(id: AccentId, recipe: AccentRecipe): AccentDefinition {
  const { hue, chroma } = recipe;
  const inkHue = recipe.inkHue ?? hue;
  const light = neutrals.light;
  const dark = neutrals.dark;
  const hc = neutrals["high-contrast"];

  const lightSolid = recipe.lightFill
    ? tone(0.8, chroma, hue)
    : tone(0.53, chroma, hue);
  const hcSolid = recipe.lightFill
    ? tone(0.8, chroma, hue)
    : tone(0.42, chroma, hue);

  return {
    id,
    seedHex: recipe.seedHex,
    lightFill: recipe.lightFill ?? false,
    tones: {
      light: {
        mark: tone(
          recipe.lightMarkL ?? (recipe.lightFill ? 0.62 : 0.63),
          chroma,
          inkHue,
        ),
        solid: lightSolid,
        onSolid: recipe.lightFill ? light.onLightFill : light.onFill,
        ink: tone(0.45, chroma, inkHue),
        subtle: tone(0.95, 0.03, hue),
        onSubtle: tone(0.42, chroma, inkHue),
        ring: recipe.lightFill ? tone(0.55, chroma, inkHue) : lightSolid,
        ...recipe.lightTones,
      },
      dark: {
        mark: tone(0.72, chroma, hue),
        solid: tone(recipe.lightFill ? 0.8 : 0.72, chroma, hue),
        onSolid: dark.onLightFill,
        ink: tone(recipe.lightFill ? 0.8 : 0.72, chroma, hue),
        // Mockup `--sel` #172334 for Blue; every accent shares its L and C.
        subtle: tone(0.254, 0.036, hue),
        onSubtle: tone(recipe.lightFill ? 0.84 : 0.8, chroma, hue),
        ring: tone(recipe.lightFill ? 0.8 : 0.72, chroma, hue),
      },
      "high-contrast": {
        mark: tone(0.5, chroma, inkHue),
        solid: hcSolid,
        onSolid: recipe.lightFill ? hc.onLightFill : hc.onFill,
        ink: tone(0.4, chroma, inkHue),
        subtle: tone(0.93, 0.04, hue),
        onSubtle: tone(0.36, chroma, inkHue),
        ring: recipe.lightFill ? tone(0.42, chroma, inkHue) : hcSolid,
      },
    },
  };
}

const blueSeed = fromHex(logoHex.blue);

export const accents: Record<AccentId, AccentDefinition> = {
  /** Notrelix Blue — default accent, hue from the logo (#1E90FF). */
  blue: buildAccent("blue", {
    seedHex: logoHex.blue,
    hue: blueSeed.h,
    chroma: blueSeed.c,
    // Mockup light accent: `--accent` #1666CC, `--accent-soft` #E7F0FC,
    // `--accent-text` #0F56AE, `--accent-ink` white.
    lightTones: {
      solid: oklch(0.525, 0.175, 257.4),
      onSolid: neutrals.light.raised,
      ink: oklch(0.466, 0.155, 257.1),
      subtle: oklch(0.952, 0.019, 255.5),
      onSubtle: oklch(0.466, 0.155, 257.1),
      ring: oklch(0.525, 0.175, 257.4),
    },
  }),
  /** Coral — from #FC744C. */
  coral: buildAccent("coral", {
    seedHex: logoHex.coral,
    hue: 42,
    chroma: 0.17,
  }),
  /**
   * Ember — from #FE3154, rotated toward rose so it stays visibly distinct
   * from the danger red (hue 27): ΔE_OK ≥ 0.08 in every appearance.
   */
  ember: buildAccent("ember", {
    seedHex: logoHex.ember,
    hue: 358,
    chroma: 0.2,
  }),
  /**
   * Sage — from #8CADA1, deepened (more chroma, lower L) to pass contrast.
   * Light mark L 0.62 (#539581): 0.63 is 2.98:1 on the mockup sidebar #EFF1F4.
   */
  sage: buildAccent("sage", {
    seedHex: logoHex.sage,
    hue: 172,
    chroma: 0.075,
    lightMarkL: 0.62,
  }),
  /** Amber — from #F9C942; fills carry dark text, ink warms toward brown. */
  amber: buildAccent("amber", {
    seedHex: logoHex.gold,
    hue: 85,
    chroma: 0.155,
    lightFill: true,
    inkHue: 70,
  }),
};

// ─── Status ──────────────────────────────────────────────────────────────

export const STATUS_IDS = ["success", "warning", "danger", "info"] as const;
export type StatusId = (typeof STATUS_IDS)[number];

export interface StatusTones {
  /** Filled indicator / solid button. */
  readonly solid: Oklch;
  readonly onSolid: Oklch;
  /** Status-colored text and icons on surfaces. */
  readonly text: Oklch;
  /** Soft background (banners, badges). */
  readonly subtle: Oklch;
  readonly onSubtle: Oklch;
}

interface StatusRecipe {
  readonly hue: number;
  readonly chroma: number;
  readonly lightFill?: boolean;
  readonly textHue?: number;
}

function buildStatus(
  recipe: StatusRecipe,
): Record<ThemeAppearance, StatusTones> {
  const { hue, chroma } = recipe;
  const textHue = recipe.textHue ?? hue;
  return {
    light: {
      solid: tone(recipe.lightFill ? 0.8 : 0.48, chroma, hue),
      onSolid: recipe.lightFill
        ? neutrals.light.onLightFill
        : neutrals.light.onFill,
      text: tone(0.5, chroma, textHue),
      subtle: tone(0.955, 0.035, hue),
      onSubtle: tone(0.42, chroma, textHue),
    },
    dark: {
      solid: tone(recipe.lightFill ? 0.8 : 0.72, chroma, hue),
      onSolid: neutrals.dark.onLightFill,
      text: tone(recipe.lightFill ? 0.82 : 0.76, chroma, hue),
      subtle: tone(0.29, 0.05, hue),
      onSubtle: tone(recipe.lightFill ? 0.86 : 0.83, chroma, hue),
    },
    "high-contrast": {
      solid: tone(recipe.lightFill ? 0.8 : 0.38, chroma, hue),
      onSolid: recipe.lightFill
        ? neutrals["high-contrast"].onLightFill
        : neutrals["high-contrast"].onFill,
      text: tone(0.4, chroma, textHue),
      subtle: tone(0.93, 0.04, hue),
      onSubtle: tone(0.35, chroma, textHue),
    },
  };
}

export const statuses: Record<
  StatusId,
  Record<ThemeAppearance, StatusTones>
> = {
  success: buildStatus({ hue: 150, chroma: 0.14 }),
  warning: withTones(
    buildStatus({
      hue: 78,
      chroma: 0.16,
      lightFill: true,
      textHue: 62,
    }),
    {
      // Mockup `--warn-soft` #FFF4D6 with `--warn-text` #8A5A00.
      light: {
        subtle: oklch(0.968, 0.041, 90.3),
        onSubtle: oklch(0.508, 0.108, 73.3),
        text: oklch(0.508, 0.108, 73.3),
      },
    },
  ),
  danger: withTones(buildStatus({ hue: 27, chroma: 0.21 }), {
    // Mockup `--danger` #C42340 / #FF6B81 and `--danger-soft` #FDECEF.
    light: {
      subtle: oklch(0.957, 0.019, 5.3),
      onSubtle: oklch(0.535, 0.194, 18),
      text: oklch(0.535, 0.194, 18),
    },
    dark: { text: oklch(0.716, 0.18, 13.8) },
  }),
  info: buildStatus({ hue: 238, chroma: 0.13 }),
};

/** Replaces status tones in the named appearances (mockup values). */
function withTones(
  tones: Record<ThemeAppearance, StatusTones>,
  overrides: Partial<Record<ThemeAppearance, Partial<StatusTones>>>,
): Record<ThemeAppearance, StatusTones> {
  const result = { ...tones };
  for (const appearance of THEME_APPEARANCES) {
    const override = overrides[appearance];
    if (override) result[appearance] = { ...tones[appearance], ...override };
  }
  return result;
}

// ─── Work status ─────────────────────────────────────────────────────────

/**
 * Work-status roles: the four lifecycle buckets an item's status falls into.
 * Colour only; status always carries a label or icon as well (DESIGN.md §9,
 * §46). The labels are Work Management copy, not tokens.
 */
export const WORK_STATUS_IDS = ["todo", "doing", "review", "done"] as const;
export type WorkStatusId = (typeof WORK_STATUS_IDS)[number];

export interface WorkStatusTones {
  /** Dot / fill. ≥3:1 on panel (≥4.5:1 in high contrast). */
  readonly fill: Oklch;
  /** Status-coloured text. ≥4.5:1 on canvas, panel and sidebar (≥7:1 in high contrast). */
  readonly text: Oklch;
}

/*
 * Light and dark are the mockup values (`--st-*` dots, `--muted` /
 * `--accent-text` / `--warn-text` and the done green for text), authored on
 * the CSS emit grid so they render the mockup hex. Dark text equals the dark
 * fill. Contrast adjustment: light review fill #D99A00 is 2.43:1 on panel;
 * L lowered in 0.01 steps to 0.67 → #C18A12 (3.02:1).
 * High contrast is derived from the light hue/chroma by lowering L in 0.01
 * steps until fill ≥4.5:1 on panel and text ≥7:1 on canvas/panel/sidebar:
 *   todo   fill L 0.56 #6C7583 (4.65)  text L 0.44 #4A5361 (7.12)
 *   doing  fill L 0.57 #1676DB (4.52)  text L 0.44 #064FA4 (7.26)
 *   review fill L 0.57 #9B6E0C (4.53)  text L 0.44 #704905 (7.25)
 *   done   fill L 0.55 #278552 (4.58)  text L 0.43 #0B6035 (7.07)
 */
export const workStatuses: Record<
  WorkStatusId,
  Record<ThemeAppearance, WorkStatusTones>
> = {
  todo: {
    light: {
      fill: oklch(0.607, 0.024, 259.2), // #7A8391
      text: oklch(0.458, 0.026, 259.2), // #4F5866
    },
    dark: {
      fill: oklch(0.667, 0.023, 259.2), // #8C95A3
      text: oklch(0.667, 0.023, 259.2),
    },
    "high-contrast": {
      fill: oklch(0.56, 0.024, 259.2),
      text: oklch(0.44, 0.026, 259.2),
    },
  },
  doing: {
    light: {
      fill: oklch(0.586, 0.175, 254.7), // #1E7BE0
      text: oklch(0.466, 0.155, 257.1), // #0F56AE
    },
    dark: {
      fill: oklch(0.722, 0.148, 252.3), // #5AA9FF
      text: oklch(0.722, 0.148, 252.3),
    },
    "high-contrast": {
      fill: oklch(0.57, 0.175, 254.7),
      text: oklch(0.44, 0.153, 257.2),
    },
  },
  review: {
    light: {
      fill: oklch(0.67, 0.135, 79.3), // #C18A12 (mockup #D99A00, contrast)
      text: oklch(0.508, 0.108, 73.3), // #8A5A00
    },
    dark: {
      fill: oklch(0.834, 0.141, 85.4), // #F2C14E
      text: oklch(0.834, 0.141, 85.4),
    },
    "high-contrast": {
      fill: oklch(0.57, 0.115, 79.3),
      text: oklch(0.44, 0.091, 73.3),
    },
  },
  done: {
    light: {
      fill: oklch(0.569, 0.119, 154.9), // #2E8B57
      text: oklch(0.47, 0.102, 154.7), // #1D6B40
    },
    dark: {
      fill: oklch(0.753, 0.14, 156.5), // #56C98A
      text: oklch(0.753, 0.14, 156.5),
    },
    "high-contrast": {
      fill: oklch(0.55, 0.119, 154.9),
      text: oklch(0.43, 0.102, 154.7),
    },
  },
};

/**
 * Lane tints: the Board Kanban column background behind cards of one status
 * bucket. Cards (panel) must stay distinguishable on every lane (ΔL ≥ 0.02).
 */
export const workStatusLanes: Record<
  WorkStatusId,
  Record<ThemeAppearance, Oklch>
> = (() => {
  /*
   * Light and dark are the Final-Board `--lane-*` values on the CSS emit
   * grid. Contrast adjustment: dark todo #151B23 is ΔL 0.004 from the dark
   * panel #151A21; L raised in 0.01 steps from 0.22 to 0.24 → #1A2028
   * (ΔL 0.024, lighter than the panel like the other dark lanes). High
   * contrast reuses the light tints (ΔL ≥ 0.034 from its white panel).
   */
  const light = {
    todo: oklch(0.949, 0.006, 264.5), // #ECEEF2
    doing: oklch(0.949, 0.019, 255.5), // #E6EFFB
    review: oklch(0.966, 0.032, 87.3), // #FDF3DC
    done: oklch(0.951, 0.019, 157.9), // #E5F3EA
  };
  const dark = {
    todo: oklch(0.24, 0.018, 255.7), // #1A2028 (mockup #151B23, contrast)
    doing: oklch(0.247, 0.053, 263), // #13203A
    review: oklch(0.247, 0.027, 84), // #272012
    done: oklch(0.245, 0.031, 162.5), // #12251C
  };
  const lanes = {} as Record<WorkStatusId, Record<ThemeAppearance, Oklch>>;
  for (const id of WORK_STATUS_IDS) {
    lanes[id] = {
      light: light[id],
      dark: dark[id],
      "high-contrast": light[id],
    };
  }
  return lanes;
})();

// ─── Label tones ─────────────────────────────────────────────────────────

/**
 * Label tones: Board item label chips (Final-Board `.t-*`). Colour only; a
 * label always shows its name. The label names are Work Management data,
 * not tokens.
 */
export const LABEL_TONE_IDS = [
  "data",
  "security",
  "training",
  "report",
  "doc",
  "test",
] as const;
export type LabelToneId = (typeof LABEL_TONE_IDS)[number];

export interface LabelTone {
  readonly fill: Oklch;
  /** Chip text on `fill`. ≥4.5:1 (≥7:1 in high contrast). */
  readonly onFill: Oklch;
}

/*
 * Light and dark are the mockup chips on the CSS emit grid (every pair is
 * ≥ 6.3:1). High contrast keeps the light fills; text below 7:1 is lowered
 * in 0.01 L steps: security 0.455 → 0.435 #970C31 (7.28), training
 * 0.442 → 0.412 #235547 (7.20), report 0.448 → 0.428 #684901 (7.28).
 */
export const labelTones: Record<
  LabelToneId,
  Record<ThemeAppearance, LabelTone>
> = (() => {
  const light: Record<LabelToneId, LabelTone> = {
    data: {
      fill: oklch(0.944, 0.023, 250.1), // #E1EEFC
      onFill: oklch(0.419, 0.124, 255.1), // #124C8E
    },
    security: {
      fill: oklch(0.943, 0.027, 3.4), // #FDE5EA
      onFill: oklch(0.455, 0.167, 15.6), // #9E1736
    },
    training: {
      fill: oklch(0.942, 0.018, 170.1), // #E1F0EA
      onFill: oklch(0.442, 0.06, 171.6), // #2C5E4F
    },
    report: {
      fill: oklch(0.956, 0.04, 89.7), // #FBF0D3
      onFill: oklch(0.448, 0.088, 80.8), // #6E4F0A
    },
    doc: {
      fill: oklch(0.949, 0.006, 264.5), // #ECEEF2
      onFill: oklch(0.396, 0.03, 261.9), // #3E4757
    },
    test: {
      fill: oklch(0.939, 0.028, 301.8), // #EEE7FB
      onFill: oklch(0.432, 0.178, 290.6), // #5532A8
    },
  };
  const dark: Record<LabelToneId, LabelTone> = {
    data: {
      fill: oklch(0.306, 0.065, 254.1), // #16304F
      onFill: oklch(0.819, 0.085, 252), // #9CC8FA
    },
    security: {
      fill: oklch(0.295, 0.074, 7.3), // #4A1A26
      onFill: oklch(0.821, 0.104, 7.6), // #FFA8B8
    },
    training: {
      fill: oklch(0.319, 0.045, 172.2), // #173A30
      onFill: oklch(0.832, 0.064, 170.7), // #9ED6C2
    },
    report: {
      fill: oklch(0.316, 0.046, 86.4), // #3C3014
      onFill: oklch(0.881, 0.099, 89.1), // #F2D58A
    },
    doc: {
      fill: oklch(0.298, 0.023, 256.4), // #262E39
      onFill: oklch(0.839, 0.018, 256.3), // #C3CBD6
    },
    test: {
      fill: oklch(0.294, 0.079, 292.1), // #2E2350
      onFill: oklch(0.816, 0.096, 297), // #C9B6FA
    },
  };
  const highContrastText: Partial<Record<LabelToneId, Oklch>> = {
    security: oklch(0.435, 0.167, 15.6), // #970C31
    training: oklch(0.412, 0.06, 171.6), // #235547
    report: oklch(0.428, 0.088, 80.8), // #684901
  };
  const tones = {} as Record<LabelToneId, Record<ThemeAppearance, LabelTone>>;
  for (const id of LABEL_TONE_IDS) {
    tones[id] = {
      light: light[id],
      dark: dark[id],
      "high-contrast": {
        fill: light[id].fill,
        onFill: highContrastText[id] ?? light[id].onFill,
      },
    };
  }
  return tones;
})();

// ─── Priority ────────────────────────────────────────────────────────────

export interface PriorityTones {
  /** Icon / dot. ≥3:1 on panel (≥4.5:1 in high contrast). */
  readonly fill: Oklch;
  /** Priority-coloured text. ≥4.5:1 on canvas, panel and sidebar (≥7:1 in high contrast). */
  readonly text: Oklch;
}

/*
 * High priority: the Final-Board `--high` orange, distinct from the danger
 * red (ΔE_OK ≥ 0.08). Light #B4500B, dark #FFA060 for both fill and text.
 * High contrast keeps the light fill (5.12:1 on white); text lowered in
 * 0.01 L steps from 0.551 to 0.451, chroma capped to sRGB → #893B06 (7.78:1
 * on panel, 7.14:1 on sidebar).
 */
export const priorityHigh: Record<ThemeAppearance, PriorityTones> = {
  light: {
    fill: oklch(0.551, 0.147, 47.4), // #B4500B
    text: oklch(0.551, 0.147, 47.4),
  },
  dark: {
    fill: oklch(0.79, 0.138, 53.3), // #FFA060
    text: oklch(0.79, 0.138, 53.3),
  },
  "high-contrast": {
    fill: oklch(0.551, 0.147, 47.4),
    text: oklch(0.451, 0.12, 47.4), // #893B06, 7.14:1 on sidebar
  },
};

// ─── Identity ────────────────────────────────────────────────────────────

export const IDENTITY_IDS = ["blue", "coral", "gold", "sage", "ember"] as const;
export type IdentityId = (typeof IDENTITY_IDS)[number];

export interface IdentityTone {
  readonly fill: Oklch;
  /** Text (e.g. avatar initials) on `fill`. ≥4.5:1. */
  readonly onFill: Oklch;
}

/**
 * Identity colors: exact logo values. Used for logo marks, Board/Doc tiles,
 * avatars and chart series — never for status (DESIGN.md §9.3). Workspace
 * and department (Space) chips use the mark tones (WX-DEC-014).
 */
export const identity: Record<IdentityId, IdentityTone> = {
  blue: { fill: fromHex(logoHex.blue), onFill: neutrals.light.onLightFill },
  coral: { fill: fromHex(logoHex.coral), onFill: neutrals.light.onLightFill },
  gold: { fill: fromHex(logoHex.gold), onFill: neutrals.light.onLightFill },
  sage: { fill: fromHex(logoHex.sage), onFill: neutrals.light.onLightFill },
  ember: { fill: fromHex(logoHex.ember), onFill: neutrals.light.onLightFill },
};

// ─── Mark tones ──────────────────────────────────────────────────────────

/**
 * Mark tones: workspace and department (Space) initials chips (WX-DEC-014).
 * Identity colours stay for logo marks, Board/Doc tiles, avatars and charts.
 * Never status colours (WX-DEC-006).
 */
export const MARK_TONE_IDS = [
  "blue",
  "gold",
  "coral",
  "sage",
  "crimson",
  "violet",
] as const;
export type MarkToneId = (typeof MARK_TONE_IDS)[number];

export interface MarkTone {
  readonly fill: Oklch;
  /** White initials on `fill`. ≥4.5:1 in every appearance. */
  readonly onFill: Oklch;
}

const markInitials = oklch(1, 0, 0);

/*
 * The mockup department colours (CN, KT, MK, NS, GĐ, PC), one fill in every
 * appearance, authored on the CSS emit grid so they render the mockup hex.
 * Contrast adjustment: sage #4E8574 is 4.26:1 under white; L lowered in 0.01
 * steps from 0.573 to 0.553 → #487F6E (4.63:1).
 */
export const markTones: Record<MarkToneId, MarkTone> = {
  blue: { fill: oklch(0.549, 0.168, 256), onFill: markInitials }, // #1E6FD0 4.94:1
  gold: { fill: oklch(0.561, 0.112, 76), onFill: markInitials }, // #9A6A12 4.72:1
  coral: { fill: oklch(0.553, 0.174, 38.3), onFill: markInitials }, // #C2410C 5.19:1
  sage: { fill: oklch(0.553, 0.065, 172.1), onFill: markInitials }, // #487F6E 4.63:1
  crimson: { fill: oklch(0.534, 0.205, 17.2), onFill: markInitials }, // #C8143F 5.80:1
  violet: { fill: oklch(0.54, 0.184, 278.3), onFill: markInitials }, // #5B5BD6 5.37:1
};

// ─── System rail ─────────────────────────────────────────────────────────

export interface RailTones {
  /** The rail column; dark in every appearance. */
  readonly surface: Oklch;
  /** Labels and icons on the rail. */
  readonly ink: Oklch;
  /** Current workspace label and ring, hovered labels. */
  readonly activeInk: Oklch;
  /** Hover fill (translucent white over the rail). */
  readonly hover: Oklch;
  /** Separator line (translucent white over the rail). */
  readonly separator: Oklch;
  /** Dashed outline of the "add workspace" entry (translucent white). */
  readonly outline: Oklch;
  /** Dim over the layout while the rail overlays it. */
  readonly scrim: Oklch;
  /** Unread badge fill and its text. */
  readonly badge: Oklch;
  readonly onBadge: Oklch;
  /**
   * Keyboard focus ring on the rail: the app `focus-ring` is < 3:1 on the
   * dark rail in light and high contrast, so the rail scopes its own.
   */
  readonly focusRing: Oklch;
}

const railWhite = oklch(1, 0, 0);
const railShared = {
  activeInk: railWhite,
  hover: oklch(1, 0, 0, 0.08),
  separator: oklch(1, 0, 0, 0.14),
  outline: oklch(1, 0, 0, 0.35),
  badge: oklch(0.596, 0.23, 16.8), // #E8174B, white 4.52:1
  onBadge: railWhite,
  focusRing: railWhite, // ≥ 13:1 on every rail surface and its hover tint
} as const;

/*
 * The Notrelix system rail (Final-Space `.rail`), authored on the CSS emit
 * grid so it renders the mockup hex: light rail #16202E with ink #AFBACB
 * (8.37:1), dark rail #05070A with ink #8C97A8 (6.82:1), high contrast black
 * with white ink. The rail stays dark in every appearance.
 */
export const railTones: Record<ThemeAppearance, RailTones> = {
  light: {
    surface: oklch(0.241, 0.031, 256.9), // #16202E
    ink: oklch(0.786, 0.027, 259), // #AFBACB
    scrim: oklch(0.208, 0.04, 265.8, 0.14), // rgba(15,23,42,.14)
    ...railShared,
  },
  dark: {
    surface: oklch(0.127, 0.009, 253.7), // #05070A
    ink: oklch(0.673, 0.028, 259), // #8C97A8
    scrim: oklch(0, 0, 0, 0.42), // rgba(0,0,0,.42)
    ...railShared,
  },
  "high-contrast": {
    surface: oklch(0, 0, 0),
    ink: railWhite,
    scrim: oklch(0, 0, 0, 0.42),
    ...railShared,
  },
};

/** Chart series order (identity colors, never status). */
export const CHART_SERIES: readonly IdentityId[] = [
  "blue",
  "coral",
  "sage",
  "gold",
  "ember",
];

/** Brand gradient for the logo mark only (not interface surfaces). */
export const logoSpectrum = `linear-gradient(90deg, ${logoHex.red} 0%, ${logoHex.coral} 25%, ${logoHex.gold} 50%, ${logoHex.sage} 75%, ${logoHex.blue} 100%)`;
