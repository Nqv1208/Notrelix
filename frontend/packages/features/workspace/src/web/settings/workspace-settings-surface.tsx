import * as React from "react";
import { AlertTriangle, Check, Info } from "lucide-react";
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
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
  Switch,
  Textarea,
  cn,
} from "@notrelix/ui-web";
import { ASSIGNABLE_ROLES } from "../../core/model/members";
import {
  WORKSPACE_TIME_ZONES,
  type WorkspaceSettings,
} from "../../core/types/workspace";
import type { SectionState } from "../home/home-models";
import { formatCopy } from "../shell/shell-models";
import type { WorkspaceSettingsCopy } from "./workspace-settings.copy";

export interface WorkspaceProfileValues {
  readonly name: string;
  readonly description: string;
}

/** The outcome of the last write of one section. */
export type SaveStatus =
  | { readonly state: "idle" }
  | { readonly state: "saving" }
  | { readonly state: "saved" }
  | { readonly state: "conflict" }
  | { readonly state: "error"; readonly message: string };

export interface WorkspaceSettingsSurfaceProps {
  readonly copy: WorkspaceSettingsCopy;
  /** `null` while the workspace record loads. */
  readonly workspaceName: string | null;
  /** Admins and owners manage the workspace (`ManageWorkspace`). */
  readonly canManage: boolean;
  /**
   * The producer requires the workspace version on every write; when it is
   * not reported, saving is explained and turned off (open item WX-SET-001).
   */
  readonly versionAvailable: boolean;
  readonly profile: SectionState<WorkspaceProfileValues>;
  readonly defaults: SectionState<WorkspaceSettings>;
  readonly profileStatus: SaveStatus;
  readonly defaultsStatus: SaveStatus;
  readonly archiving: boolean;
  readonly onSaveProfile: (values: WorkspaceProfileValues) => void;
  readonly onSaveDefaults: (settings: WorkspaceSettings) => void;
  readonly onArchive: () => void;
  /** Re-reads the workspace after a conflict. */
  readonly onReload: () => void;
  readonly onRetryDefaults: () => void;
}

const NAME_MAX = 200;
const DESCRIPTION_MAX = 1024;

function SettingsSection({
  id,
  title,
  description,
  tone = "default",
  children,
}: {
  id: string;
  title: string;
  description: string;
  tone?: "default" | "danger";
  children: React.ReactNode;
}) {
  return (
    <section
      aria-labelledby={id}
      className="grid gap-4 border-t border-border py-8 first:border-t-0 first:pt-0 lg:grid-cols-[16rem_minmax(0,1fr)] lg:gap-10"
    >
      <div className="flex flex-col gap-1">
        <h2
          id={id}
          className={cn(
            "text-subheading",
            tone === "danger" ? "text-danger-text" : "text-foreground",
          )}
        >
          {title}
        </h2>
        <p className="text-metadata text-muted-foreground">{description}</p>
      </div>
      <div className="flex min-w-0 flex-col gap-5">{children}</div>
    </section>
  );
}

function SectionSkeleton({ label, fields }: { label: string; fields: number }) {
  return (
    <div role="status" aria-label={label} className="flex flex-col gap-5">
      {Array.from({ length: fields }, (_, index) => (
        <div key={index} className="flex flex-col gap-2">
          <Skeleton className="h-3.5 w-32" />
          <Skeleton className="h-9 w-full max-w-md" />
        </div>
      ))}
    </div>
  );
}

function SaveBar({
  copy,
  status,
  dirty,
  invalid,
  disabled,
  onDiscard,
  onReload,
}: {
  copy: WorkspaceSettingsCopy;
  status: SaveStatus;
  dirty: boolean;
  invalid: boolean;
  disabled: boolean;
  onDiscard: () => void;
  onReload: () => void;
}) {
  const saving = status.state === "saving";
  return (
    <div className="flex flex-col gap-3">
      {status.state === "conflict" ? (
        <div
          role="alert"
          className="flex flex-wrap items-center gap-3 rounded-md bg-warning-subtle px-3 py-2 text-metadata text-warning-subtle-foreground"
        >
          <AlertTriangle aria-hidden="true" className="size-4 shrink-0" />
          <span className="min-w-0 flex-1">{copy.conflict}</span>
          <Button size="xs" variant="outline" onClick={onReload}>
            {copy.reload}
          </Button>
        </div>
      ) : status.state === "error" ? (
        <p
          role="alert"
          className="rounded-md bg-danger-subtle px-3 py-2 text-metadata text-danger-subtle-foreground"
        >
          {status.message}
        </p>
      ) : null}
      <div className="flex flex-wrap items-center gap-2">
        <Button
          type="submit"
          size="sm"
          disabled={disabled || saving || !dirty || invalid}
        >
          {saving ? copy.saving : copy.save}
        </Button>
        {dirty && !saving ? (
          <Button type="button" size="sm" variant="ghost" onClick={onDiscard}>
            {copy.discard}
          </Button>
        ) : null}
        {status.state === "saved" && !dirty ? (
          <span
            role="status"
            className="inline-flex items-center gap-1 text-metadata text-success-text"
          >
            <Check aria-hidden="true" className="size-3.5" />
            {copy.saved}
          </span>
        ) : null}
      </div>
    </div>
  );
}

