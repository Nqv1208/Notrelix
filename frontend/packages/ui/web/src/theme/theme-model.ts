/**
 * Theme model — two independent axes (FE-UI-009/010, WX-DEC-005):
 *
 *   mode:   light | dark | system | high-contrast     (user preference)
 *   accent: blue | coral | ember | sage | amber        (user preference)
 *
 * `system` resolves to light/dark from `prefers-color-scheme`; the resolved
 * value is the *appearance* (light | dark | high-contrast) that the token CSS
 * implements. Persistence goes through an injected KeyValueStorage
 * (FE-UI-011); failures degrade to defaults (FE-UI-067).
 *
 * Pure module: no React, no DOM access.
 */

import {
  ACCENT_IDS,
  DEFAULT_ACCENT,
  type AccentId,
  type ThemeAppearance,
} from "@notrelix/ui-tokens";

export interface KeyValueStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem?(key: string): void;
  clear?(): void;
}

export const THEME_MODES = [
  "light",
  "dark",
  "system",
  "high-contrast",
] as const;
export type ThemeMode = (typeof THEME_MODES)[number];

export const THEME_ACCENTS = ACCENT_IDS;
export type ThemeAccent = AccentId;

export type EffectiveAppearance = ThemeAppearance;

export const DEFAULT_THEME_MODE: ThemeMode = "light";
export const DEFAULT_THEME_ACCENT: ThemeAccent = DEFAULT_ACCENT;

export const THEME_STORAGE_KEYS = {
  mode: "notrelix-theme-mode",
  accent: "notrelix-theme-accent",
} as const;

/**
 * Keys written by the previous theme runtime. `theme` was the web host's
 * mode key; `notrelix-ui-theme` the provider default; `notrelix-color-theme`
 * the seven-accent picker.
 */
export const LEGACY_THEME_STORAGE_KEYS = {
  mode: ["theme", "notrelix-ui-theme"],
  accent: ["notrelix-color-theme"],
} as const;

const LEGACY_MODES = ["light", "dark", "system"] as const;

/** Old accent theme id → nearest new accent. */
export const LEGACY_ACCENT_MIGRATION: Readonly<Record<string, ThemeAccent>> = {
  default: "blue",
  ocean: "blue",
  midnight: "blue",
  sage: "sage",
  aurora: "sage",
  editorial: "amber",
  sunset: "coral",
  rose: "ember",
};

export function isThemeMode(value: unknown): value is ThemeMode {
  return (
    typeof value === "string" &&
    (THEME_MODES as readonly string[]).includes(value)
  );
}

export function isThemeAccent(value: unknown): value is ThemeAccent {
  return (
    typeof value === "string" &&
    (THEME_ACCENTS as readonly string[]).includes(value)
  );
}

export function resolveAppearance(
  mode: ThemeMode,
  systemPrefersDark: boolean,
): EffectiveAppearance {
  if (mode === "system") return systemPrefersDark ? "dark" : "light";
  return mode;
}

export interface ThemePreference {
  readonly mode: ThemeMode;
  readonly accent: ThemeAccent;
}

export interface ReadThemePreferenceResult {
  readonly preference: ThemePreference;
  /** True when a value came from a legacy key and should be re-persisted. */
  readonly migrated: boolean;
}

function safeGet(storage: KeyValueStorage | undefined, key: string) {
  if (!storage) return null;
  try {
    return storage.getItem(key);
  } catch {
    return null;
  }
}

export function readThemePreference(
  storage: KeyValueStorage | undefined,
  defaults: ThemePreference = {
    mode: DEFAULT_THEME_MODE,
    accent: DEFAULT_THEME_ACCENT,
  },
): ReadThemePreferenceResult {
  let migrated = false;

  let mode: ThemeMode | undefined;
  const storedMode = safeGet(storage, THEME_STORAGE_KEYS.mode);
  if (isThemeMode(storedMode)) {
    mode = storedMode;
  } else {
    for (const key of LEGACY_THEME_STORAGE_KEYS.mode) {
      const legacy = safeGet(storage, key);
      if ((LEGACY_MODES as readonly (string | null)[]).includes(legacy)) {
        mode = legacy as ThemeMode;
        migrated = true;
        break;
      }
    }
  }

  let accent: ThemeAccent | undefined;
  const storedAccent = safeGet(storage, THEME_STORAGE_KEYS.accent);
  if (isThemeAccent(storedAccent)) {
    accent = storedAccent;
  } else {
    for (const key of LEGACY_THEME_STORAGE_KEYS.accent) {
      const legacy = safeGet(storage, key);
      const next = legacy ? LEGACY_ACCENT_MIGRATION[legacy] : undefined;
      if (next) {
        accent = next;
        migrated = true;
        break;
      }
    }
  }

  return {
    preference: {
      mode: mode ?? defaults.mode,
      accent: accent ?? defaults.accent,
    },
    migrated,
  };
}

/** Persists both axes under the current keys and drops legacy keys. */
export function writeThemePreference(
  storage: KeyValueStorage | undefined,
  preference: ThemePreference,
): void {
  if (!storage) return;
  try {
    storage.setItem(THEME_STORAGE_KEYS.mode, preference.mode);
    storage.setItem(THEME_STORAGE_KEYS.accent, preference.accent);
    for (const key of [
      ...LEGACY_THEME_STORAGE_KEYS.mode,
      ...LEGACY_THEME_STORAGE_KEYS.accent,
    ]) {
      storage.removeItem?.(key);
    }
  } catch {
    // Storage unavailable: the in-memory preference still applies.
  }
}

/** Root attributes the token CSS reads (see @notrelix/ui-tokens css/build-css). */
export interface ThemeRootState {
  readonly colorClass: "light" | "dark";
  readonly contrast: "high" | null;
  readonly accent: ThemeAccent;
}

export function themeRootState(
  appearance: EffectiveAppearance,
  accent: ThemeAccent,
): ThemeRootState {
  return {
    colorClass: appearance === "dark" ? "dark" : "light",
    contrast: appearance === "high-contrast" ? "high" : null,
    accent,
  };
}

/** Option metadata for pickers. The swatch is a preview, not styling authority (FE-UI-068). */
export const THEME_ACCENT_OPTIONS: readonly {
  readonly id: ThemeAccent;
  readonly swatch: string;
}[] = THEME_ACCENTS.map((id) => ({ id, swatch: `var(--accent-swatch-${id})` }));
