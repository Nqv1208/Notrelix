import * as React from "react";
import { Skeleton, cn } from "@notrelix/ui-web";
import { identityToneFor } from "../../core/model/navigation";
import type { WorkspaceDestination } from "../../core/model/navigation";
import type { SectionState } from "../home/home-models";
import { IdentityMark, ShellLink } from "../shell/shell-link";
import {
  formatCopy,
  isPlainClick,
  type ShellNavigation,
} from "../shell/shell-models";
import {
  BTN,
  BTN_PRIMARY,
  BTN_SM,
  FOCUS,
  Glyph,
  SpaceAvatar,
} from "../spaces/space-page-parts";
import {
  ArchiveTeamDialog,
  CreateTeamDialog,
  type CreateTeamValues,
  type TeamCandidate,
} from "./create-team-dialog";
import {
  filterTeamRows,
  teamInitials,
  type ArchiveLoss,
  type TeamRowView,
} from "./workspace-teams-models";
import {
  TeamTile,
  WorkspaceSetupFrame,
  type SetupFrameProps,
} from "./workspace-setup-frame";
import type { WorkspaceTeamsCopy } from "./workspace-teams.copy";

export type TeamsFilter = "active" | "archived";

export interface TeamsAdminView {
  readonly id: string;
  readonly name: string;
  readonly email: string | null;
  readonly role: "owner" | "admin";
}

export interface WorkspaceTeamsSurfaceProps {
  readonly copy: WorkspaceTeamsCopy;
  readonly frame: SetupFrameProps;
  /** Every Team with its extras: active ones first, then the archived. */
  readonly teams: SectionState<readonly TeamRowView[]>;
  /** Workspace admins and owners manage Teams. */
  readonly canManage: boolean;
  /** The admins a member who cannot manage Teams is told to ask. */
  readonly admins: readonly TeamsAdminView[];
  readonly navigation: ShellNavigation;
  readonly candidates: readonly TeamCandidate[];
  readonly creating: boolean;
  readonly createError: string | null;
  /** Team ids with a write in flight. */
  readonly pendingIds: ReadonlySet<string>;
  readonly lossFor: (teamId: string) => ArchiveLoss | null;
  readonly onCreate: (values: CreateTeamValues) => void;
  readonly onArchive: (teamId: string) => void;
  readonly onRestore: (teamId: string) => void;
  readonly onRetry: () => void;
  readonly formatDay: (iso: string) => string;
  /** Starts with the create dialog open (stories, tests). */
  readonly initialCreateOpen?: boolean;
  readonly initialMenuTeamId?: string | null;
  readonly initialArchiveTeamId?: string | null;
  readonly initialQuery?: string;
}

const TAV =
  "grid shrink-0 place-items-center rounded-[9px] text-mark-sm font-bold tracking-[0.02em]";
const COLS =
  "grid grid-cols-[minmax(0,1fr)_130px_172px_224px_76px] items-center gap-3.5 px-3.5";

function Stack({ row }: { row: TeamRowView }) {
  return (
    <span data-f="ppl" className="flex items-center gap-2 text-compact">
      <span data-f="stack" aria-hidden="true" className="flex">
        {row.people.avatars.map((person, index) => (
          <SpaceAvatar
            key={person.id}
            person={person}
            className={cn(
              "size-6 text-mark-xs shadow-[0_0_0_2px_var(--popover)]",
              index > 0 && "-ml-[5px]",
            )}
          />
        ))}
        {row.people.more > 0 ? (
          <span
            data-f="av"
            className="-ml-[5px] grid size-6 shrink-0 place-items-center rounded-full bg-state-hover-strong text-mark-xs font-bold text-muted-foreground"
          >
            +{row.people.more}
          </span>
        ) : null}
      </span>
    </span>
  );
}

