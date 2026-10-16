import * as React from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import { identityToneFor, markToneFor } from "../../core/model/navigation";
import {
  teamCapabilities,
  teamMemberCapabilities,
  type Team,
  type TeamMember,
  type TeamRole,
} from "../../core/model/teams";
import type { WorkspaceRole } from "../../core/types/workspace";
import type { SectionState } from "../home/home-models";
import { IdentityMark } from "../shell/shell-link";
import { formatCopy, isPlainClick } from "../shell/shell-models";
import {
  BTN,
  BTN_PRIMARY,
  FOCUS,
  Glyph,
  SpaceAvatar,
} from "../spaces/space-page-parts";
import {
  ArchiveTeamDialog,
  TEAM_DESCRIPTION_MAX,
  TEAM_NAME_MAX,
  type TeamCandidate,
} from "./create-team-dialog";
import {
  TeamTile,
  WorkspaceSetupFrame,
  type SetupFrameProps,
} from "./workspace-setup-frame";
import {
  plainText,
  spaceLabel,
  teamInitials,
  type ArchiveLoss,
  type TeamExt,
} from "./workspace-teams-models";
import type { WorkspaceTeamsCopy } from "./workspace-teams.copy";

export interface AddTeamMemberInput {
  readonly userId: string;
  readonly role: TeamRole;
}

export interface WorkspaceTeamSurfaceProps {
  readonly copy: WorkspaceTeamsCopy;
  readonly frame: SetupFrameProps;
  readonly team: SectionState<Team>;
  readonly members: SectionState<readonly TeamMember[]>;
  /** Email, job line, day joined, who made it and where it is in use. */
  readonly ext: TeamExt | null;
  readonly viewer: {
    readonly userId: string | null;
    readonly role: WorkspaceRole | null;
  };
  /** Active people of the workspace who may join. */
  readonly candidates: readonly TeamCandidate[];
  readonly pendingUserIds: ReadonlySet<string>;
  readonly saving: boolean;
  readonly lossFor: ArchiveLoss | null;
  readonly formatDay: (iso: string) => string;
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
  readonly usageNavigation: {
    readonly getHref: (usage: TeamExt["usage"][number]) => string;
    readonly onNavigate: (usage: TeamExt["usage"][number]) => void;
  };
  readonly initialRoleMenuUserId?: string | null;
  readonly initialEditOpen?: boolean;
  readonly initialArchiveOpen?: boolean;
}

const INPUT =
  "box-border h-9 w-full rounded-[8px] border border-line-strong bg-popover px-2.5 text-body leading-[1.45] text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-state-selected dark:bg-card";

const ROLE_TAG: Record<TeamRole, string> = {
  Lead: "bg-primary",
  Member: "bg-success",
};

function RoleTag({ role }: { role: TeamRole }) {
  return (
    <span
      data-f="rtag"
      aria-hidden="true"
      className={cn(
        "inline-block size-2 shrink-0 rounded-[2px]",
        ROLE_TAG[role],
      )}
    />
  );
}

