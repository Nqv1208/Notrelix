/**
 * @notrelix/ui-tokens — Semantic color roles per appearance × accent.
 *
 * Two independent axes (FE-UI-009/010):
 *   appearance: light | dark | high-contrast   (mode "system" resolves to light/dark)
 *   accent:     blue | coral | ember | sage | amber
 *
 * Accent-independent roles are resolved from neutrals/status/identity.
 * Accent-dependent roles are resolved from the active accent's tones; CSS
 * emits them through `--accent-tone-*` indirection so an accent change is a
 * single attribute flip on the themed element.
 *
 * Role names keep the shadcn variable vocabulary (`primary`, `background`,
 * `sidebar-*`, …) so existing components keep working, and add Notrelix
 * roles (surface tiers, state set, status pairs, identity).
 *
 * Framework-neutral: no React, no DOM.
 */

import type { Oklch } from "./oklch";
import {
  CHART_SERIES,
  IDENTITY_IDS,
  LABEL_TONE_IDS,
  MARK_TONE_IDS,
  STATUS_IDS,
  WORK_STATUS_IDS,
  accents,
  identity,
  labelTones,
  markTones,
  neutrals,
  priorityHigh,
  railTones,
  statuses,
  workStatusLanes,
  workStatuses,
  type AccentId,
  type AccentTones,
  type ThemeAppearance,
} from "./palette";

export type AccentToneKey = keyof AccentTones;

/** Roles whose value comes from the active accent. */
export const ACCENT_ROLES = {
  primary: "solid",
  "primary-foreground": "onSolid",
  ring: "ring",
  "sidebar-primary": "solid",
  "sidebar-primary-foreground": "onSolid",
  "sidebar-ring": "ring",
  "brand-mark": "mark",
  "brand-solid": "solid",
  "brand-solid-foreground": "onSolid",
  "brand-ink": "ink",
  link: "ink",
  "brand-subtle": "subtle",
  "brand-subtle-foreground": "onSubtle",
  "state-selected": "subtle",
  "state-selected-foreground": "onSubtle",
  "focus-ring": "ring",
  // Transitional aliases consumed by Work Management table/badge styling.
  "table-selected": "subtle",
  "badge-medium-bg": "subtle",
  "badge-medium-text": "onSubtle",
} as const satisfies Record<string, AccentToneKey>;

export type AccentRole = keyof typeof ACCENT_ROLES;

/**
 * Transitional aliases kept only because current consumers read them
 * (`--app-*`, `--table-*`, `--badge-*`). New code uses surface/status roles.
 */
export const TRANSITIONAL_ROLES = [
  "app-shell",
  "app-header",
  "table-bg",
  "table-row",
  "table-row-hover",
  "table-header",
  "table-group",
  "table-selected",
  "badge-default-bg",
  "badge-default-text",
  "badge-done-bg",
  "badge-done-text",
  "badge-working-bg",
  "badge-working-text",
  "badge-stuck-bg",
  "badge-stuck-text",
  "badge-urgent-bg",
  "badge-urgent-text",
  "badge-high-bg",
  "badge-high-text",
  "badge-medium-bg",
  "badge-medium-text",
  "badge-low-bg",
  "badge-low-text",
] as const;

