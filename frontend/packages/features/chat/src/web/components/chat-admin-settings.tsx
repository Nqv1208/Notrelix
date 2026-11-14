import { useId, useState } from "react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
  Skeleton,
} from "@notrelix/ui-web";
import {
  CHAT_RETENTION_DAYS,
  type ChatExport,
  type ChatRetentionMode,
  type ChatRetentionPolicy,
} from "../../core/types/chat";
import {
  canDownload,
  draftOf,
  formatBytes,
  isDirty,
  isExportPending,
  needsPurgeConfirmation,
  parseDays,
  type RetentionDraft,
} from "../../core/model/retention-model";
import type { ChatCopy } from "./chat.copy";
import { ConfirmDialog } from "./chat-dialogs";
import { fillCopy } from "./use-chat-copy";

export interface RetentionSectionProps {
  readonly copy: ChatCopy;
  readonly policy: ChatRetentionPolicy | undefined;
  readonly loading: boolean;
  readonly loadFailed: boolean;
  readonly busy: boolean;
  /** An error or notice from the last save, already worded. */
  readonly message: {
    readonly tone: "error" | "ok";
    readonly text: string;
  } | null;
  readonly onRetry: () => void;
  readonly onSave: (input: {
    retentionDays: number | null;
    mode: ChatRetentionMode;
    expectedVersion: number;
  }) => void;
}

/**
 * The Workspace retention rule (UC-CHT-H05). Only the Owner edits it; everyone else who opens the settings reads the rule. A purge
 * asks for a confirmation that says what cannot be undone.
 */
export function RetentionSection(props: RetentionSectionProps) {
  const { copy, policy, loading, loadFailed, onRetry } = props;
  const text = copy.admin.retention;
  return (
    <section aria-labelledby="chat-retention-heading" className="space-y-3">
      <h3 id="chat-retention-heading" className="text-base font-semibold">
        {text.heading}
      </h3>
      <p className="text-sm text-muted-foreground">{text.description}</p>
      {loading ? (
        <div role="status" aria-label={text.loading}>
          <Skeleton className="h-24 w-full" />
        </div>
      ) : loadFailed || !policy ? (
        <div role="alert" className="flex items-center gap-3 text-sm">
          <span>{text.loadFailed}</span>
          <Button type="button" variant="outline" size="sm" onClick={onRetry}>
            {text.retry}
          </Button>
        </div>
      ) : (
        <RetentionForm key={policy.version} {...props} policy={policy} />
      )}
    </section>
  );
}

