import * as React from "react";
import { Contrast, Monitor, Moon, Sun, type LucideIcon } from "lucide-react";
import { cn } from "../lib/cn";
import { useTheme } from "./theme-provider";
import {
  THEME_ACCENT_OPTIONS,
  THEME_MODES,
  type ThemeAccent,
  type ThemeMode,
} from "./theme-model";

interface OptionLabel {
  readonly label: string;
  readonly description: string;
}

/**
 * Copy is supplied by the consumer (FE-UI-069): this primitive owns the
 * interaction and layout, the host/feature owns translated labels.
 */
export interface ThemePickerLabels {
  readonly mode: {
    readonly legend: string;
    readonly options: Readonly<Record<ThemeMode, OptionLabel>>;
  };
  readonly accent: {
    readonly legend: string;
    readonly options: Readonly<Record<ThemeAccent, OptionLabel>>;
  };
}

const MODE_ICONS: Readonly<Record<ThemeMode, LucideIcon>> = {
  light: Sun,
  dark: Moon,
  system: Monitor,
  "high-contrast": Contrast,
};

const optionClass =
  "relative flex cursor-pointer gap-3 rounded-control border border-border bg-card p-3 text-left transition-colors hover:bg-state-hover has-[:checked]:border-primary has-[:checked]:bg-state-selected has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-focus-ring";

export interface ThemePickerProps {
  readonly labels: ThemePickerLabels;
  readonly className?: string;
  /** Called after the user picks a mode (e.g. to mirror it to account preferences). */
  readonly onModeChange?: (mode: ThemeMode) => void;
  /** Called after the user picks an accent. */
  readonly onAccentChange?: (accent: ThemeAccent) => void;
}

/** Two independent radio groups: appearance mode and accent. */
export function ThemePicker({
  labels,
  className,
  onModeChange,
  onAccentChange,
}: ThemePickerProps) {
  const { mode, accent, setMode, setAccent } = useTheme();
  const id = React.useId();

  return (
    <div
      className={cn("flex flex-col gap-6", className)}
      data-slot="theme-picker"
    >
      <fieldset className="flex flex-col gap-3">
        <legend className="text-label text-foreground mb-3">
          {labels.mode.legend}
        </legend>
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          {THEME_MODES.map((option) => {
            const Icon = MODE_ICONS[option];
            const text = labels.mode.options[option];
            return (
              <label key={option} className={cn(optionClass, "items-start")}>
                <input
                  type="radio"
                  name={`${id}-mode`}
                  value={option}
                  checked={mode === option}
                  onChange={() => {
                    setMode(option);
                    onModeChange?.(option);
                  }}
                  className="sr-only"
                />
                <Icon
                  className="text-icon mt-0.5 size-4 shrink-0"
                  aria-hidden
                />
                <span className="flex min-w-0 flex-col gap-0.5">
                  <span className="text-label text-foreground">
                    {text.label}
                  </span>
                  <span className="text-caption text-muted-foreground">
                    {text.description}
                  </span>
                </span>
              </label>
            );
          })}
        </div>
      </fieldset>

      <fieldset className="flex flex-col gap-3">
        <legend className="text-label text-foreground mb-3">
          {labels.accent.legend}
        </legend>
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          {THEME_ACCENT_OPTIONS.map((option) => {
            const text = labels.accent.options[option.id];
            return (
              <label
                key={option.id}
                className={cn(optionClass, "items-center")}
              >
                <input
                  type="radio"
                  name={`${id}-accent`}
                  value={option.id}
                  checked={accent === option.id}
                  onChange={() => {
                    setAccent(option.id);
                    onAccentChange?.(option.id);
                  }}
                  className="sr-only"
                />
                <span
                  aria-hidden
                  className="size-5 shrink-0 rounded-full border border-border"
                  style={{ background: option.swatch }}
                />
                <span className="flex min-w-0 flex-col gap-0.5">
                  <span className="text-label text-foreground">
                    {text.label}
                  </span>
                  <span className="text-caption text-muted-foreground">
                    {text.description}
                  </span>
                </span>
              </label>
            );
          })}
        </div>
      </fieldset>
    </div>
  );
}
