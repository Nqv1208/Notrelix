/**
 * @notrelix/ui-tokens — Color barrel.
 *
 * OKLCH model, primitive palette (neutrals, accents, status, identity) and
 * semantic role resolution. See `./color/*` for the sources.
 *
 * Framework-neutral: no React, no DOM, no Tailwind dependency.
 * Consumed by ui/web (generated CSS variables) and ui/mobile (hex via toHex).
 */

export * from "./color/oklch";
export * from "./color/palette";
export * from "./color/theme";