function RetentionForm({
  copy,
  policy,
  busy,
  message,
  onSave,
}: RetentionSectionProps & { readonly policy: ChatRetentionPolicy }) {
  const text = copy.admin.retention;
  const [draft, setDraft] = useState<RetentionDraft>(() => draftOf(policy));
  const [confirming, setConfirming] = useState(false);
  const daysId = useId();
  const hintId = useId();
  const readOnly = !policy.canChange;
  const days = parseDays(draft.days);
  const invalid = days === "invalid";
  const dirty = isDirty(policy, draft);

  function send() {
    if (days === "invalid") return;
    onSave({
      retentionDays: days,
      mode: draft.mode,
      expectedVersion: policy.version,
    });
  }

  return (
    <form
      className="space-y-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (readOnly || busy || invalid || !dirty) return;
        if (needsPurgeConfirmation(policy, draft)) setConfirming(true);
        else send();
      }}
    >
      <p className="text-sm">
        {policy.retentionDays === null
          ? text.currentNone
          : fillCopy(text.current, {
              days: policy.retentionDays,
              mode: text.modeName[policy.mode],
            })}
      </p>
      {readOnly ? (
        <p role="note" className="text-sm text-muted-foreground">
          {text.readOnly}
        </p>
      ) : null}
      <div className="space-y-1.5">
        <Label htmlFor={daysId}>{text.daysLabel}</Label>
        <Input
          id={daysId}
          inputMode="numeric"
          className="max-w-40"
          value={draft.days}
          disabled={readOnly || busy}
          aria-invalid={invalid}
          aria-describedby={hintId}
          onChange={(event) => setDraft({ ...draft, days: event.target.value })}
        />
        <p
          id={hintId}
          className={
            invalid
              ? "text-xs text-destructive"
              : "text-xs text-muted-foreground"
          }
        >
          {invalid
            ? text.daysInvalid
            : `${text.daysHint} (${CHAT_RETENTION_DAYS.min}-${CHAT_RETENTION_DAYS.max})`}
        </p>
      </div>
      <fieldset className="space-y-2" disabled={readOnly || busy}>
        <legend className="text-sm font-medium">{text.modeLabel}</legend>
        {(["tombstone", "purge"] as const).map((mode) => (
          <label key={mode} className="flex items-start gap-2 text-sm">
            <input
              type="radio"
              name="chat-retention-mode"
              className="mt-1"
              checked={draft.mode === mode}
              onChange={() => setDraft({ ...draft, mode })}
            />
            <span>
              <span className="block font-medium">{text[mode]}</span>
              <span className="block text-xs text-muted-foreground">
                {text[mode === "purge" ? "purgeHint" : "tombstoneHint"]}
              </span>
            </span>
          </label>
        ))}
      </fieldset>
      {message ? (
        <p
          role={message.tone === "error" ? "alert" : "status"}
          className={
            message.tone === "error"
              ? "text-sm text-destructive"
              : "text-sm text-muted-foreground"
          }
        >
          {message.text}
        </p>
      ) : null}
      {readOnly ? null : (
        <Button type="submit" disabled={busy || invalid || !dirty}>
          {text.save}
        </Button>
      )}
      <ConfirmDialog
        open={confirming}
        title={text.confirmTitle}
        body={fillCopy(text.confirmBody, {
          days: days === "invalid" || days === null ? "" : days,
        })}
        confirmLabel={text.confirm}
        cancelLabel={text.cancel}
        destructive
        busy={busy}
        onOpenChange={setConfirming}
        onConfirm={() => {
          setConfirming(false);
          send();
        }}
      />
    </form>
  );
}

export interface ExportSectionProps {
  readonly copy: ChatCopy;
  readonly people: readonly { readonly id: string; readonly name: string }[];
  /** The export being followed (its status is polled by the host); null before the first request. */
  readonly job: ChatExport | null;
  readonly busy: boolean;
  readonly downloading: boolean;
  readonly error: string | null;
  /** How a time is shown (the host's locale and zone). */
  readonly formatTime: (iso: string) => string;
  readonly onRequest: (input: {
    subjectUserId: string;
    rangeFrom: string | null;
    rangeTo: string | null;
  }) => void;
  readonly onDownload: (job: ChatExport) => void;
}

