// @notrelix/ui-tokens — Design tokens barrel export

export {
  primitive,
  brand,
  semantic,
  surface,
  gradients,
  badge,
  badgeDark,
} from './colors';

export { fonts, weights, typeScale } from './typography';
export type { TypeToken } from './typography';

export { baseUnit, spacing, layout, grid } from './spacing';

export { radius } from './radius';

export { shadows } from './shadows';

export { duration, easing } from './motion';

export { surfaces, tableSurface, focusRing } from './semantic';

export { lightTheme } from './themes/light';
export { darkTheme } from './themes/dark';

/**
 * Combines class names with Tailwind merge support.
 */
import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
