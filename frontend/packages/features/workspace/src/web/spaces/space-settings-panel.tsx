import * as React from "react";
import { Archive, Check, Info, Lock, Users } from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
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
  RadioGroup,
  RadioGroupItem,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
  cn,
} from "@notrelix/ui-web";
import {
  SPACE_TYPES,
  SPACE_VISIBILITIES,
  archiveBlocked,
  type SpaceContentCounts,
  type SpaceNode,
  type SpaceType,
  type SpaceVisibility,
} from "../../core/model/spaces";
import { pluralForm } from "../home/home-models";
import { formatCopy } from "../shell/shell-models";
import { SPACE_NAME_MAX } from "./create-space-dialog";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

export const SPACE_DESCRIPTION_MAX = 1024;

export interface SpaceDetailsValues {
  readonly name: string;
  readonly description: string;
  readonly type: SpaceType;
  readonly visibility: SpaceVisibility;
}

export type SpaceSaveStatus = "idle" | "saving" | "saved";

export interface SpaceSettingsPanelProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly space: SpaceNode;
  readonly canManage: boolean;
  readonly saveStatus: SpaceSaveStatus;
  /** What the member can see inside the Space; `null` while it loads. */
  readonly content: SpaceContentCounts | null;
  readonly archiving: boolean;
  /** The producer refused an archive the visible content did not explain. */
  readonly archiveRefused: boolean;
  /** Only the changed fields are passed. */
  readonly onSave: (changes: Partial<SpaceDetailsValues>) => void;
  readonly onArchive: () => void;
}

const VISIBILITY_ICON = { Private: Lock, Workspace: Users } as const;

function valuesOf(space: SpaceNode): SpaceDetailsValues {
  return {
    name: space.name,
    description: space.description ?? "",
    type: space.type,
    visibility: space.visibility,
  };
}

function contentPhrase(
  copy: WorkspaceSpacesCopy,
  counts: SpaceContentCounts,
): string {
  const d = copy.settings.danger;
  const parts = [
    counts.childSpaces > 0
      ? pluralForm(
          { one: d.childSpaces_one, other: d.childSpaces_other },
          counts.childSpaces,
        )
      : null,
    counts.boards > 0
      ? pluralForm({ one: d.boards_one, other: d.boards_other }, counts.boards)
      : null,
    counts.docs > 0
      ? pluralForm({ one: d.docs_one, other: d.docs_other }, counts.docs)
      : null,
  ].filter((part): part is string => part !== null);
  if (parts.length <= 1) return parts.join("");
  return `${parts.slice(0, -1).join(", ")}${d.and}${parts[parts.length - 1]}`;
}

/**
 * Space settings (WSP-021..023): name, type, description and visibility in
 * one form that saves only what changed, then Archive. Archive never
 * cascades (WSP-022): while the member can see active Spaces, Boards or
 * Docs inside, the action is unavailable and the reason is shown; the
 * producer still decides for content the member cannot see.
 */
