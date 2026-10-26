import type { ColorToken, HighlightToken } from "@notrelix/docs-core";

/** Reading colours of a document on a light surface (rich text colour tokens). */
export const TEXT_COLORS: Readonly<Record<ColorToken, string>> = {
  default: "#111827",
  muted: "#6b7280",
  primary: "#4338ca",
  accent: "#7c3aed",
  success: "#15803d",
  warning: "#b45309",
  danger: "#b91c1c",
};

export const HIGHLIGHT_COLORS: Readonly<Record<HighlightToken, string>> = {
  none: "transparent",
  muted: "#f3f4f6",
  accent: "#ede9fe",
  primary: "#e0e7ff",
  success: "#dcfce7",
  warning: "#fef3c7",
  danger: "#fee2e2",
};

export const CALLOUT_COLORS = {
  info: "#e0e7ff",
  success: "#dcfce7",
  warning: "#fef3c7",
  danger: "#fee2e2",
  neutral: "#f3f4f6",
} as const;

export const DOC_COLORS = {
  text: TEXT_COLORS.default,
  muted: TEXT_COLORS.muted,
  line: "#e5e7eb",
  codeBackground: "#f3f4f6",
  link: "#4338ca",
} as const;
