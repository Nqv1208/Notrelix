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
] as const;

const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      "font-size": [{ text: [...TEXT_ROLES] }],
    },
  },
});

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