function RoleMenu({
  copy,
  member,
  caps,
  onPick,
  onRemove,
  onClose,
}: {
  copy: WorkspaceTeamsCopy;
  member: TeamMember;
  caps: ReturnType<typeof teamMemberCapabilities>;
  onPick: (role: TeamRole) => void;
  onRemove: () => void;
  onClose: () => void;
}) {
  const d = copy.detail;
  const ref = React.useRef<HTMLDivElement>(null);
  React.useEffect(() => {
    ref.current?.querySelector<HTMLElement>("[aria-checked='true']")?.focus();
    const away = (event: PointerEvent) => {
      if (!ref.current?.contains(event.target as Node)) onClose();
    };
    document.addEventListener("pointerdown", away);
    return () => document.removeEventListener("pointerdown", away);
  }, [onClose]);
  const locked = caps.blockedBy === "last-lead";
  const row =
    "mx-1 flex w-[calc(100%-8px)] items-start gap-2.5 rounded-[7px] border-0 bg-transparent px-3 py-[9px] text-left text-foreground hover:bg-state-hover-strong aria-disabled:cursor-not-allowed aria-disabled:opacity-55 aria-disabled:hover:bg-transparent";
  const roles: readonly { role: TeamRole; label: string; hint: string }[] = [
    { role: "Lead", label: d.roleLead, hint: d.roleHintLead },
    {
      role: "Member",
      label: d.roleMember,
      hint: locked
        ? formatCopy(d.roleLockedMember, {
            name: member.displayName.split(/\s+/).pop() ?? member.displayName,
          })
        : d.roleHintMember,
    },
  ];
  return (
    <div
      ref={ref}
      data-f="tmenu"
      role="menu"
      aria-label={formatCopy(d.roleMenuLabel, { name: member.displayName })}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          event.stopPropagation();
          onClose();
        }
      }}
      className="absolute top-12 right-16 z-20 w-[300px] rounded-[10px] border border-line-strong bg-popover py-1 shadow-dialog dark:bg-card"
    >
      {roles.map((item) => {
        const checked = member.role === item.role;
        const disabled = !checked && !caps.roles.includes(item.role);
        return (
          <button
            key={item.role}
            type="button"
            role="menuitemradio"
            aria-checked={checked}
            aria-disabled={disabled ? true : undefined}
            onClick={() => {
              if (disabled) return;
              if (!checked) onPick(item.role);
              onClose();
            }}
            className={cn(row, FOCUS)}
          >
            <span className="mt-1.5">
              <RoleTag role={item.role} />
            </span>
            <span>
              <b className="block text-body leading-[1.45] font-semibold">
                {item.label}
              </b>
              <small className="block text-xs leading-[1.45] text-meta-foreground">
                {item.hint}
              </small>
            </span>
            {checked ? (
              <Glyph
                name="check"
                className="mt-0.5 ml-auto size-4 text-state-selected-foreground"
              />
            ) : null}
          </button>
        );
      })}
      <div role="separator" className="my-1 h-px bg-line" />
      <button
        type="button"
        role="menuitem"
        aria-disabled={caps.canRemove ? undefined : true}
        onClick={() => {
          if (!caps.canRemove) return;
          onRemove();
          onClose();
        }}
        className={cn(row, FOCUS)}
      >
        <span className="w-2" />
        <span>
          <b className="block text-body leading-[1.45] font-semibold text-danger-text">
            {d.removeFromTeam}
          </b>
          <small className="block text-xs leading-[1.45] text-meta-foreground">
            {caps.canRemove ? "" : d.removeLocked}
          </small>
        </span>
      </button>
      {locked ? (
        <div
          id="team-lead-rule"
          className="mt-1 flex gap-2 border-t border-line px-3 pt-2 pb-1.5 text-xs leading-normal text-muted-foreground"
        >
          <Glyph
            name="infoCircle"
            className="mt-px size-3.5 text-meta-foreground"
          />
          <span>{d.leadNote}</span>
        </div>
      ) : null}
    </div>
  );
}

