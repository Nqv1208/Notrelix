import * as React from "react";
import { Lock } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import {
  Avatar,
  BTN,
  BTN_PRIMARY,
  FOCUS,
  INPUT,
  LINK_BUTTON_DANGER,
  LoadError,
  PageHead,
  Toast,
} from "../shared/account-parts";
import {
  fillCopy,
  pluralCopy,
  type AccountFormatters,
  type AccountSaveStatus,
  type AccountSection,
} from "../shared/account-surface-model";
import { checkPhone, phoneForSave } from "../../core/phone";
import type { AccountPhotoFailure } from "../../core/types/account";
import type { AccountProfileCopy } from "./account-profile.copy";

/** The fields the profile page edits. */
export interface AccountProfileValues {
  readonly name: string;
  readonly jobTitle: string;
  readonly phone: string;
  /** Other members of the person's workspaces may see the phone; off = only the person. */
  readonly phoneVisibleToWorkspaceMembers: boolean;
  readonly timezone: string;
  readonly dateFormat: string;
}

/** A loaded profile: the editable values plus what is read-only. */
export interface AccountProfileData extends AccountProfileValues {
  readonly email: string;
  /** ISO timestamp; `null` hides the "member since" line. */
  readonly joinedAt: string | null;
  readonly avatarUrl: string | null;
  /** Who manages the email (a single sign-on provider); `null` = nobody. */
  readonly managedBy: string | null;
  readonly workspaceName: string;
}

export interface AccountProfileSurfaceProps {
  readonly copy: AccountProfileCopy;
  readonly formatters: AccountFormatters;
  readonly profile: AccountSection<AccountProfileData>;
  /** The outcome of the last save; the container owns the mutation. */
  readonly save: AccountSaveStatus;
  /** Why the photo was not changed; `null` = nothing to say. */
  readonly photoError?: AccountPhotoFailure | null;
  /** A photo upload or removal is running: its buttons wait. */
  readonly photoBusy?: boolean;
  readonly onSave: (values: AccountProfileValues) => void;
  readonly onRetry: () => void;
  readonly onPhotoChange: (file: File) => void;
  /** Takes the photo away; offered only while there is one. */
  readonly onPhotoRemove?: () => void;
}

/** The producer's `User.MaxNameLength`. */
const NAME_MAX_LENGTH = 200;

const TIMEZONES = ["UTC", "Asia/Ho_Chi_Minh", "Asia/Tokyo", "Europe/Paris"];
const DATE_FORMATS = ["dd/mm/yyyy", "yyyy-mm-dd"];

function lookup(labels: Readonly<Record<string, string>>, id: string): string {
  return labels[id] ?? id;
}

/** The listed choices, plus the saved value when it is not one of them. */
function choicesWith(listed: readonly string[], current: string): string[] {
  return current && !listed.includes(current)
    ? [...listed, current]
    : [...listed];
}

type FieldKey = keyof AccountProfileValues;

function Field({
  htmlFor,
  label,
  edited,
  editedText,
  editedId,
  hint,
  hintId,
  children,
}: {
  htmlFor: string;
  label: string;
  edited: boolean;
  editedText: string;
  editedId: string;
  hint?: string;
  hintId?: string;
  children: React.ReactNode;
}) {
  return (
    <div data-f="fg" className="flex min-w-0 flex-col gap-1.5">
      <div className="flex items-center gap-2">
        <label
          htmlFor={htmlFor}
          className="text-crumb leading-[1.45] font-semibold text-muted-foreground"
        >
          {label}
        </label>
        {edited ? (
          <span
            id={editedId}
            className="text-micro leading-[1.45] font-semibold text-link"
          >
            {editedText}
          </span>
        ) : null}
      </div>
      {children}
      {hint ? (
        <span
          id={hintId}
          className="text-xs leading-[1.45] text-meta-foreground"
        >
          {hint}
        </span>
      ) : null}
    </div>
  );
}

