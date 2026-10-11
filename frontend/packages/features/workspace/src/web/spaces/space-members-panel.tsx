import * as React from "react";
import { Info, MoreHorizontal, Trash2, UserPlus, Users } from "lucide-react";
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Field,
  FieldError,
  FieldLabel,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Spinner,
  ToggleGroup,
  ToggleGroupItem,
} from "@notrelix/ui-web";
import { identityToneFor } from "../../core/model/navigation";
import {
  SPACE_ROLES,
  spaceMemberCapabilities,
  spaceMemberKey,
  type SpaceMemberEntry,
  type SpaceRole,
  type SpaceSubjectType,
  type SpaceTeam,
} from "../../core/model/spaces";
import { pluralForm, type SectionState } from "../home/home-models";
import { IdentityMark } from "../shell/shell-link";
import { formatCopy } from "../shell/shell-models";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

export interface SpaceMemberCandidate {
  readonly id: string;
  readonly name: string;
  readonly avatarUrl: string | null;
}

export interface AddSpaceMemberInput {
  readonly subjectType: SpaceSubjectType;
  readonly subjectId: string;
  readonly role: SpaceRole;
}

export interface SpaceMembersPanelProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly spaceName: string;
  readonly canManage: boolean;
  readonly members: SectionState<readonly SpaceMemberEntry[]>;
  /** Workspace members who could be added (active members of the workspace). */
  readonly users: SectionState<readonly SpaceMemberCandidate[]>;
  readonly teams: SectionState<readonly SpaceTeam[]>;
  /** Entries with a write in flight (`spaceMemberKey`). */
  readonly pendingKeys: ReadonlySet<string>;
  readonly adding: boolean;
  readonly addError: string | null;
  /** Incremented by the host after a successful add to clear the form. */
  readonly addResetKey: number;
  readonly onAdd: (input: AddSpaceMemberInput) => void;
  readonly onChangeRole: (entry: SpaceMemberEntry, role: SpaceRole) => void;
  readonly onRemove: (entry: SpaceMemberEntry) => void;
  readonly onRetry: () => void;
}

