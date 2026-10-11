import * as React from "react";
import {
  Archive,
  ArchiveRestore,
  ArrowLeft,
  Info,
  Trash2,
  UserPlus,
} from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Badge,
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
  Spinner,
  Textarea,
} from "@notrelix/ui-web";
import {
  identityToneFor,
  normalizeSearchText,
} from "../../core/model/navigation";
import {
  TEAM_ROLES,
  addableTeamMembers,
  teamCapabilities,
  teamMemberCapabilities,
  type Team,
  type TeamMember,
  type TeamRole,
} from "../../core/model/teams";
import type {
  WorkspaceMember,
  WorkspaceRole,
} from "../../core/types/workspace";
import { pluralForm, type SectionState } from "../home/home-models";
import { IdentityMark } from "../shell/shell-link";
import { formatCopy, isPlainClick } from "../shell/shell-models";
import { TEAM_DESCRIPTION_MAX, TEAM_NAME_MAX } from "./create-team-dialog";
import type { WorkspaceTeamsCopy } from "./workspace-teams.copy";

export interface AddTeamMemberInput {
  readonly userId: string;
  readonly role: TeamRole;
}

export interface WorkspaceTeamSurfaceProps {
  readonly copy: WorkspaceTeamsCopy;
  readonly team: SectionState<Team>;
  readonly members: SectionState<readonly TeamMember[]>;
  readonly viewer: {
    readonly userId: string | null;
    readonly role: WorkspaceRole | null;
  };
  /** Workspace members the host knows; the surface offers those not yet in the Team. */
  readonly candidates: readonly WorkspaceMember[];
  /** Members with a write in flight. */
  readonly pendingUserIds: ReadonlySet<string>;
  /** A Team write (rename, describe, archive, add) is in flight. */
  readonly saving: boolean;
  readonly onRename: (name: string) => void;
  /** `null` clears the description. */
  readonly onDescribe: (description: string | null) => void;
  readonly onArchive: () => void;
  readonly onUnarchive: () => void;
  readonly onAddMember: (input: AddTeamMemberInput) => void;
  readonly onChangeRole: (userId: string, role: TeamRole) => void;
  readonly onRemove: (userId: string) => void;
  readonly onRetry: () => void;
  /** The Teams list: where the error state leads when this Team is gone or unreadable. */
  readonly backNavigation: {
    readonly getHref: () => string;
    readonly onNavigate: () => void;
  };
}

type Confirmation =
  | { readonly type: "remove"; readonly member: TeamMember }
  | { readonly type: "archive" | "unarchive" };

function SectionError({
  message,
  retry,
  onRetry,
}: {
  message: string;
  retry: string;
  onRetry: () => void;
}) {
  return (
    <div
      role="alert"
      className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-surface-panel px-4 py-3"
    >
      <p className="text-body text-muted-foreground">{message}</p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        {retry}
      </Button>
    </div>
  );
}

function RowsSkeleton({ label, rows = 3 }: { label: string; rows?: number }) {
  return (
    <div
      role="status"
      aria-label={label}
      className="overflow-hidden rounded-lg border border-border bg-surface-panel"
    >
      {Array.from({ length: rows }, (_, index) => (
        <div
          key={index}
          className="flex h-14 items-center gap-3 border-b border-border px-4 last:border-0"
        >
          <Skeleton className="size-8 rounded-full" />
          <Skeleton className={index % 2 ? "h-3.5 w-40" : "h-3.5 w-28"} />
          <Skeleton className="ml-auto h-8 w-28" />
        </div>
      ))}
    </div>
  );
}

function ReadOnlyNotice({ id, children }: { id: string; children: string }) {
  return (
    <p
      id={id}
      className="flex items-start gap-2 rounded-md bg-info-subtle px-3 py-2 text-metadata text-info-subtle-foreground"
    >
      <Info aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
      {children}
    </p>
  );
}

