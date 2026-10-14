import {
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { cn, Skeleton } from "@notrelix/ui-web";
import {
  identityToneFor,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import {
  type SpaceMemberEntry,
  type SpaceRole,
  type SpaceTeam,
} from "../../core/model/spaces";
import type { SectionState } from "../home/home-models";
import { formatCopy, type ShellNavigation } from "../shell/shell-models";
import { ShellLink } from "../shell/shell-link";
import {
  buildMemberRows,
  filterMemberRows,
  memberSummary,
  NO_MEMBER_FILTERS,
  type MemberFilters,
  type MemberRoleFilter,
  type MemberViaFilter,
  type SpaceDirectoryPerson,
  type SpacePendingInviteView,
  type SpaceMemberRowView,
  type TeamPersonView,
} from "./space-members-models";
import { FOCUS, Glyph, SpaceAvatar } from "./space-page-parts";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

/**
 * The members tab of a department (Final-Members): the toolbar, the list of
 * people and teams with the role menu, and the aside that explains the roles.
 * Roles and removals are the Space member writes; every one is authorized by
 * the producer, and the last admin cannot be lowered or removed (WSP-024).
 */

export const ROLE_TAG: Record<SpaceRole, string> = {
  Admin: "bg-primary",
  Member: "bg-work-status-done",
  Viewer: "bg-work-status-todo",
};

const DEFAULT_PAGE = 10;

export function RoleTag({
  role,
  className,
  shrinks = false,
}: {
  role: SpaceRole;
  className?: string;
  /** In the role menu the tag shrinks with the row, as in the mockup. */
  shrinks?: boolean;
}) {
  return (
    <span
      data-f="rtag"
      aria-hidden="true"
      className={cn(
        "inline-block size-2 rounded-[2px]",
        !shrinks && "shrink-0",
        ROLE_TAG[role],
        className,
      )}
    />
  );
}

const GRID =
  "grid grid-cols-[minmax(0,1fr)_150px_210px_104px_36px] items-center gap-3.5 px-3.5";

export interface MemberMenuState {
  readonly key: string;
  readonly placement: "below" | "above";
}

export interface SpaceMembersViewProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly navigation: ShellNavigation;
  readonly spaceName: string;
  readonly members: SectionState<readonly SpaceMemberEntry[]>;
  readonly directory: ReadonlyMap<string, SpaceDirectoryPerson>;
  readonly teams: readonly SpaceTeam[] | null;
  readonly meId: string | null;
  readonly canManage: boolean;
  readonly pendingKeys: ReadonlySet<string>;
  /** People of the teams that were expanded, by team id. */
  readonly teamPeople: ReadonlyMap<
    string,
    SectionState<readonly TeamPersonView[]>
  >;
  readonly childSpaces: readonly {
    id: string;
    name: string;
    isPrivate: boolean;
  }[];
  readonly also?: ReadonlyMap<string, { team: string; role: SpaceRole }>;
  readonly formatDay: (iso: string) => string;
  readonly onExpandTeam: (teamId: string) => void;
  readonly onChangeRole: (entry: SpaceMemberEntry, role: SpaceRole) => void;
  readonly onAskRemove: (entry: SpaceMemberEntry) => void;
  readonly onRetry: () => void;
  readonly initialExpanded?: readonly string[];
  readonly initialMenu?: MemberMenuState | null;
  readonly initialFilters?: Partial<MemberFilters>;
  /** Waiting workspace invitations; none until a producer ties them to the department. */
  readonly invites?: readonly SpacePendingInviteView[];
  /** Rows before "Show n more"; defaults to 10. */
  readonly pageSize?: number;
}

function Chevron({ className }: { className?: string }) {
  return <Glyph name="chevronDown" className={cn("size-[13px]", className)} />;
}

function Pill({
  label,
  value,
  options,
  onChange,
}: {
  label: string;
  value: string;
  options: readonly { id: string; label: string }[];
  onChange: (id: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!open) return;
    const away = (event: PointerEvent) => {
      if (!ref.current?.contains(event.target as Node)) setOpen(false);
    };
    window.addEventListener("pointerdown", away);
    return () => window.removeEventListener("pointerdown", away);
  }, [open]);
  const current = options.find((option) => option.id === value);
  return (
    <div ref={ref} className="relative">
      <button
        data-f="pill"
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen((next) => !next)}
        onKeyDown={(event) => {
          if (event.key === "Escape") setOpen(false);
        }}
        className={cn(
          "inline-flex h-8 items-center gap-1.5 rounded-[7px] border-0 bg-transparent px-2.5 text-compact leading-[1.45] font-normal text-muted-foreground hover:bg-state-hover-strong",
          FOCUS,
        )}
      >
        {label}{" "}
        <b data-f="pillv" className="font-semibold text-foreground">
          {current?.label}
        </b>
        <Chevron />
      </button>
      {open ? (
        <div
          role="menu"
          aria-label={label}
          className="absolute top-9 left-0 z-dropdown box-content w-[180px] rounded-[10px] border border-line-strong bg-popover p-1 shadow-popover dark:bg-card"
        >
          {options.map((option) => (
            <button
              key={option.id}
              type="button"
              role="menuitemradio"
              aria-checked={option.id === value}
              onClick={() => {
                onChange(option.id);
                setOpen(false);
              }}
              className={cn(
                "flex h-8 w-full items-center gap-2 rounded-[7px] border-0 bg-transparent px-2 text-left text-compact leading-[1.45] font-normal text-foreground hover:bg-state-hover-strong",
                option.id === value && "font-semibold",
                FOCUS,
              )}
            >
              {option.label}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}

function RoleMenu({
  copy,
  row,
  placement,
  onPick,
  onRemove,
  onClose,
}: {
  copy: WorkspaceSpacesCopy;
  row: SpaceMemberRowView;
  placement: "below" | "above";
  onPick: (role: SpaceRole) => void;
  onRemove: () => void;
  onClose: (focusBack: boolean) => void;
}) {
  const m = copy.members.roleMenu;
  const menu = useRef<HTMLDivElement>(null);
  const roles: readonly SpaceRole[] = ["Admin", "Member", "Viewer"];
  const items = () =>
    Array.from(
      menu.current?.querySelectorAll<HTMLElement>(
        '[role="menuitemradio"],[role="menuitem"]',
      ) ?? [],
    );
  useEffect(() => {
    const list = items();
    (
      list.find((item) => item.getAttribute("aria-checked") === "true") ??
      list[0]
    )?.focus({ preventScroll: true });
  }, []);
  useEffect(() => {
    const away = (event: PointerEvent) => {
      if (!menu.current?.contains(event.target as Node)) onClose(false);
    };
    window.addEventListener("pointerdown", away);
    return () => window.removeEventListener("pointerdown", away);
  }, [onClose]);
  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === "Escape") {
      event.stopPropagation();
      onClose(true);
      return;
    }
    if (event.key === "Tab") {
      onClose(false);
      return;
    }
    const list = items();
    const index = list.indexOf(document.activeElement as HTMLElement);
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      const step = event.key === "ArrowDown" ? 1 : -1;
      list[(index + step + list.length) % list.length]?.focus();
    }
  };
  const hint = (role: SpaceRole) =>
    role === "Admin"
      ? m.AdminHint
      : role === "Member"
        ? m.MemberHint
        : m.ViewerHint;
  const label = (role: SpaceRole) => m[role];
  const blocked = row.blockedBy === "last-admin";
  const noteAlso = row.also
    ? formatCopy(m.noteAlso, {
        name: row.shortName,
        team: row.also.team,
        role: label(row.also.role),
      })
    : null;
  return (
    <div
      ref={menu}
      data-f="rmenu"
      role="menu"
      aria-label={formatCopy(m.label, { name: row.name })}
      onKeyDown={onKeyDown}
      className={cn(
        "absolute left-[228px] z-dropdown box-content w-[320px] rounded-[10px] border border-line-strong bg-popover py-1 text-nav font-normal shadow-popover dark:bg-card",
        placement === "below" ? "top-[52px]" : "bottom-[50px]",
      )}
    >
      {roles.map((role) => {
        const checked = row.entry.role === role;
        const disabled = !row.roles.includes(role);
        return (
          <div
            key={role}
            data-f="ro"
            role="menuitemradio"
            aria-checked={checked}
            aria-disabled={disabled ? true : undefined}
            tabIndex={-1}
            onClick={() => {
              if (disabled) return;
              onClose(true);
              if (!checked) onPick(role);
            }}
            onKeyDown={(event) => {
              if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                event.stopPropagation();
                if (disabled) return;
                onClose(true);
                if (!checked) onPick(role);
              }
            }}
            className={cn(
              "mx-1 flex items-start gap-2.5 rounded-[7px] px-3 py-[9px] hover:bg-state-hover-strong focus-visible:bg-state-hover-strong",
              disabled && "cursor-not-allowed opacity-55 hover:bg-transparent",
              !disabled && "cursor-pointer",
              "outline-none",
            )}
          >
            <RoleTag role={role} shrinks className="mt-1.5" />
            <span>
              <b
                data-f="rot"
                className="block text-nav leading-[1.45] font-semibold text-foreground"
              >
                {label(role)}
              </b>
              <small
                data-f="roh"
                className="block text-xs leading-[1.45] font-normal text-meta-foreground"
              >
                {disabled && blocked
                  ? formatCopy(m.lastAdminRole, { name: row.shortName })
                  : hint(role)}
              </small>
            </span>
            {checked ? (
              <Glyph
                name="check"
                className="mt-0.5 ml-auto size-4 text-state-selected-foreground"
              />
            ) : null}
          </div>
        );
      })}
      <div data-f="rsep" role="separator" className="my-1 h-px bg-line" />
      <div
        data-f="ro"
        role="menuitem"
        aria-disabled={row.canRemove ? undefined : true}
        tabIndex={-1}
        onClick={() => {
          if (!row.canRemove) return;
          onClose(false);
          onRemove();
        }}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            event.stopPropagation();
            if (!row.canRemove) return;
            onClose(false);
            onRemove();
          }
        }}
        className={cn(
          "mx-1 flex items-start gap-2.5 rounded-[7px] px-3 py-[9px] outline-none hover:bg-state-hover-strong focus-visible:bg-state-hover-strong",
          row.canRemove
            ? "cursor-pointer"
            : "cursor-not-allowed opacity-55 hover:bg-transparent",
        )}
      >
        <span className="w-2" />
        <span>
          <b
            data-f="rot"
            className="block text-nav leading-[1.45] font-semibold text-danger-text"
          >
            {m.remove}
          </b>
          <small
            data-f="roh"
            className="block text-xs leading-[1.45] font-normal text-meta-foreground"
          >
            {row.canRemove ? m.removeHint : m.removeBlocked}
          </small>
        </span>
      </div>
      {blocked || noteAlso || row.canRemove ? (
        <div
          data-f="rnote"
          className="mt-1 flex gap-2 border-t border-line px-3 pt-2 pb-1.5 text-xs leading-normal font-normal text-muted-foreground"
        >
          <Glyph
            name="infoCircle"
            className="mt-px size-[14px] text-meta-foreground"
          />
          <span>
            {blocked
              ? formatCopy(m.noteLast, { name: row.shortName })
              : (noteAlso ?? m.noteHighest)}
          </span>
        </div>
      ) : null}
    </div>
  );
}

