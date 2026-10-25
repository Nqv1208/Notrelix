import * as React from "react";
import { Check } from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import {
  BTN_PRIMARY,
  FOCUS,
  LoadError,
  Note,
  PageHead,
  Toast,
} from "../shared/account-parts";
import {
  fillCopy,
  type AccountSaveStatus,
} from "../shared/account-surface-model";
import type { AccountNotificationsCopy } from "./account-notifications.copy";

export const NOTIFICATION_EVENTS = [
  "assigned",
  "mention",
  "approval",
  "overdue",
  "watching",
  "invitation",
  "digest",
] as const;
export type AccountNotificationEvent = (typeof NOTIFICATION_EVENTS)[number];

/** The channels the producer delivers on; mobile and quiet hours are later decisions. */
export const NOTIFICATION_CHANNELS = ["inApp", "email"] as const;
export type AccountNotificationChannel = (typeof NOTIFICATION_CHANNELS)[number];

export type AccountChannelChoice = Record<AccountNotificationChannel, boolean>;

/**
 * The values the notifications page edits and saves: the member's choices in
 * one workspace, with the version the save must quote (`expectedVersion`).
 */
export interface AccountNotificationValues {
  readonly version: number;
  readonly rows: Readonly<
    Record<AccountNotificationEvent, Readonly<AccountChannelChoice>>
  >;
}

export type AccountNotificationsState =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | {
      readonly status: "ready";
      readonly values: AccountNotificationValues;
      /** The member's name for the mention note. */
      readonly personName: string;
    };

export interface AccountNotificationsSurfaceProps {
  readonly copy: AccountNotificationsCopy;
  readonly preferences: AccountNotificationsState;
  readonly save: AccountSaveStatus;
  readonly onSave: (values: AccountNotificationValues) => void;
  readonly onRetry: () => void;
}

function Checkbox({
  checked,
  label,
  disabled,
  onChange,
}: {
  checked: boolean;
  label: string;
  disabled: boolean;
  onChange: (next: boolean) => void;
}) {
  return (
    <button
      type="button"
      role="checkbox"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={cn(
        "mx-auto box-border grid size-5 place-items-center rounded-[5px] border-[1.5px] p-0 disabled:cursor-not-allowed disabled:opacity-70",
        checked
          ? "border-primary bg-primary text-primary-foreground"
          : "border-meta-foreground bg-popover text-primary-foreground dark:bg-card",
        FOCUS,
      )}
    >
      <Check
        aria-hidden="true"
        strokeWidth={2.6}
        className={cn("size-[13px]", checked ? "visible" : "invisible")}
      />
    </button>
  );
}

