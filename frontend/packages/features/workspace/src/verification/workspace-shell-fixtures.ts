/**
 * Deterministic fixtures and scenarios for the workspace shell surfaces
 * (WX-SHELL-001…005). No clocks, no randomness, no network.
 */
import {
  buildWorkspaceBreadcrumb,
  type WorkspaceBreadcrumbItem,
} from "../core/model/breadcrumb";
import {
  markToneFor,
  type WorkspaceDestination,
} from "../core/model/navigation";
import {
  workspaceBreadcrumbLabels,
  type ShellCurrentWorkspace,
  type ShellNavigation,
  type ShellPaletteMember,
  type ShellPaletteResource,
  type ShellUser,
  type ShellWorkspace,
} from "../web/shell/shell-models";
import {
  workspaceShellCopy,
  type WorkspaceShellCopy,
} from "../web/shell/workspace-shell.copy";
import type { WorkspaceSidebarSurfaceProps } from "../web/shell/workspace-sidebar-surface";

export const SHELL_FIXTURE_WORKSPACE_ID = "ws-product";

export function shellCopyFixture(
  locale: "en" | "vi" = "en",
): WorkspaceShellCopy {
  return workspaceShellCopy(locale);
}

/** Pure href builder mirroring the web host's routes. */
export function shellHrefFixture(
  destination: WorkspaceDestination,
  workspaceId = SHELL_FIXTURE_WORKSPACE_ID,
): string {
  const base = `/workspaces/${workspaceId}`;
  switch (destination.section) {
    case "home":
      return base;
    case "board":
      return `${base}/boards/${destination.boardId}`;
    case "doc":
      return `${base}/docs/${destination.docId}`;
    case "account":
      return `${base}/account/${destination.page ?? "profile"}`;
    case "space":
      return `${base}/spaces/${destination.spaceId}`;
    default:
      return `${base}/${destination.section}`;
  }
}

export function shellNavigationFixture(
  onNavigate: (destination: WorkspaceDestination) => void = () => undefined,
): ShellNavigation {
  return {
    getHref: (destination) => shellHrefFixture(destination),
    onNavigate,
  };
}

export function shellWorkspace(
  overrides: Partial<ShellCurrentWorkspace> &
    Pick<ShellCurrentWorkspace, "id" | "name">,
): ShellCurrentWorkspace {
  return {
    mark: markToneFor(overrides.id),
    role: "admin",
    ...overrides,
  };
}

export function shellWorkspaceList(): ShellWorkspace[] {
  return [
    { id: "ws-product", name: "Product", mark: "blue" },
    { id: "ws-design", name: "Design Studio", mark: "coral" },
    { id: "ws-ops", name: "Operations", mark: "sage" },
  ];
}

export function shellUser(overrides: Partial<ShellUser> = {}): ShellUser {
  return {
    id: "user-linh",
    name: "Linh Tran",
    email: "linh.tran@notrelix.test",
    avatarUrl: null,
    identity: "gold",
    ...overrides,
  };
}

type SidebarScenario = Omit<
  WorkspaceSidebarSurfaceProps,
  | "navigation"
  | "onOpenSystemRail"
  | "onOpenCommand"
  | "onOpenShortcuts"
  | "onSignOut"
>;

/** English product world: four departments, My work 7, Inbox 3. */
export function sidebarDefaultScenario(): SidebarScenario {
  return {
    copy: shellCopyFixture(),
    workspace: shellWorkspace({
      id: "ws-product",
      name: "Product",
      mark: "blue",
      departmentCount: 4,
      memberCount: 32,
    }),
    location: { section: "board", boardId: "board-roadmap" },
    counts: { myWork: 7, inbox: 3 },
    user: shellUser(),
    commandShortcut: "⌘K",
  };
}

/** Workspace record and counts still loading (counts unavailable). */
export function sidebarLoadingScenario(): SidebarScenario {
  return {
    ...sidebarDefaultScenario(),
    workspace: null,
    location: { section: "home" },
    counts: { myWork: null, inbox: null },
  };
}