function InviteRow({
  copy,
  invite,
}: {
  copy: WorkspaceSpacesCopy;
  invite: SpacePendingInviteView;
}) {
  const m = copy.members;
  return (
    <div
      data-f="mr"
      role="row"
      className={cn(
        GRID,
        "relative box-content min-h-[56px] border-t border-line",
      )}
    >
      <span data-f="who" className="flex min-w-0 items-center gap-[11px]">
        <span
          data-f="av"
          aria-hidden="true"
          className="grid size-[30px] shrink-0 place-items-center rounded-full bg-state-hover-strong text-muted-foreground"
        >
          <Glyph name="mail" />
        </span>
        <span className="min-w-0">
          <b
            data-f="wname"
            className="block truncate text-body leading-[1.45] font-semibold text-foreground"
          >
            {invite.email}
          </b>
          <small
            data-f="wsub"
            className="block text-xs leading-[1.45] text-meta-foreground"
          >
            {[invite.name, invite.sent].filter(Boolean).join(" · ")}
          </small>
        </span>
      </span>
      <button
        data-f="rsel"
        type="button"
        aria-disabled="true"
        title={m.inviteUnavailable}
        className="inline-flex h-[30px] cursor-default items-center gap-1.5 justify-self-start rounded-[7px] border border-transparent bg-transparent px-[9px] text-compact leading-[1.45] font-medium text-foreground"
      >
        <RoleTag role={invite.role} />
        {m.roleMenu[invite.role]}
        <Chevron className="text-meta-foreground" />
      </button>
      <span
        data-f="src"
        className="flex flex-wrap gap-1 text-xs text-muted-foreground"
      >
        <span
          data-f="srcc"
          className="inline-flex min-h-[22px] items-center rounded-[5px] bg-warning-subtle px-[7px] py-0.5 leading-[1.4] font-semibold text-warning-text"
        >
          {m.invitePending}
        </span>
      </span>
      <span
        data-f="act"
        className="flex gap-2.5 text-xs leading-[1.45] text-meta-foreground"
      >
        <button
          type="button"
          aria-disabled="true"
          title={m.inviteUnavailable}
          className="cursor-default border-0 bg-transparent p-0 text-xs leading-[1.45] font-semibold text-state-selected-foreground"
        >
          {m.inviteResend}
        </button>
        <button
          type="button"
          aria-disabled="true"
          title={m.inviteUnavailable}
          className="cursor-default border-0 bg-transparent p-0 text-xs leading-[1.45] font-semibold text-danger-text"
        >
          {m.inviteRevoke}
        </button>
      </span>
      <span aria-hidden="true" />
    </div>
  );
}

