/**
 * @notrelix/ui-tokens — Primitive color palette (OKLCH).
 *
 * Strategy: Restrained. Neutrals carry the interface; one accent marks
 * interactive intent; status colors keep stable meaning; the logo's warm
 * colors are identity colors (workspace/Board/avatar marks, chart series)
 * and are never status colors (DESIGN.md §9, WX-DEC-003..006).
 *
 * Framework-neutral: no React, no DOM.
 */

import { fromHex, tone, type Oklch } from "./oklch";

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
  /** Pointer hover / keyboard-highlight fill. */
  readonly hover: Oklch;
  readonly border: Oklch;
  /** Form control boundary. */
  readonly input: Oklch;
  readonly text: Oklch;
  readonly textMuted: Oklch;
  readonly disabled: Oklch;
  readonly textDisabled: Oklch;
  /** Text placed on saturated fills (white-ish). */
  readonly onFill: Oklch;
  /** Dark text placed on light fills. */
  readonly onLightFill: Oklch;
}

const n = (l: number, c: number) => tone(l, c, NEUTRAL_HUE);

export const neutrals: Record<ThemeAppearance, NeutralRoles> = {
  light: {
    canvas: n(0.985, 0.003),
    sidebar: n(0.965, 0.006),
    panel: n(0.997, 0.002),
    raised: n(1, 0),
    subtle: n(0.955, 0.006),
    hover: n(0.945, 0.006),
    border: n(0.91, 0.006),
    input: n(0.87, 0.008),
    text: n(0.22, 0.008),
    textMuted: n(0.49, 0.01),
    disabled: n(0.94, 0.004),
    textDisabled: n(0.66, 0.006),
    onFill: n(0.99, 0.002),
    onLightFill: n(0.2, 0.01),
  },
  dark: {
    canvas: n(0.18, 0.008),
    sidebar: n(0.155, 0.008),
    panel: n(0.205, 0.008),
    raised: n(0.235, 0.009),
    subtle: n(0.25, 0.009),
    hover: n(0.27, 0.01),
    border: n(0.3, 0.01),
    input: n(0.36, 0.01),
    text: n(0.95, 0.004),
    textMuted: n(0.73, 0.01),
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
    border: n(0.45, 0.01),
    input: n(0.4, 0.01),
    text: n(0.13, 0.01),
    textMuted: n(0.33, 0.01),
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
        mark: tone(recipe.lightFill ? 0.62 : 0.63, chroma, inkHue),
        solid: lightSolid,
        onSolid: recipe.lightFill ? light.onLightFill : light.onFill,
        ink: tone(0.45, chroma, inkHue),
        subtle: tone(0.95, 0.03, hue),
        onSubtle: tone(0.42, chroma, inkHue),
        ring: recipe.lightFill ? tone(0.55, chroma, inkHue) : lightSolid,
      },
      dark: {
        mark: tone(0.72, chroma, hue),
        solid: tone(recipe.lightFill ? 0.8 : 0.72, chroma, hue),
        onSolid: dark.onLightFill,
        ink: tone(recipe.lightFill ? 0.8 : 0.72, chroma, hue),
        subtle: tone(0.3, 0.05, hue),
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
  /** Sage — from #8CADA1, deepened (more chroma, lower L) to pass contrast. */
  sage: buildAccent("sage", {
    seedHex: logoHex.sage,
    hue: 172,
    chroma: 0.075,
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
  warning: buildStatus({
    hue: 78,
    chroma: 0.16,
    lightFill: true,
    textHue: 62,
  }),
  danger: buildStatus({ hue: 27, chroma: 0.21 }),
  info: buildStatus({ hue: 238, chroma: 0.13 }),
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
 * Identity colors: exact logo values. Used for workspace/Board/avatar marks
 * and chart series — never for status (DESIGN.md §9.3).
 */
export const identity: Record<IdentityId, IdentityTone> = {
  blue: { fill: fromHex(logoHex.blue), onFill: neutrals.light.onLightFill },
  coral: { fill: fromHex(logoHex.coral), onFill: neutrals.light.onLightFill },
  gold: { fill: fromHex(logoHex.gold), onFill: neutrals.light.onLightFill },
  sage: { fill: fromHex(logoHex.sage), onFill: neutrals.light.onLightFill },
  ember: { fill: fromHex(logoHex.ember), onFill: neutrals.light.onLightFill },
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