function RowMenu({
  copy,
  row,
  onOpen,
  onArchive,
  onClose,
}: {
  copy: WorkspaceTeamsCopy;
  row: TeamRowView;
  onOpen: () => void;
  onArchive: () => void;
  onClose: () => void;
}) {
  const ref = React.useRef<HTMLDivElement>(null);
  React.useEffect(() => {
    ref.current?.querySelector<HTMLElement>("[role='menuitem']")?.focus();
    const away = (event: PointerEvent) => {
      if (!ref.current?.contains(event.target as Node)) onClose();
    };
    document.addEventListener("pointerdown", away);
    return () => document.removeEventListener("pointerdown", away);
  }, [onClose]);
  const item =
    "flex h-9 w-full items-center rounded-[7px] border-0 bg-transparent px-2.5 text-left text-compact leading-[1.45] text-foreground hover:bg-state-hover-strong";
  return (
    <div
      ref={ref}
      data-f="rmenu"
      role="menu"
      aria-label={formatCopy(copy.page.rowActions, { name: row.name })}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          event.stopPropagation();
          onClose();
        }
      }}
      className="absolute top-[46px] right-3.5 z-20 w-[200px] rounded-[10px] border border-line-strong bg-popover p-1 shadow-dialog dark:bg-card"
    >
      <button
        type="button"
        role="menuitem"
        onClick={onOpen}
        className={cn(item, FOCUS)}
      >
        {copy.page.menu.open}
      </button>
      <button
        type="button"
        role="menuitem"
        onClick={onArchive}
        className={cn(item, "text-danger-text", FOCUS)}
      >
        {copy.page.menu.archive}
      </button>
    </div>
  );
}

function TeamRow({
  copy,
  row,
  navigation,
  canManage,
  menuOpen,
  pending,
  onToggleMenu,
  onCloseMenu,
  onArchive,
  onRestore,
}: {
  copy: WorkspaceTeamsCopy;
  row: TeamRowView;
  navigation: ShellNavigation;
  canManage: boolean;
  menuOpen: boolean;
  pending: boolean;
  onToggleMenu: () => void;
  onCloseMenu: () => void;
  onArchive: () => void;
  onRestore: () => void;
}) {
  const href = navigation.getHref({ section: "teams", teamId: row.id });
  return (
    <div
      data-f="tr"
      role="row"
      className={cn(
        COLS,
        "relative box-border min-h-16 border-t border-line py-2 hover:bg-background",
        row.archived && "text-meta-foreground",
        menuOpen && "bg-state-selected hover:bg-state-selected",
      )}
    >
      <span
        data-f="tname"
        role="cell"
        className="flex min-w-0 items-center gap-[11px]"
      >
        {row.archived ? (
          <span
            data-f="tav"
            aria-hidden="true"
            className={cn(
              TAV,
              "size-[34px] bg-state-hover-strong text-meta-foreground",
            )}
          >
            {teamInitials(row.name)}
          </span>
        ) : (
          <TeamTile
            name={row.name}
            tone={row.tone}
            className="size-[34px] rounded-[9px] text-mark-sm"
          />
        )}
        <a
          href={href}
          onClick={(event) => {
            if (!isPlainClick(event)) return;
            event.preventDefault();
            navigation.onNavigate({ section: "teams", teamId: row.id });
          }}
          className={cn("group min-w-0", FOCUS)}
        >
          <b
            className={cn(
              "block truncate text-body leading-[1.45] font-semibold text-foreground group-hover:text-state-selected-foreground",
              row.archived && "font-medium text-muted-foreground",
            )}
          >
            {row.name}
          </b>
          <small className="block truncate text-xs leading-[1.45] text-meta-foreground">
            {row.archived ? row.archivedLine : row.description}
          </small>
        </a>
      </span>
      <span
        data-f="ppl"
        role="cell"
        className="flex items-center gap-2 text-compact"
      >
        {row.archived ? null : <Stack row={row} />}
        {formatCopy(copy.page.peopleCount, { count: row.people.count })}
      </span>
      <span
        data-f="lead"
        role="cell"
        className="flex min-w-0 items-center gap-2 text-compact"
      >
        {row.lead ? (
          <>
            <SpaceAvatar
              person={row.lead}
              className={cn(
                "size-[22px] text-mark-xs",
                row.archived && "opacity-70",
              )}
            />
            <span className="truncate">{row.lead.name}</span>
          </>
        ) : null}
      </span>
      <span
        data-f="src"
        role="cell"
        className="flex flex-wrap gap-1 text-xs text-muted-foreground"
      >
        {row.usage.length === 0 ? (
          <span data-f="none" className="text-meta-foreground">
            {copy.page.unused}
          </span>
        ) : (
          row.usage.map((usage) => (
            <span
              key={usage.key}
              data-f="srcc"
              className="inline-flex min-h-[22px] items-center gap-[5px] rounded-[5px] bg-state-hover-strong px-[7px] whitespace-nowrap"
            >
              {usage.mark ? (
                <IdentityMark
                  name={usage.mark.name}
                  markTone={usage.mark.tone}
                  size="sm"
                  className="size-[14px] rounded-[4px] text-mark-3xs"
                />
              ) : (
                <Glyph name="kanban" className="size-3" />
              )}
              {usage.text}
              {row.archived ? ` · ${copy.page.paused}` : ""}
            </span>
          ))
        )}
      </span>
      <span data-f="ract" role="cell" className="justify-self-end">
        {row.archived ? (
          canManage ? (
            <button
              type="button"
              disabled={pending}
              aria-label={formatCopy(copy.page.restoreLabel, {
                name: row.name,
              })}
              onClick={onRestore}
              className={cn(BTN_SM, FOCUS)}
            >
              {pending ? copy.page.restoring : copy.page.restore}
            </button>
          ) : null
        ) : canManage ? (
          <button
            type="button"
            aria-haspopup="menu"
            aria-expanded={menuOpen}
            aria-label={formatCopy(copy.page.rowActions, { name: row.name })}
            onClick={onToggleMenu}
            className={cn(
              "grid size-8 place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
              FOCUS,
            )}
          >
            <Glyph name="more" />
          </button>
        ) : null}
        {menuOpen ? (
          <RowMenu
            copy={copy}
            row={row}
            onOpen={() =>
              navigation.onNavigate({ section: "teams", teamId: row.id })
            }
            onArchive={onArchive}
            onClose={onCloseMenu}
          />
        ) : null}
      </span>
    </div>
  );
}

