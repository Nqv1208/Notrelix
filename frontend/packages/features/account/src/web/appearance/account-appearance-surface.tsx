import * as React from "react";
import {
  Check,
  Contrast,
  Monitor,
  Moon,
  Sun,
  type LucideIcon,
} from "lucide-react";
import {
  Button,
  Field,
  FieldDescription,
  FieldLabel,
  Label,
  RadioGroup,
  RadioGroupItem,
  Skeleton,
  Switch,
  THEME_ACCENT_OPTIONS,
  THEME_MODES,
  type ThemeAccent,
  type ThemeMode,
} from "@notrelix/ui-web";
import type {
  AccountLocale,
  AccountSaveStatus,
  AccountSection,
} from "../shared/account-surface-model";
import type { AccountAppearanceCopy } from "./account-appearance.copy";

/** The account preference this page loads: the sidebar default. */
export interface AccountSidebarPreference {
  readonly collapsed: boolean;
}

export interface AccountAppearanceSurfaceProps {
  readonly copy: AccountAppearanceCopy;
  /** Device appearance (theme runtime): always available, never loading. */
  readonly mode: ThemeMode;
  readonly accent: ThemeAccent;
  readonly locale: AccountLocale;
  /** The account's sidebar preference; the only part that loads. */
  readonly sidebar: AccountSection<AccountSidebarPreference>;
  readonly sidebarSave: AccountSaveStatus;
  readonly onModeChange: (mode: ThemeMode) => void;
  readonly onAccentChange: (accent: ThemeAccent) => void;
  readonly onLocaleChange: (locale: AccountLocale) => void;
  readonly onSidebarCollapsedChange: (collapsed: boolean) => void;
  readonly onRetryPreferences: () => void;
}

const MODE_ICONS: Readonly<Record<ThemeMode, LucideIcon>> = {
  light: Sun,
  dark: Moon,
  system: Monitor,
  "high-contrast": Contrast,
};

const MODE_COPY_KEYS = {
  light: "light",
  dark: "dark",
  system: "system",
  "high-contrast": "highContrast",
} as const;

const LOCALE_OPTIONS = ["vi", "en"] as const satisfies readonly AccountLocale[];

const optionClass =
  "relative flex min-w-0 cursor-pointer items-start gap-3 rounded-control border border-border bg-card p-3 text-left leading-snug transition-colors hover:bg-state-hover has-[[data-state=checked]]:border-primary has-[[data-state=checked]]:bg-state-selected has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-focus-ring";

function OptionCard({
  id,
  value,
  label,
  description,
  lead,
  lang,
}: {
  id: string;
  value: string;
  label: string;
  description?: string;
  lead?: React.ReactNode;
  lang?: string;
}) {
  const labelId = `${id}-label`;
  const descriptionId = `${id}-description`;
  return (
    <Label htmlFor={id} lang={lang} className={optionClass}>
      <RadioGroupItem
        id={id}
        value={value}
        aria-labelledby={labelId}
        aria-describedby={description ? descriptionId : undefined}
        className="mt-0.5"
      />
      {lead}
      <span className="flex min-w-0 flex-1 flex-col gap-0.5">
        <span id={labelId} className="text-label text-foreground">
          {label}
        </span>
        {description ? (
          <span
            id={descriptionId}
            className="text-caption font-normal text-muted-foreground"
          >
            {description}
          </span>
        ) : null}
      </span>
    </Label>
  );
}

function ChoiceGroup({
  legend,
  value,
  onValueChange,
  children,
}: {
  legend: string;
  value: string;
  onValueChange: (value: string) => void;
  children: React.ReactNode;
}) {
  const legendId = React.useId();
  return (
    <section className="flex min-w-0 flex-col gap-3">
      <h2 id={legendId} className="text-label text-foreground">
        {legend}
      </h2>
      <RadioGroup
        aria-labelledby={legendId}
        value={value}
        onValueChange={onValueChange}
        className="grid-cols-1 gap-2 sm:grid-cols-2"
      >
        {children}
      </RadioGroup>
    </section>
  );
}

