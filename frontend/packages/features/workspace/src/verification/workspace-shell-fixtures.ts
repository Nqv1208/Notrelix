/**
 * Deterministic fixtures and scenarios for the workspace shell surfaces
 * (WX-SHELL-001…005). No clocks, no randomness, no network.
 */
import {
  buildWorkspaceBreadcrumb,
  type WorkspaceBreadcrumbItem,
} from "../core/model/breadcrumb";
import {
  NAVIGATION_RESOURCE_KIND,
  markToneFor,
  type NavigationEntry,
  type WorkspaceDestination,
} from "../core/model/navigation";
import {
  workspaceBreadcrumbLabels,
  type NavigationListState,
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

export function navigationEntry(
  overrides: Partial<NavigationEntry> &
    Pick<NavigationEntry, "resourceId" | "name">,
): NavigationEntry {
  return {
    kind: NAVIGATION_RESOURCE_KIND.board,
    at: "2026-10-09T08:00:00.000Z",
    pinned: false,
    ...overrides,
  };
}

export function pinnedEntriesFixture(): NavigationEntry[] {
  return [
    navigationEntry({
      resourceId: "board-roadmap",
      name: "Product Roadmap",
      pinned: true,
    }),
    navigationEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      resourceId: "doc-spec",
      name: "Q4 product specification",
      pinned: true,
    }),
  ];
}

export function recentEntriesFixture(): NavigationEntry[] {
  return [
    navigationEntry({ resourceId: "board-launch", name: "Launch checklist" }),
    navigationEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      resourceId: "doc-retro",
      name: "Sprint 42 retro",
    }),
    navigationEntry({ resourceId: "board-bugs", name: "Bug triage" }),
  ];
}

function ready(entries: NavigationEntry[]): NavigationListState {
  return { status: "ready", entries };
}

type SidebarScenario = Omit<
  WorkspaceSidebarSurfaceProps,
  | "navigation"
  | "onSwitchWorkspace"
  | "onCreateWorkspace"
  | "onTogglePin"
  | "onRetryNavigation"
  | "onOpenShortcuts"
  | "onSignOut"
>;

export function sidebarDefaultScenario(): SidebarScenario {
  return {
    copy: shellCopyFixture(),
    workspace: shellWorkspace({
      id: "ws-product",
      name: "Product",
      mark: "blue",
    }),
    workspaces: shellWorkspaceList(),
    location: { section: "board", boardId: "board-roadmap" },
    pinned: ready(pinnedEntriesFixture()),
    recent: ready(recentEntriesFixture()),
    user: shellUser(),
  };
}

export function sidebarLoadingScenario(): SidebarScenario {
  return {
    ...sidebarDefaultScenario(),
    workspace: null,
    location: { section: "home" },
    pinned: { status: "loading" },
    recent: { status: "loading" },
  };
}

export function sidebarEmptyScenario(): SidebarScenario {
  return {
    ...sidebarDefaultScenario(),
    location: { section: "home" },
    pinned: ready([]),
    recent: ready([]),
  };
}

export function sidebarErrorScenario(): SidebarScenario {
  return {
    ...sidebarDefaultScenario(),
    location: { section: "members" },
    pinned: { status: "error" },
    recent: { status: "error" },
  };
}

export function sidebarEdgeDataScenario(): SidebarScenario {
  const longName =
    "Kế hoạch ra mắt sản phẩm toàn cầu — giai đoạn hai (bản nháp cuối cùng)";
  return {
    ...sidebarDefaultScenario(),
    copy: shellCopyFixture("vi"),
    workspace: shellWorkspace({
      id: "ws-global",
      name: "Không gian làm việc của nhóm Vận hành Toàn cầu",
      role: "guest",
      mark: "crimson",
    }),
    location: { section: "doc", docId: "doc-edge-3" },
    pinned: ready(
      Array.from({ length: 6 }, (_, index) =>
        navigationEntry({
          resourceId: `board-edge-${index}`,
          name: index === 0 ? longName : `Bảng số ${index + 1}`,
          pinned: true,
        }),
      ),
    ),
    recent: ready(
      Array.from({ length: 8 }, (_, index) =>
        navigationEntry({
          kind: NAVIGATION_RESOURCE_KIND.doc,
          resourceId: `doc-edge-${index}`,
          name: index === 1 ? `${longName} 📄` : `Tài liệu ${index + 1}`,
        }),
      ),
    ),
    user: shellUser({
      name: "Nguyễn Thị Phương Thảo",
      email: "nguyen.thi.phuong.thao.operations@notrelix.test",
      identity: "sage",
    }),
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
