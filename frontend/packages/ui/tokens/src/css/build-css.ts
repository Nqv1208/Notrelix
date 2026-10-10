/**
 * @notrelix/ui-tokens — CSS custom-property emitter.
 *
 * Produces `src/css/index.css` (the `@notrelix/ui-tokens/css` export) from the
 * TypeScript token source. The file is generated; regenerate with
 * `pnpm --filter @notrelix/ui-tokens generate:css`. A unit test fails when the
 * committed file drifts from this output.
 *
 * Theming contract (applied by the ui-web theme runtime on one element,
 * normally <html>):
 *   class="light" | class="dark"     appearance
 *   data-contrast="high"             high-contrast appearance (with class="light")
 *   data-accent="<accent id>"        accent (absent = blue)
 *
 * Mode and accent must sit on the same element: accent-dependent roles are
 * declared in the appearance block and resolve against that element's
 * `--accent-tone-*` values.
 *
 * Framework-neutral: plain CSS, no Tailwind syntax.
 */

import { toCss } from "../color/oklch";
import {
  ACCENT_IDS,
  DEFAULT_ACCENT,
  THEME_APPEARANCES,
  accents,
  type AccentId,
  type AccentTones,
  type ThemeAppearance,
} from "../color/palette";
import { ACCENT_ROLES, buildAppearanceColors } from "../color/theme";
import { durationMs, easing, reducedMotionDuration } from "../motion";
import { radius, radiusBase, radiusRoles } from "../radius";
import { focusRing } from "../semantic";
import { elevation, ELEVATION_ROLES } from "../shadows";
import { layout, sizes, space } from "../spacing";
import { fontSizeScale, fonts, textRoles } from "../typography";
import { zIndex } from "../z-index";

const TONE_KEYS: readonly (keyof AccentTones)[] = [
  "mark",
  "solid",
  "onSolid",
  "ink",
  "subtle",
  "onSubtle",
  "ring",
];

const kebab = (value: string) =>
  value.replace(/[A-Z]/g, (char) => `-${char.toLowerCase()}`);

function toneVar(appearance: ThemeAppearance, key: keyof AccentTones): string {
  return `--accent-tone-${appearance}-${kebab(key)}`;
}

function block(selector: string, declarations: readonly string[]): string {
  return `${selector} {\n${declarations.map((d) => `  ${d};`).join("\n")}\n}`;
}

function staticDeclarations(): string[] {
  const out: string[] = [];

  out.push(`--font-family-sans: ${fonts.sans}`);
  out.push(`--font-family-mono: ${fonts.mono}`);
  for (const [step, rem] of Object.entries(fontSizeScale)) {
    out.push(`--type-scale-${step}: ${rem}rem`);
  }
  for (const [role, spec] of Object.entries(textRoles)) {
    out.push(`--type-${role}-size: var(--type-scale-${spec.size})`);
    out.push(`--type-${role}-line-height: ${spec.lineHeight}rem`);
    out.push(`--type-${role}-weight: ${spec.weight}`);
    out.push(`--type-${role}-tracking: ${spec.letterSpacing}`);
    out.push(`--type-${role}-family: var(--font-family-${spec.family})`);
  }

  for (const [role, value] of Object.entries(space)) {
    out.push(`--space-${role}: ${value}`);
  }
  for (const [role, value] of Object.entries(sizes)) {
    out.push(`--size-${role}: ${value}`);
  }
  for (const [role, value] of Object.entries(layout)) {
    out.push(`--layout-${role}: ${value}`);
  }

  out.push(`--radius: ${radiusBase}`);
  for (const [step, value] of Object.entries(radius)) {
    out.push(`--corner-${step}: ${value}`);
  }
  for (const [role, value] of Object.entries(radiusRoles)) {
    out.push(`--corner-${role}: ${value}`);
  }

  out.push(`--focus-ring-width: ${focusRing.width}`);
  out.push(`--focus-ring-offset: ${focusRing.offset}`);

  for (const [role, value] of Object.entries(zIndex)) {
    out.push(`--z-${role}: ${value}`);
  }

  for (const [role, ms] of Object.entries(durationMs)) {
    out.push(`--motion-duration-${role}: ${ms}ms`);
  }
  for (const [role, curve] of Object.entries(easing)) {
    out.push(`--motion-ease-${role}: ${curve}`);
  }

  for (const accent of ACCENT_IDS) {
    out.push(
      `--accent-swatch-${accent}: ${toCss(accents[accent].tones.light.solid)}`,
    );
  }

  return out;
}

function accentDeclarations(accent: AccentId): string[] {
  const out: string[] = [];
  for (const appearance of THEME_APPEARANCES) {
    const tones = accents[accent].tones[appearance];
    for (const key of TONE_KEYS) {
      out.push(`${toneVar(appearance, key)}: ${toCss(tones[key])}`);
    }
  }
  return out;
}

function appearanceDeclarations(appearance: ThemeAppearance): string[] {
  const out: string[] = [
    `color-scheme: ${appearance === "dark" ? "dark" : "light"}`,
  ];
  const colors = buildAppearanceColors(appearance);
  for (const role of Object.keys(colors).sort()) {
    out.push(`--${role}: ${toCss(colors[role]!)}`);
  }
  for (const role of Object.keys(ACCENT_ROLES).sort()) {
    const key = ACCENT_ROLES[role as keyof typeof ACCENT_ROLES];
    out.push(`--${role}: var(${toneVar(appearance, key)})`);
  }
  for (const role of ELEVATION_ROLES) {
    out.push(`--elevation-${role}: ${elevation[appearance][role]}`);
  }
  return out;
}

const APPEARANCE_SELECTORS: Record<ThemeAppearance, string> = {
  light: ":root,\n.light",
  dark: ".dark",
  "high-contrast": '[data-contrast="high"]',
};

export const GENERATED_CSS_HEADER = `/*
 * GENERATED FILE — do not edit by hand.
 * Producer: packages/ui/tokens/src/css/build-css.ts
 * Regenerate: pnpm --filter @notrelix/ui-tokens generate:css
 */`;

export function buildTokensCss(): string {
  const sections: string[] = [GENERATED_CSS_HEADER];

  sections.push("/* Static tokens: type, space, shape, stacking, motion. */");
  sections.push(block(":root", staticDeclarations()));

  const reduced = Object.keys(durationMs)
    .map((role) => `    --motion-duration-${role}: ${reducedMotionDuration};`)
    .join("\n");
  sections.push(
    `@media (prefers-reduced-motion: reduce) {\n  :root {\n${reduced}\n  }\n}`,
  );

  sections.push("/* Accent tones (accent axis). */");
  for (const accent of ACCENT_IDS) {
    const selector =
      accent === DEFAULT_ACCENT
        ? `:root,\n[data-accent="${accent}"]`
        : `[data-accent="${accent}"]`;
    sections.push(block(selector, accentDeclarations(accent)));
  }

  sections.push("/* Appearance roles (mode axis). */");
  for (const appearance of THEME_APPEARANCES) {
    sections.push(
      block(
        APPEARANCE_SELECTORS[appearance],
        appearanceDeclarations(appearance),
      ),
    );
  }

  return `${sections.join("\n\n")}\n`;
}
