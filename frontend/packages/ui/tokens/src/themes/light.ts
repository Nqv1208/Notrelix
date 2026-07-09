/**
 * @notrelix/ui-tokens — Light theme CSS variables.
 *
 * Returns CSS custom property declarations for light mode.
 * Framework-neutral: no React, no DOM.
 */

import { brand, primitive } from '../colors';

export const lightTheme = {
  '--background': 'oklch(0.975 0.004 260)',
  '--foreground': 'oklch(0.245 0.006 260)',
  '--app-shell': 'oklch(0.965 0.006 260)',
  '--app-header': 'oklch(0.948 0.024 275)',
  '--card': 'oklch(1 0 0)',
  '--card-foreground': 'oklch(0.245 0.006 260)',
  '--popover': 'oklch(1 0 0)',
  '--popover-foreground': 'oklch(0.245 0.006 260)',
  '--primary': 'oklch(0.58 0.23 285)',
  '--primary-foreground': 'oklch(0.985 0 0)',
  '--secondary': 'oklch(0.955 0.006 260)',
  '--secondary-foreground': 'oklch(0.245 0.006 260)',
  '--muted': 'oklch(0.955 0.006 260)',
  '--muted-foreground': 'oklch(0.49 0.015 260)',
  '--accent': 'oklch(0.935 0.02 275)',
  '--accent-foreground': 'oklch(0.42 0.18 285)',
  '--destructive': 'oklch(0.58 0.22 27)',
  '--border': 'oklch(0.922 0 0)',
  '--input': 'oklch(0.922 0 0)',
  '--ring': 'var(--primary)',
  '--radius': '0.625rem',

  '--sidebar': 'var(--card)',
  '--sidebar-foreground': 'var(--foreground)',
  '--sidebar-primary': 'var(--primary)',
  '--sidebar-primary-foreground': 'var(--primary-foreground)',
  '--sidebar-accent': 'var(--muted)',
  '--sidebar-accent-foreground': 'var(--foreground)',
  '--sidebar-border': 'var(--border)',
  '--sidebar-ring': 'var(--ring)',

  '--color-deep-space': primitive.deepSpace,
  '--color-void-black': primitive.voidBlack,
  '--color-paper': 'var(--card)',
  '--color-fog': 'var(--background)',
  '--color-mist': 'var(--muted)',
  '--color-graphite': 'var(--foreground)',
  '--color-slate': 'var(--muted-foreground)',
  '--color-iron': 'var(--muted-foreground)',
  '--color-silver': 'var(--border)',
  '--color-ash': primitive.ash,

  '--color-brand-violet': 'var(--primary)',
  '--color-brand-indigo': 'var(--primary)',
  '--color-brand-purple': brand.purple,
  '--color-brand-ocean': brand.ocean,
  '--color-brand-sky': brand.sky,
  '--color-brand-frost': brand.frost,
} as const;