function MemberRow({
  member,
  members,
  team,
  copy,
  canEdit,
  isSelf,
  pending,
  saving,
  noticeId,
  onChangeRole,
  onConfirmRemove,
}: {
  member: TeamMember;
  members: readonly TeamMember[];
  team: Team;
  copy: WorkspaceTeamsCopy;
  canEdit: boolean;
  isSelf: boolean;
  /** This member's own write is in flight (row-level feedback). */
  pending: boolean;
  /** Any Team write is in flight; each write needs the version the last one bumps. */
  saving: boolean;
  noticeId: string | undefined;
  onChangeRole: WorkspaceTeamSurfaceProps["onChangeRole"];
  onConfirmRemove: (member: TeamMember) => void;
}) {
  const caps = teamMemberCapabilities(member, members, team, canEdit);
  const reasonId = React.useId();
  const lastLead = caps.blockedBy === "last-lead";
  const editable = caps.roles.length > 1;
  const name = member.displayName;

  return (
    <li className="flex flex-wrap items-center gap-x-3 gap-y-2 border-t border-border px-4 py-2.5 first:border-t-0 hover:bg-state-hover sm:flex-nowrap">
      <span className="flex min-w-0 flex-1 basis-48 items-center gap-3">
        <IdentityMark
          name={name}
          tone={identityToneFor(member.userId)}
          shape="round"
        />
        <span className="flex min-w-0 flex-col">
          <span className="flex min-w-0 items-center gap-2">
            <span className="truncate text-label text-foreground">{name}</span>
            {isSelf ? (
              <Badge variant="secondary" className="shrink-0">
                {copy.members.you}
              </Badge>
            ) : null}
          </span>
          {member.email ? (
            <span className="truncate text-metadata text-muted-foreground">
              {member.email}
            </span>
          ) : null}
          {lastLead ? (
            <span
              id={reasonId}
              className="flex items-start gap-1 text-metadata text-muted-foreground"
            >
              <Info aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
              <span>{copy.blocked.lastLead}</span>
            </span>
          ) : null}
        </span>
      </span>
      <span className="flex shrink-0 items-center gap-1">
        {editable ? (
          <Select
            value={member.role}
            disabled={pending || saving}
            onValueChange={(value) => {
              const role = value as TeamRole;
              if (role !== member.role) onChangeRole(member.userId, role);
            }}
          >
            <SelectTrigger
              size="sm"
              className="w-32 focus-visible:ring-focus-ring sm:w-36"
              aria-label={formatCopy(
                pending ? copy.members.roleUpdating : copy.members.roleSelect,
                { name },
              )}
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {caps.roles.map((role) => (
                <SelectItem key={role} value={role} title={copy.roleHelp[role]}>
                  {copy.roles[role]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        ) : (
          <span
            className="w-32 text-body text-foreground sm:w-36"
            aria-describedby={lastLead ? reasonId : noticeId}
          >
            {copy.roles[member.role]}
          </span>
        )}
        <span className="flex size-8 items-center justify-center">
          {pending ? (
            <>
              <Spinner aria-hidden="true" />
              <span className="sr-only">
                {formatCopy(copy.members.removing, { name })}
              </span>
            </>
          ) : caps.canRemove ? (
            <Button
              variant="ghost"
              size="icon-sm"
              aria-label={formatCopy(copy.members.remove, { name })}
              disabled={saving}
              onClick={() => onConfirmRemove(member)}
              className="text-muted-foreground hover:bg-danger-subtle hover:text-danger-text focus-visible:ring-focus-ring"
            >
              <Trash2 aria-hidden="true" />
            </Button>
          ) : null}
        </span>
      </span>
    </li>
  );
}

function AddMemberForm({
  copy,
  addable,
  saving,
  onAddMember,
}: {
  copy: WorkspaceTeamsCopy;
  addable: readonly WorkspaceMember[];
  saving: boolean;
  onAddMember: WorkspaceTeamSurfaceProps["onAddMember"];
}) {
  const [filter, setFilter] = React.useState("");
  const [userId, setUserId] = React.useState("");
  const [role, setRole] = React.useState<TeamRole>("Member");
  const [touched, setTouched] = React.useState(false);
  const filterId = React.useId();
  const userFieldId = React.useId();
  const roleFieldId = React.useId();
  const errorId = React.useId();

  // Once the producer adds the person they leave the candidates; clear the choice.
  React.useEffect(() => {
    if (userId !== "" && !addable.some((member) => member.userId === userId)) {
      setUserId("");
      setTouched(false);
    }
  }, [addable, userId]);

  const needle = normalizeSearchText(filter.trim());
  const options = needle
    ? addable.filter(
        (member) =>
          member.userId === userId ||
          normalizeSearchText(member.name).includes(needle),
      )
    : addable;
  const missing = touched && userId === "";
  const hint =
    addable.length === 0
      ? copy.members.add.noCandidates
      : options.length === 0
        ? formatCopy(copy.members.add.noMatch, { query: filter.trim() })
        : null;

  return (
    <form
      noValidate
      aria-label={copy.members.add.title}
      onSubmit={(event) => {
        event.preventDefault();
        setTouched(true);
        if (userId === "" || saving) return;
        onAddMember({ userId, role });
      }}
      className="flex flex-col gap-3 rounded-lg border border-border bg-surface-panel p-4"
    >
      <h3 className="text-subheading text-foreground">
        {copy.members.add.title}
      </h3>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
        <Field className="sm:flex-1">
          <FieldLabel htmlFor={filterId}>
            {copy.members.add.filterLabel}
          </FieldLabel>
          <Input
            id={filterId}
            type="search"
            value={filter}
            placeholder={copy.members.add.filterPlaceholder}
            disabled={addable.length === 0}
            onChange={(event) => setFilter(event.target.value)}
          />
        </Field>
        <Field data-invalid={missing ? true : undefined} className="sm:flex-1">
          <FieldLabel htmlFor={userFieldId}>
            {copy.members.add.userLabel}
          </FieldLabel>
          <Select
            value={userId}
            onValueChange={setUserId}
            disabled={options.length === 0}
          >
            <SelectTrigger
              id={userFieldId}
              className="w-full"
              aria-invalid={missing ? true : undefined}
              aria-describedby={missing ? errorId : undefined}
            >
              <SelectValue placeholder={copy.members.add.userPlaceholder} />
            </SelectTrigger>
            <SelectContent>
              {options.map((member) => (
                <SelectItem key={member.userId} value={member.userId}>
                  {member.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {missing ? (
            <FieldError id={errorId}>{copy.members.add.required}</FieldError>
          ) : hint ? (
            <FieldDescription>{hint}</FieldDescription>
          ) : null}
        </Field>
        <Field className="sm:w-40">
          <FieldLabel htmlFor={roleFieldId}>
            {copy.members.add.roleLabel}
          </FieldLabel>
          <Select
            value={role}
            onValueChange={(value) => setRole(value as TeamRole)}
          >
            <SelectTrigger id={roleFieldId} className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {TEAM_ROLES.map((option) => (
                <SelectItem
                  key={option}
                  value={option}
                  title={copy.roleHelp[option]}
                >
                  {copy.roles[option]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Button
          type="submit"
          disabled={saving || addable.length === 0}
          className="sm:mt-8"
        >
          <UserPlus aria-hidden="true" />
          {saving ? copy.members.add.submitting : copy.members.add.submit}
        </Button>
      </div>
    </form>
  );
}

function DetailsSection({
  copy,
  team,
  saving,
  onRename,
  onDescribe,
}: Pick<
  WorkspaceTeamSurfaceProps,
  "copy" | "saving" | "onRename" | "onDescribe"
> & { team: Team }) {
  const [name, setName] = React.useState(team.name);
  const [description, setDescription] = React.useState(team.description ?? "");
  const [nameTouched, setNameTouched] = React.useState(false);
  const [last, setLast] = React.useState<"name" | "description" | null>(null);
  const nameId = React.useId();
  const nameErrorId = React.useId();
  const descriptionId = React.useId();
  const descriptionHelpId = React.useId();
  const titleId = React.useId();

  // A new server value replaces the form (after a save or a refresh).
  React.useEffect(() => {
    setName(team.name);
    setNameTouched(false);
  }, [team.name]);
  React.useEffect(() => {
    setDescription(team.description ?? "");
  }, [team.description]);

  const trimmedName = name.trim();
  const trimmedDescription = description.trim();
  const nameDirty = trimmedName !== team.name;
  const descriptionDirty = trimmedDescription !== (team.description ?? "");
  const nameMessage = !nameTouched
    ? null
    : trimmedName === ""
      ? copy.team.nameRequired
      : trimmedName.length > TEAM_NAME_MAX
        ? copy.team.nameTooLong
        : null;
  const descriptionMessage =
    trimmedDescription.length > TEAM_DESCRIPTION_MAX
      ? copy.team.descriptionTooLong
      : null;

  return (
    <section aria-labelledby={titleId} className="flex flex-col gap-4">
      <header className="flex flex-col gap-1">
        <h2 id={titleId} className="text-section-heading text-foreground">
          {copy.team.details.title}
        </h2>
        <p className="text-body text-muted-foreground">
          {copy.team.details.description}
        </p>
      </header>
      <form
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          setNameTouched(true);
          if (
            saving ||
            !nameDirty ||
            trimmedName === "" ||
            trimmedName.length > TEAM_NAME_MAX
          ) {
            return;
          }
          setLast("name");
          onRename(trimmedName);
        }}
        className="flex flex-col gap-3 rounded-lg border border-border bg-surface-panel p-4 sm:flex-row sm:items-start"
      >
        <Field
          data-invalid={nameMessage ? true : undefined}
          className="sm:flex-1"
        >
          <FieldLabel htmlFor={nameId}>{copy.team.nameLabel}</FieldLabel>
          <Input
            id={nameId}
            value={name}
            maxLength={TEAM_NAME_MAX}
            aria-invalid={nameMessage ? true : undefined}
            aria-describedby={nameMessage ? nameErrorId : undefined}
            onChange={(event) => setName(event.target.value)}
          />
          {nameMessage ? (
            <FieldError id={nameErrorId}>{nameMessage}</FieldError>
          ) : null}
        </Field>
        <Button
          type="submit"
          disabled={!nameDirty || saving}
          className="sm:mt-8"
        >
          {saving && last === "name" ? copy.team.saving : copy.team.rename}
        </Button>
      </form>
      <form
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          if (saving || !descriptionDirty || descriptionMessage) return;
          setLast("description");
          onDescribe(trimmedDescription === "" ? null : trimmedDescription);
        }}
        className="flex flex-col gap-3 rounded-lg border border-border bg-surface-panel p-4"
      >
        <Field data-invalid={descriptionMessage ? true : undefined}>
          <FieldLabel htmlFor={descriptionId}>
            {copy.team.descriptionLabel}
          </FieldLabel>
          <Textarea
            id={descriptionId}
            value={description}
            rows={3}
            aria-describedby={descriptionHelpId}
            aria-invalid={descriptionMessage ? true : undefined}
            onChange={(event) => setDescription(event.target.value)}
          />
          {descriptionMessage ? (
            <FieldError id={descriptionHelpId}>{descriptionMessage}</FieldError>
          ) : (
            <FieldDescription id={descriptionHelpId}>
              {copy.team.descriptionHelp}
            </FieldDescription>
          )}
        </Field>
        <div className="flex justify-end">
          <Button
            type="submit"
            disabled={
              !descriptionDirty || Boolean(descriptionMessage) || saving
            }
          >
            {saving && last === "description"
              ? copy.team.saving
              : copy.team.saveDescription}
          </Button>
        </div>
      </form>
    </section>
  );
}

/**
 * One Team (WX-TEAM-001, GOV-026): its details, its members with their Team
 * role, adding members, and archive. The last Lead is never offered a
 * demotion or removal; an archived Team's membership is frozen; both show
 * their reason as text. Workspace members who are not admins see it read-only.
 * Every write is a callback; the host owns the producer's outcome.
 */
export function WorkspaceTeamSurface(props: WorkspaceTeamSurfaceProps) {
  const { copy, team, members, viewer } = props;
  const [confirmation, setConfirmation] = React.useState<Confirmation | null>(
    null,
  );
  const noticeId = React.useId();
  const membersTitleId = React.useId();
  const dangerTitleId = React.useId();

  if (team.status === "loading") {
    return (
      <div className="mx-auto flex w-full max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6">
        <Skeleton className="h-8 w-64" />
        <RowsSkeleton label={copy.team.loading} />
      </div>
    );
  }
  if (team.status === "error") {
    return (
      <div className="mx-auto flex w-full max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6">
        <SectionError
          message={copy.team.error}
          retry={copy.team.retry}
          onRetry={props.onRetry}
        />
        <a
          href={props.backNavigation.getHref()}
          onClick={(event) => {
            if (!isPlainClick(event)) return;
            event.preventDefault();
            props.backNavigation.onNavigate();
          }}
          className="inline-flex w-fit items-center gap-1.5 rounded-sm text-label text-link outline-none hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring"
        >
          <ArrowLeft aria-hidden="true" className="size-4" />
          {copy.team.backToTeams}
        </a>
      </div>
    );
  }

  const current = team.data;
  const caps = teamCapabilities(current, viewer.role);
  const archived = current.status === "Archived";
  const list = members.status === "ready" ? members.data : [];
  const addable = addableTeamMembers(props.candidates, list);
  const memberCount =
    members.status === "ready" ? list.length : current.memberCount;
  const notice = !caps.canManage
    ? copy.readOnly
    : archived
      ? copy.archivedReason
      : null;

  const confirmText = (() => {
    if (!confirmation) return null;
    if (confirmation.type === "remove") {
      const values = {
        name: confirmation.member.displayName,
        team: current.name,
      };
      return {
        title: formatCopy(copy.confirmRemove.title, values),
        description: formatCopy(copy.confirmRemove.description, values),
        action: copy.confirmRemove.action,
        cancel: copy.confirmRemove.cancel,
      };
    }
    const values = { team: current.name };
    return confirmation.type === "archive"
      ? {
          title: formatCopy(copy.confirmArchive.title, values),
          description: copy.confirmArchive.description,
          action: copy.confirmArchive.action,
          cancel: copy.confirmArchive.cancel,
        }
      : {
          title: formatCopy(copy.confirmArchive.unarchiveTitle, values),
          description: copy.confirmArchive.unarchiveDescription,
          action: copy.confirmArchive.unarchiveAction,
          cancel: copy.confirmArchive.cancel,
        };
  })();

  const confirm = () => {
    if (!confirmation) return;
    if (confirmation.type === "remove") {
      props.onRemove(confirmation.member.userId);
    } else if (confirmation.type === "archive") {
      props.onArchive();
    } else {
      props.onUnarchive();
    }
    setConfirmation(null);
  };

  return (
    <div className="mx-auto flex w-full max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6">
      <header className="flex flex-col gap-1">
        <h1 className="flex flex-wrap items-center gap-2 text-page-title text-foreground">
          <span className="min-w-0 break-words">{current.name}</span>
          {archived ? (
            <Badge variant="secondary" className="font-normal">
              {copy.team.archivedBadge}
            </Badge>
          ) : null}
        </h1>
        <p className="text-metadata tabular-nums text-muted-foreground">
          {pluralForm(
            { one: copy.members.count_one, other: copy.members.count_other },
            memberCount,
          )}
        </p>
        {!caps.canEditDetails ? (
          <p className="max-w-prose break-words text-body text-muted-foreground">
            {current.description ?? copy.team.noDescription}
          </p>
        ) : null}
      </header>

      {notice ? <ReadOnlyNotice id={noticeId}>{notice}</ReadOnlyNotice> : null}

      {caps.canEditDetails ? (
        <DetailsSection
          copy={copy}
          team={current}
          saving={props.saving}
          onRename={props.onRename}
          onDescribe={props.onDescribe}
        />
      ) : null}

      <section aria-labelledby={membersTitleId} className="flex flex-col gap-4">
        <header className="flex flex-col gap-1">
          <h2
            id={membersTitleId}
            className="text-section-heading text-foreground"
          >
            {copy.members.title}
          </h2>
          <p className="text-body text-muted-foreground">
            {copy.members.description}
          </p>
        </header>
        {caps.canEditMembers && members.status === "ready" ? (
          <AddMemberForm
            copy={copy}
            addable={addable}
            saving={props.saving}
            onAddMember={props.onAddMember}
          />
        ) : null}
        {members.status === "loading" ? (
          <RowsSkeleton label={copy.members.loading} />
        ) : members.status === "error" ? (
          <SectionError
            message={copy.members.error}
            retry={copy.members.retry}
            onRetry={props.onRetry}
          />
        ) : list.length === 0 ? (
          <p className="rounded-lg border border-dashed border-border bg-surface-panel px-4 py-6 text-center text-body text-muted-foreground">
            {copy.members.empty}
          </p>
        ) : (
          <ul
            aria-label={copy.members.title}
            className="overflow-hidden rounded-lg border border-border bg-surface-panel"
          >
            {list.map((member) => (
              <MemberRow
                key={member.userId}
                member={member}
                members={list}
                team={current}
                copy={copy}
                canEdit={caps.canEditMembers}
                isSelf={
                  viewer.userId !== null && viewer.userId === member.userId
                }
                pending={props.pendingUserIds.has(member.userId)}
                saving={props.saving}
                noticeId={notice ? noticeId : undefined}
                onChangeRole={props.onChangeRole}
                onConfirmRemove={(target) =>
                  setConfirmation({ type: "remove", member: target })
                }
              />
            ))}
          </ul>
        )}
      </section>

      {caps.canManage ? (
        <section
          aria-labelledby={dangerTitleId}
          className="flex flex-col gap-3 rounded-lg border border-danger-subtle bg-surface-panel p-4 sm:p-6"
        >
          <header className="flex flex-col gap-1">
            <h2 id={dangerTitleId} className="text-subheading text-foreground">
              {copy.danger.title}
            </h2>
            <p className="text-metadata text-muted-foreground">
              {archived
                ? copy.danger.archivedDescription
                : copy.danger.description}
            </p>
          </header>
          <div>
            {archived ? (
              <Button
                variant="outline"
                disabled={props.saving}
                onClick={() => setConfirmation({ type: "unarchive" })}
              >
                <ArchiveRestore aria-hidden="true" />
                {copy.danger.unarchive}
              </Button>
            ) : (
              <Button
                variant="outline"
                disabled={props.saving}
                onClick={() => setConfirmation({ type: "archive" })}
                className="border-danger-subtle text-danger-text hover:bg-danger-subtle hover:text-danger-subtle-foreground"
              >
                <Archive aria-hidden="true" />
                {copy.danger.archive}
              </Button>
            )}
          </div>
        </section>
      ) : null}

      <AlertDialog
        open={confirmation !== null}
        onOpenChange={(open) => {
          if (!open) setConfirmation(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="break-words">
              {confirmText?.title}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {confirmText?.description}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{confirmText?.cancel}</AlertDialogCancel>
            <AlertDialogAction
              variant={
                confirmation?.type === "unarchive" ? "default" : "destructive"
              }
              onClick={confirm}
            >
              {confirmText?.action}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
