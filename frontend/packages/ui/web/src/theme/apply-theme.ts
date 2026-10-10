import {
  themeRootState,
  type EffectiveAppearance,
  type ThemeAccent,
} from "./theme-model";

/**
 * Applies appearance + accent to a themed element (normally <html>). Mode
 * and accent always land on the same element; the token CSS resolves
 * accent-dependent roles against that element (see @notrelix/ui-tokens css).
 */
export function applyThemeToElement(
  element: HTMLElement,
  appearance: EffectiveAppearance,
  accent: ThemeAccent,
): void {
  const state = themeRootState(appearance, accent);
  const legacyAccentClasses = Array.from(element.classList).filter((name) =>
    name.startsWith("theme-"),
  );
  element.classList.remove("light", "dark", ...legacyAccentClasses);
  element.classList.add(state.colorClass);
  if (state.contrast) element.setAttribute("data-contrast", state.contrast);
  else element.removeAttribute("data-contrast");
  element.setAttribute("data-accent", state.accent);
  element.style.colorScheme = state.colorClass;
}