/** The export of one person's chat data (UC-CHT-H06): who and the range, then the status of the job and the download. */
export function ExportSection({
  copy,
  people,
  job,
  busy,
  downloading,
  error,
  formatTime,
  onRequest,
  onDownload,
}: ExportSectionProps) {
  const text = copy.admin.export;
  const [person, setPerson] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const personId = useId();
  const fromId = useId();
  const toId = useId();
  const rangeBad = from !== "" && to !== "" && to <= from;

  const statusText = job
    ? job.status === "queued"
      ? text.queued
      : job.status === "running"
        ? text.running
        : job.status === "expired"
          ? text.expired
          : job.status === "failed"
            ? (text.failedReasons[
                (job.failureCode ?? "") as keyof typeof text.failedReasons
              ] ?? text.failed)
            : fillCopy(text.ready, {
                messages: job.messageCount,
                spaces: job.spaceCount,
                size: formatBytes(job.sizeBytes),
              })
    : null;

  return (
    <section aria-labelledby="chat-export-heading" className="space-y-3">
      <h3 id="chat-export-heading" className="text-base font-semibold">
        {text.heading}
      </h3>
      <p className="text-sm text-muted-foreground">{text.description}</p>
      <form
        className="space-y-3"
        onSubmit={(event) => {
          event.preventDefault();
          if (busy || person === "" || rangeBad) return;
          onRequest({
            subjectUserId: person,
            rangeFrom: from === "" ? null : new Date(from).toISOString(),
            rangeTo: to === "" ? null : new Date(to).toISOString(),
          });
        }}
      >
        <div className="space-y-1.5">
          <Label htmlFor={personId}>{text.personLabel}</Label>
          <select
            id={personId}
            value={person}
            disabled={busy}
            onChange={(event) => setPerson(event.target.value)}
            className="h-9 w-full max-w-sm rounded-md border border-input bg-background px-2 text-sm text-foreground focus-visible:outline-2 focus-visible:outline-ring disabled:opacity-60"
          >
            <option value="">{text.personPlaceholder}</option>
            {people.map((entry) => (
              <option key={entry.id} value={entry.id}>
                {entry.name}
              </option>
            ))}
          </select>
        </div>
        <div className="flex flex-wrap gap-3">
          <div className="space-y-1.5">
            <Label htmlFor={fromId}>{text.fromLabel}</Label>
            <Input
              id={fromId}
              type="date"
              value={from}
              disabled={busy}
              onChange={(event) => setFrom(event.target.value)}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor={toId}>{text.toLabel}</Label>
            <Input
              id={toId}
              type="date"
              value={to}
              disabled={busy}
              aria-invalid={rangeBad}
              onChange={(event) => setTo(event.target.value)}
            />
          </div>
        </div>
        {rangeBad ? (
          <p role="alert" className="text-xs text-destructive">
            {text.rangeInvalid}
          </p>
        ) : null}
        {error ? (
          <p role="alert" className="text-sm text-destructive">
            {error}
          </p>
        ) : null}
        <Button type="submit" disabled={busy || person === "" || rangeBad}>
          {text.request}
        </Button>
      </form>
      {job && statusText ? (
        <div role="status" className="space-y-2 text-sm">
          <p>{statusText}</p>
          {job.status === "ready" ? (
            <>
              {job.excludedSpaceCount > 0 || job.tombstoneCount > 0 ? (
                <p className="text-muted-foreground">
                  {fillCopy(text.readyOmitted, {
                    excluded: job.excludedSpaceCount,
                    tombstones: job.tombstoneCount,
                  })}
                </p>
              ) : null}
              {job.expiresAt ? (
                <p className="text-muted-foreground">
                  {fillCopy(text.expiresAt, {
                    time: formatTime(job.expiresAt),
                  })}
                </p>
              ) : null}
            </>
          ) : null}
          {isExportPending(job) ? null : canDownload(job) ? (
            <Button
              type="button"
              variant="outline"
              disabled={downloading}
              onClick={() => onDownload(job)}
            >
              {text.download}
            </Button>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}

export interface ChatAdminSettingsDialogProps {
  readonly open: boolean;
  readonly copy: ChatCopy;
  readonly onOpenChange: (open: boolean) => void;
  readonly retention: RetentionSectionProps;
  /** Owner and Admin export; the section is left out for anyone else. */
  readonly exporter: ExportSectionProps | null;
}

/** The Chat admin settings of the Workspace: the retention rule and the export of a person's chat data. */
export function ChatAdminSettingsDialog({
  open,
  copy,
  onOpenChange,
  retention,
  exporter,
}: ChatAdminSettingsDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>{copy.admin.title}</DialogTitle>
          <DialogDescription>{copy.admin.description}</DialogDescription>
        </DialogHeader>
        <div className="space-y-8">
          <RetentionSection {...retention} />
          {exporter ? <ExportSection {...exporter} /> : null}
        </div>
      </DialogContent>
    </Dialog>
  );
}