function ProfileBody({
  copy,
  formatters,
  data,
  save,
  photoError,
  photoBusy,
  onSave,
  onPhotoChange,
  onPhotoRemove,
}: {
  copy: AccountProfileCopy;
  formatters: AccountFormatters;
  data: AccountProfileData;
  save: AccountSaveStatus;
  photoError: AccountPhotoFailure | null;
  photoBusy: boolean;
  onSave: (values: AccountProfileValues) => void;
  onPhotoChange: (file: File) => void;
  onPhotoRemove: (() => void) | undefined;
}) {
  const [values, setValues] = React.useState<AccountProfileValues>({
    name: data.name,
    jobTitle: data.jobTitle,
    phone: data.phone,
    phoneVisibleToWorkspaceMembers: data.phoneVisibleToWorkspaceMembers,
    timezone: data.timezone,
    dateFormat: data.dateFormat,
  });
  const ids = {
    name: React.useId(),
    jobTitle: React.useId(),
    email: React.useId(),
    emailHelp: React.useId(),
    phone: React.useId(),
    timezone: React.useId(),
    dateFormat: React.useId(),
    nameError: React.useId(),
    phoneError: React.useId(),
    phoneHelp: React.useId(),
    phoneVisible: React.useId(),
    phoneVisibleHint: React.useId(),
  };
  const fileInput = React.useRef<HTMLInputElement>(null);
  // Taking the photo away asks first, in place; the safe answer is focused and focus comes back.
  const [confirmRemove, setConfirmRemove] = React.useState(false);
  const keepButton = React.useRef<HTMLButtonElement>(null);
  const removeButton = React.useRef<HTMLButtonElement>(null);
  const askedBefore = React.useRef(false);
  React.useEffect(() => {
    if (confirmRemove) {
      askedBefore.current = true;
      keepButton.current?.focus();
    } else if (askedBefore.current) {
      removeButton.current?.focus();
    }
  }, [confirmRemove]);

  // A refetch after a save brings the stored values back into the draft.
  React.useEffect(() => {
    setValues({
      name: data.name,
      jobTitle: data.jobTitle,
      phone: data.phone,
      phoneVisibleToWorkspaceMembers: data.phoneVisibleToWorkspaceMembers,
      timezone: data.timezone,
      dateFormat: data.dateFormat,
    });
  }, [
    data.name,
    data.jobTitle,
    data.phone,
    data.phoneVisibleToWorkspaceMembers,
    data.timezone,
    data.dateFormat,
  ]);

  const set = (
    key: Exclude<FieldKey, "phoneVisibleToWorkspaceMembers">,
    value: string,
  ) => setValues((current) => ({ ...current, [key]: value }));

  const FIELD_ORDER: readonly FieldKey[] = [
    "name",
    "jobTitle",
    "phone",
    "phoneVisibleToWorkspaceMembers",
    "timezone",
    "dateFormat",
  ];
  // The phone compares as the number it stands for: spacing is not a change.
  const norm = (key: FieldKey, from: AccountProfileValues): string | boolean =>
    key === "phoneVisibleToWorkspaceMembers"
      ? from.phoneVisibleToWorkspaceMembers
      : key === "timezone" || key === "dateFormat"
        ? from[key]
        : key === "phone"
          ? (phoneForSave(from.phone) ?? from.phone.trim())
          : from[key].trim();
  const changed = FIELD_ORDER.filter(
    (key) => norm(key, values) !== norm(key, data),
  );
  const labels: Record<FieldKey, string> = {
    name: copy.nameLabel,
    jobTitle: copy.jobTitleLabel,
    phone: copy.phoneLabel,
    phoneVisibleToWorkspaceMembers: copy.phoneVisibleLabel,
    timezone: copy.timezoneLabel,
    dateFormat: copy.dateFormatLabel,
  };
  const nameError =
    values.name.trim() === ""
      ? copy.nameRequired
      : values.name.trim().length > NAME_MAX_LENGTH
        ? fillCopy(copy.nameTooLong, { count: NAME_MAX_LENGTH })
        : null;
  const phoneCheck = checkPhone(values.phone);
  const phoneError =
    phoneCheck.kind !== "invalid"
      ? null
      : phoneCheck.reason === "country-code"
        ? copy.phoneCountryCode
        : copy.phoneInvalid;
  const saving = save.state === "saving";
  const dirty = changed.length > 0;
  const isEdited = (key: FieldKey) => changed.includes(key);
  const editedClass = (key: FieldKey) =>
    isEdited(key) ? "border-primary ring-1 ring-primary" : undefined;

  const submit = () => {
    if (!dirty || saving || nameError || phoneError) return;
    onSave({
      name: values.name.trim(),
      jobTitle: values.jobTitle.trim(),
      // Valid here: an invalid number blocks the save above.
      phone: phoneForSave(values.phone) ?? "",
      phoneVisibleToWorkspaceMembers: values.phoneVisibleToWorkspaceMembers,
      timezone: values.timezone,
      dateFormat: values.dateFormat,
    });
  };
  const discard = () =>
    setValues({
      name: data.name,
      jobTitle: data.jobTitle,
      phone: data.phone,
      phoneVisibleToWorkspaceMembers: data.phoneVisibleToWorkspaceMembers,
      timezone: data.timezone,
      dateFormat: data.dateFormat,
    });

  return (
    <>
      <div
        data-f="avrow"
        className="flex items-center gap-4 rounded-[12px] border border-line bg-popover p-4 dark:bg-card"
      >
        <Avatar
          name={data.name}
          url={data.avatarUrl}
          className="size-16 text-xl"
        />
        <div className="min-w-0 flex-1">
          <b className="block truncate text-item-heading font-semibold text-foreground">
            {data.name}
          </b>
          {data.joinedAt ? (
            <small className="block text-crumb leading-[1.45] text-meta-foreground">
              {fillCopy(copy.memberSince, {
                date: formatters.date(data.joinedAt, {
                  day: "2-digit",
                  month: "2-digit",
                  year: "numeric",
                }),
              })}
            </small>
          ) : null}
        </div>
        <input
          ref={fileInput}
          type="file"
          accept="image/png,image/jpeg,image/webp"
          tabIndex={-1}
          aria-hidden="true"
          className="hidden"
          onChange={(event) => {
            const file = event.target.files?.[0];
            event.target.value = "";
            if (file) onPhotoChange(file);
          }}
        />
        <button
          type="button"
          aria-label={copy.changePhotoLabel}
          disabled={photoBusy}
          onClick={() => fileInput.current?.click()}
          className={cn(BTN, FOCUS)}
        >
          {copy.changePhoto}
        </button>
        {data.avatarUrl && onPhotoRemove ? (
          confirmRemove ? (
            <span
              role="group"
              aria-label={copy.removePhotoConfirm}
              className="flex items-center gap-2"
            >
              <span className="text-crumb text-foreground">
                {copy.removePhotoConfirm}
              </span>
              <button
                type="button"
                disabled={photoBusy}
                onClick={() => {
                  setConfirmRemove(false);
                  onPhotoRemove();
                }}
                className={cn(LINK_BUTTON_DANGER, FOCUS)}
              >
                {copy.removePhotoYes}
              </button>
              <button
                ref={keepButton}
                type="button"
                onClick={() => setConfirmRemove(false)}
                className={cn(BTN, FOCUS)}
              >
                {copy.removePhotoKeep}
              </button>
            </span>
          ) : (
            <button
              ref={removeButton}
              type="button"
              aria-label={copy.removePhotoLabel}
              disabled={photoBusy}
              onClick={() => setConfirmRemove(true)}
              className={cn(LINK_BUTTON_DANGER, FOCUS)}
            >
              {copy.removePhoto}
            </button>
          )
        ) : null}
      </div>
      <p role="status" className="sr-only">
        {photoBusy ? copy.photoWorking : null}
      </p>
      {photoError ? (
        <p role="alert" className="m-0 text-crumb text-danger-text">
          {photoError === "unsupported"
            ? copy.photoUnsupported
            : photoError === "too-large"
              ? copy.photoTooLarge
              : copy.photoFailed}
        </p>
      ) : null}

      <form
        noValidate
        aria-label={copy.title}
        onSubmit={(event) => {
          event.preventDefault();
          submit();
        }}
        data-f="fcard"
        className="grid grid-cols-2 gap-x-5 gap-y-4 rounded-[12px] border border-line bg-popover p-[18px] max-md:grid-cols-1 dark:bg-card"
      >
        <Field
          htmlFor={ids.name}
          label={copy.nameLabel}
          edited={isEdited("name")}
          editedText={copy.edited}
          editedId={`${ids.name}-edited`}
        >
          <input
            id={ids.name}
            type="text"
            required
            aria-required="true"
            autoComplete="name"
            value={values.name}
            disabled={saving}
            placeholder={copy.namePlaceholder}
            aria-invalid={nameError ? true : undefined}
            aria-describedby={
              cn(
                nameError && ids.nameError,
                isEdited("name") && `${ids.name}-edited`,
              ) || undefined
            }
            onChange={(event) => set("name", event.target.value)}
            className={cn(INPUT, editedClass("name"))}
          />
          {nameError ? (
            <span
              id={ids.nameError}
              role="alert"
              className="text-xs text-danger-text"
            >
              {nameError}
            </span>
          ) : null}
        </Field>
        <Field
          htmlFor={ids.jobTitle}
          label={copy.jobTitleLabel}
          edited={isEdited("jobTitle")}
          editedText={copy.edited}
          editedId={`${ids.jobTitle}-edited`}
        >
          <input
            id={ids.jobTitle}
            type="text"
            autoComplete="organization-title"
            value={values.jobTitle}
            disabled={saving}
            onChange={(event) => set("jobTitle", event.target.value)}
            className={cn(INPUT, editedClass("jobTitle"))}
          />
        </Field>
        <Field
          htmlFor={ids.email}
          label={copy.emailLabel}
          edited={false}
          editedText=""
          editedId=""
          hint={
            data.managedBy
              ? fillCopy(copy.emailManaged, {
                  workspace: data.workspaceName,
                  provider: data.managedBy,
                })
              : copy.emailHelp
          }
          hintId={ids.emailHelp}
        >
          <div className="relative">
            <input
              id={ids.email}
              type="email"
              readOnly
              value={data.email}
              aria-describedby={ids.emailHelp}
              className={cn(INPUT, "bg-background pr-9 text-muted-foreground")}
            />
            <Lock
              aria-hidden="true"
              className="absolute top-[10px] right-2.5 size-[15px] text-meta-foreground"
            />
          </div>
        </Field>
        <Field
          htmlFor={ids.phone}
          label={copy.phoneLabel}
          edited={isEdited("phone")}
          editedText={copy.edited}
          editedId={`${ids.phone}-edited`}
          hint={copy.phoneHelp}
          hintId={ids.phoneHelp}
        >
          <input
            id={ids.phone}
            type="tel"
            inputMode="tel"
            autoComplete="tel"
            placeholder={copy.phonePlaceholder}
            value={values.phone}
            disabled={saving}
            aria-invalid={phoneError ? true : undefined}
            aria-describedby={cn(
              phoneError && ids.phoneError,
              ids.phoneHelp,
              isEdited("phone") && `${ids.phone}-edited`,
            )}
            onChange={(event) => set("phone", event.target.value)}
            className={cn(INPUT, editedClass("phone"))}
          />
          {phoneError ? (
            <span
              id={ids.phoneError}
              role="alert"
              className="text-xs text-danger-text"
            >
              {phoneError}
            </span>
          ) : null}
          <div className="mt-1 flex items-start gap-2">
            <input
              id={ids.phoneVisible}
              type="checkbox"
              checked={values.phoneVisibleToWorkspaceMembers}
              disabled={
                saving ||
                (phoneCheck.kind === "empty" &&
                  !values.phoneVisibleToWorkspaceMembers)
              }
              aria-describedby={ids.phoneVisibleHint}
              onChange={(event) =>
                setValues((current) => ({
                  ...current,
                  phoneVisibleToWorkspaceMembers: event.target.checked,
                }))
              }
              className={cn("mt-[3px] size-4 shrink-0 accent-primary", FOCUS)}
            />
            <div className="min-w-0 text-compact leading-[1.45]">
              <label htmlFor={ids.phoneVisible} className="text-foreground">
                {copy.phoneVisibleLabel}
              </label>
              <small
                id={ids.phoneVisibleHint}
                className="block text-xs text-meta-foreground"
              >
                {copy.phoneVisibleHint}
              </small>
            </div>
          </div>
        </Field>
        <Field
          htmlFor={ids.timezone}
          label={copy.timezoneLabel}
          edited={isEdited("timezone")}
          editedText={copy.edited}
          editedId={`${ids.timezone}-edited`}
          hint={copy.timezoneHelp}
        >
          <Select
            value={values.timezone}
            disabled={saving}
            onValueChange={(value) => set("timezone", value)}
          >
            <SelectTrigger
              id={ids.timezone}
              className={cn("w-full", editedClass("timezone"))}
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {choicesWith(TIMEZONES, values.timezone).map((zone) => (
                <SelectItem key={zone} value={zone}>
                  {lookup(copy.timezones, zone)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Field
          htmlFor={ids.dateFormat}
          label={copy.dateFormatLabel}
          edited={isEdited("dateFormat")}
          editedText={copy.edited}
          editedId={`${ids.dateFormat}-edited`}
        >
          <Select
            value={values.dateFormat}
            disabled={saving}
            onValueChange={(value) => set("dateFormat", value)}
          >
            <SelectTrigger
              id={ids.dateFormat}
              className={cn("w-full", editedClass("dateFormat"))}
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {choicesWith(DATE_FORMATS, values.dateFormat).map((format) => (
                <SelectItem key={format} value={format}>
                  {lookup(copy.dateFormats, format)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
      </form>

      {save.state === "error" || save.state === "conflict" ? (
        <p
          role="alert"
          className="m-0 rounded-md bg-danger-subtle px-3 py-2 text-metadata text-danger-subtle-foreground"
        >
          {save.state === "conflict" ? copy.saveConflict : copy.saveError}
        </p>
      ) : null}

      {dirty ? (
        <div
          role="region"
          aria-label={copy.unsavedRegion}
          data-f="savebar"
          className="sticky bottom-0 z-10 mt-auto flex items-center gap-2.5 rounded-[12px] border border-line-strong bg-popover py-3 pr-3.5 pl-4 shadow-lg dark:bg-card"
        >
          <span className="text-compact leading-[1.45] text-foreground">
            {pluralCopy(copy.unsaved, changed.length)}
          </span>
          <small className="text-xs leading-[1.45] text-meta-foreground">
            · {changed.map((key) => labels[key]).join(", ")}
          </small>
          <span className="grow" />
          <button
            type="button"
            disabled={saving}
            onClick={discard}
            className={cn(BTN, FOCUS)}
          >
            {copy.discard}
          </button>
          <button
            type="button"
            disabled={saving || nameError !== null || phoneError !== null}
            onClick={submit}
            className={cn(BTN_PRIMARY, FOCUS)}
          >
            {saving ? copy.saving : copy.save}
          </button>
        </div>
      ) : null}
      {save.state === "saved" && !dirty ? <Toast>{copy.saved}</Toast> : null}
    </>
  );
}

function ProfileSkeleton({ label }: { label: string }) {
  return (
    <div role="status" aria-label={label} className="flex flex-col gap-4">
      <Skeleton className="h-[98px] w-full rounded-[12px]" />
      <Skeleton className="h-[314px] w-full rounded-[12px]" />
    </div>
  );
}

/**
 * Account profile (Final-Account `ho-so`): the photo and member-since line,
 * the editable details (name, job title, phone, time zone, date format), the
 * read-only email and a save bar that appears once something changed. The
 * container owns the mutations and reports the save through `save`.
 */
export function AccountProfileSurface({
  copy,
  formatters,
  profile,
  save,
  photoError = null,
  photoBusy = false,
  onSave,
  onRetry,
  onPhotoChange,
  onPhotoRemove,
}: AccountProfileSurfaceProps) {
  return (
    <section
      aria-labelledby="account-profile-title"
      className="flex max-w-[760px] min-w-0 flex-1 flex-col gap-[18px]"
    >
      <PageHead
        id="account-profile-title"
        title={copy.title}
        description={copy.description}
      />
      {profile.status === "loading" ? (
        <ProfileSkeleton label={copy.loading} />
      ) : profile.status === "error" ? (
        <LoadError
          message={copy.loadError}
          retry={copy.retry}
          onRetry={onRetry}
        />
      ) : (
        <ProfileBody
          copy={copy}
          formatters={formatters}
          data={profile.data}
          save={save}
          photoError={photoError}
          photoBusy={photoBusy}
          onSave={onSave}
          onPhotoChange={onPhotoChange}
          onPhotoRemove={onPhotoRemove}
        />
      )}
    </section>
  );
}
