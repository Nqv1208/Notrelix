import * as React from "react";
import { applyThemeToElement } from "./apply-theme";
import {
  DEFAULT_THEME_ACCENT,
  DEFAULT_THEME_MODE,
  THEME_ACCENTS,
  THEME_MODES,
  readThemePreference,
  resolveAppearance,
  writeThemePreference,
  type EffectiveAppearance,
  type KeyValueStorage,
  type ThemeAccent,
  type ThemeMode,
} from "./theme-model";

export type { KeyValueStorage } from "./theme-model";

const DARK_QUERY = "(prefers-color-scheme: dark)";

type MatchMedia = (
  query: string,
) => Pick<
  MediaQueryList,
  "matches" | "addEventListener" | "removeEventListener"
>;

export interface ThemeProviderProps {
  children: React.ReactNode;
  /** Host-provided persistence (runtime KeyValueStorage). Omit for in-memory. */
  storage?: KeyValueStorage;
  defaultMode?: ThemeMode;
  defaultAccent?: ThemeAccent;
  /**
   * Element that receives the theme classes/attributes. Defaults to
   * `document.documentElement`; pass `null` to manage state without touching
   * the DOM (tests, nested previews).
   */
  target?: HTMLElement | null;
  /** Injected for tests; defaults to `window.matchMedia`. */
  matchMedia?: MatchMedia;
}

export interface ThemeContextValue {
  /** Stored preference (may be `system`). */
  readonly mode: ThemeMode;
  readonly accent: ThemeAccent;
  /** What is rendered: light | dark | high-contrast. */
  readonly appearance: EffectiveAppearance;
  readonly setMode: (mode: ThemeMode) => void;
  readonly setAccent: (accent: ThemeAccent) => void;
  readonly modes: readonly ThemeMode[];
  readonly accents: readonly ThemeAccent[];
}

const useIsomorphicLayoutEffect =
  typeof window === "undefined" ? React.useEffect : React.useLayoutEffect;

const ThemeContext = React.createContext<ThemeContextValue | null>(null);

function defaultMatchMedia(): MatchMedia | undefined {
  if (typeof window === "undefined" || !window.matchMedia) return undefined;
  return (query) => window.matchMedia(query);
}

function defaultTarget(): HTMLElement | null {
  return typeof document === "undefined" ? null : document.documentElement;
}

export function ThemeProvider({
  children,
  storage,
  defaultMode = DEFAULT_THEME_MODE,
  defaultAccent = DEFAULT_THEME_ACCENT,
  target,
  matchMedia,
}: ThemeProviderProps) {
  const initial = React.useMemo(
    () =>
      readThemePreference(storage, {
        mode: defaultMode,
        accent: defaultAccent,
      }),
    // Read once per storage instance; later changes go through the setters.
    [storage, defaultMode, defaultAccent],
  );

  const [mode, setModeState] = React.useState<ThemeMode>(
    initial.preference.mode,
  );
  const [accent, setAccentState] = React.useState<ThemeAccent>(
    initial.preference.accent,
  );

  const media = React.useMemo(
    () => (matchMedia ?? defaultMatchMedia())?.(DARK_QUERY),
    [matchMedia],
  );
  const [systemPrefersDark, setSystemPrefersDark] = React.useState(
    () => media?.matches ?? false,
  );

  // System sync: follow OS changes only while mode is `system` (FE-UI-012).
  React.useEffect(() => {
    if (!media || mode !== "system") return;
    setSystemPrefersDark(media.matches);
    const onChange = (event: MediaQueryListEvent) =>
      setSystemPrefersDark(event.matches);
    media.addEventListener("change", onChange);
    return () => media.removeEventListener("change", onChange);
  }, [media, mode]);

  // Re-persist preferences that were read from legacy keys.
  React.useEffect(() => {
    if (initial.migrated) writeThemePreference(storage, initial.preference);
  }, [initial, storage]);

  const appearance = resolveAppearance(mode, systemPrefersDark);
  const element = target === undefined ? defaultTarget() : target;

  // Layout effect: apply before paint on every change (the pre-paint script
  // already applied the stored state, so the first run is a no-op visually).
  useIsomorphicLayoutEffect(() => {
    if (element) applyThemeToElement(element, appearance, accent);
  }, [element, appearance, accent]);

  const setMode = React.useCallback(
    (next: ThemeMode) => {
      setModeState(next);
      writeThemePreference(storage, { mode: next, accent });
    },
    [storage, accent],
  );

  const setAccent = React.useCallback(
    (next: ThemeAccent) => {
      setAccentState(next);
      writeThemePreference(storage, { mode, accent: next });
    },
    [storage, mode],
  );

  const value = React.useMemo<ThemeContextValue>(
    () => ({
      mode,
      accent,
      appearance,
      setMode,
      setAccent,
      modes: THEME_MODES,
      accents: THEME_ACCENTS,
    }),
    [mode, accent, appearance, setMode, setAccent],
  );

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
}

/**
 * Theme state and setters. Throws outside a ThemeProvider so missing
 * composition is visible (FE-UI-066).
 */
export function useTheme(): ThemeContextValue {
  const context = React.useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used inside <ThemeProvider>.");
  }
  return context;
}

/**
 * Intentional fallback for primitives that render outside a provider
 * (e.g. a standalone Toaster in Storybook): returns `null` instead of throwing.
 */
export function useOptionalTheme(): ThemeContextValue | null {
  return React.useContext(ThemeContext);
}
