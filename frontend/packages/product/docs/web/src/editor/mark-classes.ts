import { COLOR_TOKENS, HIGHLIGHT_TOKENS } from "@notrelix/docs-core";
import type { ColorToken, HighlightToken } from "@notrelix/docs-core";

/** App-token classes for the contract's text colour tokens (light and dark follow the tokens). */
export const COLOR_CLASS: Readonly<Record<ColorToken, string>> = {
  default: "",
  muted: "text-muted-foreground",
  primary: "text-brand-subtle-foreground",
  accent: "text-info-text",
  success: "text-success-text",
  warning: "text-warning-text",
  danger: "text-danger-text",
};

export const HIGHLIGHT_CLASS: Readonly<Record<HighlightToken, string>> = {
  none: "",
  muted: "bg-state-hover-strong",
  accent: "bg-info-subtle",
  primary: "bg-brand-subtle",
  success: "bg-success-subtle",
  warning: "bg-warning-subtle",
  danger: "bg-danger-subtle",
};

/** Swatch classes (the text colour as a filled dot). */
export const COLOR_SWATCH: Readonly<Record<ColorToken, string>> = {
  default: "bg-foreground",
  muted: "bg-muted-foreground",
  primary: "bg-brand-subtle-foreground",
  accent: "bg-info-text",
  success: "bg-success-text",
  warning: "bg-warning-text",
  danger: "bg-danger-text",
};

export { COLOR_TOKENS, HIGHLIGHT_TOKENS };
