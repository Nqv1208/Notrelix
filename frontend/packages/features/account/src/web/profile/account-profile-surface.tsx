import * as React from "react";
import { Check } from "lucide-react";
import {
  Button,
  Field,
  FieldDescription,
  FieldError,
  FieldLabel,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import {
  fillCopy,
  type AccountFormatters,
  type AccountSaveStatus,
  type AccountSection,
} from "../shared/account-surface-model";
import type { AccountProfileCopy } from "./account-profile.copy";

/** The fields the profile page edits. */
export interface AccountProfileValues {
  readonly name: string;
  readonly timezone: string;
  readonly locale: string;
}

/** A loaded profile: the editable values plus the read-only email and join date. */
export interface AccountProfileData extends AccountProfileValues {
  readonly email: string;
  /** ISO timestamp; `null` hides the "member since" line. */
  readonly createdAt: string | null;
}

export interface AccountProfileSurfaceProps {
  readonly copy: AccountProfileCopy;
  readonly formatters: AccountFormatters;
  readonly profile: AccountSection<AccountProfileData>;
  /** The outcome of the last save; the container owns the mutation. */
  readonly save: AccountSaveStatus;
  readonly onSave: (values: AccountProfileValues) => void;
  readonly onRetry: () => void;
}

const TIMEZONES = [
  "UTC",
  "America/New_York",
  "America/Chicago",
  "America/Denver",
  "America/Los_Angeles",
  "Europe/London",
  "Europe/Paris",
  "Asia/Tokyo",
  "Asia/Ho_Chi_Minh",
] as const;

const LOCALES = ["en", "vi"] as const;

function lookup(labels: Readonly<Record<string, string>>, id: string): string {
  return labels[id] ?? id;
}

/** The listed choices, plus the saved value when it is not one of them. */
function choicesWith(listed: readonly string[], current: string): string[] {
  return current && !listed.includes(current)
    ? [...listed, current]
    : [...listed];
}

function ProfileForm({
  copy,
  formatters,
  data,
  save,
  onSave,
}: {
  copy: AccountProfileCopy;
  formatters: AccountFormatters;
  data: AccountProfileData;
  save: AccountSaveStatus;
  onSave: (values: AccountProfileValues) => void;
}) {
  const [name, setName] = React.useState(data.name);
  const [timezone, setTimezone] = React.useState(data.timezone);
  const [locale, setLocale] = React.useState(data.locale);
  const nameId = React.useId();
  const nameErrorId = React.useId();
  const emailId = React.useId();
  const emailHelpId = React.useId();
  const timezoneId = React.useId();
  const localeId = React.useId();

  // A refetch after a save brings the stored values back into the draft.
  React.useEffect(() => {
    setName(data.name);
    setTimezone(data.timezone);
    setLocale(data.locale);
  }, [data.name, data.timezone, data.locale]);

  const trimmed = name.trim();
  const nameError = trimmed === "" ? copy.nameRequired : null;
  const dirty =
    trimmed !== data.name.trim() ||
    timezone !== data.timezone ||
    locale !== data.locale;
  const saving = save.state === "saving";

  return (
    <form
      noValidate
      aria-label={copy.title}
      onSubmit={(event) => {
        event.preventDefault();
        if (nameError || !dirty || saving) return;
        onSave({ name: trimmed, timezone, locale });
      }}
      className="flex min-w-0 flex-col gap-5"
    >
      <Field
        data-invalid={dirty && nameError ? true : undefined}
        className="gap-2"
      >
        <FieldLabel htmlFor={nameId}>{copy.nameLabel}</FieldLabel>
        <Input
          id={nameId}
          value={name}
          disabled={saving}
          placeholder={copy.namePlaceholder}
          autoComplete="name"
          aria-invalid={nameError ? true : undefined}
          aria-describedby={nameError ? nameErrorId : undefined}
          onChange={(event) => setName(event.target.value)}
        />
        {nameError ? (
          <FieldError id={nameErrorId}>{nameError}</FieldError>
        ) : null}
      </Field>

      <Field className="gap-2">
        <FieldLabel htmlFor={emailId}>{copy.emailLabel}</FieldLabel>
        <Input
          id={emailId}
          value={data.email}
          disabled
          readOnly
          aria-describedby={emailHelpId}
          className="min-w-0 text-muted-foreground"
        />
        <FieldDescription id={emailHelpId}>{copy.emailHelp}</FieldDescription>
      </Field>

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
        <Field className="min-w-0 gap-2">
          <FieldLabel htmlFor={timezoneId}>{copy.timezoneLabel}</FieldLabel>
          <Select
            value={timezone}
            disabled={saving}
            onValueChange={setTimezone}
          >
            <SelectTrigger id={timezoneId} className="w-full min-w-0">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {choicesWith(TIMEZONES, timezone).map((zone) => (
                <SelectItem key={zone} value={zone}>
                  {lookup(copy.timezones, zone)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Field className="min-w-0 gap-2">
          <FieldLabel htmlFor={localeId}>{copy.localeLabel}</FieldLabel>
          <Select value={locale} disabled={saving} onValueChange={setLocale}>
            <SelectTrigger id={localeId} className="w-full min-w-0">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {choicesWith(LOCALES, locale).map((option) => (
                <SelectItem key={option} value={option}>
                  {lookup(copy.locales, option)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
      </div>

      {data.createdAt ? (
        <p className="text-metadata text-muted-foreground">
          {fillCopy(copy.memberSince, {
            date: formatters.date(data.createdAt),
          })}
        </p>
      ) : null}

      {save.state === "error" ? (
        <p
          role="alert"
          className="rounded-md bg-danger-subtle px-3 py-2 text-metadata text-danger-subtle-foreground"
        >
          {copy.saveError}
        </p>
      ) : null}

      <div className="flex flex-wrap items-center gap-2">
        <Button
          type="submit"
          size="sm"
          disabled={saving || !dirty || nameError !== null}
          className="h-auto min-h-8 max-w-full whitespace-normal"
        >
          {saving ? copy.saving : copy.save}
        </Button>
        {dirty && !saving ? (
          <Button
            type="button"
            size="sm"
            variant="ghost"
            className="h-auto min-h-8 max-w-full whitespace-normal"
            onClick={() => {
              setName(data.name);
              setTimezone(data.timezone);
              setLocale(data.locale);
            }}
          >
            {copy.discard}
          </Button>
        ) : null}
        {save.state === "saved" && !dirty ? (
          <span
            role="status"
            className={cn(
              "inline-flex min-w-0 items-center gap-1 text-metadata text-success-text",
            )}
          >
            <Check aria-hidden="true" className="size-3.5 shrink-0" />
            {copy.saved}
          </span>
        ) : null}
      </div>
    </form>
  );
}

function ProfileSkeleton({ label }: { label: string }) {
  return (
    <div role="status" aria-label={label} className="flex flex-col gap-5">
      {Array.from({ length: 3 }, (_, index) => (
        <div key={index} className="flex flex-col gap-2">
          <Skeleton className="h-3.5 w-28" />
          <Skeleton className="h-9 w-full" />
        </div>
      ))}
    </div>
  );
}

/**
 * Account profile: name, read-only email, time zone and language. Saving is
 * disabled until a field changes and the name is present; the container owns
 * the mutation and reports its outcome through `save`.
 */
export function AccountProfileSurface({
  copy,
  formatters,
  profile,
  save,
  onSave,
  onRetry,
}: AccountProfileSurfaceProps) {
  return (
    <div className="mx-auto flex w-full min-w-0 max-w-2xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-page-title text-foreground">{copy.title}</h1>
        <p className="text-body text-muted-foreground">{copy.description}</p>
      </header>

      {profile.status === "loading" ? (
        <ProfileSkeleton label={copy.loading} />
      ) : profile.status === "error" ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-border px-3 py-2"
        >
          <p className="min-w-0 text-body text-muted-foreground">
            {copy.loadError}
          </p>
          <Button size="xs" variant="outline" onClick={onRetry}>
            {copy.retry}
          </Button>
        </div>
      ) : (
        <ProfileForm
          copy={copy}
          formatters={formatters}
          data={profile.data}
          save={save}
          onSave={onSave}
        />
      )}
    </div>
  );
}
