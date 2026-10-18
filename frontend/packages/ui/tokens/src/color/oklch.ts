/**
 * @notrelix/ui-tokens — OKLCH color model and sRGB conversion.
 *
 * Framework-neutral math used to author tokens, emit CSS, derive sRGB/hex
 * values for native hosts, and verify contrast. No React, no DOM.
 *
 * Conversion follows Björn Ottosson's OKLab definition and the sRGB transfer
 * function from IEC 61966-2-1. Contrast follows WCAG 2.x relative luminance.
 */

export interface Oklch {
  /** Perceptual lightness, 0–1. */
  readonly l: number;
  /** Chroma, 0–~0.37 inside sRGB. */
  readonly c: number;
  /** Hue angle in degrees, 0–360. */
  readonly h: number;
  /** Optional alpha, 0–1. */
  readonly alpha?: number;
}

export type RgbTriplet = readonly [number, number, number];

const GAMUT_EPSILON = 1e-4;

export function oklch(l: number, c: number, h: number, alpha?: number): Oklch {
  return alpha === undefined ? { l, c, h } : { l, c, h, alpha };
}

function round(value: number, digits: number): number {
  const factor = 10 ** digits;
  return Math.round(value * factor) / factor;
}

/** Serializes to a CSS `oklch()` value. */
export function toCss(color: Oklch): string {
  const base = `${round(color.l, 3)} ${round(color.c, 3)} ${round(color.h, 1)}`;
  return color.alpha === undefined || color.alpha === 1
    ? `oklch(${base})`
    : `oklch(${base} / ${round(color.alpha, 3)})`;
}

export function oklchToLinearSrgb(color: Oklch): RgbTriplet {
  const hue = (color.h * Math.PI) / 180;
  const a = color.c * Math.cos(hue);
  const b = color.c * Math.sin(hue);

  const l_ = color.l + 0.3963377774 * a + 0.2158037573 * b;
  const m_ = color.l - 0.1055613458 * a - 0.0638541728 * b;
  const s_ = color.l - 0.0894841775 * a - 1.291485548 * b;

  const l = l_ ** 3;
  const m = m_ ** 3;
  const s = s_ ** 3;

  return [
    4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
    -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s,
  ];
}

export function isInSrgbGamut(color: Oklch): boolean {
  return oklchToLinearSrgb(color).every(
    (channel) => channel >= -GAMUT_EPSILON && channel <= 1 + GAMUT_EPSILON,
  );
}

function clamp01(value: number): number {
  return Math.min(1, Math.max(0, value));
}

function encodeSrgb(linear: number): number {
  const v = clamp01(linear);
  return v <= 0.0031308 ? 12.92 * v : 1.055 * v ** (1 / 2.4) - 0.055;
}

function decodeSrgb(encoded: number): number {
  return encoded <= 0.04045
    ? encoded / 12.92
    : ((encoded + 0.055) / 1.055) ** 2.4;
}

/** Gamma-encoded sRGB channels, 0–1, clipped to gamut. */
export function toSrgb(color: Oklch): RgbTriplet {
  const [r, g, b] = oklchToLinearSrgb(color);
  return [encodeSrgb(r), encodeSrgb(g), encodeSrgb(b)];
}

/** `#rrggbb` for hosts that cannot read OKLCH (e.g. native StyleSheet). */
export function toHex(color: Oklch): string {
  return `#${toSrgb(color)
    .map((channel) =>
      Math.round(channel * 255)
        .toString(16)
        .padStart(2, "0"),
    )
    .join("")}`;
}

export function fromHex(hex: string): Oklch {
  const value = Number.parseInt(hex.replace("#", ""), 16);
  const [r, g, b] = [(value >> 16) & 255, (value >> 8) & 255, value & 255].map(
    (channel) => decodeSrgb(channel / 255),
  ) as [number, number, number];

  const l = Math.cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
  const m = Math.cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
  const s = Math.cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

  const L = 0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s;
  const A = 1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s;
  const B = 0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s;

  const hue = (Math.atan2(B, A) * 180) / Math.PI;
  return { l: L, c: Math.hypot(A, B), h: hue < 0 ? hue + 360 : hue };
}

/** Largest chroma at (l, h) that stays inside sRGB. */
export function maxSrgbChroma(l: number, h: number): number {
  let low = 0;
  let high = 0.4;
  for (let i = 0; i < 32; i += 1) {
    const mid = (low + high) / 2;
    if (isInSrgbGamut({ l, c: mid, h })) low = mid;
    else high = mid;
  }
  return low;
}

/**
 * Builds an in-gamut tone: requested chroma, reduced to stay inside sRGB with
 * a small safety margin so every emitted token renders exactly as authored.
 */
export function tone(l: number, c: number, h: number): Oklch {
  return { l, c: Math.min(c, maxSrgbChroma(l, h) * 0.97), h };
}

/** Alpha-composites `top` over an opaque `bottom` in gamma-encoded sRGB, as browsers do. */
export function compositeOver(top: Oklch, bottom: Oklch): RgbTriplet {
  const alpha = top.alpha ?? 1;
  const t = toSrgb(top);
  const b = toSrgb(bottom);
  return [0, 1, 2].map(
    (i) => t[i]! * alpha + b[i]! * (1 - alpha),
  ) as unknown as RgbTriplet;
}

/** WCAG relative luminance of gamma-encoded sRGB channels. */
export function luminanceOfSrgb(srgb: RgbTriplet): number {
  const [r, g, b] = srgb.map(decodeSrgb) as unknown as [number, number, number];
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

export function relativeLuminance(color: Oklch): number {
  return luminanceOfSrgb(toSrgb(color));
}

function ratio(a: number, b: number): number {
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
}

/** WCAG 2.x contrast ratio between two opaque colors. */
export function contrastRatio(a: Oklch, b: Oklch): number {
  return ratio(relativeLuminance(a), relativeLuminance(b));
}

/** Contrast of `text` over `tint` (translucent) composited on `base`. */
export function contrastOverComposite(
  text: Oklch,
  tint: Oklch,
  base: Oklch,
): number {
  return ratio(
    relativeLuminance(text),
    luminanceOfSrgb(compositeOver(tint, base)),
  );
}

/** Euclidean OKLab distance (ΔE_OK). ~0.02 is a just-noticeable difference. */
export function deltaEOk(a: Oklch, b: Oklch): number {
  const ha = (a.h * Math.PI) / 180;
  const hb = (b.h * Math.PI) / 180;
  return Math.hypot(
    a.l - b.l,
    a.c * Math.cos(ha) - b.c * Math.cos(hb),
    a.c * Math.sin(ha) - b.c * Math.sin(hb),
  );
}

export function withAlpha(color: Oklch, alpha: number): Oklch {
  return { l: color.l, c: color.c, h: color.h, alpha };
}
