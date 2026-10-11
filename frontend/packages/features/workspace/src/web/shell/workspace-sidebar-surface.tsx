import * as React from "react";
import {
  Check,
  ChevronRight,
  ChevronsUpDown,
  FileText,
  Home,
  Inbox,
  Keyboard,
  Library,
  LogOut,
  MoreHorizontal,
  Palette,
  Pin,
  PinOff,
  Plus,
  Search,
  Settings,
  SquareKanban,
  User,
  UserRoundCheck,
  Users,
  UsersRound,
  type LucideIcon,
} from "lucide-react";
import {
  Button,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuAction,
  SidebarMenuButton,
  SidebarMenuItem,
  Skeleton,
  useSidebar,
} from "@notrelix/ui-web";
import {
  destinationForEntry,
  isDestinationActive,
  NAVIGATION_RESOURCE_KIND,
  type NavigationEntry,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import { SpaceTreeNav, type SpaceTreeNavProps } from "../spaces/space-tree-nav";
import { IdentityMark, ShellLink } from "./shell-link";
import {
  formatCopy,
  type NavigationListState,
  type ShellCurrentWorkspace,
  type ShellNavigation,
  type ShellUser,
  type ShellWorkspace,
} from "./shell-models";
import type { WorkspaceShellCopy } from "./workspace-shell.copy";

export interface WorkspaceSidebarSurfaceProps {
  readonly copy: WorkspaceShellCopy;
  readonly navigation: ShellNavigation;
  /** `null` while the workspace record loads. */
  readonly workspace: ShellCurrentWorkspace | null;
  readonly workspaces: readonly ShellWorkspace[];
  /** The current location in the product hierarchy. */
  readonly location: WorkspaceDestination | null;
  readonly pinned: NavigationListState;
  readonly recent: NavigationListState;
  /** The Space org tree (OT-5); omitted by hosts without Spaces. */
  readonly spaces?: Omit<SpaceTreeNavProps, "navigation" | "onNavigated">;
  readonly user: ShellUser | null;
  readonly signingOut?: boolean;
  readonly onSwitchWorkspace: (workspaceId: string) => void;
  readonly onCreateWorkspace: () => void;
  readonly onTogglePin: (entry: NavigationEntry, pinned: boolean) => void;
  readonly onRetryNavigation: () => void;
  readonly onOpenShortcuts: () => void;
  readonly onSignOut: () => void;
}

const ACTIVE_ITEM =
  "data-active:bg-state-selected data-active:text-state-selected-foreground data-active:font-medium focus-visible:ring-focus-ring";

interface NavItem {
  readonly id: string;
  readonly label: string;
  readonly icon: LucideIcon;
  readonly destination: WorkspaceDestination;
}

function useCloseMobileDrawer(): () => void {
  const { isMobile, setOpenMobile } = useSidebar();
  return React.useCallback(() => {
    if (isMobile) setOpenMobile(false);
  }, [isMobile, setOpenMobile]);
}

function NavList({
  items,
  location,
  navigation,
}: {
  items: readonly NavItem[];
  location: WorkspaceDestination | null;
  navigation: ShellNavigation;
}) {
  const closeDrawer = useCloseMobileDrawer();
  return (
    <SidebarMenu>
      {items.map((item) => {
        const active = isDestinationActive(item.destination, location);
        return (
          <SidebarMenuItem key={item.id}>
            <SidebarMenuButton
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
                <item.icon aria-hidden="true" />
                <span>{item.label}</span>
              </ShellLink>
            </SidebarMenuButton>
          </SidebarMenuItem>
        );
      })}
    </SidebarMenu>
  );
}

function WorkspaceSwitcher({
  copy,
  workspace,
  workspaces,
  onSwitchWorkspace,
  onCreateWorkspace,
}: Pick<
  WorkspaceSidebarSurfaceProps,
  | "copy"
  | "workspace"
  | "workspaces"
  | "onSwitchWorkspace"
  | "onCreateWorkspace"
>) {
  if (!workspace) {
    return (
      <div
        className="flex h-12 items-center gap-2 px-2"
        role="status"
        aria-label={copy.switcher.loading}
      >
        <Skeleton className="size-8 rounded-md" />
        <div className="flex flex-1 flex-col gap-1.5">
          <Skeleton className="h-3.5 w-3/4" />
          <Skeleton className="h-3 w-1/3" />
        </div>
      </div>
    );
  }

  const roleLabel = workspace.role ? copy.switcher.roles[workspace.role] : null;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <SidebarMenuButton
          size="lg"
          aria-label={formatCopy(copy.switcher.trigger, {
            name: workspace.name,
          })}
          className="data-[state=open]:bg-sidebar-accent focus-visible:ring-focus-ring"
        >
          <IdentityMark name={workspace.name} tone={workspace.identity} />
          <span className="flex min-w-0 flex-1 flex-col text-left">
            <span className="truncate text-label text-foreground">
              {workspace.name}
            </span>
            {roleLabel ? (
              <span className="truncate text-metadata text-muted-foreground">
                {roleLabel}
              </span>
            ) : null}
          </span>
          <ChevronsUpDown
            aria-hidden="true"
            className="ml-auto text-muted-foreground"
          />
        </SidebarMenuButton>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" sideOffset={4} className="w-64">
        <DropdownMenuLabel className="text-metadata text-muted-foreground">
          {copy.switcher.heading}
        </DropdownMenuLabel>
        {workspaces.map((item) => {
          const current = item.id === workspace.id;
          return (
            <DropdownMenuItem
              key={item.id}
              onSelect={() => {
                if (!current) onSwitchWorkspace(item.id);
              }}
              className="gap-2"
            >
              <IdentityMark name={item.name} tone={item.identity} size="sm" />
              <span className="min-w-0 flex-1 truncate">{item.name}</span>
              {current ? (
                <>
                  <Check aria-hidden="true" className="text-icon" />
                  <span className="sr-only">{copy.switcher.current}</span>
                </>
              ) : null}
            </DropdownMenuItem>
          );
        })}
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={onCreateWorkspace} className="gap-2">
          <Plus aria-hidden="true" className="text-icon" />
          <span>{copy.switcher.create}</span>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function ResourceList({
  title,
  state,
  emptyHint,
  copy,
  location,
  navigation,
  onTogglePin,
  onRetry,
}: {
  title: string;
  state: NavigationListState;
  emptyHint: string;
  copy: WorkspaceShellCopy;
  location: WorkspaceDestination | null;
  navigation: ShellNavigation;
  onTogglePin: WorkspaceSidebarSurfaceProps["onTogglePin"];
  onRetry: () => void;
}) {
  const closeDrawer = useCloseMobileDrawer();

  return (
    <Collapsible defaultOpen className="group/collapsible">
      <SidebarGroup className="py-1">
        <SidebarGroupLabel asChild>
          <CollapsibleTrigger
            aria-label={formatCopy(copy.lists.toggle, { section: title })}
            className="w-full gap-1 text-metadata font-medium text-muted-foreground hover:text-foreground focus-visible:ring-focus-ring"
          >
            <ChevronRight
              aria-hidden="true"
              className="transition-transform duration-fast group-data-[state=open]/collapsible:rotate-90"
            />
            <span>{title}</span>
          </CollapsibleTrigger>
        </SidebarGroupLabel>
        <CollapsibleContent>
          <SidebarGroupContent>
            {state.status === "loading" ? (
              <div className="flex flex-col gap-1 px-2 py-1" aria-hidden="true">
                {["w-3/4", "w-1/2", "w-2/3"].map((width) => (
                  <div key={width} className="flex h-8 items-center gap-2">
                    <Skeleton className="size-4 rounded-sm" />
                    <Skeleton className={`h-3.5 ${width}`} />
                  </div>
                ))}
              </div>
            ) : state.status === "error" ? (
              <div className="flex items-center justify-between gap-2 px-2 py-1.5">
                <p className="text-metadata text-muted-foreground">
                  {copy.lists.error}
                </p>
                <Button variant="ghost" size="xs" onClick={onRetry}>
                  {copy.lists.retry}
                </Button>
              </div>
            ) : state.entries.length === 0 ? (
              <p className="px-2 py-1.5 text-metadata text-muted-foreground">
                {emptyHint}
              </p>
            ) : (
              <SidebarMenu>
                {state.entries.map((entry) => {
                  const destination = destinationForEntry(entry);
                  const active = isDestinationActive(destination, location);
                  const isBoard = entry.kind === NAVIGATION_RESOURCE_KIND.board;
                  const Icon = isBoard ? SquareKanban : FileText;
                  return (
                    <SidebarMenuItem key={`${entry.kind}:${entry.resourceId}`}>
                      <SidebarMenuButton
                        asChild
                        isActive={active}
                        className={ACTIVE_ITEM}
                      >
                        <ShellLink
                          destination={destination}
                          navigation={navigation}
                          onNavigated={closeDrawer}
                          aria-current={active ? "page" : undefined}
                        >
                          <Icon aria-hidden="true" className="text-icon" />
                          <span className="sr-only">
                            {isBoard ? copy.lists.board : copy.lists.doc}:{" "}
                          </span>
                          <span>{entry.name}</span>
                        </ShellLink>
                      </SidebarMenuButton>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <SidebarMenuAction
                            showOnHover
                            aria-label={formatCopy(copy.lists.rowMenu, {
                              name: entry.name,
                            })}
                            className="focus-visible:ring-focus-ring"
                          >
                            <MoreHorizontal aria-hidden="true" />
                          </SidebarMenuAction>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent
                          side="right"
                          align="start"
                          className="w-44"
                        >
                          <DropdownMenuItem
                            onSelect={() => {
                              navigation.onNavigate(destination);
                              closeDrawer();
                            }}
                            className="gap-2"
                          >
                            <Icon aria-hidden="true" className="text-icon" />
                            {copy.lists.open}
                          </DropdownMenuItem>
                          <DropdownMenuItem
                            onSelect={() => onTogglePin(entry, !entry.pinned)}
                            className="gap-2"
                          >
                            {entry.pinned ? (
                              <PinOff
                                aria-hidden="true"
                                className="text-icon"
                              />
                            ) : (
                              <Pin aria-hidden="true" className="text-icon" />
                            )}
                            {entry.pinned ? copy.lists.unpin : copy.lists.pin}
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </SidebarMenuItem>
                  );
                })}
              </SidebarMenu>
            )}
          </SidebarGroupContent>
        </CollapsibleContent>
      </SidebarGroup>
    </Collapsible>
  );
}

function SidebarSpaces({
  spaces,
  navigation,
}: {
  spaces: NonNullable<WorkspaceSidebarSurfaceProps["spaces"]>;
  navigation: ShellNavigation;
}) {
  const closeDrawer = useCloseMobileDrawer();
  return (
    <SpaceTreeNav
      {...spaces}
      navigation={navigation}
      onNavigated={closeDrawer}
    />
  );
}

function UserCard({
  copy,
  user,
  navigation,
  signingOut,
  onOpenShortcuts,
  onSignOut,
}: Pick<
  WorkspaceSidebarSurfaceProps,
  | "copy"
  | "user"
  | "navigation"
  | "signingOut"
  | "onOpenShortcuts"
  | "onSignOut"
>) {
  const closeDrawer = useCloseMobileDrawer();
  const name = user?.name ?? copy.user.unknown;
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <SidebarMenuButton
          size="lg"
          aria-label={formatCopy(copy.user.menu, { name })}
          className="data-[state=open]:bg-sidebar-accent focus-visible:ring-focus-ring"
        >
          <IdentityMark
            name={name}
            tone={user?.identity ?? "blue"}
            shape="round"
            imageUrl={user?.avatarUrl}
          />
          <span className="flex min-w-0 flex-1 flex-col text-left">
            <span className="truncate text-label text-foreground">{name}</span>
            {user?.email ? (
              <span className="truncate text-metadata text-muted-foreground">
                {user.email}
              </span>
            ) : null}
          </span>
          <ChevronsUpDown
            aria-hidden="true"
            className="ml-auto text-muted-foreground"
          />
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
 * The one workspace sidebar (WX-SHELL-001), on the `Sidebar` primitive.
 * Renders inside a `SidebarProvider`; on mobile the primitive turns it into
 * a drawer. Every entry points at a real destination.
 */
export function WorkspaceSidebarSurface(props: WorkspaceSidebarSurfaceProps) {
  const { copy, navigation, location } = props;

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
    },
    {
      id: "inbox",
      label: copy.nav.inbox,
      icon: Inbox,
      destination: { section: "inbox" },
    },
    {
      id: "library",
      label: copy.nav.library,
      icon: Library,
      destination: { section: "library" },
    },
    {
      id: "search",
      label: copy.nav.search,
      icon: Search,
      destination: { section: "search" },
    },
  ];
  const manage: NavItem[] = [
    {
      id: "members",
      label: copy.nav.members,
      icon: Users,
      destination: { section: "members" },
    },
    {
      id: "teams",
      label: copy.nav.teams,
      icon: UsersRound,
      destination: { section: "teams" },
    },
    {
      id: "settings",
      label: copy.nav.settings,
      icon: Settings,
      destination: { section: "settings" },
    },
  ];

  return (
    <Sidebar
      mobileTitle={copy.nav.label}
      mobileDescription={props.workspace?.name ?? copy.switcher.loading}
      className="border-sidebar-border"
    >
      <SidebarHeader className="border-b border-sidebar-border">
        <SidebarMenu>
          <SidebarMenuItem>
            <WorkspaceSwitcher {...props} />
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent className="gap-0">
        <nav aria-label={copy.nav.label} className="contents">
          <SidebarGroup className="pb-1">
            <SidebarGroupLabel className="sr-only">
              {copy.nav.primary}
            </SidebarGroupLabel>
            <SidebarGroupContent>
              <NavList
                items={primary}
                location={location}
                navigation={navigation}
              />
            </SidebarGroupContent>
          </SidebarGroup>
          <ResourceList
            title={copy.lists.pinned}
            state={props.pinned}
            emptyHint={copy.lists.pinnedEmpty}
            copy={copy}
            location={location}
            navigation={navigation}
            onTogglePin={props.onTogglePin}
            onRetry={props.onRetryNavigation}
          />
          {props.spaces ? (
            <SidebarSpaces spaces={props.spaces} navigation={navigation} />
          ) : null}
          <ResourceList
            title={copy.lists.recent}
            state={props.recent}
            emptyHint={copy.lists.recentEmpty}
            copy={copy}
            location={location}
            navigation={navigation}
            onTogglePin={props.onTogglePin}
            onRetry={props.onRetryNavigation}
          />
          <SidebarGroup className="mt-auto">
            <SidebarGroupLabel className="text-metadata font-medium text-muted-foreground">
              {copy.nav.manage}
            </SidebarGroupLabel>
            <SidebarGroupContent>
              <NavList
                items={manage}
                location={location}
                navigation={navigation}
              />
            </SidebarGroupContent>
          </SidebarGroup>
        </nav>
      </SidebarContent>
      <SidebarFooter className="border-t border-sidebar-border">
        <SidebarMenu>
          <SidebarMenuItem>
            <UserCard {...props} />
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
    </Sidebar>
  );
}
