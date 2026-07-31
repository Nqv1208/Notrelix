/**
 * @notrelix/ui-tokens — Color primitives, brand, semantic, and surface palette.
 *
 * Framework-neutral: no React, no DOM, no Tailwind dependency.
 * Consumed by ui/web (Tailwind theme) and ui/mobile (StyleSheet).
 */

// ─── Primitive Palette ───

export const primitive = {
  deepSpace: '#02093a',
  voidBlack: '#000000',
  paper: '#ffffff',
  fog: '#f5f6f8',
  mist: '#f6f5f4',
  graphite: '#333333',
  slate: '#676879',
  iron: '#535768',
  silver: '#d0d4e4',
  ash: '#c6c6c5',
} as const;

// ─── Brand & Interactive ───

export const brand = {
  violet: '#6161ff',
  indigo: '#455dd3',
  purple: '#9450fd',
  ocean: '#0075de',
  sky: '#3ac9ff',
  frost: '#62aef0',
} as const;

// ─── Semantic States ───

export const semantic = {
  success: '#1aae39',
  warning: '#ffb110',
  danger: '#f64932',
  info: '#097fe8',
} as const;

// ─── Content Surface Palette ───

export const surface = {
  mint: '#bcfe90',
  lavender: '#eddff7',
  sky: '#abf0ff',
  sunset: '#ff8940',
  paleBlue: '#e7ecff',
  ocean: '#93beff',
  ice: '#d1faff',
  fuchsia: '#ff83dd',
  gold: '#ffc95e',
  teal: '#2a9d99',
  coral: '#ff8a33',
  grape: '#ad6ded',
} as const;

// ─── Gradients ───

export const gradients = {
  brandSweep: 'linear-gradient(135deg, #6161ff 0%, #9450fd 100%)',
  vibrantFlow:
    'linear-gradient(90deg, #fe81e4 0%, #fe81e4 31%, #fda900 88%)',
  depthFade: 'linear-gradient(180deg, #02093a 0%, #000000 100%)',
  glassLight:
    'linear-gradient(135deg, rgba(255,255,255,0.15), rgba(255,255,255,0.05))',
  spectrumRing:
    'conic-gradient(from 270deg, #8181ff 15%, #33dbdb 40%, #33d58e 55%, #ffd633 65%, #fc527d 85%, #8181ff 100%)',
} as const;

// ─── Badge Semantic Colors ───

export const badge = {
  default: { bg: '#f1f5f9', text: '#475569' },
  done: { bg: '#d1fae5', text: '#065f46' },
  working: { bg: '#fef3c7', text: '#92400e' },
  stuck: { bg: '#fee2e2', text: '#991b1b' },
  urgent: { bg: '#fee2e2', text: '#991b1b' },
  high: { bg: '#dbeafe', text: '#1e40af' },
  medium: { bg: '#ede9fe', text: '#6d28d9' },
  low: { bg: '#f1f5f9', text: '#475569' },
} as const;

export const badgeDark = {
  default: { bg: '#2D3748', text: '#CBD5E1' },
  done: { bg: '#254A3A', text: '#9BE7C0' },
  working: { bg: '#4A3F24', text: '#FFD98A' },
  stuck: { bg: '#563036', text: '#FF9AA8' },
  urgent: { bg: '#5A3038', text: '#FFB4C0' },
  high: { bg: '#3A456A', text: '#AFC4FF' },
  medium: { bg: '#3B3A66', text: '#C7C3FF' },
  low: { bg: '#2D3748', text: '#CBD5E1' },
} as const;