function PersonLine({
  person,
  extra,
}: {
  person: TeamPersonView;
  extra: ReactNode;
}) {
  return (
    <div
      data-f="sp"
      className="flex min-h-[30px] items-center gap-2 text-compact leading-[1.45] text-foreground"
    >
      <SpaceAvatar
        person={{ id: person.id, name: person.name, tone: person.tone }}
        className="size-[22px] text-avatar-sm"
      />
      {person.name}
      {extra}
    </div>
  );
}

function MemberRow({
  copy,
  row,
  menuOpen,
  placement,
  expanded,
  isFirst,
  showHint,
  canManage,
  teamPeople,
  directIds,
  navigation,
  onToggle,
  onOpenMenu,
  onCloseMenu,
  onPick,
  onAskRemove,
}: {
  copy: WorkspaceSpacesCopy;
  row: SpaceMemberRowView;
  menuOpen: boolean;
  placement: "below" | "above";
  expanded: boolean;
  isFirst: boolean;
  /** The note on changing the people of a team shows once, under the first team. */
  showHint: boolean;
  canManage: boolean;
  teamPeople: SectionState<readonly TeamPersonView[]> | null;
  directIds: ReadonlySet<string>;
  navigation: ShellNavigation;
  onToggle: () => void;
  onOpenMenu: (anchor: HTMLElement | null) => void;
  onCloseMenu: (focusBack: boolean) => void;
  onPick: (role: SpaceRole) => void;
  onAskRemove: () => void;
}) {
  const m = copy.members;
  const rsel = useRef<HTMLButtonElement>(null);
  const label = copy.members.roleMenu[row.entry.role];
  const teams: WorkspaceDestination = { section: "teams" };
  return (
    <>
      <div
        data-f="mr"
        role="row"
        className={cn(
          GRID,
          "relative box-content min-h-[56px] border-t border-line hover:bg-background",
          isFirst && "border-t-0",
          menuOpen && "bg-state-selected hover:bg-state-selected",
        )}
      >
        <span data-f="who" className="flex min-w-0 items-center gap-[11px]">
          {row.kind === "team" ? (
            <button
              data-f="tw"
              type="button"
              aria-expanded={expanded}
              aria-label={formatCopy(expanded ? m.collapse : m.expand, {
                name: row.name,
              })}
              onClick={onToggle}
              className={cn(
                "-mr-1.5 grid size-[22px] shrink-0 place-items-center rounded-[6px] border-0 bg-transparent p-0 text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
                FOCUS,
              )}
            >
              <Glyph
                name="chevronDown"
                className={cn("size-[14px]", !expanded && "-rotate-90")}
              />
            </button>
          ) : null}
          {row.kind === "team" ? (
            <span
              data-f="tm"
              aria-hidden="true"
              className="grid size-[30px] shrink-0 place-items-center rounded-[9px] bg-state-hover-strong text-muted-foreground"
            >
              <Glyph name="team" />
            </span>
          ) : row.avatar ? (
            <SpaceAvatar
              person={row.avatar}
              className="size-[30px] text-avatar-lg"
            />
          ) : null}
          <span className="min-w-0">
            <b
              data-f="wname"
              className="block truncate text-body leading-[1.45] font-semibold text-foreground"
            >
              {row.name}
              {row.isYou ? (
                <span
                  data-f="you"
                  className="ml-1.5 rounded-[4px] bg-state-selected px-[5px] py-px text-key leading-[1.45] font-semibold text-state-selected-foreground"
                >
                  {m.you}
                </span>
              ) : null}
            </b>
            {row.detail ? (
              <small
                data-f="wsub"
                className="block text-xs leading-[1.45] text-meta-foreground"
              >
                {row.detail}
              </small>
            ) : null}
          </span>
        </span>
        <button
          ref={rsel}
          data-f="rsel"
          type="button"
          aria-haspopup="menu"
          aria-expanded={menuOpen}
          aria-disabled={canManage ? undefined : true}
          title={canManage ? undefined : m.readOnly}
          onClick={canManage ? () => onOpenMenu(rsel.current) : undefined}
          className={cn(
            "inline-flex h-[30px] items-center gap-1.5 justify-self-start rounded-[7px] border border-transparent bg-transparent px-[9px] text-compact leading-[1.45] font-medium text-foreground",
            canManage
              ? "hover:border-line-strong hover:bg-popover dark:hover:bg-card"
              : "cursor-default",
            menuOpen &&
              "border-primary bg-popover ring-2 ring-state-selected dark:bg-card",
            FOCUS,
          )}
        >
          <RoleTag role={row.entry.role} />
          {label}
          {canManage ? <Chevron className="text-meta-foreground" /> : null}
        </button>
        <span
          data-f="src"
          className="flex flex-wrap gap-1 text-xs leading-[1.45] text-muted-foreground"
        >
          {row.sources.map((source) => (
            <span
              key={source.text}
              data-f="srcc"
              className="inline-flex h-[22px] items-center gap-1 rounded-[5px] bg-state-hover-strong px-[7px]"
            >
              {source.icon ? <Glyph name="person" className="size-3" /> : null}
              {source.text}
            </span>
          ))}
        </span>
        <span
          data-f="act"
          className="text-xs leading-[1.45] text-meta-foreground"
        >
          {row.pending ? m.pending : row.activity}
        </span>
        {canManage ? (
          <button
            data-f="ib"
            type="button"
            aria-haspopup="menu"
            aria-label={formatCopy(m.rowActions, { name: row.name })}
            onClick={() => onOpenMenu(rsel.current)}
            className={cn(
              "grid size-8 shrink-0 place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
              FOCUS,
            )}
          >
            <Glyph name="more" />
          </button>
        ) : (
          <span aria-hidden="true" />
        )}
        {menuOpen ? (
          <RoleMenu
            copy={copy}
            row={row}
            placement={placement}
            onPick={onPick}
            onRemove={onAskRemove}
            onClose={(back) => {
              onCloseMenu(back);
              if (back) rsel.current?.focus();
            }}
          />
        ) : null}
      </div>
      {row.kind === "team" && expanded ? (
        <div
          data-f="sub"
          role="row"
          className="border-t border-line bg-background pt-1.5 pr-3.5 pb-2.5 pl-14"
        >
          {teamPeople === null || teamPeople.status === "loading" ? (
            <p
              className="m-0 py-1.5 text-xs text-meta-foreground"
              role="status"
            >
              {m.teamLoading}
            </p>
          ) : teamPeople.status === "error" ? (
            <p className="m-0 py-1.5 text-xs text-danger-text" role="alert">
              {m.teamError}
            </p>
          ) : teamPeople.data.length === 0 ? (
            <p className="m-0 py-1.5 text-xs text-meta-foreground">
              {m.teamEmpty}
            </p>
          ) : (
            teamPeople.data.map((person) => (
              <PersonLine
                key={person.id}
                person={person}
                extra={
                  directIds.has(person.id) || person.note ? (
                    <small
                      data-f="spn"
                      className="text-xs leading-[1.45] text-meta-foreground"
                    >
                      · {directIds.has(person.id) ? m.alsoDirect : person.note}
                    </small>
                  ) : null
                }
              />
            ))
          )}
          {showHint ? (
            <div
              data-f="hint"
              className="mt-1 text-xs leading-[1.45] text-meta-foreground"
            >
              {m.teamHint}
              <ShellLink
                destination={teams}
                navigation={navigation}
                className={cn(
                  "font-medium text-state-selected-foreground hover:underline",
                  FOCUS,
                )}
              >
                {m.teamHintLink}
              </ShellLink>
              {m.teamHintEnd}
            </div>
          ) : null}
        </div>
      ) : null}
    </>
  );
}