function ListSkeleton({ label }: { label: string }) {
  return (
    <div
      role="status"
      aria-label={label}
      className="overflow-hidden rounded-[12px] border border-line bg-popover dark:bg-card"
    >
      {[0, 1, 2, 3].map((row) => (
        <div
          key={row}
          className="flex h-16 items-center gap-3 border-t border-line px-3.5 first:border-t-0"
        >
          <Skeleton className="size-[34px] rounded-[9px]" />
          <Skeleton className="h-3.5 w-40" />
          <Skeleton className="ml-auto h-3.5 w-28" />
        </div>
      ))}
    </div>
  );
}

/**
 * The Teams of the workspace inside "Thiết lập workspace" (Final-Teams
 * `danh-sach`, `tao-moi`, `luu-tru`, `khong-co-quyen`): the table with the
 * people, the lead and where each Team is in use, the archived ones apart,
 * create and archive, and for a member who cannot manage Teams the page that
 * says whom to ask.
 */
export function WorkspaceTeamsSurface(props: WorkspaceTeamsSurfaceProps) {
  const { copy, navigation, canManage } = props;
  const [query, setQuery] = React.useState(props.initialQuery ?? "");
  const [createOpen, setCreateOpen] = React.useState(
    props.initialCreateOpen ?? false,
  );
  const [menuId, setMenuId] = React.useState<string | null>(
    props.initialMenuTeamId ?? null,
  );
  const [archiveId, setArchiveId] = React.useState<string | null>(
    props.initialArchiveTeamId ?? null,
  );
  const closeMenu = React.useCallback(() => setMenuId(null), []);

  // A member who cannot manage Teams gets the page that says whom to ask.
  if (!canManage && props.teams.status !== "loading") {
    const d = copy.deny;
    const home: WorkspaceDestination = { section: "home" };
    return (
      <WorkspaceSetupFrame copy={copy} frame={props.frame} hideNav>
        <section
          data-f="deny"
          aria-labelledby="teams-deny"
          className="grid flex-1 place-items-center p-6"
        >
          <div className="flex max-w-[440px] flex-col items-center gap-3 text-center">
            <span className="grid size-11 place-items-center rounded-[12px] bg-state-hover-strong text-muted-foreground">
              <Glyph name="lock" className="size-5" />
            </span>
            <h2 id="teams-deny" className="m-0 text-subheading text-foreground">
              {d.title}
            </h2>
            <p className="m-0 text-compact leading-[1.55] text-muted-foreground">
              {d.text}
            </p>
            <ul
              aria-label={formatCopy(d.adminsLabel, {
                workspace: props.frame.workspaceName,
              })}
              className="m-0 flex w-full list-none flex-col gap-2 p-0 text-left"
            >
              {props.admins.map((admin) => (
                <li
                  key={admin.id}
                  className="flex items-center gap-2.5 rounded-[10px] border border-line px-3 py-2"
                >
                  <SpaceAvatar
                    person={{
                      id: admin.id,
                      name: admin.name,
                      tone: identityToneFor(admin.id),
                    }}
                    className="size-[30px] text-avatar-lg"
                  />
                  <span className="min-w-0 flex-1">
                    <b className="block text-compact font-semibold text-foreground">
                      {admin.name}
                    </b>
                    <small className="block truncate text-xs text-meta-foreground">
                      {[admin.role === "owner" ? d.owner : d.admin, admin.email]
                        .filter(Boolean)
                        .join(" · ")}
                    </small>
                  </span>
                  <span
                    aria-disabled="true"
                    title={d.message}
                    className={cn(BTN_SM, "cursor-default")}
                  >
                    {d.message}
                  </span>
                </li>
              ))}
            </ul>
            <ShellLink
              destination={home}
              navigation={navigation}
              className={cn(BTN, FOCUS)}
            >
              {d.home}
            </ShellLink>
          </div>
        </section>
      </WorkspaceSetupFrame>
    );
  }

  const all = props.teams.status === "ready" ? props.teams.data : [];
  const active = all.filter((row) => !row.archived);
  const archived = all.filter((row) => row.archived);
  const shownActive = filterTeamRows(active, query);
  const shownArchived = filterTeamRows(archived, query);
  const archiveRow = archiveId ? all.find((row) => row.id === archiveId) : null;

  const head = (
    <div
      data-f="th"
      role="row"
      className={cn(
        COLS,
        "box-content h-9 rounded-t-[12px] border-b border-line bg-background text-micro leading-[1.45] font-semibold text-meta-foreground",
      )}
    >
      <span role="columnheader">{copy.page.head.team}</span>
      <span role="columnheader">{copy.page.head.people}</span>
      <span role="columnheader">{copy.page.head.lead}</span>
      <span role="columnheader">{copy.page.head.usage}</span>
      <span role="columnheader">
        <span className="sr-only">{copy.page.head.actions}</span>
      </span>
    </div>
  );

  return (
    <WorkspaceSetupFrame copy={copy} frame={props.frame}>
      <section aria-labelledby="teams-title" className="flex flex-col gap-4">
        <div data-f="ph" className="flex items-start gap-4">
          <div>
            <h2
              id="teams-title"
              className="m-0 text-lg leading-[1.45] font-[650] text-foreground"
            >
              {copy.page.title}
            </h2>
            <p className="m-0 mt-[3px] max-w-[66ch] text-compact leading-normal text-meta-foreground">
              {copy.page.description}
            </p>
          </div>
          <span className="flex-1" />
          {canManage ? (
            <button
              type="button"
              aria-haspopup="dialog"
              onClick={() => setCreateOpen(true)}
              className={cn(BTN_PRIMARY, FOCUS)}
            >
              <Glyph name="plus" />
              {copy.page.new}
            </button>
          ) : null}
        </div>
        <div data-f="bar" className="flex items-center gap-2.5">
          <label
            data-f="field"
            className="box-border flex h-8 w-[260px] items-center gap-2 rounded-[7px] border border-line-strong bg-popover px-2.5 text-compact text-meta-foreground focus-within:border-primary focus-within:ring-2 focus-within:ring-state-selected dark:bg-card"
          >
            <Glyph name="search" />
            <span className="sr-only">{copy.page.searchLabel}</span>
            <input
              type="search"
              value={query}
              placeholder={copy.page.searchPlaceholder}
              onChange={(event) => setQuery(event.target.value)}
              className="h-full min-w-0 flex-1 border-0 bg-transparent text-compact text-foreground outline-none placeholder:text-meta-foreground"
            />
          </label>
          <span className="flex-1" />
          {props.teams.status === "ready" ? (
            <span data-f="sum" className="text-crumb text-meta-foreground">
              {formatCopy(copy.page.summary, {
                active: active.length,
                archived: archived.length,
              })}
            </span>
          ) : null}
        </div>

        {props.teams.status === "loading" ? (
          <ListSkeleton label={copy.list.loading} />
        ) : props.teams.status === "error" ? (
          <div
            role="alert"
            className="flex flex-wrap items-center justify-between gap-3 rounded-[12px] border border-line bg-popover px-4 py-3 dark:bg-card"
          >
            <p className="m-0 text-body text-muted-foreground">
              {copy.list.error}
            </p>
            <button
              type="button"
              onClick={props.onRetry}
              className={cn(BTN_SM, FOCUS)}
            >
              {copy.list.retry}
            </button>
          </div>
        ) : (
          <>
            <div
              data-f="list"
              role="table"
              aria-label={formatCopy(copy.page.tableLabel, {
                workspace: props.frame.workspaceName,
              })}
              className="rounded-[12px] border border-line bg-popover dark:bg-card"
            >
              {head}
              {shownActive.length === 0 ? (
                <p
                  data-f="empty"
                  className="m-0 px-4 py-6 text-center text-compact text-meta-foreground"
                >
                  {active.length === 0
                    ? copy.list.empty.title
                    : copy.page.noMatches}
                </p>
              ) : (
                shownActive.map((row) => (
                  <TeamRow
                    key={row.id}
                    copy={copy}
                    row={row}
                    navigation={navigation}
                    canManage={canManage}
                    menuOpen={menuId === row.id}
                    pending={props.pendingIds.has(row.id)}
                    onToggleMenu={() =>
                      setMenuId((current) =>
                        current === row.id ? null : row.id,
                      )
                    }
                    onCloseMenu={closeMenu}
                    onArchive={() => {
                      setMenuId(null);
                      setArchiveId(row.id);
                    }}
                    onRestore={() => props.onRestore(row.id)}
                  />
                ))
              )}
            </div>
            {archived.length > 0 &&
            (query === "" || shownArchived.length > 0) ? (
              <>
                <div
                  data-f="sec"
                  className="mx-0.5 mt-1.5 -mb-1.5 flex items-center gap-2 text-crumb font-semibold text-meta-foreground"
                >
                  {formatCopy(copy.page.archivedSection, {
                    count: archived.length,
                  })}
                  <span className="ml-auto font-normal">
                    {copy.page.archivedNote}
                  </span>
                </div>
                <div
                  role="table"
                  aria-label={copy.page.archivedTableLabel}
                  className="rounded-[12px] border border-line bg-popover dark:bg-card"
                >
                  {shownArchived.map((row) => (
                    <TeamRow
                      key={row.id}
                      copy={copy}
                      row={row}
                      navigation={navigation}
                      canManage={canManage}
                      menuOpen={false}
                      pending={props.pendingIds.has(row.id)}
                      onToggleMenu={() => undefined}
                      onCloseMenu={closeMenu}
                      onArchive={() => undefined}
                      onRestore={() => props.onRestore(row.id)}
                    />
                  ))}
                </div>
              </>
            ) : null}
          </>
        )}
      </section>
      <CreateTeamDialog
        copy={copy}
        workspaceName={props.frame.workspaceName}
        open={createOpen}
        pending={props.creating}
        error={props.createError}
        candidates={props.candidates}
        onOpenChange={setCreateOpen}
        onSubmit={(values) => {
          props.onCreate(values);
        }}
      />
      <ArchiveTeamDialog
        copy={copy}
        open={archiveRow != null}
        teamName={archiveRow?.name ?? ""}
        loss={archiveRow ? props.lossFor(archiveRow.id) : null}
        pending={archiveRow ? props.pendingIds.has(archiveRow.id) : false}
        onClose={() => setArchiveId(null)}
        onConfirm={() => {
          if (archiveRow) props.onArchive(archiveRow.id);
          setArchiveId(null);
        }}
      />
    </WorkspaceSetupFrame>
  );
}
