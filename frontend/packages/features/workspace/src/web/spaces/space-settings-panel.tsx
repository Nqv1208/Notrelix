import * as React from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  cn,
} from "@notrelix/ui-web";
import {
  archiveBlocked,
  type SpaceContentCounts,
  type SpaceNode,
  type SpaceType,
  type SpaceVisibility,
} from "../../core/model/spaces";
import { pluralForm } from "../home/home-models";
import { formatCopy } from "../shell/shell-models";
import { SPACE_NAME_MAX } from "./create-space-dialog";
import { BTN_SM, BTN_SM_PRIMARY, FOCUS } from "./space-page-parts";
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
  readonly workspaceName: string;
  /** The ancestors of the Space, root first (without the Space itself). */
  readonly ancestors: readonly string[];
  /** Names of the visible sub-departments, for the move hint. */
  readonly childNames: readonly string[];
  readonly canManage: boolean;
  readonly saveStatus: SpaceSaveStatus;
  /** What the member can see inside the Space; `null` while it loads. */
  readonly content: SpaceContentCounts | null;
  readonly archiving: boolean;
  /** The producer refused an archive the visible content did not explain. */
  readonly archiveRefused: boolean;
  /** Only the changed fields are passed. */
  readonly onSave: (changes: Partial<SpaceDetailsValues>) => void;
  readonly onMove: () => void;
  readonly onArchive: () => void;
  /** `null`: there is no delete write, the button is drawn inert. */
  readonly onDelete: (() => void) | null;
  /** Starts with the delete confirmation open (stories, tests). */
  readonly initialDeleteOpen?: boolean;
}

const SECTION =
  "flex flex-col gap-3 rounded-[12px] border border-line bg-popover px-[18px] py-4 dark:bg-card";
const H2 = "m-0 text-item-heading leading-[1.45] text-foreground";
const HINT = "m-0 text-crumb leading-[1.55] text-meta-foreground";
const LABEL = "text-crumb leading-[1.45] font-semibold text-muted-foreground";
const INPUT =
  "box-border font-normal h-[34px] w-full rounded-[7px] border border-line-strong bg-popover px-2.5 text-nav leading-[1.45] text-foreground aria-disabled:cursor-default disabled:cursor-default disabled:text-muted-foreground dark:bg-card";

function contentPhrase(
  copy: WorkspaceSpacesCopy,
  counts: SpaceContentCounts,
): string {
  const s = copy.settings;
  const parts = [
    counts.childSpaces > 0
      ? pluralForm(
          { one: s.childSpaces_one, other: s.childSpaces_other },
          counts.childSpaces,
        )
      : null,
    counts.boards > 0
      ? pluralForm({ one: s.boards_one, other: s.boards_other }, counts.boards)
      : null,
    counts.docs > 0
      ? pluralForm({ one: s.docs_one, other: s.docs_other }, counts.docs)
      : null,
  ].filter((part): part is string => part !== null);
  if (parts.length <= 1) return parts.join("");
  return `${parts.slice(0, -1).join(", ")}${s.and}${parts[parts.length - 1]}`;
}

const RADIO_OPTIONS = ["Private", "Workspace"] as const;

/**
 * The settings of a department (Final-Space `thiet-lap`): name and
 * description with Save, Private or Open to the workspace, where it sits in
 * the tree, then Archive and Delete. Visibility saves when chosen. Archive
 * never cascades (WSP-022): while the member can see active departments,
 * Boards or Docs inside, the action is unavailable and the reason shown; the
 * producer still decides for content the member cannot see. Delete has no
 * write behind it, so it is drawn inert.
 */
