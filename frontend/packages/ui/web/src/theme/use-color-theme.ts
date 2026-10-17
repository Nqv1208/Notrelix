import { useTheme } from "./theme-provider";
import { THEME_ACCENT_OPTIONS, type ThemeAccent } from "./theme-model";

/**
 * Accent axis of the theme (the "color theme"). Thin view over
 * {@link useTheme}; mode and accent stay independent (FE-UI-009).
 */
export function useColorTheme(): {
  readonly accent: ThemeAccent;
  readonly setAccent: (accent: ThemeAccent) => void;
  readonly options: typeof THEME_ACCENT_OPTIONS;
} {
  const { accent, setAccent } = useTheme();
  return { accent, setAccent, options: THEME_ACCENT_OPTIONS };
}
