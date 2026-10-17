/**
 * @notrelix/ui-tokens — Light appearance, default accent, fully resolved.
 *
 * CSS custom property map (`--role: oklch(...)`) for consumers that need a
 * static snapshot. The web host uses the generated `@notrelix/ui-tokens/css`.
 * Framework-neutral: no React, no DOM.
 */

import { resolveThemeVariables } from "./resolve";

export const lightTheme = resolveThemeVariables("light", "blue");