function AddMemberForm({
  copy,
  entries,
  users,
  teams,
  adding,
  addError,
  addResetKey,
  onAdd,
}: Pick<
  SpaceMembersPanelProps,
  "copy" | "users" | "teams" | "adding" | "addError" | "addResetKey" | "onAdd"
> & { entries: readonly SpaceMemberEntry[] }) {
  const [subjectType, setSubjectType] =
    React.useState<SpaceSubjectType>("User");
  const [subjectId, setSubjectId] = React.useState("");
  const [role, setRole] = React.useState<SpaceRole>("Member");
  const [touched, setTouched] = React.useState(false);
  const subjectFieldId = React.useId();
  const roleFieldId = React.useId();
  const errorId = React.useId();

  React.useEffect(() => {
    setSubjectId("");
    setTouched(false);
  }, [addResetKey]);

  const taken = new Set(entries.map(spaceMemberKey));
  const userOptions =
    users.status === "ready"
      ? users.data.filter(
          (user) =>
            !taken.has(
              spaceMemberKey({ subjectType: "User", subjectId: user.id }),
            ),
        )
      : [];
  const teamOptions =
    teams.status === "ready"
      ? teams.data.filter(
          (team) =>
            !taken.has(
              spaceMemberKey({ subjectType: "Team", subjectId: team.id }),
            ),
        )
      : [];
  const options = subjectType === "User" ? userOptions : teamOptions;
  const source = subjectType === "User" ? users : teams;
  const missing = touched && subjectId === "";
  const message = missing ? copy.members.add.required : addError;

  const emptyHint =
    source.status === "error"
      ? copy.members.add.teamsError
      : source.status === "ready" && options.length === 0
        ? subjectType === "User"
          ? copy.members.add.noUsers
          : copy.members.add.noTeams
        : null;

  return (
    <form
      noValidate
      aria-label={copy.members.add.title}
      onSubmit={(event) => {
        event.preventDefault();
        setTouched(true);
        if (subjectId === "" || adding) return;
        onAdd({ subjectType, subjectId, role });
      }}
      className="flex flex-col gap-3 rounded-lg border border-border bg-surface-panel p-4"
    >
      <h3 className="text-subheading text-foreground">
        {copy.members.add.title}
      </h3>
      <ToggleGroup
        type="single"
        variant="outline"
        size="sm"
        value={subjectType}
        onValueChange={(value) => {
          if (value === "User" || value === "Team") {
            setSubjectType(value);
            setSubjectId("");
          }
        }}
        aria-label={copy.members.add.subjectLabel}
        className="self-start"
      >
        <ToggleGroupItem value="User">
          {copy.members.add.userTab}
        </ToggleGroupItem>
        <ToggleGroupItem value="Team">
          {copy.members.add.teamTab}
        </ToggleGroupItem>
      </ToggleGroup>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
        <Field data-invalid={message ? true : undefined} className="sm:flex-1">
          <FieldLabel htmlFor={subjectFieldId}>
            {subjectType === "User"
              ? copy.members.add.userLabel
              : copy.members.add.teamLabel}
          </FieldLabel>
          <Select
            value={subjectId}
            onValueChange={setSubjectId}
            disabled={source.status !== "ready" || options.length === 0}
          >
            <SelectTrigger
              id={subjectFieldId}
              className="w-full"
              aria-invalid={message ? true : undefined}
              aria-describedby={message ? errorId : undefined}
            >
              <SelectValue
                placeholder={
                  subjectType === "User"
                    ? copy.members.add.userPlaceholder
                    : copy.members.add.teamPlaceholder
                }
              />
            </SelectTrigger>
            <SelectContent>
              {subjectType === "User"
                ? userOptions.map((user) => (
                    <SelectItem key={user.id} value={user.id}>
                      {user.name}
                    </SelectItem>
                  ))
                : teamOptions.map((team) => (
                    <SelectItem key={team.id} value={team.id}>
                      {team.name} ·{" "}
                      {pluralForm(
                        {
                          one: copy.members.add.teamMembers_one,
                          other: copy.members.add.teamMembers_other,
                        },
                        team.memberCount,
                      )}
                    </SelectItem>
                  ))}
            </SelectContent>
          </Select>
          {message ? (
            <FieldError id={errorId}>{message}</FieldError>
          ) : emptyHint ? (
            <p className="text-metadata text-muted-foreground">{emptyHint}</p>
          ) : null}
        </Field>
        <Field className="sm:w-40">
          <FieldLabel htmlFor={roleFieldId}>
            {copy.members.add.roleLabel}
          </FieldLabel>
          <Select
            value={role}
            onValueChange={(value) => setRole(value as SpaceRole)}
          >
            <SelectTrigger id={roleFieldId} className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {SPACE_ROLES.map((option) => (
                <SelectItem
                  key={option}
                  value={option}
                  description={copy.roleHelp[option]}
                >
                  {copy.roles[option]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Button type="submit" disabled={adding} className="sm:mt-8">
          <UserPlus aria-hidden="true" />
          {adding ? copy.members.add.submitting : copy.members.add.submit}
        </Button>
      </div>
    </form>
  );
}

function MemberRow({
  entry,
  entries,
  copy,
  canManage,
  pending,
  onChangeRole,
  onConfirmRemove,
}: {
  entry: SpaceMemberEntry;
  entries: readonly SpaceMemberEntry[];
  copy: WorkspaceSpacesCopy;
  canManage: boolean;
  pending: boolean;
  onChangeRole: SpaceMembersPanelProps["onChangeRole"];
  onConfirmRemove: (entry: SpaceMemberEntry) => void;
}) {
  const caps = spaceMemberCapabilities(entry, entries, canManage);
  const reasonId = React.useId();
  const lastAdmin = caps.blockedBy === "last-admin";
  const editable = caps.roles.length > 1;

  return (
    <li className="flex flex-wrap items-center gap-3 border-t border-border px-4 py-2.5 first:border-t-0 hover:bg-state-hover sm:flex-nowrap">
      <span className="flex min-w-0 flex-1 items-center gap-3">
        {entry.subjectType === "Team" ? (
          <span
            aria-hidden="true"
            className="flex size-8 shrink-0 items-center justify-center rounded-md border border-border bg-surface-raised text-icon"
          >
            <Users className="size-4" />
          </span>
        ) : (
          <IdentityMark
            name={entry.displayName}
            tone={identityToneFor(entry.subjectId)}
            shape="round"
            imageUrl={entry.avatarUrl}
          />
        )}
        <span className="flex min-w-0 flex-col">
          <span className="flex min-w-0 items-center gap-2">
            <span className="truncate text-label text-foreground">
              {entry.displayName}
            </span>
            <Badge variant="secondary" className="shrink-0">
              {entry.subjectType === "Team"
                ? copy.members.team
                : copy.members.user}
            </Badge>
          </span>
          {lastAdmin ? (
            <span
              id={reasonId}
              className="flex items-center gap-1 text-metadata text-muted-foreground"
            >
              <Info aria-hidden="true" className="size-3.5 shrink-0" />
              <span>{copy.members.lastAdmin}</span>
            </span>
          ) : null}
        </span>
      </span>
      <span className="flex shrink-0 items-center gap-1">
        {editable ? (
          <Select
            value={entry.role}
            disabled={pending}
            onValueChange={(value) => {
              const role = value as SpaceRole;
              if (role !== entry.role) onChangeRole(entry, role);
            }}
          >
            <SelectTrigger
              size="sm"
              className="w-32 focus-visible:ring-focus-ring"
              aria-label={formatCopy(
                pending ? copy.members.roleUpdating : copy.members.roleSelect,
                { name: entry.displayName },
              )}
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {caps.roles.map((role) => (
                <SelectItem
                  key={role}
                  value={role}
                  description={copy.roleHelp[role]}
                >
                  {copy.roles[role]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        ) : (
          <span
            className="w-32 text-body text-foreground"
            aria-describedby={lastAdmin ? reasonId : undefined}
          >
            {copy.roles[entry.role]}
          </span>
        )}
        <span className="flex size-8 items-center justify-center">
          {pending ? (
            <>
              <Spinner aria-hidden="true" />
              <span className="sr-only">
                {formatCopy(copy.members.roleUpdating, {
                  name: entry.displayName,
                })}
              </span>
            </>
          ) : caps.canRemove ? (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  aria-label={formatCopy(copy.members.rowMenu, {
                    name: entry.displayName,
                  })}
                  className="focus-visible:ring-focus-ring"
                >
                  <MoreHorizontal aria-hidden="true" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-52">
                <DropdownMenuItem
                  variant="destructive"
                  onSelect={() => onConfirmRemove(entry)}
                >
                  <Trash2 aria-hidden="true" />
                  {copy.members.remove}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          ) : null}
        </span>
      </span>
    </li>
  );
}

/**
 * Space members (WSP-024): Users and Teams with a Space role. The last Admin
 * entry is never offered a downgrade or removal, and the reason is shown.
 * Removal confirms in an `AlertDialog`. Read-only for non-managers.
 */
export function SpaceMembersPanel(props: SpaceMembersPanelProps) {
  const { copy, spaceName, canManage, members } = props;
  const [confirming, setConfirming] = React.useState<SpaceMemberEntry | null>(
    null,
  );
  const entries = members.status === "ready" ? members.data : [];
  const titleId = React.useId();

  return (
    <section aria-labelledby={titleId} className="flex flex-col gap-4">
      <header className="flex flex-col gap-1">
        <h2 id={titleId} className="text-section-heading text-foreground">
          {copy.members.title}
        </h2>
        <p className="text-body text-muted-foreground">
          {copy.members.description}
        </p>
      </header>
      {!canManage ? (
        <p className="flex items-center gap-2 rounded-md bg-info-subtle px-3 py-2 text-metadata text-info-subtle-foreground">
          <Info aria-hidden="true" className="size-4 shrink-0" />
          {copy.members.readOnly}
        </p>
      ) : null}
      {canManage && members.status === "ready" ? (
        <AddMemberForm {...props} entries={entries} />
      ) : null}
      {members.status === "loading" ? (
        <div
          role="status"
          aria-label={copy.members.loading}
          className="overflow-hidden rounded-lg border border-border bg-surface-panel"
        >
          {[0, 1, 2].map((row) => (
            <div
              key={row}
              className="flex h-14 items-center gap-3 border-b border-border px-4 last:border-0"
            >
              <Skeleton className="size-8 rounded-full" />
              <Skeleton className={row % 2 ? "h-3.5 w-40" : "h-3.5 w-28"} />
              <Skeleton className="ml-auto h-8 w-28" />
            </div>
          ))}
        </div>
      ) : members.status === "error" ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-surface-panel px-4 py-3"
        >
          <p className="text-body text-muted-foreground">
            {copy.members.error}
          </p>
          <Button variant="outline" size="sm" onClick={props.onRetry}>
            {copy.members.retry}
          </Button>
        </div>
      ) : entries.length === 0 ? (
        <p className="rounded-lg border border-dashed border-border bg-surface-panel px-4 py-6 text-center text-body text-muted-foreground">
          {copy.members.empty}
        </p>
      ) : (
        <ul
          aria-label={copy.members.title}
          className="overflow-hidden rounded-lg border border-border bg-surface-panel"
        >
          {entries.map((entry) => (
            <MemberRow
              key={spaceMemberKey(entry)}
              entry={entry}
              entries={entries}
              copy={copy}
              canManage={canManage}
              pending={props.pendingKeys.has(spaceMemberKey(entry))}
              onChangeRole={props.onChangeRole}
              onConfirmRemove={setConfirming}
            />
          ))}
        </ul>
      )}
      <AlertDialog
        open={confirming !== null}
        onOpenChange={(open) => {
          if (!open) setConfirming(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {confirming
                ? formatCopy(copy.members.confirm.removeTitle, {
                    name: confirming.displayName,
                    space: spaceName,
                  })
                : ""}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {copy.members.confirm.removeDescription}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{copy.members.confirm.cancel}</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              onClick={() => {
                if (confirming) props.onRemove(confirming);
                setConfirming(null);
              }}
            >
              {copy.members.confirm.removeAction}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </section>
  );
}
