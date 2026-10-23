import * as React from "react";
import {
  Info,
  Mail,
  MoreHorizontal,
  Search,
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Field,
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
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  cn,
} from "@notrelix/ui-web";
import {
  INVITABLE_ROLES,
  countOwners,
  invitationState,
  isPlausibleEmail,
  isWorkspaceAdmin,
  memberCapabilities,
} from "../../core/model/members";
import {
  identityToneFor,
  normalizeSearchText,
} from "../../core/model/navigation";
import type {
  CreateWorkspaceInvitationInput,
  WorkspaceInvitation,
  WorkspaceMember,
  WorkspaceRole,
} from "../../core/types/workspace";
import type { SectionState, SurfaceFormatters } from "../home/home-models";
import { IdentityMark } from "../shell/shell-link";
import { formatCopy } from "../shell/shell-models";
import type { WorkspaceMembersCopy } from "./workspace-members.copy";

export type MembersTab = "members" | "invitations";

export interface WorkspaceMembersSurfaceProps {
  readonly copy: WorkspaceMembersCopy;
  readonly formatters: SurfaceFormatters;
  /** Reference time for invitation expiry (epoch ms). */
  readonly now: number;
  readonly workspaceName: string | null;
  readonly viewer: {
    readonly userId: string | null;
    readonly role: WorkspaceRole | null;
  };
  readonly members: SectionState<readonly WorkspaceMember[]>;
  /** `null` hides the Invitations tab (members who are not admins). */
  readonly invitations: SectionState<readonly WorkspaceInvitation[]> | null;
  readonly tab: MembersTab;
  readonly onTabChange: (tab: MembersTab) => void;
  /** Members with a write in flight. */
  readonly pendingMemberIds: ReadonlySet<string>;
  readonly pendingInvitationIds: ReadonlySet<string>;
  readonly inviting: boolean;
  /** A field-level invite failure (the typed address is kept). */
  readonly inviteError: string | null;
  /** Incremented by the host after a successful invite to clear the form. */
  readonly inviteResetKey: number;
  readonly onChangeRole: (member: WorkspaceMember, role: WorkspaceRole) => void;
  readonly onRemove: (member: WorkspaceMember) => void;
  readonly onSuspend: (member: WorkspaceMember) => void;
  readonly onActivate: (member: WorkspaceMember) => void;
  readonly onInvite: (input: CreateWorkspaceInvitationInput) => void;
  readonly onRevoke: (invitation: WorkspaceInvitation) => void;
  readonly onRetryMembers: () => void;
  readonly onRetryInvitations: () => void;
}

type Confirmation =
  | { readonly type: "remove" | "suspend"; readonly member: WorkspaceMember }
  | { readonly type: "revoke"; readonly invitation: WorkspaceInvitation };

const DATE_FORMAT: Intl.DateTimeFormatOptions = { dateStyle: "medium" };

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

function RowsSkeleton({ label, rows = 5 }: { label: string; rows?: number }) {
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
          <Skeleton className={index % 2 ? "h-3.5 w-40" : "h-3.5 w-32"} />
          <Skeleton className="ml-auto h-8 w-28" />
        </div>
      ))}
    </div>
  );
}

