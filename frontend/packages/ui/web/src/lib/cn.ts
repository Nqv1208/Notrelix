/**
 * @notrelix/ui-web — cn utility
 *
 * Combines class names with Tailwind CSS merge support.
 * Extracted from apps/app/lib/utils.ts for package independence.
 */
import { type ClassValue, clsx } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
