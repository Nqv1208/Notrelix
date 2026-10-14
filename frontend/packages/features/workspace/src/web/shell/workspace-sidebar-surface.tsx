import * as React from "react";
import {
  BarChart3,
  ChevronsUpDown,
  File,
  House,
  Inbox,
  Keyboard,
  LogOut,
  Palette,
  PinOff,
  Search,
  Settings2,
  SquareCheckBig,
  User,
  type LucideIcon,
} from "lucide-react";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Sidebar,
  SidebarContent,
  SidebarFooter,
  Button,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuAction,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  Skeleton,
  cn,
  useSidebar,
} from "@notrelix/ui-web";
import {
  destinationForEntry,
  isDestinationActive,
  NAVIGATION_RESOURCE_KIND,
  type MarkToneId,
  type NavigationEntry,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import { SpaceTreeNav, type SpaceTreeNavProps } from "../spaces/space-tree-nav";
import { SidebarDocsTreeNav } from "./sidebar-docs-tree";
import { IdentityMark, ResourceMark, ShellLink } from "./shell-link";
import {
  formatCopy,
  pluralCopy,
  SIDEBAR_SECTION_LABEL_CLASS,
  sidebarCountBadge,
  workspaceSetupLinks,
  type WorkspaceSetupLink,
  type NavigationListState,
  type ShellCurrentWorkspace,
  type ShellNavigation,
  type ShellSidebarCounts,
  type ShellUser,
  type SidebarDocsTree,
} from "./shell-models";
import type { WorkspaceShellCopy } from "./workspace-shell.copy";

export interface WorkspaceSidebarSurfaceProps {
  readonly copy: WorkspaceShellCopy;
  readonly navigation: ShellNavigation;
  /** `null` while the workspace record loads. */
  readonly workspace: ShellCurrentWorkspace | null;
  /** The current location in the product hierarchy. */
  readonly location: WorkspaceDestination | null;
  /** My work and Inbox counts; `null` = unavailable (no badge, never "0"). */
  readonly counts: ShellSidebarCounts;
  /**
   * The member's pinned Boards and Docs (WX-DEC-011). The group shows above
   * the departments only when there is at least one pin (or pins failed).
   */
  readonly pinned: NavigationListState;
  /**
   * The document tree under the Docs row, shown while a document is open;
   * omitted elsewhere.
   */
  readonly docsTree?: SidebarDocsTree | null;
  /**
   * Boards the member opens without seeing their department (shared with them),
   * listed under "Được chia sẻ với bạn"; empty or omitted draws no section.
   */
  readonly sharedBoards?: readonly {
    readonly id: string;
    readonly name: string;
    readonly departmentName: string | null;
    readonly markTone: MarkToneId;
  }[];
  /** The department (Space) tree (OT-5); omitted by hosts without Spaces. */
  readonly spaces?: Omit<SpaceTreeNavProps, "navigation" | "onNavigated">;
  readonly user: ShellUser | null;
  readonly signingOut?: boolean;
  /** Platform label of the command palette shortcut ("⌘K", "Ctrl K"). */
  readonly commandShortcut: string;
  /** Whether the Notrelix system rail is open (the chip's `aria-expanded`). */
  readonly systemRailOpen?: boolean;
  /** The workspace chip opens the Notrelix system rail (workspace switching). */
  readonly onOpenSystemRail: () => void;
  readonly onOpenCommand: () => void;
  readonly onOpenShortcuts: () => void;
  readonly onSignOut: () => void;
  /** Pins or unpins a Board or Doc (the sidebar only unpins). */
  readonly onTogglePin: (entry: NavigationEntry, pinned: boolean) => void;
  /** Reloads the pinned list after an error. */
  readonly onRetryNavigation: () => void;
  /**
   * Mobile drawer only: rendered first inside the drawer, above the
   * workspace chip (the system rail's workspaces on narrow screens).
   * Ignored on desktop.
   */
  readonly mobileSystemRail?: React.ReactNode;
}

const ACTIVE_ITEM =
  "data-active:bg-state-selected data-active:text-state-selected-foreground data-active:font-semibold hover:bg-sidebar-accent focus-visible:ring-focus-ring";

/**
 * A 34 px navigation row (Final-Space `.side .nav`): 16 px icon, 10 px gap,
 * 10 px inline padding, 7 px radius, 13.5/500 muted text.
 */
const NAV_ROW =
  "h-8.5 gap-2.5 rounded-[7px] px-2.5 py-0 text-nav text-muted-foreground [&_svg]:stroke-[1.8]";

type CountKind = "myWork" | "inbox";

interface NavItem {
  readonly id: string;
  readonly label: string;
  readonly icon: LucideIcon;
  readonly destination: WorkspaceDestination;
  readonly count?: CountKind;
}

function useCloseMobileDrawer(): () => void {
  const { isMobile, setOpenMobile } = useSidebar();
  return React.useCallback(() => {
    if (isMobile) setOpenMobile(false);
  }, [isMobile, setOpenMobile]);
}

function CountBadge({
  kind,
  count,
}: {
  kind: CountKind;
  count: number | null;
}) {
  const text = sidebarCountBadge(count);
  if (text === null) return null;
  return (
    <span
      aria-hidden="true"
      className={cn(
        "ml-auto flex h-4.25 shrink-0 items-center text-metadata font-medium tabular-nums",
        kind === "inbox"
          ? "rounded-full bg-brand-solid px-1.75 font-semibold text-brand-solid-foreground"
          : "text-meta-foreground",
      )}
    >
      {text}
    </span>
  );
}

function PrimaryNav({
  copy,
  items,
  counts,
  location,
  navigation,
  docsTree,
}: {
  copy: WorkspaceShellCopy;
  items: readonly NavItem[];
  counts: ShellSidebarCounts;
  location: WorkspaceDestination | null;
  navigation: ShellNavigation;
  docsTree?: SidebarDocsTree | null;
}) {
  const closeDrawer = useCloseMobileDrawer();
  return (
    <SidebarMenu aria-label={copy.nav.primary} className="gap-0.5">
      {items.map((item) => {
        // A document page belongs to the Docs destination.
        const active =
          isDestinationActive(item.destination, location) ||
          (item.id === "library" && location?.section === "doc");
        const count = item.count ? counts[item.count] : null;
        const badge = sidebarCountBadge(count);
        const label =
          item.count && badge !== null && count !== null
            ? pluralCopy(
                item.count === "inbox"
                  ? {
                      one: copy.sidebar.inboxCount_one,
                      other: copy.sidebar.inboxCount_other,
                    }
                  : {
                      one: copy.sidebar.myWorkCount_one,
                      other: copy.sidebar.myWorkCount_other,
                    },
                count,
                { label: item.label },
              )
            : undefined;
        return (
          <SidebarMenuItem key={item.id}>
            <SidebarMenuButton
              asChild
              isActive={active}
              className={cn(NAV_ROW, ACTIVE_ITEM)}
            >
              <ShellLink
                destination={item.destination}
                navigation={navigation}
                onNavigated={closeDrawer}
                aria-current={active ? "page" : undefined}
                aria-label={label}
              >
                <item.icon aria-hidden="true" />
                <span className="truncate">{item.label}</span>
                {item.count ? (
                  <CountBadge kind={item.count} count={count} />
                ) : null}
              </ShellLink>
            </SidebarMenuButton>
            {item.id === "library" && docsTree ? (
              <div className="mt-0.5">
                <SidebarDocsTreeNav
                  copy={copy}
                  navigation={navigation}
                  tree={docsTree}
                  onNavigated={closeDrawer}
                />
              </div>
            ) : null}
          </SidebarMenuItem>
        );
      })}
    </SidebarMenu>
  );
}

function WorkspaceChip({
  copy,
  workspace,
  systemRailOpen,
  onOpenSystemRail,
}: Pick<
  WorkspaceSidebarSurfaceProps,
  "copy" | "workspace" | "systemRailOpen" | "onOpenSystemRail"
>) {
  if (!workspace) {
    return (
      <div
        className="flex h-12 items-center gap-2 px-2"
        role="status"
        aria-label={copy.switcher.loading}
      >
        <Skeleton className="size-7 rounded-md" />
        <div className="flex flex-1 flex-col gap-1.5">
          <Skeleton className="h-3.5 w-3/4" />
          <Skeleton className="h-3 w-1/3" />
        </div>
      </div>
    );
  }

  const meta =
    workspace.departmentCount != null && workspace.memberCount != null
      ? formatCopy(copy.sidebar.workspaceMeta, {
          departments: pluralCopy(
            {
              one: copy.sidebar.departments_one,
              other: copy.sidebar.departments_other,
            },
            workspace.departmentCount,
          ),
          members: pluralCopy(
            {
              one: copy.sidebar.people_one,
              other: copy.sidebar.people_other,
            },
            workspace.memberCount,
          ),
        })
      : workspace.role
        ? copy.sidebar.roles[workspace.role]
        : null;

  return (
    <SidebarMenuButton
      size="lg"
      type="button"
      onClick={onOpenSystemRail}
      // Hosts return focus here when the rail closes (WebKit does not focus
      // a clicked button).
      data-workspace-chip=""
      aria-label={formatCopy(copy.sidebar.workspaceChip, {
        name: workspace.name,
      })}
      aria-expanded={systemRailOpen}
      className="h-13.5 gap-2.5 rounded-md p-2 hover:bg-sidebar-accent focus-visible:ring-focus-ring [&_svg]:stroke-[1.8]"
    >
      <IdentityMark
        name={workspace.name}
        markTone={workspace.mark}
        className="size-7 rounded-md text-mark-md font-bold"
      />
      <span className="flex min-w-0 flex-1 flex-col text-left">
        <span className="truncate text-label font-semibold text-foreground">
          {workspace.name}
        </span>
        {meta ? (
          <span className="truncate text-metadata leading-4.25 text-meta-foreground">
            {meta}
          </span>
        ) : null}
      </span>
      <ChevronsUpDown aria-hidden="true" className="ml-auto" />
    </SidebarMenuButton>
  );
}

function CommandRow({
  copy,
  commandShortcut,
  onOpenCommand,
}: Pick<
  WorkspaceSidebarSurfaceProps,
  "copy" | "commandShortcut" | "onOpenCommand"
>) {
  return (
    <button
      type="button"
      onClick={onOpenCommand}
      className="mt-1.5 mb-2.5 flex h-8.5 w-full items-center gap-2 rounded-md border border-line bg-surface-panel px-2.5 text-left text-compact text-meta-foreground outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
    >
      <Search aria-hidden="true" className="size-4 shrink-0 stroke-[1.8]" />
      <span className="min-w-0 flex-1 truncate">{copy.sidebar.search}</span>
      <kbd className="shrink-0 rounded-[4px] border border-line-strong bg-keycap px-[5px] font-mono text-key text-muted-foreground">
        {commandShortcut}
      </kbd>
    </button>
  );
}

function setupLabel(
  copy: WorkspaceShellCopy,
  link: WorkspaceSetupLink,
): string {
  switch (link) {
    case "settings":
      return copy.sidebar.setupGeneral;
    case "members":
      return copy.sidebar.setupMembers;
    case "teams":
      return copy.sidebar.setupTeams;
    case "billing":
      return copy.sidebar.setupBilling;
  }
}

/**
 * The "Workspace setup" disclosure, gated by the member's workspace role
 * (owners/admins: every link; members: Members and Teams; guests: none).
 */
function WorkspaceSetup({
  copy,
  links,
  location,
  navigation,
}: {
  copy: WorkspaceShellCopy;
  links: readonly WorkspaceSetupLink[];
  location: WorkspaceDestination | null;
  navigation: ShellNavigation;
}) {
  const closeDrawer = useCloseMobileDrawer();
  const items = links.map((link) => ({
    label: setupLabel(copy, link),
    destination: { section: link } satisfies WorkspaceDestination,
  }));
  const inSetup =
    location !== null && links.some((link) => link === location.section);

  return (
    <Collapsible defaultOpen={inSetup} className="group/setup">
      <SidebarMenuItem>
        <CollapsibleTrigger asChild>
          <SidebarMenuButton
            className={cn(
              NAV_ROW,
              "hover:bg-sidebar-accent focus-visible:ring-focus-ring",
            )}
          >
            <Settings2 aria-hidden="true" />
            <span className="truncate">{copy.sidebar.setup}</span>
          </SidebarMenuButton>
        </CollapsibleTrigger>
        <CollapsibleContent>
          <SidebarMenuSub>
            {items.map((item) => {
              const active = isDestinationActive(item.destination, location);
              return (
                <SidebarMenuSubItem key={item.destination.section}>
                  <SidebarMenuSubButton
                    asChild
                    isActive={active}
                    className={ACTIVE_ITEM}
                  >
                    <ShellLink
                      destination={item.destination}
                      navigation={navigation}
                      onNavigated={closeDrawer}
                      aria-current={active ? "page" : undefined}
                    >
                      <span>{item.label}</span>
                    </ShellLink>
                  </SidebarMenuSubButton>
                </SidebarMenuSubItem>
              );
            })}
          </SidebarMenuSub>
        </CollapsibleContent>
      </SidebarMenuItem>
    </Collapsible>
  );
}

function UserCard({
  copy,
  user,
  workspace,
  navigation,
  signingOut,
  onOpenShortcuts,
  onSignOut,
}: Pick<
  WorkspaceSidebarSurfaceProps,
  | "copy"
  | "user"
  | "workspace"
  | "navigation"
  | "signingOut"
  | "onOpenShortcuts"
  | "onSignOut"
>) {
  const closeDrawer = useCloseMobileDrawer();
  const name = user?.name ?? copy.user.unknown;
  const roleLine =
    workspace?.roleLabel ??
    (workspace?.role ? copy.sidebar.roles[workspace.role] : null);
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <SidebarMenuButton
          size="lg"
          aria-label={formatCopy(copy.user.menu, { name })}
          className="h-13 gap-2.25 rounded-[7px] p-2 hover:bg-sidebar-accent data-[state=open]:bg-sidebar-accent focus-visible:ring-focus-ring"
        >
          <IdentityMark
            name={name}
            tone={user?.identity ?? "blue"}
            shape="round"
            imageUrl={user?.avatarUrl}
            className="size-6.5 text-mark-sm font-bold"
          />
          <span className="flex min-w-0 flex-1 flex-col text-left">
            <span className="truncate text-compact leading-4.75 font-semibold text-foreground">
              {name}
            </span>
            {roleLine ? (
              <span className="truncate text-metadata leading-4.25 text-meta-foreground">
                {roleLine}
              </span>
            ) : null}
          </span>
        </SidebarMenuButton>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        side="top"
        align="start"
        sideOffset={4}
        className="w-60"
      >
        <DropdownMenuItem
          onSelect={() => {
            navigation.onNavigate({ section: "account", page: "profile" });
            closeDrawer();
          }}
          className="gap-2"
        >
          <User aria-hidden="true" className="text-icon" />
          {copy.user.profile}
        </DropdownMenuItem>
        <DropdownMenuItem onSelect={onOpenShortcuts} className="gap-2">
          <Keyboard aria-hidden="true" className="text-icon" />
          {copy.user.shortcuts}
        </DropdownMenuItem>
        <DropdownMenuItem
          onSelect={() => {
            navigation.onNavigate({ section: "account", page: "appearance" });
            closeDrawer();
          }}
          className="gap-2"
        >
          <Palette aria-hidden="true" className="text-icon" />
          {copy.user.appearance}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onSelect={onSignOut}
          disabled={signingOut}
          className="gap-2"
        >
          <LogOut aria-hidden="true" className="text-icon" />
          {signingOut ? copy.user.signingOut : copy.user.signOut}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/**
 * Pinned Boards and Docs (WX-DEC-011), above the departments as in the
 * mobile mockup (`Final-M-Space` `danh-sach`). Hidden while loading and when
 * empty so members without pins see no empty group; an error stays visible
 * with a retry. Each row has a keyboard-reachable unpin action.
 */
function PinnedList({
  copy,
  state,
  location,
  navigation,
  onTogglePin,
  onRetry,
}: {
  copy: WorkspaceShellCopy;
  state: NavigationListState;
  location: WorkspaceDestination | null;
  navigation: ShellNavigation;
  onTogglePin: WorkspaceSidebarSurfaceProps["onTogglePin"];
  onRetry: () => void;
}) {
  const closeDrawer = useCloseMobileDrawer();
  if (state.status === "loading") return null;
  if (state.status === "ready" && state.entries.length === 0) return null;

  return (
    <SidebarGroup className="p-0">
      <div className={SIDEBAR_SECTION_LABEL_CLASS}>
        <span>
          {copy.sidebar.pinned}
          {state.status === "ready" ? (
            <span
              aria-hidden="true"
              className="ml-1.5 font-medium tabular-nums"
            >
              {state.entries.length}
            </span>
          ) : null}
        </span>
      </div>
      <SidebarGroupContent>
        {state.status === "error" ? (
          <div className="flex items-center justify-between gap-2 px-2 py-1.5">
            <p className="text-metadata text-muted-foreground">
              {copy.sidebar.pinnedError}
            </p>
            <Button variant="ghost" size="xs" onClick={onRetry}>
              {copy.sidebar.pinnedRetry}
            </Button>
          </div>
        ) : (
          <SidebarMenu aria-label={copy.sidebar.pinned} className="gap-0.5">
            {state.entries.map((entry) => {
              const destination = destinationForEntry(entry);
              const active = isDestinationActive(destination, location);
              const isBoard = entry.kind === NAVIGATION_RESOURCE_KIND.board;
              return (
                <SidebarMenuItem key={`${entry.kind}:${entry.resourceId}`}>
                  <SidebarMenuButton
                    asChild
                    isActive={active}
                    className={cn(NAV_ROW, ACTIVE_ITEM)}
                  >
                    <ShellLink
                      destination={destination}
                      navigation={navigation}
                      onNavigated={closeDrawer}
                      aria-current={active ? "page" : undefined}
                    >
                      <ResourceMark
                        kind={entry.kind}
                        id={entry.resourceId}
                        size="sm"
                      />
                      <span className="sr-only">
                        {isBoard ? copy.sidebar.board : copy.sidebar.doc}:{" "}
                      </span>
                      <span className="truncate" title={entry.name}>
                        {entry.name}
                      </span>
                    </ShellLink>
                  </SidebarMenuButton>
                  <SidebarMenuAction
                    showOnHover
                    type="button"
                    onClick={() => onTogglePin(entry, false)}
                    aria-label={formatCopy(copy.sidebar.unpin, {
                      name: entry.name,
                    })}
                    title={formatCopy(copy.sidebar.unpin, { name: entry.name })}
                    className="focus-visible:ring-focus-ring"
                  >
                    <PinOff aria-hidden="true" />
                  </SidebarMenuAction>
                </SidebarMenuItem>
              );
            })}
          </SidebarMenu>
        )}
      </SidebarGroupContent>
    </SidebarGroup>
  );
}

function SharedBoards({
  copy,
  boards,
  location,
  navigation,
}: {
  copy: WorkspaceShellCopy;
  boards: NonNullable<WorkspaceSidebarSurfaceProps["sharedBoards"]>;
  location: WorkspaceDestination | null;
  navigation: ShellNavigation;
}) {
  const closeDrawer = useCloseMobileDrawer();
  return (
    <SidebarGroup className="p-0">
      <div className={SIDEBAR_SECTION_LABEL_CLASS}>
        <span>{copy.sidebar.sharedTitle}</span>
      </div>
      <SidebarGroupContent>
        <SidebarMenu aria-label={copy.sidebar.sharedTitle} className="gap-0.5">
          {boards.map((board) => {
            const destination = {
              section: "board",
              boardId: board.id,
            } as const;
            const active = isDestinationActive(destination, location);
            return (
              <SidebarMenuItem key={board.id}>
                <SidebarMenuButton
                  asChild
                  isActive={active}
                  className={cn(NAV_ROW, ACTIVE_ITEM)}
                >
                  <ShellLink
                    destination={destination}
                    navigation={navigation}
                    onNavigated={closeDrawer}
                    aria-current={active ? "page" : undefined}
                  >
                    {board.departmentName ? (
                      <IdentityMark
                        size="sm"
                        name={board.departmentName}
                        markTone={board.markTone}
                      />
                    ) : (
                      <ResourceMark
                        kind="work-management.board"
                        id={board.id}
                        size="sm"
                      />
                    )}
                    <span className="truncate" title={board.name}>
                      {board.name}
                    </span>
                  </ShellLink>
                </SidebarMenuButton>
              </SidebarMenuItem>
            );
          })}
        </SidebarMenu>
      </SidebarGroupContent>
    </SidebarGroup>
  );
}

function SidebarSpaces({
  spaces,
  location,
  navigation,
}: {
  spaces: NonNullable<WorkspaceSidebarSurfaceProps["spaces"]>;
  location: WorkspaceDestination | null;
  navigation: ShellNavigation;
}) {
  const closeDrawer = useCloseMobileDrawer();
  return (
    <SpaceTreeNav
      activeBoardId={location?.section === "board" ? location.boardId : null}
      {...spaces}
      navigation={navigation}
      onNavigated={closeDrawer}
    />
  );
}

/**
 * The one workspace sidebar (WX-SHELL-001) at the October 2026 layout: the
 * workspace chip (opens the Notrelix system rail), the command row, the
 * primary destinations with counts, the pinned items (Ghim), the
 * department tree with its Boards, and a footer with workspace setup and
 * the member. Recents live on Home. Renders inside a `SidebarProvider`; on mobile the primitive turns
 * it into a drawer. Every entry points at a real destination.
 */
export function WorkspaceSidebarSurface(props: WorkspaceSidebarSurfaceProps) {
  const { copy, navigation, location } = props;
  const { isMobile } = useSidebar();
  const setupLinks = workspaceSetupLinks(props.workspace?.role ?? null);

  const primary: NavItem[] = [
    {
      id: "home",
      label: copy.nav.home,
      icon: House,
      destination: { section: "home" },
    },
    {
      id: "my-work",
      label: copy.nav.myWork,
      icon: SquareCheckBig,
      destination: { section: "my-work" },
      count: "myWork",
    },
    {
      id: "inbox",
      label: copy.nav.inbox,
      icon: Inbox,
      destination: { section: "inbox" },
      count: "inbox",
    },
    {
      id: "library",
      label: copy.nav.library,
      icon: File,
      destination: { section: "library" },
    },
    {
      id: "reports",
      label: copy.nav.reports,
      icon: BarChart3,
      destination: { section: "reports" },
    },
  ];

  return (
    <Sidebar
      mobileTitle={copy.nav.label}
      mobileDescription={props.workspace?.name ?? copy.switcher.loading}
      className="border-line"
    >
      {isMobile ? props.mobileSystemRail : null}
      <SidebarHeader className="gap-0.5 p-0 px-2.5 pt-2.5">
        <SidebarMenu>
          <SidebarMenuItem>
            <WorkspaceChip {...props} />
          </SidebarMenuItem>
        </SidebarMenu>
        <CommandRow {...props} />
      </SidebarHeader>
      <nav aria-label={copy.nav.label} className="contents">
        <SidebarContent className="gap-0.5 px-2.5">
          <SidebarGroup className="p-0">
            <SidebarGroupContent>
              <PrimaryNav
                copy={copy}
                items={primary}
                counts={props.counts}
                location={location}
                navigation={navigation}
                docsTree={props.docsTree}
              />
            </SidebarGroupContent>
          </SidebarGroup>
          <PinnedList
            copy={copy}
            state={props.pinned}
            location={location}
            navigation={navigation}
            onTogglePin={props.onTogglePin}
            onRetry={props.onRetryNavigation}
          />
          {props.spaces ? (
            <SidebarSpaces
              spaces={props.spaces}
              location={location}
              navigation={navigation}
            />
          ) : null}
          {props.sharedBoards && props.sharedBoards.length > 0 ? (
            <SharedBoards
              copy={copy}
              boards={props.sharedBoards}
              location={location}
              navigation={navigation}
            />
          ) : null}
        </SidebarContent>
        {setupLinks.length > 0 ? (
          <SidebarFooter className="gap-0.5 p-0 px-2.5 pt-0.5">
            <SidebarMenu>
              <WorkspaceSetup
                copy={copy}
                links={setupLinks}
                location={location}
                navigation={navigation}
              />
            </SidebarMenu>
          </SidebarFooter>
        ) : null}
      </nav>
      <SidebarFooter className="p-0 px-2.5 pt-1.5 pb-3">
        <SidebarMenu>
          <SidebarMenuItem>
            <UserCard {...props} />
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
    </Sidebar>
  );
}