function EditDialog({
  copy,
  open,
  team,
  saving,
  onClose,
  onRename,
  onDescribe,
}: {
  copy: WorkspaceTeamsCopy;
  open: boolean;
  team: Team;
  saving: boolean;
  onClose: () => void;
  onRename: (name: string) => void;
  onDescribe: (description: string | null) => void;
}) {
  const d = copy.detail;
  const [name, setName] = React.useState(team.name);
  const [description, setDescription] = React.useState(team.description ?? "");
  React.useEffect(() => {
    if (!open) return;
    setName(team.name);
    setDescription(team.description ?? "");
  }, [open, team.description, team.name]);
  const trimmed = name.trim();
  const valid =
    trimmed !== "" &&
    trimmed.length <= TEAM_NAME_MAX &&
    description.length <= TEAM_DESCRIPTION_MAX;
  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!valid) return;
    if (trimmed !== team.name) onRename(trimmed);
    const next = description.trim() === "" ? null : description.trim();
    if (next !== (team.description ?? null)) onDescribe(next);
    onClose();
  };
  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        className="top-[110px] box-border flex w-[470px] max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 text-body shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card"
      >
        <form onSubmit={submit} noValidate className="contents">
          <div className="px-5 pt-[18px] pb-1.5">
            <DialogTitle className="m-0 text-dialog-title font-[650] text-foreground">
              {d.editTitle}
            </DialogTitle>
          </div>
          <DialogDescription className="sr-only">
            {d.editTitle}
          </DialogDescription>
          <div className="flex flex-col gap-3.5 px-5 pt-3.5 pb-4">
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="team-edit-name"
                className="text-crumb font-semibold text-muted-foreground"
              >
                {d.editName}
              </label>
              <input
                id="team-edit-name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                className={INPUT}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="team-edit-desc"
                className="text-crumb font-semibold text-muted-foreground"
              >
                {d.editDescription}{" "}
                <span className="font-medium text-meta-foreground">
                  {d.editOptional}
                </span>
              </label>
              <textarea
                id="team-edit-desc"
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                className={cn(INPUT, "h-16 resize-none py-2")}
              />
            </div>
          </div>
          <div className="flex items-center gap-2 border-t border-line bg-background px-5 py-3.5">
            <span className="flex-1" />
            <button type="button" onClick={onClose} className={cn(BTN, FOCUS)}>
              {d.editCancel}
            </button>
            <button
              type="submit"
              disabled={!valid || saving}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              {d.editSave}
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

/**
 * One Team inside "Thiết lập workspace" (Final-Teams `chi-tiet`): its people
 * with their role, the day they joined and Remove; add people by name or
 * email; where the Team has access; what a lead is; archive. Only workspace
 * admins write; an archived Team is read-only until it is restored.
 */