const ASIDE_H3 =
  "m-0 mb-2.5 text-compact leading-[1.45] font-semibold text-muted-foreground";

function Aside({
  copy,
  navigation,
  childSpaces,
}: {
  copy: WorkspaceSpacesCopy;
  navigation: ShellNavigation;
  childSpaces: readonly { id: string; name: string; isPrivate: boolean }[];
}) {
  const a = copy.members.aside;
  const roleRow = (role: SpaceRole, text: string) => (
    <div data-f="rl" className="flex items-start gap-2.5">
      <span className="mt-1.5 inline-flex">
        <RoleTag role={role} />
      </span>
      <span>
        <b className="block text-nav leading-[1.45] font-semibold text-foreground">
          {copy.members.roleMenu[role]}
        </b>
        <small className="block text-crumb leading-normal font-normal text-meta-foreground">
          {text}
        </small>
      </span>
    </div>
  );
  const open = childSpaces.filter((child) => !child.isPrivate);
  const closed = childSpaces.filter((child) => child.isPrivate);
  const link = (child: { id: string; name: string }) => (
    <ShellLink
      key={child.id}
      destination={{ section: "space", spaceId: child.id }}
      navigation={navigation}
      className={cn("text-state-selected-foreground", FOCUS)}
    >
      <b className="font-semibold">{child.name}</b>
    </ShellLink>
  );
  const names = (list: typeof childSpaces) =>
    list.flatMap((child, index) =>
      index === 0 ? [link(child)] : [", ", link(child)],
    );
  const sentence = (template: string, list: typeof childSpaces) => {
    const [before, after] = template.split("{{names}}");
    return (
      <>
        {before}
        {names(list)}
        {after}
      </>
    );
  };
  return (
    <aside
      data-f="aside"
      aria-label={a.label}
      className="box-border flex w-[330px] shrink-0 flex-col gap-[22px] overflow-auto border-l border-line bg-popover px-5 pt-5 pb-7 dark:bg-card"
    >
      <div data-f="ab">
        <h3 data-f="abh" className={ASIDE_H3}>
          {a.roles}
        </h3>
        <div data-f="rls" className="flex flex-col gap-3">
          {roleRow("Admin", a.adminHint)}
          {roleRow("Member", a.memberHint)}
          {roleRow("Viewer", a.viewerHint)}
        </div>
        <div
          data-f="map"
          className="mt-3 border-t border-line pt-3 text-crumb leading-[1.55] text-muted-foreground"
        >
          {a.map}
          <br />
          {a.mapAdmins}
        </div>
      </div>
      <div
        data-f="callout"
        className="rounded-[10px] border border-line bg-background p-3 text-crumb leading-[1.55] text-muted-foreground"
      >
        <b className="font-semibold text-foreground">{a.misreadLead}</b>
        {a.misreadText}
      </div>
      <div data-f="ab">
        <h3 data-f="abh" className={ASIDE_H3}>
          {a.inherit}
        </h3>
        <div
          data-f="callout"
          className="rounded-[10px] border border-line bg-popover p-3 text-crumb leading-[1.55] text-muted-foreground dark:bg-card"
        >
          {childSpaces.length === 0 ? (
            a.inheritNone
          ) : (
            <>
              {open.length > 0 ? sentence(a.inheritOpen, open) : null}
              {open.length > 0 && closed.length > 0 ? " " : null}
              {closed.length > 0
                ? sentence(
                    closed.length > 1
                      ? a.inheritPrivatePlural
                      : a.inheritPrivate,
                    closed,
                  )
                : null}
            </>
          )}
        </div>
      </div>
    </aside>
  );
}

