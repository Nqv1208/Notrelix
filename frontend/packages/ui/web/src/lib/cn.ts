/**
 * @notrelix/ui-web — cn utility
 *
 * Combines class names with Tailwind CSS merge support.
 * Extracted from apps/app/lib/utils.ts for package independence.
 */
import { type ClassValue, clsx } from "clsx";
import { extendTailwindMerge } from "tailwind-merge";

/**
 * The semantic text roles (WX-FND-003, DESIGN.md §8.1) are font sizes. Without
 * this, tailwind-merge reads `text-metadata` as a text color and drops it when
 * a color such as `text-muted-foreground` follows it in the same `cn()` call.
 */
export const TEXT_ROLES = [
  "display",
  "page-title",
  "section-heading",
  "subheading",
  "body",
  "dense-body",
  "label",
  "metadata",
  "caption",
  "code",
  "prose",
  "nav",
  "compact",
  "rail-label",
  "key",
  "mark-2xs",
  "mark-xs",
  "mark-sm",
  "mark-md",
  "mark-lg",
  "board-title",
  "sheet-title",
  "crumb",
  "micro",
  "mark-3xs",
  "avatar-sm",
  "avatar-md",
  "avatar-lg",
  "greeting",
  "list-title",
  "account-title",
  "brand-name",
  "brand-name-compact",
  "card-title",
  "brand-promise",
  "empty-title",
  "dialog-title",
  "doc-title",
  "doc-heading",
  "doc-prose",
  "doc-list",
  "doc-check",
  "doc-callout",
  "item-title",
  "item-heading",
  "item-prose",
  "item-text",
  "item-comment",
  "item-ui",
  "item-small",
  "item-note",
  "avatar-xs",
  "calendar-title",
  "hint",
] as const;

/**
 * The semantic stacking roles (`@notrelix/ui-tokens` `zIndex`, utilities
 * `z-dropdown` ... `z-tooltip`) are z-index values. Without this, tailwind-merge
 * keeps a numeric z utility and `z-modal` side by side instead of letting the last win.
 */
export const Z_ROLES = [
  "dropdown",
  "sticky",
  "backdrop",
  "modal",
  "toast",
  "tooltip",
] as const;

const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      "font-size": [{ text: [...TEXT_ROLES] }],
      z: [{ z: [...Z_ROLES] }],
    },
  },
});

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