function SidebarSection({
  copy,
  sidebar,
  save,
  onChange,
  onRetry,
}: {
  copy: AccountAppearanceCopy;
  sidebar: AccountSection<AccountSidebarPreference>;
  save: AccountSaveStatus;
  onChange: (collapsed: boolean) => void;
  onRetry: () => void;
}) {
  const id = React.useId();
  const helpId = React.useId();
  return (
    <section className="flex min-w-0 flex-col gap-3">
      <h2 className="text-label text-foreground">{copy.sidebar.legend}</h2>
      {sidebar.status === "loading" ? (
        <div
          role="status"
          aria-label={copy.sidebar.loading}
          className="flex flex-col gap-2 rounded-lg border border-border p-4"
        >
          <Skeleton className="h-3.5 w-40" />
          <Skeleton className="h-3 w-56 max-w-full" />
        </div>
      ) : sidebar.status === "error" ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-border px-3 py-2"
        >
          <p className="min-w-0 text-body text-muted-foreground">
            {copy.sidebar.loadError}
          </p>
          <Button size="xs" variant="outline" onClick={onRetry}>
            {copy.sidebar.retry}
          </Button>
        </div>
      ) : (
        <div className="flex min-w-0 flex-col gap-2 rounded-lg border border-border p-4">
          <Field
            orientation="horizontal"
            className="items-start justify-between gap-4"
          >
            <div className="flex min-w-0 flex-col gap-0.5">
              <FieldLabel htmlFor={id}>{copy.sidebar.label}</FieldLabel>
              <FieldDescription id={helpId}>
                {copy.sidebar.help}
              </FieldDescription>
            </div>
            <Switch
              id={id}
              checked={sidebar.data.collapsed}
              disabled={save.state === "saving"}
              aria-describedby={helpId}
              onCheckedChange={onChange}
            />
          </Field>
          {save.state === "saving" ? (
            <p role="status" className="text-metadata text-muted-foreground">
              {copy.sidebar.saving}
            </p>
          ) : save.state === "saved" ? (
            <p
              role="status"
              className="inline-flex items-center gap-1 text-metadata text-success-text"
            >
              <Check aria-hidden="true" className="size-3.5 shrink-0" />
              {copy.sidebar.saved}
            </p>
          ) : save.state === "error" ? (
            <p
              role="alert"
              className="rounded-md bg-danger-subtle px-3 py-2 text-metadata text-danger-subtle-foreground"
            >
              {copy.sidebar.saveError}
            </p>
          ) : null}
        </div>
      )}
    </section>
  );
}

/**
 * Account appearance: theme mode, accent colour and language as labelled
 * radio groups (device preferences handled by the theme and locale
 * runtimes), plus the account's sidebar default as a switch.
 */
export function AccountAppearanceSurface({
  copy,
  mode,
  accent,
  locale,
  sidebar,
  sidebarSave,
  onModeChange,
  onAccentChange,
  onLocaleChange,
  onSidebarCollapsedChange,
  onRetryPreferences,
}: AccountAppearanceSurfaceProps) {
  const prefix = React.useId();
  return (
    <div className="mx-auto flex w-full min-w-0 max-w-3xl flex-col gap-8 px-4 py-6 md:px-8 md:py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-page-title text-foreground">{copy.title}</h1>
        <p className="text-body text-muted-foreground">{copy.description}</p>
      </header>

      <ChoiceGroup
        legend={copy.mode.legend}
        value={mode}
        onValueChange={(value) => onModeChange(value as ThemeMode)}
      >
        {THEME_MODES.map((option) => {
          const Icon = MODE_ICONS[option];
          const text = copy.mode[MODE_COPY_KEYS[option]];
          return (
            <OptionCard
              key={option}
              id={`${prefix}-mode-${option}`}
              value={option}
              label={text.label}
              description={text.description}
              lead={
                <Icon
                  aria-hidden="true"
                  className="text-icon mt-0.5 size-4 shrink-0"
                />
              }
            />
          );
        })}
      </ChoiceGroup>

      <ChoiceGroup
        legend={copy.accent.legend}
        value={accent}
        onValueChange={(value) => onAccentChange(value as ThemeAccent)}
      >
        {THEME_ACCENT_OPTIONS.map((option) => {
          const text = copy.accent[option.id];
          return (
            <OptionCard
              key={option.id}
              id={`${prefix}-accent-${option.id}`}
              value={option.id}
              label={text.label}
              description={text.description}
              lead={
                <span
                  aria-hidden="true"
                  data-accent-swatch={option.id}
                  className="mt-0.5 size-5 shrink-0 rounded-full border border-border"
                  style={{ background: option.swatch }}
                />
              }
            />
          );
        })}
      </ChoiceGroup>

      <ChoiceGroup
        legend={copy.language.legend}
        value={locale}
        onValueChange={(value) => onLocaleChange(value as AccountLocale)}
      >
        {LOCALE_OPTIONS.map((option) => (
          <OptionCard
            key={option}
            id={`${prefix}-language-${option}`}
            value={option}
            label={copy.language[option]}
            lang={option}
          />
        ))}
      </ChoiceGroup>

      <SidebarSection
        copy={copy}
        sidebar={sidebar}
        save={sidebarSave}
        onChange={onSidebarCollapsedChange}
        onRetry={onRetryPreferences}
      />
    </div>
  );
}