export function SpaceMembersView(props: SpaceMembersViewProps) {
  const { copy, navigation, members, canManage } = props;
  const m = copy.members;
  const [filters, setFilters] = useState<MemberFilters>({
    ...NO_MEMBER_FILTERS,
    ...props.initialFilters,
  });
  const [expanded, setExpanded] = useState<ReadonlySet<string>>(
    () => new Set(props.initialExpanded ?? []),
  );
  const [menu, setMenu] = useState<MemberMenuState | null>(
    props.initialMenu ?? null,
  );
  const page = props.pageSize ?? DEFAULT_PAGE;
  const [shown, setShown] = useState(page);
  const listRef = useRef<HTMLDivElement>(null);

  const entries = members.status === "ready" ? members.data : null;
  const rows = useMemo(
    () =>
      entries
        ? buildMemberRows(copy, entries, {
            directory: props.directory,
            teams: props.teams,
            meId: props.meId,
            canManage,
            pendingKeys: props.pendingKeys,
            toneOf: identityToneFor,
            formatDay: props.formatDay,
            also: props.also,
          })
        : [],
    [
      canManage,
      copy,
      entries,
      props.also,
      props.directory,
      props.formatDay,
      props.meId,
      props.pendingKeys,
      props.teams,
    ],
  );
  const filtered = useMemo(
    () => filterMemberRows(rows, filters),
    [rows, filters],
  );
  const visible = filtered.slice(0, shown);
  const directIds = useMemo(
    () =>
      new Set(
        (entries ?? [])
          .filter((entry) => entry.subjectType === "User")
          .map((entry) => entry.subjectId),
      ),
    [entries],
  );

  const toggle = (row: SpaceMemberRowView) => {
    setExpanded((current) => {
      const next = new Set(current);
      if (next.has(row.key)) next.delete(row.key);
      else {
        next.add(row.key);
        props.onExpandTeam(row.entry.subjectId);
      }
      return next;
    });
  };
  // The teams that start expanded ask for their people.
  const asked = useRef(false);
  useEffect(() => {
    if (asked.current) return;
    asked.current = true;
    for (const key of props.initialExpanded ?? []) {
      const row = rows.find((candidate) => candidate.key === key);
      if (row) props.onExpandTeam(row.entry.subjectId);
    }
  }, [props, rows]);

  // A menu near the bottom of the list opens upwards.
  const openMenu = (row: SpaceMemberRowView, anchor: HTMLElement | null) => {
    const rect = anchor?.getBoundingClientRect();
    const below = rect ? window.innerHeight - rect.bottom : Infinity;
    setMenu({ key: row.key, placement: below < 330 ? "above" : "below" });
  };
  useLayoutEffect(() => {
    setShown(page);
  }, [filters, page]);

  const roleOptions: readonly { id: MemberRoleFilter; label: string }[] = [
    { id: "all", label: m.roleAll },
    { id: "Admin", label: m.roleMenu.Admin },
    { id: "Member", label: m.roleMenu.Member },
    { id: "Viewer", label: m.roleMenu.Viewer },
  ];
  const viaOptions: readonly { id: MemberViaFilter; label: string }[] = [
    { id: "any", label: m.viaAny },
    { id: "direct", label: m.viaDirect },
    { id: "team", label: m.viaTeam },
  ];

  return (
    <>
      <div
        data-f="col"
        className="relative flex min-w-0 flex-1 flex-col gap-3.5 overflow-auto px-7 pt-[18px] pb-8"
      >
        <div data-f="bar" className="flex flex-wrap items-center gap-2">
          <label
            data-f="field"
            className="box-border flex h-8 w-[220px] items-center gap-2 rounded-[7px] border border-line-strong bg-popover px-2.5 text-compact leading-[1.45] font-normal text-meta-foreground focus-within:border-primary focus-within:ring-2 focus-within:ring-state-selected dark:bg-card"
          >
            <Glyph name="search" />
            <span className="sr-only">{m.searchLabel}</span>
            <input
              data-f="inp"
              type="search"
              value={filters.query}
              placeholder={m.searchPlaceholder}
              onChange={(event) =>
                setFilters((current) => ({
                  ...current,
                  query: event.target.value,
                }))
              }
              className="h-full min-w-0 flex-1 border-0 bg-transparent text-compact leading-[1.45] font-normal text-foreground outline-none placeholder:text-meta-foreground"
            />
          </label>
          <Pill
            label={m.roleFilter}
            value={filters.role}
            options={roleOptions}
            onChange={(id) =>
              setFilters((current) => ({
                ...current,
                role: id as MemberRoleFilter,
              }))
            }
          />
          <Pill
            label={m.viaFilter}
            value={filters.via}
            options={viaOptions}
            onChange={(id) =>
              setFilters((current) => ({
                ...current,
                via: id as MemberViaFilter,
              }))
            }
          />
          <span className="flex-1" />
          {entries ? (
            <span
              data-f="sum"
              className="text-crumb leading-[1.45] text-meta-foreground"
            >
              {memberSummary(copy, entries)}
              {(props.invites ?? []).length > 0
                ? ` · ${formatCopy(m.summaryInvites, { count: (props.invites ?? []).length })}`
                : ""}
            </span>
          ) : null}
        </div>

        {members.status === "loading" ? (
          <div
            role="status"
            aria-label={m.loading}
            className="flex flex-col gap-2"
          >
            <Skeleton className="h-9 w-full rounded-[12px]" />
            <Skeleton className="h-14 w-full rounded-[12px]" />
            <Skeleton className="h-14 w-full rounded-[12px]" />
          </div>
        ) : members.status === "error" ? (
          <div
            role="alert"
            className="flex flex-wrap items-center justify-between gap-3 rounded-[12px] border border-line bg-popover px-4 py-3 dark:bg-card"
          >
            <p className="m-0 text-body text-muted-foreground">{m.error}</p>
            <button
              type="button"
              onClick={props.onRetry}
              className={cn(
                "inline-flex h-7 items-center rounded-[7px] border border-line-strong bg-popover px-2.5 text-crumb font-medium hover:bg-state-hover-strong dark:bg-card",
                FOCUS,
              )}
            >
              {m.retry}
            </button>
          </div>
        ) : (
          <div
            ref={listRef}
            data-f="list"
            role="table"
            aria-label={formatCopy(m.listLabel, { name: props.spaceName })}
            className="rounded-[12px] border border-line bg-popover dark:bg-card"
          >
            <div
              data-f="lh"
              role="row"
              className={cn(
                GRID,
                "box-content h-9 rounded-t-[12px] border-b border-line bg-background text-micro leading-[1.45] font-semibold text-meta-foreground",
              )}
            >
              <span role="columnheader">{m.head.who}</span>
              <span role="columnheader">{m.head.role}</span>
              <span role="columnheader">{m.head.via}</span>
              <span role="columnheader">{m.head.activity}</span>
              <span />
            </div>
            {visible.length === 0 ? (
              <p
                data-f="empty"
                className="m-0 px-4 py-6 text-center text-compact leading-[1.45] text-meta-foreground"
              >
                {rows.length === 0 ? m.empty : m.noMatches}
              </p>
            ) : (
              visible.map((row, index) => (
                <MemberRow
                  key={row.key}
                  copy={copy}
                  row={row}
                  isFirst={index === 0}
                  showHint={
                    row.kind === "team" &&
                    visible.findIndex((item) => item.kind === "team") === index
                  }
                  canManage={canManage}
                  menuOpen={menu?.key === row.key}
                  placement={menu?.placement ?? "below"}
                  expanded={expanded.has(row.key)}
                  teamPeople={props.teamPeople.get(row.entry.subjectId) ?? null}
                  directIds={directIds}
                  navigation={navigation}
                  onToggle={() => toggle(row)}
                  onOpenMenu={(anchor) =>
                    menu?.key === row.key
                      ? setMenu(null)
                      : openMenu(row, anchor)
                  }
                  onCloseMenu={() => setMenu(null)}
                  onPick={(role) => props.onChangeRole(row.entry, role)}
                  onAskRemove={() => props.onAskRemove(row.entry)}
                />
              ))
            )}
            {(props.invites ?? []).map((invite) => (
              <InviteRow key={invite.id} copy={copy} invite={invite} />
            ))}
            {filtered.length > shown ? (
              <button
                data-f="more-row"
                type="button"
                onClick={() => setShown(filtered.length)}
                className={cn(
                  "flex h-11 w-full items-center justify-center rounded-b-[12px] border-0 border-t border-line bg-transparent text-compact leading-[1.45] font-normal text-state-selected-foreground hover:bg-background",
                  FOCUS,
                )}
              >
                {formatCopy(m.moreRows, { count: filtered.length - shown })}
              </button>
            ) : null}
          </div>
        )}
      </div>
      <Aside
        copy={copy}
        navigation={navigation}
        childSpaces={props.childSpaces}
      />
    </>
  );
}