function Matrix({
  copy,
  values,
  personName,
  saving,
  onChange,
}: {
  copy: AccountNotificationsCopy;
  values: AccountNotificationValues;
  personName: string;
  saving: boolean;
  onChange: (
    event: AccountNotificationEvent,
    channel: AccountNotificationChannel,
    next: boolean,
  ) => void;
}) {
  return (
    <table
      aria-label={copy.matrixLabel}
      data-f="mx"
      className="w-full border-separate border-spacing-0 rounded-[12px] border border-line bg-popover dark:bg-card"
    >
      <thead>
        <tr>
          <th
            scope="col"
            className="h-[38px] rounded-tl-[12px] bg-background px-3.5 text-left text-xs leading-[1.45] font-semibold text-meta-foreground"
          >
            {copy.when}
          </th>
          {NOTIFICATION_CHANNELS.map((channel, index) => (
            <th
              key={channel}
              scope="col"
              className={cn(
                "h-[38px] w-[17%] bg-background px-3.5 text-center text-xs leading-[1.45] font-semibold text-meta-foreground max-sm:w-16 max-sm:px-1",
                index === NOTIFICATION_CHANNELS.length - 1 &&
                  "rounded-tr-[12px]",
              )}
            >
              {copy.channels[channel]}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {NOTIFICATION_EVENTS.map((event) => {
          const title = copy.events[event];
          const note =
            event in copy.eventNotes
              ? copy.eventNotes[event as keyof typeof copy.eventNotes]
              : null;
          return (
            <tr key={event}>
              <th
                scope="row"
                className="border-t border-line px-3.5 py-[7px] text-left text-nav leading-[1.45] font-medium text-foreground max-sm:px-2"
              >
                {title}
                {note ? (
                  <small className="block text-xs leading-[1.45] font-normal text-meta-foreground">
                    {fillCopy(note, { name: personName })}
                  </small>
                ) : null}
              </th>
              {NOTIFICATION_CHANNELS.map((channel) => (
                <td
                  key={channel}
                  className="w-[17%] border-t border-line px-3.5 py-[7px] text-center max-sm:w-16 max-sm:px-1"
                >
                  <Checkbox
                    checked={values.rows[event][channel]}
                    disabled={saving}
                    label={fillCopy(copy.cellLabel, {
                      event: title,
                      channel: copy.channels[channel],
                    })}
                    onChange={(next) => onChange(event, channel, next)}
                  />
                </td>
              ))}
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}

function Ready({
  copy,
  initial,
  personName,
  save,
  onSave,
}: {
  copy: AccountNotificationsCopy;
  initial: AccountNotificationValues;
  personName: string;
  save: AccountSaveStatus;
  onSave: (values: AccountNotificationValues) => void;
}) {
  const [values, setValues] = React.useState(initial);
  React.useEffect(() => setValues(initial), [initial]);
  const saving = save.state === "saving";
  return (
    <>
      <p className="m-0 text-crumb leading-[1.55] text-muted-foreground">
        {copy.workspaceScope}
      </p>
      <Matrix
        copy={copy}
        values={values}
        personName={personName}
        saving={saving}
        onChange={(event, channel, next) =>
          setValues((current) => ({
            ...current,
            rows: {
              ...current.rows,
              [event]: { ...current.rows[event], [channel]: next },
            },
          }))
        }
      />
      {save.state === "error" ? (
        <p
          role="alert"
          className="m-0 rounded-md bg-danger-subtle px-3 py-2 text-metadata text-danger-subtle-foreground"
        >
          {copy.saveError}
        </p>
      ) : null}
      <div className="flex items-center gap-2.5">
        <Note className="flex-1">{copy.mentionRule}</Note>
        <button
          type="button"
          disabled={saving}
          onClick={() => onSave(values)}
          className={cn(BTN_PRIMARY, FOCUS)}
        >
          {saving ? copy.saving : copy.save}
        </button>
      </div>
      {save.state === "saved" ? <Toast>{copy.saved}</Toast> : null}
    </>
  );
}

/**
 * Account notifications (Final-Account `thong-bao`): the matrix of events by
 * channel (in the app, email) for one workspace and the save. The
 * container owns the write and reports it through `save`.
 */
export function AccountNotificationsSurface({
  copy,
  preferences,
  save,
  onSave,
  onRetry,
}: AccountNotificationsSurfaceProps) {
  const titleId = React.useId();
  return (
    <section aria-labelledby={titleId} className="flex min-w-0 flex-col gap-4">
      <PageHead
        id={titleId}
        title={copy.title}
        description={copy.description}
      />
      {preferences.status === "loading" ? (
        <div
          role="status"
          aria-label={copy.loading}
          className="flex flex-col gap-3"
        >
          <Skeleton className="h-[330px] w-full rounded-[12px]" />
        </div>
      ) : preferences.status === "error" ? (
        <LoadError
          message={copy.loadError}
          retry={copy.retry}
          onRetry={onRetry}
        />
      ) : (
        <Ready
          copy={copy}
          initial={preferences.values}
          personName={preferences.personName}
          save={save}
          onSave={onSave}
        />
      )}
    </section>
  );
}
