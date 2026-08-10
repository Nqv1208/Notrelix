/**
 * @notrelix/ui-tokens — Typography tokens.
 *
 * Framework-neutral: no React, no DOM.
 */

// ─── Font Families ───

export const fonts = {
  display: "'Poppins', ui-sans-serif, system-ui, sans-serif",
  body: "'Inter', ui-sans-serif, system-ui, sans-serif",
  editorial: "'Lyon Text', Georgia, 'Times New Roman', serif",
  mono: "'JetBrains Mono', 'Courier New', monospace",
} as const;

// ─── Font Weights ───

export const weights = {
  regular: 400,
  medium: 500,
  semibold: 600,
  bold: 700,
} as const;

// ─── Type Scale ───

export interface TypeToken {
  size: string;
  lineHeight: number;
  letterSpacing: string;
  fontFamily: string;
  fontWeight: number;
}

function typeToken(
  size: string,
  lineHeight: number,
  letterSpacing: string,
  fontFamily: string,
  fontWeight: number,
): TypeToken {
  return { size, lineHeight, letterSpacing, fontFamily, fontWeight };
}

export const typeScale = {
  caption: typeToken("12px", 1.5, "0.01em", fonts.body, weights.regular),
  label: typeToken("13px", 1.4, "0.01em", fonts.display, weights.medium),
  bodySm: typeToken("14px", 1.5, "-0.006em", fonts.body, weights.regular),
  body: typeToken("16px", 1.6, "-0.006em", fonts.body, weights.regular),
  bodyLg: typeToken("18px", 1.5, "-0.011em", fonts.body, weights.regular),
  subheading: typeToken(
    "20px",
    1.35,
    "-0.011em",
    fonts.display,
    weights.medium,
  ),
  headingSm: typeToken(
    "24px",
    1.3,
    "-0.015em",
    fonts.display,
    weights.semibold,
  ),
  heading: typeToken("32px", 1.3, "-0.02em", fonts.display, weights.bold),
  headingLg: typeToken("40px", 1.2, "-0.02em", fonts.display, weights.bold),
  displaySm: typeToken("52px", 1.15, "-0.03em", fonts.display, weights.bold),
  display: typeToken("64px", 1.1, "-0.04em", fonts.display, weights.bold),
  editorial: typeToken(
    "32px",
    1.25,
    "normal",
    fonts.editorial,
    weights.regular,
  ),
} as const;
