export {
  ThemeProvider,
  useTheme,
  useOptionalTheme,
  type ThemeContextValue,
  type ThemeProviderProps,
} from "./theme-provider";
export { useColorTheme } from "./use-color-theme";
export {
  ThemePicker,
  type ThemePickerLabels,
  type ThemePickerProps,
} from "./theme-picker";
export { applyThemeToElement } from "./apply-theme";
// The pre-paint script reads browser storage directly, so it is not part of
// the pure UI graph: import it from "@notrelix/ui-web/theme-init-script".
export {
  THEME_MODES,
  THEME_ACCENTS,
  THEME_ACCENT_OPTIONS,
  THEME_STORAGE_KEYS,
  LEGACY_THEME_STORAGE_KEYS,
  LEGACY_ACCENT_MIGRATION,
  DEFAULT_THEME_MODE,
  DEFAULT_THEME_ACCENT,
  isThemeMode,
  isThemeAccent,
  resolveAppearance,
  readThemePreference,
  writeThemePreference,
  themeRootState,
  type KeyValueStorage,
  type ThemeMode,
  type ThemeAccent,
  type EffectiveAppearance,
  type ThemePreference,
} from "./theme-model";
