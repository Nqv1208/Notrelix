import * as React from "react";
import {
  BarChart3,
  ChevronRight,
  ChevronsUpDown,
  FileText,
  Home,
  Inbox,
  Keyboard,
  LogOut,
  Palette,
  Search,
  Settings2,
  User,
  UserRoundCheck,
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
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
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
  isDestinationActive,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import { SpaceTreeNav, type SpaceTreeNavProps } from "../spaces/space-tree-nav";
import { IdentityMark, ShellLink } from "./shell-link";
import {
  formatCopy,
  pluralCopy,
  sidebarCountBadge,
  workspaceSetupLinks,
  type WorkspaceSetupLink,
  type ShellCurrentWorkspace,
  type ShellNavigation,
  type ShellSidebarCounts,
  type ShellUser,
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
  /**
   * Mobile drawer only: rendered first inside the drawer, above the
   * workspace chip (the system rail's workspaces on narrow screens).
   * Ignored on desktop.
   */
  readonly mobileSystemRail?: React.ReactNode;
}

const ACTIVE_ITEM =
  "data-active:bg-state-selected data-active:text-state-selected-foreground data-active:font-semibold hover:bg-sidebar-accent focus-visible:ring-focus-ring";

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

function CountBadge({ kind, count }: { kind: CountKind; count: number | null }) {
  const text = sidebarCountBadge(count);
  if (text === null) return null;
  return (
    <span
      aria-hidden="true"
      className={cn(
        "ml-auto shrink-0 text-metadata font-medium tabular-nums",
        kind === "inbox"
          ? "rounded-full bg-brand-solid px-1.5 font-semibold text-brand-solid-foreground"
          : "text-muted-foreground",
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
}: {
  copy: WorkspaceShellCopy;
  items: readonly NavItem[];
  counts: ShellSidebarCounts;
  location: WorkspaceDestination | null;
  navigation: ShellNavigation;
}) {
  const closeDrawer = useCloseMobileDrawer();
  return (
    <SidebarMenu aria-label={copy.nav.primary}>
      {items.map((item) => {
        const active = isDestinationActive(item.destination, location);
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
              className={cn("text-muted-foreground", ACTIVE_ITEM)}
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
      className="hover:bg-sidebar-accent focus-visible:ring-focus-ring"
    >
      <IdentityMark
        name={workspace.name}
        markTone={workspace.mark}
        className="size-7 rounded-lg"
      />
      <span className="flex min-w-0 flex-1 flex-col text-left">
        <span className="truncate text-label font-semibold text-foreground">
          {workspace.name}
        </span>
        {meta ? (
          <span className="truncate text-metadata text-muted-foreground">
            {meta}
          </span>
        ) : null}
      </span>
      <ChevronsUpDown
        aria-hidden="true"
        className="ml-auto text-muted-foreground"
      />
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
      className="flex h-(--size-row-dense) w-full items-center gap-2 rounded-md border border-sidebar-border bg-surface-panel px-2.5 text-left text-sm text-muted-foreground outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
    >
      <Search aria-hidden="true" className="size-4 shrink-0" />
      <span className="min-w-0 flex-1 truncate">{copy.sidebar.search}</span>
      <kbd className="shrink-0 rounded-sm border border-border bg-muted px-1 font-mono text-caption text-muted-foreground">
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
    location !== null &&
    links.some((link) => link === location.section);

  return (
    <Collapsible defaultOpen={inSetup} className="group/setup">
      <SidebarMenuItem>
        <CollapsibleTrigger asChild>
          <SidebarMenuButton className="text-muted-foreground hover:bg-sidebar-accent focus-visible:ring-focus-ring">
            <Settings2 aria-hidden="true" />
            <span className="truncate">{copy.sidebar.setup}</span>
            <ChevronRight
              aria-hidden="true"
              className="ml-auto transition-transform duration-fast group-data-[state=open]/setup:rotate-90"
            />
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
  const roleLine = workspace?.role ? copy.sidebar.roles[workspace.role] : null;
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <SidebarMenuButton
          size="lg"
          aria-label={formatCopy(copy.user.menu, { name })}
          className="hover:bg-sidebar-accent data-[state=open]:bg-sidebar-accent focus-visible:ring-focus-ring"
        >
          <IdentityMark
            name={name}
            tone={user?.identity ?? "blue"}
            shape="round"
            imageUrl={user?.avatarUrl}
            className="size-7"
          />
          <span className="flex min-w-0 flex-1 flex-col text-left">
            <span className="truncate text-label font-semibold text-foreground">
              {name}
            </span>
            {roleLine ? (
              <span className="truncate text-metadata text-muted-foreground">
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
 * primary destinations with counts, the department tree with its Boards,
 * and a footer with workspace setup and the member. Pins and recents live
 * on Home. Renders inside a `SidebarProvider`; on mobile the primitive turns
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
      icon: Home,
      destination: { section: "home" },
    },
    {
      id: "my-work",
      label: copy.nav.myWork,
      icon: UserRoundCheck,
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
      icon: FileText,
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
      className="border-sidebar-border"
    >
      {isMobile ? props.mobileSystemRail : null}
      <SidebarHeader className="gap-1.5 pb-1">
        <SidebarMenu>
          <SidebarMenuItem>
            <WorkspaceChip {...props} />
          </SidebarMenuItem>
        </SidebarMenu>
        <CommandRow {...props} />
      </SidebarHeader>
      <nav aria-label={copy.nav.label} className="contents">
        <SidebarContent className="gap-0">
          <SidebarGroup className="pt-1 pb-0">
            <SidebarGroupContent>
              <PrimaryNav
                copy={copy}
                items={primary}
                counts={props.counts}
                location={location}
                navigation={navigation}
              />
            </SidebarGroupContent>
          </SidebarGroup>
          {props.spaces ? (
            <SidebarSpaces
              spaces={props.spaces}
              location={location}
              navigation={navigation}
            />
          ) : null}
        </SidebarContent>
        {setupLinks.length > 0 ? (
          <SidebarFooter className="gap-1 pt-1">
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
      <SidebarFooter className="pt-0">
        <SidebarMenu>
          <SidebarMenuItem>
            <UserCard {...props} />
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
    </Sidebar>
  );
}