/** Nothing to do yet: zero counts show no badge. */
export function sidebarEmptyScenario(): SidebarScenario {
  return {
    ...sidebarDefaultScenario(),
    location: { section: "home" },
    counts: { myWork: 0, inbox: 0 },
  };
}

/** Counts unavailable after a failed load (no badge, never "0"). */
export function sidebarErrorScenario(): SidebarScenario {
  return {
    ...sidebarDefaultScenario(),
    location: { section: "home" },
    counts: { myWork: null, inbox: null },
  };
}

/** The `Final-Space` mockup (state `tong-quan`): Vietnamese, Minh An Group. */
export function sidebarMockupScenario(): SidebarScenario {
  return {
    copy: shellCopyFixture("vi"),
    workspace: shellWorkspace({
      id: "ws-minh-an",
      name: "Minh An Group",
      mark: "blue",
      role: "admin",
      departmentCount: 5,
      memberCount: 86,
    }),
    location: { section: "space", spaceId: "sp-cn" },
    counts: { myWork: 7, inbox: 3 },
    user: shellUser({
      id: "user-thu-ha",
      name: "Nguyễn Thu Hà",
      email: "thuha.nguyen@minhan.test",
      identity: "sage",
    }),
    commandShortcut: "⌘K",
  };
}

/** Long names, a guest, My work 0 (no badge) and Inbox 120 ("99+"). */
export function sidebarEdgeDataScenario(): SidebarScenario {
  return {
    ...sidebarMockupScenario(),
    workspace: shellWorkspace({
      id: "ws-global",
      name: "Không gian làm việc của nhóm Vận hành Toàn cầu",
      role: "guest",
      mark: "crimson",
      departmentCount: 12,
      memberCount: 1250,
    }),
    location: { section: "board", boardId: "board-edge-03" },
    counts: { myWork: 0, inbox: 120 },
    user: shellUser({
      name: "Nguyễn Thị Phương Thảo",
      email: "nguyen.thi.phuong.thao.operations@notrelix.test",
      identity: "sage",
    }),
    commandShortcut: "Ctrl K",
  };
}

export const shellBoardsFixture: readonly ShellPaletteResource[] = [
  { id: "board-roadmap", name: "Product Roadmap" },
  { id: "board-launch", name: "Launch checklist" },
  { id: "board-bugs", name: "Bug triage" },
];

export const shellDocsFixture: readonly ShellPaletteResource[] = [
  { id: "doc-spec", name: "Q4 product specification" },
  { id: "doc-retro", name: "Sprint 42 retro" },
];

export function shellMembersFixture(
  copy = shellCopyFixture(),
): ShellPaletteMember[] {
  return [
    {
      id: "user-linh",
      name: "Linh Tran",
      identity: "gold",
      roleLabel: copy.switcher.roles.owner,
    },
    {
      id: "user-minh",
      name: "Minh Pham",
      identity: "blue",
      roleLabel: copy.switcher.roles.admin,
    },
    {
      id: "user-an",
      name: "An Le",
      identity: "sage",
      roleLabel: copy.switcher.roles.member,
    },
  ];
}

export function breadcrumbFixture(
  location: WorkspaceDestination | null,
  options: {
    loadingNames?: boolean;
    workspaceName?: string | null;
    copy?: WorkspaceShellCopy;
  } = {},
): WorkspaceBreadcrumbItem[] {
  const copy = options.copy ?? shellCopyFixture();
  return buildWorkspaceBreadcrumb({
    workspaceName:
      options.workspaceName === undefined ? "Product" : options.workspaceName,
    location,
    boards: options.loadingNames ? null : shellBoardsFixture,
    docs: options.loadingNames ? null : shellDocsFixture,
    labels: workspaceBreadcrumbLabels(copy),
  });
}
