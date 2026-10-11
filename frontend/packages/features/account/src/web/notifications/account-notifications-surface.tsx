import * as React from "react";
import { Check } from "lucide-react";
import {
  Button,
  Field,
  FieldDescription,
  FieldLabel,
  Skeleton,
  Switch,
} from "@notrelix/ui-web";
import type { AccountSaveStatus } from "../shared/account-surface-model";
import type { AccountNotificationsCopy } from "./account-notifications.copy";

/** The notification channels a member can turn on or off. */
export interface AccountNotificationValues {
  readonly emailEnabled: boolean;
  readonly pushEnabled: boolean;
}

/**
 * The saved preferences. When they cannot be read, `error` carries the
 * defaults the page shows so the member can still see and set the channels.
 */
export type AccountNotificationsState =
  | { readonly status: "loading" }
  | { readonly status: "ready"; readonly values: AccountNotificationValues }
  | { readonly status: "error"; readonly fallback: AccountNotificationValues };

export interface AccountNotificationsSurfaceProps {
  readonly copy: AccountNotificationsCopy;
  readonly preferences: AccountNotificationsState;
  /** The outcome of the last save; the container owns the mutation. */
  readonly save: AccountSaveStatus;
  readonly onSave: (values: AccountNotificationValues) => void;
  readonly onRetry: () => void;
}

const CHANNELS = [
  { id: "email", field: "emailEnabled" },
  { id: "push", field: "pushEnabled" },
] as const satisfies readonly {
  id: keyof AccountNotificationsCopy["channels"];
  field: keyof AccountNotificationValues;
}[];

function ChannelSwitch({
  label,
  description,
  checked,
  disabled,
  onCheckedChange,
}: {
  label: string;
  description: string;
  checked: boolean;
  disabled: boolean;
  onCheckedChange: (checked: boolean) => void;
}) {
  const id = React.useId();
  const descriptionId = React.useId();
  return (
    <Field
      orientation="horizontal"
      className="items-start justify-between gap-4 rounded-lg border border-border p-4"
    >
      <div className="flex min-w-0 flex-col gap-0.5">
        <FieldLabel htmlFor={id}>{label}</FieldLabel>
        <FieldDescription id={descriptionId}>{description}</FieldDescription>
      </div>
      <Switch
        id={id}
        checked={checked}
        disabled={disabled}
        aria-describedby={descriptionId}
        onCheckedChange={onCheckedChange}
      />
    </Field>
  );
}

function PreferencesForm({
  copy,
  base,
  save,
  onSave,
}: {
  copy: AccountNotificationsCopy;
  base: AccountNotificationValues;
  save: AccountSaveStatus;
  onSave: (values: AccountNotificationValues) => void;
}) {
  const [draft, setDraft] = React.useState<AccountNotificationValues>(base);

  // A refetch after a save brings the stored values back into the draft.
  React.useEffect(() => {
    setDraft({
      emailEnabled: base.emailEnabled,
      pushEnabled: base.pushEnabled,
    });
  }, [base.emailEnabled, base.pushEnabled]);

  const dirty =
    draft.emailEnabled !== base.emailEnabled ||
    draft.pushEnabled !== base.pushEnabled;
  const saving = save.state === "saving";

  return (
    <form
      noValidate
      aria-label={copy.title}
      onSubmit={(event) => {
        event.preventDefault();
        if (!dirty || saving) return;
        onSave(draft);
      }}
      className="flex min-w-0 flex-col gap-4"
    >
      <div className="flex min-w-0 flex-col gap-3">
        {CHANNELS.map(({ id, field }) => (
          <ChannelSwitch
            key={id}
            label={copy.channels[id].label}
            description={copy.channels[id].description}
            checked={draft[field]}
            disabled={saving}
            onCheckedChange={(checked) =>
              setDraft((current) => ({ ...current, [field]: checked }))
            }
          />
        ))}
      </div>

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
          disabled={saving || !dirty}
          className="h-auto min-h-8 max-w-full whitespace-normal"
        >
          {saving ? copy.saving : copy.save}
        </Button>
        {save.state === "saved" && !dirty ? (
          <span
            role="status"
            className="inline-flex min-w-0 items-center gap-1 text-metadata text-success-text"
          >
            <Check aria-hidden="true" className="size-3.5 shrink-0" />
            {copy.saved}
          </span>
        ) : null}
      </div>
    </form>
  );
}

function PreferencesSkeleton({ label }: { label: string }) {
  return (
    <div role="status" aria-label={label} className="flex flex-col gap-3">
      {Array.from({ length: 2 }, (_, index) => (
        <Skeleton key={index} className="h-16 w-full rounded-lg" />
      ))}
    </div>
  );
}

/**
 * Account notification preferences: one real switch per channel, saved
 * together. Saving is disabled until a switch changes; the container owns
 * the mutation and reports its outcome through `save`.
 */
export function AccountNotificationsSurface({
  copy,
  preferences,
  save,
  onSave,
  onRetry,
}: AccountNotificationsSurfaceProps) {
  return (
    <div className="mx-auto flex w-full min-w-0 max-w-2xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-page-title text-foreground">{copy.title}</h1>
        <p className="text-body text-muted-foreground">{copy.description}</p>
      </header>

      {preferences.status === "loading" ? (
        <PreferencesSkeleton label={copy.loading} />
      ) : (
        <>
          {preferences.status === "error" ? (
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
          ) : null}
          <PreferencesForm
            copy={copy}
            base={
              preferences.status === "ready"
                ? preferences.values
                : preferences.fallback
            }
            save={save}
            onSave={onSave}
          />
        </>
      )}
    </div>
  );
}
