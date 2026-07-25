/**
 * @notrelix/ui-tokens — Semantic design tokens.
 *
 * Maps primitive colors to their semantic roles.
 * Framework-neutral: no React, no DOM.
 */

import { primitive, brand } from './colors';

export const surfaces = {
  underlay: { light: '#f0f1f4', dark: '#000000' },
  canvas: { light: '#ffffff', dark: '#02093a' },
  raised: { light: '#f5f6f8', dark: 'rgba(255,255,255,0.04)' },
  card: { light: '#ffffff', dark: 'rgba(255,255,255,0.08)' },
  overlay: { light: '#ffffff', dark: '#02093a' },
  toast: { light: '#1a1a2e', dark: '#ffffff' },
} as const;

export const tableSurface = {
  bg: primitive.paper,
  row: primitive.paper,
  rowHover: primitive.fog,
  header: primitive.fog,
  group: primitive.fog,
} as const;

export const focusRing = {
  color: brand.violet,
  width: '3px',
  offset: '0px',
  opacity: 0.15,
} as const;
