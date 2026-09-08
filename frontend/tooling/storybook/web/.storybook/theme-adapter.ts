import { isUiEvidenceThemeId } from "../../../testing/src/ui-visual-registry";

/**
 * Application-semantic theme adapter (FUIR-WU-051).
 *
 * Applies the same root class/data contract the Web application's
 * ThemeProvider uses (`document.documentElement` classList light/dark) so
 * dark/light screenshots reflect real Web theme token semantics, not a
 * background swap. The theme is selected deterministically from the
 * `fui-theme` query param, which the visual runner derives from manifest
 * visualTargets.
 */
export function applyFuiTheme(search: string, root: HTMLElement): void {
  const theme = new URLSearchParams(search).get("fui-theme");
  if (!theme || !isUiEvidenceThemeId(theme)) return;

  root.classList.remove("light", "dark");
  root.classList.add(theme);
  root.dataset.fuiTheme = theme;
}

export function applyStorybookThemeFromLocation(root: HTMLElement): void {
  if (typeof window === "undefined") return;
  applyFuiTheme(window.location.search, root);
}