function ProfileForm({
  copy,
  values,
  status,
  locked,
  onSave,
  onReload,
}: {
  copy: WorkspaceSettingsCopy;
  values: WorkspaceProfileValues;
  status: SaveStatus;
  locked: boolean;
  onSave: (values: WorkspaceProfileValues) => void;
  onReload: () => void;
}) {
  const [name, setName] = React.useState(values.name);
  const [description, setDescription] = React.useState(values.description);
  const nameId = React.useId();
  const nameHelpId = React.useId();
  const nameErrorId = React.useId();
  const descriptionId = React.useId();
  const descriptionHelpId = React.useId();
  const descriptionErrorId = React.useId();

  React.useEffect(() => {
    setName(values.name);
    setDescription(values.description);
  }, [values.name, values.description]);

  const nameError =
    name.trim() === ""
      ? copy.profile.nameRequired
      : name.trim().length > NAME_MAX
        ? copy.profile.nameTooLong
        : null;
  const descriptionError =
    description.trim().length > DESCRIPTION_MAX
      ? copy.profile.descriptionTooLong
      : null;
  const dirty =
    name.trim() !== values.name.trim() ||
    description.trim() !== values.description.trim();
  const saving = status.state === "saving";

  return (
    <form
      noValidate
      aria-label={copy.profile.title}
      onSubmit={(event) => {
        event.preventDefault();
        if (locked || nameError || descriptionError || !dirty || saving) return;
        onSave({ name: name.trim(), description: description.trim() });
      }}
      className="flex flex-col gap-5"
    >
      <Field
        data-invalid={dirty && nameError ? true : undefined}
        className="gap-2"
      >
        <FieldLabel htmlFor={nameId}>{copy.profile.nameLabel}</FieldLabel>
        <Input
          id={nameId}
          value={name}
          disabled={locked || saving}
          maxLength={NAME_MAX + 50}
          aria-invalid={dirty && nameError ? true : undefined}
          aria-describedby={cn(nameHelpId, dirty && nameError && nameErrorId)}
          onChange={(event) => setName(event.target.value)}
          className="max-w-md"
        />
        <FieldDescription id={nameHelpId}>
          {copy.profile.nameHelp}
        </FieldDescription>
        {dirty && nameError ? (
          <FieldError id={nameErrorId}>{nameError}</FieldError>
        ) : null}
      </Field>
      <Field
        data-invalid={descriptionError ? true : undefined}
        className="gap-2"
      >
        <FieldLabel htmlFor={descriptionId}>
          {copy.profile.descriptionLabel}
        </FieldLabel>
        <Textarea
          id={descriptionId}
          value={description}
          rows={4}
          disabled={locked || saving}
          placeholder={copy.profile.descriptionPlaceholder}
          aria-invalid={descriptionError ? true : undefined}
          aria-describedby={cn(
            descriptionHelpId,
            descriptionError && descriptionErrorId,
          )}
          onChange={(event) => setDescription(event.target.value)}
          className="max-w-xl resize-y"
        />
        <div className="flex max-w-xl items-start justify-between gap-3">
          <FieldDescription id={descriptionHelpId}>
            {copy.profile.descriptionHelp}
          </FieldDescription>
          <span
            aria-hidden="true"
            className={cn(
              "shrink-0 text-metadata tabular-nums",
              descriptionError ? "text-danger-text" : "text-muted-foreground",
            )}
          >
            {formatCopy(copy.profile.counter, {
              count: description.trim().length,
              max: DESCRIPTION_MAX,
            })}
          </span>
        </div>
        {descriptionError ? (
          <FieldError id={descriptionErrorId}>{descriptionError}</FieldError>
        ) : null}
      </Field>
      {locked ? null : (
        <SaveBar
          copy={copy}
          status={status}
          dirty={dirty}
          invalid={Boolean(nameError || descriptionError)}
          disabled={locked}
          onDiscard={() => {
            setName(values.name);
            setDescription(values.description);
          }}
          onReload={onReload}
        />
      )}
    </form>
  );
}

