import type { ReactNode } from "react";
import { Badge } from "../components/ui/badge";
import { Button } from "../components/ui/button";
import { Input } from "../components/ui/input";
import { ThemePicker, type ThemePickerLabels } from "./theme-picker";
import { ThemeProvider } from "./theme-provider";
import {
  THEME_ACCENTS,
  themeRootState,
  type EffectiveAppearance,
  type ThemeAccent,
} from "./theme-model";

/**
 * Visual review of every appearance × accent (WX-FND-001/002). Each cell
 * carries the same root contract the runtime applies to <html> (class,
 * data-contrast, data-accent), so the generated token CSS resolves per cell.
 */

const APPEARANCES: readonly EffectiveAppearance[] = [
  "light",
  "dark",
  "high-contrast",
];

const SWATCH_ROLES = [
  "surface-canvas",
  "surface-sidebar",
  "surface-panel",
  "surface-raised",
  "brand-mark",
  "brand-solid",
  "brand-ink",
  "brand-subtle",
  "focus-ring",
  "success",
  "warning",
  "danger",
  "info",
] as const;

function ThemedCell({
  appearance,
  accent,
  children,
}: {
  appearance: EffectiveAppearance;
  accent: ThemeAccent;
  children: ReactNode;
}) {
  const state = themeRootState(appearance, accent);
  return (
    <div
      className={`${state.colorClass} bg-background text-foreground border-border rounded-surface border p-4`}
      data-accent={state.accent}
      data-contrast={state.contrast ?? undefined}
      style={{ colorScheme: state.colorClass }}
    >
      {children}
    </div>
  );
}

function Cell({
  appearance,
  accent,
}: {
  appearance: EffectiveAppearance;
  accent: ThemeAccent;
}) {
  return (
    <ThemedCell appearance={appearance} accent={accent}>
      <div className="flex flex-col gap-3">
        <p className="text-label">
          {appearance} · {accent}
        </p>
        <div className="flex flex-wrap gap-1" aria-hidden>
          {SWATCH_ROLES.map((role) => (
            <span
              key={role}
              title={`--${role}`}
              className="border-border size-5 rounded-sm border"
              style={{ background: `var(--${role})` }}
            />
          ))}
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button size="sm">Primary</Button>
          <Button size="sm" variant="outline">
            Outline
          </Button>
          <Button size="sm" variant="destructive">
            Delete
          </Button>
          <Button size="sm" variant="link">
            Link
          </Button>
        </div>
        <Input
          aria-label={`Name (${appearance} ${accent})`}
          placeholder="Board name"
        />
        <div className="flex flex-wrap items-center gap-2">
          <Badge>Default</Badge>
          <Badge variant="secondary">Secondary</Badge>
          <Badge variant="destructive">Danger</Badge>
          <Badge variant="outline">Outline</Badge>
        </div>
        <p className="text-body text-muted-foreground">
          Muted body with an{" "}
          <a
            href="#theme-matrix"
            className="text-link underline underline-offset-4"
          >
            inline link
          </a>
          . <span className="tabular-nums">1,234.50</span>
        </p>
        <div className="flex flex-wrap gap-2">
          <span className="bg-success-subtle text-success-subtle-foreground text-caption rounded-sm px-2 py-0.5">
            Done
          </span>
          <span className="bg-warning-subtle text-warning-subtle-foreground text-caption rounded-sm px-2 py-0.5">
            At risk
          </span>
          <span className="bg-danger-subtle text-danger-subtle-foreground text-caption rounded-sm px-2 py-0.5">
            Blocked
          </span>
          <span className="bg-info-subtle text-info-subtle-foreground text-caption rounded-sm px-2 py-0.5">
            Info
          </span>
        </div>
        <button
          type="button"
          className="focus-ring bg-state-selected text-state-selected-foreground text-label rounded-control w-fit px-2 py-1"
        >
          Focus ring
        </button>
      </div>
    </ThemedCell>
  );
}

const meta = {
  title: "UI Web/Theme",
  parameters: { layout: "fullscreen" },
};

export default meta;

export const ModeAccentMatrix = {
  render: () => (
    <div id="theme-matrix" className="flex flex-col gap-6 p-6">
      {APPEARANCES.map((appearance) => (
        <section key={appearance} className="flex flex-col gap-3">
          <h2 className="text-section-heading">{appearance}</h2>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
            {THEME_ACCENTS.map((accent) => (
              <Cell key={accent} appearance={appearance} accent={accent} />
            ))}
          </div>
        </section>
      ))}
    </div>
  ),
};

const labels: ThemePickerLabels = {
  mode: {
    legend: "Mode",
    options: {
      light: { label: "Light", description: "Light surfaces" },
      dark: { label: "Dark", description: "Dark surfaces" },
      system: { label: "System", description: "Follows the device" },
      "high-contrast": {
        label: "High contrast",
        description: "Maximum legibility",
      },
    },
  },
  accent: {
    legend: "Accent color",
    options: {
      blue: { label: "Notrelix Blue", description: "Default brand color" },
      coral: { label: "Coral", description: "Warm orange" },
      ember: { label: "Ember", description: "Deep rose red" },
      sage: { label: "Sage", description: "Muted green" },
      amber: { label: "Amber", description: "Warm yellow" },
    },
  },
};

/** Live picker: drives the Storybook document root like the app does. */
export const Picker = {
  render: () => (
    <div className="bg-background text-foreground min-h-screen p-6">
      <ThemeProvider>
        <ThemePicker labels={labels} className="max-w-2xl" />
      </ThemeProvider>
    </div>
  ),
};
