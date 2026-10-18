import { toCss } from "../color/oklch";
import type { AccentId, ThemeAppearance } from "../color/palette";
import { resolveSemanticColors } from "../color/theme";

/** `{ "--primary": "oklch(...)", ... }` for one appearance × accent. */
export function resolveThemeVariables(
  appearance: ThemeAppearance,
  accent: AccentId,
): Readonly<Record<string, string>> {
  const colors = resolveSemanticColors(appearance, accent);
  return Object.fromEntries(
    Object.keys(colors)
      .sort()
      .map((role) => [`--${role}`, toCss(colors[role]!)]),
  );
}
