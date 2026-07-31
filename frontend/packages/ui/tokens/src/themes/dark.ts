/**
 * @notrelix/ui-tokens — Dark theme CSS variables.
 *
 * Returns CSS custom property declarations for dark mode.
 * Framework-neutral: no React, no DOM.
 */

export const darkTheme = {
  '--background': 'oklch(0.148 0.025 265)',
  '--foreground': 'oklch(0.967 0.006 260)',
  '--app-shell': 'oklch(0.148 0.025 265)',
  '--app-header': 'oklch(0.268 0.032 268)',
  '--card': 'oklch(0.196 0.030 265)',
  '--card-foreground': 'oklch(0.967 0.006 260)',
  '--popover': 'oklch(0.240 0.028 265)',
  '--popover-foreground': 'oklch(0.967 0.006 260)',
  '--primary': 'oklch(0.656 0.214 286)',
  '--primary-foreground': 'oklch(0.985 0 0)',
  '--secondary': 'oklch(0.255 0.027 265)',
  '--secondary-foreground': 'oklch(0.967 0.006 260)',
  '--muted': 'oklch(0.255 0.027 265)',
  '--muted-foreground': 'oklch(0.627 0.019 265)',
  '--accent': 'oklch(0.296 0.070 287)',
  '--accent-foreground': 'oklch(0.970 0.006 260)',
  '--destructive': 'oklch(0.660 0.191 22.216)',
  '--border': 'oklch(0.348 0.024 265)',
  '--input': 'oklch(0.365 0.024 265)',
  '--ring': 'var(--primary)',

  '--sidebar': 'oklch(0.218 0.026 265)',
  '--sidebar-foreground': 'oklch(0.967 0.006 260)',
  '--sidebar-primary': 'var(--primary)',
  '--sidebar-primary-foreground': 'var(--primary-foreground)',
  '--sidebar-accent': 'oklch(0.263 0.028 265)',
  '--sidebar-accent-foreground': 'oklch(0.967 0.006 260)',
  '--sidebar-border': 'var(--border)',
  '--sidebar-ring': 'oklch(0.556 0 0)',
} as const;