export function SpaceSettingsPanel({
  copy,
  space,
  workspaceName,
  ancestors,
  childNames,
  canManage,
  saveStatus,
  content,
  archiving,
  archiveRefused,
  onSave,
  onMove,
  onArchive,
  onDelete,
  initialDeleteOpen = false,
}: SpaceSettingsPanelProps) {
  const s = copy.settings;
  const [name, setName] = React.useState(space.name);
  const [description, setDescription] = React.useState(space.description ?? "");
  const [touched, setTouched] = React.useState(false);
  const [confirmArchive, setConfirmArchive] = React.useState(false);
  const [deleteOpen, setDeleteOpen] = React.useState(initialDeleteOpen);
  const [deleteName, setDeleteName] = React.useState("");
  const radios = React.useRef<Array<HTMLDivElement | null>>([]);

  // A new server version replaces the form (after a save or a reload).
  React.useEffect(() => {
    setName(space.name);
    setDescription(space.description ?? "");
    setTouched(false);
  }, [space.name, space.description, space.version]);

  const readOnly = !canManage || space.isArchived;
  const trimmedName = name.trim();
  const trimmedDescription = description.trim();
  const changes: { name?: string; description?: string } = {};
  if (trimmedName !== space.name) changes.name = trimmedName;
  if (trimmedDescription !== (space.description ?? ""))
    changes.description = trimmedDescription;
  const dirty = Object.keys(changes).length > 0;
  const nameMessage = touched && trimmedName === "" ? s.nameRequired : null;
  const descriptionMessage =
    trimmedDescription.length > SPACE_DESCRIPTION_MAX
      ? s.descriptionTooLong
      : null;
  const saving = saveStatus === "saving";
  const canSave =
    !readOnly &&
    !saving &&
    dirty &&
    trimmedName !== "" &&
    descriptionMessage === null;

  const blocked = content !== null && archiveBlocked(content);
  const canArchive =
    canManage && !space.isArchived && content !== null && !blocked;
  const archiveNote = blocked
    ? formatCopy(s.archiveBlocked, { content: contentPhrase(copy, content) })
    : archiveRefused
      ? s.archiveBlockedUnknown
      : s.archiveHint;

  const parent =
    ancestors.length === 0
      ? formatCopy(s.placeRoot, { workspace: workspaceName })
      : [workspaceName, ...ancestors].join(s.placeBreadcrumb);
  const placeHint =
    childNames.length > 0
      ? formatCopy(s.placeHint, {
          children: childNames.slice(0, 2).join(s.placeOr),
        })
      : s.placeHintNoChildren;

  const deleteScope = [
    ...childNames,
    ...(content !== null && content.boards > 0
      ? [
          pluralForm(
            { one: s.boards_one, other: s.boards_other },
            content.boards,
          ),
        ]
      : []),
  ];
  const deleteContent =
    deleteScope.length <= 1
      ? deleteScope.join("")
      : `${deleteScope.slice(0, -1).join(", ")}${s.and}${deleteScope[deleteScope.length - 1]}`;
  const deleteMatches = deleteName.trim() === space.name;

  const selectVisibility = (value: SpaceVisibility) => {
    if (readOnly || saving || value === space.visibility) return;
    onSave({ visibility: value });
  };

  const onRadioKey = (
    event: React.KeyboardEvent<HTMLDivElement>,
    index: number,
  ) => {
    const next =
      event.key === "ArrowDown" || event.key === "ArrowRight"
        ? (index + 1) % RADIO_OPTIONS.length
        : event.key === "ArrowUp" || event.key === "ArrowLeft"
          ? (index - 1 + RADIO_OPTIONS.length) % RADIO_OPTIONS.length
          : null;
    if (next === null) return;
    event.preventDefault();
    radios.current[next]?.focus();
    selectVisibility(RADIO_OPTIONS[next] ?? space.visibility);
  };

  return (
    <div
      data-f="col"
      className="box-content flex min-w-0 max-w-[760px] flex-1 flex-col gap-[18px] overflow-auto px-7 pt-[22px] pb-8"
    >
      <p
        data-f="note"
        className="m-0 text-crumb leading-[1.55] text-meta-foreground"
      >
        {s.note}
      </p>

      <form
        noValidate
        data-f="set"
        aria-labelledby="space-h-name"
        className={SECTION}
        onSubmit={(event) => {
          event.preventDefault();
          setTouched(true);
          if (canSave) onSave(changes);
        }}
      >
        <h2 id="space-h-name" data-f="seth" className={H2}>
          {s.nameTitle}
        </h2>
        <div data-f="fld" className="flex flex-col gap-[5px]">
          <label htmlFor="space-name" data-f="lab" className={LABEL}>
            {s.nameLabel}
          </label>
          <input
            id="space-name"
            data-f="inp"
            type="text"
            value={name}
            maxLength={SPACE_NAME_MAX}
            disabled={readOnly}
            aria-invalid={nameMessage ? true : undefined}
            aria-describedby={nameMessage ? "space-name-error" : undefined}
            onChange={(event) => setName(event.target.value)}
            className={cn(INPUT, FOCUS)}
          />
          {nameMessage ? (
            <span
              id="space-name-error"
              role="alert"
              className="text-crumb leading-[1.45] text-danger-text"
            >
              {nameMessage}
            </span>
          ) : null}
        </div>
        <div data-f="fld" className="flex flex-col gap-[5px]">
          <label htmlFor="space-desc" data-f="lab" className={LABEL}>
            {s.descriptionLabel}
          </label>
          <textarea
            id="space-desc"
            data-f="ta"
            value={description}
            disabled={readOnly}
            aria-invalid={descriptionMessage ? true : undefined}
            aria-describedby={
              descriptionMessage ? "space-desc-error" : undefined
            }
            onChange={(event) => setDescription(event.target.value)}
            className={cn(
              INPUT,
              "h-16 resize-none px-2.5 py-2 leading-normal",
              FOCUS,
            )}
          />
          {descriptionMessage ? (
            <span
              id="space-desc-error"
              role="alert"
              className="text-crumb leading-[1.45] text-danger-text"
            >
              {descriptionMessage}
            </span>
          ) : null}
        </div>
        {!readOnly ? (
          <div className="flex items-center gap-3">
            <button
              data-f="save"
              type="submit"
              aria-disabled={canSave ? undefined : true}
              className={cn(BTN_SM_PRIMARY, FOCUS)}
            >
              {saving ? s.saving : s.save}
            </button>
            {saveStatus === "saved" && !dirty ? (
              <span
                role="status"
                className="text-crumb leading-[1.45] text-success-text"
              >
                {s.saved}
              </span>
            ) : null}
          </div>
        ) : null}
      </form>

      <section data-f="set" aria-labelledby="space-h-vis" className={SECTION}>
        <h2 id="space-h-vis" data-f="seth" className={H2}>
          {s.visibilityTitle}
        </h2>
        <div
          role="radiogroup"
          aria-labelledby="space-h-vis"
          className="flex flex-col gap-2"
        >
          {RADIO_OPTIONS.map((option, index) => {
            const on = space.visibility === option;
            return (
              <div
                key={option}
                ref={(node) => {
                  radios.current[index] = node;
                }}
                data-f="rad"
                role="radio"
                aria-checked={on}
                aria-disabled={readOnly ? true : undefined}
                tabIndex={on ? 0 : -1}
                onClick={() => selectVisibility(option)}
                onKeyDown={(event) => {
                  if (event.key === " " || event.key === "Enter") {
                    event.preventDefault();
                    selectVisibility(option);
                  } else onRadioKey(event, index);
                }}
                className={cn(
                  "flex items-start gap-2.5 rounded-[10px] border px-3 py-2.5",
                  readOnly ? "cursor-default" : "cursor-pointer",
                  on
                    ? "border-primary bg-state-selected"
                    : "border-line hover:bg-state-hover",
                  FOCUS,
                )}
              >
                <span
                  data-f="radio-o"
                  className={cn(
                    "mt-0.5 box-border grid size-4 shrink-0 place-items-center rounded-full border-[1.5px]",
                    on ? "border-primary" : "border-line-strong",
                  )}
                >
                  {on ? (
                    <span className="size-2 rounded-full bg-primary" />
                  ) : null}
                </span>
                <span>
                  <b className="block text-nav leading-[1.45] font-semibold text-foreground">
                    {option === "Private"
                      ? s.visibilityPrivate
                      : s.visibilityOpen}
                  </b>
                  <small className="block text-crumb leading-normal text-meta-foreground">
                    {option === "Private"
                      ? s.visibilityPrivateHint
                      : s.visibilityOpenHint}
                  </small>
                </span>
              </div>
            );
          })}
        </div>
        <p data-f="hint" className={HINT}>
          {s.visibilityHint}
        </p>
      </section>

      <section data-f="set" aria-labelledby="space-h-place" className={SECTION}>
        <h2 id="space-h-place" data-f="seth" className={H2}>
          {s.placeTitle}
        </h2>
        <div
          data-f="place"
          className="flex items-center gap-2.5 rounded-[10px] border border-line px-3 py-2.5 text-nav leading-[1.45] font-normal"
        >
          <span data-f="path" className="text-meta-foreground">
            {s.placeParent}
          </span>
          <b className="font-semibold text-foreground">{parent}</b>
          <button
            data-f="btn-sm"
            type="button"
            aria-disabled={canManage && !space.isArchived ? undefined : true}
            onClick={canManage && !space.isArchived ? onMove : undefined}
            className={cn(BTN_SM, "ml-auto", FOCUS)}
          >
            {s.placeMove}
          </button>
        </div>
        <p data-f="hint" className={HINT}>
          {placeHint}
        </p>
      </section>

      <section
        data-f="set"
        aria-labelledby="space-h-danger"
        className={cn(SECTION, "border-danger-text")}
      >
        <h2
          id="space-h-danger"
          data-f="seth"
          className={cn(H2, "text-danger-text")}
        >
          {s.dangerTitle}
        </h2>
        <p data-f="hint" className={HINT}>
          {s.dangerHint}
        </p>
        <div data-f="drow" className="flex items-start gap-3.5">
          <span>
            <b className="block text-nav leading-[1.45] font-semibold text-foreground">
              {s.archiveTitle}
            </b>
            <small
              data-f="dsm"
              className="mt-0.5 block text-crumb leading-normal text-meta-foreground"
            >
              {archiveNote}
            </small>
          </span>
          <button
            data-f="btn-sm"
            type="button"
            aria-disabled={canArchive && !archiving ? undefined : true}
            onClick={
              canArchive && !archiving
                ? () => setConfirmArchive(true)
                : undefined
            }
            className={cn(BTN_SM, "ml-auto", FOCUS)}
          >
            {archiving ? s.archiving : s.archive}
          </button>
        </div>
        <div
          data-f="drow"
          className="flex items-start gap-3.5 border-t border-line pt-3"
        >
          <span>
            <b className="block text-nav leading-[1.45] font-semibold text-foreground">
              {s.deleteTitle}
            </b>
            <small
              data-f="dsm"
              className="mt-0.5 block text-crumb leading-normal text-meta-foreground"
            >
              {onDelete ? s.deleteHint : s.deleteUnavailable}
            </small>
          </span>
          <button
            data-f="btn-sm"
            type="button"
            aria-expanded={onDelete ? deleteOpen : undefined}
            aria-controls={onDelete ? "space-delete-confirm" : undefined}
            aria-disabled={onDelete && canManage ? undefined : true}
            title={onDelete ? undefined : s.deleteUnavailable}
            onClick={
              onDelete && canManage
                ? () => setDeleteOpen((open) => !open)
                : undefined
            }
            className={cn(
              BTN_SM,
              "ml-auto border-danger-text text-danger-text",
              FOCUS,
            )}
          >
            {s.delete}
          </button>
        </div>
        {onDelete && deleteOpen ? (
          <div
            id="space-delete-confirm"
            data-f="confirm"
            role="group"
            aria-labelledby="space-delete-q"
            className="flex flex-col gap-2 rounded-[10px] border border-danger-text bg-danger-subtle p-3 text-crumb leading-[1.45] text-foreground"
          >
            <b id="space-delete-q" className="font-semibold">
              {formatCopy(s.deleteConfirmTitle, { name: space.name })}
            </b>
            <span>
              {formatCopy(s.deleteConfirmText, { content: deleteContent })}
            </span>
            <label htmlFor="space-delete-name" className="font-semibold">
              {formatCopy(s.deleteConfirmLabel, { name: space.name })}
            </label>
            <div data-f="crow" className="flex items-center gap-2">
              <input
                id="space-delete-name"
                data-f="inp"
                type="text"
                value={deleteName}
                placeholder={space.name}
                onChange={(event) => setDeleteName(event.target.value)}
                className={cn(INPUT, "flex-1", FOCUS)}
              />
              <button
                data-f="btn-sm"
                type="button"
                onClick={() => {
                  setDeleteOpen(false);
                  setDeleteName("");
                }}
                className={cn(BTN_SM, FOCUS)}
              >
                {s.cancel}
              </button>
              <button
                data-f="btn-sm"
                type="button"
                aria-disabled={deleteMatches ? undefined : true}
                onClick={deleteMatches ? onDelete : undefined}
                className={cn(
                  "inline-flex h-7 items-center rounded-[7px] border border-danger-text bg-danger-text px-2.5 text-crumb leading-[1.45] font-semibold whitespace-nowrap text-danger-foreground aria-disabled:cursor-default",
                  FOCUS,
                )}
              >
                {s.delete}
              </button>
            </div>
          </div>
        ) : null}
      </section>

      <AlertDialog open={confirmArchive} onOpenChange={setConfirmArchive}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {formatCopy(s.archiveConfirmTitle, { name: space.name })}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {s.archiveConfirmDescription}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{s.cancel}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                setConfirmArchive(false);
                onArchive();
              }}
            >
              {s.archiveConfirmAction}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