export function SpaceSettingsPanel({
  copy,
  space,
  canManage,
  saveStatus,
  content,
  archiving,
  archiveRefused,
  onSave,
  onArchive,
}: SpaceSettingsPanelProps) {
  const [values, setValues] = React.useState(() => valuesOf(space));
  const [touched, setTouched] = React.useState(false);
  const [confirmOpen, setConfirmOpen] = React.useState(false);
  const nameId = React.useId();
  const nameErrorId = React.useId();
  const descriptionId = React.useId();
  const descriptionHelpId = React.useId();
  const typeId = React.useId();
  const visibilityLabelId = React.useId();
  const blockedId = React.useId();

  // A new server version replaces the form (after a save or a reload).
  React.useEffect(() => {
    setValues(valuesOf(space));
    setTouched(false);
  }, [space]);

  const original = valuesOf(space);
  const name = values.name.trim();
  const description = values.description.trim();
  const nameMessage =
    touched && name === "" ? copy.settings.profile.nameRequired : null;
  const descriptionMessage =
    description.length > SPACE_DESCRIPTION_MAX
      ? copy.settings.profile.descriptionTooLong
      : null;
  const changes: {
    -readonly [K in keyof SpaceDetailsValues]?: SpaceDetailsValues[K];
  } = {};
  if (name !== original.name) changes.name = name;
  if (description !== original.description) changes.description = description;
  if (values.type !== original.type) changes.type = values.type;
  if (values.visibility !== original.visibility)
    changes.visibility = values.visibility;
  const dirty = Object.keys(changes).length > 0;
  const readOnly = !canManage || space.isArchived;
  const saving = saveStatus === "saving";

  const blocked = content !== null && archiveBlocked(content);
  const canArchive =
    canManage && !space.isArchived && content !== null && !blocked;

  return (
    <div className="flex flex-col gap-6">
      {!canManage ? (
        <p className="flex items-center gap-2 rounded-md bg-info-subtle px-3 py-2 text-metadata text-info-subtle-foreground">
          <Info aria-hidden="true" className="size-4 shrink-0" />
          {copy.settings.readOnly}
        </p>
      ) : null}
      <form
        noValidate
        aria-label={copy.settings.profile.title}
        onSubmit={(event) => {
          event.preventDefault();
          setTouched(true);
          if (readOnly || saving || !dirty || name === "" || descriptionMessage)
            return;
          onSave(changes);
        }}
        className="flex flex-col gap-5 rounded-lg border border-border bg-surface-panel p-4 sm:p-6"
      >
        <header className="flex flex-col gap-1">
          <h2 className="text-subheading text-foreground">
            {copy.settings.profile.title}
          </h2>
          <p className="text-metadata text-muted-foreground">
            {copy.settings.profile.description}
          </p>
        </header>
        <Field data-invalid={nameMessage ? true : undefined}>
          <FieldLabel htmlFor={nameId}>
            {copy.settings.profile.nameLabel}
          </FieldLabel>
          <Input
            id={nameId}
            value={values.name}
            maxLength={SPACE_NAME_MAX}
            disabled={readOnly}
            aria-invalid={nameMessage ? true : undefined}
            aria-describedby={nameMessage ? nameErrorId : undefined}
            onChange={(event) =>
              setValues((current) => ({ ...current, name: event.target.value }))
            }
          />
          {nameMessage ? (
            <FieldError id={nameErrorId}>{nameMessage}</FieldError>
          ) : null}
        </Field>
        <Field>
          <FieldLabel htmlFor={typeId}>
            {copy.settings.profile.typeLabel}
          </FieldLabel>
          <Select
            value={values.type}
            disabled={readOnly}
            onValueChange={(value) =>
              setValues((current) => ({ ...current, type: value as SpaceType }))
            }
          >
            <SelectTrigger id={typeId} className="w-full sm:w-64">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {SPACE_TYPES.map((option) => (
                <SelectItem key={option} value={option}>
                  {copy.types[option]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <FieldDescription>{copy.typeHelp[values.type]}</FieldDescription>
        </Field>
        <Field data-invalid={descriptionMessage ? true : undefined}>
          <FieldLabel htmlFor={descriptionId}>
            {copy.settings.profile.descriptionLabel}
          </FieldLabel>
          <Textarea
            id={descriptionId}
            value={values.description}
            rows={3}
            disabled={readOnly}
            aria-describedby={descriptionHelpId}
            aria-invalid={descriptionMessage ? true : undefined}
            onChange={(event) =>
              setValues((current) => ({
                ...current,
                description: event.target.value,
              }))
            }
          />
          {descriptionMessage ? (
            <FieldError id={descriptionHelpId}>{descriptionMessage}</FieldError>
          ) : (
            <FieldDescription id={descriptionHelpId}>
              {copy.settings.profile.descriptionHelp}
            </FieldDescription>
          )}
        </Field>
        <fieldset className="flex flex-col gap-2" disabled={readOnly}>
          <legend
            id={visibilityLabelId}
            className="mb-1 text-label text-foreground"
          >
            {copy.settings.access.title}
          </legend>
          <p className="mb-1 text-metadata text-muted-foreground">
            {copy.settings.access.description}
          </p>
          <RadioGroup
            value={values.visibility}
            disabled={readOnly}
            onValueChange={(value) =>
              setValues((current) => ({
                ...current,
                visibility: value as SpaceVisibility,
              }))
            }
            aria-labelledby={visibilityLabelId}
            className="gap-2"
          >
            {SPACE_VISIBILITIES.map((option) => {
              const Icon = VISIBILITY_ICON[option];
              const optionId = `${visibilityLabelId}-${option}`;
              return (
                <label
                  key={option}
                  htmlFor={optionId}
                  className={cn(
                    "flex items-start gap-3 rounded-md border border-border p-3",
                    readOnly
                      ? "cursor-default"
                      : "cursor-pointer hover:bg-state-hover",
                    values.visibility === option &&
                      "border-focus-ring bg-state-selected",
                  )}
                >
                  <RadioGroupItem
                    id={optionId}
                    value={option}
                    aria-describedby={`${optionId}-help`}
                    className="mt-0.5"
                  />
                  <span className="flex min-w-0 flex-col gap-0.5">
                    <span className="flex items-center gap-1.5 text-label text-foreground">
                      <Icon aria-hidden="true" className="size-4 text-icon" />
                      {copy.visibility[option]}
                    </span>
                    <span
                      id={`${optionId}-help`}
                      className="text-metadata text-muted-foreground"
                    >
                      {copy.visibilityHelp[option]}
                    </span>
                  </span>
                </label>
              );
            })}
          </RadioGroup>
        </fieldset>
        {canManage && !space.isArchived ? (
          <div className="flex items-center justify-end gap-3">
            {saveStatus === "saved" && !dirty ? (
              <span
                role="status"
                className="flex items-center gap-1 text-metadata text-success-text"
              >
                <Check aria-hidden="true" className="size-4" />
                {copy.settings.saved}
              </span>
            ) : null}
            <Button type="submit" disabled={!dirty || saving}>
              {saving ? copy.settings.saving : copy.settings.save}
            </Button>
          </div>
        ) : null}
      </form>

      {canManage && !space.isArchived ? (
        <section
          aria-label={copy.settings.danger.title}
          className="flex flex-col gap-3 rounded-lg border border-danger-subtle bg-surface-panel p-4 sm:p-6"
        >
          <header className="flex flex-col gap-1">
            <h2 className="text-subheading text-foreground">
              {copy.settings.danger.title}
            </h2>
            <p className="text-metadata text-muted-foreground">
              {copy.settings.danger.description}
            </p>
          </header>
          {blocked && content ? (
            <p
              id={blockedId}
              className="flex items-start gap-2 rounded-md bg-warning-subtle px-3 py-2 text-metadata text-warning-subtle-foreground"
            >
              <Info aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
              {formatCopy(copy.settings.danger.blocked, {
                content: contentPhrase(copy, content),
              })}
            </p>
          ) : archiveRefused ? (
            <p
              id={blockedId}
              role="alert"
              className="flex items-start gap-2 rounded-md bg-warning-subtle px-3 py-2 text-metadata text-warning-subtle-foreground"
            >
              <Info aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
              {copy.settings.danger.blockedUnknown}
            </p>
          ) : null}
          <div>
            <Button
              variant="outline"
              disabled={!canArchive || archiving}
              aria-describedby={
                blocked || archiveRefused ? blockedId : undefined
              }
              onClick={() => setConfirmOpen(true)}
              className="border-danger-subtle text-danger-text hover:bg-danger-subtle hover:text-danger-subtle-foreground"
            >
              <Archive aria-hidden="true" />
              {archiving
                ? copy.settings.danger.archiving
                : copy.settings.danger.archive}
            </Button>
          </div>
        </section>
      ) : null}

      <AlertDialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {formatCopy(copy.settings.danger.confirmTitle, {
                name: space.name,
              })}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {copy.settings.danger.confirmDescription}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{copy.settings.danger.cancel}</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={() => {
                setConfirmOpen(false);
                onArchive();
              }}
            >
              {copy.settings.danger.confirmAction}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