function SwitchField({
  label,
  help,
  checked,
  disabled,
  onCheckedChange,
}: {
  label: string;
  help: string;
  checked: boolean;
  disabled: boolean;
  onCheckedChange: (checked: boolean) => void;
}) {
  const id = React.useId();
  const helpId = React.useId();
  return (
    <Field
      orientation="horizontal"
      className="items-start justify-between gap-4"
    >
      <div className="flex min-w-0 flex-col gap-0.5">
        <FieldLabel htmlFor={id}>{label}</FieldLabel>
        <FieldDescription id={helpId}>{help}</FieldDescription>
      </div>
      <Switch
        id={id}
        checked={checked}
        disabled={disabled}
        aria-describedby={helpId}
        onCheckedChange={onCheckedChange}
      />
    </Field>
  );
}

function DefaultsForm({
  copy,
  values,
  status,
  locked,
  onSave,
  onReload,
}: {
  copy: WorkspaceSettingsCopy;
  values: WorkspaceSettings;
  status: SaveStatus;
  locked: boolean;
  onSave: (settings: WorkspaceSettings) => void;
  onReload: () => void;
}) {
  const [draft, setDraft] = React.useState<WorkspaceSettings>(values);
  const [expiryText, setExpiryText] = React.useState(
    String(values.invitationExpiryDays),
  );
  const roleId = React.useId();
  const roleHelpId = React.useId();
  const expiryId = React.useId();
  const expiryHelpId = React.useId();
  const expiryErrorId = React.useId();
  const zoneId = React.useId();
  const zoneHelpId = React.useId();
  const weekId = React.useId();

  React.useEffect(() => {
    setDraft(values);
    setExpiryText(String(values.invitationExpiryDays));
  }, [values]);

  const expiry = Number(expiryText);
  const expiryValid =
    /^\d+$/.test(expiryText.trim()) && expiry >= 1 && expiry <= 30;
  const next: WorkspaceSettings = {
    ...draft,
    invitationExpiryDays: expiryValid ? expiry : draft.invitationExpiryDays,
  };
  const dirty =
    next.defaultMemberRole !== values.defaultMemberRole ||
    next.invitationExpiryDays !== values.invitationExpiryDays ||
    next.allowGuestInvites !== values.allowGuestInvites ||
    next.allowPublicSharing !== values.allowPublicSharing ||
    next.enforceMfa !== values.enforceMfa ||
    next.timeZone !== values.timeZone ||
    next.weekStart !== values.weekStart ||
    expiryText.trim() !== String(values.invitationExpiryDays);
  const saving = status.state === "saving";
  const disabled = locked || saving;

  return (
    <form
      noValidate
      aria-label={copy.defaults.title}
      onSubmit={(event) => {
        event.preventDefault();
        if (locked || !expiryValid || !dirty || saving) return;
        onSave(next);
      }}
      className="flex flex-col gap-5"
    >
      <Field className="gap-2">
        <FieldLabel htmlFor={roleId}>{copy.defaults.roleLabel}</FieldLabel>
        <Select
          value={draft.defaultMemberRole}
          disabled={disabled}
          onValueChange={(value) =>
            setDraft((current) => ({
              ...current,
              defaultMemberRole:
                value as WorkspaceSettings["defaultMemberRole"],
            }))
          }
        >
          <SelectTrigger
            id={roleId}
            aria-describedby={roleHelpId}
            className="max-w-48"
          >
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {ASSIGNABLE_ROLES.map((role) => (
              <SelectItem key={role} value={role}>
                {copy.roles[role]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <FieldDescription id={roleHelpId}>
          {copy.defaults.roleHelp}
        </FieldDescription>
      </Field>
      <Field data-invalid={!expiryValid ? true : undefined} className="gap-2">
        <FieldLabel htmlFor={expiryId}>{copy.defaults.expiryLabel}</FieldLabel>
        <Input
          id={expiryId}
          inputMode="numeric"
          value={expiryText}
          disabled={disabled}
          aria-invalid={!expiryValid ? true : undefined}
          aria-describedby={cn(expiryHelpId, !expiryValid && expiryErrorId)}
          onChange={(event) => setExpiryText(event.target.value)}
          className="max-w-24 tabular-nums"
        />
        <FieldDescription id={expiryHelpId}>
          {copy.defaults.expiryHelp}
        </FieldDescription>
        {!expiryValid ? (
          <FieldError id={expiryErrorId}>
            {copy.defaults.expiryInvalid}
          </FieldError>
        ) : null}
      </Field>
      <Field className="gap-2">
        <FieldLabel htmlFor={zoneId}>{copy.defaults.timeZoneLabel}</FieldLabel>
        <Select
          value={draft.timeZone}
          disabled={disabled}
          onValueChange={(value) =>
            setDraft((current) => ({
              ...current,
              timeZone: value as WorkspaceSettings["timeZone"],
            }))
          }
        >
          <SelectTrigger
            id={zoneId}
            aria-describedby={zoneHelpId}
            className="max-w-64"
          >
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {WORKSPACE_TIME_ZONES.map((zone) => (
              <SelectItem key={zone} value={zone}>
                {copy.timeZones[zone]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <FieldDescription id={zoneHelpId}>
          {copy.defaults.timeZoneHelp}
        </FieldDescription>
      </Field>
      <Field className="gap-2">
        <FieldLabel htmlFor={weekId}>{copy.defaults.weekStartLabel}</FieldLabel>
        <Select
          value={draft.weekStart}
          disabled={disabled}
          onValueChange={(value) =>
            setDraft((current) => ({
              ...current,
              weekStart: value as WorkspaceSettings["weekStart"],
            }))
          }
        >
          <SelectTrigger id={weekId} className="max-w-48">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="monday">{copy.weekDays.monday}</SelectItem>
            <SelectItem value="sunday">{copy.weekDays.sunday}</SelectItem>
          </SelectContent>
        </Select>
      </Field>
      <div className="flex max-w-xl flex-col gap-4">
        <SwitchField
          label={copy.defaults.guestsLabel}
          help={copy.defaults.guestsHelp}
          checked={draft.allowGuestInvites}
          disabled={disabled}
          onCheckedChange={(checked) =>
            setDraft((current) => ({ ...current, allowGuestInvites: checked }))
          }
        />
        <SwitchField
          label={copy.defaults.sharingLabel}
          help={copy.defaults.sharingHelp}
          checked={draft.allowPublicSharing}
          disabled={disabled}
          onCheckedChange={(checked) =>
            setDraft((current) => ({ ...current, allowPublicSharing: checked }))
          }
        />
        <SwitchField
          label={copy.defaults.mfaLabel}
          help={copy.defaults.mfaHelp}
          checked={draft.enforceMfa}
          disabled={disabled}
          onCheckedChange={(checked) =>
            setDraft((current) => ({ ...current, enforceMfa: checked }))
          }
        />
      </div>
      {locked ? null : (
        <SaveBar
          copy={copy}
          status={status}
          dirty={dirty}
          invalid={!expiryValid}
          disabled={locked}
          onDiscard={() => {
            setDraft(values);
            setExpiryText(String(values.invitationExpiryDays));
          }}
          onReload={onReload}
        />
      )}
    </form>
  );
}

function ArchiveFlow({
  copy,
  workspaceName,
  disabled,
  archiving,
  onArchive,
}: {
  copy: WorkspaceSettingsCopy;
  workspaceName: string;
  disabled: boolean;
  archiving: boolean;
  onArchive: () => void;
}) {
  const [open, setOpen] = React.useState(false);
  const [typed, setTyped] = React.useState("");
  const inputId = React.useId();
  const mismatchId = React.useId();
  const matches = typed.trim() === workspaceName.trim() && workspaceName !== "";
  const values = { workspace: workspaceName };

  return (
    <div className="flex flex-col gap-3 rounded-lg border border-danger-subtle p-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex min-w-0 flex-col gap-0.5">
        <h3 className="text-label text-foreground">
          {copy.danger.archiveTitle}
        </h3>
        <p className="text-metadata text-muted-foreground">
          {formatCopy(copy.danger.archiveDescription, values)}
        </p>
      </div>
      <Button
        variant="destructive"
        size="sm"
        disabled={disabled || archiving}
        onClick={() => {
          setTyped("");
          setOpen(true);
        }}
        className="shrink-0"
      >
        {archiving ? copy.danger.archiving : copy.danger.archiveAction}
      </Button>
      <AlertDialog open={open} onOpenChange={setOpen}>
        <AlertDialogContent>
          <form
            noValidate
            onSubmit={(event) => {
              event.preventDefault();
              if (!matches) return;
              setOpen(false);
              onArchive();
            }}
            className="flex flex-col gap-4"
          >
            <AlertDialogHeader>
              <AlertDialogTitle>
                {formatCopy(copy.danger.confirmTitle, values)}
              </AlertDialogTitle>
              <AlertDialogDescription>
                {copy.danger.confirmDescription}
              </AlertDialogDescription>
            </AlertDialogHeader>
            <Field data-invalid={typed && !matches ? true : undefined}>
              <FieldLabel htmlFor={inputId}>
                {copy.danger.confirmLabel}
              </FieldLabel>
              <Input
                id={inputId}
                value={typed}
                autoComplete="off"
                placeholder={workspaceName}
                aria-describedby={typed && !matches ? mismatchId : undefined}
                onChange={(event) => setTyped(event.target.value)}
              />
              {typed && !matches ? (
                <FieldError id={mismatchId}>
                  {formatCopy(copy.danger.confirmMismatch, values)}
                </FieldError>
              ) : null}
            </Field>
            <AlertDialogFooter>
              <AlertDialogCancel type="button">
                {copy.danger.cancel}
              </AlertDialogCancel>
              <Button type="submit" variant="destructive" disabled={!matches}>
                {copy.danger.confirmAction}
              </Button>
            </AlertDialogFooter>
          </form>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

/**
 * Workspace settings (WX-SET-001) in grouped sections — Profile, Defaults,
 * Danger zone — each saved on its own with bound labels, inline validation
 * and conflict recovery. Archiving confirms by typing the workspace name.
 */
export function WorkspaceSettingsSurface({
  copy,
  workspaceName,
  canManage,
  versionAvailable,
  profile,
  defaults,
  profileStatus,
  defaultsStatus,
  archiving,
  onSaveProfile,
  onSaveDefaults,
  onArchive,
  onReload,
  onRetryDefaults,
}: WorkspaceSettingsSurfaceProps) {
  const locked = !canManage || !versionAvailable;
  const notice = !canManage
    ? copy.readOnly
    : !versionAvailable
      ? copy.versionUnavailable
      : null;

  return (
    <div className="mx-auto flex w-full max-w-5xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-page-title text-foreground">{copy.title}</h1>
        <p className="text-body text-muted-foreground">
          {workspaceName
            ? formatCopy(copy.description, { workspace: workspaceName })
            : copy.descriptionLoading}
        </p>
      </header>

      {notice ? (
        <p className="flex items-start gap-2 rounded-md bg-info-subtle px-3 py-2 text-metadata text-info-subtle-foreground">
          <Info aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
          {notice}
        </p>
      ) : null}

      <div className="flex flex-col">
        <SettingsSection
          id="settings-profile"
          title={copy.profile.title}
          description={copy.profile.description}
        >
          {profile.status === "loading" ? (
            <SectionSkeleton label={copy.profile.title} fields={2} />
          ) : profile.status === "error" ? (
            <p role="alert" className="text-body text-muted-foreground">
              {copy.loadError}
            </p>
          ) : (
            <ProfileForm
              copy={copy}
              values={profile.data}
              status={profileStatus}
              locked={locked}
              onSave={onSaveProfile}
              onReload={onReload}
            />
          )}
        </SettingsSection>

        <SettingsSection
          id="settings-defaults"
          title={copy.defaults.title}
          description={copy.defaults.description}
        >
          {defaults.status === "loading" ? (
            <SectionSkeleton label={copy.defaults.title} fields={3} />
          ) : defaults.status === "error" ? (
            <div
              role="alert"
              className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-border px-3 py-2"
            >
              <p className="text-metadata text-muted-foreground">
                {copy.loadError}
              </p>
              <Button size="xs" variant="outline" onClick={onRetryDefaults}>
                {copy.retry}
              </Button>
            </div>
          ) : (
            <DefaultsForm
              copy={copy}
              values={defaults.data}
              status={defaultsStatus}
              locked={locked}
              onSave={onSaveDefaults}
              onReload={onReload}
            />
          )}
        </SettingsSection>

        {canManage ? (
          <SettingsSection
            id="settings-danger"
            title={copy.danger.title}
            description={copy.danger.description}
            tone="danger"
          >
            <ArchiveFlow
              copy={copy}
              workspaceName={workspaceName ?? ""}
              disabled={!versionAvailable || !workspaceName}
              archiving={archiving}
              onArchive={onArchive}
            />
          </SettingsSection>
        ) : null}
      </div>
    </div>
  );
}