function MemberRow({
  member,
  copy,
  formatters,
  viewer,
  ownerCount,
  pending,
  onChangeRole,
  onActivate,
  onConfirm,
}: {
  member: WorkspaceMember;
  copy: WorkspaceMembersCopy;
  formatters: SurfaceFormatters;
  viewer: WorkspaceMembersSurfaceProps["viewer"];
  ownerCount: number;
  pending: boolean;
  onChangeRole: WorkspaceMembersSurfaceProps["onChangeRole"];
  onActivate: WorkspaceMembersSurfaceProps["onActivate"];
  onConfirm: (confirmation: Confirmation) => void;
}) {
  const caps = memberCapabilities(member, viewer, ownerCount);
  const isSelf = viewer.userId !== null && viewer.userId === member.userId;
  const reasonId = React.useId();
  const blockedReason =
    caps.roleBlockedBy && caps.roleBlockedBy !== "not-admin"
      ? copy.blocked[caps.roleBlockedBy]
      : null;
  const hasActions = caps.canRemove || caps.canSuspend || caps.canActivate;

  return (
    <tr className="border-t border-border first:border-t-0 hover:bg-state-hover">
      <td className="w-full min-w-36 max-w-0 px-4 py-2.5 align-middle">
        <span className="flex min-w-0 items-center gap-3">
          <IdentityMark
            name={member.name}
            tone={identityToneFor(member.userId)}
            shape="round"
            imageUrl={member.avatarUrl}
          />
          <span className="flex min-w-0 flex-col">
            <span className="flex min-w-0 items-center gap-2">
              <span className="truncate text-label text-foreground">
                {member.name}
              </span>
              {isSelf ? (
                <Badge variant="secondary" className="shrink-0">
                  {copy.you}
                </Badge>
              ) : null}
            </span>
            {blockedReason ? (
              <span
                id={reasonId}
                className="flex items-center gap-1 text-metadata text-muted-foreground"
              >
                <Info aria-hidden="true" className="size-3.5 shrink-0" />
                <span className="truncate">{blockedReason}</span>
              </span>
            ) : null}
          </span>
        </span>
      </td>
      <td className="px-2 py-2.5 align-middle whitespace-nowrap">
        {caps.assignableRoles.length > 0 ? (
          <Select
            value={member.role}
            disabled={pending}
            onValueChange={(value) => {
              const role = value as WorkspaceRole;
              if (role !== member.role) onChangeRole(member, role);
            }}
          >
            <SelectTrigger
              size="sm"
              className="w-24 focus-visible:ring-focus-ring sm:w-36"
              aria-label={formatCopy(
                pending ? copy.roleUpdating : copy.roleSelect,
                { name: member.name },
              )}
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {caps.assignableRoles.map((role) => (
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
            className="text-body text-foreground"
            aria-describedby={blockedReason ? reasonId : undefined}
          >
            {copy.roles[member.role]}
          </span>
        )}
      </td>
      <td className="hidden px-2 py-2.5 align-middle whitespace-nowrap text-metadata tabular-nums text-muted-foreground md:table-cell">
        {member.joinedAt ? formatters.date(member.joinedAt, DATE_FORMAT) : "—"}
      </td>
      <td className="hidden px-2 py-2.5 align-middle whitespace-nowrap sm:table-cell">
        {member.membership === "suspended" ? (
          <Badge
            variant="outline"
            className="border-warning-subtle bg-warning-subtle text-warning-subtle-foreground"
          >
            {copy.status.suspended}
          </Badge>
        ) : member.membership === "active" ? (
          <span className="text-metadata text-muted-foreground">
            {copy.status.active}
          </span>
        ) : (
          <span
            className="text-metadata text-muted-foreground"
            title={copy.status.unknownHint}
          >
            <span aria-hidden="true">{copy.status.unknown}</span>
            <span className="sr-only">{copy.status.unknownHint}</span>
          </span>
        )}
      </td>
      <td className="w-12 px-2 py-2.5 text-right align-middle">
        {pending ? (
          <span className="inline-flex size-8 items-center justify-center">
            <Spinner aria-hidden="true" />
            <span className="sr-only">
              {formatCopy(copy.roleUpdating, { name: member.name })}
            </span>
          </span>
        ) : hasActions ? (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon-sm"
                aria-label={formatCopy(copy.rowMenu, { name: member.name })}
                className="focus-visible:ring-focus-ring"
              >
                <MoreHorizontal aria-hidden="true" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              {caps.canActivate ? (
                <DropdownMenuItem onSelect={() => onActivate(member)}>
                  {copy.actions.activate}
                </DropdownMenuItem>
              ) : null}
              {caps.canSuspend ? (
                <DropdownMenuItem
                  onSelect={() => onConfirm({ type: "suspend", member })}
                >
                  {copy.actions.suspend}
                </DropdownMenuItem>
              ) : null}
              {caps.canRemove ? (
                <>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    variant="destructive"
                    onSelect={() => onConfirm({ type: "remove", member })}
                  >
                    <Trash2 aria-hidden="true" />
                    {copy.actions.remove}
                  </DropdownMenuItem>
                </>
              ) : null}
            </DropdownMenuContent>
          </DropdownMenu>
        ) : null}
      </td>
    </tr>
  );
}

function InviteForm({
  copy,
  inviting,
  inviteError,
  inviteResetKey,
  onInvite,
}: Pick<
  WorkspaceMembersSurfaceProps,
  "copy" | "inviting" | "inviteError" | "inviteResetKey" | "onInvite"
>) {
  const [email, setEmail] = React.useState("");
  const [role, setRole] =
    React.useState<CreateWorkspaceInvitationInput["role"]>("member");
  const [touched, setTouched] = React.useState(false);
  const emailId = React.useId();
  const roleId = React.useId();
  const errorId = React.useId();

  React.useEffect(() => {
    setEmail("");
    setTouched(false);
  }, [inviteResetKey]);

  const invalid = touched && !isPlausibleEmail(email);
  const message = invalid ? copy.invitations.emailInvalid : inviteError;

  return (
    <form
      noValidate
      onSubmit={(event) => {
        event.preventDefault();
        setTouched(true);
        if (!isPlausibleEmail(email) || inviting) return;
        onInvite({ email: email.trim(), role });
      }}
      className="flex flex-col gap-3 rounded-lg border border-border bg-surface-panel p-4"
      aria-label={copy.invitations.formTitle}
    >
      <h2 className="text-subheading text-foreground">
        {copy.invitations.formTitle}
      </h2>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
        <Field data-invalid={message ? true : undefined} className="sm:flex-1">
          <FieldLabel htmlFor={emailId}>
            {copy.invitations.emailLabel}
          </FieldLabel>
          <Input
            id={emailId}
            type="email"
            autoComplete="email"
            value={email}
            placeholder={copy.invitations.emailPlaceholder}
            aria-invalid={message ? true : undefined}
            aria-describedby={message ? errorId : undefined}
            onChange={(event) => setEmail(event.target.value)}
            onBlur={() => email && setTouched(true)}
          />
          {message ? <FieldError id={errorId}>{message}</FieldError> : null}
        </Field>
        <Field className="sm:w-40">
          <FieldLabel htmlFor={roleId}>{copy.invitations.roleLabel}</FieldLabel>
          <Select
            value={role}
            onValueChange={(value) =>
              setRole(value as CreateWorkspaceInvitationInput["role"])
            }
          >
            <SelectTrigger id={roleId} className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {INVITABLE_ROLES.map((option) => (
                <SelectItem key={option} value={option}>
                  {copy.roles[option]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Button type="submit" disabled={inviting} className="sm:mt-8">
          <Mail aria-hidden="true" />
          {inviting ? copy.invitations.sending : copy.invitations.send}
        </Button>
      </div>
    </form>
  );
}

/**
 * Members and invitations (WX-MEM-001). Roles are edited inline; the last
 * owner is never offered a demotion, suspension or removal (WSP-008).
 * Destructive actions confirm in an `AlertDialog`; the host reports the
 * producer's outcome. Read-only for members who are not admins.
 */
export function WorkspaceMembersSurface(props: WorkspaceMembersSurfaceProps) {
  const {
    copy,
    formatters,
    now,
    workspaceName,
    viewer,
    members,
    invitations,
    tab,
    onTabChange,
  } = props;
  const [filter, setFilter] = React.useState("");
  const [confirmation, setConfirmation] = React.useState<Confirmation | null>(
    null,
  );
  const filterId = React.useId();
  const admin = isWorkspaceAdmin(viewer.role);
  const workspace = workspaceName ?? "";

  const memberList = members.status === "ready" ? members.data : [];
  const ownerCount = countOwners(memberList);
  const needle = normalizeSearchText(filter.trim());
  const shownMembers = needle
    ? memberList.filter((member) =>
        normalizeSearchText(member.name).includes(needle),
      )
    : memberList;

  const visibleInvitations =
    invitations?.status === "ready"
      ? invitations.data
          .filter(
            (invitation) => invitationState(invitation, now) !== "accepted",
          )
          .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
      : [];
  const pendingCount = visibleInvitations.filter(
    (invitation) => invitationState(invitation, now) === "pending",
  ).length;
  const activeTab: MembersTab = invitations ? tab : "members";

  const confirm = () => {
    if (!confirmation) return;
    switch (confirmation.type) {
      case "remove":
        props.onRemove(confirmation.member);
        break;
      case "suspend":
        props.onSuspend(confirmation.member);
        break;
      case "revoke":
        props.onRevoke(confirmation.invitation);
        break;
    }
    setConfirmation(null);
  };

  const confirmText = (() => {
    if (!confirmation) return null;
    if (confirmation.type === "revoke") {
      const values = { email: confirmation.invitation.email };
      return {
        title: formatCopy(copy.confirm.revokeTitle, values),
        description: copy.confirm.revokeDescription,
        action: copy.confirm.revokeAction,
      };
    }
    const values = { name: confirmation.member.name, workspace };
    return confirmation.type === "remove"
      ? {
          title: formatCopy(copy.confirm.removeTitle, values),
          description: formatCopy(copy.confirm.removeDescription, values),
          action: copy.confirm.removeAction,
        }
      : {
          title: formatCopy(copy.confirm.suspendTitle, values),
          description: formatCopy(copy.confirm.suspendDescription, values),
          action: copy.confirm.suspendAction,
        };
  })();

  const membersPanel = (
    <div className="flex flex-col gap-3">
      {!admin && members.status === "ready" ? (
        <p className="flex items-center gap-2 rounded-md bg-info-subtle px-3 py-2 text-metadata text-info-subtle-foreground">
          <Info aria-hidden="true" className="size-4 shrink-0" />
          {copy.readOnly}
        </p>
      ) : null}
      {members.status === "loading" ? (
        <RowsSkeleton label={copy.tabs.members} />
      ) : members.status === "error" ? (
        <SectionError
          message={copy.error.members}
          retry={copy.error.retry}
          onRetry={props.onRetryMembers}
        />
      ) : (
        <>
          <div className="relative sm:w-72">
            <label htmlFor={filterId} className="sr-only">
              {copy.filter.label}
            </label>
            <Search
              aria-hidden="true"
              className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground"
            />
            <Input
              id={filterId}
              type="search"
              value={filter}
              placeholder={copy.filter.placeholder}
              onChange={(event) => setFilter(event.target.value)}
              className="h-8 pl-8"
            />
          </div>
          {shownMembers.length === 0 ? (
            <p className="px-1 py-4 text-body text-muted-foreground">
              {formatCopy(copy.filter.noResults, { query: filter.trim() })}
            </p>
          ) : (
            <div className="overflow-x-auto rounded-lg border border-border bg-surface-panel">
              <table className="w-full border-collapse text-left">
                <thead>
                  <tr className="border-b border-border">
                    <th
                      scope="col"
                      className="h-9 px-4 text-metadata font-medium text-muted-foreground"
                    >
                      {copy.columns.member}
                    </th>
                    <th
                      scope="col"
                      className="h-9 px-2 text-metadata font-medium text-muted-foreground"
                    >
                      {copy.columns.role}
                    </th>
                    <th
                      scope="col"
                      className="hidden h-9 px-2 text-metadata font-medium text-muted-foreground md:table-cell"
                    >
                      {copy.columns.joined}
                    </th>
                    <th
                      scope="col"
                      className="hidden h-9 px-2 text-metadata font-medium text-muted-foreground sm:table-cell"
                    >
                      {copy.columns.status}
                    </th>
                    <th scope="col" className="h-9 px-2">
                      <span className="sr-only">{copy.columns.actions}</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {shownMembers.map((member) => (
                    <MemberRow
                      key={member.userId}
                      member={member}
                      copy={copy}
                      formatters={formatters}
                      viewer={viewer}
                      ownerCount={ownerCount}
                      pending={props.pendingMemberIds.has(member.userId)}
                      onChangeRole={props.onChangeRole}
                      onActivate={props.onActivate}
                      onConfirm={setConfirmation}
                    />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </div>
  );

  const invitationsPanel = invitations ? (
    <div className="flex flex-col gap-6">
      <InviteForm
        copy={copy}
        inviting={props.inviting}
        inviteError={props.inviteError}
        inviteResetKey={props.inviteResetKey}
        onInvite={props.onInvite}
      />
      <section
        aria-labelledby="pending-invitations"
        className="flex flex-col gap-3"
      >
        <h2
          id="pending-invitations"
          className="text-subheading text-foreground"
        >
          {copy.invitations.pendingTitle}
        </h2>
        {invitations.status === "loading" ? (
          <RowsSkeleton label={copy.invitations.pendingTitle} rows={3} />
        ) : invitations.status === "error" ? (
          <SectionError
            message={copy.error.invitations}
            retry={copy.error.retry}
            onRetry={props.onRetryInvitations}
          />
        ) : visibleInvitations.length === 0 ? (
          <p className="text-body text-muted-foreground">
            {copy.invitations.empty}
          </p>
        ) : (
          <div className="overflow-x-auto rounded-lg border border-border bg-surface-panel">
            <table className="w-full border-collapse text-left">
              <thead>
                <tr className="border-b border-border">
                  <th
                    scope="col"
                    className="h-9 px-4 text-metadata font-medium text-muted-foreground"
                  >
                    {copy.invitations.columns.email}
                  </th>
                  <th
                    scope="col"
                    className="h-9 px-2 text-metadata font-medium text-muted-foreground"
                  >
                    {copy.invitations.columns.role}
                  </th>
                  <th
                    scope="col"
                    className="hidden h-9 px-2 text-metadata font-medium text-muted-foreground sm:table-cell"
                  >
                    {copy.invitations.columns.sent}
                  </th>
                  <th
                    scope="col"
                    className="h-9 px-2 text-metadata font-medium text-muted-foreground"
                  >
                    {copy.invitations.columns.expires}
                  </th>
                  <th scope="col" className="h-9 px-2">
                    <span className="sr-only">
                      {copy.invitations.columns.actions}
                    </span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {visibleInvitations.map((invitation) => {
                  const expired =
                    invitationState(invitation, now) === "expired";
                  const pending = props.pendingInvitationIds.has(invitation.id);
                  return (
                    <tr
                      key={invitation.id}
                      className={cn(
                        "border-t border-border first:border-t-0 hover:bg-state-hover",
                        expired && "text-muted-foreground",
                      )}
                    >
                      <td className="w-full min-w-36 max-w-0 px-4 py-2.5 align-middle">
                        <span className="block truncate text-label">
                          {invitation.email}
                        </span>
                      </td>
                      <td className="px-2 py-2.5 align-middle whitespace-nowrap text-body">
                        {copy.roles[invitation.role]}
                      </td>
                      <td className="hidden px-2 py-2.5 align-middle whitespace-nowrap text-metadata tabular-nums text-muted-foreground sm:table-cell">
                        {formatters.date(invitation.createdAt, DATE_FORMAT)}
                      </td>
                      <td className="px-2 py-2.5 align-middle whitespace-nowrap text-metadata tabular-nums">
                        {expired ? (
                          <Badge variant="outline">
                            {copy.invitations.expired}
                          </Badge>
                        ) : (
                          <span className="text-muted-foreground">
                            {formatters.relativeTime(invitation.expiresAt, now)}
                          </span>
                        )}
                      </td>
                      <td className="w-12 px-2 py-2.5 text-right align-middle">
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          disabled={pending}
                          aria-label={formatCopy(
                            pending
                              ? copy.invitations.revoking
                              : copy.invitations.revoke,
                            { email: invitation.email },
                          )}
                          onClick={() =>
                            setConfirmation({ type: "revoke", invitation })
                          }
                          className="text-muted-foreground hover:bg-danger-subtle hover:text-danger-text focus-visible:ring-focus-ring"
                        >
                          {pending ? (
                            <Spinner aria-hidden="true" />
                          ) : (
                            <Trash2 aria-hidden="true" />
                          )}
                        </Button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  ) : null;

  return (
    <div className="mx-auto flex w-full max-w-5xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div className="flex min-w-0 flex-col gap-1">
          <h1 className="text-page-title text-foreground">{copy.title}</h1>
          <p className="text-body text-muted-foreground">
            {workspaceName
              ? formatCopy(copy.description, { workspace: workspaceName })
              : copy.descriptionLoading}
          </p>
        </div>
        {invitations && activeTab === "members" ? (
          <Button size="sm" onClick={() => onTabChange("invitations")}>
            <UserPlus aria-hidden="true" />
            {copy.invite}
          </Button>
        ) : null}
      </header>

      {invitations ? (
        <Tabs
          value={activeTab}
          onValueChange={(value) => onTabChange(value as MembersTab)}
        >
          <TabsList aria-label={copy.tabs.label}>
            <TabsTrigger value="members" onClick={() => onTabChange("members")}>
              {copy.tabs.members}
              {members.status === "ready" ? (
                <span className="tabular-nums text-muted-foreground">
                  {memberList.length}
                </span>
              ) : null}
            </TabsTrigger>
            <TabsTrigger
              value="invitations"
              onClick={() => onTabChange("invitations")}
            >
              {copy.tabs.invitations}
              {invitations.status === "ready" && pendingCount > 0 ? (
                <span className="tabular-nums text-muted-foreground">
                  {pendingCount}
                </span>
              ) : null}
            </TabsTrigger>
          </TabsList>
          <TabsContent value="members" className="mt-4 min-w-0">
            {membersPanel}
          </TabsContent>
          <TabsContent value="invitations" className="mt-4 min-w-0">
            {invitationsPanel}
          </TabsContent>
        </Tabs>
      ) : (
        membersPanel
      )}

      <AlertDialog
        open={confirmation !== null}
        onOpenChange={(open) => {
          if (!open) setConfirmation(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{confirmText?.title}</AlertDialogTitle>
            <AlertDialogDescription>
              {confirmText?.description}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{copy.confirm.cancel}</AlertDialogCancel>
            <AlertDialogAction variant="destructive" onClick={confirm}>
              {confirmText?.action}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
