/**
 * @notrelix/ui-tokens — Spacing and layout tokens.
 *
 * Framework-neutral: no React, no DOM.
 */

// ─── Base Unit ───

export const baseUnit = 8;

// ─── Spacing Scale ───

export const spacing = {
  4: '4px',
  8: '8px',
  12: '12px',
  16: '16px',
  24: '24px',
  32: '32px',
  40: '40px',
  48: '48px',
  64: '64px',
  80: '80px',
  96: '96px',
} as const;

// ─── Layout ───

export const layout = {
  sectionGap: '48px',
  cardPadding: '24px',
  elementGap: '8px',
  sidebarWidth: 240,
  sidebarCollapsedWidth: 56,
  contentMaxWidth: 720,
  pageMaxWidth: 1280,
  topBarHeight: 56,
} as const;

// ─── Grid ───

export const grid = {
  desktop: { columns: 12, gutter: '24px', maxWidth: 1280 },
  tablet: { columns: 8, gutter: '16px', maxWidth: 960 },
  mobile: { columns: 4, gutter: '16px', maxWidth: '100%' },
} as const;