export function WorkspaceTeamSurface(props: WorkspaceTeamSurfaceProps) {
  const { copy, team, members, ext, viewer } = props;
  const d = copy.detail;
  const [roleMenu, setRoleMenu] = React.useState<string | null>(
    props.initialRoleMenuUserId ?? null,
  );
  const [editOpen, setEditOpen] = React.useState(
    props.initialEditOpen ?? false,
  );
  const [archiveOpen, setArchiveOpen] = React.useState(
    props.initialArchiveOpen ?? false,
  );
  const [moreOpen, setMoreOpen] = React.useState(false);
  const [query, setQuery] = React.useState("");
  const closeRoleMenu = React.useCallback(() => setRoleMenu(null), []);
  const addInput = React.useRef<HTMLInputElement>(null);

  const back = (
    <a
      data-f="back"
      href={props.backNavigation.getHref()}
      onClick={(event) => {
        if (!isPlainClick(event)) return;
        event.preventDefault();
        props.backNavigation.onNavigate();
      }}
      className={cn(
        "inline-flex items-center gap-1.5 self-start text-compact font-medium text-state-selected-foreground",
        FOCUS,
      )}
    >
      <Glyph name="chevronLeft" className="size-3.5" />
      {d.back}
    </a>
  );

  if (team.status === "loading") {
    return (
      <WorkspaceSetupFrame copy={copy} frame={props.frame}>
        <div
          role="status"
          aria-label={copy.team.loading}
          className="flex flex-col gap-3"
        >
          <Skeleton className="h-4 w-16" />
          <Skeleton className="h-11 w-72" />
          <Skeleton className="h-40 w-full rounded-[12px]" />
        </div>
      </WorkspaceSetupFrame>
    );
  }
  if (team.status === "error") {
    return (
      <WorkspaceSetupFrame copy={copy} frame={props.frame}>
        {back}
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-[12px] border border-line bg-popover px-4 py-3 dark:bg-card"
        >
          <p className="m-0 text-body text-muted-foreground">
            {copy.team.error}
          </p>
          <button
            type="button"
            onClick={props.onRetry}
            className={cn(BTN, FOCUS)}
          >
            {copy.team.retry}
          </button>
        </div>
      </WorkspaceSetupFrame>
    );
  }

  const t = team.data;
  const caps = teamCapabilities(t, viewer.role);
  const archived = t.status === "Archived";
  const list = members.status === "ready" ? members.data : [];
  const info = new Map(
    (ext?.people ?? []).map((person) => [person.userId, person]),
  );
  const inTeam = new Set(list.map((member) => member.userId));
  const suggestions = (() => {
    const needle = plainText(query.trim());
    if (!needle) return [];
    return props.candidates
      .filter(
        (candidate) =>
          !inTeam.has(candidate.id) &&
          plainText(`${candidate.name} ${candidate.email ?? ""}`).includes(
            needle,
          ),
      )
      .slice(0, 6);
  })();
  const meta = formatCopy(t.description ? d.meta : d.metaNoDescription, {
    description: t.description ?? "",
    date: props.formatDay(ext?.createdAt ?? t.createdAt),
    name: ext?.createdBy?.name ?? "",
  });

  return (
    <WorkspaceSetupFrame copy={copy} frame={props.frame}>
      {back}
      <div data-f="dhead" className="flex items-center gap-3.5">
        <TeamTile
          name={t.name}
          tone={identityToneFor(t.id)}
          className="size-11 rounded-[12px] text-mark-lg"
        />
        <div className="min-w-0">
          <h2 className="m-0 text-xl leading-[1.35] font-[650] tracking-[-0.01em] text-foreground">
            {t.name}
          </h2>
          <small className="mt-0.5 block text-crumb text-meta-foreground">
            {meta}
          </small>
        </div>
        <div className="ml-auto flex items-center gap-2">
          {caps.canEditDetails ? (
            <button
              type="button"
              onClick={() => setEditOpen(true)}
              className={cn(BTN, FOCUS)}
            >
              {d.edit}
            </button>
          ) : null}
          {caps.canEditMembers ? (
            <button
              type="button"
              onClick={() => addInput.current?.focus()}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              <Glyph name="userPlus" />
              {d.addPeople}
            </button>
          ) : null}
          {caps.canManage ? (
            <span className="relative">
              <button
                type="button"
                aria-haspopup="menu"
                aria-expanded={moreOpen}
                aria-label={formatCopy(d.more, { name: t.name })}
                onClick={() => setMoreOpen((open) => !open)}
                className={cn(
                  "grid size-8 place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
                  FOCUS,
                )}
              >
                <Glyph name="more" />
              </button>
              {moreOpen ? (
                <div
                  role="menu"
                  onKeyDown={(event) => {
                    if (event.key === "Escape") setMoreOpen(false);
                  }}
                  className="absolute top-9 right-0 z-20 w-[190px] rounded-[10px] border border-line-strong bg-popover p-1 shadow-dialog dark:bg-card"
                >
                  <button
                    type="button"
                    role="menuitem"
                    onClick={() => {
                      setMoreOpen(false);
                      if (archived) props.onUnarchive();
                      else setArchiveOpen(true);
                    }}
                    className={cn(
                      "flex h-9 w-full items-center rounded-[7px] border-0 bg-transparent px-2.5 text-left text-compact hover:bg-state-hover-strong",
                      archived ? "text-foreground" : "text-danger-text",
                      FOCUS,
                    )}
                  >
                    {archived ? d.restore : d.archive}
                  </button>
                </div>
              ) : null}
            </span>
          ) : null}
          {caps.canManage && archived ? (
            <button
              type="button"
              disabled={props.saving}
              onClick={props.onUnarchive}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              <Glyph name="archive" />
              {d.restore}
            </button>
          ) : null}
        </div>
      </div>
      {archived ? (
        <p
          role="status"
          className="m-0 rounded-[8px] border border-line bg-background px-3 py-2 text-crumb text-muted-foreground"
        >
          {d.archivedBanner}
        </p>
      ) : null}
      <div
        data-f="dgrid"
        className="grid grid-cols-[minmax(0,1fr)_300px] items-start gap-5"
      >
        <section
          aria-labelledby="team-people"
          className="rounded-[12px] border border-line bg-popover dark:bg-card"
        >
          <div
            data-f="card-t"
            className="flex items-center gap-2 border-b border-line px-3.5 py-3 text-compact font-semibold"
          >
            <span id="team-people">
              {d.peopleTitle}{" "}
              <span className="font-normal text-meta-foreground">
                {t.memberCount}
              </span>
            </span>
          </div>
          {members.status === "loading" ? (
            <div
              role="status"
              aria-label={copy.members.loading}
              className="flex flex-col gap-2 p-3.5"
            >
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          ) : members.status === "error" ? (
            <div
              role="alert"
              className="flex items-center justify-between gap-3 px-3.5 py-3"
            >
              <p className="m-0 text-body text-muted-foreground">
                {copy.members.error}
              </p>
              <button
                type="button"
                onClick={props.onRetry}
                className={cn(BTN, FOCUS)}
              >
                {copy.members.retry}
              </button>
            </div>
          ) : (
            list.map((member, index) => {
              const person = info.get(member.userId);
              const memberCaps = teamMemberCapabilities(
                member,
                list,
                t,
                caps.canEditMembers,
              );
              const pending = props.pendingUserIds.has(member.userId);
              const open = roleMenu === member.userId;
              const roleLabel =
                member.role === "Lead" ? d.roleLead : d.roleMember;
              return (
                <div
                  key={member.userId}
                  data-f="pr"
                  className={cn(
                    "relative grid min-h-14 grid-cols-[minmax(0,1fr)_140px_74px_44px] items-center gap-3 px-3.5",
                    index > 0 && "border-t border-line",
                    open && "bg-state-selected",
                  )}
                >
                  <span
                    data-f="who"
                    className="flex min-w-0 items-center gap-[11px]"
                  >
                    <SpaceAvatar
                      person={{
                        id: member.userId,
                        name: member.displayName,
                        tone: identityToneFor(member.userId),
                      }}
                      className="size-[30px] text-avatar-lg"
                    />
                    <span className="min-w-0">
                      <b className="block truncate text-body leading-[1.45] font-semibold text-foreground">
                        {member.displayName}
                        {member.userId === viewer.userId ? (
                          <span className="ml-1.5 rounded-[4px] bg-state-selected px-[5px] py-px text-key font-semibold text-state-selected-foreground">
                            {copy.members.you}
                          </span>
                        ) : null}
                      </b>
                      <small className="block truncate text-xs leading-[1.45] text-meta-foreground">
                        {[person?.email ?? member.email, person?.title]
                          .filter(Boolean)
                          .join(" · ")}
                      </small>
                    </span>
                  </span>
                  <span>
                    <button
                      type="button"
                      aria-haspopup="menu"
                      aria-expanded={open}
                      aria-disabled={caps.canEditMembers ? undefined : true}
                      disabled={pending}
                      aria-label={formatCopy(d.roleAria, {
                        name: member.displayName,
                        role: roleLabel,
                      })}
                      onClick={
                        caps.canEditMembers
                          ? () =>
                              setRoleMenu((current) =>
                                current === member.userId
                                  ? null
                                  : member.userId,
                              )
                          : undefined
                      }
                      className={cn(
                        "inline-flex h-[30px] items-center gap-1.5 rounded-[7px] border border-transparent bg-transparent px-[9px] text-compact leading-[1.45] font-medium text-foreground",
                        caps.canEditMembers
                          ? "hover:border-line-strong hover:bg-popover dark:hover:bg-card"
                          : "cursor-default",
                        open &&
                          "border-primary bg-popover ring-2 ring-state-selected dark:bg-card",
                        FOCUS,
                      )}
                    >
                      <RoleTag role={member.role} />
                      {roleLabel}
                      {caps.canEditMembers ? (
                        <Glyph
                          name="chevronDown"
                          className="size-[13px] text-meta-foreground"
                        />
                      ) : null}
                    </button>
                  </span>
                  <span className="text-xs text-meta-foreground">
                    {formatCopy(d.since, {
                      date: props.formatDay(person?.since ?? t.createdAt),
                    })}
                  </span>
                  <span>
                    {caps.canEditMembers ? (
                      <button
                        type="button"
                        disabled={!memberCaps.canRemove || pending}
                        title={memberCaps.canRemove ? undefined : d.leadRule}
                        aria-label={formatCopy(d.removeAria, {
                          name: member.displayName,
                        })}
                        onClick={() => props.onRemove(member.userId)}
                        className={cn(
                          "border-0 bg-transparent p-0 text-crumb font-semibold",
                          memberCaps.canRemove
                            ? "cursor-pointer text-danger-text"
                            : "cursor-not-allowed text-meta-foreground opacity-70",
                          FOCUS,
                        )}
                      >
                        {d.remove}
                      </button>
                    ) : null}
                  </span>
                  {open ? (
                    <RoleMenu
                      copy={copy}
                      member={member}
                      caps={memberCaps}
                      onPick={(role) => props.onChangeRole(member.userId, role)}
                      onRemove={() => props.onRemove(member.userId)}
                      onClose={closeRoleMenu}
                    />
                  ) : null}
                </div>
              );
            })
          )}
          {caps.canEditMembers ? (
            <div
              data-f="addrow"
              className="relative flex flex-col gap-1.5 rounded-b-[12px] border-t border-line bg-background px-3.5 pt-3 pb-3.5"
            >
              <label className="flex h-8 w-full items-center gap-2 rounded-[7px] border border-line-strong bg-popover px-2.5 text-compact text-meta-foreground focus-within:border-primary focus-within:ring-2 focus-within:ring-state-selected dark:bg-card">
                <Glyph name="userPlus" />
                <span className="sr-only">{d.addPeople}</span>
                <input
                  ref={addInput}
                  role="combobox"
                  aria-expanded={suggestions.length > 0}
                  aria-controls="team-add-list"
                  aria-describedby="team-add-hint"
                  autoComplete="off"
                  value={query}
                  placeholder={formatCopy(d.addPlaceholder, {
                    workspace: props.frame.workspaceName,
                  })}
                  onChange={(event) => setQuery(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter" && suggestions[0]) {
                      event.preventDefault();
                      props.onAddMember({
                        userId: suggestions[0].id,
                        role: "Member",
                      });
                      setQuery("");
                    }
                  }}
                  className="h-full min-w-0 flex-1 border-0 bg-transparent text-compact text-foreground outline-none placeholder:text-meta-foreground"
                />
              </label>
              {suggestions.length > 0 ? (
                <ul
                  id="team-add-list"
                  role="listbox"
                  className="m-0 list-none rounded-[8px] border border-line-strong bg-popover p-1 dark:bg-card"
                >
                  {suggestions.map((person, index) => (
                    <li
                      key={person.id}
                      role="option"
                      aria-selected={index === 0}
                      onMouseDown={(event) => {
                        event.preventDefault();
                        props.onAddMember({
                          userId: person.id,
                          role: "Member",
                        });
                        setQuery("");
                      }}
                      className={cn(
                        "flex cursor-pointer items-center gap-2 rounded-[6px] px-2 py-1.5 text-compact",
                        index === 0 && "bg-state-hover-strong",
                      )}
                    >
                      {person.name}
                      {person.email ? (
                        <small className="text-xs text-meta-foreground">
                          {person.email}
                        </small>
                      ) : null}
                    </li>
                  ))}
                </ul>
              ) : query.trim() !== "" ? (
                <p className="m-0 text-xs text-meta-foreground">
                  {formatCopy(d.addNoMatch, { query: query.trim() })}
                </p>
              ) : null}
              <span
                id="team-add-hint"
                className="text-xs leading-normal text-meta-foreground"
              >
                {d.addHint}
              </span>
            </div>
          ) : null}
        </section>
        <div className="flex flex-col gap-3.5">
          <section
            aria-labelledby="team-grant"
            className="flex flex-col gap-2.5 rounded-[12px] border border-line bg-popover p-3.5 dark:bg-card"
          >
            <h3
              id="team-grant"
              className="m-0 text-compact font-semibold text-muted-foreground"
            >
              {d.usageTitle}
            </h3>
            {(ext?.usage ?? []).length === 0 ? (
              <p className="m-0 text-crumb leading-[1.55] text-muted-foreground">
                {d.usageNone}
              </p>
            ) : (
              (ext?.usage ?? []).map((usage) => (
                <a
                  key={`${usage.kind}:${usage.id}`}
                  data-f="grant"
                  href={props.usageNavigation.getHref(usage)}
                  onClick={(event) => {
                    if (!isPlainClick(event)) return;
                    event.preventDefault();
                    props.usageNavigation.onNavigate(usage);
                  }}
                  className={cn(
                    "-mx-2 flex items-center gap-2.5 rounded-[8px] p-2 hover:bg-state-hover-strong",
                    FOCUS,
                  )}
                >
                  {usage.kind === "space" ? (
                    <IdentityMark
                      name={usage.name}
                      markTone={markToneFor(usage.id)}
                      className="size-7 rounded-[8px] text-mark-xs font-bold"
                    />
                  ) : (
                    <span className="grid size-7 place-items-center rounded-[8px] bg-state-hover-strong text-muted-foreground">
                      <Glyph name="kanban" />
                    </span>
                  )}
                  <span className="min-w-0">
                    <b className="block text-nav font-semibold text-foreground">
                      {usage.kind === "space"
                        ? spaceLabel(copy, usage.name, usage.spaceType)
                        : usage.name}
                    </b>
                    <small className="block text-xs text-meta-foreground">
                      {usage.kind === "space"
                        ? formatCopy(d.usageSpace, {
                            role: copy.page.spaceRoles[
                              usage.grant as "Admin" | "Member" | "Viewer"
                            ],
                            date: props.formatDay(usage.since ?? t.createdAt),
                          })
                        : formatCopy(d.usageBoard, {
                            level:
                              copy.page.boardLevels[
                                usage.grant as
                                  "manager" | "editor" | "commenter" | "viewer"
                              ],
                            date: props.formatDay(usage.since ?? t.createdAt),
                          })}
                      {archived ? ` · ${d.usagePaused}` : ""}
                    </small>
                  </span>
                  <Glyph
                    name="chevronRight"
                    className="ml-auto size-3.5 text-meta-foreground"
                  />
                </a>
              ))
            )}
            <p className="m-0 text-crumb leading-[1.55] text-muted-foreground">
              {d.usageNoteBefore}
              <b className="font-semibold text-foreground">{d.usageNoteBold}</b>
              {d.usageNoteAfter}
            </p>
            <p className="m-0 text-crumb leading-[1.55] text-muted-foreground">
              {d.usageNoteChange}
            </p>
          </section>
          <section
            aria-labelledby="team-lead-what"
            className="flex flex-col gap-2.5 rounded-[12px] border border-line bg-popover p-3.5 dark:bg-card"
          >
            <h3
              id="team-lead-what"
              className="m-0 text-compact font-semibold text-muted-foreground"
            >
              {d.leadTitle}
            </h3>
            <p className="m-0 text-crumb leading-[1.55] text-muted-foreground">
              {d.leadText}
            </p>
          </section>
          {caps.canManage && !archived ? (
            <button
              type="button"
              onClick={() => setArchiveOpen(true)}
              className={cn(
                "inline-flex h-8 items-center gap-1.5 self-start rounded-[7px] border border-transparent bg-transparent px-3 text-compact font-medium text-danger-text hover:bg-state-hover-strong",
                FOCUS,
              )}
            >
              <Glyph name="archive" />
              {d.archive}
            </button>
          ) : null}
        </div>
      </div>
      <EditDialog
        copy={copy}
        open={editOpen}
        team={t}
        saving={props.saving}
        onClose={() => setEditOpen(false)}
        onRename={props.onRename}
        onDescribe={props.onDescribe}
      />
      <ArchiveTeamDialog
        copy={copy}
        open={archiveOpen}
        teamName={t.name}
        loss={props.lossFor}
        pending={props.saving}
        onClose={() => setArchiveOpen(false)}
        onConfirm={() => {
          props.onArchive();
          setArchiveOpen(false);
        }}
      />
    </WorkspaceSetupFrame>
  );
}

export { teamInitials };