/** Accent-independent roles for one appearance. */
export function buildAppearanceColors(
  appearance: ThemeAppearance,
): Record<string, Oklch> {
  const nr = neutrals[appearance];
  const status = (id: (typeof STATUS_IDS)[number]) => statuses[id][appearance];

  const roles: Record<string, Oklch> = {
    // shadcn vocabulary
    background: nr.canvas,
    foreground: nr.text,
    card: nr.panel,
    "card-foreground": nr.text,
    popover: nr.raised,
    "popover-foreground": nr.text,
    secondary: nr.subtle,
    "secondary-foreground": nr.text,
    muted: nr.subtle,
    "muted-foreground": nr.textMuted,
    accent: nr.hover,
    "accent-foreground": nr.text,
    destructive: status("danger").solid,
    "destructive-foreground": status("danger").onSolid,
    border: nr.border,
    input: nr.input,
    sidebar: nr.sidebar,
    "sidebar-foreground": nr.text,
    "sidebar-accent": nr.hoverStrong,
    "sidebar-accent-foreground": nr.text,
    "sidebar-border": nr.border,

    // Surface tiers
    "surface-canvas": nr.canvas,
    "surface-sidebar": nr.sidebar,
    "surface-panel": nr.panel,
    "surface-raised": nr.raised,

    // Text and icon
    "text-strong": nr.text,
    "text-muted": nr.textMuted,
    icon: nr.textMuted,

    // State set
    "state-hover": nr.hover,
    "state-disabled": nr.disabled,
    "state-disabled-foreground": nr.textDisabled,

    // Transitional aliases
    "app-shell": nr.canvas,
    "app-header": nr.canvas,
    "table-bg": nr.panel,
    "table-row": nr.panel,
    "table-row-hover": nr.hover,
    "table-header": nr.subtle,
    "table-group": nr.subtle,
    "badge-default-bg": nr.subtle,
    "badge-default-text": nr.textMuted,
    "badge-low-bg": nr.subtle,
    "badge-low-text": nr.textMuted,
    "badge-done-bg": status("success").subtle,
    "badge-done-text": status("success").onSubtle,
    "badge-working-bg": status("warning").subtle,
    "badge-working-text": status("warning").onSubtle,
    "badge-stuck-bg": status("danger").subtle,
    "badge-stuck-text": status("danger").onSubtle,
    "badge-urgent-bg": status("danger").subtle,
    "badge-urgent-text": status("danger").onSubtle,
    "badge-high-bg": status("info").subtle,
    "badge-high-text": status("info").onSubtle,
  };

  for (const id of STATUS_IDS) {
    const tones = status(id);
    roles[id] = tones.solid;
    roles[`${id}-foreground`] = tones.onSolid;
    roles[`${id}-text`] = tones.text;
    roles[`${id}-subtle`] = tones.subtle;
    roles[`${id}-subtle-foreground`] = tones.onSubtle;
  }

  for (const id of WORK_STATUS_IDS) {
    roles[`work-status-${id}`] = workStatuses[id][appearance].fill;
    roles[`work-status-${id}-text`] = workStatuses[id][appearance].text;
    roles[`work-status-${id}-lane`] = workStatusLanes[id][appearance];
  }

  for (const id of LABEL_TONE_IDS) {
    roles[`label-${id}`] = labelTones[id][appearance].fill;
    roles[`label-${id}-foreground`] = labelTones[id][appearance].onFill;
  }

  roles["priority-high"] = priorityHigh[appearance].fill;
  roles["priority-high-text"] = priorityHigh[appearance].text;

  for (const id of IDENTITY_IDS) {
    roles[`identity-${id}`] = identity[id].fill;
    roles[`identity-${id}-foreground`] = identity[id].onFill;
  }

  for (const id of MARK_TONE_IDS) {
    roles[`mark-${id}`] = markTones[id].fill;
    roles[`mark-${id}-foreground`] = markTones[id].onFill;
  }

  const rail = railTones[appearance];
  roles.rail = rail.surface;
  roles["rail-foreground"] = rail.ink;
  roles["rail-active-foreground"] = rail.activeInk;
  roles["rail-hover"] = rail.hover;
  roles["rail-separator"] = rail.separator;
  roles["rail-badge"] = rail.badge;
  roles["rail-badge-foreground"] = rail.onBadge;
  roles["rail-focus-ring"] = rail.focusRing;

  CHART_SERIES.forEach((id, index) => {
    roles[`chart-${index + 1}`] = identity[id].fill;
  });

  return roles;
}

export function accentTones(
  accent: AccentId,
  appearance: ThemeAppearance,
): AccentTones {
  return accents[accent].tones[appearance];
}

/** Every semantic role fully resolved for one appearance × accent. */
export function resolveSemanticColors(
  appearance: ThemeAppearance,
  accent: AccentId,
): Record<string, Oklch> {
  const tones = accentTones(accent, appearance);
  const resolved = { ...buildAppearanceColors(appearance) };
  for (const [role, toneKey] of Object.entries(ACCENT_ROLES)) {
    resolved[role] = tones[toneKey];
  }
  return resolved;
}

/** Names of every semantic color role (CSS variable names without `--`). */
export function semanticColorRoleNames(): string[] {
  return Object.keys(resolveSemanticColors("light", "blue")).sort();
}
